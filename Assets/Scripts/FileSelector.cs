using UnityEngine;
using System;
using SFB;

public class FileSelector
{
    public event Action<string[]> OnFilesSelected;
    
    private string _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    
    private ExtensionFilter[] _filesExtensions = new [] {
        new ExtensionFilter("All Supported Video Files ", "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "m4v", "mpg", "mpeg", "3gp", "ts", "mts", "m2ts"),
        new ExtensionFilter("MP4 ", "mp4"),
        new ExtensionFilter("MKV ", "mkv"),
        new ExtensionFilter("AVI ", "avi"),
        new ExtensionFilter("MOV ", "mov"),
        new ExtensionFilter("WMV ", "wmv"),
        new ExtensionFilter("WebM ", "webm"),
        new ExtensionFilter("MPEG ", "mpg", "mpeg"),
        new ExtensionFilter("3GP ", "3gp"),
        new ExtensionFilter("Transport Stream ", "ts", "mts", "m2ts")
    };

    public void SelectFiles()
    {
        Debug.Log("[FileSelector] Start selecting files!");
        
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", _desktopPath, _filesExtensions, false, (string[] paths) =>
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
