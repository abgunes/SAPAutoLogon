using System;
using System.IO;
using Microsoft.Win32;

namespace SAPAutoLogon
{
    /// <summary>
    /// <see cref="ILandscapeEnvironment"/> over the Windows registry, environment
    /// and file system. Read only. Never throws.
    /// </summary>
    public sealed class WindowsLandscapeEnvironment : ILandscapeEnvironment
    {
        private const string SoftwarePrefix = @"Software\";
        private const string Wow6432Node = @"Wow6432Node\";

        public string ReadRegistry(RegistryRoot root, string subKey, string valueName)
        {
            try
            {
                if (string.IsNullOrEmpty(subKey)) return "";

                RegistryKey baseKey;
                string path = subKey;
                switch (root)
                {
                    case RegistryRoot.CurrentUser:
                        baseKey = Registry.CurrentUser;
                        break;
                    case RegistryRoot.LocalMachine:
                        baseKey = Registry.LocalMachine;
                        break;
                    case RegistryRoot.LocalMachine32:
                        baseKey = Registry.LocalMachine;
                        path = To32BitPath(subKey);
                        break;
                    default:
                        return "";
                }

                using (RegistryKey key = baseKey.OpenSubKey(path, false))
                {
                    if (key == null) return "";
                    // REG_EXPAND_SZ is expanded by GetValue's default behaviour.
                    string value = key.GetValue(valueName) as string;
                    return value ?? "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        // Software\X -> Software\Wow6432Node\X
        private static string To32BitPath(string subKey)
        {
            if (subKey.StartsWith(SoftwarePrefix, StringComparison.OrdinalIgnoreCase))
                return subKey.Substring(0, SoftwarePrefix.Length) + Wow6432Node + subKey.Substring(SoftwarePrefix.Length);
            return SoftwarePrefix + Wow6432Node + subKey;
        }

        public string GetEnvironmentVariable(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return "";
                return Environment.GetEnvironmentVariable(name) ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public bool FileExists(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string ExpandVariables(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return "";
                return Environment.ExpandEnvironmentVariables(text);
            }
            catch (Exception)
            {
                return text ?? "";
            }
        }
    }
}
