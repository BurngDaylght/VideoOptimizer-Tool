using UnityEngine;
using Zenject;

public class OptimizeButton : BaseButton
{
    private FileProcessor _fileProcessor;
    
    [Inject]
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }
    private void OnEnable() => OnClickAnimationComplete += OptimizeFiles;
    private void OnDisable() => OnClickAnimationComplete -= OptimizeFiles;

    private void OptimizeFiles()
    {
        _fileProcessor.OptimizeFiles();
        Debug.Log("[OptimizeButton] Optimize file!");
    }
}
