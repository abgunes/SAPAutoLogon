namespace SAPAutoLogon
{
    /// <summary>
    /// Texts of the launch actions (entry list columns and entry context menu)
    /// and the rule for when a menu item is enabled.
    /// </summary>
    public static class LaunchText
    {
        public const string GuiColumnName = "SAP GUI";
        public const string BusinessClientColumnName = "Business Client";

        // Cell text of a launchable entry (both columns): a play symbol (U+25B6) and a
        // short wording. Change either constant to change the text. The cell action
        // itself is a double-click (KeePass runs a column action only on double-click).
        public const string CellGlyph = "\u25B6 ";
        public const string CellWording = "Click-To-Open";
        public const string GuiCell = CellGlyph + CellWording;
        public const string BusinessClientCell = CellGlyph + CellWording;

        public const string GuiMenuItem = "Open in SAP GUI";
        public const string BusinessClientMenuItem = "Open in Business Client";

        // Shortcut text shown on the right of the menu items (see LaunchShortcut)
        public const string GuiShortcutText = "Ctrl+Shift+G";
        public const string BusinessClientShortcutText = "Ctrl+Shift+N";

        /// <summary>The label when the entry can be started, otherwise an empty string.</summary>
        public static string Cell(bool executable, string label)
        {
            return executable ? label : string.Empty;
        }

        /// <summary>
        /// A context menu item is enabled for exactly one selected entry that has a
        /// complete SAP connection while the program of the item exists.
        /// </summary>
        public static bool MenuItemEnabled(int selectedCount, bool entryLaunchable, bool programExists)
        {
            return selectedCount == 1 && entryLaunchable && programExists;
        }
    }
}
