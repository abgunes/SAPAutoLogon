using KeePassLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAPAutoLogon
{
    /// <summary>
    /// Abstract class of plugin's column with mandatory functionality
    /// </summary>
    public abstract class PluginColumn : IPluginColumn
    {
        // How long a File.Exists answer is reused while the entry list is drawn.
        private const int ExistsCacheMs = 2000;

        protected string Name;
        protected PluginParameters pluginParameters;

        private string m_checkedPath;
        private bool m_checkedExists;
        private int m_checkedAt;

        public PluginColumn(string name, PluginParameters pluginParameters)
        {
            Name = name;
            this.pluginParameters = pluginParameters;
        }

        public string GetName() { return Name; }

        public string GetContent(PwEntry entry) { return GetContentInt(entry); }

        public bool HasAction() { return true; }

        public void PerformAction(PwEntry entry) { PerformActionInt(entry); }

        /// <summary>
        /// True when the program of this column exists (fresh disk check, for the context menu).
        /// </summary>
        public bool IsProgramAvailable()
        {
            return ProgramExists(true);
        }

        /// <summary>Forgets the remembered File.Exists answer (the options were changed).</summary>
        public void InvalidateCache()
        {
            m_checkedPath = null;
            m_checkedExists = false;
            m_checkedAt = 0;
        }

        public string GetMissingProgramMessage()
        {
            return ProgramExists(true) ? string.Empty : MissingProgramMessage;
        }

        protected virtual string GetContentInt(PwEntry entry) { return string.Empty; }
        protected virtual void PerformActionInt(PwEntry entry) { }

        /// <summary>Configured folder of this column's program.</summary>
        protected abstract string ProgramFolder { get; }

        /// <summary>File name of this column's program.</summary>
        protected abstract string ProgramFile { get; }

        /// <summary>Warning shown when the program is not found.</summary>
        protected abstract string MissingProgramMessage { get; }

        /// <summary>Full path of this column's program, "" when no folder is set.</summary>
        protected string ProgramPath
        {
            get { return ExePath.Combine(ProgramFolder, ProgramFile); }
        }

        // File.Exists of the program path; while drawing, the answer is reused
        // for a short time so a long entry list does not hit the disk per cell.
        private bool ProgramExists(bool fresh)
        {
            string path = ProgramPath;
            if (path.Length == 0) { return false; }

            int now = Environment.TickCount;
            if (fresh || path != m_checkedPath || unchecked(now - m_checkedAt) > ExistsCacheMs || unchecked(now - m_checkedAt) < 0)
            {
                bool exists;
                try { exists = File.Exists(path); }
                catch (Exception) { exists = false; }

                m_checkedPath = path;
                m_checkedExists = exists;
                m_checkedAt = now;
            }
            return m_checkedExists;
        }

        /// <summary>
        /// Reads the SAP connection fields of an entry (missing fields are empty).
        /// </summary>
        protected static Dictionary<string, string> ReadFields(PwEntry entry)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            foreach (string name in SapFields.All)
            {
                fields[name] = entry.Strings.ReadSafe(name);
            }
            return fields;
        }

        /// <summary>
        /// True when the entry has a complete SAP connection (see SapShortcut.IsLaunchable).
        /// </summary>
        public static bool IsEntryLaunchable(PwEntry entry)
        {
            if (entry == null) { return false; }
            return SapShortcut.IsLaunchable(ReadFields(entry));
        }

        /// <summary>
        /// True when the entry is launchable and the program of this column exists.
        /// </summary>
        protected bool IsEntryExecutable(PwEntry entry)
        {
            if (!IsEntryLaunchable(entry)) { return false; }
            return ProgramExists(false);
        }

        protected string BuildArguments(PwEntry entry)
        {
            return SapShortcut.BuildArguments(
                ReadFields(entry),
                entry.Strings.ReadSafe(PwDefs.UserNameField),
                entry.Strings.ReadSafe(PwDefs.PasswordField),
                pluginParameters.isMaxGui);
        }
    }

    /// <summary>
    /// SAP GUI column
    /// </summary>
    public class PluginColumnGui : PluginColumn
    {
        public PluginColumnGui(string name, PluginParameters pluginParameters) : base(name, pluginParameters) { }

        protected override string ProgramFolder { get { return pluginParameters.sapGuiPath; } }
        protected override string ProgramFile { get { return PluginParameters.SAPGUIShortCutEXE; } }
        protected override string MissingProgramMessage
        {
            get { return "SAP GUI (sapshcut.exe) was not found. Set its folder in Tools > SAPAutoLogon options."; }
        }

        protected override string GetContentInt(PwEntry entry)
        {
            return LaunchText.Cell(IsEntryExecutable(entry), LaunchText.GuiCell);
        }

        protected override void PerformActionInt(PwEntry entry)
        {
            new SAPBinaries().DoLogon(ProgramPath, BuildArguments(entry));
        }
    }

    /// <summary>
    /// SAP Business Client column
    /// </summary>
    public class PluginColumnBC : PluginColumn
    {
        public PluginColumnBC(string name, PluginParameters pluginParameters) : base(name, pluginParameters) { }

        protected override string ProgramFolder { get { return pluginParameters.nBCPath; } }
        protected override string ProgramFile { get { return PluginParameters.SAPNWBCShortCutEXE; } }
        protected override string MissingProgramMessage
        {
            get { return "SAP Business Client (NWBC.exe) was not found. Set its folder in Tools > SAPAutoLogon options."; }
        }

        protected override string GetContentInt(PwEntry entry)
        {
            return LaunchText.Cell(IsEntryExecutable(entry), LaunchText.BusinessClientCell);
        }

        protected override void PerformActionInt(PwEntry entry)
        {
            new SAPBinaries().DoLogon(ProgramPath, "/SHORTCUT=\"" + BuildArguments(entry) + "\"");
        }
    }
}
