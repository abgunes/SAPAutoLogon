using System.Collections.Generic;

namespace SAPAutoLogon
{
    /// <summary>
    /// A folder of an SAP Logon workspace tree: a name, child folders and the
    /// systems placed directly in it.
    /// </summary>
    public sealed class SapFolder
    {
        public string Name = "";
        public List<SapFolder> Folders = new List<SapFolder>();
        public List<SapSystem> Systems = new List<SapSystem>();

        /// <summary>Number of systems in this folder and all its descendants.</summary>
        public int CountSystems()
        {
            int count = Systems.Count;
            foreach (SapFolder f in Folders)
            {
                count += f.CountSystems();
            }
            return count;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
