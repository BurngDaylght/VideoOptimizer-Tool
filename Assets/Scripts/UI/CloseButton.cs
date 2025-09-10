using UnityEngine;

public class CloseButton : BaseButton
{
    private void OnEnable() => OnClick += CloseProgram;
    private void OnDisable() => OnClick -= CloseProgram;

    private void CloseProgram()
    {
        Application.Quit();
        Debug.Log("[Close Button] Program is closed!");
    }
}
