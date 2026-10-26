using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SapFieldWriterTests
    {
        private static bool Apply(Dictionary<string, string> values, Dictionary<string, string> store, List<string> log)
        {
            return SapFieldWriter.Apply(values,
                delegate(string k) { string v; return store.TryGetValue(k, out v) ? v : ""; },
                delegate(string k, string v) { store[k] = v; log.Add("set " + k); },
                delegate(string k) { store.Remove(k); log.Add("remove " + k); });
        }

        [TestMethod]
        public void Sets_new_and_changed_values_and_removes_emptied_ones()
        {
            var store = new Dictionary<string, string> { { "client", "100" }, { "group", "PUBLIC" }, { "transaction", "SE80" } };
            var log = new List<string>();

            bool changed = Apply(new Dictionary<string, string> { { "client", "200" }, { "group", "" }, { "systemId", "DEV" } }, store, log);

            Assert.IsTrue(changed);
            Assert.AreEqual("200", store["client"]);
            Assert.IsFalse(store.ContainsKey("group"));
            Assert.AreEqual("DEV", store["systemId"]);
            Assert.AreEqual("SE80", store["transaction"], "keys not in the values are untouched");
        }

        [TestMethod]
        public void Unchanged_and_already_absent_values_are_not_written()
        {
            var store = new Dictionary<string, string> { { "client", "100" } };
            var log = new List<string>();

            bool changed = Apply(new Dictionary<string, string> { { "client", "100" }, { "group", "" }, { "sncName", null } }, store, log);

            Assert.IsFalse(changed);
            Assert.AreEqual(0, log.Count);
        }

        [TestMethod]
        public void Null_values_dictionary_does_nothing()
        {
            Assert.IsFalse(Apply(null, new Dictionary<string, string>(), new List<string>()));
        }
    }
}
