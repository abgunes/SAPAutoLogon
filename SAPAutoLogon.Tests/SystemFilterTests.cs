using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SystemFilterTests
    {
        private static List<SapSystem> Sample()
        {
            return new List<SapSystem>
            {
                new SapSystem { Name = "Development", SystemId = "DEV", ApplicationServer = "dev.example.test" },
                new SapSystem { Name = "Production", SystemId = "PRD", MessageServer = "msg.prod.example.test" },
                new SapSystem { Name = "Quality", SystemId = "QAS", ApplicationServer = "qas.example.test" }
            };
        }

        [TestMethod]
        public void Filter_matches_name_server_and_system_id_case_insensitively()
        {
            List<SapSystem> all = Sample();

            List<SapSystem> byName = SystemFilter.Filter(all, "PRODUCT");
            Assert.AreEqual(1, byName.Count);
            Assert.AreSame(all[1], byName[0]);

            List<SapSystem> byId = SystemFilter.Filter(all, "qas");
            Assert.AreEqual(1, byId.Count);
            Assert.AreSame(all[2], byId[0]);

            List<SapSystem> byApp = SystemFilter.Filter(all, "DEV.EXAMPLE");
            Assert.AreEqual(1, byApp.Count);
            Assert.AreSame(all[0], byApp[0]);

            List<SapSystem> byMsg = SystemFilter.Filter(all, "MSG.PROD");
            Assert.AreEqual(1, byMsg.Count);
            Assert.AreSame(all[1], byMsg[0]);

            Assert.AreEqual(3, SystemFilter.Filter(all, "example.test").Count);
            Assert.AreEqual(0, SystemFilter.Filter(all, "nomatch").Count);
        }

        [TestMethod]
        public void Filter_empty_text_returns_everything_in_order()
        {
            List<SapSystem> all = Sample();

            foreach (string text in new string[] { "", "   ", null })
            {
                List<SapSystem> result = SystemFilter.Filter(all, text);
                Assert.AreNotSame(all, result);
                Assert.AreEqual(all.Count, result.Count);
                for (int i = 0; i < all.Count; i++)
                    Assert.AreSame(all[i], result[i]);
            }
        }

        [TestMethod]
        public void Filter_keeps_object_identity_for_duplicate_names()
        {
            SapSystem a = new SapSystem { Name = "Same", SystemId = "AAA" };
            SapSystem b = new SapSystem { Name = "Same", SystemId = "BBB" };
            List<SapSystem> all = new List<SapSystem> { a, b };

            List<SapSystem> result = SystemFilter.Filter(all, "same");

            Assert.AreEqual(2, result.Count);
            Assert.AreSame(a, result[0]);
            Assert.AreSame(b, result[1]);
        }

        [TestMethod]
        public void Filter_null_list_returns_empty_list()
        {
            List<SapSystem> result = SystemFilter.Filter(null, "x");
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}
