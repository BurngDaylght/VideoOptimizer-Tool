using UnityEngine;
using System;
using SFB;

public class FileSelector
{
    public event Action<string[]> OnFilesSelected;
    
    private string _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    
    private readonly FileExtensionsConfig _formats;
    
    public FileSelector(FileExtensionsConfig formats)
    {
        _formats = formats;
    }

    public void SelectFiles()
    {
        Debug.Log("[FileSelector] Start selecting files!");
        
        StandaloneFileBrowser.OpenFilePanelAsync("Choose a File", _desktopPath, _formats.InputFormats, false, paths =>
        {
            if (paths == null || paths.Length == 0)
                return;
            
            foreach (var path in paths)
                Debug.Log("[FileSelector] " + path);

            OnFilesSelected?.Invoke(paths);
            
            Debug.Log("[FileSelector] End selecting files!");
        });
    }
}
