using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LandscapeReaderTests
    {
        private static string Fixture()
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape.xml"));
        }

        private static List<SapSystem> ReadFixture()
        {
            return LandscapeReader.Read(new string[] { Fixture() });
        }

        private static SapSystem Find(List<SapSystem> list, string name)
        {
            return list.First(s => s.Name == name);
        }

        [TestMethod]
        public void Lists_only_SAPGUI_services_sorted_by_name()
        {
            List<SapSystem> list = ReadFixture();
            Assert.AreEqual(7, list.Count);
            Assert.AreEqual("Q25 - Group", list[0].Name);
            Assert.IsFalse(list.Any(s => s.Name == "Ref" || s.Name == "Cmd"));
        }

        [TestMethod]
        public void Direct_server_is_split_into_host_and_instance()
        {
            SapSystem s = Find(ReadFixture(), "S25 - Development");
            Assert.AreEqual("10.0.0.5", s.ApplicationServer);
            Assert.AreEqual("00", s.SystemNumber);
            Assert.IsFalse(s.IsGroup);
            Assert.AreEqual("", s.MessageServer);
            Assert.AreEqual("", s.Group);
            Assert.AreEqual("100", s.Client);
            Assert.AreEqual("EN", s.Language);
            Assert.AreEqual("ALICE", s.User);
            Assert.AreEqual("/H/router.example.test/H/10.0.0.1", s.SapRouterString);
            Assert.AreEqual("", s.SncType);
        }

        [TestMethod]
        public void Group_system_uses_message_server_and_ignores_mode()
        {
            SapSystem s = Find(ReadFixture(), "Q25 - Group");
            Assert.IsTrue(s.IsGroup);
            Assert.AreEqual("ms1.example.test", s.MessageServer);
            Assert.AreEqual("3600", s.MessageServerPort);
            Assert.AreEqual("PUBLIC", s.Group);
            Assert.AreEqual("Q25", s.SystemId);
            Assert.AreEqual("", s.ApplicationServer);
            Assert.AreEqual("", s.SystemNumber);
            Assert.AreEqual("/H/router.example.test/S/3299", s.SapRouterString);
        }

        [TestMethod]
        public void Message_server_without_port_gives_empty_port()
        {
            SapSystem s = Find(ReadFixture(), "Q26 - No port");
            Assert.AreEqual("", s.MessageServerPort);
        }

        [TestMethod]
        public void Snc_entry_keeps_name_and_level()
        {
            SapSystem s = Find(ReadFixture(), "S26 - SNC");
            Assert.AreEqual("p:CN=S26", s.SncName);
            Assert.AreEqual("3", s.SncType);
            Assert.AreEqual("01", s.SystemNumber);
        }

        [TestMethod]
        public void Server_in_other_format_goes_whole_into_application_server()
        {
            SapSystem s = Find(ReadFixture(), "X1 - Odd port");
            Assert.AreEqual("odd.example.test:3300", s.ApplicationServer);
            Assert.AreEqual("", s.SystemNumber);
        }

        [TestMethod]
        public void Unresolved_msid_is_treated_as_direct_without_throwing()
        {
            SapSystem s = Find(ReadFixture(), "Z - Broken group");
            Assert.IsFalse(s.IsGroup);
            Assert.AreEqual("GRP", s.ApplicationServer);
        }

        [TestMethod]
        public void Duplicate_names_are_kept_as_separate_systems()
        {
            List<SapSystem> dups = ReadFixture().Where(s => s.Name == "S25 - Development").ToList();
            Assert.AreEqual(2, dups.Count);
            Assert.AreEqual("10.0.0.5", dups[0].ApplicationServer);
            Assert.AreEqual("10.0.0.7", dups[1].ApplicationServer);
        }

        [TestMethod]
        public void Uuid_is_read_from_service()
        {
            List<SapSystem> dups = ReadFixture().Where(s => s.Name == "S25 - Development").ToList();
            Assert.AreEqual("s-1", dups[0].Uuid);
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void Invalid_xml_throws_XmlException()
        {
            LandscapeReader.Read(new string[] { "<Landscape><Services>" });
        }

        [TestMethod]
        public void Second_document_can_supply_message_server_for_first()
        {
            string first = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" uuid=\"a\" name=\"G1\" systemid=\"OLD\" server=\"GRP\" msid=\"ms-9\"/>"
                + "</Services></Landscape>";
            string second = "<Landscape><Messageservers>"
                + "<Messageserver uuid=\"ms-9\" name=\"M9\" host=\"ms9.example.test\" port=\"3601\"/>"
                + "</Messageservers></Landscape>";
            List<SapSystem> list = LandscapeReader.Read(new string[] { first, second });
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list[0].IsGroup);
            Assert.AreEqual("ms9.example.test", list[0].MessageServer);
            Assert.AreEqual("M9", list[0].SystemId);
        }

        [TestMethod]
        public void Same_service_uuid_in_two_documents_gives_one_system()
        {
            string first = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" uuid=\"u1\" name=\"Old name\" systemid=\"AAA\" server=\"10.0.0.5:3200\" client=\"100\"/>"
                + "</Services></Landscape>";
            string second = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" uuid=\"u1\" name=\"New name\" systemid=\"BBB\" server=\"10.0.0.6:3201\" client=\"200\"/>"
                + "</Services></Landscape>";
            List<SapSystem> list = LandscapeReader.Read(new string[] { first, second });
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("New name", list[0].Name);
            Assert.AreEqual("BBB", list[0].SystemId);
            Assert.AreEqual("10.0.0.6", list[0].ApplicationServer);
            Assert.AreEqual("200", list[0].Client);
        }

        [TestMethod]
        public void Services_without_uuid_are_all_kept()
        {
            string xml = "<Landscape><Services>"
                + "<Service type=\"SAPGUI\" name=\"N1\" server=\"10.0.0.5:3200\"/>"
                + "<Service type=\"SAPGUI\" name=\"N2\" server=\"10.0.0.6:3201\"/>"
                + "</Services></Landscape>";
            List<SapSystem> list = LandscapeReader.Read(new string[] { xml, xml });
            Assert.AreEqual(4, list.Count);
        }
    }
}
