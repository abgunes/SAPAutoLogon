namespace SAPAutoLogon
{
    /// <summary>The program a launch shortcut starts.</summary>
    public enum LaunchTarget
    {
        None,
        SapGui,
        BusinessClient
    }

    /// <summary>
    /// Maps a key combination to the program it launches: Ctrl+Shift+G for SAP GUI,
    /// Ctrl+Shift+N for Business Client.
    /// </summary>
    public static class LaunchShortcut
    {
        // Virtual key codes of the letters (same as their upper case characters)
        public const int KeyG = 0x47;
        public const int KeyN = 0x4E;

        /// <summary>
        /// Alt must not be pressed: on some keyboard layouts AltGr is reported as Ctrl+Alt
        /// and is used to type characters.
        /// </summary>
        public static LaunchTarget Match(int keyCode, bool ctrl, bool shift, bool alt)
        {
            if (!ctrl || !shift || alt) { return LaunchTarget.None; }

            if (keyCode == KeyG) { return LaunchTarget.SapGui; }
            if (keyCode == KeyN) { return LaunchTarget.BusinessClient; }
            return LaunchTarget.None;
        }

        /// <summary>
        /// True for the repeated key-down messages sent while a key is held: bit 30 of the
        /// message's lParam is the previous key state (set when the key was already down).
        /// </summary>
        public static bool IsAutoRepeat(long lParam)
        {
            return (lParam & 0x40000000L) != 0;
        }

        /// <summary>
        /// True when a key-down must be swallowed without launching: the repeat of a matched
        /// shortcut. Passed on, it would reach the focused control as a control character.
        /// </summary>
        public static bool ShouldSwallowRepeat(LaunchTarget target, bool isAutoRepeat)
        {
            return target != LaunchTarget.None && isAutoRepeat;
        }
    }
}
