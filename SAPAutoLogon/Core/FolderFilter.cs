using System;
using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// One folder of a filtered SAP Logon tree. Folder and the systems are the
    /// original objects, never copies.
    /// </summary>
    public sealed class FolderMatch
    {
        public SapFolder Folder;
        public bool NameMatches;
        public List<SapSystem> Systems = new List<SapSystem>();
        public List<FolderMatch> Folders = new List<FolderMatch>();
    }

    public static class FolderFilter
    {
        public static bool IsActive(string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// Filters the folder tree by text. Inactive text keeps everything. Otherwise a
        /// folder is kept when its name matches, one of its direct systems matches, or
        /// a child folder is kept. A folder whose name matches keeps all its direct
        /// systems and its whole subtree; NameMatches marks every kept folder whose own
        /// name contains the text.
        /// </summary>
        public static List<FolderMatch> Apply(IList<SapFolder> roots, string text)
        {
            bool active = IsActive(text);
            string needle = active ? text.Trim() : "";
            return Walk(roots, active, needle, false);
        }

        private static List<FolderMatch> Walk(IList<SapFolder> folders, bool active, string needle, bool keepAll)
        {
            List<FolderMatch> result = new List<FolderMatch>();
            if (folders == null)
            {
                return result;
            }

            foreach (SapFolder folder in folders)
            {
                if (folder == null)
                {
                    continue;
                }

                FolderMatch match = new FolderMatch();
                match.Folder = folder;
                match.NameMatches = active && folder.Name != null
                    && folder.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

                bool all = !active || keepAll || match.NameMatches;
                match.Folders = Walk(folder.Folders, active, needle, all);
                match.Systems = all
                    ? new List<SapSystem>(folder.Systems)
                    : SystemFilter.Filter(folder.Systems, needle);

                if (all || match.Systems.Count > 0 || match.Folders.Count > 0)
                {
                    result.Add(match);
                }
            }
            return result;
        }
    }
}
