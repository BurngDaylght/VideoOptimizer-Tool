using System;
using Zenject;
using UnityEngine;
using SFB;
using System.IO;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

public class FileProcessor : IInitializable, IDisposable
{
    #region WinAPI

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX, dwY, dwXSize, dwYSize;
        public int dwXCountChars, dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CreateProcess(
        string lpApplicationName, StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
        bool bInheritHandles, int dwCreationFlags,
        IntPtr lpEnvironment, string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    private const int STARTF_USESTDHANDLES = 0x00000100;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const int SW_HIDE = 0;
    private const int CREATE_NO_WINDOW = 0x08000000;
    private const uint INFINITE = 0xFFFFFFFF;
    private const uint STILL_ACTIVE = 259;

    #endregion

    public event Action OnOptimizeStart;
    public event Action OnOptimizeEnd;
    public event Action OnOptimizeStop;

    private int _quality = 23;
    private string[] _files;
    private string _ffmpeg = Path.Combine(Application.streamingAssetsPath, "FFmpeg/bin/ffmpeg.exe");
    private float _duration;
    private IntPtr _currentProcessHandle = IntPtr.Zero;
    private string _currentOutputFile;
    private bool _isCancelled = false;

    private readonly FileSelector _fileSelector;
    private readonly ProgressBar _progressBar;
    private readonly NotificationService _notificationService;
    private readonly FileExtensionsConfig _formats;

    public FileProcessor(
        FileSelector fileSelector,
        ProgressBar progressBar,
        NotificationService notificationService,
        FileExtensionsConfig formats)
    {
        _fileSelector = fileSelector;
        _progressBar = progressBar;
        _notificationService = notificationService;
        _formats = formats;
    }

    public void Initialize()
    {
        _fileSelector.OnFilesSelected += SetFilesPaths;
        Application.quitting += OnApplicationQuitting;
    }

    public void Dispose()
    {
        Application.quitting -= OnApplicationQuitting;
        _fileSelector.OnFilesSelected -= SetFilesPaths;
        _isCancelled = true;
        KillCurrentProcess();

        if (!string.IsNullOrEmpty(_currentOutputFile) && File.Exists(_currentOutputFile))
        {
            System.Threading.Thread.Sleep(200);
            try { File.Delete(_currentOutputFile); }
            catch { }
        }
    }

    public void StopOptimize()
    {
        _isCancelled = true;
        KillCurrentProcess();
        DeleteOutputFileAsync().Forget();
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
            _formats.OutputFormats,
            chosenPath =>
            {
                if (!string.IsNullOrEmpty(chosenPath))
                    HandleFileSave(chosenPath).Forget();
            });
    }

    private async UniTaskVoid HandleFileSave(string chosenPath)
    {
        string extension = Path.GetExtension(chosenPath);

        foreach (var file in _files)
        {
            string outputFile = chosenPath;
            if (!outputFile.EndsWith(extension))
                outputFile += extension;

            _currentOutputFile = outputFile;
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
        _isCancelled = false;
        _duration = 0f;

        string codec = GetBestCodec(Path.GetExtension(outputPath).ToLowerInvariant());
        string args = $"\"{_ffmpeg}\" -y -nostdin -hide_banner -i \"{inputFile}\" {codec} \"{outputPath}\"";
        Debug.Log("[FFmpeg args] " + args);

        var sa = new SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true };

        if (!CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref sa, 0))
        {
            Debug.LogError("[FileProcessor] Failed to create pipe");
            return;
        }

        var si = new STARTUPINFO
        {
            cb = Marshal.SizeOf<STARTUPINFO>(),
            dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW,
            wShowWindow = SW_HIDE,
            hStdOutput = hWritePipe,
            hStdError = hWritePipe,
            hStdInput = IntPtr.Zero
        };

        var cmdLine = new StringBuilder(args);

