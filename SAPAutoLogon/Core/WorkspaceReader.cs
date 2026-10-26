using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SAPAutoLogon
{
    /// <summary>
    /// Reads the folder tree (the Workspaces element) of SAP Logon landscape
    /// XML documents and resolves its items to already loaded systems.
    /// </summary>
    public static class WorkspaceReader
    {
        public const string NoFolderName = "(no folder)";
        public const string UnnamedFolderName = "(unnamed)";
        public const string DefaultWorkspaceName = "Workspace";

        /// <summary>
        /// One root folder per visible workspace in document order, followed by
        /// a "(no folder)" root holding every system no item referenced
        /// (omitted when empty). Hidden elements are left out with their subtree.
        /// </summary>
        /// <exception cref="System.Xml.XmlException">A document is not valid XML.</exception>
        public static List<SapFolder> Read(IEnumerable<string> xmlDocuments, IList<SapSystem> systems)
        {
            List<SapFolder> roots = new List<SapFolder>();

            Dictionary<string, SapSystem> byUuid = new Dictionary<string, SapSystem>();
            if (systems != null)
            {
                foreach (SapSystem s in systems)
                {
                    if (s != null && !string.IsNullOrEmpty(s.Uuid) && !byUuid.ContainsKey(s.Uuid))
                    {
                        byUuid[s.Uuid] = s;
                    }
                }
            }

            HashSet<string> referenced = new HashSet<string>(StringComparer.Ordinal);

            if (xmlDocuments != null)
            {
                foreach (string xml in xmlDocuments)
                {
                    XDocument doc = XDocument.Parse(xml);
                    if (doc.Root == null) continue;
                    foreach (XElement ws in doc.Root.Elements("Workspaces").Elements("Workspace"))
                    {
                        // Items of hidden workspaces still count as placed.
                        if (IsHidden(ws))
                        {
                            CollectReferenced(ws, referenced);
                            continue;
                        }
                        SapFolder root = new SapFolder();
                        root.Name = NameOf(ws, DefaultWorkspaceName);
                        Fill(root, ws, byUuid, referenced);
                        roots.Add(root);
                    }
                }
            }

            SapFolder noFolder = new SapFolder();
            noFolder.Name = NoFolderName;
            if (systems != null)
            {
                foreach (SapSystem s in systems)
                {
                    if (s == null) continue;
                    if (string.IsNullOrEmpty(s.Uuid) || !referenced.Contains(s.Uuid))
                    {
                        noFolder.Systems.Add(s);
                    }
                }
            }
            if (noFolder.Systems.Count > 0) roots.Add(noFolder);

            return roots;
        }

        private static void Fill(SapFolder folder, XElement parent, Dictionary<string, SapSystem> byUuid, HashSet<string> referenced)
        {
            foreach (XElement e in parent.Elements())
            {
                string kind = e.Name.LocalName;
                if (kind == "Node")
                {
                    if (IsHidden(e))
                    {
                        CollectReferenced(e, referenced);
                        continue;
                    }
                    SapFolder child = new SapFolder();
                    child.Name = NameOf(e, UnnamedFolderName);
                    folder.Folders.Add(child);
                    Fill(child, e, byUuid, referenced);
                }
                else if (kind == "Item")
                {
                    if (IsHidden(e))
                    {
                        CollectReferenced(e, referenced);
                        continue;
                    }
                    string id = ServiceId(e);
                    if (id.Length == 0) continue;
                    referenced.Add(id);
                    SapSystem system;
                    if (byUuid.TryGetValue(id, out system)) folder.Systems.Add(system);
                }
            }
        }

        // Marks every item below (and including) the element as referenced.
        private static void CollectReferenced(XElement e, HashSet<string> referenced)
        {
            foreach (XElement item in e.DescendantsAndSelf("Item"))
            {
                string id = ServiceId(item);
                if (id.Length > 0) referenced.Add(id);
            }
        }

        private static string ServiceId(XElement item)
        {
            XAttribute a = item.Attribute("serviceid");
            string id = a == null ? "" : a.Value.Trim();
            int at = id.IndexOf('@');
            return at >= 0 ? id.Substring(at + 1) : id;
        }

        private static bool IsHidden(XElement e)
        {
            XAttribute a = e.Attribute("hidden");
            if (a == null) return false;
            string v = a.Value.Trim();
            return v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NameOf(XElement e, string fallback)
        {
            XAttribute a = e.Attribute("name");
            string name = a == null ? "" : a.Value.Trim();
            return name.Length == 0 ? fallback : name;
        }
    }
}
