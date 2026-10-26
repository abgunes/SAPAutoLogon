using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using KeePassLib;
using KeePassLib.Security;

namespace SAPAutoLogon
{
    /// <summary>What an import actually attached to the database.</summary>
    public sealed class ImportResult
    {
        public int EntriesAdded;
        public int GroupsCreated;
    }

    /// <summary>
    /// The KeePass side of "Import from SAP Logon": reads a group into a snapshot
    /// for the planner and applies a finished plan to the database tree.
    /// </summary>
    public static class KeePassGroupImporter
    {
        /// <summary>Reads a group, its entries and (recursively) its subgroups. Every group's Handle is the PwGroup.</summary>
        public static GroupSnapshot Snapshot(PwGroup group)
        {
            GroupSnapshot snapshot = new GroupSnapshot();
            if (group == null)
            {
                return snapshot;
            }

            snapshot.Name = group.Name ?? "";
            snapshot.Handle = group;

            foreach (PwEntry entry in group.Entries)
            {
                EntrySnapshot e = new EntrySnapshot();
                e.Title = entry.Strings.ReadSafe(PwDefs.TitleField);
                foreach (string key in SapFields.FromLandscape)
                {
                    e.Fields[key] = entry.Strings.ReadSafe(key);
                }
                snapshot.Entries.Add(e);
            }

            foreach (PwGroup child in group.Groups)
            {
                snapshot.Groups.Add(Snapshot(child));
            }

            return snapshot;
        }

        /// <summary>
        /// Adds the planned groups and entries under target. Everything new is built
        /// detached first, then attached; a failure while building changes nothing,
        /// a failure while attaching leaves a consistent tree (each add is atomic)
        /// and is rethrown after the remaining adds were attempted.
        /// The entries it creates get the SAP logo as their custom icon (once built; the
        /// icon is stored once per database). If the icon cannot be added they keep the
        /// default icon and the import goes on. Existing entries and groups are untouched.
        /// Does not refresh the UI and does not set db.Modified.
        /// </summary>
        public static ImportResult Apply(ImportPlan plan, PwGroup target, PwDatabase db)
        {
            return Apply(plan, target, db, null);
        }

