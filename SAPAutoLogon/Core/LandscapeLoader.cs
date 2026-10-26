using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SAPAutoLogon
{
    /// <summary>Locations of the landscape files to load. Empty string = not set.</summary>
    public sealed class LandscapeFiles
    {
        public string LocalPath = "";
        public string GlobalPath = "";
    }

    /// <summary>Outcome of <see cref="LandscapeLoader.Load"/>.</summary>
    public sealed class LandscapeLoadResult
    {
        public List<SapSystem> Systems = new List<SapSystem>();

        /// <summary>Empty on success, otherwise a short human-readable text.</summary>
        public string Error = "";

        /// <summary>
        /// Short human-readable texts about optional files that could not be read,
        /// or an empty result. Never null. The list may be incomplete when not empty.
        /// </summary>
        public List<string> Warnings = new List<string>();

        /// <summary>
        /// The SAP Logon folder tree (workspaces) built from the same documents as
        /// <see cref="Systems"/>. Empty when the load failed. Never null.
        /// </summary>
        public List<SapFolder> Folders = new List<SapFolder>();
    }

    /// <summary>
    /// Reads the local landscape file, the optional global one and every
    /// Include they reference, and merges them with <see cref="LandscapeReader"/>.
    /// All I/O goes through the supplied delegate. Never throws.
    /// </summary>
    public static class LandscapeLoader
    {
        private const int MaxIncludeDepth = 3;

        private const string NoSystemsWarning = "No SAP GUI systems found in the SAP Logon files.";

        private sealed class Pending
        {
            public string Url;
            public string Location;
            public int Depth;
        }

        /// <param name="readText">Takes a path or URL and returns its text; throws on failure.</param>
        public static LandscapeLoadResult Load(LandscapeFiles files, Func<string, string> readText)
        {
            LandscapeLoadResult result = new LandscapeLoadResult();
            try
            {
                string local = files == null ? "" : (files.LocalPath ?? "");
                string global = files == null ? "" : (files.GlobalPath ?? "");

                if (local.Length == 0)
                {
                    result.Error = "SAP Logon landscape file not found.";
                    return result;
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                seen.Add(Key(local));
                if (global.Length > 0) seen.Add(Key(global));

                List<string> documents = new List<string>();
                Queue<Pending> queue = new Queue<Pending>();

                // The local file is mandatory: any failure here is reported.
                string localText;
                try
                {
                    localText = readText(local);
                    XDocument localDoc = XDocument.Parse(localText);
                    documents.Add(localText);
                    EnqueueIncludes(localDoc, 1, seen, queue);
                }
                catch (Exception ex)
                {
                    result.Error = "Cannot read SAP Logon landscape file '" + local + "': " + ex.Message;
                    return result;
                }

                if (global.Length > 0)
                {
                    if (!TryRead(global, 0, readText, documents, seen, queue))
                        result.Warnings.Add("Could not read the shared SAP Logon file: " + global);
                }

                while (queue.Count > 0)
                {
                    Pending p = queue.Dequeue();
                    if (p.Location.Length == 0)
                    {
                        result.Warnings.Add("Could not use the SAP Logon include: " + p.Url);
                        continue;
                    }
                    if (!TryRead(p.Location, p.Depth, readText, documents, seen, queue))
                        result.Warnings.Add("Could not read the included SAP Logon file: " + p.Url);
                }

                result.Systems = LandscapeReader.Read(documents);
                try
                {
                    result.Folders = WorkspaceReader.Read(documents, result.Systems);
                }
                catch (Exception)
                {
                    // The folder tree is optional; the systems stay usable without it.
                    result.Folders = new List<SapFolder>();
                }
                if (result.Systems.Count == 0) result.Warnings.Add(NoSystemsWarning);
            }
            catch (Exception ex)
            {
                result.Systems = new List<SapSystem>();
                result.Folders = new List<SapFolder>();
                result.Warnings = new List<string>();
                result.Error = "Cannot load SAP Logon landscape: " + ex.Message;
            }
            return result;
        }

        // Optional documents (global file, includes): a failure skips the document
        // and returns false; the caller reports it as a warning.
        private static bool TryRead(string location, int depth, Func<string, string> readText,
            List<string> documents, HashSet<string> seen, Queue<Pending> queue)
        {
            try
            {
                string text = readText(location);
                XDocument doc = XDocument.Parse(text);
                documents.Add(text);
                EnqueueIncludes(doc, depth + 1, seen, queue);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void EnqueueIncludes(XDocument doc, int depth, HashSet<string> seen, Queue<Pending> queue)
        {
            if (depth > MaxIncludeDepth) return;
            foreach (XElement inc in doc.Descendants("Include"))
            {
                XAttribute a = inc.Attribute("url");
                if (a == null) continue;

                string url = a.Value.Trim();
                if (url.Length == 0) continue;

                // An unusable url is queued with an empty location and reported as a warning.
                string location = ToLocation(url);
                if (location.Length > 0 && !seen.Add(Key(location))) continue;

                Pending p = new Pending();
                p.Url = url;
                p.Location = location;
                p.Depth = depth;
                queue.Enqueue(p);
            }
        }

        // file:/// urls become local paths, everything else is passed through.
        // Returns "" when the url cannot be used.
        private static string ToLocation(string url)
        {
            if (url.Length == 0) return "";
            if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return new Uri(url).LocalPath;
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return url;
        }

        private static string Key(string location)
        {
            if (location.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                location.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
            return location.Replace('/', '\\');
        }
    }
}
