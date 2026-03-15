#if UNITY_STANDALONE_WIN

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SFB
{
    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetOpenFileName(ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSaveFileName(ref OPENFILENAME ofn);

        private const int OFN_FILEMUSTEXIST   = 0x00001000;
        private const int OFN_PATHMUSTEXIST   = 0x00000800;
        private const int OFN_ALLOWMULTISELECT = 0x00000200;
        private const int OFN_EXPLORER        = 0x00080000;
        private const int OFN_OVERWRITEPROMPT = 0x00000002;
        private const int MAX_PATH            = 32767;

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = GetActiveWindow();
            ofn.lpstrTitle = title;
            ofn.lpstrFilter = GetFilter(extensions);
            ofn.nFilterIndex = 1;
            ofn.lpstrFile = new string('\0', MAX_PATH);
            ofn.nMaxFile = MAX_PATH;
            ofn.lpstrInitialDir = directory;
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_EXPLORER;

            if (multiselect)
                ofn.Flags |= OFN_ALLOWMULTISELECT;

            if (!GetOpenFileName(ref ofn))
                return new string[0];

            return multiselect
                ? ParseMultiSelect(ofn.lpstrFile)
                : new[] { ofn.lpstrFile.TrimEnd('\0') };
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            var result = OpenFilePanel(title, directory, null, false);
            if (result.Length == 0) return new string[0];
            return new[] { Path.GetDirectoryName(result[0]) };
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = GetActiveWindow();
            ofn.lpstrTitle = title;
            ofn.lpstrFilter = GetFilter(extensions);
            ofn.nFilterIndex = 1;
            ofn.lpstrFile = defaultName != null
                ? defaultName.PadRight(MAX_PATH, '\0')
                : new string('\0', MAX_PATH);
            ofn.nMaxFile = MAX_PATH;
            ofn.lpstrInitialDir = directory;
            ofn.Flags = OFN_OVERWRITEPROMPT | OFN_EXPLORER;

            if (extensions != null && extensions.Length > 0)
                ofn.lpstrDefExt = extensions[0].Extensions[0];

            if (!GetSaveFileName(ref ofn))
                return string.Empty;

            return ofn.lpstrFile.TrimEnd('\0');
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
        {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        private static string GetFilter(ExtensionFilter[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
                return "All Files\0*.*\0\0";

            var sb = new StringBuilder();
            foreach (var ext in extensions)
            {
                sb.Append(ext.Name).Append('\0');
                var patterns = new StringBuilder();
                foreach (var e in ext.Extensions)
                    patterns.Append("*.").Append(e).Append(';');
                if (patterns.Length > 0)
                    patterns.Length--;
                sb.Append(patterns).Append('\0');
            }
            sb.Append('\0');
            return sb.ToString();
        }

        private static string[] ParseMultiSelect(string raw)
        {
            var parts = raw.TrimEnd('\0').Split('\0');
            if (parts.Length == 1)
                return parts;

            var dir = parts[0];
            var result = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
                result[i - 1] = Path.Combine(dir, parts[i]);
            return result;
        }
    }
}

#endif