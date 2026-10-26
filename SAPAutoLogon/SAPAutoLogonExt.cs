using KeePass.Plugins;
using KeePassLib;
using KeePassLib.Utility;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    public sealed class SAPAutoLogonExt : Plugin
    {
        #region Public section
        // Plugin name constant
        public const string PlugInName = "SAPAutoLogon";

        // Name of the columns
        public const string ColGuiName = LaunchText.GuiColumnName;
        public const string ColNBCName = LaunchText.BusinessClientColumnName;

        // Remember plugin's host instance
        public static IPluginHost oHost = null;

        // Set plugin's icon
        public override Image SmallIcon
        {
            get
            {
                if (oSmallIcon == null) { oSmallIcon = Properties.Resources.sap_image; }
                return oSmallIcon;
            }
        }

        // The resource getter creates a new bitmap on every call
        private Image oSmallIcon = null;

        /// <summary>
        /// KP constructor called when plugin is loaded
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public override bool Initialize(IPluginHost host)
        {
            Terminate();

            if (host == null) { return false; }

            // Initialize runtime
            InitGlobals(host);

            //Create columns
            InitColumns();
            oEvenHandler.OptionsApplied = InvalidateColumnCaches;

            // Show the columns once without manual selection. KeePass builds the list's
            // columns from the configuration again right after loading the plugins, so
            // the change is visible at once. Never allowed to break the plugin.
            try { ColumnSetup.Apply(oParameters, SaveColumnsSetUp); }
            catch (Exception) { }

            // Get a reference to the 'Tools' menu item container
            ToolStripItemCollection oMenu = oHost.MainWindow.ToolsMenu.DropDownItems;

            // Add the popup menu item
            oMenuItem = new ToolStripMenuItem
            {
                Text = PluginParameters.menuItemText,
                Image = SmallIcon
            };
            oMenuItem.Click += oEvenHandler.OnSettingsDlg;
            oMenu.Add(oMenuItem);

            // Launch items in the entry list's context menu; never allowed to break the plugin
            try { InitEntryMenu(); }
            catch (Exception) { }

            // Ctrl+Shift+G / Ctrl+Shift+N launch shortcuts; never allowed to break the plugin
            try { oShortcutFilter = new LaunchShortcutFilter(oHost.MainWindow, OnLaunchShortcut); }
            catch (Exception) { oShortcutFilter = null; }

            // Import from SAP Logon in the group tree's context menu; never allowed to break the plugin
            try { oImportCommand = new ImportGroupCommand(oHost); }
            catch (Exception) { oImportCommand = null; }

            // SAP tab in the Add/Edit Entry dialog; never allowed to break the plugin
            try { oTabHost = new SapTabHost(); }
            catch (Exception) { oTabHost = null; }

            // Initialization was succefull
            return true;
        }

        /// <summary>
        /// The <c>Terminate</c> method is called by KeePass when
        /// you should free all resources, close files/streams,
        /// remove event handlers, etc.
        /// </summary>
        public override void Terminate()
        {
            if (oTabHost != null)
            {
                try { oTabHost.Dispose(); }
                catch (Exception) { }
                oTabHost = null;
            }

            if (oShortcutFilter != null)
            {
                try { oShortcutFilter.Dispose(); }
                catch (Exception) { }
                oShortcutFilter = null;
            }

            if (oHost == null) return;

            RemoveEntryMenu();

            if (oImportCommand != null)
            {
                try { oImportCommand.Dispose(); }
                catch (Exception) { }
                oImportCommand = null;
            }

            oHost.ColumnProviderPool.Remove(oColumnGui);
            oColumnGui = null;
            oHost.ColumnProviderPool.Remove(oColumnBC);
            oColumnBC = null;

            // Remove menu items
            ToolStripItemCollection oMenu = oHost.MainWindow.ToolsMenu.DropDownItems;

            if (oMenuItem != null)
            {
                // Unregister event handler
                oMenuItem.Click -= oEvenHandler.OnSettingsDlg;
                oMenu.Remove(oMenuItem);
            }

            oHost = null;

            base.Terminate();
        }
        #endregion

        #region Protected section

        #endregion

        #region Private section
        // Atribute holds app menu context
        private ToolStripMenuItem oMenuItem = null;

        // Prepare refernces for plugin's column
        private HotColumnProvider oColumnGui = null;
        private HotColumnProvider oColumnBC = null;

        // Items of the entry list's context menu
        private ToolStripSeparator oEntrySeparator = null;
        private ToolStripMenuItem oEntryGuiItem = null;
        private ToolStripMenuItem oEntryNbcItem = null;
        private Image oEntryIcon = null;

        // Plugin parameters and default values
        private PluginParameters oParameters;

        // Plugin option handler
        private PluginOptions oOptions;

        // Event handler
        private PluginEventHandler oEvenHandler = null;

        // Adds the SAP tab to entry dialogs
        private SapTabHost oTabHost = null;

        // Launch shortcuts (Ctrl+Shift+G, Ctrl+Shift+N)
        private LaunchShortcutFilter oShortcutFilter = null;

        // Import from SAP Logon in the group context menu
        private ImportGroupCommand oImportCommand = null;

        private void InitGlobals(IPluginHost host)
        {
            oHost = host;

            // Get KeePass option handler
            oOptions = new PluginOptions(oHost.CustomConfig);

            // Get saved plugin parameters
            oParameters = PluginParameters.Parse(oOptions.PluginOptionString);

            // Get binaries path
            var oExec = new SAPBinaries();
            if (string.IsNullOrEmpty(oParameters.sapGuiPath))
            {
                oParameters.sapGuiPath = oExec.DetectSAPGUIPath(PluginParameters.SAPGUIShortCutEXE);
            }

            if (string.IsNullOrEmpty(oParameters.nBCPath))
            {
                oParameters.nBCPath = oExec.DetectSAPGUIPath(PluginParameters.SAPNWBCShortCutEXE);
            }

            // Inicialize event handler
            oEvenHandler = new PluginEventHandler(oParameters, oOptions);
        }

        // The caller wraps this in try/catch: a failing menu must not break the plugin
        private void InitEntryMenu()
        {
            ContextMenuStrip menu = oHost.MainWindow.EntryContextMenu;
            if (menu == null) { return; }

            oEntryIcon = CreateMenuIcon();

            oEntrySeparator = new ToolStripSeparator();
            oEntryGuiItem = new ToolStripMenuItem(LaunchText.GuiMenuItem, oEntryIcon);
            // Display text only: setting ShortcutKeys would fire together with the message filter
            oEntryGuiItem.ShortcutKeyDisplayString = LaunchText.GuiShortcutText;
            oEntryGuiItem.Click += OnEntryGuiClick;
            oEntryNbcItem = new ToolStripMenuItem(LaunchText.BusinessClientMenuItem, oEntryIcon);
            oEntryNbcItem.ShortcutKeyDisplayString = LaunchText.BusinessClientShortcutText;
            oEntryNbcItem.Click += OnEntryNbcClick;

            menu.Items.Add(oEntrySeparator);
            menu.Items.Add(oEntryGuiItem);
            menu.Items.Add(oEntryNbcItem);
            menu.Opening += OnEntryMenuOpening;
        }

        private void RemoveEntryMenu()
        {
            try
            {
                ContextMenuStrip menu = oHost.MainWindow.EntryContextMenu;
                if (menu != null)
                {
                    menu.Opening -= OnEntryMenuOpening;
                    if (oEntryGuiItem != null) { menu.Items.Remove(oEntryGuiItem); }
                    if (oEntryNbcItem != null) { menu.Items.Remove(oEntryNbcItem); }
                    if (oEntrySeparator != null) { menu.Items.Remove(oEntrySeparator); }
                }
            }
            catch (Exception) { }

            if (oEntryGuiItem != null) { oEntryGuiItem.Click -= OnEntryGuiClick; oEntryGuiItem.Dispose(); oEntryGuiItem = null; }
            if (oEntryNbcItem != null) { oEntryNbcItem.Click -= OnEntryNbcClick; oEntryNbcItem.Dispose(); oEntryNbcItem = null; }
            if (oEntrySeparator != null) { oEntrySeparator.Dispose(); oEntrySeparator = null; }
            if (oEntryIcon != null) { oEntryIcon.Dispose(); oEntryIcon = null; }
        }

        // The SAP image is large; the menu needs a 16x16 copy.
        internal static Image CreateMenuIcon()
        {
            try
            {
                using (Image source = Properties.Resources.sap_image)
                {
                    Bitmap icon = new Bitmap(16, 16);
                    using (Graphics g = Graphics.FromImage(icon))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(source, 0, 0, 16, 16);
                    }
                    return icon;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // The entry when exactly one is selected in the entry list, otherwise null.
        private static PwEntry GetSingleSelectedEntry(out int count)
        {
            count = 0;
            PwEntry[] selected = oHost.MainWindow.GetSelectedEntries();
            if (selected == null) { return null; }

            count = selected.Length;
            return count == 1 ? selected[0] : null;
        }

        private void OnEntryMenuOpening(object sender, CancelEventArgs e)
        {
            try
            {
                int count;
                PwEntry entry = GetSingleSelectedEntry(out count);

                SetEnabled(oEntryGuiItem, oColumnGui, ColGuiName, entry, count);
                SetEnabled(oEntryNbcItem, oColumnBC, ColNBCName, entry, count);
            }
            catch (Exception)
            {
                if (oEntryGuiItem != null) { oEntryGuiItem.Enabled = false; }
                if (oEntryNbcItem != null) { oEntryNbcItem.Enabled = false; }
            }
        }

        private static void SetEnabled(ToolStripMenuItem item, HotColumnProvider column, string name, PwEntry entry, int count)
        {
            if (item == null) { return; }

            // Same helper as the columns for the entry; the program file is checked only
            // for an entry that could be launched
            bool launchable = PluginColumn.IsEntryLaunchable(entry);
            bool programExists = launchable && column != null && column.IsProgramAvailable(name);
            item.Enabled = LaunchText.MenuItemEnabled(count, launchable, programExists);
        }

        private void OnEntryGuiClick(object sender, EventArgs e) { TryLaunchSelected(oColumnGui, ColGuiName); }
        private void OnEntryNbcClick(object sender, EventArgs e) { TryLaunchSelected(oColumnBC, ColNBCName); }

        // Called by the shortcut filter; true only when a program was started
        private bool OnLaunchShortcut(LaunchTarget target)
        {
            switch (target)
            {
                case LaunchTarget.SapGui:
                    return TryLaunchSelected(oColumnGui, ColGuiName);
                case LaunchTarget.BusinessClient:
                    return TryLaunchSelected(oColumnBC, ColNBCName);
                default:
                    return false;
            }
        }

        // The one place that decides whether the selection can be launched and does it,
        // for the context menu items and the shortcuts: exactly one entry selected, the
        // entry launchable and the program available. Returns true only when the launch
        // was started; otherwise false and no dialog, so the caller (and KeePass) can
        // treat the click or key as not handled.
        private static bool TryLaunchSelected(HotColumnProvider column, string name)
        {
            try
            {
                if (column == null || oHost == null) { return false; }

                int count;
                PwEntry entry = GetSingleSelectedEntry(out count);

                bool launchable = PluginColumn.IsEntryLaunchable(entry);
                bool programExists = launchable && column.IsProgramAvailable(name);
                if (!LaunchText.MenuItemEnabled(count, launchable, programExists)) { return false; }

                // Reports a failure of the start itself in a warning
                column.Launch(name, entry);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Stores only the flag: the stored string is re-read so auto-detected program
        // folders (not chosen by the user) are not written to the configuration.
        private void SaveColumnsSetUp()
        {
            PluginParameters stored = PluginParameters.Parse(oOptions.PluginOptionString);
            stored.columnsSetUp = true;
            oOptions.PluginOptionString = stored.ToString();
        }

        private void InvalidateColumnCaches()
        {
            if (oColumnGui != null) { oColumnGui.InvalidateCache(); }
            if (oColumnBC != null) { oColumnBC.InvalidateCache(); }
        }

        private void InitColumns()
        {
            // Set name of new columns
            string[] columns = { ColGuiName, ColNBCName };

            // Create instances of new column handler and add it keepass
            // Gui
            oColumnGui = new HotColumnProvider(columns[0], oParameters);
            oHost.ColumnProviderPool.Add(oColumnGui);
            // BC
            oColumnBC = new HotColumnProvider(columns[1], oParameters);
            oHost.ColumnProviderPool.Add(oColumnBC);
        }
        #endregion       
    }
}
