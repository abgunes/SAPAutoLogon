using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class SapShortcutTests
    {
        private static Dictionary<string, string> Direct()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.SystemId] = "S25";
            f[SapFields.Client] = "100";
            f[SapFields.ApplicationServer] = "10.0.0.5";
            f[SapFields.SystemNumber] = "00";
            f[SapFields.Language] = "EN";
            return f;
        }

        private static Dictionary<string, string> Group()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.SystemId] = "S26";
            f[SapFields.Client] = "100";
            f[SapFields.MessageServer] = "ms1.example.test";
            f[SapFields.Group] = "PUBLIC";
            return f;
        }

        [TestMethod]
        public void Direct_entry()
        {
            string args = SapShortcut.BuildArguments(Direct(), "ALICE", "secret", true);

            Assert.AreEqual(
                "-system=S25 -client=100 -user=ALICE -pw=secret -language=EN -guiparm=\"/H/10.0.0.5/S/3200\" -maxgui",
                args);
        }

        [TestMethod]
        public void Direct_entry_through_router()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.SapRouterString] = "/H/router.example.test/S/3299";

            string args = SapShortcut.BuildArguments(f, "ALICE", "secret", true);

            Assert.IsTrue(args.Contains("-guiparm=\"/H/router.example.test/S/3299/H/10.0.0.5/S/3200\""), args);
        }

        [TestMethod]
        public void Group_entry_with_port()
        {
            Dictionary<string, string> f = Group();
            f[SapFields.MessageServerPort] = "3600";

            string args = SapShortcut.BuildArguments(f, "ALICE", "secret", false);

            Assert.IsTrue(args.Contains("-guiparm=\"/M/ms1.example.test/S/3600/G/PUBLIC\""), args);
            Assert.IsFalse(args.Contains("-maxgui"), args);
        }

        [TestMethod]
        public void Group_entry_without_port()
        {
            Dictionary<string, string> f = Group();
            f[SapFields.MessageServer] = "ms2.example.test";

            string args = SapShortcut.BuildArguments(f, "ALICE", "secret", true);

            Assert.IsTrue(args.Contains("-guiparm=\"/M/ms2.example.test/G/PUBLIC\""), args);
        }

        [TestMethod]
        public void Snc_arguments_only_when_name_present()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.SncName] = "p:CN=S26";
            f[SapFields.SncType] = "3";
            Assert.IsTrue(SapShortcut.BuildArguments(f, "ALICE", "secret", true).Contains("-snc_name=p:CN=S26 -snc_qop=3"));

            f[SapFields.SncName] = "p:CN=S26, O=Corp";
            Assert.IsTrue(SapShortcut.BuildArguments(f, "ALICE", "secret", true).Contains("-snc_name=\"p:CN=S26, O=Corp\""));

            f[SapFields.SncName] = "";
            string args = SapShortcut.BuildArguments(f, "ALICE", "secret", true);
            Assert.IsFalse(args.Contains("-snc_name"), args);
            Assert.IsFalse(args.Contains("-snc_qop"), args);
        }

        [TestMethod]
        public void Transaction_is_passed_as_command()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.Transaction] = "se38";

            string args = SapShortcut.BuildArguments(f, "ALICE", "secret", true);

            Assert.IsTrue(args.Contains("-command=se38"), args);
        }

        [TestMethod]
        public void Password_with_space_and_quote_stays_one_argument()
        {
            string args = SapShortcut.BuildArguments(Direct(), "ALICE", "p w\"d", true);

            Assert.IsTrue(args.Contains("-pw=\"p w\\\"d\""), args);
        }

        [TestMethod]
        public void Password_ending_in_backslash_is_doubled_inside_quotes()
        {
            string args = SapShortcut.BuildArguments(Direct(), "ALICE", "a b\\", true);

            Assert.IsTrue(args.Contains("-pw=\"a b\\\\\""), args);
        }

        [TestMethod]
        public void Password_with_backslash_before_quote_is_escaped()
        {
            string args = SapShortcut.BuildArguments(Direct(), "ALICE", "a b\\\"c", true);

            Assert.IsTrue(args.Contains("-pw=\"a b\\\\\\\"c\""), args);
        }

        [TestMethod]
        public void Password_with_leading_and_trailing_spaces_is_passed_exactly()
        {
            string args = SapShortcut.BuildArguments(Direct(), "ALICE", " pw ", true);

            Assert.IsTrue(args.Contains("-pw=\" pw \""), args);
        }

        [TestMethod]
        public void User_and_transaction_with_spaces_stay_one_argument()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.Transaction] = "se38 x";

            string args = SapShortcut.BuildArguments(f, "AL ICE", "secret", true);

            Assert.IsTrue(args.Contains("-user=\"AL ICE\""), args);
            Assert.IsTrue(args.Contains("-command=\"se38 x\""), args);
        }

        [TestMethod]
        public void Null_user_password_and_missing_keys_do_not_throw()
        {
            Dictionary<string, string> f = new Dictionary<string, string>();
            f[SapFields.SystemId] = "S25";

            string args = SapShortcut.BuildArguments(f, null, null, false);
            Assert.AreEqual("-system=S25", args);
            Assert.AreEqual("", SapShortcut.BuildArguments(null, "u", "p", true));
        }

        [TestMethod]
        public void IsLaunchable_direct_group_and_negative_cases()
        {
            Assert.IsTrue(SapShortcut.IsLaunchable(Direct()));
            Assert.IsTrue(SapShortcut.IsLaunchable(Group()));

            Dictionary<string, string> noClient = Direct();
            noClient[SapFields.Client] = "";
            Assert.IsFalse(SapShortcut.IsLaunchable(noClient));

            Dictionary<string, string> onlySystem = new Dictionary<string, string>();
            onlySystem[SapFields.SystemId] = "S25";
            Assert.IsFalse(SapShortcut.IsLaunchable(onlySystem));

            Assert.IsFalse(SapShortcut.IsLaunchable(new Dictionary<string, string>()));
            Assert.IsFalse(SapShortcut.IsLaunchable(null));
        }

        [TestMethod]
        public void Values_are_trimmed_except_user_and_password()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.SystemId] = " S25 ";
            f[SapFields.Client] = "100 ";
            f[SapFields.Language] = " EN";
            f[SapFields.Transaction] = " SE38 ";
            f[SapFields.SncName] = " p:CN=S25 ";
            f[SapFields.SncType] = "3 ";

            string args = SapShortcut.BuildArguments(f, " AL ", " pw ", false);

            Assert.AreEqual(
                "-system=S25 -client=100 -user=\" AL \" -pw=\" pw \" -language=EN -guiparm=\"/H/10.0.0.5/S/3200\" -command=SE38 -snc_name=p:CN=S25 -snc_qop=3",
                args);
        }

        [TestMethod]
        public void Router_with_trailing_H_is_not_doubled()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.SapRouterString] = "/H/router.example.test/S/3299/H/";
            Assert.IsTrue(SapShortcut.BuildArguments(f, "ALICE", "secret", true)
                .Contains("-guiparm=\"/H/router.example.test/S/3299/H/10.0.0.5/S/3200\""));

            f[SapFields.SapRouterString] = " /H/router.example.test/h/ ";
            Assert.IsTrue(SapShortcut.BuildArguments(f, "ALICE", "secret", true)
                .Contains("-guiparm=\"/H/router.example.test/H/10.0.0.5/S/3200\""));

            Dictionary<string, string> g = Group();
            g[SapFields.SapRouterString] = "/H/router.example.test/H/";
            Assert.IsTrue(SapShortcut.BuildArguments(g, "ALICE", "secret", true)
                .Contains("-guiparm=\"/H/router.example.test/M/ms1.example.test/G/PUBLIC\""));
        }

        [TestMethod]
        public void MissingForLaunch_is_empty_for_direct_and_group()
        {
            Assert.AreEqual(0, SapShortcut.MissingForLaunch(Direct()).Count);
            Assert.AreEqual(0, SapShortcut.MissingForLaunch(Group()).Count);
        }

        [TestMethod]
        public void MissingForLaunch_names_an_empty_client()
        {
            Dictionary<string, string> f = Direct();
            f[SapFields.Client] = "  ";

            CollectionAssert.AreEqual(new[] { "Client" }, SapShortcut.MissingForLaunch(f));
        }

        [TestMethod]
        public void MissingForLaunch_lists_everything_when_nothing_is_set()
        {
            string[] all = { "System ID", "Client", "Application server + System number or Message server + Group" };

            CollectionAssert.AreEqual(all, SapShortcut.MissingForLaunch(new Dictionary<string, string>()));
            CollectionAssert.AreEqual(all, SapShortcut.MissingForLaunch(null));
        }

        [TestMethod]
        public void MissingForLaunch_names_the_missing_half_of_a_started_connection()
        {
            Dictionary<string, string> direct = Direct();
            direct[SapFields.SystemNumber] = "";
            CollectionAssert.AreEqual(new[] { "System number" }, SapShortcut.MissingForLaunch(direct));

            Dictionary<string, string> group = Group();
            group[SapFields.Group] = "";
            CollectionAssert.AreEqual(new[] { "Group" }, SapShortcut.MissingForLaunch(group));
        }

        [TestMethod]
        public void MissingForLaunch_agrees_with_IsLaunchable()
        {
            Dictionary<string, string> noClient = Direct();
            noClient[SapFields.Client] = "";
            Dictionary<string, string> half = Group();
            half[SapFields.MessageServer] = "";

            foreach (Dictionary<string, string> f in new[] { Direct(), Group(), noClient, half, new Dictionary<string, string>() })
                Assert.AreEqual(SapShortcut.IsLaunchable(f), SapShortcut.MissingForLaunch(f).Count == 0);
        }
    }
}
