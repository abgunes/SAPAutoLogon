using KeePass.Forms;
using KeePassLib.Utility;
using System;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    public partial class OptionDialog : Form
    {
        private readonly PluginOptions pluginOptions;
        private readonly MainForm mainForm;
        private PluginParameters parameters;

        public OptionDialog(MainForm form, PluginOptions config, PluginParameters parameters)
        {
            pluginOptions = config;
            mainForm = form;
            this.parameters = parameters;

            // Standard method
            InitializeComponent();

            // Load data to form fileds
            LoadData();
        }

        private void LoadData()
        {
            if (pluginOptions == null) { return; }

            // Set Form fields value
            isMaxGui.Checked = parameters.isMaxGui;
            iSapGuiPath.Text = parameters.sapGuiPath;
            iNBCSPath.Text = parameters.nBCPath;
        }

        // Fills the box with the folder found in the registry; a box is left
        // unchanged when the program is not found there.
        private void DetectInto(TextBox box, string exe, string programName)
        {
            try
            {
                string folder = new SAPBinaries().DetectSAPGUIPath(exe);
                if (folder.Length > 0)
                {
                    box.Text = folder;
                }
                else
                {
                    MessageService.ShowWarning(programName + " (" + exe + ") was not found on this PC. Type its folder in the box.");
                }
            }
            catch (Exception) { }
        }

        private void OnBtnDetectGui_Click(object sender, EventArgs e)
        {
            DetectInto(iSapGuiPath, PluginParameters.SAPGUIShortCutEXE, "SAP GUI");
        }

        private void OnBtnDetectNbc_Click(object sender, EventArgs e)
        {
            DetectInto(iNBCSPath, PluginParameters.SAPNWBCShortCutEXE, "SAP Business Client");
        }

        private void SaveOptions()
        {
            if (pluginOptions != null)
            {
                // Copy the form values to the active plugin parameters
                parameters.isMaxGui = isMaxGui.Checked;
                parameters.sapGuiPath = iSapGuiPath.Text;
                parameters.nBCPath = iNBCSPath.Text;

                // Save only these three values into the stored string: the other keys
                // (columns, import options) may have been written since the plugin started.
                PluginParameters stored = PluginParameters.Parse(pluginOptions.PluginOptionString);
                stored.isMaxGui = parameters.isMaxGui;
                stored.sapGuiPath = parameters.sapGuiPath;
                stored.nBCPath = parameters.nBCPath;
                pluginOptions.PluginOptionString = stored.ToString();
            }
        }

        private void OnBtnOK_Click(object sender, EventArgs e)
        {
            // Plugin has not any check for this moment
            DialogResult = DialogResult.OK;

            SaveOptions();
            Close();
        }

        private void OnBtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
