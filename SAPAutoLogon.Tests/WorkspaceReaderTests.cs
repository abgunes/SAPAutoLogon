using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class WorkspaceReaderTests
    {
        private static string Fixture()
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape.xml"));
        }

        private static List<SapFolder> ReadFixture()
        {
            string text = Fixture();
            List<SapSystem> systems = LandscapeReader.Read(new string[] { text });
            return WorkspaceReader.Read(new string[] { text }, systems);
        }

        private static SapFolder Find(SapFolder parent, string name)
        {
            return parent.Folders.FirstOrDefault(f => f.Name == name);
        }

        private static List<string> Names(IEnumerable<SapFolder> folders)
        {
            return folders.Select(f => f.Name).ToList();
        }

        private static List<string> Names(IEnumerable<SapSystem> systems)
        {
            return systems.Select(s => s.Name).ToList();
        }

        private static SapFolder Local(List<SapFolder> roots)
        {
            return roots[0];
        }

        [TestMethod]
        public void Roots_are_visible_workspaces_then_no_folder()
        {
            List<SapFolder> roots = ReadFixture();
            Assert.AreEqual(2, roots.Count);
            Assert.AreEqual("Local", roots[0].Name);
            Assert.AreEqual("(no folder)", roots[1].Name);
        }

        [TestMethod]
        public void Items_directly_under_workspace_are_root_systems()
        {
            SapFolder local = Local(ReadFixture());
            Assert.AreEqual(1, local.Systems.Count);
            Assert.AreEqual("X1 - Odd port", local.Systems[0].Name);
        }

        [TestMethod]
        public void Folders_keep_file_order_and_nesting()
        {
            SapFolder local = Local(ReadFixture());
            CollectionAssert.AreEqual(new string[] { "DE", "Demo" }, Names(local.Folders));
            SapFolder demo = Find(local, "Demo");
            CollectionAssert.AreEqual(new string[] { "Sub1", "Sub2", "Sub2", "Empty", "(unnamed)" }, Names(demo.Folders));
            Assert.AreEqual("Sub1.1", Find(demo, "Sub1").Folders[0].Name);
        }

        [TestMethod]
        public void Serviceid_with_Service_prefix_resolves()
        {
            SapFolder de = Find(Local(ReadFixture()), "DE");
            Assert.AreEqual(1, de.Systems.Count);
            Assert.AreEqual("10.0.0.5", de.Systems[0].ApplicationServer);
        }

        [TestMethod]
        public void Items_for_unknown_or_non_sapgui_services_are_skipped()
        {
            SapFolder de = Find(Local(ReadFixture()), "DE");
            Assert.AreEqual(1, de.Systems.Count);
        }

        [TestMethod]
        public void Same_service_in_two_folders_is_the_same_object()
        {
            SapFolder local = Local(ReadFixture());
            SapFolder de = Find(local, "DE");
            SapFolder sub11 = Find(Find(Find(local, "Demo"), "Sub1"), "Sub1.1");
            Assert.AreSame(de.Systems[0], sub11.Systems[1]);
        }

        [TestMethod]
        public void Systems_in_a_folder_keep_item_order()
        {
            SapFolder sub11 = Find(Find(Find(Local(ReadFixture()), "Demo"), "Sub1"), "Sub1.1");
            CollectionAssert.AreEqual(new string[] { "S26 - SNC", "S25 - Development" }, Names(sub11.Systems));
        }

        [TestMethod]
        public void Hidden_nodes_and_workspaces_are_not_shown()
        {
            List<SapFolder> roots = ReadFixture();
            Assert.IsFalse(roots.Any(r => r.Name == "Secret"));
            Assert.IsNull(Find(Local(roots), "Hidden"));
        }

        [TestMethod]
        public void System_only_in_hidden_node_is_not_shown_anywhere()
        {
            string xml = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" uuid=\"a\" name=\"A\" server=\"10.0.0.1:3200\"/>"
                + "<Service type=\"SAPGUI\" uuid=\"b\" name=\"B\" server=\"10.0.0.2:3200\"/>"
                + "</Services><Workspaces><Workspace uuid=\"w\" name=\"W\">"
                + "<Node uuid=\"n\" name=\"N\" hidden=\"1\"><Item uuid=\"i\" serviceid=\"b\"/></Node>"
                + "</Workspace></Workspaces></Landscape>";
            List<SapSystem> systems = LandscapeReader.Read(new string[] { xml });
            List<SapFolder> roots = WorkspaceReader.Read(new string[] { xml }, systems);
            Assert.AreEqual(2, roots.Count);
            Assert.AreEqual("W", roots[0].Name);
            Assert.AreEqual(0, roots[0].Systems.Count);
            Assert.AreEqual(0, roots[0].Folders.Count);
            Assert.AreEqual("(no folder)", roots[1].Name);
            CollectionAssert.AreEqual(new string[] { "A" }, Names(roots[1].Systems));
        }

        [TestMethod]
        public void Unreferenced_systems_go_to_no_folder()
        {
            SapFolder none = ReadFixture()[1];
            CollectionAssert.AreEqual(new string[] { "Z - Broken group" }, Names(none.Systems));
        }

        [TestMethod]
        public void Empty_folder_is_kept()
        {
            SapFolder empty = Find(Find(Local(ReadFixture()), "Demo"), "Empty");
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Systems.Count);
            Assert.AreEqual(0, empty.Folders.Count);
        }

        [TestMethod]
        public void Empty_name_becomes_unnamed()
        {
            SapFolder unnamed = Find(Find(Local(ReadFixture()), "Demo"), "(unnamed)");
            Assert.AreEqual("X1 - Odd port", unnamed.Systems[0].Name);
        }

        [TestMethod]
        public void Duplicate_folder_names_under_same_parent_stay_separate_in_tree()
        {
            SapFolder demo = Find(Local(ReadFixture()), "Demo");
            List<SapFolder> subs = demo.Folders.Where(f => f.Name == "Sub2").ToList();
            Assert.AreEqual(2, subs.Count);
            Assert.AreEqual("10.0.0.7", subs[0].Systems[0].ApplicationServer);
            Assert.AreEqual("Q26 - No port", subs[1].Systems[0].Name);
        }

        [TestMethod]
        public void Count_systems_is_recursive()
        {
            SapFolder local = Local(ReadFixture());
            SapFolder demo = Find(local, "Demo");
            Assert.AreEqual(8, local.CountSystems());
            Assert.AreEqual(6, demo.CountSystems());
            Assert.AreEqual(0, Find(demo, "Empty").CountSystems());
        }

        [TestMethod]
        public void No_workspaces_element_gives_single_no_folder_root_with_all_systems()
        {
            string xml = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" uuid=\"a\" name=\"A\" server=\"10.0.0.1:3200\"/>"
                + "<Service type=\"SAPGUI\" uuid=\"b\" name=\"B\" server=\"10.0.0.2:3200\"/>"
                + "</Services></Landscape>";
            List<SapSystem> systems = LandscapeReader.Read(new string[] { xml });
            List<SapFolder> roots = WorkspaceReader.Read(new string[] { xml }, systems);
            Assert.AreEqual(1, roots.Count);
            Assert.AreEqual("(no folder)", roots[0].Name);
            Assert.AreEqual(2, roots[0].Systems.Count);

            string empty = "<Landscape><Services/></Landscape>";
            List<SapSystem> none = LandscapeReader.Read(new string[] { empty });
            Assert.AreEqual(0, WorkspaceReader.Read(new string[] { empty }, none).Count);
        }

        [TestMethod]
        public void Workspace_without_name_is_called_Workspace()
        {
            string xml = "<Landscape><Workspaces><Workspace uuid=\"w\"/></Workspaces></Landscape>";
            List<SapFolder> roots = WorkspaceReader.Read(new string[] { xml }, new List<SapSystem>());
            Assert.AreEqual(1, roots.Count);
            Assert.AreEqual("Workspace", roots[0].Name);
        }

        [TestMethod]
        public void Null_arguments_give_empty_list()
        {
            Assert.AreEqual(0, WorkspaceReader.Read(null, null).Count);
        }
    }
}
