using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    /// <summary>
    /// The "Import from SAP Logon" popup: header band, folder tree, system list,
    /// Import and Cancel. The load runs in the background; the dialog is shown
    /// at once. Contains no KeePass types.
    /// </summary>
    public sealed class SapImportDialog : Form
    {
        private const int LogoSize = 32;
        private const int HeaderPadding = 12;

        private readonly string m_targetGroupName;
        private readonly SapImportPanel m_panel;
        private readonly Button m_btnImport;
        private readonly Button m_btnCancel;
        private readonly PictureBox m_picLogo;
        private readonly Label m_lblTitle;
        private readonly Label m_lblSubtitle;
        private readonly TableLayoutPanel m_header;

        // Fonts this dialog created; the images passed to SetImages belong to the caller.
        private Font m_dialogFont;
        private Font m_titleFont;

        private volatile bool m_closed;

        // The thread that called BeginLoad (the UI thread), and a result that
        // arrived on another thread before the dialog had a window handle.
        private readonly object m_lock = new object();
        private int m_uiThreadId = -1;
        private LandscapeLoadResult m_pending;
        private bool m_hasPending;

        /// <summary>The splitter position the dialog had when it was last in its normal state; 0 when not known.</summary>
        private int m_normalSplitter;

        public SapImportDialog(string targetGroupName)
            : this(targetGroupName, null)
        {
        }

        public SapImportDialog(string targetGroupName, GroupSnapshot target)
        {
            m_targetGroupName = targetGroupName ?? "";

            SuspendLayout();

            // Font and scaling first: every control added below inherits the
            // font, and the logical sizes are scaled from the 7x15 baseline.
            m_dialogFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
            Font = m_dialogFont;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;

            m_picLogo = new PictureBox();
            m_picLogo.Size = new Size(LogoSize, LogoSize);
            m_picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            m_picLogo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            m_picLogo.Margin = new Padding(0, 0, 9, 0);
            m_picLogo.BackColor = SystemColors.Window;
            m_picLogo.Visible = false;

            m_titleFont = new Font(Font.FontFamily, Font.Size + 3F, FontStyle.Bold);
            m_lblTitle = new Label();
            m_lblTitle.Text = ImportText.DialogTitle;
            m_lblTitle.AutoSize = true;
            m_lblTitle.Font = m_titleFont;
            m_lblTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblTitle.Margin = new Padding(0, 0, 0, 3);

            m_lblSubtitle = new Label();
            m_lblSubtitle.Text = ImportText.HeaderSubtitle(m_targetGroupName);
            m_lblSubtitle.AutoSize = true;
            m_lblSubtitle.ForeColor = SystemColors.GrayText;
            m_lblSubtitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblSubtitle.Margin = new Padding(0);

            TableLayoutPanel header = new TableLayoutPanel();
            m_header = header;
            header.Dock = DockStyle.Top;
            header.AutoSize = true;
            header.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            header.BackColor = SystemColors.Window;
            header.Padding = new Padding(HeaderPadding);
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.RowCount = 2;
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.Controls.Add(m_picLogo, 0, 0);
            header.SetRowSpan(m_picLogo, 2);
            header.Controls.Add(m_lblTitle, 1, 0);
            header.Controls.Add(m_lblSubtitle, 1, 1);
            header.TabStop = false;

            // The 1 px line that closes the band.
            Panel headerLine = new Panel();
            headerLine.Dock = DockStyle.Top;
            headerLine.Height = 1;
            headerLine.BackColor = SystemColors.ControlDark;
            headerLine.TabStop = false;

            m_panel = new SapImportPanel();
            m_panel.Dock = DockStyle.Fill;
            m_panel.TabIndex = 0;
            m_panel.SelectionChanged += OnPanelSelectionChanged;

            m_btnImport = new Button();
            m_btnImport.Text = ImportText.ImportButton;
            m_btnImport.DialogResult = DialogResult.OK;
            m_btnImport.AutoSize = true;
            m_btnImport.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_btnImport.Enabled = false;
            m_btnImport.TabIndex = 0;

            m_btnCancel = new Button();
            m_btnCancel.Text = ImportText.CancelButton;
            m_btnCancel.DialogResult = DialogResult.Cancel;
            m_btnCancel.AutoSize = true;
            m_btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_btnCancel.TabIndex = 1;

            // Right to left: the first control sits at the right edge.
            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.TabIndex = 1;
            buttons.Controls.Add(m_btnCancel);
            buttons.Controls.Add(m_btnImport);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(9);
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(m_panel, 0, 0);
            root.Controls.Add(buttons, 0, 1);
            root.TabIndex = 0;

            Text = ImportText.DialogTitle;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MinimumSize = new Size(700, 520);
            ClientSize = new Size(780, 560);
            AcceptButton = m_btnImport;
            CancelButton = m_btnCancel;
            // Docking runs from the last added control: the band sits on top,
            // its line below it, and the content fills the rest.
            Controls.Add(root);
            Controls.Add(headerLine);
            Controls.Add(header);
            FormClosed += OnDialogClosed;
            header.SizeChanged += OnHeaderSizeChanged;

            m_panel.TargetSnapshot = target;
            UpdateSubtitleWidth();

            ResumeLayout(true);
            // Again after the autoscale pass, which changes the band's width.
            UpdateSubtitleWidth();
            UpdateImportButton();
        }

        public PictureBox HeaderLogo { get { return m_picLogo; } }

        public Label HeaderTitleLabel { get { return m_lblTitle; } }

        public Label HeaderSubtitleLabel { get { return m_lblSubtitle; } }

        /// <summary>True when "Set user name for imported entries" is checked.</summary>
        public bool UserNameOptionChecked { get { return m_panel.UserNameCheckBox.Checked; } }

        /// <summary>The user-name text box text, trimmed; "" when blank.</summary>
        public string UserNameOptionText
        {
            get
            {
                string text = m_panel.UserNameTextBox.Text;
                return text == null ? "" : text.Trim();
            }
        }

        /// <summary>The user name for created entries without one, or null.</summary>
        public string DefaultUserName { get { return m_panel.DefaultUserName; } }

        /// <summary>
        /// Shows the logo in the header band and the icon in front of every
        /// system row; null shows none. The dialog never disposes either image:
        /// the caller owns them and disposes them after the dialog.
        /// </summary>
        public void SetImages(Image headerLogo, Image rowIcon)
        {
            try
            {
                m_picLogo.Image = headerLogo;
                m_picLogo.Visible = headerLogo != null;
                UpdateSubtitleWidth();
            }
            catch (Exception)
            {
                // The logo is decoration only.
            }

            try
            {
                m_panel.SetRowImage(rowIcon);
            }
            catch (Exception)
            {
                // The row icon is decoration only.
            }
        }

        public void SetUserNameOption(bool enabled, string text)
        {
            try
            {
                m_panel.SetUserNameOption(enabled, text);
            }
            catch (Exception)
            {
                // The option keeps its defaults.
            }
        }

        /// <summary>
        /// Applies a remembered window size (clamped to the minimum size and the
        /// primary screen's working area) and then the splitter position.
        /// Values up to 0 are ignored. Never throws.
        /// </summary>
        public void RestoreLayout(int width, int height, int splitterDistance)
        {
            try
            {
                // Size first: the split keeps its ratio when its width changes,
                // so a splitter placed before the resize would move again.
                if (width > 0 && height > 0)
                {
                    Size = ClampSize(width, height);
                    PerformLayout();
                }

                if (splitterDistance > 0)
                {
                    m_panel.SetSplitterDistance(splitterDistance);
                    if (WindowState == FormWindowState.Normal)
                        m_normalSplitter = m_panel.SplitterDistance;
                }
            }
            catch (Exception)
            {
                // A remembered layout is a convenience only.
            }
        }

        /// <summary>
        /// The window size (the restored one when maximized) and the splitter position.
        /// When the window is not in its normal state the splitter is the one it had
        /// when it was last normal (0 when not known), because the maximised layout
        /// would make the tree too wide when the normal size is restored.
        /// </summary>
        public void CaptureLayout(out int width, out int height, out int splitterDistance)
        {
            width = 0;
            height = 0;
            splitterDistance = 0;
            try
            {
                Size size = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
                width = size.Width;
                height = size.Height;
                if (WindowState == FormWindowState.Normal)
                    m_normalSplitter = m_panel.SplitterDistance;
                splitterDistance = m_normalSplitter;
            }
            catch (Exception)
            {
                // Nothing is remembered then.
            }
        }

        /// <summary>
        /// The dialog's keyboard shortcuts. Ctrl+F goes to the search box; Esc in
        /// a non-empty search box clears the search (otherwise Esc cancels the
        /// dialog as usual); Enter in the search box moves to the tree.
        /// Returns true when the key was handled.
        /// </summary>
        public bool HandleShortcut(Keys keyData, bool searchBoxFocused)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                m_panel.FocusSearch();
                return true;
            }

            // Shift is allowed (Shift+Esc / Shift+Enter would otherwise slip past
            // and cancel or import); Ctrl and Alt combinations are left alone.
            Keys key = keyData & Keys.KeyCode;
            bool noCtrlAlt = (keyData & (Keys.Control | Keys.Alt)) == Keys.None;

            if (key == Keys.Escape && noCtrlAlt && searchBoxFocused && m_panel.SearchBox.TextLength > 0)
            {
                m_panel.ClearSearch();
                return true;
            }

            // Enter in the search box only confirms the search text: the folder
            // to import is chosen in the tree, and the filter may have just
            // changed what is selected. Letting it reach the AcceptButton would
            // start an import the user did not ask for, so it is swallowed here
            // (ProcessCmdKey runs before the form's AcceptButton handling, which
            // fires for Return with or without Shift).
            // Keys.Return is the same value as Keys.Enter.
            if (key == Keys.Enter && noCtrlAlt && searchBoxFocused)
            {
                m_panel.FocusContent();
                return true;
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool handled = false;
            try
            {
                handled = HandleShortcut(keyData, m_panel.SearchBox.Focused);
            }
            catch (Exception)
            {
                handled = false;
            }

            if (handled)
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    // The logo belongs to the caller: detach it, never dispose it.
                    m_picLogo.Image = null;
                    if (m_titleFont != null)
                        m_titleFont.Dispose();
                    if (m_dialogFont != null)
                        m_dialogFont.Dispose();
                    m_titleFont = null;
                    m_dialogFont = null;
                }
            }
        }

        private Size ClampSize(int width, int height)
        {
            Size min = MinimumSize;
            int w = Math.Max(width, min.Width);
            int h = Math.Max(height, min.Height);

            Screen screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                Size area = screen.WorkingArea.Size;
                w = Math.Min(w, Math.Max(area.Width, min.Width));
                h = Math.Min(h, Math.Max(area.Height, min.Height));
            }
            return new Size(w, h);
        }

        // The subtitle wraps to the band's width (scaled values: the band's
        // client width, its padding and the logo column).
        private void UpdateSubtitleWidth()
        {
            try
            {
                int logo = m_picLogo.Image != null ? m_picLogo.Width + m_picLogo.Margin.Horizontal : 0;
                int width = m_header.ClientSize.Width - m_header.Padding.Horizontal - logo
                    - m_lblSubtitle.Margin.Horizontal;
                Size max = new Size(Math.Max(width, 100), 0);
                if (m_lblSubtitle.MaximumSize != max)
                    m_lblSubtitle.MaximumSize = max;
            }
            catch (Exception)
            {
                // Wrapping is cosmetic only.
            }
        }

        private void OnHeaderSizeChanged(object sender, EventArgs e)
        {
            UpdateSubtitleWidth();
        }

        public SapImportPanel Panel { get { return m_panel; } }

        public Button ImportButton { get { return m_btnImport; } }

        public Button CancelButtonEx { get { return m_btnCancel; } }

        public SapFolder SelectedFolder { get { return m_panel.SelectedFolder; } }

        /// <summary>
        /// Shows the loading state and fills the dialog as soon as the cache has
        /// the landscape: at once when it is cached, otherwise when it arrives.
        /// </summary>
        public void BeginLoad(LandscapeLoadCache cache)
        {
            m_uiThreadId = Thread.CurrentThread.ManagedThreadId;
            m_panel.ShowLoading();
            UpdateImportButton();
            m_panel.FocusContent();

            if (cache == null)
            {
                Apply(null);
                return;
            }

            LandscapeLoadResult cached;
            try
            {
                cached = cache.Request(OnLoaded);
            }
            catch (Exception)
            {
                Apply(null);
                return;
            }

            if (cached != null)
                Apply(cached);
        }

        // May run on a worker thread.
        private void OnLoaded(LandscapeLoadResult result)
        {
            try
            {
                if (m_closed || IsDisposed)
                    return;

                if (Thread.CurrentThread.ManagedThreadId == m_uiThreadId)
                {
                    Apply(result);
                    return;
                }

                if (IsHandleCreated)
                {
                    BeginInvoke(new Action<LandscapeLoadResult>(Apply), result);
                    return;
                }

                // Worker thread and no handle yet: the controls must not be
                // touched from here. The UI thread applies it once the handle exists.
                lock (m_lock)
                {
                    m_pending = result;
                    m_hasPending = true;
                }

                // The handle may have appeared while the result was being parked.
                if (IsHandleCreated)
                    BeginInvoke(new Action(ApplyPending));
            }
            catch (Exception)
            {
                // The dialog may be gone already; there is nobody to tell.
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyPending();
        }

        // Runs on the UI thread.
        private void ApplyPending()
        {
            LandscapeLoadResult result;
            bool has;
            lock (m_lock)
            {
                has = m_hasPending;
                result = m_pending;
                m_hasPending = false;
                m_pending = null;
            }

            if (has)
                Apply(result);
        }

        private void Apply(LandscapeLoadResult result)
        {
            if (m_closed || IsDisposed)
                return;

            try
            {
                m_panel.SetLandscape(result, m_targetGroupName);
            }
            catch (Exception)
            {
                // Never stay on "Loading...": show the failure state instead.
                try
                {
                    m_panel.SetLandscape(null, m_targetGroupName);
                }
                catch (Exception)
                {
                    // Cancel still works.
                }
            }
            UpdateImportButton();
            m_panel.FocusContent();
        }

        private void UpdateImportButton()
        {
            m_btnImport.Enabled = m_panel.CanImport;
        }

        private void OnPanelSelectionChanged(object sender, EventArgs e)
        {
            UpdateImportButton();
        }

        private void OnDialogClosed(object sender, FormClosedEventArgs e)
        {
            m_closed = true;
        }
    }
}
