using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class ExePathTests
    {
        [TestMethod]
        public void Combines_folder_with_and_without_trailing_backslash()
        {
            Assert.AreEqual(@"C:\SAP\SAPgui\sapshcut.exe", ExePath.Combine(@"C:\SAP\SAPgui\", "sapshcut.exe"));
            Assert.AreEqual(@"C:\SAP\SAPgui\sapshcut.exe", ExePath.Combine(@"C:\SAP\SAPgui", "sapshcut.exe"));
        }

        [TestMethod]
        public void Empty_folder_gives_empty_path()
        {
            Assert.AreEqual("", ExePath.Combine("", "sapshcut.exe"));
            Assert.AreEqual("", ExePath.Combine("   ", "sapshcut.exe"));
            Assert.AreEqual("", ExePath.Combine(null, "sapshcut.exe"));
        }

        [TestMethod]
        public void Surrounding_spaces_and_quotes_are_removed()
        {
            Assert.AreEqual(@"C:\SAP\NWBC\NWBC.exe", ExePath.Combine(" \"C:\\SAP\\NWBC\\\" ", "NWBC.exe"));
        }

        [TestMethod]
        public void Empty_exe_gives_empty_path()
        {
            Assert.AreEqual("", ExePath.Combine(@"C:\SAP", ""));
            Assert.AreEqual("", ExePath.Combine(@"C:\SAP", null));
        }
    }
}
