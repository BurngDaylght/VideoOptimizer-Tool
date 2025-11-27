using UnityEngine;
using Zenject;

public class StopButton : BaseButton
{
    private FileProcessor _fileProcessor;
    
    [Inject]
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }
    private void OnEnable()
    {
        OnClickAnimationComplete += StopOptimizeFiles;

        _fileProcessor.OnOptimizeStart += EnableButton;
        _fileProcessor.OnOptimizeStart += ShowInternal;
        
        _fileProcessor.OnOptimizeEnd += DisableButton;
        _fileProcessor.OnOptimizeEnd += HideInternal;
        
        _fileProcessor.OnOptimizeStop += EnableButton;
        _fileProcessor.OnOptimizeStop += ShowInternal;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= StopOptimizeFiles;
        
        _fileProcessor.OnOptimizeStart -= EnableButton;
        _fileProcessor.OnOptimizeStart -= ShowInternal;
        
        _fileProcessor.OnOptimizeEnd -= DisableButton;
        _fileProcessor.OnOptimizeEnd -= HideInternal;
        
        _fileProcessor.OnOptimizeStop -= EnableButton;
        _fileProcessor.OnOptimizeStop -= ShowInternal;
    }

    private void Start()
    {
        Hide(true);
    }

    private void ShowInternal()
    {
        Show();
    }
    
    private void HideInternal()
    {
        Hide();
    }

    private void StopOptimizeFiles()
    {
        _fileProcessor.StopOptimize();
        Debug.Log("[StopButton] Stop Optimize!");
    }
}
