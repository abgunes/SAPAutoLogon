using System;
using System.Windows.Forms;

namespace SAPAutoLogon
{
    /// <summary>
    /// Reacts to the launch shortcuts while the main window is the active window.
    /// The key is swallowed only when the launch callback reports that a program was
    /// started; in every other case KeePass still sees the key.
    /// </summary>
    public sealed class LaunchShortcutFilter : IMessageFilter, IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;

        private Form mainWindow;
        private Func<LaunchTarget, bool> launch;
        private bool registered;

        public LaunchShortcutFilter(Form mainWindow, Func<LaunchTarget, bool> launch)
        {
            this.mainWindow = mainWindow;
            this.launch = launch;

            Application.AddMessageFilter(this);
            registered = true;
        }

        public bool PreFilterMessage(ref Message m)
        {
            try
            {
                if (m.Msg != WM_KEYDOWN) { return false; }

                Form window = mainWindow;
                Func<LaunchTarget, bool> callback = launch;
                if (window == null || callback == null) { return false; }

                // An open dialog is another active form, or disables the main window
                if (!object.ReferenceEquals(Form.ActiveForm, window) || !window.Enabled) { return false; }

                // The low bits of WParam hold the virtual key; never overflows on 64-bit
                int keyCode = unchecked((int)(m.WParam.ToInt64() & 0xFF));

                Keys modifiers = Control.ModifierKeys;
                LaunchTarget target = LaunchShortcut.Match(
                    keyCode,
                    (modifiers & Keys.Control) == Keys.Control,
                    (modifiers & Keys.Shift) == Keys.Shift,
                    (modifiers & Keys.Alt) == Keys.Alt);
                if (target == LaunchTarget.None) { return false; }

                // Holding the keys repeats WM_KEYDOWN: only the first press launches. The
                // repeats are swallowed without launching, so they do not reach the focused
                // control as a control character (possible beep).
                if (LaunchShortcut.ShouldSwallowRepeat(target, LaunchShortcut.IsAutoRepeat(m.LParam.ToInt64()))) { return true; }

                return callback(target);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (registered)
                {
                    registered = false;
                    Application.RemoveMessageFilter(this);
                }
            }
            catch (Exception) { }

            mainWindow = null;
            launch = null;
        }
    }
}
