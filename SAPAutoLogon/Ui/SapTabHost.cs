using System;
using System.Collections.Generic;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.UI;
using KeePassLib.Collections;
using KeePassLib.Security;

namespace SAPAutoLogon
{
    /// <summary>
    /// Adds the SAP tab to KeePass's Add/Edit Entry dialog and keeps it in sync
    /// with the dialog's working copy of the string fields (PwEntryForm.EntryStrings,
    /// which KeePass only writes to the entry on OK). Every failure is swallowed:
    /// the entry dialog must always open and work.
    /// </summary>
    public sealed class SapTabHost : IDisposable
    {
        private const string TabTitle = "SAP";
        private const string LoadingText = "Loading SAP Logon systems...";

        private readonly List<Binding> m_bindings = new List<Binding>();
        private bool m_subscribed;

        public SapTabHost()
        {
            GlobalWindowManager.WindowAdded += OnWindowAdded;
            m_subscribed = true;
        }

        public void Dispose()
        {
            try
            {
                if (m_subscribed)
                {
                    GlobalWindowManager.WindowAdded -= OnWindowAdded;
                    m_subscribed = false;
                }

                foreach (Binding b in m_bindings.ToArray())
                    b.Detach();
                m_bindings.Clear();
            }
            catch (Exception) { }
        }

        // Raised from PwEntryForm.OnFormLoad, before KeePass initialises its tabs;
        // the SAP tab is therefore added when the form is shown.
        private void OnWindowAdded(object sender, GwmWindowEventArgs e)
        {
            try
            {
                PwEntryForm form = e == null ? null : e.Form as PwEntryForm;
                if (form == null) return;
                foreach (Binding b in m_bindings)
                    if (b.Form == form) return;

                Binding binding = new Binding(this, form);
                m_bindings.Add(binding);
                form.Shown += binding.OnFormShown;
                form.FormClosed += binding.OnFormClosed;
            }
            catch (Exception) { }
        }

        private void Remove(Binding binding)
        {
            m_bindings.Remove(binding);
        }

        /// <summary>State and handlers for one entry dialog.</summary>
        private sealed class Binding
        {
            public readonly PwEntryForm Form;

            private readonly SapTabHost m_host;
            private TabControl m_tabMain;
            private TextBox m_tbTitle;
            private TextBox m_tbUserName;
            private TabPage m_page;
            private SapEntryPanel m_panel;
            private bool m_attached;
            private bool m_loadRequested;
            private bool m_listStale;
            private volatile bool m_closed;

            public Binding(SapTabHost host, PwEntryForm form)
            {
                m_host = host;
                Form = form;
            }

            public void OnFormShown(object sender, EventArgs e)
            {
                try
                {
                    Form.Shown -= OnFormShown;
                    Attach();
                }
                catch (Exception)
                {
                    SafeDetachHandlers();
                    SafeRemovePage();
                }
            }

            public void OnFormClosed(object sender, FormClosedEventArgs e)
            {
                Detach();
            }

            public void Detach()
            {
                m_closed = true;
                try
                {
                    Form.Shown -= OnFormShown;
                    Form.FormClosed -= OnFormClosed;
                }
                catch (Exception) { }
                SafeDetachHandlers();
                try { m_host.Remove(this); }
                catch (Exception) { }
            }

            private void SafeDetachHandlers()
            {
                try
                {
                    if (m_tabMain != null)
                    {
                        m_tabMain.Selected -= OnTabSelected;
                        m_tabMain.Deselected -= OnTabDeselected;
                    }
                    if (m_panel != null)
                    {
                        m_panel.SystemPicked -= OnSystemPicked;
                        m_panel.FieldEdited -= OnFieldEdited;
                    }
                }
                catch (Exception) { }
            }

            private void SafeRemovePage()
            {
                try
                {
                    if (m_tabMain != null && m_page != null && m_tabMain.TabPages.Contains(m_page))
                        m_tabMain.TabPages.Remove(m_page);
                }
                catch (Exception) { }
            }

            private void Attach()
            {
                if (m_attached) return;
                if (Form.IsDisposed) return;
                if (Form.EditModeEx == PwEditMode.ViewReadOnlyEntry) return;
                if (Form.EditModeEx == PwEditMode.Invalid) return;
                if (Form.MultipleValuesEntryContext != null) return;
                if (Form.EntryStrings == null) return;

                m_tabMain = FindControl<TabControl>("m_tabMain");
                m_tbTitle = FindControl<TextBox>("m_tbTitle");
                m_tbUserName = FindControl<TextBox>("m_tbUserName");
                if (m_tabMain == null || m_tbTitle == null || m_tbUserName == null) return;

                foreach (TabPage existing in m_tabMain.TabPages)
                    if (existing.Tag is SapEntryPanel) return;

                SapEntryPanel panel = new SapEntryPanel();
                panel.Dock = DockStyle.Fill;

                TabPage page = new TabPage(TabTitle);
                page.UseVisualStyleBackColor = true;
                page.Padding = new Padding(3);
                page.Tag = panel;
                page.Controls.Add(panel);

                m_panel = panel;
                m_page = page;
                LoadPanelFromEntry();

                m_tabMain.TabPages.Add(page);
                m_attached = true;

                panel.SystemPicked += OnSystemPicked;
                panel.FieldEdited += OnFieldEdited;
                m_tabMain.Selected += OnTabSelected;
                m_tabMain.Deselected += OnTabDeselected;

                if (m_tabMain.SelectedTab == page) OnSapTabSelected();
            }

