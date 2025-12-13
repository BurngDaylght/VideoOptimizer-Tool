using System;
using Zenject;
using UnityEngine;
using SFB;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;

public class FileProcessor : IInitializable, IDisposable
{
    public event Action OnOptimizeStart;
    public event Action OnOptimizeEnd;
    public event Action OnOptimizeStop;
    
    private int _quality = 23;
    
    private string[] _files;
    private string _ffmpeg = Path.Combine(Application.streamingAssetsPath, "FFmpeg/bin/ffmpeg.exe");
    
    private float _duration;
    private Process _currentProcess;
    
    private readonly FileSelector _fileSelector;
    private readonly ProgressBar _progressBar;
    private readonly NotificationService _notificationService;

    public FileProcessor(FileSelector fileSelector, ProgressBar progressBar, NotificationService notificationService)
    {
        _fileSelector = fileSelector;
        _progressBar = progressBar;
        _notificationService = notificationService;
    }
    
    public void Initialize() => _fileSelector.OnFilesSelected += SetFilesPaths;
    
    public void Dispose()
    {
        _fileSelector.OnFilesSelected -= SetFilesPaths;

        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            Debug.Log("[FileProcessor] Killing FFmpeg on dispose...");
            _currentProcess.Kill();
            _currentProcess.Dispose();
        }
    }
    
    public void StopOptimize()
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            Debug.Log("[FileProcessor] Stopping FFmpeg...");
            _currentProcess.Kill();
            _currentProcess.Dispose();
        }

        _files = null;
        
        OnOptimizeStop?.Invoke();
    }
    
    public async UniTask OptimizeFiles()
    {
        if (_files == null || _files.Length == 0)
        {
            Debug.LogWarning("[FileProcessor] No files selected!");
            return;
        }

        StandaloneFileBrowser.SaveFilePanelAsync(
            "Save File",
            Path.GetDirectoryName(_files[0]),
            Path.GetFileNameWithoutExtension(_files[0]) + "_optimized",
            Path.GetExtension(_files[0]).TrimStart('.'),
            chosenPath => HandleFileSave(chosenPath).Forget()
        );
    }
    
    private async UniTaskVoid HandleFileSave(string chosenPath)
    {
        if (string.IsNullOrEmpty(chosenPath))
        {
            Debug.Log("[FileProcessor] User canceled saving file.");
            return;
        }

        string extension = Path.GetExtension(chosenPath);

        foreach (var file in _files)
        {
            string outputFile = chosenPath;

            if (!outputFile.EndsWith(extension))
                outputFile += extension;

            OnOptimizeStart?.Invoke();

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
                await WaitForFileRelease(outputFile);
            }

            await RunFFmpeg(file, outputFile);
        }

        Debug.Log("[FileProcessor] All files optimized!");
    }
    
    private async UniTask RunFFmpeg(string inputFile, string outputPath)
    {
        long originalSize = new FileInfo(inputFile).Length;
        _duration = 0f;

        string args = $"-y -nostdin -hide_banner -i \"{inputFile}\" -vcodec libx264 -crf {_quality} \"{outputPath}\"";
        Debug.Log("[FFmpeg args] " + args);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = _ffmpeg,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        
        _currentProcess = process;

        var taskCompletionSource = new UniTaskCompletionSource();

        var timeRegex = new Regex(@"time=(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);
        var durationRegex = new Regex(@"Duration:\s(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log("[FFmpeg Out] " + e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            string data = e.Data;
            
            var durationMatch = durationRegex.Match(data);
            if (durationMatch.Success)
            {
                string durationString = durationMatch.Groups[1].Value;
                if (TryParseTimestampToSeconds(durationString, out float d))
                {
                    _duration = d;
                    Debug.Log($"[FileProcessor] Duration found: {_duration}s");
                }
            }

            var timeMatch = timeRegex.Match(data);
            if (timeMatch.Success && _duration > 0)
            {
                string timeString = timeMatch.Groups[1].Value;
                if (TryParseTimestampToSeconds(timeString, out float currentTime))
                {
                    float progress = Mathf.Clamp01(currentTime / _duration);

                    UniTask.Post(() => 
                    {
                        if (_progressBar != null) 
                            _progressBar.SetProgress(progress);
                    });
                }
            }
        };

        process.Exited += async (sender, e) =>
        {
            Debug.Log("[FFmpeg] Finished with exit code: " + process.ExitCode);
            
            await UniTask.SwitchToMainThread();

            _progressBar.SetProgress(1f);
            
            await UniTask.Delay(200);

            taskCompletionSource.TrySetResult();

            if (File.Exists(outputPath))
            {
                long compressedSize = new FileInfo(outputPath).Length;
                string orig = FormatBytes(originalSize);
                string comp = FormatBytes(compressedSize);
                float reduction = 100f - (compressedSize / (float)originalSize * 100f);
                string reductionText = reduction > 0  ? $"(-{reduction:F0}%)" : "(no reduction)";
                
                _notificationService.ShowNotification(NotificationType.CompressionSuccess, $"{orig}", $"{comp} {reductionText}");
            }
            else
            {
                Debug.LogError("[FileProcessor] Output file not found after FFmpeg exit!");
            }

            OnOptimizeEnd?.Invoke();
            _files = null;
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await taskCompletionSource.Task;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileProcessor] FFmpeg Start Error: {ex.Message}");
            taskCompletionSource.TrySetResult();
        }
    }

    private bool TryParseTimestampToSeconds(string timestamp, out float result)
    {
        result = 0f;
        try
        {
            string[] parts = timestamp.Split(':');
            if (parts.Length == 3)
            {
                float hours = float.Parse(parts[0]);
                float minutes = float.Parse(parts[1]);
                float seconds = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                result = hours * 3600 + minutes * 60 + seconds;
                return true;
            }
        }
        catch
        {
            
        }
        return false;
    }

    private string FormatBytes(long bytes)
    {
        float mb = bytes / 1048576f;
        if (mb > 1024)
            return (mb / 1024f).ToString("F2") + " GB";
        return mb.ToString("F2") + " MB";
    }
    
    private float ParseTimestampToSeconds(string timestamp)
    {
        string[] parts = timestamp.Split(':');
        float hours = float.Parse(parts[0]);
        float minutes = float.Parse(parts[1]);
        float seconds = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
        return hours * 3600 + minutes * 60 + seconds;
    }
    
    private async UniTask WaitForFileRelease(string path)
    {
        if (!File.Exists(path))
            return;

        while (true)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return;
                }
            }
            catch
            {
                await UniTask.Delay(50);
            }
        }
    }
    
    private void SetFilesPaths(string[] files) => _files = files;
    public void SetQuality(int quality) => _quality = Mathf.Clamp(quality, 0, 51);
    public bool IsFilesSelected() => _files != null && _files.Length > 0;
}
