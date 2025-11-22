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
    private void OnEnable()
    {
        OnClickAnimationComplete += OptimizeFiles;

        _fileProcessor.OnOptimizeStart += DisableButton;
        _fileProcessor.OnOptimizeEnd += EnableButton;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= OptimizeFiles;
        
        _fileProcessor.OnOptimizeStart -= DisableButton;
        _fileProcessor.OnOptimizeEnd -= EnableButton;
    }

    private void OptimizeFiles()
    {
        _fileProcessor.OptimizeFiles();
        Debug.Log("[OptimizeButton] Optimize file!");
    }
}
