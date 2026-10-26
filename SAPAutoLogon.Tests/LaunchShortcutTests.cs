using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LaunchShortcutTests
    {
        [TestMethod]
        public void Ctrl_Shift_G_is_SapGui()
        {
            Assert.AreEqual(LaunchTarget.SapGui, LaunchShortcut.Match(LaunchShortcut.KeyG, true, true, false));
            Assert.AreEqual(0x47, LaunchShortcut.KeyG);
        }

        [TestMethod]
        public void Ctrl_Shift_N_is_BusinessClient()
        {
            Assert.AreEqual(LaunchTarget.BusinessClient, LaunchShortcut.Match(LaunchShortcut.KeyN, true, true, false));
            Assert.AreEqual(0x4E, LaunchShortcut.KeyN);
        }

        [TestMethod]
        public void Missing_ctrl_or_shift_gives_None()
        {
            int[] keys = { LaunchShortcut.KeyG, LaunchShortcut.KeyN };
            foreach (int key in keys)
            {
                Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(key, false, false, false));
                Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(key, true, false, false));
                Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(key, false, true, false));
            }
        }

        [TestMethod]
        public void Alt_gives_None()
        {
            // AltGr on some keyboard layouts is reported as Ctrl+Alt
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(LaunchShortcut.KeyG, true, true, true));
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(LaunchShortcut.KeyN, true, true, true));
        }

        [TestMethod]
        public void Other_keys_give_None()
        {
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match('H', true, true, false));
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match('F', true, true, false));
        }

        [TestMethod]
        public void Lowercase_or_other_codes_give_None()
        {
            // 0x67 is the virtual key of numpad 7, 'g' (0x67) and 'n' (0x6E) are not key codes of the letters
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(0x67, true, true, false));
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(0x6E, true, true, false));
            Assert.AreEqual(LaunchTarget.None, LaunchShortcut.Match(0, true, true, false));
        }

        [TestMethod]
        public void IsAutoRepeat_true_when_bit_30_set()
        {
            Assert.IsTrue(LaunchShortcut.IsAutoRepeat(0x40000000L));
            Assert.IsTrue(LaunchShortcut.IsAutoRepeat(0x40000001L));
        }

        [TestMethod]
        public void IsAutoRepeat_false_for_first_press()
        {
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(0x00000001L));
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(0x20000001L));
        }

        [TestMethod]
        public void IsAutoRepeat_ignores_other_bits()
        {
            Assert.IsTrue(LaunchShortcut.IsAutoRepeat(0x7FFFFFFFL));
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(0x3FFFFFFFL));

            // 64-bit and negative values must not throw
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(0x100000000L));
            Assert.IsTrue(LaunchShortcut.IsAutoRepeat(0x140000000L));
            Assert.IsTrue(LaunchShortcut.IsAutoRepeat(-1L));
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(long.MinValue));
            Assert.IsFalse(LaunchShortcut.IsAutoRepeat(0L));
        }

        [TestMethod]
        public void ShouldSwallowRepeat_true_for_repeat_of_matched_shortcut()
        {
            Assert.IsTrue(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.SapGui, true));
            Assert.IsTrue(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.BusinessClient, true));
        }

        [TestMethod]
        public void ShouldSwallowRepeat_false_for_first_press_or_no_match()
        {
            Assert.IsFalse(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.SapGui, false));
            Assert.IsFalse(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.BusinessClient, false));
            Assert.IsFalse(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.None, true));
            Assert.IsFalse(LaunchShortcut.ShouldSwallowRepeat(LaunchTarget.None, false));
        }
    }
}
