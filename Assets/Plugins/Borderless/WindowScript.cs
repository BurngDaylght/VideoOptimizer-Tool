using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class WindowScript : MonoBehaviour, IDragHandler
{
    [Header("Active")]
    [SerializeField] private bool _useBorderless = true;

    [Header("UI Elements Controlled by Borderless Mode")]
    [SerializeField] private List<GameObject> _borderlessOnlyUI = new List<GameObject>();

    [Header("Window Options")]
    public Vector2Int defaultWindowSize;
    public Vector2Int borderSize;

    private Vector2 _deltaValue = Vector2.zero;
    private bool _maximized;

    private void Awake()
    {
        ApplyUIState();

#if !UNITY_EDITOR
        if (_useBorderless)
            OnNoBorderBtnClick();
#endif
    }

    private void Start()
    {
#if !UNITY_EDITOR
        ResetWindowSize();

        if (_useBorderless)
            BorderlessWindow.CenterWindow(defaultWindowSize.x, defaultWindowSize.y);
#endif
    }

    private void OnValidate()
    {
        ApplyUIState();
    }

    private void ApplyUIState()
    {
        if (_borderlessOnlyUI == null) return;

        foreach (var obj in _borderlessOnlyUI)
        {
            if (obj != null)
                obj.SetActive(_useBorderless);
        }
    }

    public void OnBorderBtnClick()
    {
        if (!_useBorderless) return;
        if (BorderlessWindow.framed) return;

        BorderlessWindow.SetFramedWindow();
        BorderlessWindow.MoveWindowPos(Vector2Int.zero, Screen.width + borderSize.x, Screen.height + borderSize.y);
    }

    public void OnNoBorderBtnClick()
    {
        if (!_useBorderless) return;
        if (!BorderlessWindow.framed) return;

        BorderlessWindow.SetFramelessWindow();
        BorderlessWindow.MoveWindowPos(Vector2Int.zero, Screen.width - borderSize.x, Screen.height - borderSize.y);
    }

    public void ResetWindowSize()
    {
        if (!_useBorderless) return;

        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        int x = (screenWidth - defaultWindowSize.x) / 2;
        int y = (screenHeight - defaultWindowSize.y) / 2;

        BorderlessWindow.SetWindowPosAbsolute(x, y, defaultWindowSize.x, defaultWindowSize.y);
    }

    public void OnCloseBtnClick()
    {
        EventSystem.current.SetSelectedGameObject(null);
        Application.Quit();
    }

    public void OnMinimizeBtnClick()
    {
        EventSystem.current.SetSelectedGameObject(null);
        if (!_useBorderless) return;
        BorderlessWindow.MinimizeWindow();
    }

    public void OnMaximizeBtnClick()
    {
        EventSystem.current.SetSelectedGameObject(null);
        if (!_useBorderless) return;

        if (_maximized)
            BorderlessWindow.RestoreWindow();
        else
            BorderlessWindow.MaximizeWindow();

        _maximized = !_maximized;
    }

    public void OnDrag(PointerEventData data)
    {
        if (!_useBorderless) return;
        if (BorderlessWindow.framed) return;

        _deltaValue += data.delta;
        if (data.dragging)
        {
            BorderlessWindow.MoveWindowPos(_deltaValue, Screen.width, Screen.height);
        }
    }
}