            private T FindControl<T>(string name) where T : Control
            {
                Control[] found = Form.Controls.Find(name, true);
                foreach (Control c in found)
                {
                    T typed = c as T;
                    if (typed != null) return typed;
                }
                return null;
            }

            private void OnTabSelected(object sender, TabControlEventArgs e)
            {
                try
                {
                    if (e != null && e.TabPage == m_page) OnSapTabSelected();
                }
                catch (Exception) { }
            }

            // Leaving the SAP tab: show the edits made here on the Advanced tab.
            private void OnTabDeselected(object sender, TabControlEventArgs e)
            {
                try
                {
                    if (e != null && e.TabPage == m_page && m_listStale) RefreshKeePassView();
                }
                catch (Exception) { }
            }

            private void OnSapTabSelected()
            {
                if (!m_loadRequested)
                {
                    m_loadRequested = true;
                    m_panel.SetSystems(null, LoadingText);
                    LandscapeLoadResult cached = LandscapeSession.Cache.Request(OnLandscapeLoaded);
                    if (cached != null) ApplyLandscape(cached);
                }

                // Pick up edits made on the Advanced tab (last edit wins).
                LoadPanelFromEntry();
            }

            // Called on the worker thread (or, if the worker could not be started, on the
            // UI thread) when a load finishes. The dialog may have been closed meanwhile.
            private void OnLandscapeLoaded(LandscapeLoadResult result)
            {
                try
                {
                    if (m_closed) return;
                    PwEntryForm form = Form;
                    if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
                    form.BeginInvoke(new MethodInvoker(delegate { SafeApplyLandscape(result); }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (Exception) { }
            }

            // UI thread.
            private void SafeApplyLandscape(LandscapeLoadResult result)
            {
                try
                {
                    if (m_closed || Form.IsDisposed || m_panel == null || m_panel.IsDisposed) return;
                    ApplyLandscape(result);
                }
                catch (Exception) { }
            }

            private void ApplyLandscape(LandscapeLoadResult result)
            {
                if (result.Error.Length > 0)
                {
                    // No usable list: Automatic is disabled, Manual keeps working.
                    m_panel.SetSystems(result.Systems, result.Error);
                    return;
                }

                // Possibly partial list: Automatic stays usable, warnings are shown.
                m_panel.SetSystems(result.Systems, "");
                m_panel.SetWarning(string.Join(Environment.NewLine, result.Warnings.ToArray()));
            }

            private void LoadPanelFromEntry()
            {
                ProtectedStringDictionary strings = Form.EntryStrings;
                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (string key in SapFields.All)
                    values[key] = strings.ReadSafe(key);
                m_panel.LoadFields(values);
            }

            private void OnSystemPicked(object sender, SystemPickedEventArgs e)
            {
                try
                {
                    if (e == null || e.System == null) return;

                    // The picked object itself is used: systems are never looked up by name.
                    MappedEntry mapped = FieldMapper.Map(e.System);
                    // Automatic mode: a client or language SAP Logon does not store gets the default.
                    FieldMapper.ApplyDefaults(mapped.Fields);

                    Dictionary<string, string> merged = m_panel.ReadFields();
                    foreach (KeyValuePair<string, string> pair in mapped.Fields)
                        merged[pair.Key] = pair.Value;
                    m_panel.LoadFields(merged);

                    // Keep unsaved Title/User name/Password/URL/Notes edits: the refresh
                    // below writes the working copy back into those controls.
                    Form.UpdateEntryStrings(true, false);
                    WriteToEntry(mapped.Fields);
                    Form.UpdateEntryStrings(false, false, true);
                    m_listStale = false;

                    if (mapped.UserName != null)
                        m_tbUserName.Text = mapped.UserName;
                    if (Form.EditModeEx == PwEditMode.AddNewEntry)
                        m_tbTitle.Text = mapped.Title ?? "";
                }
                catch (Exception) { }
            }

            private void OnFieldEdited(object sender, EventArgs e)
            {
                try
                {
                    if (WriteToEntry(m_panel.ReadFields())) m_listStale = true;
                }
                catch (Exception) { }
            }

            private bool WriteToEntry(IDictionary<string, string> values)
            {
                ProtectedStringDictionary strings = Form.EntryStrings;
                return SapFieldWriter.Apply(values,
                    delegate(string key) { return strings.ReadSafe(key); },
                    delegate(string key, string value) { strings.Set(key, new ProtectedString(false, value)); },
                    delegate(string key) { strings.Remove(key); });
            }

            private void RefreshKeePassView()
            {
                Form.UpdateEntryStrings(true, false);
                Form.UpdateEntryStrings(false, false, true);
                m_listStale = false;
            }
        }
    }
}
