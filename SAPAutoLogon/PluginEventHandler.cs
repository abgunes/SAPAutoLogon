using System;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    internal class PluginEventHandler
    {
        private PluginParameters oParameters;
        private PluginOptions oOptions;

        /// <summary>Called after the options dialog was closed with OK, before the entry list is refreshed.</summary>
        public Action OptionsApplied;

        public PluginEventHandler(PluginParameters oParameters, PluginOptions oOptions)
        {
            this.oParameters = oParameters;
            this.oOptions = oOptions;
        }

        /// <summary>
        /// Handle event for menu click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnSettingsDlg(object sender, EventArgs e)
        {
            OptionDialog dialog = new OptionDialog(SAPAutoLogonExt.oHost.MainWindow, oOptions, oParameters);
            DialogResult result = dialog.ShowDialog();

            dialog.Dispose();

            if (result == DialogResult.OK) { ApplyNewOptions(); }
        }

        // The program folders may have changed: forget the remembered file checks and
        // redraw the entry list so the launch columns show the new state at once.
        // Never throws into KeePass.
        private void ApplyNewOptions()
        {
            try
            {
                if (OptionsApplied != null) { OptionsApplied(); }
            }
            catch (Exception) { }

            try
            {
                // Redraws the rows that are shown; the selection stays
                SAPAutoLogonExt.oHost.MainWindow.RefreshEntriesList();
            }
            catch (Exception) { }
        }
    }
}