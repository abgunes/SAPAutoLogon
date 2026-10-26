using System;

namespace SAPAutoLogon
{
    public enum RegistryRoot
    {
        CurrentUser,
        LocalMachine,

        /// <summary>HKLM\SOFTWARE\Wow6432Node</summary>
        LocalMachine32
    }

    /// <summary>Everything the locator needs from the machine. Implemented over the registry and file system at runtime.</summary>
    public interface ILandscapeEnvironment
    {
        /// <summary>Returns "" when the key or value does not exist.</summary>
        string ReadRegistry(RegistryRoot root, string subKey, string valueName);

        /// <summary>Returns "" when the variable is not set.</summary>
        string GetEnvironmentVariable(string name);

        bool FileExists(string path);

        /// <summary>Expands %VAR% references.</summary>
        string ExpandVariables(string text);
    }

    /// <summary>
    /// Finds SAP Logon's landscape files in the same order as SAP ADT for VS Code.
    /// Read only. Never throws.
    /// </summary>
    public static class LandscapeLocator
    {
        private const string LastUsedKey = @"Software\SAP\SAPLogon\LandscapeFilesLastUsed";
        private const string OptionsKey = @"Software\SAP\SAPLogon\Options";
        private const string LocalFileName = "SAPUILandscape.xml";
        private const string GlobalFileName = "SAPUILandscapeGlobal.xml";

        private static readonly RegistryRoot[] OptionRoots = new RegistryRoot[]
        {
            RegistryRoot.CurrentUser,
            RegistryRoot.LocalMachine,
            RegistryRoot.LocalMachine32
        };

        public static LandscapeFiles Locate(ILandscapeEnvironment env)
        {
            LandscapeFiles none = new LandscapeFiles();
            if (env == null) return none;

            LandscapeFiles found;

            found = Attempt(delegate { return FromLastUsed(env); });
            if (found != null) return found;

            found = Attempt(delegate { return FromEnvironmentVariable(env); });
            if (found != null) return found;

            found = Attempt(delegate { return FromOptions(env); });
            if (found != null) return found;

            found = Attempt(delegate { return FromAppData(env); });
            if (found != null) return found;

            return none;
        }

        private static LandscapeFiles Attempt(Func<LandscapeFiles> step)
        {
            try
            {
                return step();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // 1. HKCU\...\LandscapeFilesLastUsed
        private static LandscapeFiles FromLastUsed(ILandscapeEnvironment env)
        {
            string local = Read(env, RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFile");
            if (local.Length == 0 || !env.FileExists(local)) return null;

            string global = Read(env, RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFileOnServer");
            if (global.Length == 0) global = Read(env, RegistryRoot.CurrentUser, LastUsedKey, "CoreLandscapeFileOnServer");
            if (global.Length == 0) global = Read(env, RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFileGlobal");

            return Make(local, global);
        }

        // 2. SAPLOGON_LSXML_FILE
        private static LandscapeFiles FromEnvironmentVariable(ILandscapeEnvironment env)
        {
            string local = Expand(env, env.GetEnvironmentVariable("SAPLOGON_LSXML_FILE"));
            if (local.Length == 0 || !env.FileExists(local)) return null;

            return Make(local, SiblingGlobal(env, local));
        }

        // SAPUILandscapeGlobal.xml next to the local file, or "" when it does not exist.
        private static string SiblingGlobal(ILandscapeEnvironment env, string local)
        {
            int cut = Math.Max(local.LastIndexOf('\\'), local.LastIndexOf('/'));
            if (cut < 0) return "";
            string sibling = local.Substring(0, cut + 1) + GlobalFileName;
            return env.FileExists(sibling) ? sibling : "";
        }

        // 3. SAPLogon\Options (HKCU, then HKLM, then HKLM\Wow6432Node, per value)
        private static LandscapeFiles FromOptions(ILandscapeEnvironment env)
        {
            string dir = ReadOption(env, "PathConfigFilesLocal");
            if (dir.Length == 0) return null;

            string local = dir.EndsWith("\\") || dir.EndsWith("/") ? dir + LocalFileName : dir + "\\" + LocalFileName;
            if (!env.FileExists(local)) return null;

            // Sequential overrides: sibling file, then Core..., then LandscapeFileOnServer (last one wins).
            string global = SiblingGlobal(env, local);
            string core = ReadOption(env, "CoreLandscapeFileOnServer");
            if (core.Length > 0) global = core;
            string server = ReadOption(env, "LandscapeFileOnServer");
            if (server.Length > 0) global = server;

            return Make(local, global);
        }

        // 4. %APPDATA%\SAP\Common\SAPUILandscape.xml
        private static LandscapeFiles FromAppData(ILandscapeEnvironment env)
        {
            string local = Expand(env, @"%APPDATA%\SAP\Common\" + LocalFileName);
            if (local.Length == 0 || !env.FileExists(local)) return null;
            return Make(local, "");
        }

        private static string ReadOption(ILandscapeEnvironment env, string valueName)
        {
            foreach (RegistryRoot root in OptionRoots)
            {
                string value = Read(env, root, OptionsKey, valueName);
                if (value.Length > 0) return value;
            }
            return "";
        }

        // A failing lookup counts as "not present" so later roots/values still get their turn.
        private static string Read(ILandscapeEnvironment env, RegistryRoot root, string subKey, string valueName)
        {
            try
            {
                return Expand(env, env.ReadRegistry(root, subKey, valueName));
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string Expand(ILandscapeEnvironment env, string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string expanded = env.ExpandVariables(text);
            return expanded == null ? "" : expanded.Trim();
        }

        private static LandscapeFiles Make(string local, string global)
        {
            LandscapeFiles files = new LandscapeFiles();
            files.LocalPath = local;
            files.GlobalPath = global;
            return files;
        }
    }
}
