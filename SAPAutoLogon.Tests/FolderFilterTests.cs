using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class FolderFilterTests
    {
        private static List<SapFolder> Roots()
        {
            string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "landscape.xml"));
            List<SapSystem> systems = LandscapeReader.Read(new string[] { text });
            return WorkspaceReader.Read(new string[] { text }, systems);
        }

        private static FolderMatch Find(List<FolderMatch> level, string name)
        {
            foreach (FolderMatch m in level)
            {
                if (m.Folder.Name == name)
                {
                    return m;
                }
            }
            return null;
        }

        private static SapFolder FindFolder(List<SapFolder> level, string name)
        {
            foreach (SapFolder f in level)
            {
                if (f.Name == name)
                {
                    return f;
                }
            }
            return null;
        }

        private static void AssertNoNameMatches(List<FolderMatch> level)
        {
            foreach (FolderMatch m in level)
            {
                Assert.IsFalse(m.NameMatches);
                AssertNoNameMatches(m.Folders);
            }
        }

        [TestMethod]
        public void Inactive_text_returns_the_whole_tree()
        {
            List<SapFolder> roots = Roots();
            foreach (string text in new string[] { null, "   " })
            {
                List<FolderMatch> result = FolderFilter.Apply(roots, text);
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Local", result[0].Folder.Name);
                Assert.AreEqual("(no folder)", result[1].Folder.Name);
                Assert.AreEqual(5, Find(Find(result, "Local").Folders, "Demo").Folders.Count);
                Assert.AreEqual(1, Find(result, "Local").Systems.Count);
                AssertNoNameMatches(result);
                Assert.AreSame(roots[0], result[0].Folder);
            }
        }

        [TestMethod]
        public void Whitespace_text_is_not_a_filter()
        {
            Assert.IsFalse(FolderFilter.IsActive("  "));
            Assert.IsTrue(FolderFilter.IsActive("a"));
            Assert.IsFalse(FolderFilter.IsActive(null));
        }

        [TestMethod]
        public void Folder_name_match_keeps_all_its_systems_and_children()
        {
            List<FolderMatch> result = FolderFilter.Apply(Roots(), "demo");

            Assert.AreEqual(1, result.Count);
            FolderMatch local = result[0];
            Assert.AreEqual("Local", local.Folder.Name);
            Assert.AreEqual(1, local.Folders.Count);
            Assert.IsNull(Find(local.Folders, "DE"));
            FolderMatch demo = Find(local.Folders, "Demo");
            Assert.IsNotNull(demo);
            Assert.IsTrue(demo.NameMatches);
            Assert.AreEqual(5, demo.Folders.Count);
            Assert.AreEqual(1, Find(demo.Folders, "Sub1").Systems.Count);
        }

        [TestMethod]
        public void System_match_keeps_the_folder_path_and_only_matching_systems()
        {
            List<FolderMatch> result = FolderFilter.Apply(Roots(), "10.0.0.7");

            Assert.AreEqual(1, result.Count);
            FolderMatch local = result[0];
            Assert.AreEqual("Local", local.Folder.Name);
            Assert.AreEqual(0, local.Systems.Count);
            Assert.IsNull(Find(local.Folders, "DE"));
            FolderMatch demo = Find(local.Folders, "Demo");
            Assert.AreEqual(1, demo.Folders.Count);
            FolderMatch sub2 = demo.Folders[0];
            Assert.AreEqual("Sub2", sub2.Folder.Name);
            Assert.IsFalse(sub2.NameMatches);
            Assert.AreEqual(1, sub2.Systems.Count);
            Assert.AreEqual("S25 - Development", sub2.Systems[0].Name);
        }

        [TestMethod]
        public void Match_is_case_insensitive_and_trimmed()
        {
            List<FolderMatch> result = FolderFilter.Apply(Roots(), "  snc ");

            FolderMatch demo = Find(Find(result, "Local").Folders, "Demo");
            FolderMatch sub1 = Find(demo.Folders, "Sub1");
            FolderMatch sub11 = Find(sub1.Folders, "Sub1.1");
            Assert.AreEqual(1, sub11.Systems.Count);
            Assert.AreEqual("S26 - SNC", sub11.Systems[0].Name);
            Assert.AreEqual(0, sub1.Systems.Count);
        }

        [TestMethod]
        public void No_match_gives_empty_list()
        {
            Assert.AreEqual(0, FolderFilter.Apply(Roots(), "zzz").Count);
        }

        [TestMethod]
        public void Systems_are_the_original_objects()
        {
            List<SapFolder> roots = Roots();
            List<FolderMatch> result = FolderFilter.Apply(roots, "10.0.0.7");

            FolderMatch sub2 = Find(Find(Find(result, "Local").Folders, "Demo").Folders, "Sub2");
            SapFolder originalDemo = FindFolder(FindFolder(roots, "Local").Folders, "Demo");
            SapFolder originalSub2 = FindFolder(originalDemo.Folders, "Sub2");

            Assert.AreSame(originalSub2, sub2.Folder);
            Assert.AreSame(originalSub2.Systems[0], sub2.Systems[0]);
        }

        [TestMethod]
        public void Null_roots_give_empty_list()
        {
            Assert.AreEqual(0, FolderFilter.Apply(null, "x").Count);
        }
    }
}
