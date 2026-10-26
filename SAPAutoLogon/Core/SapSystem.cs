namespace SAPAutoLogon
{
    /// <summary>
    /// One SAP GUI entry of an SAP Logon landscape, resolved to the values
    /// SAPAutoLogon stores in a KeePass entry. All values default to "".
    /// </summary>
    public sealed class SapSystem
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string SystemId { get; set; }
        public string ApplicationServer { get; set; }
        public string SystemNumber { get; set; }
        public string MessageServer { get; set; }
        public string MessageServerPort { get; set; }
        public string Group { get; set; }
        public string SapRouterString { get; set; }
        public string SncName { get; set; }
        public string SncType { get; set; }
        public string Client { get; set; }
        public string Language { get; set; }
        public string User { get; set; }
        public bool IsGroup { get; set; }

        public SapSystem()
        {
            Uuid = "";
            Name = "";
            SystemId = "";
            ApplicationServer = "";
            SystemNumber = "";
            MessageServer = "";
            MessageServerPort = "";
            Group = "";
            SapRouterString = "";
            SncName = "";
            SncType = "";
            Client = "";
            Language = "";
            User = "";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
