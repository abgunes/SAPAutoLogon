using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class FieldMapperTests
    {
        private static SapSystem Direct()
        {
            return new SapSystem
            {
                Name = "DEV direct",
                SystemId = "DEV",
                ApplicationServer = "10.0.0.5",
                SystemNumber = "00",
                SapRouterString = "/H/router/S/3299",
                SncName = "p:CN=DEV",
                SncType = "1",
                Client = "100",
                Language = "EN",
                User = "ALICE"
            };
        }

        private static SapSystem Grouped()
        {
            return new SapSystem
            {
                Name = "PRD group",
                SystemId = "PRD",
                MessageServer = "msg.example.test",
                MessageServerPort = "3601",
                Group = "PUBLIC"
            };
        }

        [TestMethod]
        public void Direct_system_maps_all_fields_and_clears_group_fields()
        {
            MappedEntry m = FieldMapper.Map(Direct());

            Assert.AreEqual("DEV", m.Fields["systemId"]);
            Assert.AreEqual("10.0.0.5", m.Fields["applicationServer"]);
            Assert.AreEqual("00", m.Fields["systemNumber"]);
            Assert.AreEqual("", m.Fields["messageServer"]);
            Assert.AreEqual("", m.Fields["group"]);
            Assert.AreEqual("", m.Fields["messageServerPort"]);
            Assert.AreEqual("/H/router/S/3299", m.Fields["sapRouterString"]);
            Assert.AreEqual("p:CN=DEV", m.Fields["sncName"]);
            Assert.AreEqual("1", m.Fields["sncType"]);
            Assert.AreEqual("100", m.Fields["client"]);
            Assert.AreEqual("EN", m.Fields["language"]);
            Assert.AreEqual(SapFields.FromLandscape.Length, m.Fields.Count);
            Assert.IsFalse(m.Fields.ContainsKey("transaction"));
            foreach (string key in SapFields.FromLandscape)
                Assert.IsTrue(m.Fields.ContainsKey(key), key);
        }

        [TestMethod]
        public void Group_system_clears_application_server_and_number()
        {
            MappedEntry m = FieldMapper.Map(Grouped());

            Assert.AreEqual("", m.Fields["applicationServer"]);
            Assert.AreEqual("", m.Fields["systemNumber"]);
            Assert.AreEqual("msg.example.test", m.Fields["messageServer"]);
            Assert.AreEqual("3601", m.Fields["messageServerPort"]);
            Assert.AreEqual("PUBLIC", m.Fields["group"]);
            Assert.AreEqual(SapFields.FromLandscape.Length, m.Fields.Count);
        }

        [TestMethod]
        public void User_name_is_null_when_sap_logon_has_none()
        {
            SapSystem s = Direct();
            s.User = "";
            Assert.IsNull(FieldMapper.Map(s).UserName);

            s.User = null;
            Assert.IsNull(FieldMapper.Map(s).UserName);
        }

        [TestMethod]
        public void User_name_is_copied_when_present()
        {
            Assert.AreEqual("ALICE", FieldMapper.Map(Direct()).UserName);
        }

        [TestMethod]
        public void Title_is_system_name()
        {
            Assert.AreEqual("DEV direct", FieldMapper.Map(Direct()).Title);
        }

        [TestMethod]
        public void ApplyDefaults_fills_empty_client_and_language()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.Client] = "";
            f[SapFields.Language] = "";

            FieldMapper.ApplyDefaults(f);

            Assert.AreEqual("100", f[SapFields.Client]);
            Assert.AreEqual("EN", f[SapFields.Language]);
            Assert.AreEqual("100", FieldMapper.DefaultClient);
            Assert.AreEqual("EN", FieldMapper.DefaultLanguage);
        }

        [TestMethod]
        public void ApplyDefaults_keeps_existing_values()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.Client] = "200";
            f[SapFields.Language] = "DE";

            FieldMapper.ApplyDefaults(f);

            Assert.AreEqual("200", f[SapFields.Client]);
            Assert.AreEqual("DE", f[SapFields.Language]);
        }

        [TestMethod]
        public void ApplyDefaults_treats_whitespace_and_missing_as_empty()
        {
            Dictionary<string, string> whitespace = new Dictionary<string, string>();
            whitespace[SapFields.Client] = "  ";
            whitespace[SapFields.Language] = "\t";
            FieldMapper.ApplyDefaults(whitespace);
            Assert.AreEqual("100", whitespace[SapFields.Client]);
            Assert.AreEqual("EN", whitespace[SapFields.Language]);

            Dictionary<string, string> missing = new Dictionary<string, string>();
            FieldMapper.ApplyDefaults(missing);
            Assert.AreEqual("100", missing[SapFields.Client]);
            Assert.AreEqual("EN", missing[SapFields.Language]);

            Dictionary<string, string> nulls = new Dictionary<string, string>();
            nulls[SapFields.Client] = null;
            nulls[SapFields.Language] = null;
            FieldMapper.ApplyDefaults(nulls);
            Assert.AreEqual("100", nulls[SapFields.Client]);
            Assert.AreEqual("EN", nulls[SapFields.Language]);
        }

        [TestMethod]
        public void ApplyDefaults_does_not_touch_other_keys()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.SystemId] = "DEV";
            f[SapFields.ApplicationServer] = "";
            f[SapFields.Client] = "";

            FieldMapper.ApplyDefaults(f);

            Assert.AreEqual("DEV", f[SapFields.SystemId]);
            Assert.AreEqual("", f[SapFields.ApplicationServer]);
            Assert.AreEqual(4, f.Count);
        }

        [TestMethod]
        public void ApplyDefaults_null_is_noop()
        {
            FieldMapper.ApplyDefaults(null);
        }

        [TestMethod]
        public void Map_still_returns_raw_values()
        {
            MappedEntry m = FieldMapper.Map(Grouped());

            Assert.AreEqual("", m.Fields[SapFields.Client]);
            Assert.AreEqual("", m.Fields[SapFields.Language]);
        }

        [TestMethod]
        public void Default_user_name_is_trimmed()
        {
            Assert.AreEqual("alice", FieldMapper.NormalizeDefaultUserName("  alice "));
            Assert.AreEqual("alice", FieldMapper.ApplyDefaultUserName(null, " alice "));
        }

        [TestMethod]
        public void Blank_default_user_name_is_null()
        {
            Assert.IsNull(FieldMapper.NormalizeDefaultUserName("   "));
            Assert.IsNull(FieldMapper.NormalizeDefaultUserName(null));
            Assert.IsNull(FieldMapper.NormalizeDefaultUserName(""));
            Assert.IsNull(FieldMapper.ApplyDefaultUserName(null, "  "));
            Assert.IsNull(FieldMapper.ApplyDefaultUserName("", null));
        }

        [TestMethod]
        public void Sap_logon_user_wins_over_default()
        {
            Assert.AreEqual("ALICE", FieldMapper.ApplyDefaultUserName("ALICE", "bob"));
            Assert.AreEqual(" BOB ", FieldMapper.ApplyDefaultUserName(" BOB ", "x"));
        }

        [TestMethod]
        public void Default_user_name_keeps_inner_spaces_and_special_characters()
        {
            Assert.AreEqual("a b;c=d%é", FieldMapper.ApplyDefaultUserName(null, "a b;c=d%é"));
        }
    }
}
