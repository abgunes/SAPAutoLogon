using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class ImportTextTests
    {
        [TestMethod]
        public void Confirmation_singular_and_plural()
        {
            Assert.AreEqual("Import 6 systems into group 'SAP'?", ImportText.Confirmation(6, "SAP"));
            Assert.AreEqual("Import 1 system into group 'SAP'?", ImportText.Confirmation(1, "SAP"));
            Assert.AreEqual("Import 0 systems into group 'SAP'?", ImportText.Confirmation(0, "SAP"));
        }

        [TestMethod]
        public void Summary_text_exact()
        {
            Assert.AreEqual("6 entries added, 0 skipped (already present), 6 groups created", ImportText.Summary(6, 0, 6));
            Assert.AreEqual("1 entries added, 2 skipped (already present), 0 groups created", ImportText.Summary(1, 2, 0));
        }

        [TestMethod]
        public void Header_subtitle_names_the_group()
        {
            Assert.AreEqual(
                "Choose an SAP Logon folder. Its subfolders become groups and its systems become entries in group 'SAP'.",
                ImportText.HeaderSubtitle("SAP"));
            Assert.AreEqual(
                "Choose an SAP Logon folder. Its subfolders become groups and its systems become entries in group ''.",
                ImportText.HeaderSubtitle(null));
        }

        [TestMethod]
        public void Folder_label_has_count()
        {
            Assert.AreEqual("Demo (6)", ImportText.FolderLabel("Demo", 6));
            Assert.AreEqual("Empty (0)", ImportText.FolderLabel("Empty", 0));
            Assert.AreEqual(" (0)", ImportText.FolderLabel(null, 0));
        }

        [TestMethod]
        public void Join_path_uses_separator()
        {
            Assert.AreEqual("Local > Demo > Sub1", ImportText.JoinPath(new string[] { "Local", "Demo", "Sub1" }));
            Assert.AreEqual("", ImportText.JoinPath(null));
            Assert.AreEqual("", ImportText.JoinPath(new string[0]));
        }

        [TestMethod]
        public void List_caption_variants()
        {
            Assert.AreEqual("Systems in the selected folder", ImportText.ListCaption(null, 0, 0));
            Assert.AreEqual("Systems in the selected folder", ImportText.ListCaption("", 3, 3));
            Assert.AreEqual("Systems in Local > Demo (none directly in this folder)",
                ImportText.ListCaption("Local > Demo", 0, 0));
            Assert.AreEqual("Systems in Local > Demo > Sub1", ImportText.ListCaption("Local > Demo > Sub1", 2, 2));
            Assert.AreEqual("Systems in Local > Demo > Sub1 > Sub1.1 (1 of 2 match the search)",
                ImportText.ListCaption("Local > Demo > Sub1 > Sub1.1", 1, 2));
        }

        [TestMethod]
        public void Preview_text_exact()
        {
            Assert.AreEqual("6 entries and 6 groups will be created.", ImportText.Preview(6, 6, 0));
            Assert.AreEqual("0 entries and 0 groups will be created, 6 entries already present will be skipped.",
                ImportText.Preview(0, 0, 6));
            Assert.AreEqual("1 entries and 0 groups will be created, 5 entries already present will be skipped.",
                ImportText.Preview(1, 0, 5));
        }

        [TestMethod]
        public void Server_column_shows_group_for_group_systems()
        {
            SapSystem group = new SapSystem();
            group.IsGroup = true;
            group.Group = "PUBLIC";
            group.ApplicationServer = "ignored";
            Assert.AreEqual("PUBLIC", ImportText.ServerColumn(group));

            SapSystem direct = new SapSystem();
            direct.ApplicationServer = "10.0.0.5";
            direct.Group = "ignored";
            Assert.AreEqual("10.0.0.5", ImportText.ServerColumn(direct));

            Assert.AreEqual("", ImportText.ServerColumn(null));
        }

        [TestMethod]
        public void Menu_enabled_only_when_open_writable_group_outside_recycle_bin()
        {
            Assert.IsTrue(ImportText.MenuEnabled(true, false, true, false));
            Assert.IsFalse(ImportText.MenuEnabled(false, false, true, false));
            Assert.IsFalse(ImportText.MenuEnabled(true, true, true, false));
            Assert.IsFalse(ImportText.MenuEnabled(true, false, false, false));
            Assert.IsFalse(ImportText.MenuEnabled(true, false, true, true));
        }

        [TestMethod]
        public void User_name_preview_exact()
        {
            Assert.AreEqual("User name 'alice' will be set for 5 entries that have no user in SAP Logon.",
                ImportText.UserNamePreview("alice", 5));
        }
    }
}
