using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    public sealed class SystemPickedEventArgs : EventArgs
    {
        public SapSystem System;

        public SystemPickedEventArgs(SapSystem system)
        {
            System = system;
        }
    }

    /// <summary>
    /// Content of the SAP tab: Manual/Automatic choice, SAP Logon system list
    /// and one text box per SAP field. Contains no KeePass types.
    /// </summary>
    public sealed class SapEntryPanel : UserControl
    {
        private readonly RadioButton m_radioAutomatic;
        private readonly RadioButton m_radioManual;
        private readonly Label m_lblMessage;
        private readonly Label m_lblLaunchHint;
        private readonly TextBox m_tbSearch;
        private readonly ListBox m_lbSystems;
        private readonly Dictionary<string, TextBox> m_fields = new Dictionary<string, TextBox>();

        private IList<SapSystem> m_systems = new List<SapSystem>();
        private bool m_suppressEvents;

        public event EventHandler<SystemPickedEventArgs> SystemPicked;
        public event EventHandler FieldEdited;

        public SapEntryPanel()
        {
            SuspendLayout();

            m_radioAutomatic = new RadioButton();
            m_radioAutomatic.Text = "Automatic (from SAP Logon)";
            m_radioAutomatic.AutoSize = true;
            m_radioAutomatic.Checked = false;
            m_radioAutomatic.CheckedChanged += OnModeChanged;

            m_radioManual = new RadioButton();
            m_radioManual.Text = "Manual";
            m_radioManual.AutoSize = true;
            m_radioManual.Checked = true;

            FlowLayoutPanel radios = new FlowLayoutPanel();
            radios.AutoSize = true;
            radios.Dock = DockStyle.Top;
            radios.WrapContents = false;
            radios.Controls.Add(m_radioAutomatic);
            radios.Controls.Add(m_radioManual);

            m_lblMessage = new Label();
            m_lblMessage.AutoSize = true;
            m_lblMessage.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblMessage.Visible = false;
            m_lblMessage.ForeColor = SystemColors.GrayText;

            m_lblLaunchHint = new Label();
            m_lblLaunchHint.AutoSize = true;
            m_lblLaunchHint.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblLaunchHint.Margin = new Padding(3, 6, 3, 3);

            Label lblSystems = new Label();
            lblSystems.Text = "SAP Logon systems";
            lblSystems.AutoSize = true;
            lblSystems.Dock = DockStyle.Top;

            m_tbSearch = new TextBox();
            m_tbSearch.Dock = DockStyle.Top;
            m_tbSearch.TextChanged += OnSearchChanged;

            m_lbSystems = new ListBox();
            m_lbSystems.Dock = DockStyle.Fill;
            m_lbSystems.IntegralHeight = false;
            m_lbSystems.MinimumSize = new Size(0, 60);
            m_lbSystems.SelectedIndexChanged += OnSystemSelected;
            // Items stay SapSystem objects; only their text is formatted.
            m_lbSystems.FormattingEnabled = true;
            m_lbSystems.Format += OnFormatSystem;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowCount = 7;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(radios, 0, 0);
            root.Controls.Add(m_lblMessage, 0, 1);
            root.Controls.Add(lblSystems, 0, 2);
            root.Controls.Add(m_tbSearch, 0, 3);
            root.Controls.Add(m_lbSystems, 0, 4);
            root.Controls.Add(BuildFieldGrid(), 0, 5);
            root.Controls.Add(m_lblLaunchHint, 0, 6);

            Controls.Add(root);
            SizeChanged += OnPanelSizeChanged;
            Size = new Size(480, 420);
            UpdateLabelWidths();

            ResumeLayout(true);
            UpdateModeState();
            UpdateLaunchHint();
        }

        private TableLayoutPanel BuildFieldGrid()
        {
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.AutoSize = true;
            grid.ColumnCount = 6;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));

            // System ID / Client / Language
            AddField(grid, 0, 0, SapFields.SystemId, "System ID", 1);
            AddField(grid, 0, 2, SapFields.Client, "Client", 1);
            AddField(grid, 0, 4, SapFields.Language, "Language", 1);
            // Application server / System number
            AddField(grid, 1, 0, SapFields.ApplicationServer, "Application server", 1);
            AddField(grid, 1, 2, SapFields.SystemNumber, "System number", 1);
            // Message server / Port / Group
            AddField(grid, 2, 0, SapFields.MessageServer, "Message server", 1);
            AddField(grid, 2, 2, SapFields.MessageServerPort, "Port", 1);
            AddField(grid, 2, 4, SapFields.Group, "Group", 1);
            // SAProuter string
            AddField(grid, 3, 0, SapFields.SapRouterString, "SAProuter string", 5);
            // SNC name / SNC level
            AddField(grid, 4, 0, SapFields.SncName, "SNC name", 1);
            AddField(grid, 4, 2, SapFields.SncType, "SNC level", 1);
            // Transaction
            AddField(grid, 5, 0, SapFields.Transaction, "Transaction", 1);

            grid.RowCount = 6;
            for (int i = 0; i < 6; ++i)
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return grid;
        }

        private void AddField(TableLayoutPanel grid, int row, int column, string key, string caption, int span)
        {
            Label label = new Label();
            label.Text = caption;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(3, 6, 3, 3);

            TextBox box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.TextChanged += OnFieldTextChanged;
            m_fields[key] = box;

            grid.Controls.Add(label, column, row);
            grid.Controls.Add(box, column + 1, row);
            if (span > 1)
                grid.SetColumnSpan(box, span);
        }

        public ListBox SystemList { get { return m_lbSystems; } }

        public RadioButton AutomaticRadio { get { return m_radioAutomatic; } }

        public TextBox SearchBox { get { return m_tbSearch; } }

        public Label MessageLabel { get { return m_lblMessage; } }

        /// <summary>Status line at the bottom: what is still needed to launch the entry.</summary>
        public Label LaunchHintLabel { get { return m_lblLaunchHint; } }

        public TextBox Field(string key)
        {
            return m_fields[key];
        }

        public bool AutomaticMode
        {
            get { return m_radioAutomatic.Checked; }
        }

        public void SetSystems(IList<SapSystem> systems, string message)
        {
            m_systems = systems ?? new List<SapSystem>();

            bool hasMessage = !string.IsNullOrEmpty(message);
            m_lblMessage.Text = hasMessage ? message : "";
            m_lblMessage.Visible = hasMessage;
            m_radioAutomatic.Enabled = !hasMessage;
            if (hasMessage)
            {
                m_systems = new List<SapSystem>();
                m_radioManual.Checked = true;
            }

            RefreshList();
            UpdateModeState();
        }

        /// <summary>
        /// Shows a note in the message line without disabling Automatic
        /// (for example an unreadable shared landscape file). Empty hides it.
        /// </summary>
        public void SetWarning(string warning)
        {
            bool show = !string.IsNullOrEmpty(warning);
            m_lblMessage.Text = show ? warning : "";
            m_lblMessage.Visible = show;
        }

        public void LoadFields(IDictionary<string, string> values)
        {
            m_suppressEvents = true;
            try
            {
                foreach (string key in SapFields.All)
                {
                    string value;
                    if (values == null || !values.TryGetValue(key, out value) || value == null)
                        value = "";
                    m_fields[key].Text = value;
                }
            }
            finally
            {
                m_suppressEvents = false;
            }
            UpdateLaunchHint();
        }

        public Dictionary<string, string> ReadFields()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string key in SapFields.All)
                result[key] = m_fields[key].Text;
            return result;
        }

        private void RefreshList()
        {
            List<SapSystem> filtered = SystemFilter.Filter(m_systems, m_tbSearch.Text);

            bool previous = m_suppressEvents;
            m_suppressEvents = true;
            try
            {
                m_lbSystems.BeginUpdate();
                m_lbSystems.Items.Clear();
                foreach (SapSystem s in filtered)
                    m_lbSystems.Items.Add(s);
                m_lbSystems.EndUpdate();
            }
            finally
            {
                m_suppressEvents = previous;
            }
        }

        /// <summary>Refreshes the status line from the current field values.</summary>
        public void UpdateLaunchHint()
        {
            List<string> missing = SapShortcut.MissingForLaunch(ReadFields());
            m_lblLaunchHint.Text = missing.Count == 0
                ? "Ready to launch."
                : "Not launchable yet: enter " + string.Join(", ", missing.ToArray()) + ".";
        }

        // Long texts (paths) wrap instead of being cut off.
        private void UpdateLabelWidths()
        {
            int width = Math.Max(ClientSize.Width - 12, 50);
            m_lblMessage.MaximumSize = new Size(width, 0);
            m_lblLaunchHint.MaximumSize = new Size(width, 0);
        }

        private void OnPanelSizeChanged(object sender, EventArgs e)
        {
            UpdateLabelWidths();
        }

        private void OnFormatSystem(object sender, ListControlConvertEventArgs e)
        {
            SapSystem system = e.ListItem as SapSystem;
            if (system != null)
                e.Value = SystemDisplay.Format(system);
        }

        private void UpdateModeState()
        {
            bool auto = m_radioAutomatic.Checked;
            m_lbSystems.Enabled = auto;
            m_tbSearch.Enabled = auto;
        }

        private void ClearSelection()
        {
            bool previous = m_suppressEvents;
            m_suppressEvents = true;
            try
            {
                m_lbSystems.ClearSelected();
            }
            finally
            {
                m_suppressEvents = previous;
            }
        }

        private void OnModeChanged(object sender, EventArgs e)
        {
            ClearSelection();
            UpdateModeState();
        }

        private void OnSearchChanged(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void OnSystemSelected(object sender, EventArgs e)
        {
            if (m_suppressEvents || !AutomaticMode)
                return;

            SapSystem picked = m_lbSystems.SelectedItem as SapSystem;
            if (picked == null)
                return;

            EventHandler<SystemPickedEventArgs> handler = SystemPicked;
            if (handler != null)
                handler(this, new SystemPickedEventArgs(picked));
        }

        private void OnFieldTextChanged(object sender, EventArgs e)
        {
            if (m_suppressEvents)
                return;

            // The fields no longer match the highlighted system.
            ClearSelection();
            UpdateLaunchHint();

            EventHandler handler = FieldEdited;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
