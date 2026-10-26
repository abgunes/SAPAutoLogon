using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// The values a picked SAP Logon system writes into a KeePass entry.
    /// </summary>
    public sealed class MappedEntry
    {
        /// <summary>Every key of SapFields.FromLandscape; empty string means "clear this field".</summary>
        public Dictionary<string, string> Fields = new Dictionary<string, string>();

        /// <summary>User name from SAP Logon, or null to leave the entry's user name alone.</summary>
        public string UserName;

        /// <summary>Entry title (the system's name).</summary>
        public string Title;
    }

    public static class FieldMapper
    {
        public static MappedEntry Map(SapSystem system)
        {
            MappedEntry entry = new MappedEntry();
            Dictionary<string, string> f = entry.Fields;

            f[SapFields.SystemId] = Safe(system.SystemId);
            f[SapFields.ApplicationServer] = Safe(system.ApplicationServer);
            f[SapFields.SystemNumber] = Safe(system.SystemNumber);
            f[SapFields.MessageServer] = Safe(system.MessageServer);
            f[SapFields.MessageServerPort] = Safe(system.MessageServerPort);
            f[SapFields.Group] = Safe(system.Group);
            f[SapFields.SapRouterString] = Safe(system.SapRouterString);
            f[SapFields.SncName] = Safe(system.SncName);
            f[SapFields.SncType] = Safe(system.SncType);
            f[SapFields.Client] = Safe(system.Client);
            f[SapFields.Language] = Safe(system.Language);

            entry.UserName = string.IsNullOrEmpty(system.User) ? null : system.User;
            entry.Title = Safe(system.Name);
            return entry;
        }

        /// <summary>Client used when SAP Logon stores none for a system.</summary>
        public const string DefaultClient = "100";

        /// <summary>Language used when SAP Logon stores none for a system.</summary>
        public const string DefaultLanguage = "EN";

        /// <summary>
        /// Fills client and language with the defaults when they are missing, null, empty or
        /// whitespace. Any other value and every other key stays as it is. Map does not call
        /// this: the import planner must keep seeing what SAP Logon really stores.
        /// </summary>
        public static void ApplyDefaults(IDictionary<string, string> fields)
        {
            if (fields == null)
            {
                return;
            }

            DefaultIfBlank(fields, SapFields.Client, DefaultClient);
            DefaultIfBlank(fields, SapFields.Language, DefaultLanguage);
        }

        /// <summary>
        /// The default user name as it will be stored: trimmed, or null when it is null, empty
        /// or whitespace (meaning "set no user name").
        /// </summary>
        public static string NormalizeDefaultUserName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return text.Trim();
        }

        /// <summary>
        /// The user name a newly created entry gets: the one SAP Logon stores (as stored) or,
        /// when it has none, the normalized default (null = set no user name). Like
        /// ApplyDefaults this applies at fill time only and never takes part in matching.
        /// </summary>
        public static string ApplyDefaultUserName(string sapUserName, string defaultUserName)
        {
            if (!string.IsNullOrEmpty(sapUserName))
            {
                return sapUserName;
            }
            return NormalizeDefaultUserName(defaultUserName);
        }

        private static void DefaultIfBlank(IDictionary<string, string> fields, string key, string value)
        {
            string current;
            if (!fields.TryGetValue(key, out current) || string.IsNullOrWhiteSpace(current))
            {
                fields[key] = value;
            }
        }

        private static string Safe(string value)
        {
            return value ?? "";
        }
    }
}
