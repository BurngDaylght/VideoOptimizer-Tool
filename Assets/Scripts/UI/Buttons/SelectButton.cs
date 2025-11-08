using UnityEngine;
using Zenject;

public class SelectButton : BaseButton
{
    private FileSelector _fileSelector;

    [Inject]
    private void Construct(FileSelector fileSelector)
    {
        _fileSelector = fileSelector;
    }
    
    private void OnEnable() => OnClickAnimationComplete += SelectFiles;
    private void OnDisable() => OnClickAnimationComplete -= SelectFiles;

    private void SelectFiles()
    {
        _fileSelector.SelectFiles();
        Debug.Log("[SelectButton] Select file!");
    }
}
