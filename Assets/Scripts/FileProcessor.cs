using System;
using Zenject;
using UnityEngine;
using SFB;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class FileProcessor : IInitializable, IDisposable
{
    private string[] _files;
    private string _ffmpeg = Path.Combine(Application.streamingAssetsPath, "FFmpeg/bin/ffmpeg.exe");
    private FileSelector _fileSelector;

    public FileProcessor(FileSelector fileSelector)
    {
        _fileSelector = fileSelector;
    }
    public void Initialize() => _fileSelector.OnFilesSelected += SetFilesPaths;
    public void Dispose() => _fileSelector.OnFilesSelected -= SetFilesPaths;
    private void SetFilesPaths(string[] files) => _files = files;

    public void OptimizeFiles()
    {
        if (_files == null || _files.Length == 0)
        {
            Debug.LogWarning("[FileProcessor] No files selected!");
            return;
        }

        StandaloneFileBrowser.SaveFilePanelAsync(
            "Select output folder and filename",
            Path.GetDirectoryName(_files[0]), 
            "optimized_" + Path.GetFileNameWithoutExtension(_files[0]),
            Path.GetExtension(_files[0]).TrimStart('.'),
            (string chosenPath) =>
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
                    string outputFile = Path.Combine(
                        outputFolder,
                        baseName + "_" + Path.GetFileName(file)
                    );

                    if (!outputFile.EndsWith(extension))
                        outputFile += extension;

                    RunFFmpeg(file, outputFile);
                }
            }
        );
    }

    private void RunFFmpeg(string inputFile, string outputPath)
    {
        string args = $"-i \"{inputFile}\" -vcodec libx264 -crf 23 \"{outputPath}\"";
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

        Process process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log("[FFmpeg] " + e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.LogWarning("[FFmpeg ERROR] " + e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
        Debug.Log("[FFmpeg] Finished with exit code: " + process.ExitCode);
    }
}