        /// <summary>
        /// As Apply(plan, target, db); a created entry whose SAP Logon system has no
        /// user name gets defaultUserName (trimmed; null or blank sets none).
        /// The plan and its counts are not changed.
        /// </summary>
        public static ImportResult Apply(ImportPlan plan, PwGroup target, PwDatabase db, string defaultUserName)
        {
            ImportResult result = new ImportResult();
            if (plan == null || plan.Root == null)
            {
                return result;
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }

            // Phase 1: build; nothing built here is attached to the database yet.
            Dictionary<PlannedGroup, PwGroup> groups = new Dictionary<PlannedGroup, PwGroup>();
            Dictionary<PlannedGroup, List<PwEntry>> entriesForExisting = new Dictionary<PlannedGroup, List<PwEntry>>();
            List<PwEntry> newEntries = new List<PwEntry>();
            Build(plan.Root, db, defaultUserName, groups, entriesForExisting, newEntries);

            // The icon only touches db.CustomIcons (never throws) and the new, still detached entries.
            // Nothing new, nothing to iconize: an all-skipped import must not add an unused icon.
            if (newEntries.Count > 0)
            {
                PwUuid iconUuid = SapIconHelper.EnsureIcon(db);
                if (!iconUuid.Equals(PwUuid.Zero))
                {
                    foreach (PwEntry entry in newEntries)
                    {
                        entry.CustomIconUuid = iconUuid;
                    }
                }
            }

            // Phase 2: attach; remember the first failure and keep going.
            Exception failure = null;
            try
            {
                if (plan.Root.ExistingHandle == null)
                {
                    Attach(target, groups[plan.Root], result, ref failure);
                }
                else
                {
                    AttachInto(plan.Root, groups, entriesForExisting, result, ref failure);
                }

                target.Touch(true, false);
            }
            catch (Exception ex)
            {
                if (failure == null)
                {
                    failure = ex;
                }
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
            return result;
        }

        /// <summary>True when group is the Recycle Bin or lies inside it. Never throws.</summary>
        public static bool IsInRecycleBin(PwGroup group, PwDatabase db)
        {
            try
            {
                if (group == null || db == null || db.RecycleBinUuid.Equals(PwUuid.Zero) || db.RootGroup == null)
                {
                    return false;
                }

                PwGroup bin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
                return bin != null && (ReferenceEquals(group, bin) || group.IsContainedIn(bin));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>True when the database file is local and carries the Windows read-only attribute. Never throws.</summary>
        public static bool IsFileReadOnly(PwDatabase db)
        {
            try
            {
                if (db == null || db.IOConnectionInfo == null || !db.IOConnectionInfo.IsLocalFile())
                {
                    return false;
                }

                string path = db.IOConnectionInfo.Path;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return false;
                }

                return (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            }
            catch
            {
                return false;
            }
        }

        private static void Build(
            PlannedGroup planned,
            PwDatabase db,
            string defaultUserName,
            Dictionary<PlannedGroup, PwGroup> groups,
            Dictionary<PlannedGroup, List<PwEntry>> entriesForExisting,
            List<PwEntry> newEntries)
        {
            PwGroup built;
            if (planned.ExistingHandle == null)
            {
                built = new PwGroup(true, true);
                built.Name = planned.Name ?? "";
            }
            else
            {
                built = planned.ExistingHandle as PwGroup;
                if (built == null)
                {
                    throw new InvalidOperationException("The import plan refers to a group that is not a KeePass group.");
                }
            }
            groups[planned] = built;

            // Entries of an existing group wait for the attach phase; those of a new group join it now.
            List<PwEntry> pending = null;
            if (planned.ExistingHandle != null)
            {
                pending = new List<PwEntry>();
                entriesForExisting[planned] = pending;
            }

            foreach (PlannedEntry pe in planned.Entries)
            {
                PwEntry entry = BuildEntry(pe, db, defaultUserName);
                newEntries.Add(entry);
                if (pending != null)
                {
                    pending.Add(entry);
                }
                else
                {
                    built.AddEntry(entry, true);
                }
            }

            foreach (PlannedGroup child in planned.Groups)
            {
                Build(child, db, defaultUserName, groups, entriesForExisting, newEntries);
                if (planned.ExistingHandle == null && child.ExistingHandle == null)
                {
                    built.AddGroup(groups[child], true);
                }
            }
        }

        private static PwEntry BuildEntry(PlannedEntry pe, PwDatabase db, string defaultUserName)
        {
            PwEntry entry = new PwEntry(true, true);
            entry.Strings.Set(PwDefs.TitleField, new ProtectedString(db.MemoryProtection.ProtectTitle, pe.Title ?? ""));

            // SAP Logon's own user wins; the default only fills the built entry, never the plan.
            string userName = FieldMapper.ApplyDefaultUserName(pe.UserName, defaultUserName);
            if (userName != null)
            {
                entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(db.MemoryProtection.ProtectUserName, userName));
            }
            entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(db.MemoryProtection.ProtectPassword, ""));

            if (pe.Fields != null)
            {
                // Defaults go on a copy: the plan (and its matching against existing entries) stays raw.
                Dictionary<string, string> fields = new Dictionary<string, string>(pe.Fields);
                FieldMapper.ApplyDefaults(fields);
                foreach (KeyValuePair<string, string> field in fields)
                {
                    if (!string.IsNullOrEmpty(field.Value))
                    {
                        entry.Strings.Set(field.Key, new ProtectedString(false, field.Value));
                    }
                }
            }
            return entry;
        }

        /// <summary>Attaches a fully built new group to parent and counts it, with its whole subtree, on success.</summary>
        private static void Attach(PwGroup parent, PwGroup built, ImportResult result, ref Exception failure)
        {
            try
            {
                parent.AddGroup(built, true);
            }
            catch (Exception ex)
            {
                if (failure == null)
                {
                    failure = ex;
                }
                return;
            }

            result.GroupsCreated += 1 + (int)built.GetGroups(true).UCount;
            result.EntriesAdded += (int)built.GetEntriesCount(true);
        }

        /// <summary>Adds the new entries and new child groups of an existing group, descending into existing children.</summary>
        private static void AttachInto(
            PlannedGroup planned,
            Dictionary<PlannedGroup, PwGroup> groups,
            Dictionary<PlannedGroup, List<PwEntry>> entriesForExisting,
            ImportResult result,
            ref Exception failure)
        {
            PwGroup parent = groups[planned];

            foreach (PwEntry entry in entriesForExisting[planned])
            {
                try
                {
                    parent.AddEntry(entry, true);
                    result.EntriesAdded++;
                }
                catch (Exception ex)
                {
                    if (failure == null)
                    {
                        failure = ex;
                    }
                }
            }

            foreach (PlannedGroup child in planned.Groups)
            {
                if (child.ExistingHandle == null)
                {
                    Attach(parent, groups[child], result, ref failure);
                }
                else
                {
                    AttachInto(child, groups, entriesForExisting, result, ref failure);
                }
            }
        }
    }
}
