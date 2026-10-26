using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class PluginParametersTests
    {
        [TestMethod]
        public void Roundtrip_keeps_paths_with_colon_and_spaces()
        {
            PluginParameters original = new PluginParameters();
            original.sapGuiPath = @"C:\Program Files (x86)\SAP\FrontEnd\SAPgui\";
            original.nBCPath = @"C:\Program Files (x86)\SAP\NWBC800\";

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.AreEqual(original.sapGuiPath, parsed.sapGuiPath);
            Assert.AreEqual(original.nBCPath, parsed.nBCPath);
            Assert.IsTrue(parsed.isMaxGui);
        }

        [TestMethod]
        public void Parse_empty_gives_defaults()
        {
            PluginParameters parsed = PluginParameters.Parse(string.Empty);

            Assert.IsTrue(parsed.isMaxGui);
            Assert.AreEqual(string.Empty, parsed.sapGuiPath);
            Assert.AreEqual(string.Empty, parsed.nBCPath);

            PluginParameters fromNull = PluginParameters.Parse(null);
            Assert.IsTrue(fromNull.isMaxGui);
            Assert.AreEqual(string.Empty, fromNull.sapGuiPath);
        }

        [TestMethod]
        public void Parse_ignores_garbage_parts()
        {
            PluginParameters parsed = PluginParameters.Parse("garbage;isMaxGui=False");

            Assert.IsFalse(parsed.isMaxGui);
            Assert.AreEqual(string.Empty, parsed.sapGuiPath);
        }

        [TestMethod]
        public void Roundtrip_keeps_maxgui_false()
        {
            PluginParameters original = new PluginParameters();
            original.isMaxGui = false;

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.IsFalse(parsed.isMaxGui);
        }

        [TestMethod]
        public void ColumnsSetUp_roundtrips()
        {
            PluginParameters original = new PluginParameters();
            Assert.IsFalse(original.columnsSetUp);
            Assert.IsFalse(PluginParameters.Parse(original.ToString()).columnsSetUp);

            original.columnsSetUp = true;
            original.sapGuiPath = @"C:\SAP\";

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.IsTrue(parsed.columnsSetUp);
            Assert.AreEqual(@"C:\SAP\", parsed.sapGuiPath);
            Assert.IsTrue(parsed.isMaxGui);
        }

        [TestMethod]
        public void Old_string_without_columnsSetUp_parses_as_false()
        {
            PluginParameters parsed = PluginParameters.Parse(
                @"isMaxGui=False;sapGuiPath=C:\SAP\;nBCPath=C:\NWBC\;someFutureKey=1");

            Assert.IsFalse(parsed.columnsSetUp);
            Assert.IsFalse(parsed.isMaxGui);
            Assert.AreEqual(@"C:\SAP\", parsed.sapGuiPath);
            Assert.AreEqual(@"C:\NWBC\", parsed.nBCPath);
        }

        [TestMethod]
        public void ToString_contains_all_keys()
        {
            PluginParameters p = new PluginParameters();
            p.columnsSetUp = true;
            string text = p.ToString();

            StringAssert.Contains(text, "isMaxGui=True");
            StringAssert.Contains(text, "sapGuiPath=");
            StringAssert.Contains(text, "nBCPath=");
            StringAssert.Contains(text, "columnsSetUp=True");
        }

        [TestMethod]
        public void Import_options_roundtrip()
        {
            PluginParameters original = new PluginParameters();
            original.importSetUserName = true;
            original.importUserName = "alice";
            original.importDialogWidth = 900;
            original.importDialogHeight = 600;
            original.importSplitter = 300;

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.IsTrue(parsed.importSetUserName);
            Assert.AreEqual("alice", parsed.importUserName);
            Assert.AreEqual(900, parsed.importDialogWidth);
            Assert.AreEqual(600, parsed.importDialogHeight);
            Assert.AreEqual(300, parsed.importSplitter);
            Assert.IsTrue(parsed.isMaxGui);
        }

        [TestMethod]
        public void User_name_with_separator_characters_roundtrips()
        {
            PluginParameters original = new PluginParameters();
            original.importUserName = "a;b=c%d";
            original.sapGuiPath = @"C:\SAP\";

            string text = original.ToString();
            PluginParameters parsed = PluginParameters.Parse(text);

            StringAssert.Contains(text, "importUserName=a%3Bb%3Dc%25d");
            Assert.AreEqual("a;b=c%d", parsed.importUserName);
            Assert.AreEqual(@"C:\SAP\", parsed.sapGuiPath);
        }

        [TestMethod]
        public void User_name_with_spaces_and_unicode_roundtrips()
        {
            PluginParameters original = new PluginParameters();
            original.importUserName = "Jane Doe \u00e9";

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.AreEqual("Jane Doe \u00e9", parsed.importUserName);
        }

        [TestMethod]
        public void User_name_with_line_breaks_roundtrips()
        {
            PluginParameters original = new PluginParameters();
            original.importUserName = "a\r\nb";

            PluginParameters parsed = PluginParameters.Parse(original.ToString());

            Assert.AreEqual("a\r\nb", parsed.importUserName);
        }

        [TestMethod]
        public void Old_string_without_import_keys_gives_import_defaults()
        {
            PluginParameters parsed = PluginParameters.Parse(
                @"isMaxGui=False;sapGuiPath=C:\SAP\;nBCPath=C:\NWBC\;columnsSetUp=True");

            Assert.IsFalse(parsed.importSetUserName);
            Assert.AreEqual(string.Empty, parsed.importUserName);
            Assert.AreEqual(0, parsed.importDialogWidth);
            Assert.AreEqual(0, parsed.importDialogHeight);
            Assert.AreEqual(0, parsed.importSplitter);
            Assert.IsFalse(parsed.isMaxGui);
            Assert.IsTrue(parsed.columnsSetUp);
        }

        [TestMethod]
        public void Garbage_layout_values_parse_as_zero()
        {
            PluginParameters parsed = PluginParameters.Parse(
                "importDialogWidth=abc;importDialogHeight=-5;importSplitter=");

            Assert.AreEqual(0, parsed.importDialogWidth);
            Assert.AreEqual(0, parsed.importDialogHeight);
            Assert.AreEqual(0, parsed.importSplitter);
        }

        [TestMethod]
        public void Unescape_keeps_unknown_percent_sequences()
        {
            Assert.AreEqual("50% off %zz ;", PluginParameters.UnescapeValue("50%25 off %zz %3b"));
        }

        [TestMethod]
        public void ToString_contains_import_keys()
        {
            string text = new PluginParameters().ToString();

            StringAssert.Contains(text, "importSetUserName=False");
            StringAssert.Contains(text, "importUserName=");
            StringAssert.Contains(text, "importDialogWidth=0");
            StringAssert.Contains(text, "importDialogHeight=0");
            StringAssert.Contains(text, "importSplitter=0");
        }
    }
}
