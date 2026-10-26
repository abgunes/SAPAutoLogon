using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>An existing KeePass entry, reduced to what duplicate detection needs.</summary>
    public sealed class EntrySnapshot
    {
        public string Title = "";

        /// <summary>The SapFields.FromLandscape values read from the entry ("" when missing).</summary>
        public Dictionary<string, string> Fields = new Dictionary<string, string>();
    }

    /// <summary>An existing KeePass group with its direct subgroups and entries.</summary>
    public sealed class GroupSnapshot
    {
        public string Name = "";

        /// <summary>Whatever the adapter wants back for this group; Core never inspects it.</summary>
        public object Handle;

        public List<GroupSnapshot> Groups = new List<GroupSnapshot>();
        public List<EntrySnapshot> Entries = new List<EntrySnapshot>();
    }

    /// <summary>An entry the import will add.</summary>
    public sealed class PlannedEntry
    {
        public string Title;

        /// <summary>Every SapFields.FromLandscape key; an empty value means "do not store".</summary>
        public Dictionary<string, string> Fields;

        /// <summary>User name from SAP Logon, or null when it has none.</summary>
        public string UserName;
    }

    /// <summary>A group the import will create or reuse, with what goes into it.</summary>
    public sealed class PlannedGroup
    {
        public string Name;

        /// <summary>Null means "create this group"; otherwise the Handle of the matching snapshot group.</summary>
        public object ExistingHandle;

        public List<PlannedGroup> Groups = new List<PlannedGroup>();
        public List<PlannedEntry> Entries = new List<PlannedEntry>();
    }

    public sealed class ImportPlan
    {
        public PlannedGroup Root;
        public int EntriesToAdd;
        public int EntriesSkipped;
        public int GroupsToCreate;
    }

    /// <summary>
    /// Decides which groups to create or reuse and which entries to add or skip
    /// when a SAP Logon folder is imported into a KeePass group.
    /// </summary>
    public static class ImportPlanner
    {
        public static ImportPlan Plan(SapFolder folder, GroupSnapshot target)
        {
            ImportPlan plan = new ImportPlan();
            if (folder == null)
            {
                return plan;
            }

            GroupSnapshot match = FindExisting(IndexByName(target == null ? null : target.Groups), folder.Name);

            List<SapFolder> sources = new List<SapFolder>();
            sources.Add(folder);
            plan.Root = PlanGroup(NameOf(folder.Name), sources, match, plan);
            return plan;
        }

        /// <summary>
        /// Number of planned entries in the group and all its subgroups that have no user name
        /// from SAP Logon (null or empty). Zero for null.
        /// </summary>
        public static int CountEntriesWithoutUserName(PlannedGroup root)
        {
            if (root == null)
            {
                return 0;
            }

            int count = 0;
            foreach (PlannedEntry entry in root.Entries)
            {
                if (entry != null && string.IsNullOrEmpty(entry.UserName))
                {
                    count++;
                }
            }
            foreach (PlannedGroup child in root.Groups)
            {
                count += CountEntriesWithoutUserName(child);
            }
            return count;
        }

        /// <summary>The connection fields that decide whether an entry is already present.</summary>
        private static readonly string[] KeyFields = new string[]
        {
            SapFields.SystemId,
            SapFields.Client,
            SapFields.ApplicationServer,
            SapFields.SystemNumber,
            SapFields.MessageServer,
            SapFields.Group
        };

        /// <summary>
        /// True when an existing entry counts as the SAP Logon system already
        /// imported: the same title and, for every connection field SAP Logon
        /// stores (non-empty), an equal value. Fields SAP Logon leaves empty,
        /// typically the client, match anything, so values completed by hand do
        /// not cause a duplicate. Values are trimmed and compared ordinally;
        /// missing keys count as empty.
        /// </summary>
        public static bool MatchesExisting(string sapTitle, IDictionary<string, string> sapFields, string existingTitle, IDictionary<string, string> existingFields)
        {
            if (Clean(sapTitle) != Clean(existingTitle))
            {
                return false;
            }
            return KeyValuesMatch(KeyValues(sapFields), KeyValues(existingFields));
        }

        private static string[] KeyValues(IDictionary<string, string> fields)
        {
            string[] values = new string[KeyFields.Length];
            for (int i = 0; i < KeyFields.Length; i++)
            {
                values[i] = Value(fields, KeyFields[i]);
            }
            return values;
        }

        private static bool KeyValuesMatch(string[] sap, string[] existing)
        {
            for (int i = 0; i < sap.Length; i++)
            {
                if (sap[i].Length > 0 && sap[i] != existing[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Key values of the entries in one group, indexed by trimmed title.</summary>
        private sealed class EntryIndex
        {
            private readonly Dictionary<string, List<string[]>> m_byTitle =
                new Dictionary<string, List<string[]>>(System.StringComparer.Ordinal);

            public void Add(string title, IDictionary<string, string> fields)
            {
                string key = Clean(title);
                List<string[]> list;
                if (!m_byTitle.TryGetValue(key, out list))
                {
                    list = new List<string[]>();
                    m_byTitle[key] = list;
                }
                list.Add(KeyValues(fields));
            }

            public bool Contains(string title, IDictionary<string, string> fields)
            {
                List<string[]> list;
                if (!m_byTitle.TryGetValue(Clean(title), out list))
                {
                    return false;
                }
                string[] sap = KeyValues(fields);
                foreach (string[] candidate in list)
                {
                    if (KeyValuesMatch(sap, candidate))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static PlannedGroup PlanGroup(string name, List<SapFolder> sources, GroupSnapshot existing, ImportPlan plan)
        {
            PlannedGroup group = new PlannedGroup();
            group.Name = name;
            if (existing != null)
            {
                group.ExistingHandle = existing.Handle;
            }
            else
            {
                plan.GroupsToCreate++;
            }

            EntryIndex present = new EntryIndex();
            if (existing != null && existing.Entries != null)
            {
                foreach (EntrySnapshot e in existing.Entries)
                {
                    if (e != null)
                    {
                        present.Add(e.Title, e.Fields);
                    }
                }
            }

            foreach (SapFolder source in sources)
            {
                foreach (SapSystem system in source.Systems)
                {
                    if (system == null)
                    {
                        continue;
                    }
                    MappedEntry mapped = FieldMapper.Map(system);
                    if (!present.Contains(mapped.Title, mapped.Fields))
                    {
                        // Planned entries count as present for later systems in the same group.
                        present.Add(mapped.Title, mapped.Fields);
                        PlannedEntry entry = new PlannedEntry();
                        entry.Title = mapped.Title;
                        entry.Fields = mapped.Fields;
                        entry.UserName = mapped.UserName;
                        group.Entries.Add(entry);
                        plan.EntriesToAdd++;
                    }
                    else
                    {
                        plan.EntriesSkipped++;
                    }
                }
            }

            // Subfolders with the same name merge into one group at the first one's position.
            List<string> order = new List<string>();
            Dictionary<string, List<SapFolder>> byName = new Dictionary<string, List<SapFolder>>(System.StringComparer.Ordinal);
            foreach (SapFolder source in sources)
            {
                foreach (SapFolder child in source.Folders)
                {
                    if (child == null)
                    {
                        continue;
                    }
                    string childName = NameOf(child.Name);
                    List<SapFolder> list;
                    if (!byName.TryGetValue(childName, out list))
                    {
                        list = new List<SapFolder>();
                        byName[childName] = list;
                        order.Add(childName);
                    }
                    list.Add(child);
                }
            }

            if (order.Count > 0)
            {
                Dictionary<string, GroupSnapshot> existingChildren = IndexByName(existing == null ? null : existing.Groups);

                foreach (string childName in order)
                {
                    group.Groups.Add(PlanGroup(childName, byName[childName], FindExisting(existingChildren, childName), plan));
                }
            }

            return group;
        }

        private static string NameOf(string name)
        {
            return name ?? "";
        }

        /// <summary>First snapshot group per ordinal name.</summary>
        private static Dictionary<string, GroupSnapshot> IndexByName(List<GroupSnapshot> groups)
        {
            Dictionary<string, GroupSnapshot> index = new Dictionary<string, GroupSnapshot>(System.StringComparer.Ordinal);
            if (groups != null)
            {
                foreach (GroupSnapshot g in groups)
                {
                    if (g != null && !index.ContainsKey(NameOf(g.Name)))
                    {
                        index[NameOf(g.Name)] = g;
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// The existing group of that name, or null when there is none or it has no
        /// Handle (a group the adapter cannot hand back is planned as new).
        /// </summary>
        private static GroupSnapshot FindExisting(Dictionary<string, GroupSnapshot> index, string name)
        {
            GroupSnapshot match;
            if (index.TryGetValue(NameOf(name), out match) && match.Handle != null)
            {
                return match;
            }
            return null;
        }

        private static string Value(IDictionary<string, string> fields, string key)
        {
            string value;
            if (fields != null && fields.TryGetValue(key, out value))
            {
                return Clean(value);
            }
            return "";
        }

        private static string Clean(string value)
        {
            return value == null ? "" : value.Trim();
        }
    }
}
