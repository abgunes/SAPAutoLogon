using KeePassLib;

namespace SAPAutoLogon
{
    public interface IPluginColumn
    {
        string GetContent(PwEntry entry);
        string GetName();
        bool HasAction();

        /// <summary>"" when the program of this column can be started, otherwise a warning text.</summary>
        string GetMissingProgramMessage();

        /// <summary>True when the program of this column is configured and the file exists (checked now).</summary>
        bool IsProgramAvailable();

        /// <summary>Forgets remembered file checks (after the program folders were changed).</summary>
        void InvalidateCache();

        void PerformAction(PwEntry entry);
    }
}
