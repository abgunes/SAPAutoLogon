using System;
using System.Globalization;
using System.Text;

namespace SAPAutoLogon
{
    /// <summary>
    /// Plugin settings: the SAP GUI and Business Client folders, the maximize flag,
    /// whether the entry-list columns were already set up, and the import options
    /// (whether created entries get a user name, that user name, and the remembered
    /// size and splitter position of the import popup).
    /// Stored in the KeePass configuration as one "name=value;name=value" string.
    /// </summary>
    public sealed class PluginParameters
    {
        public bool isMaxGui = true;
        public string sapGuiPath = string.Empty;
        public string nBCPath = string.Empty;

        // True once the plugin has added its columns to the entry list (done only once,
        // so columns the user removes later stay removed)
        public bool columnsSetUp = false;

        // Import options. The layout values are 0 until the popup remembers a size.
        public bool importSetUserName = false;
        public string importUserName = "";
        public int importDialogWidth = 0;
        public int importDialogHeight = 0;
        public int importSplitter = 0;

        public const string SAPGUIShortCutEXE = "sapshcut.exe";
        public const string SAPNWBCShortCutEXE = "NWBC.exe";

        // Menu text
        public const string menuItemText = "SAPAutoLogon options...";

        public override string ToString()
        {
            return "isMaxGui=" + (isMaxGui ? "True" : "False") + ";" +
                "sapGuiPath=" + sapGuiPath + ";" +
                "nBCPath=" + nBCPath + ";" +
                "columnsSetUp=" + (columnsSetUp ? "True" : "False") + ";" +
                "importSetUserName=" + (importSetUserName ? "True" : "False") + ";" +
                "importUserName=" + EscapeValue(importUserName) + ";" +
                "importDialogWidth=" + importDialogWidth.ToString(CultureInfo.InvariantCulture) + ";" +
                "importDialogHeight=" + importDialogHeight.ToString(CultureInfo.InvariantCulture) + ";" +
                "importSplitter=" + importSplitter.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Escapes the characters that would break the "name=value;name=value" format:
        /// '%', ';', '=' and line breaks become %25, %3B, %3D, %0D and %0A.
        /// Null gives an empty string.
        /// </summary>
        public static string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) { return string.Empty; }

            return value.Replace("%", "%25")
                .Replace(";", "%3B")
                .Replace("=", "%3D")
                .Replace("\r", "%0D")
                .Replace("\n", "%0A");
        }

        /// <summary>
        /// Reverses EscapeValue (hex digits in either case). Any other '%' is kept as is.
        /// Null gives an empty string.
        /// </summary>
        public static string UnescapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) { return string.Empty; }

            StringBuilder sb = new StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c == '%' && i + 2 < value.Length)
                {
                    string code = value.Substring(i + 1, 2).ToUpperInvariant();
                    string decoded = null;
                    switch (code)
                    {
                        case "25": decoded = "%"; break;
                        case "3B": decoded = ";"; break;
                        case "3D": decoded = "="; break;
                        case "0D": decoded = "\r"; break;
                        case "0A": decoded = "\n"; break;
                    }
                    if (decoded != null)
                    {
                        sb.Append(decoded);
                        i += 3;
                        continue;
                    }
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static int ParseLayoutValue(string value)
        {
            int n;
            if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n > 0)
            {
                return n;
            }
            return 0;
        }

        /// <summary>
        /// Reads the string produced by ToString. Each part is split on the first '='
        /// (paths may contain ':'); unknown or malformed parts are ignored.
        /// Empty or null text gives the defaults.
        /// </summary>
        public static PluginParameters Parse(string text)
        {
            PluginParameters result = new PluginParameters();

            if (string.IsNullOrEmpty(text)) { return result; }

            foreach (string part in text.Split(';'))
            {
                int pos = part.IndexOf('=');
                if (pos <= 0) { continue; }

                string name = part.Substring(0, pos).Trim();
                string value = part.Substring(pos + 1);

                switch (name)
                {
                    case "isMaxGui":
                        result.isMaxGui = value.Trim() != "False";
                        break;
                    case "sapGuiPath":
                        result.sapGuiPath = value;
                        break;
                    case "nBCPath":
                        result.nBCPath = value;
                        break;
                    case "columnsSetUp":
                        result.columnsSetUp = value.Trim() == "True";
                        break;
                    case "importSetUserName":
                        result.importSetUserName = value.Trim() == "True";
                        break;
                    case "importUserName":
                        result.importUserName = UnescapeValue(value);
                        break;
                    case "importDialogWidth":
                        result.importDialogWidth = ParseLayoutValue(value);
                        break;
                    case "importDialogHeight":
                        result.importDialogHeight = ParseLayoutValue(value);
                        break;
                    case "importSplitter":
                        result.importSplitter = ParseLayoutValue(value);
                        break;
                }
            }

            return result;
        }
    }
}
