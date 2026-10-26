namespace SAPAutoLogon
{
    /// <summary>
    /// Where KeePass looks for the version information file of the plugin
    /// (Plugin.UpdateUrl). The file lives in the repository next to the source,
    /// in Info/keepass_plugin.ver, and is changed together with the assembly
    /// version when a release is made.
    /// </summary>
    public static class UpdateSource
    {
        public const string VersionFileUrl =
            "https://raw.githubusercontent.com/abgunes/SAPAutoLogon/main/SAPAutoLogon/Info/keepass_plugin.ver";
    }
}
