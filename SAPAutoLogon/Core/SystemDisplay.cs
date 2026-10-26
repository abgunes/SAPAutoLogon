using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>Text of a system in the SAP tab list.</summary>
    public static class SystemDisplay
    {
        /// <summary>
        /// "Name  [SID | server]", where server is the application server or, for a
        /// logon group, the message server. Empty parts are left out. Never throws.
        /// </summary>
        public static string Format(SapSystem system)
        {
            if (system == null)
                return "";

            string name = system.Name ?? "";
            string server = system.IsGroup ? system.MessageServer : system.ApplicationServer;
            if (string.IsNullOrEmpty(server))
                server = !string.IsNullOrEmpty(system.ApplicationServer) ? system.ApplicationServer : system.MessageServer;

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(system.SystemId))
                parts.Add(system.SystemId);
            if (!string.IsNullOrEmpty(server))
                parts.Add(server);

            if (parts.Count == 0)
                return name;
            return name + "  [" + string.Join(" | ", parts.ToArray()) + "]";
        }
    }
}
