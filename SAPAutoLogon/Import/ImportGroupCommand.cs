using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;

namespace SAPAutoLogon
{
    /// <summary>
    /// The "Import from SAP Logon" item at the end of the group tree's context
    /// menu. Nothing here may throw into KeePass: every handler catches.
    /// </summary>
    public sealed class ImportGroupCommand : IDisposable
    {
        private readonly IPluginHost m_host;
        private ContextMenuStrip m_menu;
        private ToolStripSeparator m_separator;
        private ToolStripMenuItem m_item;
        private Image m_icon;
        private PluginOptions m_options;
        private bool m_disposed;

        public ImportGroupCommand(IPluginHost host)
        {
            m_host = host;
            if (host == null || host.MainWindow == null) { return; }

            ContextMenuStrip menu = host.MainWindow.GroupContextMenu;
            if (menu == null) { return; }

            try
            {
                // Without options the popup just uses the defaults and remembers nothing.
                try
                {
                    if (host.CustomConfig != null)
                    {
                        m_options = new PluginOptions(host.CustomConfig);
                    }
                }
                catch (Exception) { m_options = null; }

                m_icon = SapIconHelper.CreateMenuImage();
                m_separator = new ToolStripSeparator();
                m_item = new ToolStripMenuItem(ImportText.MenuItem, m_icon);

                m_menu = menu;
                menu.Items.Add(m_separator);
                menu.Items.Add(m_item);
                menu.Opening += OnOpening;
                m_item.Click += OnClick;
            }
            catch (Exception)
            {
                // Leave no stray separator or item in the menu; Dispose never throws.
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (m_disposed) { return; }
            m_disposed = true;

            if (m_menu != null)
            {
                try { m_menu.Opening -= OnOpening; }
                catch (Exception) { }
                try { if (m_item != null) { m_menu.Items.Remove(m_item); } }
                catch (Exception) { }
                try { if (m_separator != null) { m_menu.Items.Remove(m_separator); } }
                catch (Exception) { }
                m_menu = null;
            }

            if (m_item != null)
            {
                try { m_item.Click -= OnClick; }
                catch (Exception) { }
                try { m_item.Dispose(); }
                catch (Exception) { }
                m_item = null;
            }

            if (m_separator != null)
            {
                try { m_separator.Dispose(); }
                catch (Exception) { }
                m_separator = null;
            }

            if (m_icon != null)
            {
                try { m_icon.Dispose(); }
                catch (Exception) { }
                m_icon = null;
            }
        }

        private void OnOpening(object sender, CancelEventArgs e)
        {
            try
            {
                if (m_item == null) { return; }

                PwDatabase db;
                PwGroup group;
                m_item.Enabled = CanImport(out db, out group);
            }
            catch (Exception)
            {
                try { if (m_item != null) { m_item.Enabled = false; } }
                catch (Exception) { }
            }
        }

        // The menu rule: an open, unlocked database whose file is not read-only,
        // and a selected group that is not the Recycle Bin or inside it.
        private bool CanImport(out PwDatabase db, out PwGroup group)
        {
            db = m_host.Database;
            PwDocument doc = m_host.MainWindow.DocumentManager.ActiveDocument;
            bool databaseOpen = db != null && db.IsOpen && !m_host.MainWindow.IsFileLocked(doc);
            bool fileReadOnly = KeePassGroupImporter.IsFileReadOnly(db);
            group = m_host.MainWindow.GetSelectedGroup();
            bool groupSelected = group != null;
            bool inRecycleBin = KeePassGroupImporter.IsInRecycleBin(group, db);
            return ImportText.MenuEnabled(databaseOpen, fileReadOnly, groupSelected, inRecycleBin);
        }

        private void OnClick(object sender, EventArgs e)
        {
            try
            {
                // The state may have changed since the menu opened.
                PwDatabase db;
                PwGroup group;
                if (!CanImport(out db, out group)) { return; }

                GroupSnapshot snapshot = KeePassGroupImporter.Snapshot(group);
                PluginParameters stored = ReadOptions();

                SapFolder folder = null;
                string userName = null;
                using (Image header = SapIconHelper.CreateLogoImage(64))
                using (Image row = SapIconHelper.CreateMenuImage())
                {
                    using (SapImportDialog dialog = new SapImportDialog(group.Name, snapshot))
                    {
                        dialog.SetImages(header, row);
                        dialog.SetUserNameOption(stored.importSetUserName, stored.importUserName);
                        dialog.RestoreLayout(stored.importDialogWidth, stored.importDialogHeight, stored.importSplitter);
                        dialog.BeginLoad(LandscapeSession.Cache);

                        DialogResult answer = ShowRegistered(dialog);
                        bool confirmed = answer == DialogResult.OK;

                        int width;
                        int height;
                        int splitter;
                        dialog.CaptureLayout(out width, out height, out splitter);

                        bool setUserName = false;
                        string userNameText = "";
                        if (confirmed)
                        {
                            folder = dialog.SelectedFolder;
                            userName = dialog.DefaultUserName;
                            setUserName = dialog.UserNameOptionChecked;
                            userNameText = dialog.UserNameOptionText;
                        }

                        SaveOptions(width, height, splitter, confirmed, setUserName, userNameText);
                        if (!confirmed) { return; }
                    }
                }
                if (folder == null) { return; }

                // The workspace may have been locked or closed while the popup was open:
                // check again, with the database read now, before touching anything.
                PwGroup target = group;
                if (!CanImport(out db, out group)) { return; }
                if (!ReferenceEquals(group, target)) { return; }
                if (db.RootGroup == null) { return; }
                if (!ReferenceEquals(group, db.RootGroup) && !group.IsContainedIn(db.RootGroup)) { return; }
                if (KeePassGroupImporter.IsInRecycleBin(group, db)) { return; }

                ImportPlan plan = ImportPlanner.Plan(folder, KeePassGroupImporter.Snapshot(group));

                ImportResult result = null;
                Exception failure = null;
                try { result = KeePassGroupImporter.Apply(plan, group, db, userName); }
                catch (Exception ex) { failure = ex; }

                // One refresh for the whole import; marks the database modified, never saves.
                m_host.MainWindow.UpdateUI(false, null, true, group, true, null, true);

                if (failure != null)
                {
                    MessageService.ShowWarning(ImportText.ImportFailed, failure.Message);
                    return;
                }

                MessageService.ShowInfo(ImportText.DialogTitle,
                    ImportText.Summary(result.EntriesAdded, plan.EntriesSkipped, result.GroupsCreated));
            }
            catch (Exception ex)
            {
                try { MessageService.ShowWarning(ImportText.ImportFailed, ex.Message); }
                catch (Exception) { }
            }
        }

        // The stored plugin options; the defaults when there are none or they cannot be read.
        private PluginParameters ReadOptions()
        {
            try
            {
                if (m_options != null)
                {
                    return PluginParameters.Parse(m_options.PluginOptionString);
                }
            }
            catch (Exception) { }
            return new PluginParameters();
        }

        // Remembers the popup layout always and the user-name option only when the
        // popup was confirmed. Re-reads the stored string so that only these keys
        // change. A failing save is ignored: it must never stop the import.
        private void SaveOptions(int width, int height, int splitter, bool confirmed, bool setUserName, string userNameText)
        {
            try
            {
                if (m_options == null) { return; }

                PluginParameters stored = PluginParameters.Parse(m_options.PluginOptionString);
                stored.importDialogWidth = width;
                stored.importDialogHeight = height;
                stored.importSplitter = splitter;
                if (confirmed)
                {
                    stored.importSetUserName = setUserName;
                    stored.importUserName = userNameText ?? "";
                }
                m_options.PluginOptionString = stored.ToString();
            }
            catch (Exception) { }
        }

        // Shows the popup owned by the main window and registered with KeePass's
        // window manager while it is open, so that KeePass can close it (Cancel)
        // when it locks the workspace.
        private DialogResult ShowRegistered(Form dialog)
        {
            WindowRegistration registration = new WindowRegistration(dialog);
            dialog.Load += registration.OnLoad;
            try
            {
                return dialog.ShowDialog(m_host.MainWindow);
            }
            finally
            {
                try { dialog.Load -= registration.OnLoad; }
                catch (Exception) { }
                registration.Unregister();
            }
        }

        private sealed class WindowRegistration : IGwmWindow
        {
            private readonly Form m_form;
            private bool m_added;

            public WindowRegistration(Form form)
            {
                m_form = form;
            }

            // The popup holds no data: KeePass may always close it.
            public bool CanCloseWithoutDataLoss { get { return true; } }

            // Like KeePass's own dialogs: registered when the form loads.
            public void OnLoad(object sender, EventArgs e)
            {
                try
                {
                    GlobalWindowManager.AddWindow(m_form, this);
                    m_added = true;
                }
                catch (Exception) { }
            }

            public void Unregister()
            {
                if (!m_added) { return; }
                m_added = false;
                try { GlobalWindowManager.RemoveWindow(m_form); }
                catch (Exception) { }
            }
        }
    }
}
