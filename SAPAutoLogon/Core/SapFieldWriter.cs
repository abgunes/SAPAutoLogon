using System;
using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// Writes SAP field values into a string store (the entry's string fields).
    /// An empty value removes the field; a value equal to the stored one is left alone.
    /// </summary>
    public static class SapFieldWriter
    {
        /// <param name="read">Returns the stored value, or "" when the field does not exist.</param>
        /// <returns>True when at least one field was set or removed.</returns>
        public static bool Apply(IDictionary<string, string> values, Func<string, string> read,
            Action<string, string> set, Action<string> remove)
        {
            if (values == null) return false;

            bool changed = false;
            foreach (KeyValuePair<string, string> pair in values)
            {
                string value = pair.Value ?? "";
                string current = read(pair.Key) ?? "";

                if (value.Length == 0)
                {
                    if (current.Length == 0) continue;
                    remove(pair.Key);
                    changed = true;
                }
                else if (!string.Equals(value, current, StringComparison.Ordinal))
                {
                    set(pair.Key, value);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
