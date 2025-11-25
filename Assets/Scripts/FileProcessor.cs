using System;
using Zenject;
using UnityEngine;
using SFB;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;

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
    private void SetFilesPaths(string[] files) => _files = files;
    
    
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
            "Select output folder and filename",
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

        string outputFolder = Path.GetDirectoryName(chosenPath);
        string baseName = Path.GetFileNameWithoutExtension(chosenPath);
        string extension = Path.GetExtension(chosenPath);

        foreach (var file in _files)
        {
            string outputFile = Path.Combine(outputFolder, baseName + "_" + Path.GetFileName(file));

            if (!outputFile.EndsWith(extension))
                outputFile += extension;

            OnOptimizeStart?.Invoke();
            await RunFFmpeg(file, outputFile);
        }

        Debug.Log("[FileProcessor] All files optimized!");
    }
    
    private async UniTask RunFFmpeg(string inputFile, string outputPath)
    {
        long originalSize = new FileInfo(inputFile).Length;

        string args = $"-i \"{inputFile}\" -vcodec libx264 -crf {_quality} \"{outputPath}\"";
        Debug.Log("[FFmpeg args] " + args);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = _ffmpeg,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        
        _currentProcess = process;

        var taskCompletionSource = new UniTaskCompletionSource();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log("[FFmpeg] " + e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            string data = e.Data;

            if (data.Contains("Duration:"))
            {
                int start = data.IndexOf("Duration:") + 10;
                int end = data.IndexOf(",", start);
                string durationString = data.Substring(start, end - start).Trim();
                _duration = ParseTimestampToSeconds(durationString);
            }

            if (data.Contains("time="))
            {
                int start = data.IndexOf("time=") + 5;
                int end = data.IndexOf(" ", start);
                if (end < 0) end = data.Length;

                string timeString = data.Substring(start, end - start);
                float currentTime = ParseTimestampToSeconds(timeString);

                float progress = currentTime / _duration;
                _progressBar.SetProgress(progress);
            }
        };

        process.Exited += async (sender, e) =>
        {
            Debug.Log("[FFmpeg] Finished with exit code: " + process.ExitCode);
            taskCompletionSource.TrySetResult();

            _progressBar.SetProgress(1f);
            await UniTask.Delay(500);

            long compressedSize = new FileInfo(outputPath).Length;

            string orig = FormatBytes(originalSize);
            string comp = FormatBytes(compressedSize);

            float reduction = 100f - (compressedSize / (float)originalSize * 100f);
            string reductionText = reduction > 0  ? $"(-{reduction:F0}%)" : "(no reduction)";
            
            _notificationService.ShowNotification(NotificationType.CompressionSuccess,$"{orig}", $"{comp} {reductionText}");

            OnOptimizeEnd?.Invoke();
            _files = null;
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await taskCompletionSource.Task;
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
    
    public void SetQuality(int quality)
    {
        _quality = Mathf.Clamp(quality, 0, 51);
    }
}
