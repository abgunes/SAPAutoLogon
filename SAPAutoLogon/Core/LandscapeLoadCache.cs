using System;
using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// Runs the SAP Logon load in the background, shares one load between all
    /// callers while it runs, and keeps a clean result (no error, no warnings)
    /// for later callers. Results with an error or warnings are loaded again
    /// on the next request. Thread-safe. Never throws to its callers.
    /// </summary>
    public sealed class LandscapeLoadCache
    {
        private readonly object m_lock = new object();
        private readonly Func<LandscapeLoadResult> m_load;
        private readonly Action<Action> m_runInBackground;

        private LandscapeLoadResult m_cached;
        private bool m_loading;
        private List<Action<LandscapeLoadResult>> m_waiting = new List<Action<LandscapeLoadResult>>();

        /// <param name="load">Does the actual load; may throw or return null.</param>
        /// <param name="runInBackground">Starts the given work on another thread; may throw.</param>
        public LandscapeLoadCache(Func<LandscapeLoadResult> load, Action<Action> runInBackground)
        {
            m_load = load;
            m_runInBackground = runInBackground;
        }

        /// <summary>
        /// Returns the cached result when there is one (onLoaded is then not called).
        /// Otherwise returns null and calls onLoaded once with the result, from the
        /// background thread (or from this thread when the background work could not be started).
        /// </summary>
        public LandscapeLoadResult Request(Action<LandscapeLoadResult> onLoaded)
        {
            bool start;
            lock (m_lock)
            {
                if (m_cached != null) return m_cached;

                if (onLoaded != null) m_waiting.Add(onLoaded);
                start = !m_loading;
                m_loading = true;
            }

            if (start)
            {
                try
                {
                    m_runInBackground(Run);
                }
                catch (Exception ex)
                {
                    Complete(ErrorResult("Cannot load SAP Logon landscape: " + ex.Message));
                }
            }
            return null;
        }

        private void Run()
        {
            LandscapeLoadResult result;
            try
            {
                result = Normalize(m_load());
            }
            catch (Exception ex)
            {
                result = ErrorResult("Cannot load SAP Logon landscape: " + ex.Message);
            }
            Complete(result);
        }

        private void Complete(LandscapeLoadResult result)
        {
            List<Action<LandscapeLoadResult>> waiting;
            lock (m_lock)
            {
                if (result.Error.Length == 0 && result.Warnings.Count == 0)
                    m_cached = result;
                m_loading = false;
                waiting = m_waiting;
                m_waiting = new List<Action<LandscapeLoadResult>>();
            }

            foreach (Action<LandscapeLoadResult> callback in waiting)
            {
                try
                {
                    callback(result);
                }
                catch (Exception)
                {
                    // One closed dialog must not keep the others from getting the result.
                }
            }
        }

        private static LandscapeLoadResult Normalize(LandscapeLoadResult result)
        {
            if (result == null)
                return ErrorResult("Cannot load SAP Logon landscape.");
            if (result.Systems == null) result.Systems = new List<SapSystem>();
            if (result.Folders == null) result.Folders = new List<SapFolder>();
            if (result.Error == null) result.Error = "";
            if (result.Warnings == null) result.Warnings = new List<string>();
            return result;
        }

        private static LandscapeLoadResult ErrorResult(string error)
        {
            LandscapeLoadResult result = new LandscapeLoadResult();
            result.Error = error;
            return result;
        }
    }
}
