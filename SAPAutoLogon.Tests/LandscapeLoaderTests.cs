using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LandscapeLoaderTests
    {
        private const string LocalPath = @"C:\u\local.xml";
        private const string GlobalPath = @"C:\sap\global.xml";

        private const string GlobalXml =
            "<Landscape><Messageservers><Messageserver uuid=\"ms-g\" name=\"G25\" host=\"msg.example.test\" port=\"3600\"/></Messageservers></Landscape>";

        private const string LocalXml =
            "<Landscape><Services><Service type=\"SAPGUI\" uuid=\"s1\" name=\"Grp\" msid=\"ms-g\" server=\"PUBLIC\"/></Services></Landscape>";

        private static string WithInclude(string includeUrl)
        {
            return "<Landscape><Includes><Include url=\"" + includeUrl + "\"/></Includes>"
                + "<Services><Service type=\"SAPGUI\" uuid=\"s1\" name=\"Grp\" msid=\"ms-g\" server=\"PUBLIC\"/></Services></Landscape>";
        }

        private sealed class Reader
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Calls = new List<string>();

            public string Read(string path)
            {
                Calls.Add(path);
                string text;
                if (!Files.TryGetValue(path, out text)) throw new IOException("not found: " + path);
                return text;
            }
        }

        [TestMethod]
        public void Merges_local_and_global_by_uuid()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;
            r.Files[GlobalPath] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.IsTrue(res.Systems[0].IsGroup);
            Assert.AreEqual("msg.example.test", res.Systems[0].MessageServer);
        }

        [TestMethod]
        public void Load_fills_folders_from_workspaces()
        {
            Reader r = new Reader();
            r.Files[LocalPath] =
                "<Landscape><Services><Service type=\"SAPGUI\" uuid=\"s1\" name=\"Grp\" msid=\"ms-g\" server=\"PUBLIC\"/></Services>"
                + "<Workspaces><Workspace name=\"Local\"><Node name=\"DE\"><Item serviceid=\"s1\"/></Node></Workspace></Workspaces></Landscape>";

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Folders.Count);
            Assert.AreEqual("Local", res.Folders[0].Name);
            Assert.AreEqual(res.Systems[0].Name, res.Folders[0].Folders[0].Systems[0].Name);
        }

        [TestMethod]
        public void Load_without_workspaces_gives_no_folder_root()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.AreEqual(1, res.Folders.Count);
            Assert.AreEqual("(no folder)", res.Folders[0].Name);
        }

        [TestMethod]
        public void Error_result_has_empty_folders()
        {
            Reader r = new Reader();

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreNotEqual("", res.Error);
            Assert.IsNotNull(res.Folders);
            Assert.AreEqual(0, res.Folders.Count);
        }

        [TestMethod]
        public void Follows_include_once()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude("file:///C:/sap/global.xml");
            r.Files[GlobalPath] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, r.Calls.Count(c => string.Equals(c, GlobalPath, StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(res.Systems[0].IsGroup);
        }

        [TestMethod]
        public void Missing_global_file_still_returns_local_systems_with_empty_error()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.IsTrue(res.Systems.Count > 0);
        }

        [TestMethod]
        public void Missing_local_file_reports_error()
        {
            Reader r = new Reader();
            r.Files[GlobalPath] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual(0, res.Systems.Count);
            Assert.AreNotEqual("", res.Error);
        }

        [TestMethod]
        public void Invalid_xml_reports_error_and_does_not_throw()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = "<not xml";

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual(0, res.Systems.Count);
            Assert.AreNotEqual("", res.Error);
        }

        [TestMethod]
        public void Empty_paths_report_not_found()
        {
            Reader r = new Reader();

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles(), r.Read);

            Assert.AreNotEqual("", res.Error);
            Assert.AreEqual(0, res.Systems.Count);
            Assert.AreEqual(0, r.Calls.Count);
        }

        [TestMethod]
        public void Invalid_xml_in_global_file_is_skipped_with_a_warning()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;
            r.Files[GlobalPath] = "<broken";

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.AreEqual(1, res.Warnings.Count);
            StringAssert.Contains(res.Warnings[0], GlobalPath);
        }

        [TestMethod]
        public void Unreadable_global_file_gives_a_warning_naming_the_path()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.AreEqual(1, res.Warnings.Count);
            Assert.AreEqual("Could not read the shared SAP Logon file: " + GlobalPath, res.Warnings[0]);
        }

        [TestMethod]
        public void Include_with_invalid_xml_gives_a_warning()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude(@"C:\i\broken.xml");
            r.Files[@"C:\i\broken.xml"] = "<not xml";

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.AreEqual(1, res.Warnings.Count);
            Assert.AreEqual(@"Could not read the included SAP Logon file: C:\i\broken.xml", res.Warnings[0]);
        }

        [TestMethod]
        public void Clean_load_has_no_warnings()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude(@"C:\i\inc.xml");
            r.Files[@"C:\i\inc.xml"] = GlobalXml;
            r.Files[GlobalPath] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.IsNotNull(res.Warnings);
            Assert.AreEqual(0, res.Warnings.Count);
        }

        [TestMethod]
        public void Successful_load_without_systems_gives_a_warning()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = "<Landscape/>";

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(0, res.Systems.Count);
            CollectionAssert.AreEqual(new[] { "No SAP GUI systems found in the SAP Logon files." }, res.Warnings);
        }

        [TestMethod]
        public void Failed_load_has_error_and_warnings_list_is_never_null()
        {
            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles(), new Reader().Read);

            Assert.AreNotEqual("", res.Error);
            Assert.IsNotNull(res.Warnings);
            Assert.AreEqual(0, res.Warnings.Count);
            Assert.IsNotNull(new LandscapeLoadResult().Warnings);
        }

        [TestMethod]
        public void Http_include_is_passed_through_unchanged()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude("https://share.example.test/landscape.xml");
            r.Files["https://share.example.test/landscape.xml"] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            CollectionAssert.Contains(r.Calls, "https://share.example.test/landscape.xml");
            Assert.IsTrue(res.Systems[0].IsGroup);
        }

        [TestMethod]
        public void Malformed_include_url_does_not_throw()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude("file://");

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, res.Systems.Count);
            Assert.AreEqual(1, res.Warnings.Count);
            StringAssert.Contains(res.Warnings[0], "file://");
        }

        [TestMethod]
        public void Include_depth_is_limited_to_three()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude(@"C:\i\1.xml");
            r.Files[@"C:\i\1.xml"] = WithInclude(@"C:\i\2.xml");
            r.Files[@"C:\i\2.xml"] = WithInclude(@"C:\i\3.xml");
            r.Files[@"C:\i\3.xml"] = WithInclude(@"C:\i\4.xml");
            r.Files[@"C:\i\4.xml"] = GlobalXml;

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            CollectionAssert.Contains(r.Calls, @"C:\i\3.xml");
            CollectionAssert.DoesNotContain(r.Calls, @"C:\i\4.xml");
        }

        [TestMethod]
        public void Include_cycles_terminate()
        {
            Reader r = new Reader();
            r.Files[LocalPath] = WithInclude(@"C:\i\a.xml");
            r.Files[@"C:\i\a.xml"] = WithInclude(@"C:\u\local.xml");

            LandscapeLoadResult res = LandscapeLoader.Load(new LandscapeFiles { LocalPath = LocalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual(1, r.Calls.Count(c => c == LocalPath));
        }

        [TestMethod]
        public void Global_fixture_loads()
        {
            string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape-global.xml"));
            Reader r = new Reader();
            r.Files[LocalPath] = LocalXml;
            r.Files[GlobalPath] = text;

            LandscapeLoadResult res = LandscapeLoader.Load(
                new LandscapeFiles { LocalPath = LocalPath, GlobalPath = GlobalPath }, r.Read);

            Assert.AreEqual("", res.Error);
            Assert.AreEqual("msg.example.test", res.Systems[0].MessageServer);
        }
    }
}
