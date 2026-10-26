using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SystemDisplayTests
    {
        [TestMethod]
        public void Direct_system_shows_id_and_application_server()
        {
            SapSystem s = new SapSystem { Name = "S25 - Development", SystemId = "S25", ApplicationServer = "10.0.0.5" };

            Assert.AreEqual("S25 - Development  [S25 | 10.0.0.5]", SystemDisplay.Format(s));
            Assert.AreEqual("S25 - Development", s.ToString());
        }

        [TestMethod]
        public void Group_system_shows_message_server()
        {
            SapSystem s = new SapSystem { Name = "Q25 - Group", SystemId = "Q25", MessageServer = "ms1.example.test", IsGroup = true };

            Assert.AreEqual("Q25 - Group  [Q25 | ms1.example.test]", SystemDisplay.Format(s));
        }

        [TestMethod]
        public void Missing_parts_are_left_out()
        {
            Assert.AreEqual("X  [X1]", SystemDisplay.Format(new SapSystem { Name = "X", SystemId = "X1" }));
            Assert.AreEqual("X  [host]", SystemDisplay.Format(new SapSystem { Name = "X", ApplicationServer = "host" }));
            Assert.AreEqual("X", SystemDisplay.Format(new SapSystem { Name = "X" }));
            Assert.AreEqual("", SystemDisplay.Format(null));
        }
    }
}
