using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Zenject;
using UnityEngine;

public class WindowsDragDrop : IInitializable, IDisposable, ITickable
{
    #region WinAPI

    [DllImport("shell32.dll")]
    private static extern void DragAcceptFiles(IntPtr hwnd, bool accept);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

    [DllImport("shell32.dll")]
    private static extern void DragFinish(IntPtr hDrop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hwnd, uint message, uint action, IntPtr changeInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    #endregion

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int  GWL_WNDPROC  = -4;
    private const uint WM_DROPFILES = 0x0233;
    private const uint MSGFLT_ALLOW = 1;
    private const int  VK_LBUTTON   = 0x01;

    public event Action<string[]> OnFilesDropped;
    public event Action OnDragEnter;
    public event Action OnDragLeave;

    private static IntPtr _oldWndProcStatic = IntPtr.Zero;
    private static readonly Queue<string[]> _pendingFilesStatic = new();
    private static bool _isDragHighlightingStatic = false;
    private static bool _lmbWasPressedOutsideStatic = false;
    private static bool _isProcessingStatic = false;
    private static IntPtr _hwndStatic = IntPtr.Zero;
    private static uint _targetPid;
    private static IntPtr _foundHwnd;

    private static WndProcDelegate _wndProcDelegate;

    private FileProcessor _fileProcessor;

    [Inject]
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }

    public void Initialize()
    {
        _fileProcessor.OnOptimizeStart += OnOptimizeStart;
        _fileProcessor.OnOptimizeEnd += OnOptimizeEnd;
        _fileProcessor.OnOptimizeStop += OnOptimizeEnd;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        _hwndStatic = FindUnityWindow();

        if (_hwndStatic == IntPtr.Zero)
        {
            Debug.LogError("[WindowsDragDrop] Window not found!");
            return;
        }

        ChangeWindowMessageFilterEx(_hwndStatic, WM_DROPFILES, MSGFLT_ALLOW, IntPtr.Zero);
        DragAcceptFiles(_hwndStatic, true);

        _wndProcDelegate = WndProcStatic;
        _oldWndProcStatic = SetWindowLongPtr(_hwndStatic, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
#endif
    }

    public void Dispose()
    {
        _fileProcessor.OnOptimizeStart -= OnOptimizeStart;
        _fileProcessor.OnOptimizeEnd -= OnOptimizeEnd;
        _fileProcessor.OnOptimizeStop -= OnOptimizeEnd;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_hwndStatic == IntPtr.Zero || _oldWndProcStatic == IntPtr.Zero) return;
        SetWindowLongPtr(_hwndStatic, GWL_WNDPROC, _oldWndProcStatic);
        DragAcceptFiles(_hwndStatic, false);
#endif
    }

    public void Tick()
    {
        string[][] batch = null;

        lock (_pendingFilesStatic)
        {
            if (_pendingFilesStatic.Count > 0)
            {
                batch = _pendingFilesStatic.ToArray();
                _pendingFilesStatic.Clear();
            }
        }

        if (batch != null)
        {
            if (_isDragHighlightingStatic)
            {
                _isDragHighlightingStatic = false;
                OnDragLeave?.Invoke();
            }

            foreach (var files in batch)
                OnFilesDropped?.Invoke(files);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        CheckDragHighlight();
#endif
    }

    private void CheckDragHighlight()
    {
        if (_isProcessingStatic) return;

        bool isLMBDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

        GetCursorPos(out POINT cursor);
        GetWindowRect(_hwndStatic, out RECT rect);

        bool isOverContent = cursor.X >= rect.Left && cursor.X <= rect.Right &&
                             cursor.Y >= rect.Top + 30 && cursor.Y <= rect.Bottom;

        if (!isLMBDown)
        {
            _lmbWasPressedOutsideStatic = false;
            if (!_isDragHighlightingStatic) return;
            _isDragHighlightingStatic = false;
            OnDragLeave?.Invoke();
            return;
        }

        if (!_isDragHighlightingStatic && !_lmbWasPressedOutsideStatic)
            _lmbWasPressedOutsideStatic = !isOverContent;

        if (isOverContent && !_isDragHighlightingStatic && _lmbWasPressedOutsideStatic)
        {
            _isDragHighlightingStatic = true;
            OnDragEnter?.Invoke();
        }
        else if (!isOverContent && _isDragHighlightingStatic)
        {
            _isDragHighlightingStatic = false;
            OnDragLeave?.Invoke();
        }
    }

    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid != _targetPid) return true;
        _foundHwnd = hWnd;
        return false;
    }

    private static IntPtr FindUnityWindow()
    {
        _foundHwnd = IntPtr.Zero;
        _targetPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        EnumWindows(EnumWindowsCallback, IntPtr.Zero);
        return _foundHwnd;
    }

    [MonoPInvokeCallback(typeof(WndProcDelegate))]
    private static IntPtr WndProcStatic(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_DROPFILES)
        {
            HandleDropStatic(wParam);
            return IntPtr.Zero;
        }
        return CallWindowProc(_oldWndProcStatic, hWnd, msg, wParam, lParam);
    }

    private static void HandleDropStatic(IntPtr hDrop)
    {
        if (_isProcessingStatic) return;

        uint count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
        var files = new string[count];

        for (uint i = 0; i < count; i++)
        {
            var sb = new StringBuilder(260);
            DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
            files[i] = sb.ToString();
        }

        DragFinish(hDrop);

        lock (_pendingFilesStatic)
            _pendingFilesStatic.Enqueue(files);
    }

    private void OnOptimizeStart() => _isProcessingStatic = true;
    private void OnOptimizeEnd() => _isProcessingStatic = false;
}