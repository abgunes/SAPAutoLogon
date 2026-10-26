using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAPAutoLogon.Tests
{
    [TestClass]
    public class LaunchTextTests
    {
        [TestMethod]
        public void Column_names_are_the_program_names()
        {
            Assert.AreEqual("SAP GUI", LaunchText.GuiColumnName);
            Assert.AreEqual("Business Client", LaunchText.BusinessClientColumnName);
        }

        [TestMethod]
        public void Cell_text_is_a_play_symbol_and_click_to_open_in_both_columns()
        {
            Assert.AreEqual("▶ Click-To-Open", LaunchText.GuiCell);
            Assert.AreEqual("▶ Click-To-Open", LaunchText.BusinessClientCell);
            Assert.AreEqual("▶ ", LaunchText.CellGlyph);
            Assert.AreEqual("Click-To-Open", LaunchText.CellWording);
        }

        [TestMethod]
        public void Cell_shows_the_label_only_when_executable()
        {
            Assert.AreEqual("▶ Click-To-Open", LaunchText.Cell(true, LaunchText.GuiCell));
            Assert.AreEqual("", LaunchText.Cell(false, LaunchText.GuiCell));
            Assert.AreEqual("▶ Click-To-Open", LaunchText.Cell(true, LaunchText.BusinessClientCell));
            Assert.AreEqual("", LaunchText.Cell(false, LaunchText.BusinessClientCell));
        }

        [TestMethod]
        public void Menu_item_texts()
        {
            Assert.AreEqual("Open in SAP GUI", LaunchText.GuiMenuItem);
            Assert.AreEqual("Open in Business Client", LaunchText.BusinessClientMenuItem);
        }

        [TestMethod]
        public void Menu_item_is_enabled_only_for_one_launchable_entry_with_an_existing_program()
        {
            Assert.IsTrue(LaunchText.MenuItemEnabled(1, true, true));

            Assert.IsFalse(LaunchText.MenuItemEnabled(0, true, true));
            Assert.IsFalse(LaunchText.MenuItemEnabled(2, true, true));
            Assert.IsFalse(LaunchText.MenuItemEnabled(1, false, true));
            Assert.IsFalse(LaunchText.MenuItemEnabled(1, true, false));
            Assert.IsFalse(LaunchText.MenuItemEnabled(1, false, false));
        }
    }
}