        if (!CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, true,
            CREATE_NO_WINDOW, IntPtr.Zero, null, ref si, out PROCESS_INFORMATION pi))
        {
            Debug.LogError($"[FileProcessor] CreateProcess failed: {Marshal.GetLastWin32Error()}");
            CloseHandle(hReadPipe);
            CloseHandle(hWritePipe);
            return;
        }

        _currentProcessHandle = pi.hProcess;
        CloseHandle(hWritePipe);

        var timeRegex = new Regex(@"time=(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);
        var durationRegex = new Regex(@"Duration:\s(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        var tcs = new UniTaskCompletionSource();

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (true)
            {
                bool success = ReadFile(hReadPipe, buffer, buffer.Length, out int bytesRead, IntPtr.Zero);
                if (!success || bytesRead == 0) break;

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                sb.Append(chunk);

                string text = sb.ToString();
                int separator;
                while ((separator = text.IndexOfAny(new[] { '\n', '\r' })) >= 0)
                {
                    string line = text.Substring(0, separator).Trim();
                    text = text.Substring(separator + 1);

                    if (string.IsNullOrEmpty(line)) continue;

                    var durationMatch = durationRegex.Match(line);
                    if (durationMatch.Success && TryParseTimestampToSeconds(durationMatch.Groups[1].Value, out float d))
                        _duration = d;

                    if (_duration > 0)
                    {
                        var timeMatch = timeRegex.Match(line);
                        if (timeMatch.Success && TryParseTimestampToSeconds(timeMatch.Groups[1].Value, out float current))
                        {
                            float progress = Mathf.Clamp01(current / _duration);
                            UniTask.Post(() => _progressBar?.SetProgress(progress));
                        }
                    }
                }
                sb.Clear();
                sb.Append(text);
            }

            CloseHandle(hReadPipe);

            GetExitCodeProcess(pi.hProcess, out uint exitCode);
            Debug.Log($"[FFmpeg] Finished with exit code: {exitCode}");

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            _currentProcessHandle = IntPtr.Zero;

            tcs.TrySetResult();
        });

        await tcs.Task;
        await UniTask.SwitchToMainThread();

        if (_isCancelled)
        {
            _isCancelled = false;
            return;
        }

        _progressBar.SetProgress(1f);
        await UniTask.Delay(200);

        if (File.Exists(outputPath))
        {
            long compressedSize = new FileInfo(outputPath).Length;

            if (compressedSize >= originalSize)
            {
                File.Copy(inputFile, outputPath, overwrite: true);
                compressedSize = originalSize;
            }

            string orig = FormatBytes(originalSize);
            string comp = FormatBytes(compressedSize);
            float reduction = 100f - (compressedSize / (float)originalSize * 100f);
            string reductionText = reduction > 0 ? $"(-{reduction:F0}%)" : "(no reduction)";

            _notificationService.ShowNotification(
                NotificationType.CompressionSuccess,
                orig, $"{comp} {reductionText}"
            );
        }

        OnOptimizeEnd?.Invoke();
        _files = null;
        _currentOutputFile = null;
    }

    private void KillCurrentProcess()
    {
        if (_currentProcessHandle == IntPtr.Zero) return;
        TerminateProcess(_currentProcessHandle, 1);
        CloseHandle(_currentProcessHandle);
        _currentProcessHandle = IntPtr.Zero;
    }

    private string GetBestCodec(string outputExt)
    {
        return outputExt switch
        {
            ".webm" => $"-vcodec libvp9 -crf {_quality} -b:v 0",
            _ => $"-vcodec libx264 -crf {_quality} -preset slow"
        };
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
        catch { }
        return false;
    }

    private string FormatBytes(long bytes)
    {
        float mb = bytes / 1048576f;
        return mb > 1024 ? $"{mb / 1024f:F2} GB" : $"{mb:F2} MB";
    }

    private async UniTask WaitForFileRelease(string path)
    {
        if (!File.Exists(path)) return;
        while (true)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    return;
            }
            catch
            {
                await UniTask.Delay(50);
            }
        }
    }
    
    private async UniTaskVoid DeleteOutputFileAsync()
    {
        if (string.IsNullOrEmpty(_currentOutputFile)) return;
    
        await WaitForFileRelease(_currentOutputFile);
    
        if (File.Exists(_currentOutputFile))
            File.Delete(_currentOutputFile);
    
        _currentOutputFile = null;
    }

    private void SetFilesPaths(string[] files) => _files = files;
    public void SetQuality(int quality) => _quality = Mathf.Clamp(quality, 0, 51);
    public bool IsFilesSelected() => _files != null && _files.Length > 0;
    
    private void OnApplicationQuitting()
    {
        _isCancelled = true;
        KillCurrentProcess();
    }
}