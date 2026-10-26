namespace SAPAutoLogon
{
    /// <summary>
    /// Names of the KeePass custom string fields used by SAPAutoLogon.
    /// The names match the property names of the SAP ADT connection settings.
    /// </summary>
    public static class SapFields
    {
        public const string SystemId = "systemId";
        public const string ApplicationServer = "applicationServer";
        public const string SystemNumber = "systemNumber";
        public const string MessageServer = "messageServer";
        public const string MessageServerPort = "messageServerPort";
        public const string Group = "group";
        public const string SapRouterString = "sapRouterString";
        public const string SncName = "sncName";
        public const string SncType = "sncType";
        public const string Client = "client";
        public const string Language = "language";
        public const string Transaction = "transaction";

        /// <summary>Fields that can be filled from an SAP landscape (all except Transaction).</summary>
        public static readonly string[] FromLandscape = new string[]
        {
            SystemId, ApplicationServer, SystemNumber, MessageServer, MessageServerPort,
            Group, SapRouterString, SncName, SncType, Client, Language
        };

        /// <summary>All fields: FromLandscape followed by Transaction.</summary>
        public static readonly string[] All = new string[]
        {
            SystemId, ApplicationServer, SystemNumber, MessageServer, MessageServerPort,
            Group, SapRouterString, SncName, SncType, Client, Language, Transaction
        };
    }
}
