using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SapEntryPanelTests
    {
        private static List<SapSystem> Sample()
        {
            return new List<SapSystem>
            {
                new SapSystem { Name = "Same", SystemId = "AAA", ApplicationServer = "a.example.test" },
                new SapSystem { Name = "Same", SystemId = "BBB", ApplicationServer = "b.example.test" },
                new SapSystem { Name = "Other", SystemId = "CCC", ApplicationServer = "c.example.test" }
            };
        }

        [TestMethod]
        public void Starts_in_manual_mode_with_list_disabled()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                Assert.IsFalse(panel.AutomaticMode);
                Assert.IsFalse(panel.AutomaticRadio.Checked);
                Assert.IsFalse(panel.SystemList.Enabled);
            }
        }

        [TestMethod]
        public void Automatic_enables_list_and_fields_stay_editable_in_both_modes()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                foreach (string key in SapFields.All)
                    Assert.IsTrue(panel.Field(key).Enabled && !panel.Field(key).ReadOnly, key);

                panel.AutomaticRadio.Checked = true;
                Assert.IsTrue(panel.AutomaticMode);
                Assert.IsTrue(panel.SystemList.Enabled);
                foreach (string key in SapFields.All)
                    Assert.IsTrue(panel.Field(key).Enabled && !panel.Field(key).ReadOnly, key);
            }
        }

        [TestMethod]
        public void Selecting_item_raises_SystemPicked_with_that_object()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                List<SapSystem> systems = Sample();
                panel.SetSystems(systems, "");
                panel.AutomaticRadio.Checked = true;

                List<SapSystem> picked = new List<SapSystem>();
                panel.SystemPicked += delegate(object s, SystemPickedEventArgs e) { picked.Add(e.System); };

                panel.SystemList.SelectedIndex = 1;

                Assert.AreEqual(1, picked.Count);
                Assert.AreSame(systems[1], picked[0]);
            }
        }

        [TestMethod]
        public void Selecting_item_in_manual_mode_does_not_raise()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                panel.SystemList.SelectedIndex = 0;

                Assert.AreEqual(0, raised);
            }
        }

        [TestMethod]
        public void Search_text_filters_the_list()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                List<SapSystem> systems = Sample();
                panel.SetSystems(systems, "");
                panel.AutomaticRadio.Checked = true;
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                Assert.AreEqual(3, panel.SystemList.Items.Count);

                panel.SearchBox.Text = "ccc";

                Assert.AreEqual(1, panel.SystemList.Items.Count);
                Assert.AreSame(systems[2], panel.SystemList.Items[0]);
                Assert.AreEqual(0, raised);

                panel.SearchBox.Text = "";
                Assert.AreEqual(3, panel.SystemList.Items.Count);
                Assert.AreEqual(0, raised);
            }
        }

        [TestMethod]
        public void LoadFields_then_ReadFields_roundtrips_and_does_not_raise_FieldEdited()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                int edited = 0;
                panel.FieldEdited += delegate { edited++; };

                Dictionary<string, string> input = new Dictionary<string, string>();
                input[SapFields.SystemId] = "S25";
                input[SapFields.Client] = "100";
                input[SapFields.Transaction] = "SE38";
                panel.LoadFields(input);

                Dictionary<string, string> output = panel.ReadFields();
                Assert.AreEqual(SapFields.All.Length, output.Count);
                Assert.AreEqual("S25", output[SapFields.SystemId]);
                Assert.AreEqual("100", output[SapFields.Client]);
                Assert.AreEqual("SE38", output[SapFields.Transaction]);
                Assert.AreEqual("", output[SapFields.Language]);
                Assert.AreEqual(0, edited);

                panel.LoadFields(new Dictionary<string, string>());
                Assert.AreEqual("", panel.ReadFields()[SapFields.SystemId]);
                Assert.AreEqual(0, edited);
            }
        }

        [TestMethod]
        public void Typing_in_a_field_raises_FieldEdited()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                int edited = 0;
                panel.FieldEdited += delegate { edited++; };

                panel.Field(SapFields.Client).Text = "200";

                Assert.AreEqual(1, edited);
                Assert.AreEqual("200", panel.ReadFields()[SapFields.Client]);
            }
        }

        [TestMethod]
        public void Selection_is_cleared_when_mode_changes_and_same_item_can_be_picked_again()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                panel.SystemList.SelectedIndex = 0;
                Assert.AreEqual(1, raised);

                panel.AutomaticRadio.Checked = false;
                Assert.AreEqual(-1, panel.SystemList.SelectedIndex);
                panel.AutomaticRadio.Checked = true;
                Assert.AreEqual(-1, panel.SystemList.SelectedIndex);
                Assert.AreEqual(1, raised);

                panel.SystemList.SelectedIndex = 0;
                Assert.AreEqual(2, raised);
            }
        }

        [TestMethod]
        public void Selection_is_cleared_after_user_edits_a_field()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                panel.SystemList.SelectedIndex = 0;
                panel.Field(SapFields.Client).Text = "300";

                Assert.AreEqual(-1, panel.SystemList.SelectedIndex);
                Assert.AreEqual(1, raised);

                panel.SystemList.SelectedIndex = 0;
                Assert.AreEqual(2, raised);
            }
        }

        [TestMethod]
        public void LoadFields_does_not_clear_selection()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                panel.SystemList.SelectedIndex = 1;
                Dictionary<string, string> values = new Dictionary<string, string>();
                values[SapFields.SystemId] = "BBB";
                panel.LoadFields(values);

                Assert.AreEqual(1, panel.SystemList.SelectedIndex);
                Assert.AreEqual(1, raised);
            }
        }

        [TestMethod]
        public void Fields_stay_editable_when_a_message_disables_automatic()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "msg");
                int edited = 0;
                panel.FieldEdited += delegate { edited++; };

                foreach (string key in SapFields.All)
                    Assert.IsTrue(panel.Field(key).Enabled && !panel.Field(key).ReadOnly, key);

                panel.Field(SapFields.Language).Text = "EN";
                Assert.AreEqual(1, edited);
            }
        }

        [TestMethod]
        public void SetSystems_and_refilter_do_not_raise_SystemPicked()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;
                panel.SystemList.SelectedIndex = 2;
                int raised = 0;
                panel.SystemPicked += delegate { raised++; };

                panel.SetSystems(Sample(), "");
                panel.SearchBox.Text = "Same";
                panel.SystemList.SelectedIndex = 0;
                raised = 0;
                panel.SetSystems(Sample(), "");
                panel.SearchBox.Text = "";

                Assert.AreEqual(0, raised);
            }
        }

        [TestMethod]
        public void Message_disables_automatic_and_shows_text()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;

                panel.SetSystems(null, "No landscape found");

                Assert.IsFalse(panel.AutomaticRadio.Enabled);
                Assert.IsFalse(panel.AutomaticMode);
                Assert.IsFalse(panel.SystemList.Enabled);
                Assert.IsFalse(panel.SearchBox.Enabled);
                Assert.IsTrue(panel.MessageLabel.Visible);
                Assert.AreEqual("No landscape found", panel.MessageLabel.Text);
                Assert.AreEqual(0, panel.SystemList.Items.Count);

                panel.SetSystems(Sample(), null);

                Assert.IsTrue(panel.AutomaticRadio.Enabled);
                Assert.IsFalse(panel.MessageLabel.Visible);
            }
        }

        [TestMethod]
        public void Warning_shows_text_and_keeps_automatic_usable()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.SetSystems(Sample(), "");
                panel.AutomaticRadio.Checked = true;

                panel.SetWarning("Could not read the shared SAP Logon file: X");

                Assert.IsTrue(panel.MessageLabel.Visible);
                Assert.AreEqual("Could not read the shared SAP Logon file: X", panel.MessageLabel.Text);
                Assert.IsTrue(panel.AutomaticRadio.Enabled);
                Assert.IsTrue(panel.AutomaticMode);
                Assert.IsTrue(panel.SystemList.Enabled);
                Assert.AreEqual(3, panel.SystemList.Items.Count);

                panel.SetWarning("");
                Assert.IsFalse(panel.MessageLabel.Visible);
                Assert.IsTrue(panel.AutomaticMode);
            }
        }

        [TestMethod]
        public void List_shows_name_id_and_server_but_items_stay_systems()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                List<SapSystem> systems = Sample();
                panel.SetSystems(systems, "");

                Assert.AreSame(systems[0], panel.SystemList.Items[0]);
                Assert.AreEqual("Same  [AAA | a.example.test]", panel.SystemList.GetItemText(panel.SystemList.Items[0]));
                Assert.AreEqual("Same  [BBB | b.example.test]", panel.SystemList.GetItemText(panel.SystemList.Items[1]));
            }
        }

        [TestMethod]
        public void Launch_hint_says_what_is_missing_and_follows_edits()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                Assert.AreEqual(
                    "Not launchable yet: enter System ID, Client, Application server + System number or Message server + Group.",
                    panel.LaunchHintLabel.Text);

                Dictionary<string, string> values = new Dictionary<string, string>();
                values[SapFields.SystemId] = "S25";
                values[SapFields.ApplicationServer] = "10.0.0.5";
                values[SapFields.SystemNumber] = "00";
                panel.LoadFields(values);
                Assert.AreEqual("Not launchable yet: enter Client.", panel.LaunchHintLabel.Text);

                panel.Field(SapFields.Client).Text = "100";
                Assert.AreEqual("Ready to launch.", panel.LaunchHintLabel.Text);

                panel.Field(SapFields.SystemNumber).Text = "";
                Assert.AreEqual("Not launchable yet: enter System number.", panel.LaunchHintLabel.Text);
            }
        }

        [TestMethod]
        public void Message_and_hint_labels_wrap_to_the_panel_width()
        {
            using (SapEntryPanel panel = new SapEntryPanel())
            {
                panel.Size = new System.Drawing.Size(300, 400);
                panel.SetWarning(new string('x', 400));

                Assert.IsTrue(panel.MessageLabel.MaximumSize.Width > 0 && panel.MessageLabel.MaximumSize.Width <= 300);
                Assert.IsTrue(panel.LaunchHintLabel.MaximumSize.Width > 0 && panel.LaunchHintLabel.MaximumSize.Width <= 300);

                panel.Size = new System.Drawing.Size(600, 400);
                Assert.IsTrue(panel.MessageLabel.MaximumSize.Width > 300);
            }
        }
    }
}
