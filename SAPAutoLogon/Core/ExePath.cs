using System;
using System.IO;

namespace SAPAutoLogon
{
    /// <summary>Builds the full path of a program from a configured folder.</summary>
    public static class ExePath
    {
        /// <summary>
        /// Joins folder and program file name; a trailing backslash on the folder is optional,
        /// surrounding spaces and quotes are ignored. Returns "" when either part is empty
        /// or the folder is not a valid path. Never throws.
        /// </summary>
        public static string Combine(string folder, string exe)
        {
            if (folder == null || string.IsNullOrEmpty(exe))
                return "";

            string clean = folder.Trim().Trim('"').Trim();
            if (clean.Length == 0)
                return "";

            try
            {
                return Path.Combine(clean, exe);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
