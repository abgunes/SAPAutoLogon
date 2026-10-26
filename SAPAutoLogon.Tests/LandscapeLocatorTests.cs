using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LandscapeLocatorTests
    {
        private const string LastUsedKey = @"Software\SAP\SAPLogon\LandscapeFilesLastUsed";
        private const string OptionsKey = @"Software\SAP\SAPLogon\Options";
        private const string AppDataFile = @"C:\Users\u\AppData\Roaming\SAP\Common\SAPUILandscape.xml";

        private sealed class FakeEnv : ILandscapeEnvironment
        {
            public Dictionary<string, string> Registry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public bool ThrowOnRegistry;

            public void SetReg(RegistryRoot root, string subKey, string name, string value)
            {
                Registry[root + "|" + subKey + "|" + name] = value;
            }

            public string ReadRegistry(RegistryRoot root, string subKey, string valueName)
            {
                if (ThrowOnRegistry) throw new InvalidOperationException("registry boom");
                string v;
                return Registry.TryGetValue(root + "|" + subKey + "|" + valueName, out v) ? v : "";
            }

            public string GetEnvironmentVariable(string name)
            {
                string v;
                return Env.TryGetValue(name, out v) ? v : "";
            }

            public bool FileExists(string path)
            {
                return Files.Contains(path);
            }

            public string ExpandVariables(string text)
            {
                return text.Replace("%APPDATA%", @"C:\Users\u\AppData\Roaming");
            }
        }

        [TestMethod]
        public void Registry_last_used_wins_over_everything()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFile", @"C:\sap\local.xml");
            env.Files.Add(@"C:\sap\local.xml");
            env.Env["SAPLOGON_LSXML_FILE"] = @"C:\env\env.xml";
            env.Files.Add(@"C:\env\env.xml");
            env.Files.Add(AppDataFile);

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(@"C:\sap\local.xml", files.LocalPath);
        }

        [TestMethod]
        public void Global_file_prefers_LandscapeFileOnServer_in_step_one()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFile", @"C:\sap\local.xml");
            env.Files.Add(@"C:\sap\local.xml");
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFileGlobal", @"\\server.example.test\share\third.xml");
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "CoreLandscapeFileOnServer", @"\\server.example.test\share\second.xml");
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFileOnServer", @"\\server.example.test\share\SAPUILandscapeGlobal.xml");

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(@"\\server.example.test\share\SAPUILandscapeGlobal.xml", files.GlobalPath);

            env.Registry.Remove(RegistryRoot.CurrentUser + "|" + LastUsedKey + "|LandscapeFileOnServer");
            Assert.AreEqual(@"\\server.example.test\share\second.xml", LandscapeLocator.Locate(env).GlobalPath);

            env.Registry.Remove(RegistryRoot.CurrentUser + "|" + LastUsedKey + "|CoreLandscapeFileOnServer");
            Assert.AreEqual(@"\\server.example.test\share\third.xml", LandscapeLocator.Locate(env).GlobalPath);
        }

        [TestMethod]
        public void Env_variable_used_when_registry_has_no_file()
        {
            FakeEnv env = new FakeEnv();
            env.Env["SAPLOGON_LSXML_FILE"] = @"C:\env\SAPUILandscape.xml";
            env.Files.Add(@"C:\env\SAPUILandscape.xml");
            env.Files.Add(@"C:\env\SAPUILandscapeGlobal.xml");

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(@"C:\env\SAPUILandscape.xml", files.LocalPath);
            Assert.AreEqual(@"C:\env\SAPUILandscapeGlobal.xml", files.GlobalPath);
        }

        [TestMethod]
        public void Env_variable_without_sibling_global_leaves_global_empty()
        {
            FakeEnv env = new FakeEnv();
            env.Env["SAPLOGON_LSXML_FILE"] = @"C:\env\SAPUILandscape.xml";
            env.Files.Add(@"C:\env\SAPUILandscape.xml");

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(@"C:\env\SAPUILandscape.xml", files.LocalPath);
            Assert.AreEqual("", files.GlobalPath);
        }

        [TestMethod]
        public void Env_variable_pointing_to_missing_file_is_skipped()
        {
            FakeEnv env = new FakeEnv();
            env.Env["SAPLOGON_LSXML_FILE"] = @"C:\env\missing.xml";
            env.Files.Add(AppDataFile);

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(AppDataFile, files.LocalPath);
        }

        [TestMethod]
        public void Options_key_falls_back_from_HKCU_to_HKLM_to_Wow6432()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.LocalMachine32, OptionsKey, "PathConfigFilesLocal", @"C:\wow");
            env.Files.Add(@"C:\wow\SAPUILandscape.xml");
            Assert.AreEqual(@"C:\wow\SAPUILandscape.xml", LandscapeLocator.Locate(env).LocalPath);

            env.SetReg(RegistryRoot.LocalMachine, OptionsKey, "PathConfigFilesLocal", @"C:\hklm");
            env.Files.Add(@"C:\hklm\SAPUILandscape.xml");
            Assert.AreEqual(@"C:\hklm\SAPUILandscape.xml", LandscapeLocator.Locate(env).LocalPath);

            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "PathConfigFilesLocal", @"C:\hkcu");
            env.Files.Add(@"C:\hkcu\SAPUILandscape.xml");
            Assert.AreEqual(@"C:\hkcu\SAPUILandscape.xml", LandscapeLocator.Locate(env).LocalPath);
        }

        [TestMethod]
        public void Options_global_is_overridden_by_Core_then_LandscapeFileOnServer()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "PathConfigFilesLocal", @"C:\hkcu");
            env.Files.Add(@"C:\hkcu\SAPUILandscape.xml");

            env.SetReg(RegistryRoot.LocalMachine32, OptionsKey, "LandscapeFileOnServer", @"\\server.example.test\share\plain.xml");
            Assert.AreEqual(@"\\server.example.test\share\plain.xml", LandscapeLocator.Locate(env).GlobalPath);

            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "LandscapeFileOnServer", @"\\server.example.test\share\own.xml");
            Assert.AreEqual(@"\\server.example.test\share\own.xml", LandscapeLocator.Locate(env).GlobalPath);

            // Core alone overrides the default...
            env.Registry.Remove(RegistryRoot.CurrentUser + "|" + OptionsKey + "|LandscapeFileOnServer");
            env.Registry.Remove(RegistryRoot.LocalMachine32 + "|" + OptionsKey + "|LandscapeFileOnServer");
            env.SetReg(RegistryRoot.LocalMachine, OptionsKey, "CoreLandscapeFileOnServer", @"\\server.example.test\share\core.xml");
            Assert.AreEqual(@"\\server.example.test\share\core.xml", LandscapeLocator.Locate(env).GlobalPath);

            // ...and LandscapeFileOnServer is applied last, so it wins when both are set.
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "LandscapeFileOnServer", @"\\server.example.test\share\own.xml");
            Assert.AreEqual(@"\\server.example.test\share\own.xml", LandscapeLocator.Locate(env).GlobalPath);
        }

        [TestMethod]
        public void Options_global_defaults_to_existing_sibling_file()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "PathConfigFilesLocal", @"C:\hkcu");
            env.Files.Add(@"C:\hkcu\SAPUILandscape.xml");
            env.Files.Add(@"C:\hkcu\SAPUILandscapeGlobal.xml");

            Assert.AreEqual(@"C:\hkcu\SAPUILandscapeGlobal.xml", LandscapeLocator.Locate(env).GlobalPath);
        }

        [TestMethod]
        public void Options_global_is_empty_when_sibling_missing_and_no_overrides()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "PathConfigFilesLocal", @"C:\hkcu");
            env.Files.Add(@"C:\hkcu\SAPUILandscape.xml");

            Assert.AreEqual("", LandscapeLocator.Locate(env).GlobalPath);
        }

        [TestMethod]
        public void Options_override_beats_existing_sibling()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "PathConfigFilesLocal", @"C:\hkcu");
            env.Files.Add(@"C:\hkcu\SAPUILandscape.xml");
            env.Files.Add(@"C:\hkcu\SAPUILandscapeGlobal.xml");
            env.SetReg(RegistryRoot.CurrentUser, OptionsKey, "CoreLandscapeFileOnServer", @"\\server.example.test\share\core.xml");

            Assert.AreEqual(@"\\server.example.test\share\core.xml", LandscapeLocator.Locate(env).GlobalPath);
        }

        [TestMethod]
        public void Appdata_default_is_last_resort()
        {
            FakeEnv env = new FakeEnv();
            env.Files.Add(AppDataFile);

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(AppDataFile, files.LocalPath);
            Assert.AreEqual("", files.GlobalPath);
        }

        [TestMethod]
        public void Nothing_found_returns_empty_paths()
        {
            LandscapeFiles files = LandscapeLocator.Locate(new FakeEnv());

            Assert.AreEqual("", files.LocalPath);
            Assert.AreEqual("", files.GlobalPath);
        }

        [TestMethod]
        public void Percent_variables_in_registry_values_are_expanded()
        {
            FakeEnv env = new FakeEnv();
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFile", @"%APPDATA%\SAP\Common\custom.xml");
            env.SetReg(RegistryRoot.CurrentUser, LastUsedKey, "LandscapeFileOnServer", @"%APPDATA%\SAP\Common\global.xml");
            env.Files.Add(@"C:\Users\u\AppData\Roaming\SAP\Common\custom.xml");

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(@"C:\Users\u\AppData\Roaming\SAP\Common\custom.xml", files.LocalPath);
            Assert.AreEqual(@"C:\Users\u\AppData\Roaming\SAP\Common\global.xml", files.GlobalPath);
        }

        [TestMethod]
        public void Throwing_environment_yields_empty_result_instead_of_throwing()
        {
            FakeEnv env = new FakeEnv();
            env.ThrowOnRegistry = true;
            env.Files.Add(AppDataFile);

            LandscapeFiles files = LandscapeLocator.Locate(env);

            Assert.AreEqual(AppDataFile, files.LocalPath);
        }

        [TestMethod]
        public void Null_environment_returns_empty_paths()
        {
            LandscapeFiles files = LandscapeLocator.Locate(null);

            Assert.AreEqual("", files.LocalPath);
            Assert.AreEqual("", files.GlobalPath);
        }
    }
}
