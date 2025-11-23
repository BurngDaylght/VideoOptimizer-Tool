using TMPro;
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
        
        _fileSelector.OnFilesSelected += HideText;

        _fileProcessor.OnOptimizeStart += DisableButton;
        
        _fileProcessor.OnOptimizeEnd += EnableButton;
        _fileProcessor.OnOptimizeEnd += ShowText;
        
        _fileProcessor.OnOptimizeStop += EnableButton;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= SelectFiles;
        
        _fileSelector.OnFilesSelected -= HideText;
        
        _fileProcessor.OnOptimizeStart -= DisableButton;
        
        _fileProcessor.OnOptimizeEnd -= EnableButton;
        _fileProcessor.OnOptimizeEnd -= ShowText;
        
        _fileProcessor.OnOptimizeStop -= EnableButton;
    }

    private void Start()
    {
        ShowText();
    }

    private void SelectFiles()
    {
        _fileSelector.SelectFiles();
        Debug.Log("[SelectButton] Select file!");
    }

    private void HideText(string[] _)
    {
        _textMeshProUGUI.gameObject.SetActive(false);
    }

    private void ShowText()
    {
        _textMeshProUGUI.gameObject.SetActive(true);
    }
}
