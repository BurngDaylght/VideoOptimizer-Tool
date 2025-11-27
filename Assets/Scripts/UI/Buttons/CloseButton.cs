using UnityEngine;
using Zenject;

public class CloseButton : BaseButton
{
    private FileProcessor _fileProcessor;
    
    [Inject]
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }
    
    private void OnEnable() => OnClick += CloseProgram;
    private void OnDisable() => OnClick -= CloseProgram;

    private void CloseProgram()
    {
        Debug.Log("[CloseButton] Program is closed!");
        _fileProcessor.StopOptimize();
        Application.Quit();
    }
}
