using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    /// <summary>
    /// Content of the import popup: a search row, the SAP Logon folder tree on
    /// the left, the systems of the selected folder on the right, a message
    /// line, the default user name option, the confirmation question and a
    /// preview of what the import creates. Contains no KeePass types.
    /// </summary>
    public sealed class SapImportPanel : UserControl
    {
        private const int IconSize = 16;
        private const int ClosedFolderImage = 0;
        private const int OpenFolderImage = 1;

        private readonly Label m_lblSearch;
        private readonly TextBox m_txtSearch;
        private readonly Button m_btnClearSearch;
        private readonly SplitContainer m_split;
        private readonly FolderTreeView m_tree;
        private readonly Label m_lblListCaption;
        private readonly ListView m_list;
        private readonly Label m_lblMessage;
        private readonly CheckBox m_chkUserName;
        private readonly TextBox m_txtUserName;
        private readonly Label m_lblUserNameHint;
        private readonly Label m_lblConfirmation;
        private readonly Label m_lblPreview;
        private readonly ColumnHeader m_colName;
        private readonly ColumnHeader m_colSid;
        private readonly ColumnHeader m_colServer;

        private readonly ImageList m_folderImages;
        private readonly Bitmap m_closedFolder;
        private readonly Bitmap m_openFolder;
        private ImageList m_rowImages;
        private Bitmap m_rowImage;
        private Font m_boldFont;

        // The folders of the last SetLandscape (null while loading or after an
        // error) and the filter result each tree node shows.
        private List<SapFolder> m_roots;
        private List<FolderMatch> m_matches = new List<FolderMatch>();
        private bool m_filterActive;
        private readonly Dictionary<TreeNode, FolderMatch> m_nodeMatches = new Dictionary<TreeNode, FolderMatch>();
        private string m_warnings = "";

        private GroupSnapshot m_targetSnapshot;
        private SapFolder m_planFolder;
        private ImportPlan m_plan;

        private string m_targetGroupName = "";
        private bool m_hasSystems;
        private bool m_suppressEvents;
        private bool m_settingUserNameOption;
        private bool m_splitPlaced;
        private int m_splitRequest;

        public event EventHandler SelectionChanged;

        public SapImportPanel()
        {
            SuspendLayout();

            m_closedFolder = DrawFolder(false);
            m_openFolder = DrawFolder(true);
            m_folderImages = new ImageList();
            m_folderImages.ColorDepth = ColorDepth.Depth32Bit;
            m_folderImages.ImageSize = new Size(IconSize, IconSize);
            m_folderImages.Images.Add(m_closedFolder);
            m_folderImages.Images.Add(m_openFolder);

            m_lblSearch = new Label();
            m_lblSearch.Text = ImportText.SearchLabel;
            m_lblSearch.AutoSize = true;
            m_lblSearch.Anchor = AnchorStyles.Left;

            m_txtSearch = new TextBox();
            m_txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            m_txtSearch.TextChanged += OnSearchTextChanged;

            m_btnClearSearch = new Button();
            m_btnClearSearch.Text = ImportText.ClearSearchButton;
            m_btnClearSearch.AutoSize = true;
            m_btnClearSearch.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_btnClearSearch.Anchor = AnchorStyles.Left;
            m_btnClearSearch.Enabled = false;
            m_btnClearSearch.Click += OnClearSearchClick;

            TableLayoutPanel searchRow = new TableLayoutPanel();
            searchRow.Dock = DockStyle.Fill;
            searchRow.AutoSize = true;
            searchRow.Margin = new Padding(0);
            searchRow.ColumnCount = 3;
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchRow.RowCount = 1;
            searchRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            searchRow.Controls.Add(m_lblSearch, 0, 0);
            searchRow.Controls.Add(m_txtSearch, 1, 0);
            searchRow.Controls.Add(m_btnClearSearch, 2, 0);

            m_tree = new FolderTreeView();
            m_tree.Dock = DockStyle.Fill;
            m_tree.HideSelection = false;
            m_tree.ImageList = m_folderImages;
            m_tree.ImageIndex = ClosedFolderImage;
            m_tree.SelectedImageIndex = OpenFolderImage;
            m_tree.BeforeSelect += OnTreeBeforeSelect;
            m_tree.AfterSelect += OnTreeAfterSelect;
            m_tree.SelectionDroppedOnFocus += OnTreeSelectionDropped;

            m_colName = new ColumnHeader();
            m_colName.Text = ImportText.ColumnName;
            m_colSid = new ColumnHeader();
            m_colSid.Text = ImportText.ColumnSid;
            m_colServer = new ColumnHeader();
            m_colServer.Text = ImportText.ColumnServer;

            m_list = new ListView();
            m_list.Dock = DockStyle.Fill;
            m_list.View = View.Details;
            m_list.FullRowSelect = true;
            m_list.MultiSelect = false;
            m_list.HideSelection = false;
            m_list.Columns.AddRange(new ColumnHeader[] { m_colName, m_colSid, m_colServer });
            m_list.SizeChanged += OnListSizeChanged;

            m_lblListCaption = new Label();
            m_lblListCaption.AutoSize = true;
            m_lblListCaption.Dock = DockStyle.Top;
            m_lblListCaption.Padding = new Padding(0, 0, 0, 3);
            m_lblListCaption.Text = ImportText.ListCaptionNoSelection;

            m_split = new SplitContainer();
            m_split.Dock = DockStyle.Fill;
            m_split.Panel1MinSize = 80;
            m_split.Panel2MinSize = 120;
            m_split.SizeChanged += OnSplitSizeChanged;
            m_split.Panel1.Controls.Add(m_tree);
            // The list fills what the caption docked on top leaves free.
            m_split.Panel2.Controls.Add(m_list);
            m_split.Panel2.Controls.Add(m_lblListCaption);
            m_split.Panel2.SizeChanged += OnListPanelSizeChanged;

            m_lblMessage = new Label();
            m_lblMessage.AutoSize = true;
            m_lblMessage.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblMessage.ForeColor = SystemColors.GrayText;
            m_lblMessage.Visible = false;

            m_chkUserName = new CheckBox();
            m_chkUserName.Text = ImportText.UserNameCheck;
            m_chkUserName.AutoSize = true;
            m_chkUserName.Anchor = AnchorStyles.Left;
            m_chkUserName.CheckedChanged += OnUserNameCheckedChanged;

            m_txtUserName = new TextBox();
            m_txtUserName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            m_txtUserName.UseSystemPasswordChar = false;
            m_txtUserName.Enabled = false;
            m_txtUserName.TextChanged += OnUserNameTextChanged;

            TableLayoutPanel userRow = new TableLayoutPanel();
            userRow.Dock = DockStyle.Fill;
            userRow.AutoSize = true;
            userRow.Margin = new Padding(0, 3, 0, 0);
            userRow.ColumnCount = 2;
            userRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            userRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            userRow.RowCount = 1;
            userRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            userRow.Controls.Add(m_chkUserName, 0, 0);
            userRow.Controls.Add(m_txtUserName, 1, 0);

            m_lblUserNameHint = new Label();
            m_lblUserNameHint.AutoSize = true;
            m_lblUserNameHint.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblUserNameHint.ForeColor = SystemColors.GrayText;
            m_lblUserNameHint.Margin = new Padding(20, 0, 3, 3);
            m_lblUserNameHint.Text = ImportText.UserNameBlankHint;
            m_lblUserNameHint.Visible = false;

            m_lblConfirmation = new Label();
            m_lblConfirmation.AutoSize = true;
            m_lblConfirmation.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblConfirmation.Margin = new Padding(3, 6, 3, 3);
            m_boldFont = new Font(Font, FontStyle.Bold);
            m_lblConfirmation.Font = m_boldFont;

            m_lblPreview = new Label();
            m_lblPreview.AutoSize = true;
            m_lblPreview.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            m_lblPreview.ForeColor = SystemColors.GrayText;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowCount = 7;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(searchRow, 0, 0);
            root.Controls.Add(m_split, 0, 1);
            root.Controls.Add(m_lblMessage, 0, 2);
            root.Controls.Add(userRow, 0, 3);
            root.Controls.Add(m_lblUserNameHint, 0, 4);
            root.Controls.Add(m_lblConfirmation, 0, 5);
            root.Controls.Add(m_lblPreview, 0, 6);

            Controls.Add(root);
            SizeChanged += OnPanelSizeChanged;
            Size = new Size(640, 340);
            UpdateLabelWidths();
            UpdateCaptionWidth();
            UpdateColumnWidths();

            ResumeLayout(true);
            SetTreeEnabled(false);
        }

        public TreeView FolderTree { get { return m_tree; } }

        public ListView SystemList { get { return m_list; } }

        public Label MessageLabel { get { return m_lblMessage; } }

        public Label ConfirmationLabel { get { return m_lblConfirmation; } }

        public TextBox SearchBox { get { return m_txtSearch; } }

        public Button ClearSearchButton { get { return m_btnClearSearch; } }

        public Label ListCaptionLabel { get { return m_lblListCaption; } }

        public Label PreviewLabel { get { return m_lblPreview; } }

        public CheckBox UserNameCheckBox { get { return m_chkUserName; } }

        public TextBox UserNameTextBox { get { return m_txtUserName; } }

        public Label UserNameHintLabel { get { return m_lblUserNameHint; } }

        public SplitContainer Splitter { get { return m_split; } }

        public int SplitterDistance { get { return m_split.SplitterDistance; } }

        /// <summary>The folder chosen in the tree, or null.</summary>
        public SapFolder SelectedFolder
        {
            get
            {
                TreeNode node = m_tree.SelectedNode;
                return node == null ? null : node.Tag as SapFolder;
            }
        }

        /// <summary>True when systems were loaded and a folder is selected.</summary>
        public bool CanImport
        {
            get { return m_hasSystems && SelectedFolder != null; }
        }

        /// <summary>The target group as it is now, for the preview; null plans against an empty group.</summary>
        public GroupSnapshot TargetSnapshot
        {
            get { return m_targetSnapshot; }
            set
            {
                m_targetSnapshot = value;
                m_plan = null;
                m_planFolder = null;
                UpdatePreview();
            }
        }

        /// <summary>The user name for created entries without one, or null when none is set.</summary>
        public string DefaultUserName
        {
            get
            {
                return m_chkUserName.Checked ? FieldMapper.NormalizeDefaultUserName(m_txtUserName.Text) : null;
            }
        }

        public void SetUserNameOption(bool enabled, string text)
        {
            m_settingUserNameOption = true;
            try
            {
                m_chkUserName.Checked = enabled;
                m_txtUserName.Text = text ?? "";
            }
            finally
            {
                m_settingUserNameOption = false;
            }
            UpdateUserNameControls();
            UpdatePreview();
        }

        /// <summary>
        /// Shows the image in front of every system row; null shows none. The
        /// panel keeps its own copy, the caller still owns the argument.
        /// </summary>
        public void SetRowImage(Image image)
        {
            ImageList oldList = m_rowImages;
            Bitmap oldImage = m_rowImage;
            m_rowImages = null;
            m_rowImage = null;

            if (image != null)
            {
                m_rowImage = new Bitmap(image, IconSize, IconSize);
                m_rowImages = new ImageList();
                m_rowImages.ColorDepth = ColorDepth.Depth32Bit;
                m_rowImages.ImageSize = new Size(IconSize, IconSize);
                m_rowImages.Images.Add(m_rowImage);
            }

            m_list.SmallImageList = m_rowImages;
            foreach (ListViewItem item in m_list.Items)
                item.ImageIndex = m_rowImages == null ? -1 : 0;

            if (oldList != null)
                oldList.Dispose();
            if (oldImage != null)
                oldImage.Dispose();
        }

        /// <summary>
        /// Places the splitter at the distance (clamped to the pane limits)
        /// instead of the one-third default; at once when the split has a
        /// usable width, otherwise as soon as it gets one. Values up to 0 are ignored.
        /// </summary>
        public void SetSplitterDistance(int distance)
        {
            if (distance <= 0)
                return;

            m_splitRequest = distance;
            if (SplitHasUsableWidth())
            {
                m_splitPlaced = true;
                ApplySplitterDistance(distance);
            }
        }

        public void FocusSearch()
        {
            try
            {
                m_txtSearch.SelectAll();
                m_txtSearch.Focus();
            }
            catch (Exception)
            {
                // Focus is a convenience only.
            }
        }

        public void ClearSearch()
        {
            m_txtSearch.Text = "";
        }

        public void ShowLoading()
        {
            SapFolder before = SelectedFolder;
            try
            {
                ShowLoadingCore();
            }
            finally
            {
                NotifyIfSelectionChanged(before);
            }
        }

        public void SetLandscape(LandscapeLoadResult result, string targetGroupName)
        {
            SapFolder before = SelectedFolder;
            try
            {
                SetLandscapeCore(result, targetGroupName);
            }
            finally
            {
                NotifyIfSelectionChanged(before);
            }
        }

        /// <summary>
        /// Puts the focus where Enter does no harm: on the tree when it is usable,
        /// otherwise on the splitter (never on a button).
        /// </summary>
        public void FocusContent()
        {
            try
            {
                if (m_tree.Enabled)
                    m_tree.Select();
                else
                    m_split.Select();
            }
            catch (Exception)
            {
                // Focus is a convenience only.
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            try
            {
                if (m_lblConfirmation == null || IsDisposed)
                    return;
                Font old = m_boldFont;
                m_boldFont = new Font(Font, FontStyle.Bold);
                m_lblConfirmation.Font = m_boldFont;
                if (old != null)
                    old.Dispose();
            }
            catch (Exception)
            {
                // The bold font is decoration only.
            }
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
                    m_folderImages.Dispose();
                    m_closedFolder.Dispose();
                    m_openFolder.Dispose();
                    if (m_rowImages != null)
                        m_rowImages.Dispose();
                    if (m_rowImage != null)
                        m_rowImage.Dispose();
                    if (m_boldFont != null)
                        m_boldFont.Dispose();
                    m_rowImages = null;
                    m_rowImage = null;
                    m_boldFont = null;
                }
            }
        }

        private void ShowLoadingCore()
        {
            m_hasSystems = false;
            m_roots = null;
            m_warnings = "";
            ClearTree();
            SetTreeEnabled(false);
            SetMessage(ImportText.LoadingText);
            UpdateSelectionView();
        }

        private void SetLandscapeCore(LandscapeLoadResult result, string targetGroupName)
        {
            m_targetGroupName = targetGroupName ?? "";
            m_hasSystems = false;
            m_roots = null;
            m_warnings = "";
            m_plan = null;
            m_planFolder = null;
            ClearTree();

            if (result == null)
            {
                SetTreeEnabled(false);
                SetMessage(ImportText.NoSystems);
                UpdateSelectionView();
                return;
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                SetTreeEnabled(false);
                SetMessage(result.Error);
                UpdateSelectionView();
                return;
            }

            int systems = 0;
            m_roots = new List<SapFolder>();
            if (result.Folders != null)
            {
                foreach (SapFolder folder in result.Folders)
                {
                    if (folder == null)
                        continue;
                    m_roots.Add(folder);
                    systems += folder.CountSystems();
                }
            }

            // Folders without a single system are nothing to import.
            m_hasSystems = systems > 0;
            m_warnings = result.Warnings == null || result.Warnings.Count == 0
                ? ""
                : string.Join(Environment.NewLine, result.Warnings.ToArray());

            // A new landscape starts without a selection.
            BuildTree(FolderFilter.Apply(m_roots, m_txtSearch.Text), FolderFilter.IsActive(m_txtSearch.Text), null);
            SetTreeEnabled(m_hasSystems);
            UpdateMessage();
            UpdateSelectionView();
        }

        // Applies the search text to the loaded folders, keeping the selected
        // folder when the filtered tree still shows it.
        private void ApplySearch()
        {
            m_btnClearSearch.Enabled = m_txtSearch.TextLength > 0;
            if (m_roots == null)
                return;

            string text = m_txtSearch.Text;
            bool active = FolderFilter.IsActive(text);
            List<FolderMatch> matches = FolderFilter.Apply(m_roots, text);

            if (active == m_filterActive && SameFolders(matches, m_matches))
            {
                // Same folders: only what the list shows changes.
                m_matches = matches;
                m_nodeMatches.Clear();
                MapNodes(m_tree.Nodes, matches);
            }
            else
            {
                BuildTree(matches, active, SelectedFolder);
            }

            UpdateMessage();
            UpdateSelectionView();
        }

        private void BuildTree(List<FolderMatch> matches, bool active, SapFolder select)
        {
            bool previous = m_suppressEvents;
            m_suppressEvents = true;
            try
            {
                m_tree.BeginUpdate();
                try
                {
                    m_tree.Nodes.Clear();
                    m_nodeMatches.Clear();
                    TreeNode selectNode = null;
                    foreach (FolderMatch match in matches)
                    {
                        TreeNode node = AddNode(m_tree.Nodes, match, select, ref selectNode);
                        if (!active)
                            node.Expand();
                    }
                    if (active)
                        m_tree.ExpandAll();
                    m_tree.SelectedNode = selectNode;
                }
                finally
                {
                    m_tree.EndUpdate();
                }
            }
            finally
            {
                m_suppressEvents = previous;
            }
            m_matches = matches;
            m_filterActive = active;
        }

        private TreeNode AddNode(TreeNodeCollection parent, FolderMatch match, SapFolder select, ref TreeNode selectNode)
        {
            SapFolder folder = match.Folder;
            TreeNode node = new TreeNode(ImportText.FolderLabel(folder.Name, folder.CountSystems()));
            node.Tag = folder;
            node.ImageIndex = ClosedFolderImage;
            node.SelectedImageIndex = OpenFolderImage;
            parent.Add(node);
            m_nodeMatches[node] = match;
            if (select != null && selectNode == null && ReferenceEquals(folder, select))
                selectNode = node;
            foreach (FolderMatch child in match.Folders)
                AddNode(node.Nodes, child, select, ref selectNode);
            return node;
        }

        private void MapNodes(TreeNodeCollection nodes, List<FolderMatch> matches)
        {
            for (int i = 0; i < matches.Count && i < nodes.Count; i++)
            {
                m_nodeMatches[nodes[i]] = matches[i];
                MapNodes(nodes[i].Nodes, matches[i].Folders);
            }
        }

        // True when both filter results show the same folders in the same places.
        private static bool SameFolders(List<FolderMatch> a, List<FolderMatch> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i].Folder, b[i].Folder) || !SameFolders(a[i].Folders, b[i].Folders))
                    return false;
            }
            return true;
        }

        private void ClearTree()
        {
            bool previous = m_suppressEvents;
            m_suppressEvents = true;
            try
            {
                m_tree.BeginUpdate();
                try
                {
                    m_tree.Nodes.Clear();
                }
                finally
                {
                    m_tree.EndUpdate();
                }
                m_nodeMatches.Clear();
                m_matches = new List<FolderMatch>();
                m_filterActive = false;
                m_list.Items.Clear();
            }
            finally
            {
                m_suppressEvents = previous;
            }
        }

        private void SetTreeEnabled(bool enabled)
        {
            m_tree.Enabled = enabled;
            m_list.Enabled = enabled;
        }

        private void SetMessage(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            m_lblMessage.Text = show ? text : "";
            m_lblMessage.Visible = show;
        }

        // The message line of a loaded landscape: warnings, preceded by the
        // reason when there is nothing to show.
        private void UpdateMessage()
        {
            string reason = null;
            if (!m_hasSystems)
                reason = ImportText.NoSystems;
            else if (m_filterActive && m_tree.Nodes.Count == 0)
                reason = ImportText.NoSearchMatch;

            if (reason == null)
                SetMessage(m_warnings);
            else
                SetMessage(m_warnings.Length == 0 ? reason : reason + Environment.NewLine + m_warnings);
        }

        // Fills the list, its caption, the confirmation and the preview from the
        // current tree selection.
        private void UpdateSelectionView()
        {
            TreeNode node = m_tree.SelectedNode;
            SapFolder folder = node == null ? null : node.Tag as SapFolder;

            m_list.BeginUpdate();
            try
            {
                m_list.Items.Clear();
                if (folder == null || !m_hasSystems)
                {
                    m_lblListCaption.Text = ImportText.ListCaptionNoSelection;
                    m_lblConfirmation.Text = m_hasSystems ? ImportText.SelectFolder : "";
                    return;
                }

                FolderMatch match;
                List<SapSystem> shown = m_nodeMatches.TryGetValue(node, out match) && match != null
                    ? match.Systems
                    : folder.Systems;
                int count = 0;
                foreach (SapSystem system in shown)
                {
                    if (system == null)
                        continue;
                    ListViewItem item = new ListViewItem(system.Name ?? "");
                    item.SubItems.Add(system.SystemId ?? "");
                    item.SubItems.Add(ImportText.ServerColumn(system));
                    item.Tag = system;
                    if (m_rowImages != null)
                        item.ImageIndex = 0;
                    m_list.Items.Add(item);
                    count++;
                }

                m_lblListCaption.Text = ImportText.ListCaption(PathOf(node), count, CountNonNull(folder.Systems));
                m_lblConfirmation.Text = ImportText.Confirmation(folder.CountSystems(), m_targetGroupName);
            }
            finally
            {
                m_list.EndUpdate();
                UpdatePreview();
            }
        }

        private static int CountNonNull(List<SapSystem> systems)
        {
            int count = 0;
            foreach (SapSystem system in systems)
            {
                if (system != null)
                    count++;
            }
            return count;
        }

        private static string PathOf(TreeNode node)
        {
            List<string> names = new List<string>();
            for (TreeNode n = node; n != null; n = n.Parent)
            {
                SapFolder folder = n.Tag as SapFolder;
                names.Add(folder == null ? "" : folder.Name ?? "");
            }
            names.Reverse();
            return ImportText.JoinPath(names);
        }

        // What the import of the selected folder would create, against the target.
        private void UpdatePreview()
        {
            string text = "";
            try
            {
                SapFolder folder = SelectedFolder;
                if (folder != null && m_hasSystems)
                {
                    if (m_plan == null || !ReferenceEquals(m_planFolder, folder))
                    {
                        m_plan = ImportPlanner.Plan(folder, m_targetSnapshot);
                        m_planFolder = folder;
                    }
                    text = ImportText.Preview(m_plan.EntriesToAdd, m_plan.GroupsToCreate, m_plan.EntriesSkipped);
                    string userName = DefaultUserName;
                    if (userName != null)
                        text += " " + ImportText.UserNamePreview(userName, ImportPlanner.CountEntriesWithoutUserName(m_plan.Root));
                }
            }
            catch (Exception)
            {
                // The preview is informative only.
                m_plan = null;
                m_planFolder = null;
                text = "";
            }
            m_lblPreview.Text = text;
        }

        private void UpdateUserNameControls()
        {
            m_txtUserName.Enabled = m_chkUserName.Checked;
            m_lblUserNameHint.Visible = m_chkUserName.Checked && string.IsNullOrWhiteSpace(m_txtUserName.Text);
        }

        private void OnUserNameCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateUserNameControls();
                if (m_settingUserNameOption)
                    return;

                UpdatePreview();
                if (m_chkUserName.Checked)
                {
                    m_txtUserName.Focus();
                }
            }
            catch (Exception)
            {
                // The option controls and the focus are conveniences only.
            }
        }

        private void OnUserNameTextChanged(object sender, EventArgs e)
        {
            try
            {
                if (m_settingUserNameOption)
                    return;
                UpdateUserNameControls();
                UpdatePreview();
            }
            catch (Exception)
            {
                // The option controls and the preview are informative only.
            }
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            SapFolder before = SelectedFolder;
            try
            {
                ApplySearch();
            }
            catch (Exception)
            {
                // A failing filter must not escape into the message loop.
            }
            finally
            {
                NotifyIfSelectionChanged(before);
            }
        }

        private void OnClearSearchClick(object sender, EventArgs e)
        {
            ClearSearch();
            try
            {
                m_txtSearch.Focus();
            }
            catch (Exception)
            {
                // Focus is a convenience only.
            }
        }

        // The native tree selects its first item when it gains focus with nothing
        // selected; Enter would then import that whole workspace. Only the user
        // (mouse, keyboard) or code may select a folder, never the focus change.
        private void OnTreeBeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (m_tree.HandlingFocus)
                e.Cancel = true;
        }

        private void OnTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            OnSelectionChangedInTree();
        }

        private void OnTreeSelectionDropped(object sender, EventArgs e)
        {
            OnSelectionChangedInTree();
        }

        private void OnSelectionChangedInTree()
        {
            if (m_suppressEvents)
                return;

            try
            {
                UpdateSelectionView();
            }
            catch (Exception)
            {
                // A failing refresh must not escape into the message loop.
            }
            RaiseSelectionChanged();
        }

        // Raised once after a reset when it dropped a selected folder.
        private void NotifyIfSelectionChanged(SapFolder before)
        {
            if (before != null && SelectedFolder != before)
                RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            try
            {
                EventHandler handler = SelectionChanged;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                // A failing listener must not escape into the message loop.
            }
        }

        // Long texts wrap instead of being cut off.
        private void UpdateLabelWidths()
        {
            int width = Math.Max(ClientSize.Width - 12, 50);
            m_lblMessage.MaximumSize = new Size(width, 0);
            m_lblConfirmation.MaximumSize = new Size(width, 0);
            m_lblPreview.MaximumSize = new Size(width, 0);
            m_lblUserNameHint.MaximumSize = new Size(Math.Max(width - m_lblUserNameHint.Margin.Left, 50), 0);
        }

        private void UpdateCaptionWidth()
        {
            m_lblListCaption.MaximumSize = new Size(Math.Max(m_split.Panel2.ClientSize.Width, 50), 0);
        }

        // The columns follow the list width so they survive scaling.
        private void UpdateColumnWidths()
        {
            int width = Math.Max(m_list.ClientSize.Width - 4, 90);
            m_colName.Width = width * 45 / 100;
            m_colSid.Width = width * 15 / 100;
            m_colServer.Width = width - m_colName.Width - m_colSid.Width;
        }

        private void OnPanelSizeChanged(object sender, EventArgs e)
        {
            UpdateLabelWidths();
        }

        private void OnListPanelSizeChanged(object sender, EventArgs e)
        {
            UpdateCaptionWidth();
        }

        private bool SplitHasUsableWidth()
        {
            int needed = m_split.Panel1MinSize + m_split.Panel2MinSize + m_split.SplitterWidth;
            return m_split.Width > needed;
        }

        private void ApplySplitterDistance(int distance)
        {
            int max = m_split.Width - m_split.Panel2MinSize - m_split.SplitterWidth;
            int value = Math.Max(m_split.Panel1MinSize, Math.Min(distance, max));
            try
            {
                m_split.SplitterDistance = value;
            }
            catch (Exception)
            {
                // A splitter position is a convenience only.
            }
        }

        // The tree gets the requested width, or a third of the width, once the
        // split has a usable size.
        private void OnSplitSizeChanged(object sender, EventArgs e)
        {
            if (m_splitPlaced)
                return;

            if (!SplitHasUsableWidth())
                return;

            m_splitPlaced = true;
            ApplySplitterDistance(m_splitRequest > 0 ? m_splitRequest : Math.Max(m_split.Panel1MinSize, m_split.Width / 3));
        }

        private void OnListSizeChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateColumnWidths();
            }
            catch (Exception)
            {
                // The column widths are a convenience only.
            }
        }

        // A 16x16 folder: warm yellow body with a darker outline and a tab; the
        // open variant adds a lighter front flap.
        private static Bitmap DrawFolder(bool open)
        {
            Bitmap bmp = new Bitmap(IconSize, IconSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush body = new SolidBrush(Color.FromArgb(240, 196, 80)))
            using (SolidBrush flap = new SolidBrush(Color.FromArgb(252, 226, 140)))
            using (Pen outline = new Pen(Color.FromArgb(176, 128, 32)))
            {
                g.Clear(Color.Transparent);
                // Tab.
                g.FillRectangle(body, 1, 2, 6, 3);
                g.DrawRectangle(outline, 1, 2, 6, 2);
                // Body.
                g.FillRectangle(body, 1, 4, 14, 10);
                g.DrawRectangle(outline, 1, 4, 13, 9);
                if (open)
                {
                    g.FillRectangle(flap, 3, 7, 12, 7);
                    g.DrawRectangle(outline, 3, 7, 11, 6);
                }
            }
            return bmp;
        }

        /// <summary>A tree that reports while it handles gaining the focus.</summary>
        private sealed class FolderTreeView : TreeView
        {
            private const int WM_SETFOCUS = 0x0007;

            private bool m_handlingFocus;

            public bool HandlingFocus { get { return m_handlingFocus; } }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg != WM_SETFOCUS)
                {
                    base.WndProc(ref m);
                    return;
                }

                bool hadSelection = SelectedNode != null;
                m_handlingFocus = true;
                try
                {
                    base.WndProc(ref m);
                }
                finally
                {
                    m_handlingFocus = false;
                }

                // In case the selection got through anyway: drop it and let the
                // panel refresh (clearing raises no AfterSelect).
                if (!hadSelection && SelectedNode != null)
                {
                    SelectedNode = null;
                    EventHandler handler = SelectionDroppedOnFocus;
                    if (handler != null)
                        handler(this, EventArgs.Empty);
                }
            }

            public event EventHandler SelectionDroppedOnFocus;
        }
    }
}
