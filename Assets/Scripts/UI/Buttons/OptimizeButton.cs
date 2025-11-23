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
        _fileProcessor.OnOptimizeStart += HideInternal;
        
        _fileProcessor.OnOptimizeEnd += EnableButton;
        _fileProcessor.OnOptimizeEnd += ShowInternal;
        
        _fileProcessor.OnOptimizeStop += EnableButton;
        _fileProcessor.OnOptimizeStop += ShowInternal;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= OptimizeFiles;
        
        _fileProcessor.OnOptimizeStart -= DisableButton;
        _fileProcessor.OnOptimizeStart -= HideInternal;
        
        _fileProcessor.OnOptimizeEnd -= EnableButton;
        _fileProcessor.OnOptimizeEnd -= ShowInternal;
        
        _fileProcessor.OnOptimizeStop -= EnableButton;
        _fileProcessor.OnOptimizeStop -= ShowInternal;
    }
    
    private void ShowInternal()
    {
        Show();
    }

    private void HideInternal()
    {
        Hide();
    }

    private void OptimizeFiles()
    {
        _fileProcessor.OptimizeFiles();
        Debug.Log("[OptimizeButton] Optimize file!");
    }
}
