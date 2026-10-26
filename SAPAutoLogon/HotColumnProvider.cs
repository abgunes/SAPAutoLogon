using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;
using System.Collections.Generic;
using System.Linq;

namespace SAPAutoLogon
{
    internal class HotColumnProvider : ColumnProvider
    {
        private PluginParameters pluginParameters;
        private Dictionary<string, IPluginColumn> Columns = new Dictionary<string, IPluginColumn>();

        public override string[] ColumnNames
        {
            get { return Columns.Keys.ToArray(); }
        }

        public HotColumnProvider(string column, PluginParameters param)
        {
            pluginParameters = param;

            switch (column)
            {
                case SAPAutoLogonExt.ColGuiName:
                    Columns.Add(column, new PluginColumnGui(column, pluginParameters));
                    break;
                case SAPAutoLogonExt.ColNBCName:
                    Columns.Add(column, new PluginColumnBC(column, pluginParameters));
                    break;
            }
        }

        public override string GetCellData(string strColumnName, PwEntry pe)
        {
            return Columns[strColumnName].GetContent(pe);
        }

        public override bool SupportsCellAction(string strColumnName)
        {
            return Columns[strColumnName].HasAction();
        }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            Launch(strColumnName, pe);
        }

        /// <summary>
        /// Starts the program of a column for an entry; shared by the cell double-click and
        /// the entry context menu. Shows a warning when the entry or the program is not
        /// usable and never lets an exception reach KeePass.
        /// </summary>
        public void Launch(string strColumnName, PwEntry pe)
        {
            if (pe == null) { return; }

            try
            {
                if (!PluginColumn.IsEntryLaunchable(pe))
                {
                    MessageService.ShowWarning("This entry has no complete SAP connection (needs systemId, client and a server or message server).");
                    return;
                }

                IPluginColumn column = Columns[strColumnName];
                string missing = column.GetMissingProgramMessage();
                if (!string.IsNullOrEmpty(missing))
                {
                    MessageService.ShowWarning(missing);
                    return;
                }

                column.PerformAction(pe);
            }
            catch (System.Exception ex)
            {
                // Never let an exception reach KeePass
                MessageService.ShowWarning("SAPAutoLogon could not start the SAP program.", ex.Message);
            }
        }

        /// <summary>True when the program of this column is configured and the file exists (checked now).</summary>
        public bool IsProgramAvailable(string strColumnName)
        {
            IPluginColumn column;
            if (!Columns.TryGetValue(strColumnName, out column)) { return false; }
            return column.IsProgramAvailable();
        }

        /// <summary>Forgets remembered file checks of all columns of this provider.</summary>
        public void InvalidateCache()
        {
            foreach (IPluginColumn column in Columns.Values)
            {
                column.InvalidateCache();
            }
        }
    }
}
