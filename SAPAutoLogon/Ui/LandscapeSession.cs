using System;
using System.Threading;

namespace SAPAutoLogon
{
    /// <summary>
    /// The one SAP Logon landscape load of the KeePass session, shared by the
    /// SAP tab of the entry dialogs and the Import from SAP Logon popup.
    /// </summary>
    public static class LandscapeSession
    {
        // SAP Logon files are read on a thread-pool thread (a share or web server
        // may be slow or unreachable). One load is shared by all open dialogs.
        // Clean loads are kept for the KeePass session; loads with an error or
        // warnings are retried in the next dialog.
        private static readonly LandscapeLoadCache s_landscape =
            new LandscapeLoadCache(LoadLandscapeNow, RunOnThreadPool);

        public static LandscapeLoadCache Cache { get { return s_landscape; } }

        // Runs on the worker thread. Exceptions are turned into an error by LandscapeLoadCache.
        private static LandscapeLoadResult LoadLandscapeNow()
        {
            LandscapeFiles files = LandscapeLocator.Locate(new WindowsLandscapeEnvironment());
            return LandscapeLoader.Load(files, LandscapeIo.ReadText);
        }

        private static void RunOnThreadPool(Action work)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                // An exception on a pool thread would end KeePass.
                try { work(); }
                catch (Exception) { }
            });
        }
    }
}
