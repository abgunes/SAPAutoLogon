using System;
using System.Collections.Generic;

namespace SAPAutoLogon
{
    public static class SystemFilter
    {
        /// <summary>
        /// Case-insensitive substring match on Name, SystemId, ApplicationServer and MessageServer.
        /// Empty or whitespace text returns all systems. Order and object references are preserved.
        /// </summary>
        public static List<SapSystem> Filter(IList<SapSystem> systems, string text)
        {
            List<SapSystem> result = new List<SapSystem>();
            if (systems == null)
                return result;

            bool all = string.IsNullOrWhiteSpace(text);
            string needle = all ? "" : text.Trim();

            foreach (SapSystem s in systems)
            {
                if (all || Matches(s, needle))
                    result.Add(s);
            }
            return result;
        }

        private static bool Matches(SapSystem s, string needle)
        {
            return Contains(s.Name, needle)
                || Contains(s.SystemId, needle)
                || Contains(s.ApplicationServer, needle)
                || Contains(s.MessageServer, needle);
        }

        private static bool Contains(string value, string needle)
        {
            return value != null && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
