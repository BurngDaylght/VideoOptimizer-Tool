using UnityEngine;
using System;
using System.IO;
using System.Linq;
using SFB;
using Zenject;

public class FileSelector : IInitializable, IDisposable
{
    public event Action<string[]> OnFilesSelected;

    private string _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    private readonly FileExtensionsConfig _formats;
    private readonly WindowsDragDrop _dragDrop;
    
    public FileSelector(FileExtensionsConfig formats, WindowsDragDrop dragDrop)
    {
        _formats  = formats;
        _dragDrop = dragDrop;
    }

    public void Initialize()
    {
        _dragDrop.OnFilesDropped += HandleDroppedFiles;
    }

    public void Dispose()
    {
        _dragDrop.OnFilesDropped -= HandleDroppedFiles;
    }

    public void SelectFiles()
    {
        Debug.Log("[FileSelector] Start selecting files!");

        StandaloneFileBrowser.OpenFilePanelAsync("Choose a File", _desktopPath, _formats.InputFormats, false, paths =>
        {
            if (paths == null || paths.Length == 0) return;

            foreach (var path in paths)
                Debug.Log("[FileSelector] " + path);

            OnFilesSelected?.Invoke(paths);
            Debug.Log("[FileSelector] End selecting files!");
        });
    }

    private void HandleDroppedFiles(string[] paths)
    {
        var valid = paths
            .Where(p => _formats.AllowedExtensions.Contains(
                Path.GetExtension(p).ToLowerInvariant()))
            .ToArray();

        if (valid.Length == 0) return;
        
        OnFilesSelected?.Invoke(valid);
    }
}