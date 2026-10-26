using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    internal static class ImportTestData
    {
        public static LandscapeLoadResult Loaded()
        {
            string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape.xml"));
            return LandscapeLoader.Load(new LandscapeFiles { LocalPath = "x" }, delegate { return text; });
        }

        public static TreeNode Node(TreeView tree, params string[] path)
        {
            TreeNodeCollection level = tree.Nodes;
            TreeNode found = null;
            foreach (string name in path)
            {
                found = null;
                foreach (TreeNode n in level)
                {
                    SapFolder folder = n.Tag as SapFolder;
                    if (folder != null && folder.Name == name)
                    {
                        found = n;
                        break;
                    }
                }
                if (found == null)
                {
                    return null;
                }
                level = found.Nodes;
            }
            return found;
        }

        public static GroupSnapshot SnapshotOf(PlannedGroup planned)
        {
            GroupSnapshot g = new GroupSnapshot();
            g.Name = planned.Name;
            g.Handle = new object();
            foreach (PlannedEntry e in planned.Entries)
            {
                EntrySnapshot es = new EntrySnapshot();
                es.Title = e.Title;
                foreach (KeyValuePair<string, string> kv in e.Fields)
                {
                    es.Fields[kv.Key] = kv.Value;
                }
                g.Entries.Add(es);
            }
            foreach (PlannedGroup child in planned.Groups)
            {
                g.Groups.Add(SnapshotOf(child));
            }
            return g;
        }
    }

    [TestClass]
    public class SapImportPanelTests
    {
        private static SapImportPanel Create()
        {
            SapImportPanel panel = new SapImportPanel();
            // Selection events only fire once the native tree exists.
            panel.CreateControl();
            IntPtr handle = panel.FolderTree.Handle;
            Assert.AreNotEqual(IntPtr.Zero, handle);
            return panel;
        }

        [TestMethod]
        public void Loading_state_shows_text_and_disables_import()
        {
            SapImportPanel panel = Create();

            panel.ShowLoading();

            Assert.AreEqual("Loading SAP Logon systems...", panel.MessageLabel.Text);
            Assert.IsFalse(panel.FolderTree.Enabled);
            Assert.IsFalse(panel.CanImport);
        }

        [TestMethod]
        public void Error_result_shows_error_and_import_stays_disabled()
        {
            SapImportPanel panel = Create();

            panel.SetLandscape(new LandscapeLoadResult { Error = "broken" }, "SAP");

            Assert.AreEqual("broken", panel.MessageLabel.Text);
            Assert.AreEqual(0, panel.FolderTree.Nodes.Count);
            Assert.IsFalse(panel.CanImport);
        }

        [TestMethod]
        public void Empty_folders_show_no_systems_message_and_disable_import()
        {
            SapImportPanel panel = Create();

            panel.SetLandscape(new LandscapeLoadResult(), "SAP");

            Assert.AreEqual(ImportText.NoSystems, panel.MessageLabel.Text);
            Assert.IsFalse(panel.CanImport);
        }

        [TestMethod]
        public void Null_result_behaves_like_error_with_no_systems_message()
        {
            SapImportPanel panel = Create();

            panel.SetLandscape(null, "SAP");

            Assert.AreEqual(ImportText.NoSystems, panel.MessageLabel.Text);
            Assert.AreEqual(0, panel.FolderTree.Nodes.Count);
            Assert.IsFalse(panel.FolderTree.Enabled);
            Assert.IsFalse(panel.CanImport);
        }

        [TestMethod]
        public void Tree_mirrors_folders()
        {
            SapImportPanel panel = Create();

            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            Assert.AreEqual(2, panel.FolderTree.Nodes.Count);
            Assert.AreEqual("Local (8)", panel.FolderTree.Nodes[0].Text);
            Assert.AreEqual("(no folder) (1)", panel.FolderTree.Nodes[1].Text);
            TreeNode sub11 = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1", "Sub1.1");
            Assert.IsNotNull(sub11);
            Assert.IsNull(ImportTestData.Node(panel.FolderTree, "Local", "Hidden"));
            SapFolder folder = sub11.Tag as SapFolder;
            Assert.IsNotNull(folder);
            Assert.AreEqual("Sub1.1", folder.Name);
        }

        [TestMethod]
        public void Selecting_a_folder_lists_its_direct_systems_with_three_columns()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1");

            Assert.AreEqual(3, panel.SystemList.Columns.Count);
            Assert.AreEqual(1, panel.SystemList.Items.Count);
            Assert.AreEqual("Q25 - Group", panel.SystemList.Items[0].Text);
            Assert.AreEqual("Q25", panel.SystemList.Items[0].SubItems[1].Text);
            Assert.AreEqual("PUBLIC", panel.SystemList.Items[0].SubItems[2].Text);

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");

            Assert.AreEqual(0, panel.SystemList.Items.Count);
        }

        [TestMethod]
        public void Selecting_a_folder_enables_import_and_shows_confirmation()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");

            Assert.IsTrue(panel.CanImport);
            Assert.AreEqual("Import 6 systems into group 'SAP'?", panel.ConfirmationLabel.Text);
            Assert.AreEqual(1, raised);
            Assert.AreEqual("Demo", panel.SelectedFolder.Name);
        }

        [TestMethod]
        public void Before_selection_confirmation_asks_to_select()
        {
            SapImportPanel panel = Create();

            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            Assert.AreEqual(ImportText.SelectFolder, panel.ConfirmationLabel.Text);
            Assert.IsFalse(panel.CanImport);
            Assert.IsNull(panel.SelectedFolder);
        }

        [TestMethod]
        public void Warnings_are_shown_without_disabling_import()
        {
            SapImportPanel panel = Create();
            LandscapeLoadResult result = ImportTestData.Loaded();
            result.Warnings.Add("shared file missing");

            panel.SetLandscape(result, "SAP");

            Assert.IsTrue(panel.MessageLabel.Visible);
            StringAssert.Contains(panel.MessageLabel.Text, "shared file missing");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            Assert.IsTrue(panel.CanImport);
        }

        [TestMethod]
        public void New_landscape_clears_selection_and_list()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1");

            panel.ShowLoading();

            Assert.AreEqual(0, panel.SystemList.Items.Count);
            Assert.IsFalse(panel.SystemList.Enabled);
            Assert.AreEqual("", panel.ConfirmationLabel.Text);
            Assert.IsNull(panel.SelectedFolder);
        }

        [TestMethod]
        public void SelectionChanged_is_raised_when_selection_is_cleared_by_SetLandscape()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };

            panel.SetLandscape(new LandscapeLoadResult { Error = "broken" }, "SAP");

            Assert.AreEqual(1, raised);
            Assert.IsNull(panel.SelectedFolder);
        }

        [TestMethod]
        public void SelectionChanged_is_raised_when_selection_is_cleared_by_ShowLoading()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };

            panel.ShowLoading();

            Assert.AreEqual(1, raised);
        }

        [TestMethod]
        public void SelectionChanged_is_not_raised_when_nothing_was_selected()
        {
            SapImportPanel panel = Create();
            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };

            panel.ShowLoading();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            Assert.AreEqual(0, raised);
        }

        [TestMethod]
        public void Failing_listener_does_not_escape()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            panel.SelectionChanged += delegate { throw new InvalidOperationException("boom"); };

            panel.ShowLoading();

            Assert.IsNull(panel.SelectedFolder);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETFOCUS = 0x0007;

        // Delivers the focus message to the native tree the way Windows does when
        // the tree gains focus; works without a visible window.
        private static void GiveTreeFocus(SapImportPanel panel)
        {
            panel.FocusContent();
            panel.FolderTree.Focus();
            SendMessage(panel.FolderTree.Handle, WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
            Application.DoEvents();
        }

        [TestMethod]
        public void Focus_after_loading_does_not_select_a_folder()
        {
            SapImportPanel panel = Create();
            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            GiveTreeFocus(panel);

            Assert.IsNull(panel.FolderTree.SelectedNode);
            Assert.IsNull(panel.SelectedFolder);
            Assert.IsFalse(panel.CanImport);
            Assert.AreEqual(ImportText.SelectFolder, panel.ConfirmationLabel.Text);
            Assert.AreEqual(0, panel.SystemList.Items.Count);
            Assert.AreEqual(0, raised);
        }

        [TestMethod]
        public void Selecting_after_focus_still_enables_import()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            GiveTreeFocus(panel);

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");

            Assert.IsTrue(panel.CanImport);
            Assert.AreEqual("Demo", panel.SelectedFolder.Name);
            Assert.AreEqual("Import 6 systems into group 'SAP'?", panel.ConfirmationLabel.Text);
        }

        [TestMethod]
        public void Focus_keeps_a_folder_the_user_selected()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1");

            GiveTreeFocus(panel);

            Assert.AreEqual("Sub1", panel.SelectedFolder.Name);
            Assert.IsTrue(panel.CanImport);
        }

        [TestMethod]
        public void Folders_without_any_system_disable_import()
        {
            SapImportPanel panel = Create();
            SapFolder child = new SapFolder();
            child.Name = "Child";
            SapFolder workspace = new SapFolder();
            workspace.Name = "Local";
            workspace.Folders.Add(child);
            LandscapeLoadResult result = new LandscapeLoadResult();
            result.Folders.Add(workspace);
            result.Warnings.Add("shared file missing");

            panel.SetLandscape(result, "SAP");
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local");

            Assert.IsFalse(panel.CanImport);
            Assert.IsTrue(panel.MessageLabel.Visible);
            Assert.AreEqual(ImportText.NoSystems + Environment.NewLine + "shared file missing", panel.MessageLabel.Text);
            Assert.AreEqual("", panel.ConfirmationLabel.Text);
        }

        [TestMethod]
        public void Without_native_handle_nothing_throws()
        {
            SapImportPanel panel = new SapImportPanel();

            panel.ShowLoading();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");

            Assert.AreEqual(2, panel.FolderTree.Nodes.Count);
        }

        private static SapSystem Sys(string name, string sid, string client, string server, string number)
        {
            SapSystem s = new SapSystem();
            s.Name = name;
            s.SystemId = sid;
            s.Client = client;
            s.ApplicationServer = server;
            s.SystemNumber = number;
            return s;
        }

        // 1 root, 14 children with 8 subfolders each, 440 systems spread over the 112 leaves.
        private static LandscapeLoadResult BigLandscape()
        {
            SapFolder root = new SapFolder();
            root.Name = "Root";
            List<SapFolder> leaves = new List<SapFolder>();
            for (int i = 0; i < 14; i++)
            {
                SapFolder child = new SapFolder();
                child.Name = "C" + i;
                root.Folders.Add(child);
                for (int j = 0; j < 8; j++)
                {
                    SapFolder leaf = new SapFolder();
                    leaf.Name = "L" + i + "_" + j;
                    child.Folders.Add(leaf);
                    leaves.Add(leaf);
                }
            }
            for (int n = 0; n < 440; n++)
            {
                string id = (n + 1).ToString("0000");
                leaves[n % leaves.Count].Systems.Add(Sys("SYS-" + id, "S" + id, "100", "host" + id + ".example.test", "00"));
            }
            LandscapeLoadResult result = new LandscapeLoadResult();
            result.Folders.Add(root);
            return result;
        }

        private static SapImportPanel Loaded()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(ImportTestData.Loaded(), "SAP");
            return panel;
        }

        [TestMethod]
        public void Folder_labels_show_recursive_counts()
        {
            SapImportPanel panel = Loaded();

            Assert.AreEqual("Demo (6)", ImportTestData.Node(panel.FolderTree, "Local", "Demo").Text);
            Assert.AreEqual("Empty (0)", ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Empty").Text);
        }

        [TestMethod]
        public void Tree_nodes_have_folder_icons()
        {
            SapImportPanel panel = Loaded();

            Assert.IsNotNull(panel.FolderTree.ImageList);
            Assert.AreEqual(2, panel.FolderTree.ImageList.Images.Count);
            Assert.IsTrue(panel.FolderTree.ImageList.ImageSize.Width >= 16);
            TreeNode local = ImportTestData.Node(panel.FolderTree, "Local");
            Assert.AreEqual(0, local.ImageIndex);
            Assert.AreEqual(1, local.SelectedImageIndex);
        }

        [TestMethod]
        public void Confirmation_label_is_bold()
        {
            SapImportPanel panel = Create();

            Assert.IsTrue(panel.ConfirmationLabel.Font.Bold);
        }

        [TestMethod]
        public void List_caption_before_and_after_selection()
        {
            SapImportPanel panel = Loaded();

            Assert.AreEqual(ImportText.ListCaptionNoSelection, panel.ListCaptionLabel.Text);

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            Assert.AreEqual("Systems in Local > Demo (none directly in this folder)", panel.ListCaptionLabel.Text);

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1");
            Assert.AreEqual("Systems in Local > Demo > Sub1", panel.ListCaptionLabel.Text);
        }

        [TestMethod]
        public void Search_filters_tree_to_matching_folders_and_systems()
        {
            SapImportPanel panel = Loaded();

            panel.SearchBox.Text = "sub1";

            Assert.AreEqual(1, panel.FolderTree.Nodes.Count);
            Assert.IsNotNull(ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1", "Sub1.1"));
            Assert.IsNull(ImportTestData.Node(panel.FolderTree, "Local", "DE"));
            Assert.IsNull(ImportTestData.Node(panel.FolderTree, "(no folder)"));
            Assert.IsTrue(ImportTestData.Node(panel.FolderTree, "Local").IsExpanded);
            Assert.IsTrue(ImportTestData.Node(panel.FolderTree, "Local", "Demo").IsExpanded);

            panel.SearchBox.Text = "10.0.0.7";

            TreeNode sub2 = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub2");
            Assert.IsNotNull(sub2);
            Assert.IsNull(ImportTestData.Node(panel.FolderTree, "Local", "DE"));

            panel.FolderTree.SelectedNode = sub2;

            Assert.AreEqual(1, panel.SystemList.Items.Count);
            Assert.AreEqual("S25 - Development", panel.SystemList.Items[0].Text);
        }

        [TestMethod]
        public void Search_keeps_whole_folder_import_counts()
        {
            SapImportPanel panel = Loaded();

            panel.SearchBox.Text = "snc";
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1", "Sub1.1");

            Assert.AreEqual(1, panel.SystemList.Items.Count);
            Assert.AreEqual("Systems in Local > Demo > Sub1 > Sub1.1 (1 of 2 match the search)", panel.ListCaptionLabel.Text);
            Assert.AreEqual("Import 2 systems into group 'SAP'?", panel.ConfirmationLabel.Text);
            Assert.AreEqual("2 entries and 1 groups will be created.", panel.PreviewLabel.Text);
            Assert.IsTrue(panel.CanImport);
        }

        [TestMethod]
        public void Search_with_no_match_shows_empty_tree_and_message()
        {
            SapImportPanel panel = Create();
            LandscapeLoadResult result = ImportTestData.Loaded();
            result.Warnings.Clear();
            result.Warnings.Add("shared file missing");
            panel.SetLandscape(result, "SAP");

            panel.SearchBox.Text = "zzz";

            Assert.AreEqual(0, panel.FolderTree.Nodes.Count);
            Assert.AreEqual(ImportText.NoSearchMatch + Environment.NewLine + "shared file missing", panel.MessageLabel.Text);
            Assert.IsNull(panel.SelectedFolder);
            Assert.IsFalse(panel.CanImport);

            panel.ClearSearch();

            Assert.AreEqual(2, panel.FolderTree.Nodes.Count);
            Assert.AreEqual("shared file missing", panel.MessageLabel.Text);
        }

        [TestMethod]
        public void Selection_survives_a_filter_that_keeps_the_folder_and_is_dropped_otherwise()
        {
            SapImportPanel panel = Loaded();
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");

            panel.SearchBox.Text = "sub1";

            Assert.AreEqual("Demo", panel.SelectedFolder.Name);
            Assert.IsTrue(panel.CanImport);

            int raised = 0;
            panel.SelectionChanged += delegate { raised++; };

            panel.SearchBox.Text = "Broken";

            Assert.IsNull(panel.SelectedFolder);
            Assert.IsFalse(panel.CanImport);
            Assert.AreEqual(1, raised);

            panel.ClearSearch();

            Assert.AreEqual(2, panel.FolderTree.Nodes.Count);
            Assert.IsNull(panel.SelectedFolder);
        }

        [TestMethod]
        public void Clear_button_follows_search_text()
        {
            SapImportPanel panel = Loaded();

            Assert.IsFalse(panel.ClearSearchButton.Enabled);

            panel.SearchBox.Text = "x";
            Assert.IsTrue(panel.ClearSearchButton.Enabled);

            panel.ClearSearchButton.PerformClick();
            Assert.AreEqual("", panel.SearchBox.Text);
            Assert.IsFalse(panel.ClearSearchButton.Enabled);
        }

        [TestMethod]
        public void Focus_after_filtering_does_not_select_a_folder()
        {
            SapImportPanel panel = Loaded();

            panel.SearchBox.Text = "demo";
            GiveTreeFocus(panel);

            Assert.IsNull(panel.FolderTree.SelectedNode);
            Assert.IsFalse(panel.CanImport);
        }

        [TestMethod]
        public void Filtering_440_systems_is_fast()
        {
            SapImportPanel panel = Create();
            panel.SetLandscape(BigLandscape(), "SAP");
            string[] sequence = new string[] { "S", "SY", "SYS", "SYS-", "SYS-0", "SYS-00", "SYS-001", "SYS-0012", "zzz", "" };

            Stopwatch watch = Stopwatch.StartNew();
            for (int round = 0; round < 2; round++)
            {
                foreach (string text in sequence)
                {
                    panel.SearchBox.Text = text;
                }
            }
            watch.Stop();

            Assert.IsTrue(watch.ElapsedMilliseconds < 3000, "took " + watch.ElapsedMilliseconds + " ms");
            Assert.AreEqual(1, panel.FolderTree.Nodes.Count);
        }

        [TestMethod]
        public void Long_folder_names_are_shown_in_full()
        {
            string name = new string('N', 300);
            SapFolder child = new SapFolder();
            child.Name = "Child";
            child.Systems.Add(Sys("A - System", "A", "100", "a.example.test", "00"));
            SapFolder root = new SapFolder();
            root.Name = name;
            root.Folders.Add(child);
            LandscapeLoadResult result = new LandscapeLoadResult();
            result.Folders.Add(root);
            SapImportPanel panel = Create();

            panel.SetLandscape(result, "SAP");

            Assert.AreEqual(name + " (1)", panel.FolderTree.Nodes[0].Text);
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, name, "Child");
            Assert.IsTrue(panel.ListCaptionLabel.Text.StartsWith("Systems in " + name + " > Child"));
        }

        [TestMethod]
        public void User_name_box_is_disabled_until_checked()
        {
            SapImportPanel panel = Loaded();

            Assert.IsFalse(panel.UserNameTextBox.Enabled);
            Assert.IsFalse(panel.UserNameCheckBox.Checked);
            Assert.IsNull(panel.DefaultUserName);

            panel.UserNameCheckBox.Checked = true;
            Assert.IsTrue(panel.UserNameTextBox.Enabled);

            panel.UserNameTextBox.Text = " alice ";
            Assert.AreEqual("alice", panel.DefaultUserName);

            panel.UserNameCheckBox.Checked = false;
            Assert.IsNull(panel.DefaultUserName);
            Assert.AreEqual(" alice ", panel.UserNameTextBox.Text);
            Assert.IsFalse(panel.UserNameTextBox.Enabled);
        }

        [TestMethod]
        public void Blank_user_name_with_box_checked_sets_nothing_and_shows_hint()
        {
            SapImportPanel panel = Loaded();

            panel.UserNameCheckBox.Checked = true;
            panel.UserNameTextBox.Text = "   ";

            Assert.IsNull(panel.DefaultUserName);
            Assert.IsTrue(panel.UserNameHintLabel.Visible);
            Assert.AreEqual(ImportText.UserNameBlankHint, panel.UserNameHintLabel.Text);

            panel.UserNameTextBox.Text = "a";
            Assert.IsFalse(panel.UserNameHintLabel.Visible);

            panel.UserNameTextBox.Text = "";
            panel.UserNameCheckBox.Checked = false;
            Assert.IsFalse(panel.UserNameHintLabel.Visible);
        }

        [TestMethod]
        public void Set_user_name_option_prefills()
        {
            SapImportPanel panel = Loaded();

            panel.SetUserNameOption(true, "bob");

            Assert.IsTrue(panel.UserNameCheckBox.Checked);
            Assert.AreEqual("bob", panel.UserNameTextBox.Text);
            Assert.AreEqual("bob", panel.DefaultUserName);

            panel.SetUserNameOption(false, null);

            Assert.IsFalse(panel.UserNameCheckBox.Checked);
            Assert.AreEqual("", panel.UserNameTextBox.Text);
        }

        [TestMethod]
        public void Preview_line_shows_plan_counts()
        {
            SapImportPanel panel = Loaded();

            Assert.AreEqual("", panel.PreviewLabel.Text);
            Assert.IsNull(panel.TargetSnapshot);

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");
            Assert.AreEqual("6 entries and 6 groups will be created.", panel.PreviewLabel.Text);

            GroupSnapshot target = new GroupSnapshot();
            target.Name = "SAP";
            target.Handle = new object();
            target.Groups.Add(ImportTestData.SnapshotOf(ImportPlanner.Plan(panel.SelectedFolder, null).Root));
            panel.TargetSnapshot = target;

            Assert.AreEqual("0 entries and 0 groups will be created, 6 entries already present will be skipped.", panel.PreviewLabel.Text);
        }

        [TestMethod]
        public void Preview_mentions_default_user_name()
        {
            SapImportPanel panel = Loaded();
            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo");

            panel.SetUserNameOption(true, "alice");
            Assert.AreEqual("6 entries and 6 groups will be created. User name 'alice' will be set for 5 entries that have no user in SAP Logon.", panel.PreviewLabel.Text);

            panel.SetUserNameOption(true, " ");
            Assert.AreEqual("6 entries and 6 groups will be created.", panel.PreviewLabel.Text);
        }

        [TestMethod]
        public void Row_image_is_applied_to_system_rows()
        {
            SapImportPanel panel = Loaded();
            using (Bitmap bmp = new Bitmap(16, 16))
            {
                panel.SetRowImage(bmp);
            }

            panel.FolderTree.SelectedNode = ImportTestData.Node(panel.FolderTree, "Local", "Demo", "Sub1");

            Assert.IsNotNull(panel.SystemList.SmallImageList);
            Assert.AreEqual(1, panel.SystemList.SmallImageList.Images.Count);
            Assert.AreEqual(0, panel.SystemList.Items[0].ImageIndex);

            SapImportPanel plain = Loaded();
            plain.FolderTree.SelectedNode = ImportTestData.Node(plain.FolderTree, "Local", "Demo", "Sub1");
            Assert.AreEqual(-1, plain.SystemList.Items[0].ImageIndex);
        }

        [TestMethod]
        public void Splitter_request_is_applied_when_the_split_has_size()
        {
            SapImportPanel panel = Create();

            panel.SetSplitterDistance(300);

            Assert.AreEqual(300, panel.SplitterDistance);
        }

        [TestMethod]
        public void Splitter_request_is_clamped()
        {
            SapImportPanel panel = Create();

            panel.SetSplitterDistance(5);
            Assert.AreEqual(panel.Splitter.Panel1MinSize, panel.SplitterDistance);
            Assert.AreEqual(80, panel.SplitterDistance);

            panel.SetSplitterDistance(100000);
            int max = panel.Splitter.Width - panel.Splitter.Panel2MinSize - panel.Splitter.SplitterWidth;
            Assert.AreEqual(max, panel.SplitterDistance);

            panel.SetSplitterDistance(0);
            Assert.AreEqual(max, panel.SplitterDistance);
        }
    }
}
