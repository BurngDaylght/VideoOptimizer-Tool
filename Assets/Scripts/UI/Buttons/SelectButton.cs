using UnityEngine;
using Zenject;

public class SelectButton : BaseButton
{
    private FileSelector _fileSelector;
    private FileProcessor _fileProcessor;

    [Inject]
    private void Construct(FileSelector fileSelector, FileProcessor fileProcessor)
    {
        _fileSelector = fileSelector;
        _fileProcessor = fileProcessor;
    }
    
    private void OnEnable()
    {
        OnClickAnimationComplete += SelectFiles;

        _fileProcessor.OnOptimizeStart += DisableButton;
        _fileProcessor.OnOptimizeEnd += EnableButton;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= SelectFiles;
        
        _fileProcessor.OnOptimizeStart -= DisableButton;
        _fileProcessor.OnOptimizeEnd -= EnableButton;
    }

    private void SelectFiles()
    {
        _fileSelector.SelectFiles();
        Debug.Log("[SelectButton] Select file!");
    }
}
