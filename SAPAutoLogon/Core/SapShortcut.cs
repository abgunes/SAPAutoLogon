using System;
using System.Collections.Generic;
using System.Text;

namespace SAPAutoLogon
{
    /// <summary>
    /// Builds the argument string for sapshcut.exe (SAP GUI shortcut launcher)
    /// from the string fields of an entry. All connection-string and quoting
    /// logic lives in this class, in the small private helpers below.
    /// </summary>
    public static class SapShortcut
    {
        /// <summary>
        /// True when the fields describe a launchable system: system id and client,
        /// plus either application server and system number or message server and group.
        /// </summary>
        private const string ConnectionText = "Application server + System number or Message server + Group";

        /// <summary>
        /// Names of the fields still needed for <see cref="IsLaunchable"/>, in screen wording
        /// (for example "Client"). Empty when the fields are launchable. Never null.
        /// </summary>
        public static List<string> MissingForLaunch(IDictionary<string, string> fields)
        {
            List<string> missing = new List<string>();
            if (fields == null)
                fields = new Dictionary<string, string>();

            if (IsEmpty(Get(fields, SapFields.SystemId)))
                missing.Add("System ID");
            if (IsEmpty(Get(fields, SapFields.Client)))
                missing.Add("Client");

            bool hasServer = !IsEmpty(Get(fields, SapFields.ApplicationServer));
            bool hasNumber = !IsEmpty(Get(fields, SapFields.SystemNumber));
            bool hasMessageServer = !IsEmpty(Get(fields, SapFields.MessageServer));
            bool hasGroup = !IsEmpty(Get(fields, SapFields.Group));

            bool direct = hasServer && hasNumber;
            bool group = hasMessageServer && hasGroup;
            if (direct || group)
                return missing;

            // Exactly one kind of connection started: name its missing half.
            bool directStarted = hasServer || hasNumber;
            bool groupStarted = hasMessageServer || hasGroup;
            if (directStarted && !groupStarted)
                missing.Add(hasServer ? "System number" : "Application server");
            else if (groupStarted && !directStarted)
                missing.Add(hasMessageServer ? "Group" : "Message server");
            else
                missing.Add(ConnectionText);

            return missing;
        }

        public static bool IsLaunchable(IDictionary<string, string> fields)
        {
            if (fields == null)
                return false;

            if (IsEmpty(Get(fields, SapFields.SystemId)) || IsEmpty(Get(fields, SapFields.Client)))
                return false;

            bool direct = !IsEmpty(Get(fields, SapFields.ApplicationServer))
                && !IsEmpty(Get(fields, SapFields.SystemNumber));
            bool group = !IsEmpty(Get(fields, SapFields.MessageServer))
                && !IsEmpty(Get(fields, SapFields.Group));

            return direct || group;
        }

        /// <summary>
        /// Builds the sapshcut.exe argument string. Empty values are omitted.
        /// Returns an empty string when fields is null.
        /// </summary>
        public static string BuildArguments(IDictionary<string, string> fields, string userName, string password, bool maxGui)
        {
            if (fields == null)
                return string.Empty;

            List<string> parts = new List<string>();

            // Field values are trimmed (a pasted "100 " must not reach SAP GUI);
            // user and password are passed exactly as stored.
            AddArgument(parts, "-system=", Trimmed(fields, SapFields.SystemId));
            AddArgument(parts, "-client=", Trimmed(fields, SapFields.Client));
            AddArgument(parts, "-user=", userName);
            AddArgument(parts, "-pw=", password);
            AddArgument(parts, "-language=", Trimmed(fields, SapFields.Language));

            string connection = BuildConnection(fields);
            if (connection.Length > 0)
                parts.Add("-guiparm=" + QuoteAlways(connection));

            AddArgument(parts, "-command=", Trimmed(fields, SapFields.Transaction));

            string sncName = Trimmed(fields, SapFields.SncName);
            if (!IsEmpty(sncName))
            {
                AddArgument(parts, "-snc_name=", sncName);
                AddArgument(parts, "-snc_qop=", Trimmed(fields, SapFields.SncType));
            }

            if (maxGui)
                parts.Add("-maxgui");

            return string.Join(" ", parts.ToArray());
        }

        // Connection string: "<router>/H/<host>/S/32<NN>" for a direct application server,
        // "<router>/M/<msgserver>[/S/<port>]/G/<group>" for a logon group.
        // Returns an empty string when neither form is complete.
        private static string BuildConnection(IDictionary<string, string> fields)
        {
            // A router string ending in "/H/" would give "/H//H/" below.
            string router = Trimmed(fields, SapFields.SapRouterString) ?? string.Empty;
            if (router.EndsWith("/H/", StringComparison.OrdinalIgnoreCase))
                router = router.Substring(0, router.Length - 3);

            string applicationServer = Get(fields, SapFields.ApplicationServer);
            string systemNumber = Get(fields, SapFields.SystemNumber);
            if (!IsEmpty(applicationServer) && !IsEmpty(systemNumber))
                return router + "/H/" + applicationServer.Trim() + "/S/32" + systemNumber.Trim();

            string messageServer = Get(fields, SapFields.MessageServer);
            string group = Get(fields, SapFields.Group);
            if (!IsEmpty(messageServer) && !IsEmpty(group))
            {
                StringBuilder sb = new StringBuilder(router);
                sb.Append("/M/").Append(messageServer.Trim());

                string port = Get(fields, SapFields.MessageServerPort);
                if (!IsEmpty(port))
                    sb.Append("/S/").Append(port.Trim());

                sb.Append("/G/").Append(group.Trim());
                return sb.ToString();
            }

            return string.Empty;
        }

        // Adds "<prefix><value>" with the value quoted when needed; skips empty values.
        // A whitespace-only user/password is deliberately treated as empty (SAP GUI then prompts).
        private static void AddArgument(List<string> parts, string prefix, string value)
        {
            if (IsEmpty(value))
                return;
            parts.Add(prefix + QuoteIfNeeded(value));
        }

        // Quotes only when the value contains whitespace or a shell-special character.
        private static string QuoteIfNeeded(string value)
        {
            return NeedsQuoting(value) ? QuoteAlways(value) : value;
        }

        private static bool NeedsQuoting(string value)
        {
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c) || c == '"' || c == '&' || c == '|'
                    || c == '<' || c == '>' || c == '^' || c == '%')
                    return true;
            }
            return false;
        }

        // Wraps the value in double quotes using the standard Windows argument rules:
        // a run of backslashes followed by an embedded " is doubled and the " becomes \",
        // a run of backslashes at the very end (before the closing quote) is doubled,
        // backslashes elsewhere stay as they are.
        private static string QuoteAlways(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');

            int backslashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    backslashes++;
                }
                else if (c == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                }
                else
                {
                    sb.Append('\\', backslashes);
                    sb.Append(c);
                    backslashes = 0;
                }
            }

            sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }

        private static string Get(IDictionary<string, string> fields, string key)
        {
            string value;
            if (fields.TryGetValue(key, out value))
                return value;
            return null;
        }

        private static string Trimmed(IDictionary<string, string> fields, string key)
        {
            string value = Get(fields, key);
            return value == null ? null : value.Trim();
        }

        private static bool IsEmpty(string value)
        {
            return value == null || value.Trim().Length == 0;
        }
    }
}
