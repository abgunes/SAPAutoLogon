using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>One existing column of the KeePass entry list, in list order.</summary>
    public sealed class ColumnInfo
    {
        /// <summary>True for columns supplied by a plugin (KeePass type PluginExt).</summary>
        public bool IsPluginColumn;
        public string Name = "";
        public int Width;
    }

    /// <summary>
    /// One step of the migration: either rename the column at RenameIndex to NewName
    /// (it keeps its position and gets RenameWidth) or append a new column NewName
    /// with AppendWidth.
    /// </summary>
    public sealed class ColumnChange
    {
        public int RenameIndex = -1;
        public string NewName = "";
        public int RenameWidth;
        public bool Append;
        public int AppendWidth;
    }

    /// <summary>
    /// Decides how the entry list's columns must change so that the plugin's two
    /// columns are shown. The previous Gui/NBC columns are renamed in place and widened
    /// to DefaultWidth when they are narrower.
    /// </summary>
    public static class ColumnMigration
    {
        public const string GuiColumn = LaunchText.GuiColumnName;
        public const string BusinessClientColumn = LaunchText.BusinessClientColumnName;
        public const string OldGuiColumn = "Gui";
        public const string OldBusinessClientColumn = "NBC";
        public const int DefaultWidth = 140;

        public static List<ColumnChange> Plan(IList<ColumnInfo> existing)
        {
            List<ColumnChange> changes = new List<ColumnChange>();
            int count = (existing == null) ? 0 : existing.Count;
            bool[] claimed = new bool[count];

            PlanOne(existing, claimed, GuiColumn, OldGuiColumn, changes);
            PlanOne(existing, claimed, BusinessClientColumn, OldBusinessClientColumn, changes);

            return changes;
        }

        private static void PlanOne(IList<ColumnInfo> existing, bool[] claimed,
            string wanted, string oldName, List<ColumnChange> changes)
        {
            int present = FindPluginColumn(existing, claimed, wanted);
            if (present >= 0)
            {
                claimed[present] = true;
                return;
            }

            ColumnChange change = new ColumnChange();
            change.NewName = wanted;

            int old = FindPluginColumn(existing, claimed, oldName);
            if (old >= 0)
            {
                claimed[old] = true;
                change.RenameIndex = old;
                // keep a width that is already wide enough for the cell text, else widen
                change.RenameWidth = System.Math.Max(existing[old].Width, DefaultWidth);
            }
            else
            {
                change.Append = true;
                change.AppendWidth = DefaultWidth;
            }

            changes.Add(change);
        }

        // Index of the first unclaimed plugin column with exactly this name, or -1.
        private static int FindPluginColumn(IList<ColumnInfo> existing, bool[] claimed, string name)
        {
            for (int i = 0; i < claimed.Length; i++)
            {
                ColumnInfo column = existing[i];
                if (claimed[i] || column == null || !column.IsPluginColumn) { continue; }
                if (string.Equals(column.Name, name, System.StringComparison.Ordinal)) { return i; }
            }

            return -1;
        }
    }
}
