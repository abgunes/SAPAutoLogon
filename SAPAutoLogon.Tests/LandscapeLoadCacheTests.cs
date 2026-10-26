using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LandscapeLoadCacheTests
    {
        // Collects background work so the test decides when it runs.
        private sealed class ManualRunner
        {
            public readonly List<Action> Queued = new List<Action>();

            public void Run(Action work)
            {
                Queued.Add(work);
            }

            public void RunAll()
            {
                Action[] work = Queued.ToArray();
                Queued.Clear();
                foreach (Action a in work) a();
            }
        }

        private static LandscapeLoadResult Clean()
        {
            LandscapeLoadResult r = new LandscapeLoadResult();
            r.Systems.Add(new SapSystem { Name = "S25" });
            return r;
        }

        [TestMethod]
        public void First_request_loads_in_background_and_calls_back()
        {
            ManualRunner runner = new ManualRunner();
            int loads = 0;
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { loads++; return Clean(); }, runner.Run);

            List<LandscapeLoadResult> got = new List<LandscapeLoadResult>();
            LandscapeLoadResult immediate = cache.Request(delegate(LandscapeLoadResult r) { got.Add(r); });

            Assert.IsNull(immediate);
            Assert.AreEqual(0, loads);
            Assert.AreEqual(0, got.Count);

            runner.RunAll();

            Assert.AreEqual(1, loads);
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(1, got[0].Systems.Count);
        }

        [TestMethod]
        public void Requests_while_loading_share_one_load()
        {
            ManualRunner runner = new ManualRunner();
            int loads = 0;
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { loads++; return Clean(); }, runner.Run);

            int calls = 0;
            Assert.IsNull(cache.Request(delegate { calls++; }));
            Assert.IsNull(cache.Request(delegate { calls++; }));
            Assert.AreEqual(1, runner.Queued.Count);

            runner.RunAll();

            Assert.AreEqual(1, loads);
            Assert.AreEqual(2, calls);
        }

        [TestMethod]
        public void Clean_result_is_cached_and_returned_immediately()
        {
            ManualRunner runner = new ManualRunner();
            int loads = 0;
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { loads++; return Clean(); }, runner.Run);
            cache.Request(delegate { });
            runner.RunAll();

            int calls = 0;
            LandscapeLoadResult immediate = cache.Request(delegate { calls++; });

            Assert.IsNotNull(immediate);
            Assert.AreEqual(1, immediate.Systems.Count);
            Assert.AreEqual(0, runner.Queued.Count);
            Assert.AreEqual(1, loads);
            Assert.AreEqual(0, calls);
        }

        [TestMethod]
        public void Results_with_warnings_or_error_are_loaded_again()
        {
            ManualRunner runner = new ManualRunner();
            int loads = 0;
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate
            {
                loads++;
                LandscapeLoadResult r = Clean();
                if (loads == 1) r.Warnings.Add("shared file missing");
                else r.Error = "broken";
                return r;
            }, runner.Run);

            cache.Request(delegate { });
            runner.RunAll();
            Assert.IsNull(cache.Request(delegate { }));
            runner.RunAll();
            Assert.IsNull(cache.Request(delegate { }));
            runner.RunAll();

            Assert.AreEqual(3, loads);
        }

        [TestMethod]
        public void Throwing_load_becomes_an_error_result()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { throw new InvalidOperationException("boom"); }, runner.Run);

            LandscapeLoadResult got = null;
            cache.Request(delegate(LandscapeLoadResult r) { got = r; });
            runner.RunAll();

            Assert.IsNotNull(got);
            StringAssert.Contains(got.Error, "boom");
            Assert.IsNotNull(got.Systems);
            Assert.IsNotNull(got.Warnings);
        }

        [TestMethod]
        public void Null_result_and_null_lists_are_normalised()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { return null; }, runner.Run);

            LandscapeLoadResult got = null;
            cache.Request(delegate(LandscapeLoadResult r) { got = r; });
            runner.RunAll();

            Assert.AreNotEqual("", got.Error);
            Assert.IsNotNull(got.Systems);
            Assert.IsNotNull(got.Warnings);

            LandscapeLoadCache cache2 = new LandscapeLoadCache(delegate
            {
                LandscapeLoadResult r = new LandscapeLoadResult();
                r.Systems = null; r.Error = null; r.Warnings = null; r.Folders = null;
                return r;
            }, runner.Run);
            cache2.Request(delegate(LandscapeLoadResult r) { got = r; });
            runner.RunAll();

            Assert.AreEqual("", got.Error);
            Assert.IsNotNull(got.Systems);
            Assert.IsNotNull(got.Warnings);
            Assert.IsNotNull(got.Folders);
        }

        [TestMethod]
        public void A_throwing_callback_does_not_stop_the_others()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { return Clean(); }, runner.Run);

            int calls = 0;
            cache.Request(delegate { throw new InvalidOperationException("ui gone"); });
            cache.Request(delegate { calls++; });
            runner.RunAll();

            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void Runner_that_cannot_start_reports_an_error_and_allows_a_retry()
        {
            bool fail = true;
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { return Clean(); }, delegate(Action work)
            {
                if (fail) throw new NotSupportedException("no threads");
                runner.Run(work);
            });

            LandscapeLoadResult got = null;
            LandscapeLoadResult immediate = cache.Request(delegate(LandscapeLoadResult r) { got = r; });

            Assert.IsNull(immediate);
            Assert.IsNotNull(got);
            StringAssert.Contains(got.Error, "no threads");

            fail = false;
            got = null;
            Assert.IsNull(cache.Request(delegate(LandscapeLoadResult r) { got = r; }));
            runner.RunAll();
            Assert.AreEqual("", got.Error);
        }

        [TestMethod]
        public void Null_callback_is_allowed()
        {
            ManualRunner runner = new ManualRunner();
            LandscapeLoadCache cache = new LandscapeLoadCache(delegate { return Clean(); }, runner.Run);

            cache.Request(null);
            runner.RunAll();

            Assert.IsNotNull(cache.Request(null));
        }
    }
}
