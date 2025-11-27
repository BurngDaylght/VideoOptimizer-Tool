using UnityEngine;
using Zenject;

public class MinimizeButton : BaseButton
{
    private WindowScript _windowScript;

    [Inject]
    private void Construct(WindowScript windowScript)
    {
        _windowScript = windowScript;
    }
    
    private void OnEnable() => OnClick += MinimizeProgram;
    private void OnDisable() => OnClick -= MinimizeProgram;

    private void MinimizeProgram()
    {
        _windowScript.OnMinimizeBtnClick();
        Debug.Log("[MinimizeButton] Program is minimized!");
    }
}
