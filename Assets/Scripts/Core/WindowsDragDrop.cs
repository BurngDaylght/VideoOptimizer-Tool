using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Zenject;
using UnityEngine;

public class WindowsDragDrop : IInitializable, IDisposable, ITickable
{
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

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWL_WNDPROC  = -4;
    private const uint WM_DROPFILES = 0x0233;
    private const uint MSGFLT_ALLOW = 1;

    public event Action<string[]> OnFilesDropped;

    private WndProcDelegate _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private IntPtr _hwnd = IntPtr.Zero;
    private readonly Queue<string[]> _pendingFiles = new();

    public void Initialize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        _hwnd = FindUnityWindow();

        if (_hwnd == IntPtr.Zero)
        {
            Debug.LogError("[WindowsDragDrop] Window not found!");
            return;
        }

        ChangeWindowMessageFilterEx(_hwnd, WM_DROPFILES, MSGFLT_ALLOW, IntPtr.Zero);
        DragAcceptFiles(_hwnd, true);

        _newWndProc = WndProc;
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
#endif
    }

    public void Dispose()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_hwnd == IntPtr.Zero || _oldWndProc == IntPtr.Zero) return;
        SetWindowLongPtr(_hwnd, GWL_WNDPROC, _oldWndProc);
        DragAcceptFiles(_hwnd, false);
#endif
    }

    public void Tick()
    {
        string[][] batch;

        lock (_pendingFiles)
        {
            if (_pendingFiles.Count == 0) return;
            batch = _pendingFiles.ToArray();
            _pendingFiles.Clear();
        }

        foreach (var files in batch)
            OnFilesDropped?.Invoke(files);
    }

    private IntPtr FindUnityWindow()
    {
        IntPtr found = IntPtr.Zero;
        uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != currentPid) return true;
            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_DROPFILES)
        {
            HandleDrop(wParam);
            return IntPtr.Zero;
        }
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void HandleDrop(IntPtr hDrop)
    {
        uint count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
        var files = new string[count];

        for (uint i = 0; i < count; i++)
        {
            var sb = new StringBuilder(260);
            DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
            files[i] = sb.ToString();
        }

        DragFinish(hDrop);

        lock (_pendingFiles)
            _pendingFiles.Enqueue(files);
    }
}