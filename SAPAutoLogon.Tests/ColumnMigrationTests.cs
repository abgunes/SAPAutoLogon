using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class ColumnMigrationTests
    {
        private static ColumnInfo Col(bool plugin, string name, int width)
        {
            ColumnInfo c = new ColumnInfo();
            c.IsPluginColumn = plugin;
            c.Name = name;
            c.Width = width;
            return c;
        }

        private static void AssertAppend(ColumnChange change, string name)
        {
            Assert.IsTrue(change.Append);
            Assert.AreEqual(-1, change.RenameIndex);
            Assert.AreEqual(name, change.NewName);
            Assert.AreEqual(ColumnMigration.DefaultWidth, change.AppendWidth);
        }

        private static void AssertRename(ColumnChange change, int index, string name, int width)
        {
            Assert.IsFalse(change.Append);
            Assert.AreEqual(index, change.RenameIndex);
            Assert.AreEqual(name, change.NewName);
            Assert.AreEqual(width, change.RenameWidth);
        }

        [TestMethod]
        public void Column_names_and_default_width()
        {
            Assert.AreEqual(LaunchText.GuiColumnName, ColumnMigration.GuiColumn);
            Assert.AreEqual(LaunchText.BusinessClientColumnName, ColumnMigration.BusinessClientColumn);
            Assert.AreEqual(140, ColumnMigration.DefaultWidth);
        }

        [TestMethod]
        public void Empty_list_appends_both()
        {
            List<ColumnChange> changes = ColumnMigration.Plan(new List<ColumnInfo>());

            Assert.AreEqual(2, changes.Count);
            AssertAppend(changes[0], "SAP GUI");
            AssertAppend(changes[1], "Business Client");
            Assert.AreEqual(140, changes[0].AppendWidth);
            Assert.AreEqual(140, changes[1].AppendWidth);
        }

        [TestMethod]
        public void Null_input_appends_both()
        {
            List<ColumnChange> changes = ColumnMigration.Plan(null);

            Assert.AreEqual(2, changes.Count);
            AssertAppend(changes[0], ColumnMigration.GuiColumn);
            AssertAppend(changes[1], ColumnMigration.BusinessClientColumn);
        }

        [TestMethod]
        public void Old_columns_are_renamed_in_place_and_widened_to_the_default()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(false, "", 200));
            existing.Add(Col(true, "Gui", 92));
            existing.Add(Col(true, "NBC", 92));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(2, changes.Count);
            // 92 is narrower than the default 140, so the renamed columns get 140
            AssertRename(changes[0], 1, "SAP GUI", 140);
            AssertRename(changes[1], 2, "Business Client", 140);
        }

        [TestMethod]
        public void Renamed_column_keeps_a_width_that_is_already_wide_enough()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(true, "Gui", 200));
            existing.Add(Col(true, "NBC", 140));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(2, changes.Count);
            AssertRename(changes[0], 0, "SAP GUI", 200);
            AssertRename(changes[1], 1, "Business Client", 140);
        }

        [TestMethod]
        public void Renamed_column_just_below_the_default_is_widened()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(true, "Gui", 139));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            AssertRename(changes[0], 0, "SAP GUI", 140);
        }

        [TestMethod]
        public void Already_present_columns_cause_no_change()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(false, "", 200));
            existing.Add(Col(true, "Business Client", 80));
            existing.Add(Col(true, "SAP GUI", 80));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(0, changes.Count);
        }

        [TestMethod]
        public void Only_missing_one_is_appended()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(false, "", 200));
            existing.Add(Col(true, "SAP GUI", 80));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(1, changes.Count);
            AssertAppend(changes[0], "Business Client");
        }

        [TestMethod]
        public void Non_plugin_column_with_same_name_is_ignored()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(false, "Gui", 100));
            existing.Add(Col(false, "SAP GUI", 100));
            existing.Add(Col(false, "NBC", 100));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(2, changes.Count);
            AssertAppend(changes[0], "SAP GUI");
            AssertAppend(changes[1], "Business Client");
        }

        [TestMethod]
        public void Column_is_not_claimed_twice()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(true, "Gui", 92));
            existing.Add(Col(true, "Gui", 92));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(2, changes.Count);
            AssertRename(changes[0], 0, "SAP GUI", 140);
            AssertAppend(changes[1], "Business Client");
        }

        [TestMethod]
        public void Name_match_is_case_sensitive()
        {
            List<ColumnInfo> existing = new List<ColumnInfo>();
            existing.Add(Col(true, "sap gui", 92));
            existing.Add(Col(true, "nbc", 92));

            List<ColumnChange> changes = ColumnMigration.Plan(existing);

            Assert.AreEqual(2, changes.Count);
            AssertAppend(changes[0], "SAP GUI");
            AssertAppend(changes[1], "Business Client");
        }
    }
}
