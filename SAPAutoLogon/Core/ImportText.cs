using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>User-visible strings of the "Import from SAP Logon" feature.</summary>
    public static class ImportText
    {
        public const string MenuItem = "Import from SAP Logon";
        public const string DialogTitle = "Import from SAP Logon";
        public const string LoadingText = "Loading SAP Logon systems...";
        public const string ColumnName = "Name";
        public const string ColumnSid = "SID";
        public const string ColumnServer = "Group/Server";
        public const string SelectFolder = "Select a folder to import.";
        public const string NoSystems = "No SAP GUI systems found in the SAP Logon files.";
        public const string ImportButton = "Import";
        public const string CancelButton = "Cancel";
        public const string ImportFailed = "SAPAutoLogon could not import from SAP Logon.";

        public const string SearchLabel = "Search:";
        public const string ClearSearchButton = "Clear";
        public const string NoSearchMatch = "No folder or system matches the search.";
        public const string ListCaptionNoSelection = "Systems in the selected folder";
        public const string PathSeparator = " > ";

        public const string UserNameCheck = "Set user name for imported entries";
        public const string UserNameBlankHint = "Type a user name or untick the box; an empty user name is not set.";

        public static string UserNamePreview(string userName, int entriesWithoutUser)
        {
            return "User name '" + (userName ?? "") + "' will be set for " + entriesWithoutUser
                + " entries that have no user in SAP Logon.";
        }

        public static string HeaderSubtitle(string groupName)
        {
            return "Choose an SAP Logon folder. Its subfolders become groups and its systems become entries in group '"
                + (groupName ?? "") + "'.";
        }

        public static string FolderLabel(string name, int count)
        {
            return (name ?? "") + " (" + count + ")";
        }

        public static string JoinPath(IList<string> names)
        {
            if (names == null)
            {
                return "";
            }
            return string.Join(PathSeparator, names);
        }

        public static string ListCaption(string path, int shown, int total)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ListCaptionNoSelection;
            }
            if (total == 0)
            {
                return "Systems in " + path + " (none directly in this folder)";
            }
            if (shown == total)
            {
                return "Systems in " + path;
            }
            return "Systems in " + path + " (" + shown + " of " + total + " match the search)";
        }

        public static string Preview(int entriesToAdd, int groupsToCreate, int skipped)
        {
            string text = entriesToAdd + " entries and " + groupsToCreate + " groups will be created";
            if (skipped > 0)
            {
                text += ", " + skipped + " entries already present will be skipped";
            }
            return text + ".";
        }

        public static string Confirmation(int systems, string groupName)
        {
            return "Import " + systems + (systems == 1 ? " system" : " systems")
                + " into group '" + groupName + "'?";
        }

        public static string Summary(int added, int skipped, int groupsCreated)
        {
            return added + " entries added, " + skipped + " skipped (already present), "
                + groupsCreated + " groups created";
        }

        public static string ServerColumn(SapSystem system)
        {
            if (system == null)
            {
                return "";
            }
            return (system.IsGroup ? system.Group : system.ApplicationServer) ?? "";
        }

        public static bool MenuEnabled(bool databaseOpen, bool fileReadOnly, bool groupSelected, bool inRecycleBin)
        {
            return databaseOpen && !fileReadOnly && groupSelected && !inRecycleBin;
        }
    }
}
