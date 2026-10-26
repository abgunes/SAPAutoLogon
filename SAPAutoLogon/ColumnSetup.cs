using KeePass.App.Configuration;
using KeePassLib.Utility;
using System;
using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// Adds the plugin's two columns to KeePass's entry-list configuration once.
    /// Must run while KeePass loads the plugins: KeePass builds the list's columns
    /// before loading plugins and builds them again from the configuration right after
    /// the plugins were loaded (a plugin added column providers), so changes made here
    /// show up without any further refresh.
    /// </summary>
    public static class ColumnSetup
    {
        /// <summary>
        /// Returns true when the configuration was changed. Never throws. The flag
        /// parameters.columnsSetUp is set and saved only after the configuration was
        /// processed successfully.
        /// </summary>
        public static bool Apply(PluginParameters parameters, Action saveOptions)
        {
            try
            {
                if (parameters == null || parameters.columnsSetUp) { return false; }

                AceMainWindow mainWindow = KeePass.Program.Config.MainWindow;
                if (mainWindow == null || mainWindow.EntryListColumns == null) { return false; }

                string displayOrder = mainWindow.EntryListColumnDisplayOrder;
                bool changed = ApplyToList(mainWindow.EntryListColumns, ref displayOrder);
                if (changed) { mainWindow.EntryListColumnDisplayOrder = displayOrder; }

                parameters.columnsSetUp = true;
                try
                {
                    if (saveOptions != null) { saveOptions(); }
                }
                catch (Exception) { }

                return changed;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Applies ColumnMigration.Plan to the list: renames keep their position and are
        /// widened to the default width when narrower, new columns go to the end.
        /// displayOrder (KeePass's space-separated display indices, one per column) is
        /// extended so the new columns are shown last; an empty or inconsistent order is
        /// reset to empty, as KeePass's own column dialog does, which means "list order".
        /// Returns true when the list was changed.
        /// </summary>
        public static bool ApplyToList(List<AceColumn> columns, ref string displayOrder)
        {
            if (columns == null) { return false; }

            List<ColumnInfo> infos = new List<ColumnInfo>();
            foreach (AceColumn column in columns)
            {
                ColumnInfo info = new ColumnInfo();
                if (column != null)
                {
                    info.IsPluginColumn = column.Type == AceColumnType.PluginExt;
                    info.Name = column.CustomName ?? "";
                    info.Width = column.Width;
                }
                infos.Add(info);
            }

            List<ColumnChange> changes = ColumnMigration.Plan(infos);
            if (changes.Count == 0) { return false; }

            int oldCount = columns.Count;
            int[] oldOrder = null;
            if (!string.IsNullOrEmpty(displayOrder))
            {
                try { oldOrder = StrUtil.DeserializeIntArray(displayOrder); }
                catch (Exception) { oldOrder = null; }
            }

            foreach (ColumnChange change in changes)
            {
                if (change.Append)
                {
                    columns.Add(new AceColumn(AceColumnType.PluginExt, change.NewName, false, change.AppendWidth));
                }
                else if (change.RenameIndex >= 0 && change.RenameIndex < oldCount)
                {
                    columns[change.RenameIndex].CustomName = change.NewName;
                    columns[change.RenameIndex].Width = change.RenameWidth;
                }
            }

            if (oldOrder == null || oldOrder.Length != oldCount)
            {
                displayOrder = string.Empty;
            }
            else if (columns.Count != oldCount)
            {
                int[] newOrder = new int[columns.Count];
                Array.Copy(oldOrder, newOrder, oldCount);
                for (int i = oldCount; i < newOrder.Length; i++) { newOrder[i] = i; }
                displayOrder = StrUtil.SerializeIntArray(newOrder);
            }

            return true;
        }
    }
}
