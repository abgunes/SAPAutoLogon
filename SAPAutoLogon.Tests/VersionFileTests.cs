using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SAPAutoLogon.Tests
{
    // The KeePass update check reads Info/keepass_plugin.ver. Its plugin name must be
    // the AssemblyTitle and its version a file version that exists,
    // or KeePass reports wrong or no updates.
    [TestClass]
    public class VersionFileTests
    {
        private static string RepoFile(params string[] parts)
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "SAPAutoLogon", "Info", "keepass_plugin.ver");
                if (File.Exists(candidate))
                {
                    string path = dir;
                    foreach (string part in parts) path = Path.Combine(path, part);
                    return path;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new FileNotFoundException("Repository root not found from " + AppContext.BaseDirectory);
        }

        private static string AssemblyAttribute(string name)
        {
            string source = File.ReadAllText(RepoFile("SAPAutoLogon", "Properties", "AssemblyInfo.cs"));
            Match m = Regex.Match(source, @"\[assembly: " + name + @"\(""([^""]*)""\)\]");
            Assert.IsTrue(m.Success, name + " not found in AssemblyInfo.cs");
            return m.Groups[1].Value;
        }

        [TestMethod]
        public void Version_file_is_utf8_without_bom()
        {
            byte[] bytes = File.ReadAllBytes(RepoFile("SAPAutoLogon", "Info", "keepass_plugin.ver"));
            Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        }

        [TestMethod]
        public void Version_file_starts_and_ends_with_the_separator_line()
        {
            string[] lines = File.ReadAllText(RepoFile("SAPAutoLogon", "Info", "keepass_plugin.ver"))
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(lines.Length >= 3);
            Assert.AreEqual(":", lines[0]);
            Assert.AreEqual(":", lines[lines.Length - 1]);
        }

        [TestMethod]
        public void Version_file_names_the_plugin_and_never_lists_a_version_newer_than_the_build()
        {
            string title = AssemblyAttribute("AssemblyTitle");
            Version built = Version.Parse(AssemblyAttribute("AssemblyFileVersion"));
            string text = File.ReadAllText(RepoFile("SAPAutoLogon", "Info", "keepass_plugin.ver"));
            Match m = Regex.Match(text, "(?m)^" + Regex.Escape(title) + @":(\d+(\.\d+){1,3})\s*$");
            Assert.IsTrue(m.Success, "no line for " + title + " in the version file");
            Version listed = Version.Parse(m.Groups[1].Value);
            // The file is changed after a release exists, so it may be older than the
            // build but must never announce a version that was not built.
            Assert.IsTrue(listed <= built, "version file lists " + listed + ", newer than the build " + built);
        }

        [TestMethod]
        public void Assembly_version_and_file_version_are_the_same()
        {
            Assert.AreEqual(AssemblyAttribute("AssemblyVersion"), AssemblyAttribute("AssemblyFileVersion"));
        }

        [TestMethod]
        public void Update_url_is_https_and_points_at_the_version_file()
        {
            StringAssert.StartsWith(UpdateSource.VersionFileUrl, "https://");
            StringAssert.EndsWith(UpdateSource.VersionFileUrl, "/SAPAutoLogon/Info/keepass_plugin.ver");
        }
    }
}
