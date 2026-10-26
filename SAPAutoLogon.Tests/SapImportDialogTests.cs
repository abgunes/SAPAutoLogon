using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SapImportDialogTests
    {
        // Collects background work so the test decides when it runs.
        private sealed class ManualRunner
        {
            public readonly List<Action> Queued = new List<Action>();

            public void Run(Action work)
            {
                Queued.Add(work);
            }

            public void RunAll()
            {
                Action[] work = Queued.ToArray();
                Queued.Clear();
                foreach (Action a in work) a();
            }
        }

        private static LandscapeLoadCache NewCache(ManualRunner runner)
        {
            return new LandscapeLoadCache(delegate { return ImportTestData.Loaded(); }, runner.Run);
        }

        [TestMethod]
        public void Import_button_follows_can_import()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.Panel.CreateControl();
                IntPtr handle = dialog.Panel.FolderTree.Handle;
                Assert.AreNotEqual(IntPtr.Zero, handle);

                Assert.IsFalse(dialog.ImportButton.Enabled);

                dialog.Panel.SetLandscape(ImportTestData.Loaded(), "SAP");
                dialog.Panel.FolderTree.SelectedNode = ImportTestData.Node(dialog.Panel.FolderTree, "Local", "Demo");

                Assert.IsTrue(dialog.ImportButton.Enabled);
                Assert.AreEqual("Demo", dialog.SelectedFolder.Name);
                Assert.AreEqual(DialogResult.OK, dialog.ImportButton.DialogResult);
                Assert.AreEqual(DialogResult.Cancel, dialog.CancelButtonEx.DialogResult);
                Assert.AreSame(dialog.ImportButton, dialog.AcceptButton);
                Assert.AreSame(dialog.CancelButtonEx, dialog.CancelButton);
                Assert.AreEqual("Import from SAP Logon", dialog.Text);
            }
        }

        [TestMethod]
        public void Cached_result_is_applied_immediately()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = NewCache(runner);
            cache.Request(delegate { });
            runner.RunAll();

            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.BeginLoad(cache);

                Assert.AreEqual(2, dialog.Panel.FolderTree.Nodes.Count);
            }
        }

        [TestMethod]
        public void Background_result_is_applied_when_it_arrives()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = NewCache(runner);

            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.BeginLoad(cache);

                Assert.AreEqual(ImportText.LoadingText, dialog.Panel.MessageLabel.Text);
                Assert.IsFalse(dialog.ImportButton.Enabled);

                runner.RunAll();

                Assert.AreEqual(2, dialog.Panel.FolderTree.Nodes.Count);
            }
        }

        [TestMethod]
        public void Result_after_dispose_is_ignored()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = NewCache(runner);
            SapImportDialog dialog = new SapImportDialog("SAP");
            dialog.BeginLoad(cache);

            dialog.Dispose();
            runner.RunAll();

            Assert.IsTrue(dialog.IsDisposed);
        }

        [TestMethod]
        public void Null_cache_disables_import_with_message()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.BeginLoad(null);

                Assert.AreEqual(ImportText.NoSystems, dialog.Panel.MessageLabel.Text);
                Assert.IsFalse(dialog.ImportButton.Enabled);
            }
        }

        [TestMethod]
        public void Error_result_from_cache_is_shown()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(
                delegate { return new LandscapeLoadResult { Error = "broken" }; }, runner.Run);

            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.BeginLoad(cache);
                runner.RunAll();

                Assert.AreEqual("broken", dialog.Panel.MessageLabel.Text);
                Assert.IsFalse(dialog.ImportButton.Enabled);
            }
        }

        [TestMethod]
        public void Result_arriving_on_worker_thread_before_handle_is_applied_on_ui_thread_after_handle()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = NewCache(runner);

            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.BeginLoad(cache);
                Assert.IsFalse(dialog.IsHandleCreated);

                Exception failure = null;
                Thread worker = new Thread(delegate()
                {
                    try
                    {
                        runner.RunAll();
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                });
                worker.Start();
                worker.Join();

                Assert.IsNull(failure);
                // Nothing may touch the controls from the worker thread.
                Assert.AreEqual(0, dialog.Panel.FolderTree.Nodes.Count);
                Assert.AreEqual(ImportText.LoadingText, dialog.Panel.MessageLabel.Text);

                IntPtr handle = dialog.Handle;
                Assert.AreNotEqual(IntPtr.Zero, handle);

                Assert.AreEqual(2, dialog.Panel.FolderTree.Nodes.Count);
                Assert.IsFalse(dialog.ImportButton.Enabled);
                dialog.Panel.CreateControl();
                IntPtr treeHandle = dialog.Panel.FolderTree.Handle;
                Assert.AreNotEqual(IntPtr.Zero, treeHandle);
                dialog.Panel.FolderTree.SelectedNode = ImportTestData.Node(dialog.Panel.FolderTree, "Local", "Demo");
                Assert.IsTrue(dialog.ImportButton.Enabled);
            }
        }

        [TestMethod]
        public void Result_after_dispose_on_worker_thread_is_ignored()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = NewCache(runner);
            SapImportDialog dialog = new SapImportDialog("SAP");
            dialog.BeginLoad(cache);
            dialog.Dispose();

            Exception failure = null;
            Thread worker = new Thread(delegate()
            {
                try
                {
                    runner.RunAll();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            worker.Start();
            worker.Join();

            Assert.IsNull(failure);
            Assert.IsTrue(dialog.IsDisposed);
        }

        [TestMethod]
        public void Dialog_button_follows_a_direct_SetLandscape_that_clears_the_selection()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.Panel.CreateControl();
                IntPtr handle = dialog.Panel.FolderTree.Handle;
                Assert.AreNotEqual(IntPtr.Zero, handle);
                dialog.Panel.SetLandscape(ImportTestData.Loaded(), "SAP");
                dialog.Panel.FolderTree.SelectedNode = ImportTestData.Node(dialog.Panel.FolderTree, "Local", "Demo");
                Assert.IsTrue(dialog.ImportButton.Enabled);

                dialog.Panel.SetLandscape(new LandscapeLoadResult { Error = "broken" }, "SAP");

                Assert.IsFalse(dialog.ImportButton.Enabled);
            }
        }

        [TestMethod]
        public void Import_comes_before_Cancel_in_tab_order()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Assert.IsTrue(dialog.ImportButton.TabIndex < dialog.CancelButtonEx.TabIndex);
            }
        }

        [TestMethod]
        public void Dialog_has_minimum_size()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Assert.IsTrue(dialog.MinimumSize.Width >= 700);
                Assert.IsTrue(dialog.MinimumSize.Height >= 520);
                Assert.IsTrue(dialog.Size.Width >= dialog.MinimumSize.Width);
                Assert.IsTrue(dialog.Size.Height >= dialog.MinimumSize.Height);
            }
        }

        [TestMethod]
        public void Dialog_uses_the_message_box_font_and_font_scaling()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Font expected = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
                Assert.AreEqual(expected.Name, dialog.Font.Name);
                Assert.AreEqual(AutoScaleMode.Font, dialog.AutoScaleMode);
                Assert.AreEqual(dialog.Font.Name, dialog.Panel.Font.Name);
            }
        }

        [TestMethod]
        public void Header_shows_title_subtitle_and_logo()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Assert.AreEqual("Import from SAP Logon", dialog.HeaderTitleLabel.Text);
                Assert.IsTrue(dialog.HeaderTitleLabel.Font.Bold);
                Assert.AreEqual(ImportText.HeaderSubtitle("SAP"), dialog.HeaderSubtitleLabel.Text);
                Assert.IsNull(dialog.HeaderLogo.Image);

                using (Bitmap b = new Bitmap(32, 32))
                {
                    dialog.SetImages(b, null);
                    Assert.AreSame(b, dialog.HeaderLogo.Image);
                    Assert.IsNull(dialog.Panel.SystemList.SmallImageList);
                }
            }
        }

        [TestMethod]
        public void Images_passed_in_are_not_disposed_by_the_dialog()
        {
            using (Bitmap logo = new Bitmap(32, 32))
            using (Bitmap row = new Bitmap(16, 16))
            {
                SapImportDialog dialog = new SapImportDialog("SAP");
                dialog.SetImages(logo, row);
                Assert.IsNotNull(dialog.Panel.SystemList.SmallImageList);
                dialog.Dispose();

                // A disposed Bitmap throws on Width.
                Assert.AreEqual(32, logo.Width);
                Assert.AreEqual(16, row.Width);
            }
        }

        [TestMethod]
        public void Target_snapshot_reaches_the_panel()
        {
            GroupSnapshot snap = new GroupSnapshot();
            using (SapImportDialog dialog = new SapImportDialog("SAP", snap))
            {
                Assert.AreSame(snap, dialog.Panel.TargetSnapshot);
            }
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Assert.IsNull(dialog.Panel.TargetSnapshot);
            }
        }

        [TestMethod]
        public void User_name_option_is_exposed()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.SetUserNameOption(true, " alice ");
                Assert.IsTrue(dialog.UserNameOptionChecked);
                Assert.AreEqual("alice", dialog.UserNameOptionText);
                Assert.AreEqual("alice", dialog.DefaultUserName);

                dialog.SetUserNameOption(false, "x");
                Assert.IsFalse(dialog.UserNameOptionChecked);
                Assert.AreEqual("x", dialog.UserNameOptionText);
                Assert.IsNull(dialog.DefaultUserName);

                dialog.SetUserNameOption(true, "  ");
                Assert.AreEqual("", dialog.UserNameOptionText);
                Assert.IsNull(dialog.DefaultUserName);
            }
        }

        [TestMethod]
        public void RestoreLayout_applies_size_and_splitter()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                dialog.RestoreLayout(900, 600, 300);

                Assert.AreEqual(Math.Min(900, area.Width), dialog.Size.Width);
                Assert.AreEqual(Math.Min(600, area.Height), dialog.Size.Height);
                Assert.AreEqual(300, dialog.Panel.SplitterDistance);

                int w, h, s;
                dialog.CaptureLayout(out w, out h, out s);
                Assert.AreEqual(dialog.Size.Width, w);
                Assert.AreEqual(dialog.Size.Height, h);
                Assert.AreEqual(300, s);
            }
        }

        [TestMethod]
        public void CaptureLayout_in_a_non_normal_state_keeps_the_last_normal_splitter()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.RestoreLayout(900, 600, 300);
                Assert.AreEqual(300, dialog.Panel.SplitterDistance);

                // What a maximised window would have: another splitter position.
                dialog.Panel.SetSplitterDistance(500);
                Assert.AreEqual(500, dialog.Panel.SplitterDistance);

                dialog.WindowState = FormWindowState.Maximized;
                int w, h, s;
                dialog.CaptureLayout(out w, out h, out s);

                Assert.AreEqual(300, s);
            }
        }

        [TestMethod]
        public void CaptureLayout_in_a_non_normal_state_without_a_known_normal_splitter_returns_zero()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.WindowState = FormWindowState.Maximized;
                int w, h, s;
                dialog.CaptureLayout(out w, out h, out s);

                Assert.AreEqual(0, s);
            }
        }

        [TestMethod]
        public void RestoreLayout_sets_the_splitter_after_the_size_change()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                // The form is never shown: force the handles and a layout pass so
                // the split really has its width before and after the resize.
                dialog.CreateControl();
                dialog.PerformLayout();
                int splitBefore = dialog.Panel.Splitter.Width;

                dialog.RestoreLayout(dialog.Size.Width + 200, dialog.Size.Height + 50, 250);
                dialog.PerformLayout();

                Assert.AreNotEqual(splitBefore, dialog.Panel.Splitter.Width);
                Assert.AreEqual(250, dialog.Panel.SplitterDistance);
            }
        }

        [TestMethod]
        public void RestoreLayout_clamps_to_minimum_and_working_area()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.RestoreLayout(10, 10, 0);
                Assert.IsTrue(dialog.Size.Width >= dialog.MinimumSize.Width);
                Assert.IsTrue(dialog.Size.Height >= dialog.MinimumSize.Height);

                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                dialog.RestoreLayout(100000, 100000, 0);
                Assert.IsTrue(dialog.Size.Width <= area.Width);
                Assert.IsTrue(dialog.Size.Height <= area.Height);
            }
        }

        [TestMethod]
        public void RestoreLayout_ignores_missing_values()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Size size = dialog.Size;
                int split = dialog.Panel.SplitterDistance;

                dialog.RestoreLayout(0, 0, 0);
                Assert.AreEqual(size, dialog.Size);
                Assert.AreEqual(split, dialog.Panel.SplitterDistance);

                dialog.RestoreLayout(-1, 500, -1);
                Assert.AreEqual(size, dialog.Size);
                Assert.AreEqual(split, dialog.Panel.SplitterDistance);
            }
        }

        [TestMethod]
        public void Shortcuts_are_handled_as_specified()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                Assert.IsTrue(dialog.HandleShortcut(Keys.Control | Keys.F, false));

                dialog.Panel.SearchBox.Text = "x";
                Assert.IsTrue(dialog.HandleShortcut(Keys.Escape, true));
                Assert.AreEqual("", dialog.Panel.SearchBox.Text);
                Assert.IsFalse(dialog.HandleShortcut(Keys.Escape, true));
                Assert.IsFalse(dialog.HandleShortcut(Keys.Escape, false));

                dialog.Panel.SearchBox.Text = "y";
                Assert.IsTrue(dialog.HandleShortcut(Keys.Shift | Keys.Escape, true));
                Assert.AreEqual("", dialog.Panel.SearchBox.Text);
                Assert.IsFalse(dialog.HandleShortcut(Keys.Shift | Keys.Escape, true));

                Assert.IsTrue(dialog.HandleShortcut(Keys.Enter, true));
                Assert.IsFalse(dialog.HandleShortcut(Keys.Enter, false));
                Assert.IsFalse(dialog.HandleShortcut(Keys.A, false));
            }
        }

        [TestMethod]
        public void Enter_in_the_search_box_does_not_import()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.Panel.CreateControl();
                IntPtr handle = dialog.Panel.FolderTree.Handle;
                Assert.AreNotEqual(IntPtr.Zero, handle);
                dialog.Panel.SetLandscape(ImportTestData.Loaded(), "SAP");
                dialog.Panel.FolderTree.SelectedNode = ImportTestData.Node(dialog.Panel.FolderTree, "Local", "Demo");
                Assert.IsTrue(dialog.ImportButton.Enabled);

                int clicks = 0;
                dialog.ImportButton.Click += delegate { clicks++; };

                // Handled (true) means ProcessCmdKey swallows the key before the
                // form's AcceptButton handling could click Import.
                Assert.IsTrue(dialog.HandleShortcut(Keys.Enter, true));
                Assert.IsTrue(dialog.HandleShortcut(Keys.Return, true));
                Assert.IsTrue(dialog.HandleShortcut(Keys.Shift | Keys.Enter, true));
                Assert.IsTrue(dialog.HandleShortcut(Keys.Shift | Keys.Return, true));
                Assert.IsFalse(dialog.HandleShortcut(Keys.Control | Keys.Enter, true));

                Assert.AreEqual(0, clicks);
                Assert.AreEqual(DialogResult.None, dialog.DialogResult);
                Assert.AreEqual("Demo", dialog.SelectedFolder.Name);
            }
        }

        [TestMethod]
        public void Import_button_still_follows_can_import_with_filter()
        {
            using (SapImportDialog dialog = new SapImportDialog("SAP"))
            {
                dialog.Panel.CreateControl();
                IntPtr handle = dialog.Panel.FolderTree.Handle;
                Assert.AreNotEqual(IntPtr.Zero, handle);

                dialog.Panel.SetLandscape(ImportTestData.Loaded(), "SAP");
                dialog.Panel.FolderTree.SelectedNode = ImportTestData.Node(dialog.Panel.FolderTree, "Local", "Demo");
                Assert.IsTrue(dialog.ImportButton.Enabled);

                dialog.Panel.SearchBox.Text = "Broken";
                Assert.IsFalse(dialog.ImportButton.Enabled);

                dialog.Panel.ClearSearch();
                Assert.IsFalse(dialog.ImportButton.Enabled);
            }
        }
    }
}
