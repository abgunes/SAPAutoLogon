using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class ImportPlannerTests
    {
        private static SapFolder Local()
        {
            string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape.xml"));
            List<SapSystem> systems = LandscapeReader.Read(new string[] { text });
            return WorkspaceReader.Read(new string[] { text }, systems)[0];
        }

        private static SapFolder Find(SapFolder parent, string name)
        {
            return parent.Folders.First(f => f.Name == name);
        }

        private static SapFolder Folder(string name, params SapSystem[] systems)
        {
            SapFolder f = new SapFolder();
            f.Name = name;
            f.Systems.AddRange(systems);
            return f;
        }

        private static SapSystem Sys(string name, string sid, string client, string server, string number)
        {
            SapSystem s = new SapSystem();
            s.Name = name;
            s.SystemId = sid;
            s.Client = client;
            s.ApplicationServer = server;
            s.SystemNumber = number;
            return s;
        }

        private static GroupSnapshot Snap(string name, params GroupSnapshot[] children)
        {
            GroupSnapshot g = new GroupSnapshot();
            g.Name = name;
            g.Handle = new object();
            g.Groups.AddRange(children);
            return g;
        }

        private static GroupSnapshot ToSnapshot(PlannedGroup planned)
        {
            GroupSnapshot g = new GroupSnapshot();
            g.Name = planned.Name;
            g.Handle = new object();
            foreach (PlannedEntry e in planned.Entries)
            {
                EntrySnapshot es = new EntrySnapshot();
                es.Title = e.Title;
                foreach (KeyValuePair<string, string> kv in e.Fields)
                {
                    es.Fields[kv.Key] = kv.Value;
                }
                g.Entries.Add(es);
            }
            foreach (PlannedGroup child in planned.Groups)
            {
                g.Groups.Add(ToSnapshot(child));
            }
            return g;
        }

        private static EntrySnapshot EntryOf(SapSystem s)
        {
            MappedEntry m = FieldMapper.Map(s);
            EntrySnapshot e = new EntrySnapshot();
            e.Title = m.Title;
            foreach (KeyValuePair<string, string> kv in m.Fields)
            {
                e.Fields[kv.Key] = kv.Value;
            }
            return e;
        }

        private static void AllHaveExistingHandle(PlannedGroup g)
        {
            Assert.IsNotNull(g.ExistingHandle, g.Name);
            foreach (PlannedGroup c in g.Groups)
            {
                AllHaveExistingHandle(c);
            }
        }

        private static void AllHaveNullHandle(PlannedGroup g)
        {
            Assert.IsNull(g.ExistingHandle, g.Name);
            foreach (PlannedGroup c in g.Groups)
            {
                AllHaveNullHandle(c);
            }
        }

        [TestMethod]
        public void Leaf_folder_plans_one_new_group_with_its_entries()
        {
            SapFolder sub11 = Find(Find(Find(Local(), "Demo"), "Sub1"), "Sub1.1");
            ImportPlan plan = ImportPlanner.Plan(sub11, new GroupSnapshot());
            Assert.AreEqual("Sub1.1", plan.Root.Name);
            Assert.IsNull(plan.Root.ExistingHandle);
            Assert.AreEqual(2, plan.Root.Entries.Count);
            Assert.AreEqual(1, plan.GroupsToCreate);
            Assert.AreEqual(2, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Nested_folders_are_planned_recursively()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan plan = ImportPlanner.Plan(demo, new GroupSnapshot());
            CollectionAssert.AreEqual(new string[] { "Sub1", "Sub2", "Empty", "(unnamed)" },
                plan.Root.Groups.Select(g => g.Name).ToList());
            Assert.AreEqual("Sub1.1", plan.Root.Groups[0].Groups[0].Name);
            Assert.AreEqual(6, plan.GroupsToCreate);
            Assert.AreEqual(6, plan.EntriesToAdd);
        }

        [TestMethod]
        public void Existing_group_with_same_name_is_reused()
        {
            SapFolder demo = Find(Local(), "Demo");
            GroupSnapshot existing = Snap("Demo");
            ImportPlan plan = ImportPlanner.Plan(demo, Snap("SAP", existing));
            Assert.AreSame(existing.Handle, plan.Root.ExistingHandle);
            Assert.AreEqual(5, plan.GroupsToCreate);
        }

        [TestMethod]
        public void Group_name_match_is_case_sensitive()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan plan = ImportPlanner.Plan(demo, Snap("SAP", Snap("demo")));
            Assert.IsNull(plan.Root.ExistingHandle);
            Assert.AreEqual(6, plan.GroupsToCreate);
        }

        [TestMethod]
        public void Entry_with_same_title_and_connection_is_skipped()
        {
            SapSystem s = Sys("S25 - Development", "S25", "100", "10.0.0.5", "00");
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(s));
            ImportPlan plan = ImportPlanner.Plan(Folder("F", s), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesSkipped);
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.Root.Entries.Count);
        }

        [TestMethod]
        public void Entry_with_same_title_but_different_client_is_added()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "200", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Entry_with_same_connection_but_different_title_is_added()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("Old name", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Planned_entry_values_come_from_FieldMapper()
        {
            SapFolder de = Find(Local(), "DE");
            ImportPlan plan = ImportPlanner.Plan(de, new GroupSnapshot());
            Assert.AreEqual(1, plan.Root.Entries.Count);
            PlannedEntry e = plan.Root.Entries[0];
            Assert.AreEqual("S25 - Development", e.Title);
            Assert.AreEqual("ALICE", e.UserName);
            Assert.AreEqual("10.0.0.5", e.Fields["applicationServer"]);
            Assert.AreEqual("", e.Fields["messageServer"]);
            Assert.AreEqual(SapFields.FromLandscape.Length, e.Fields.Count);
            Assert.IsFalse(e.Fields.ContainsKey("transaction"));
        }

        [TestMethod]
        public void Duplicate_folder_names_under_same_parent_merge_into_one_group()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan plan = ImportPlanner.Plan(demo, new GroupSnapshot());
            List<PlannedGroup> subs = plan.Root.Groups.Where(g => g.Name == "Sub2").ToList();
            Assert.AreEqual(1, subs.Count);
            CollectionAssert.AreEqual(new string[] { "S25 - Development", "Q26 - No port" },
                subs[0].Entries.Select(e => e.Title).ToList());
        }

        [TestMethod]
        public void Two_identical_systems_in_one_folder_add_one_and_skip_one()
        {
            SapFolder f = Folder("F",
                Sys("S25 - Development", "S25", "100", "10.0.0.5", "00"),
                Sys("S25 - Development", "S25", "100", "10.0.0.5", "00"));
            ImportPlan plan = ImportPlanner.Plan(f, new GroupSnapshot());
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(1, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Planning_twice_against_the_result_skips_everything()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan p1 = ImportPlanner.Plan(demo, new GroupSnapshot());
            GroupSnapshot target2 = Snap("SAP", ToSnapshot(p1.Root));
            ImportPlan p2 = ImportPlanner.Plan(demo, target2);
            Assert.AreEqual(0, p2.EntriesToAdd);
            Assert.AreEqual(0, p2.GroupsToCreate);
            Assert.AreEqual(p1.EntriesToAdd, p2.EntriesSkipped);
            AllHaveExistingHandle(p2.Root);
        }

        [TestMethod]
        public void Empty_folder_creates_a_group_with_no_entries()
        {
            SapFolder empty = Find(Find(Local(), "Demo"), "Empty");
            ImportPlan plan = ImportPlanner.Plan(empty, new GroupSnapshot());
            Assert.AreEqual(1, plan.GroupsToCreate);
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.Root.Entries.Count);
        }

        [TestMethod]
        public void Null_target_is_treated_as_empty()
        {
            ImportPlan plan = ImportPlanner.Plan(Folder("F", Sys("A", "A", "100", "h", "00")), null);
            Assert.IsNull(plan.Root.ExistingHandle);
            Assert.AreEqual(1, plan.GroupsToCreate);
            Assert.AreEqual(1, plan.EntriesToAdd);
        }

        [TestMethod]
        public void Null_folder_gives_empty_plan()
        {
            ImportPlan plan = ImportPlanner.Plan(null, new GroupSnapshot());
            Assert.IsNull(plan.Root);
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
            Assert.AreEqual(0, plan.GroupsToCreate);
        }

        [TestMethod]
        public void MatchesExisting_ignores_missing_keys_and_trims()
        {
            Dictionary<string, string> a = new Dictionary<string, string>();
            a[SapFields.SystemId] = " S25 ";
            Dictionary<string, string> b = new Dictionary<string, string>();
            b[SapFields.SystemId] = "S25";
            b[SapFields.Client] = "";
            Assert.IsTrue(ImportPlanner.MatchesExisting("T", a, "T", b));
            Assert.IsTrue(ImportPlanner.MatchesExisting("T", b, "T", a));
            Assert.IsTrue(ImportPlanner.MatchesExisting("T", null, " T ", new Dictionary<string, string>()));
        }

        [TestMethod]
        public void MatchesExisting_treats_empty_SAP_side_fields_as_wildcards()
        {
            Dictionary<string, string> sap = new Dictionary<string, string>();
            sap[SapFields.SystemId] = "S25";
            Dictionary<string, string> existing = new Dictionary<string, string>();
            existing[SapFields.SystemId] = "S25";
            existing[SapFields.Client] = "100";
            Assert.IsTrue(ImportPlanner.MatchesExisting("T", sap, "T", existing));
            // Not symmetric: a value SAP Logon stores must be present on the entry.
            Assert.IsFalse(ImportPlanner.MatchesExisting("T", existing, "T", sap));
            Assert.IsFalse(ImportPlanner.MatchesExisting("T", sap, "U", existing));
            Assert.IsFalse(ImportPlanner.MatchesExisting("T", sap, "t", existing));
        }

        [TestMethod]
        public void Entry_with_client_completed_by_hand_is_skipped()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(1, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Defaults_do_not_affect_matching()
        {
            // The user typed client 200 on an entry whose SAP Logon system stores none:
            // the planner compares raw values, so the entry is still recognised.
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "200", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(1, plan.EntriesSkipped);

            // The plan itself carries no defaults: they are applied only when an entry is created.
            ImportPlan fresh = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "", "10.0.0.5", "00")), new GroupSnapshot());
            Assert.AreEqual("", fresh.Root.Entries[0].Fields[SapFields.Client]);
            Assert.AreEqual("", fresh.Root.Entries[0].Fields[SapFields.Language]);
        }

        [TestMethod]
        public void Entry_with_client_and_language_defaults_applied_is_skipped_on_reimport()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan p1 = ImportPlanner.Plan(demo, new GroupSnapshot());
            GroupSnapshot created = ToSnapshot(p1.Root);
            ApplyDefaultsToAll(created);

            ImportPlan p2 = ImportPlanner.Plan(demo, Snap("SAP", created));
            Assert.AreEqual(0, p2.EntriesToAdd);
            Assert.AreEqual(p1.EntriesToAdd, p2.EntriesSkipped);
        }

        private static void ApplyDefaultsToAll(GroupSnapshot g)
        {
            foreach (EntrySnapshot e in g.Entries)
            {
                FieldMapper.ApplyDefaults(e.Fields);
            }
            foreach (GroupSnapshot c in g.Groups)
            {
                ApplyDefaultsToAll(c);
            }
        }

        [TestMethod]
        public void Entry_with_completed_client_but_other_SAP_client_is_added()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "200", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Entry_with_completed_client_but_other_title_is_added()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("Old name", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Empty_server_and_number_on_SAP_side_are_wildcards()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S25", "100", "", "")), Snap("SAP", f));
            Assert.AreEqual(0, plan.EntriesToAdd);
            Assert.AreEqual(1, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Same_title_but_different_system_id_is_added()
        {
            GroupSnapshot f = Snap("F");
            f.Entries.Add(EntryOf(Sys("S25 - Development", "S25", "100", "10.0.0.5", "00")));
            ImportPlan plan = ImportPlanner.Plan(
                Folder("F", Sys("S25 - Development", "S26", "", "10.0.0.5", "00")), Snap("SAP", f));
            Assert.AreEqual(1, plan.EntriesToAdd);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Reimport_after_completing_clients_skips_everything()
        {
            SapFolder demo = Find(Local(), "Demo");
            ImportPlan p1 = ImportPlanner.Plan(demo, new GroupSnapshot());
            GroupSnapshot imported = ToSnapshot(p1.Root);
            int completed = CompleteClients(imported, "123");
            Assert.IsTrue(completed > 0, "test data should contain systems without a client");

            ImportPlan p2 = ImportPlanner.Plan(demo, Snap("SAP", imported));
            Assert.AreEqual(0, p2.EntriesToAdd);
            Assert.AreEqual(0, p2.GroupsToCreate);
            Assert.AreEqual(p1.EntriesToAdd, p2.EntriesSkipped);

            ImportPlan p3 = ImportPlanner.Plan(demo, Snap("SAP", ToSnapshot(p1.Root)));
            Assert.AreEqual(0, p3.EntriesToAdd);
            Assert.AreEqual(p1.EntriesToAdd, p3.EntriesSkipped);
        }

        private static int CompleteClients(GroupSnapshot g, string client)
        {
            int count = 0;
            foreach (EntrySnapshot e in g.Entries)
            {
                string value;
                if (!e.Fields.TryGetValue(SapFields.Client, out value) || string.IsNullOrEmpty(value))
                {
                    e.Fields[SapFields.Client] = client;
                    count++;
                }
            }
            foreach (GroupSnapshot c in g.Groups)
            {
                count += CompleteClients(c, client);
            }
            return count;
        }

        [TestMethod]
        public void Snapshot_group_without_handle_is_not_treated_as_existing()
        {
            SapFolder demo = Find(Local(), "Demo");
            GroupSnapshot noHandle = Snap("Demo");
            noHandle.Handle = null;
            noHandle.Entries.Add(EntryOf(Sys("Q26 - No port", "Q26", "", "", "")));
            ImportPlan plan = ImportPlanner.Plan(demo, Snap("SAP", noHandle));
            Assert.IsNull(plan.Root.ExistingHandle);
            Assert.AreEqual(6, plan.GroupsToCreate);
            Assert.AreEqual(0, plan.EntriesSkipped);
        }

        [TestMethod]
        public void Existing_child_under_handle_less_parent_is_not_reused()
        {
            SapFolder demo = Find(Local(), "Demo");
            GroupSnapshot noHandle = Snap("Demo", Snap("Sub1"));
            noHandle.Handle = null;
            ImportPlan plan = ImportPlanner.Plan(demo, Snap("SAP", noHandle));
            Assert.AreEqual(6, plan.GroupsToCreate);
            AllHaveNullHandle(plan.Root);
        }

        [TestMethod]
        public void Same_named_sibling_folders_merge_into_one_existing_group()
        {
            SapFolder root = new SapFolder();
            root.Name = "R";
            root.Folders.Add(Folder("Sub2", Sys("A", "A", "100", "h1", "00")));
            root.Folders.Add(Folder("Sub2", Sys("B", "B", "100", "h2", "00")));
            GroupSnapshot sub2 = Snap("Sub2");
            ImportPlan plan = ImportPlanner.Plan(root, Snap("SAP", Snap("R", sub2)));
            Assert.AreEqual(1, plan.Root.Groups.Count);
            Assert.AreSame(sub2.Handle, plan.Root.Groups[0].ExistingHandle);
            Assert.AreEqual(2, plan.Root.Groups[0].Entries.Count);
            Assert.AreEqual(0, plan.GroupsToCreate);
            Assert.AreEqual(2, plan.EntriesToAdd);
        }

        [TestMethod]
        public void Duplicate_entry_in_second_merged_folder_is_skipped()
        {
            SapFolder root = new SapFolder();
            root.Name = "R";
            root.Folders.Add(Folder("Sub2", Sys("A", "A", "100", "h1", "00")));
            root.Folders.Add(Folder("Sub2", Sys("A", "A", "100", "h1", "00"), Sys("B", "B", "100", "h2", "00")));
            ImportPlan plan = ImportPlanner.Plan(root, new GroupSnapshot());
            Assert.AreEqual(1, plan.Root.Groups.Count);
            Assert.AreEqual(2, plan.Root.Groups[0].Entries.Count);
            Assert.AreEqual(2, plan.EntriesToAdd);
            Assert.AreEqual(1, plan.EntriesSkipped);
            Assert.AreEqual(2, plan.GroupsToCreate);
        }

        [TestMethod]
        public void Plans_440_systems_in_127_folders_quickly()
        {
            SapFolder root = new SapFolder();
            root.Name = "Root";
            List<SapFolder> leaves = new List<SapFolder>();
            for (int i = 0; i < 14; i++)
            {
                SapFolder child = new SapFolder();
                child.Name = "C" + i;
                root.Folders.Add(child);
                for (int j = 0; j < 8; j++)
                {
                    SapFolder leaf = new SapFolder();
                    leaf.Name = "L" + i + "_" + j;
                    child.Folders.Add(leaf);
                    leaves.Add(leaf);
                }
            }
            for (int n = 0; n < 440; n++)
            {
                string id = (n + 1).ToString("0000");
                leaves[n % leaves.Count].Systems.Add(Sys("SYS-" + id, "S" + id, "100", "host" + id + ".example.test", "00"));
            }

            Stopwatch watch = Stopwatch.StartNew();
            ImportPlan first = ImportPlanner.Plan(root, new GroupSnapshot());
            ImportPlan second = ImportPlanner.Plan(root, Snap("SAP", ToSnapshot(first.Root)));
            watch.Stop();

            Assert.AreEqual(440, first.EntriesToAdd);
            Assert.AreEqual(127, first.GroupsToCreate);
            Assert.AreEqual(440, second.EntriesSkipped);
            Assert.AreEqual(0, second.GroupsToCreate);
            Assert.IsTrue(watch.ElapsedMilliseconds < 2000, "took " + watch.ElapsedMilliseconds + " ms");
        }

        [TestMethod]
        public void Count_entries_without_user_name_is_recursive()
        {
            ImportPlan demo = ImportPlanner.Plan(Find(Local(), "Demo"), new GroupSnapshot());
            Assert.AreEqual(5, ImportPlanner.CountEntriesWithoutUserName(demo.Root));

            ImportPlan de = ImportPlanner.Plan(Find(Local(), "DE"), new GroupSnapshot());
            Assert.AreEqual(0, ImportPlanner.CountEntriesWithoutUserName(de.Root));

            Assert.AreEqual(0, ImportPlanner.CountEntriesWithoutUserName(null));
        }
    }
}
