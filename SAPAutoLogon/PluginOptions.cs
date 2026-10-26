using KeePass.App.Configuration;

namespace SAPAutoLogon
{
    public class PluginOptions
    {
        private readonly AceCustomConfig oKeePassConfig = null;

        private const string pluginOptions = SAPAutoLogonExt.PlugInName + "-" + "PluginOptionString";

        public PluginOptions(AceCustomConfig config)
        {
            oKeePassConfig = config;
        }

        public string PluginOptionString
        {
            get
            {
                string savedStr = oKeePassConfig.GetString(pluginOptions);
                if (string.IsNullOrEmpty(savedStr)) { savedStr = new PluginParameters().ToString(); }
                return savedStr;
            }

            set
            {
                oKeePassConfig.SetString(pluginOptions, value);
            }
        }
    }
}
