using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SapFieldsTests
    {
        [TestMethod]
        public void All_has_twelve_fields_with_transaction_last()
        {
            Assert.AreEqual(12, SapFields.All.Length);
            Assert.AreEqual("transaction", SapFields.All[11]);
        }

        [TestMethod]
        public void FromLandscape_excludes_transaction()
        {
            Assert.AreEqual(11, SapFields.FromLandscape.Length);
            CollectionAssert.DoesNotContain(SapFields.FromLandscape, SapFields.Transaction);
            CollectionAssert.AreEqual(
                new[]
                {
                    "systemId", "applicationServer", "systemNumber", "messageServer",
                    "messageServerPort", "group", "sapRouterString", "sncName",
                    "sncType", "client", "language"
                },
                SapFields.FromLandscape);
        }

        [TestMethod]
        public void Names_match_sap_adt()
        {
            Assert.AreEqual("systemId", SapFields.SystemId);
            Assert.AreEqual("applicationServer", SapFields.ApplicationServer);
            Assert.AreEqual("systemNumber", SapFields.SystemNumber);
            Assert.AreEqual("messageServer", SapFields.MessageServer);
            Assert.AreEqual("messageServerPort", SapFields.MessageServerPort);
            Assert.AreEqual("group", SapFields.Group);
            Assert.AreEqual("sapRouterString", SapFields.SapRouterString);
            Assert.AreEqual("sncName", SapFields.SncName);
            Assert.AreEqual("sncType", SapFields.SncType);
            Assert.AreEqual("client", SapFields.Client);
            Assert.AreEqual("language", SapFields.Language);
            Assert.AreEqual("transaction", SapFields.Transaction);
        }
    }
}
