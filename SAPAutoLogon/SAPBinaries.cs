using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace SAPAutoLogon
{
    public class SAPBinaries
    {
        private static readonly string[] AppPathsKeys =
        {
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\",
            "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\App Paths\\"
        };

        /// <summary>
        /// Looks up the folder of the given program in the Windows "App Paths" registry keys.
        /// Returns the folder with a trailing backslash only when the program exists in it,
        /// otherwise "". Never throws.
        /// </summary>
        public string DetectSAPGUIPath(string exec)
        {
            if (string.IsNullOrEmpty(exec)) { return ""; }

            foreach (string key in AppPathsKeys)
            {
                string folder = FolderFromAppPaths(key + exec);
                if (folder.Length > 0 && File.Exists(ExePath.Combine(folder, exec)))
                {
                    return folder.TrimEnd('\\') + "\\";
                }
            }
            return "";
        }

        // First entry of the "Path" value, "" when the key or value is missing.
        private static string FolderFromAppPaths(string subKeyName)
        {
            try
            {
                using (RegistryKey subKey = Registry.LocalMachine.OpenSubKey(subKeyName))
                {
                    if (subKey == null) { return ""; }

                    string path = Convert.ToString(subKey.GetValue("Path"));
                    if (string.IsNullOrEmpty(path)) { return ""; }

                    int cut = path.IndexOf(';');
                    if (cut >= 0) { path = path.Substring(0, cut); }
                    path = path.Trim();
                    return path.Length < 3 ? "" : path;  // at least "C:\"
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Starts the program. Returns false when nothing was started.
        /// Failures to start throw (the caller shows a KeePass warning, not a Windows error dialog).
        /// </summary>
        public bool DoLogon(string exeName, string queryString)
        {
            if (string.IsNullOrEmpty(exeName)) { return false; }

            ProcessStartInfo info = new ProcessStartInfo(Path.GetFullPath(exeName));
            info.Arguments = queryString;
            info.CreateNoWindow = false;
            info.UseShellExecute = true;
            info.ErrorDialog = false;

            using (Process process = Process.Start(info))
            {
                return process != null;
            }
        }
    }
}
