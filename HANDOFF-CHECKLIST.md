# SAPAutoLogon hand-off checklist

Everything in the code and tests passes (unit tests only). The builder could not run KeePass with the plugin loaded and could not log on to real SAP systems. This list is what is left for the product owner. Tick `Pass` or `Fail` for each step and write down what you saw when something fails.

- **Part A**: checks inside KeePass (needs no SAP system).
- **Part B**: real SAP logon tests with the exact `sapshcut.exe` command the plugin sends.
- **Part C**: known limitations.

Test data used below is fake: system `S25`, client `100`, user `ALICE`, application server `10.0.0.5`, instance `00`, message server `ms1.example.test`, port `3600`, group `PUBLIC`, SAProuter `/H/router.example.test/S/3299`. Replace them with real values of one of your own systems when you test for real.

---

## Part A: checks in KeePass

### Setup

1. Make a new empty test database (`File > New`), so no real entries are at risk.
2. Copy `SAPAutoLogon.dll` (from `SAPAutoLogon\bin\Release\net48\` after `dotnet build SAPAutoLogon -c Release`) into KeePass's `Plugins` folder and restart KeePass. Use a scratch copy of KeePass if you prefer (a portable copy works too).
3. To test the SAP tab without your real SAP Logon file, point the plugin at the test landscape. Copy `SAPAutoLogon.Tests\TestData\landscape.xml` to, for example, `C:\Temp\landscape.xml`. Close KeePass completely (also from the tray; a second `KeePass.exe` only brings the running one to the front and keeps its old environment). Open a Command Prompt, set the variable and start KeePass **from the same Command Prompt**:

   ```
   set SAPLOGON_LSXML_FILE=C:\Temp\landscape.xml
   "C:\Program Files\KeePass Password Safe 2\KeePass.exe"
   ```

   KeePass reads environment variables only when it starts: after every change of the variable, close KeePass and start it again from the console that has the new value.

   The test landscape has these systems: `S25 - Development` (direct, user ALICE, SAProuter), `Q25 - Group` (logon group, port 3600, SAProuter), `S26 - SNC` (direct, SNC, no client in SAP Logon), `X1 - Odd port`, a second `S25 - Development` (server `10.0.0.7`, instance 02, no client in SAP Logon), `Q26 - No port` (logon group without port) and `Z - Broken group`. Only the first `S25 - Development` stores a client (`100`) and a language (`EN`) in SAP Logon; for every other system the plugin fills the defaults client `100` and language `EN` when you pick it or import it. The list shows each system as `Name  [System ID | server]`, for example `S25 - Development  [S25 | 10.0.0.5]` and `S25 - Development  [S25 | 10.0.0.7]`, so the two systems with the same name can be told apart.

   Which landscape wins on a PC with SAP Logon installed: the plugin looks in this order and takes the first file that exists: (1) the SAP Logon registry setting `HKCU\Software\SAP\SAPLogon\LandscapeFilesLastUsed` (value `LandscapeFile`), (2) the variable `SAPLOGON_LSXML_FILE`, (3) the SAP Logon `Options` registry key (`PathConfigFilesLocal`), (4) `%APPDATA%\SAP\Common\SAPUILandscape.xml`. So:
   - If (1) exists on your PC, it wins over the variable. To make the test landscape win, close SAP Logon and KeePass and temporarily rename the file that `LandscapeFile` points to (usually `%APPDATA%\SAP\Common\SAPUILandscape.xml`), then start KeePass from the console as above. Rename it back afterwards.
   - If only (3) or (4) exist, the variable already wins; nothing to rename.
   - Or skip the test landscape and do the steps with your real systems, comparing against what SAP Logon shows.

### A1. Options dialog

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Open the Tools menu. | There is an entry **SAPAutoLogon options...**. | [ ] Pass  [ ] Fail |
| 2 | Click it. | A small dialog opens with **SAP Gui path** and **SAP Business Client path** (each a text box with a **Detect** button next to it) and a **Run with maximized window** checkbox. The layout looks tidy (it was laid out by hand and never seen). | [ ] Pass  [ ] Fail |
| 3 | Toggle the checkbox, press OK, reopen the dialog. | The setting you chose is still there. | [ ] Pass  [ ] Fail |
| 4 | Toggle the checkbox again, press Cancel, reopen. | The change was not saved. | [ ] Pass  [ ] Fail |
| 5 | Toggle the checkbox, press **Enter**; reopen, toggle, press **Esc**; reopen. | Enter saved like OK, Esc discarded like Cancel. | [ ] Pass  [ ] Fail |
| 6 | Type a wrong folder into **SAP Gui path** (for example `C:\Temp`), press OK. | The box can be edited. The entry list is redrawn right away (no restart needed): the SAP GUI column shows no `▶ Click-To-Open` for the entry of A2 step 2 any more (needs the columns from A2 step 1), and the context menu item **Open in SAP GUI** is greyed out for it. Double-clicking that empty SAP GUI cell shows: "SAP GUI (sapshcut.exe) was not found. Set its folder in Tools > SAPAutoLogon options." and no Windows error dialog. | [ ] Pass  [ ] Fail |
| 7 | Reopen the dialog and click **Detect** next to SAP Gui path, press OK. | The SAP GUI folder (with `sapshcut.exe` in it) is filled in again; after OK the entry list is redrawn right away and `▶ Click-To-Open` is back in the SAP GUI column. Without SAP GUI installed, a warning says it was not found and the box is unchanged. Same for **Detect** next to SAP Business Client path. | [ ] Pass  [ ] Fail |
| 8 | Type the SAP GUI folder by hand **without** the trailing backslash (for example `C:\Program Files (x86)\SAP\FrontEnd\SAPgui`), press OK. | After OK, `▶ Click-To-Open` is shown in the SAP GUI column at once and a double-click on the cell starts SAP GUI (the backslash is optional). | [ ] Pass  [ ] Fail |

### A2. SAP GUI and Business Client columns, context menu

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Look at the entry list (the plugin adds the two columns on its first start, see A5). If you removed them earlier, `View > Configure Columns`, tick **SAP GUI** and **Business Client**. | The two columns are in the entry list with these exact headers. | [ ] Pass  [ ] Fail |
| 2 | Create an entry with custom string fields `systemId=S25`, `client=100`, `applicationServer=10.0.0.5`, `systemNumber=00` (Advanced tab > string fields, or use the SAP tab). | The SAP GUI column shows `▶ Click-To-Open` for it. This needs a complete connection and `sapshcut.exe` to exist in the SAP GUI folder of the options dialog (detected automatically when SAP GUI is installed). The Business Client column shows `▶ Click-To-Open` only when a Business Client folder with `NWBC.exe` is set, otherwise it is empty. The text looks like a button label and the column is wide enough to read it. The cell is started by a double-click, not by a single click. | [ ] Pass  [ ] Fail |
| 3 | Create a second entry with the same fields but `client` empty, and a third entry with no SAP fields. | Both SAP GUI and Business Client cells are empty for both. | [ ] Pass  [ ] Fail |
| 4 | Double-click the SAP GUI cell of the entry **without** a complete connection. | A warning appears: "This entry has no complete SAP connection (needs systemId, client and a server or message server)." Nothing crashes, nothing starts. | [ ] Pass  [ ] Fail |
| 5 | Right-click with **no entry selected** (empty area of the list, or an empty group). | The menu has a separator and, below it, **Open in SAP GUI** and **Open in Business Client** (with the small SAP icon) at the end. Both are visible but greyed out. | [ ] Pass  [ ] Fail |
| 6 | Select **one** entry with a complete connection (A2 step 2) and right-click it. | **Open in SAP GUI** is enabled. **Open in Business Client** is enabled only when the Business Client folder with `NWBC.exe` is set, otherwise greyed out. | [ ] Pass  [ ] Fail |
| 7 | Select one entry without a complete connection (A2 step 3) and right-click it. | Both items are visible and greyed out. | [ ] Pass  [ ] Fail |
| 8 | Select **two** entries (Ctrl+click, both with complete connections if you made two) and right-click. | Both items are visible and greyed out (they work for exactly one entry). | [ ] Pass  [ ] Fail |
| 9 | Select the one launchable entry, right-click, choose **Open in SAP GUI**. | The same thing happens as with a double-click on the SAP GUI cell of that entry (SAP GUI starts with the same command, see Part B). Open the menu on a second entry first, to check that the item acts on the entry you right-clicked. Choose **Open in Business Client** the same way (see B8). | [ ] Pass  [ ] Fail |
| 10 | Set a wrong SAP GUI folder (A1 step 6), then right-click the launchable entry. | **Open in SAP GUI** is greyed out. | [ ] Pass  [ ] Fail |
| 11 | Open the options dialog, change a program folder, press **Cancel**. | The entry list and the selection do not change. After **OK** the list is redrawn at once (the new folder is used for the cell text and for the menu items, without a restart) and the selected entry stays selected. | [ ] Pass  [ ] Fail |
| 12 | Restart KeePass (a plugin cannot be unloaded while KeePass runs) and right-click an entry. | The two items and the separator appear exactly once at the end of the menu, not twice. | [ ] Pass  [ ] Fail |

### A3. SAP tab (Add Entry / Edit Entry)

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Add Entry. Select the **SAP** tab. | The tab exists, **Manual** is selected, the system list is disabled, all fields can be typed in. For a moment "Loading SAP Logon systems..." may show above the list (Automatic is disabled while it shows); then the message goes away and Automatic can be chosen. At the bottom the status line says "Not launchable yet: enter System ID, Client, Application server + System number or Message server + Group." Type some values and press OK. | [ ] Pass  [ ] Fail |
| 2 | Open that entry again, look at the Advanced tab. | The values you typed are string fields of the entry (`systemId`, `client`, ...). | [ ] Pass  [ ] Fail |
| 3 | Add Entry. Type a password on the General tab first. On the SAP tab choose **Automatic**; the list shows the systems. Type `dev` in the search box. Pick the first of the two `S25 - Development` entries, shown as `S25 - Development  [S25 \| 10.0.0.5]` (the list is sorted by name and the sort keeps the file order for equal names). | Fields: System ID `S25`, Client `100`, Language `EN`, Application server `10.0.0.5`, System number `00`, SAProuter `/H/router.example.test/H/10.0.0.1`, no SNC. **Title** becomes `S25 - Development`, **User name** becomes `ALICE`, and the password you typed is unchanged. The status line says "Ready to launch." | [ ] Pass  [ ] Fail |
| 4 | Change `client` by hand on the SAP tab, press OK, reopen the entry. | The tab shows your changed value. | [ ] Pass  [ ] Fail |
| 5 | Edit Entry on an existing entry with a Title you recognise. Automatic, pick `S26 - SNC`. | Fields change (System ID `S26`, server `10.0.0.6`, number `01`, SNC name `p:CN=S26`, SNC level `3`, and Client `100` / Language `EN` because SAP Logon stores neither for this system). The **Title is not changed**. | [ ] Pass  [ ] Fail |
| 6 | Pick `Q25 - Group`, then pick `S25 - Development`. | After the group system: System ID `Q25`, Message server `ms1.example.test`, Port `3600`, Group `PUBLIC`, application server and number empty. After the direct system again: message server, port and group are empty, and they are gone from the Advanced tab string fields. | [ ] Pass  [ ] Fail |
| 7 | Change a field on the **Advanced** tab (for example `client`), then switch to the SAP tab. | The SAP tab shows the new value. | [ ] Pass  [ ] Fail |
| 8 | Change a field on the **SAP** tab, then switch to the Advanced tab. | The Advanced list shows the new value. | [ ] Pass  [ ] Fail |
| 9 | Edit Entry, change some fields, press **Cancel**, reopen the entry. | The old values are shown. Closing the dialog with the X after changes gives KeePass's usual "save changes?" question. | [ ] Pass  [ ] Fail |
| 10 | Pick the second `S25 - Development`, shown as `S25 - Development  [S25 \| 10.0.0.7]`. | Application server `10.0.0.7`, System number `02` (each of the two same-named entries gives its own values). This system has no client and no language in SAP Logon, so Client is `100` and Language is `EN` (the defaults) and the status line says "Ready to launch." | [ ] Pass  [ ] Fail |
| 11 | Type a start transaction in **Transaction**, then pick another system. | The transaction is kept. | [ ] Pass  [ ] Fail |
| 12 | On a PC where no SAP Logon file is found (see Setup step 3 for the order; rename the real files if needed), close KeePass, set `SAPLOGON_LSXML_FILE` to a file that does not exist and start KeePass from that console. Open Add Entry > SAP tab. | The tab opens and shows a short message, **Automatic** is disabled, **Manual** works, no error dialog appears. Then close KeePass, set the variable to the test landscape in the same console, start KeePass again from it and open a new dialog: the systems load. (Changing the variable without restarting KeePass has no effect.) | [ ] Pass  [ ] Fail |
| 13 | Open a read-only entry (a history version) and, separately, edit several selected entries at once. | No SAP tab and no error. | [ ] Pass  [ ] Fail |
| 14 | Restart KeePass (KeePass cannot switch a plugin off and on while it runs), then open an entry. | There is exactly one SAP tab. | [ ] Pass  [ ] Fail |
| 15 | Edit Entry on an entry with a complete SAP connection. On the SAP tab pick `S26 - SNC` (SAP Logon stores no client for it). Press OK. | Client `100` and Language `EN` are filled and the status line says "Ready to launch." After OK the SAP GUI column of that entry shows `▶ Click-To-Open` (this needs `sapshcut.exe` in the SAP GUI folder of the options dialog, see A2 step 2). | [ ] Pass  [ ] Fail |
| 16 | Edit the same entry again, select **Manual**, clear the **Client** box by hand. Then press OK. Edit again, type a client, OK. | The status line says "Not launchable yet: enter Client." (a value cleared by hand stays empty; only a pick fills the defaults). After the first OK the SAP GUI column of that entry is empty; after typing a client and OK, `▶ Click-To-Open` is back. | [ ] Pass  [ ] Fail |
| 17 | On the SAP tab clear System ID and the Application server, one at a time. | The status line names what is missing ("System ID", "Application server", ...) and changes back to "Ready to launch." when the values are back. | [ ] Pass  [ ] Fail |
| 18 | Edit Entry, change nothing, select the SAP tab, then the General tab, then close the dialog with the **X**. | No "save changes?" question (just looking at the SAP tab is no change). | [ ] Pass  [ ] Fail |
| 19 | Edit Entry. On the General tab change **Title**, **User name** and **Notes** without pressing OK. On the SAP tab (Automatic) pick a system that has no SAP Logon user (for example `X1 - Odd port`). | Back on the General tab your typed Title (Edit Entry keeps the title), User name and Notes are still there. Press OK and reopen: they are saved. | [ ] Pass  [ ] Fail |
| 20 | Windows display scaling **125 %** and then **150 %** (sign out/in if Windows asks). Open Edit Entry > SAP tab and make the dialog smaller and larger. | Nothing is cut off or overlapping: radio buttons, search box, list, all field labels and boxes, and the status line at the bottom are readable. A long message (for example the shared-file warning with a long path) wraps onto several lines instead of being cut. Same for the options dialog (Tools > SAPAutoLogon options...). | [ ] Pass  [ ] Fail |
| 21 | With your **real** SAP Logon landscape (about 440 systems): open the SAP tab, choose Automatic. | The number of systems in the list equals what SAP Logon shows (SAP GUI entries only, including those from the shared/central landscape). Typing in the search box filters without noticeable delay. Same-named systems are told apart by `[System ID \| server]`. Pick a logon-group system and a system that uses a SAProuter; compare the fields with SAP Logon's properties of those systems. | [ ] Pass  [ ] Fail |
| 22 | Make the shared (global/central) landscape unreachable (for example VPN off, or the share/web server not reachable). Restart KeePass, open Add Entry > SAP tab. | The dialog does not freeze: "Loading SAP Logon systems..." shows while the plugin waits for the share; you can switch tabs and type meanwhile. Then the list shows the local systems and a warning such as "Could not read the shared SAP Logon file: <path>". **Automatic** still works with the local systems. No error dialog. | [ ] Pass  [ ] Fail |
| 23 | Make the shared landscape reachable again (VPN on) and open a **new** entry dialog (no KeePass restart). | The SAP tab now lists all systems again and shows no warning (a load with a warning is retried in the next dialog). | [ ] Pass  [ ] Fail |
| 24 | While "Loading SAP Logon systems..." is shown (step 22), close the dialog with Cancel, and open another entry dialog right away. | No error, no crash; the second dialog gets the list when loading finishes. | [ ] Pass  [ ] Fail |

### A4. Import from SAP Logon (group menu)

Use the test landscape from Setup step 3. Its folders are `Local` (with `DE`, `Demo` > `Sub1` > `Sub1.1`, `Sub2`, a second `Sub2`, `Empty`, and a folder without a name that shows as `(unnamed)`) and the root `(no folder)`. It also has a hidden folder `Hidden` and a hidden workspace `Secret`, which must not appear. Use the empty group `SAP` (create it in the test database) as the target unless a step says otherwise. Selecting a folder X creates a group X below the right-clicked group. While the popup is open KeePass is blocked (modal), so another group cannot be selected in the meantime. The tree labels carry the number of systems, for example `Demo (6)`; the steps below mostly name the folders without it. The search box, the user name option and the remembered window layout of the popup are checked in A7.

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Right-click a group in the group tree. Then restart KeePass and right-click again. | The menu ends with a separator and **Import from SAP Logon** with the SAP icon (the same 16x16 logo the imported entries show, sharp at 100 %, not a blurred downscale: compare it with the icon of an entry from step 23, it must be the same artwork), exactly once, also after the restart. | [ ] Pass  [ ] Fail |
| 2 | Check when the item is enabled: with no database open; select the Recycle Bin (and a group inside it); set the `.kdbx` file's read-only attribute (Explorer > Properties), reopen the database in KeePass; then clear the attribute and reopen. | No database open: item greyed out. Recycle Bin or a group inside it: greyed out. Read-only attribute set: greyed out. Attribute cleared and reopened: enabled again on a normal group. The state "no group selected" cannot be provoked by right-click (a right-click selects the group); it is covered by the unit-tested rule, so there is nothing to check beyond the README statement. | [ ] Pass  [ ] Fail |
| 3 | Lock the workspace (Ctrl+L or Tools > Lock Workspace). Try to reach the group menu. With two databases open, lock one of them and look at the item while the locked one is active. | A locked database never offers a working import: the item is greyed out (if the group menu can be opened at all). Nothing is imported. | [ ] Pass  [ ] Fail |
| 4 | Click **Import from SAP Logon** on the empty group `SAP`. | The popup titled "Import from SAP Logon" is shown at once and says "Loading SAP Logon systems..." for a moment while the tree and the list are greyed out. Then the tree shows `Local (8)` and `(no folder) (1)`, each with a yellow folder icon; only the root `Local (8)` is expanded at first (expand `Demo (6)` and `Sub1 (3)` to see the rest: `Local (8)` holds `DE (1)`, `Demo (6)` > `Sub1 (3)` > `Sub1.1 (2)`, `Sub2 (1)`, `Sub2 (1)`, `Empty (0)`, `(unnamed) (1)`); no `Hidden` and no `Secret`. The list has the columns Name, SID and Group/Server and is empty, and the caption above it says "Systems in the selected folder". **Import** is disabled, the line below the lists reads "Select a folder to import." and the grey preview line under it is empty. The keyboard focus is on the tree, but no folder is selected or highlighted (not even `Local`), and **Import** stays disabled until you select a folder. | [ ] Pass  [ ] Fail |
| 5 | Select `Sub1`, then `DE`, then `Demo`, then `Sub1.1`. | `Sub1`: the list shows only `Q25 - Group \| Q25 \| PUBLIC` (direct systems; the systems of `Sub1.1` are not listed) and the line reads "Import 3 systems into group 'SAP'?" (the count includes subfolders). `DE`: "Import 1 system into group 'SAP'?" (`S25 - Development \| S25 \| 10.0.0.5`; the item `Ref` and the missing system are not listed). `Demo`: empty list, "Import 6 systems into group 'SAP'?". `Sub1.1`: `S26 - SNC` and `S25 - Development`, "Import 2 systems into group 'SAP'?". **Import** is enabled as soon as a folder is selected. The caption above the list names the path: `Systems in Local > Demo > Sub1` for `Sub1`, and `Systems in Local > Demo (none directly in this folder)` for `Demo`. The grey preview line under the question reads `3 entries and 2 groups will be created.` for `Sub1` on the empty group `SAP` (and `6 entries and 6 groups will be created.` for `Demo`, `2 entries and 1 groups will be created.` for `Sub1.1`; the wording does not use the singular). The selected folder shows an open folder icon. | [ ] Pass  [ ] Fail |
| 6 | Select `Sub1.1`, press **Import**. | The popup closes, the group tree and the entry list are refreshed once and the group `SAP` is selected. A message "2 entries added, 0 skipped (already present), 1 groups created" appears. Group `SAP/Sub1.1` holds `S26 - SNC` and `S25 - Development`. For `S25 - Development` the Advanced tab shows exactly the SAP fields it has (`systemId`, `client`, `applicationServer`, `systemNumber`, `language`, `sapRouterString`; no `transaction`), the General tab shows the Title and User name `ALICE`, the Password is empty, and there are no notes. `S26 - SNC` has `client` `100` and `language` `EN` (SAP Logon stores neither; the import fills the defaults), so it is launchable. The title bar shows the database as modified (`*`); the file on disk is unchanged until you save. | [ ] Pass  [ ] Fail |
| 7 | Select `Demo`, press **Import**. | "6 entries added, 0 skipped (already present), 6 groups created". Groups `SAP/Demo/Sub1/Sub1.1`, `SAP/Demo/Sub2` (one group holding `S25 - Development` with server `10.0.0.7` and `Q26 - No port`), `SAP/Demo/Empty` (empty) and `SAP/Demo/(unnamed)` (`X1 - Odd port`). The two landscape folders called `Sub2` became one group. | [ ] Pass  [ ] Fail |
| 8 | Repeat step 7 (select `Demo`, Import). | "0 entries added, 6 skipped (already present), 0 groups created". No duplicate groups or entries. The database may show as modified (`*`) even though nothing was added (the import always requests the modified flag). Delete one entry in `SAP/Demo/Sub2` and import `Demo` again: "1 entries added, 5 skipped (already present), 0 groups created", and the deleted entry is back in the existing group. | [ ] Pass  [ ] Fail |
| 9 | Select `Local`, Import. Then select `(no folder)`, Import. | A group `SAP/Local` appears with `X1 - Odd port` directly in it and the groups `DE`, `Demo` (with its sub-structure) below it; the message counts 8 systems (for example "8 entries added, 0 skipped (already present), 8 groups created" when `SAP/Local` did not exist). With `(no folder)`: a group `SAP/(no folder)` holding `Z - Broken group`. | [ ] Pass  [ ] Fail |
| 10 | Open the popup, select a folder, press **Cancel**. Again with **Esc**, and once with the X of the window. | Nothing changes: no group or entry is added, no refresh, and the database is not marked modified if it was clean before. (The popup only remembers its window size and splitter position, see A7 step 9.) | [ ] Pass  [ ] Fail |
| 11 | Keyboard and focus: open the popup and press **Enter** at once while "Loading SAP Logon systems..." shows (use a slow or unreachable shared landscape, see step 17, to have time). Then, when loaded, press Tab repeatedly. Select a folder with the arrow keys and press **Enter**. Open the popup again and press **Esc**. | While loading, Enter does nothing (the popup stays open; Import is disabled) and the focus is not on a button. When loaded, the focus is on the tree and no folder is selected: press **Enter** now, before selecting anything, and nothing happens (the popup stays open, nothing is imported). Also click into the list and back into the tree with nothing selected: still no folder is selected and Import stays disabled. Tab goes through the popup in reading order: search box, **Clear** (only while it is enabled), tree, list, the **Set user name for imported entries** box (and its text box when ticked), **Import** (when enabled), **Cancel**, then back to the search box. The splitter between tree and list may take one extra Tab stop (arrow keys move it); that is expected, not a failure. With a folder selected, Enter on the tree or the list imports (once). Esc closes the popup without importing (when the search box holds text, the first Esc only clears it, see A7 step 3). | [ ] Pass  [ ] Fail |
| 12 | Open the popup and let KeePass lock the workspace while it is open. Tools > Lock Workspace and Ctrl+L cannot be used while the modal popup is open, so use the automatic lock: set a short inactivity time under Tools > Options > Security (lock workspace after N seconds of inactivity) and wait, or use another way that makes KeePass lock (for example the tray icon menu, if it can be reached). Then unlock, open the popup again, and repeat once more. | The popup closes by itself (as Cancel), nothing is imported, the database stays locked and unchanged and is not marked modified, no error message appears. This also holds with the new popup: repeat once with text typed in the search box and the user name box ticked and filled. As with Cancel, the window size and splitter position are remembered afterwards, the user name option is not (A7 steps 7 and 9). That the layout is saved when the automatic lock closes the popup is expected from the code; note what you see. After unlocking, the item and the popup work as before; the repeat behaves the same. | [ ] Pass  [ ] Fail |
| 13 | Import into a group (for example `Demo` into `SAP`), then look for Undo (Ctrl+Z, Edit menu). | KeePass has no undo for this: Ctrl+Z does nothing for the import. Check that the message matches what appeared in the tree (the counts of added entries and created groups), that the database shows as modified (`*`) and that the file on disk is unchanged until you save. Delete the imported group by hand if you want it gone. | [ ] Pass  [ ] Fail |
| 14 | Open the popup. Look at where it appears and try to click the main KeePass window. If KeePass is set to be always on top (Tools > Options > Interface), repeat. | The popup is centred over the main KeePass window, can be moved and resized (never smaller than its minimum size), and the main window cannot be used while it is open. With "always on top" the popup is not hidden behind the main window. | [ ] Pass  [ ] Fail |
| 15 | Open the popup and press **Cancel** while it still shows "Loading SAP Logon systems..." (unreachable shared landscape, step 17). Then open the popup again, and open an entry dialog with the SAP tab right away. | No error and no crash when the load finishes in the background after the popup is gone. The next popup and the SAP tab get the list when the read ends (they share one read). | [ ] Pass  [ ] Fail |
| 16 | With your **real** landscape (about 440 systems): open the popup, select the top folder of a workspace, look at the tree, then Import into an empty group. | The popup opens without freezing; the tree matches SAP Logon's folders (same order, hidden folders left out; the tree can be navigated with the arrow keys and switching between folders with many systems is quick). "Import N systems into group '...'?" shows N equal to the SAP GUI entries SAP Logon shows in that workspace (a system that is in two folders counts in both). The import finishes in a few seconds with one refresh. | [ ] Pass  [ ] Fail |
| 17 | Make the shared landscape unreachable (as A3 step 22). Open the popup. Then point `SAPLOGON_LSXML_FILE` at a file that does not exist (A3 step 12) and open the popup. | Unreachable share: the popup stays responsive and shows "Loading SAP Logon systems..." (tree greyed out), then lists the local folders with the warning "Could not read the shared SAP Logon file: ..." under the lists; Import works. Missing local file: the popup shows the error text ("SAP Logon landscape file not found." or similar), the tree stays empty and greyed out, Import is disabled, and no error dialog appears. Long warning or error texts wrap and do not cut off or push the buttons out. | [ ] Pass  [ ] Fail |
| 18 | Windows display scaling **125 %** and **150 %** (also 100 % and, if you can, 200 %). Open the popup, make it as small as it allows, and larger. | The header band, search row, tree, list columns (Name, SID, Group/Server), caption, message, user name row, confirmation line, preview line and the buttons (Import left of Cancel, at the right edge) are readable and not cut off (the new parts are checked in detail in A7 steps 1 and 11). The splitter between tree and list can be moved. The dialog is resizable down to its minimum size. | [ ] Pass  [ ] Fail |
| 19 | After using the import, open Add Entry / Edit Entry > SAP tab. Also do it the other way round (SAP tab first, then the popup). | The SAP tab behaves as in A3 (same list, same warnings, same loading text, same error text). The import does not reload a clean result that the SAP tab already read, and the other way round. | [ ] Pass  [ ] Fail |
| 20 | Exit KeePass (File > Exit) after using the import, and restart it. | No error dialog on exit; after the restart the item appears once (step 1). | [ ] Pass  [ ] Fail |
| 21 | Optional, hard to provoke: if an import ever fails halfway (for example because of a disk or memory problem), note what happens. | A warning "SAPAutoLogon could not import from SAP Logon." with the reason appears, the tree is refreshed, and the database is marked modified after any failure that happens after the import started (and also after a pure re-import with 0 added), because the command always requests the modified flag with its single refresh. No crash. The warning does not give counts. | [ ] Pass  [ ] Fail |
| 22 | Create an empty group `SAP2`. Import `Sub1.1` into it. Edit `SAP2/Sub1.1/S26 - SNC` (it got the default client `100`), change the client to `200` on the SAP tab, press OK. Import `Sub1.1` into `SAP2` again. Then change the System ID of that entry to `S99`, OK, and import `Sub1.1` into `SAP2` once more. | First import: "2 entries added, 0 skipped (already present), 1 groups created". After changing the client to `200`: "0 entries added, 2 skipped (already present), 0 groups created" (a field SAP Logon leaves empty, here the client, does not count and the default `100` is not part of the comparison, so your changed entry is not duplicated). After changing the System ID: "1 entries added, 1 skipped (already present), 0 groups created" (a value SAP Logon stores must match), and `SAP2/Sub1.1` now holds your edited entry and a new `S26 - SNC`. | [ ] Pass  [ ] Fail |
| 23 | Create an empty group `SAP3` and import `Sub1.1` into it (best in a new database that has never had an import). Look at the entry list and the group `SAP3/Sub1.1`. | Both new entries show the SAP logo (blue, white lettering) at the left of the Title instead of the yellow key, at once, without closing and reopening the database. The groups keep their folder icon. | [ ] Pass  [ ] Fail |
| 24 | Entries that already existed keep their icon: before importing, give an entry that the import will skip (for example `SAP2/Sub1.1/S26 - SNC`) another icon with Edit Entry > Icon, then import `Sub1.1` into `SAP2` again. Also look at entries you made before with Add Entry. | The skipped entry and your own entries keep their icon (no SAP logo is added to them). Only entries that are newly added by an import get the logo. | [ ] Pass  [ ] Fail |
| 25 | Import `Demo` into `SAP`, then import `Demo` into `SAP2`. Open the custom icons of the database: Edit Entry > Icon button (the custom icons are listed below the standard ones) or the database maintenance dialog (Tools > Database Tools; the menu path can differ slightly between KeePass versions). | The database holds one SAP logo only, not one per import and not one per entry. | [ ] Pass  [ ] Fail |
| 26 | Edit one imported entry, choose another icon with Edit Entry > Icon, press OK. Delete another imported entry in the same group and import its folder again. | The icon you chose stays after the import. The entry that was deleted comes back with the SAP logo; the other entries keep what they had. | [ ] Pass  [ ] Fail |
| 27 | Save the database, close it and open it again (also after a KeePass restart). | The imported entries still show the SAP logo and there is still one SAP icon in the database. | [ ] Pass  [ ] Fail |
| 28 | Windows display scaling **100 %**, **125 %** and **150 %** (sign out/in if Windows asks): look at the entry list. | The SAP logo is not cut off at each scaling and is shown at about the same size as the key. At 125 % and 150 % it may look slightly soft, because the picture is only 16x16 (see Part C); that is expected. | [ ] Pass  [ ] Fail |

Note: if an earlier build of the plugin already imported entries into a database, that database still holds the older 64x64 icon under the same id (the plugin leaves an icon it finds there untouched, by design), so those entries do not show the new logo. To see the new one, delete the SAP custom icon (database maintenance > custom icons) and import again, or import into a fresh group of a database that has no SAP icon yet.

### A5. Columns added automatically

The plugin adds its two columns only once and remembers that in its own setting (`SAPAutoLogon-PluginOptionString` in `KeePass.config.xml`, part `columnsSetUp=True`). To test "first start" again, close KeePass, make a copy of `KeePass.config.xml` (back it up), remove `;columnsSetUp=True` from that setting, and start KeePass. A portable scratch copy of KeePass with its own config is the safest place for steps 1 and 2.

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Fresh config (a scratch KeePass that never had the plugin and has the default columns Title, User Name, Password, URL, Notes): copy the plugin in, start KeePass, open a database. | Right after the start, without a restart and without View > Configure Columns, the list shows **SAP GUI** and **Business Client** as the last two columns, about 140 pixels wide each. View > Configure Columns shows both ticked. The text `▶ Click-To-Open` is not cut off at 100 %, 125 % and 150 % display scaling. | [ ] Pass  [ ] Fail |
| 2 | Config with the previous Gui/NBC columns (for example your real config, after backing it up): start KeePass with the plugin. | The columns that were headed `Gui` and `NBC` are now headed **SAP GUI** and **Business Client**, at the same position; the width is 140 when the old column was narrower (92 in your config becomes 140) and unchanged when it was already 140 or more; no extra columns were added; the cells show `▶ Click-To-Open` in both columns as in A2, and the text is not cut off at 100 %, 125 % and 150 % display scaling. | [ ] Pass  [ ] Fail |
| 3 | Remove both columns (View > Configure Columns, untick both, OK). Exit KeePass (File > Exit) and start it again. | The columns do not come back. Tick them again in View > Configure Columns: they work as before. | [ ] Pass  [ ] Fail |
| 4 | Look at whether the columns appeared at once on the first start (steps 1 and 2) or only after a restart. | Expected: at once (KeePass rebuilds the list's columns right after loading the plugins). If they appear only after a restart, write that down: it is not harmful, but it is not what the builder expected. | [ ] Pass  [ ] Fail |
| 5 | Enforced configuration: check whether `Keepass.config.enforced.xml` next to `KeePass.exe` (in your install `C:\Program Files (x86)\KeePass2x`) contains `<EntryListColumnCollection>` under `<MainWindow>`. | Without `<EntryListColumnCollection>` in the enforced file: steps 1 to 4 behave as written. With it: the enforced list wins at every start, so the plugin's change lasts only until KeePass is closed, and because the plugin adds the columns only once they do not come back after a restart. In that case add the two columns to the enforced file (Type `PluginExt`, CustomName `SAP GUI` and `Business Client`) or rename `Gui`/`NBC` there. Note which case applies. | [ ] Pass  [ ] Fail |

### A6. Launch shortcuts (Ctrl+Shift+G, Ctrl+Shift+N)

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Select **one** entry with a complete connection (A2 step 2) in the entry list and press **Ctrl+Shift+G**. | SAP GUI starts, exactly as after right-click > Open in SAP GUI (see Part B). Nothing else happens in the list (no search, no selection change). | [ ] Pass  [ ] Fail |
| 2 | Same entry, press **Ctrl+Shift+N** (with a Business Client folder that has `NWBC.exe` set). | Business Client starts, as after right-click > Open in Business Client (see B8). | [ ] Pass  [ ] Fail |
| 3 | Select no entry (empty group) and press both shortcuts. Then select two entries and press both. Then select one entry without a complete connection (A2 step 3) and press both. | Nothing happens each time: no program starts and no dialog or warning appears. | [ ] Pass  [ ] Fail |
| 4 | Set a wrong SAP GUI folder (A1 step 6), select the launchable entry, press **Ctrl+Shift+G**; also with a wrong Business Client folder and **Ctrl+Shift+N**. Restore the folders afterwards. | Nothing happens and no dialog appears. | [ ] Pass  [ ] Fail |
| 5 | Open a dialog (for example **Tools > Options**, or the Add Entry dialog), and with a launchable entry selected in the list behind it press both shortcuts. Open the **Import from SAP Logon** window and do the same. Lock the database (Ctrl+L) and press both shortcuts. | Nothing starts in any of these cases. | [ ] Pass  [ ] Fail |
| 6 | In the Add/Edit Entry dialog (or the SAP tab), click a text box and type **Ctrl+Shift+G** and **Ctrl+Shift+N** while a launchable entry is selected in the main list. | Nothing starts and the key behaves as without the plugin (the dialog's own text box handles it). | [ ] Pass  [ ] Fail |
| 7 | Right-click an entry and look at the two launch items. | **Open in SAP GUI** shows `Ctrl+Shift+G` and **Open in Business Client** shows `Ctrl+Shift+N` on the right, like KeePass's own items. Choosing the item with the mouse launches once (not twice). | [ ] Pass  [ ] Fail |
| 8 | Restart KeePass (a plugin cannot be unloaded while KeePass runs), open the database, select a launchable entry and press **Ctrl+Shift+G**. | SAP GUI starts exactly once per key press (not twice), and the shortcut still works after the restart. | [ ] Pass  [ ] Fail |
| 9 | Select a launchable entry and hold **Ctrl+Shift+G** for about 2 seconds, then release. Same for **Ctrl+Shift+N**. | Exactly one SAP GUI session (or one Business Client) starts per hold, not a series of them, and there is no beep or other reaction from the entry list while the keys are held. | [ ] Pass  [ ] Fail |
| 10 | With a launchable entry selected, click into the toolbar quick-find box (or open a main-menu dropdown such as File) and press **Ctrl+Shift+G**. | The program starts. This is expected and harmless: the main window is still the active window. | [ ] Pass  [ ] Fail |

### A7. Import popup upgrade and default user name

Use the test landscape from Setup step 3. Create a new empty group `SAP7` in the test database as the target (more empty groups for single steps are named below). The steps build on each other in the order given; do not use a database you care about. The popup remembers its window size, its splitter position and the user name option in the plugin options (`SAPAutoLogon-PluginOptionString` in `KeePass.config.xml`, see A5), so a step that needs the default look starts from a state you set up yourself. The user name option is remembered after every **Import** (the tick and the text), so from step 4 on the next popup opens already ticked and filled in with the last name used (the cursor does not jump into the box); **Cancel** does not remember it. The steps below say what the box shows when the popup opens. When a step has you restart KeePass, start it from the console where `SAPLOGON_LSXML_FILE` is set (Setup step 3), or the test landscape is gone.

| # | Step | Expected | Result |
|---|------|----------|--------|
| 1 | Look of the popup at 100 % display scaling. Right-click `SAP7`, **Import from SAP Logon**. Wait until the tree is filled, select `Demo`. Then drag the window to its smallest size. | At the top a white header band with the SAP logo on the left, the title **Import from SAP Logon** in bold (larger than the rest) and below it a grey subtitle "Choose an SAP Logon folder. Its subfolders become groups and its systems become entries in group 'SAP7'.", closed by a thin grey line. All text uses the same font as KeePass's own dialogs (Segoe UI, not the old small font). Below: a **Search:** row with an empty box and a **Clear** button that is greyed out; the tree (folders with a yellow folder icon, the selected folder with an open folder icon) next to the list (select `Sub1.1` to see rows: each row starts with the small SAP logo); a bold question line and below it a grey preview line; the **Set user name for imported entries** box with a greyed text box next to it. At the smallest size (about 700x520) nothing is cut off or overlapping and the buttons stay at the bottom right. | [ ] Pass  [ ] Fail |
| 2 | Search. Open the popup and type `sub1` in the search box. Clear it and type `demo`. Clear it and type `10.0.0.7`, select the `Sub2` that is shown. Clear it, type `snc`, select `Sub1.1`, press **Import**. Open the popup again, select `Sub1.1`, then type `zzz`, then press **Clear**. | `sub1`: the tree shows only `Local (8)` > `Demo (6)` > `Sub1 (3)` > `Sub1.1 (2)`, everything expanded; `Sub2`, `DE` and the other folders are not shown (a folder whose own name matches keeps its whole subtree; the counts do not change while you filter). Capitals do not matter (`SUB1` gives the same). `demo`: `Demo (6)` is shown with all its subfolders (`Sub1 (3)` > `Sub1.1 (2)`, both `Sub2 (1)`, `Empty (0)`, `(unnamed) (1)`) and `Local (8)` above it. `10.0.0.7`: only `Local` > `Demo` > `Sub2 (1)`; selecting it shows one row `S25 - Development \| S25 \| 10.0.0.7` and the caption `Systems in Local > Demo > Sub2`. `snc`: only `Local` > `Demo` > `Sub1` > `Sub1.1`; selecting `Sub1.1` shows one row (`S26 - SNC`) and the caption `Systems in Local > Demo > Sub1 > Sub1.1 (1 of 2 match the search)`, while the question still says `Import 2 systems into group 'SAP7'?`. **Import** creates BOTH entries in `SAP7/Sub1.1` (the filter never limits the import; message `2 entries added, 0 skipped (already present), 1 groups created`). `zzz` with `Sub1.1` selected: the selection goes away (**Import** disabled), the tree is empty and the message reads "No folder or system matches the search."; **Clear** (greyed out while the box is empty) empties the box, brings the whole tree back with no folder selected, and the cursor is back in the search box. | [ ] Pass  [ ] Fail |
| 3 | Keyboard. Create the empty group `SAP6` first. Open the popup on `SAP7`. Press **Ctrl+F** (once with the focus on the tree, once with the focus on the list). Type `sub`, press **Esc**, then **Esc** again. Open the popup, select `Sub1.1`, press **Ctrl+F**, type `s`, press **Enter**; then Ctrl+F again and **Shift+Enter**. With text in the box press **Shift+Esc**. Open the popup, press **Ctrl+F**, type `s`, press **Enter** (nothing selected). Now open the popup on `SAP6`, select a folder with the arrow keys (for example `Sub1.1`) and press **Enter** on the tree. Open the popup on `SAP6` again, select a DIFFERENT folder (for example `DE`; the same folder again would add 0), click a row in the list and press **Enter**. | Ctrl+F puts the cursor in the search box with its text selected. The first Esc (also Shift+Esc) with text in the box clears it and the whole tree returns; the popup stays open. The next Esc (empty box) closes the popup without importing. Enter and Shift+Enter in the search box move the focus to the tree and never import (even with a folder selected and **Import** enabled: the popup stays open and nothing is created). With nothing selected, Enter in the search box moves to the tree, **Import** stays disabled and no folder gets selected. With a folder selected, Enter on the tree (and on the list) presses **Import** and imports once (`SAP6` gets `Sub1.1`, then `DE`). | [ ] Pass  [ ] Fail |
| 4 | Default user name. Open the popup (the box is still unticked and empty: no earlier Import used it), select `Demo` (preview `6 entries and 6 groups will be created.`). Tick **Set user name for imported entries** with the mouse. Type `alice`. Then select `DE`, then `Sub1.1`, then `Sub2` (either one), then `Demo` again. Press **Import** into `SAP7`. | Ticking enables the text box and puts the cursor in it. Typing `alice` with `Demo` selected gives the preview `6 entries and 6 groups will be created. User name 'alice' will be set for 5 entries that have no user in SAP Logon.` (the sentence updates while you type). The sentence counts only the entries the import creates for systems WITHOUT a user in SAP Logon: `DE` says `... for 0 entries ...` (its only system has the user `ALICE` in SAP Logon), `Sub1.1` (already imported into `SAP7` in step 2) says `0 entries and 0 groups will be created, 2 entries already present will be skipped. User name 'alice' will be set for 0 entries that have no user in SAP Logon.` (the count only covers entries that will be created), `Sub2` says `1 entries and 1 groups will be created. User name 'alice' will be set for 1 entries that have no user in SAP Logon.` After Import, in `SAP7/Demo/Sub1/Sub1.1` the entry `S25 - Development` has the User name `ALICE` (the one from SAP Logon wins, capitals kept) and the other five entries in `SAP7/Demo/...` have `alice`. The Password of every entry is empty. | [ ] Pass  [ ] Fail |
| 5 | Open the popup again: the box is already ticked with `alice` (remembered from step 4). Select `Demo`, click into the text box and replace the text with `bob`; the target is `SAP7` again. Press **Import**. Then delete `SAP7/Demo/Sub2/Q26 - No port` and import `Demo` into `SAP7` once more (the box is already ticked with `bob`, remembered). | First import: the preview reads `0 entries and 0 groups will be created, 6 entries already present will be skipped. User name 'bob' will be set for 0 entries that have no user in SAP Logon.`; the message is "0 entries added, 6 skipped (already present), 0 groups created" and no existing user name is changed (still `alice` / `ALICE`). Second import: preview `1 entries and 0 groups will be created, 5 entries already present will be skipped. User name 'bob' will be set for 1 entries that have no user in SAP Logon.`; message "1 entries added, 5 skipped (already present), 0 groups created"; `Q26 - No port` is back with the User name `bob`, all the others keep `alice` / `ALICE`. | [ ] Pass  [ ] Fail |
| 6 | Blank name. Create an empty group `SAP8`. Open the popup (the box is already ticked with `bob`, remembered), select `Demo`, click into the text box and delete the text (or type three spaces). Press **Import**. Open a second popup (the box is already ticked and empty, remembered), click into the text box first (the cursor does not jump into it), type `dave`, then untick the box and press **Cancel**. | With the box ticked and empty (or only spaces) a grey hint reads "Type a user name or untick the box; an empty user name is not set." under the row, and the preview has no user-name sentence. Import works and sets no User name on the new entries, except the `ALICE` of the SAP Logon system. After unticking, the hint disappears, the text box is greyed out but keeps `dave`, and the preview has no user-name sentence. | [ ] Pass  [ ] Fail |
| 7 | Remembered option. Create empty groups `SAP9` and `SAP10`. Open the popup (the box is ticked and empty, remembered from step 6), click into the text box, type `alice`, select `DE`, **Import** into `SAP9`. Reopen the popup. Replace the text with `carol`, untick the box (a greyed-out text box cannot be typed into), then press **Cancel**. Reopen: it must still be ticked with `alice`. Restart KeePass (from the console where `SAPLOGON_LSXML_FILE` is set, Setup step 3), reopen. Tools > SAPAutoLogon options..., press **OK**, reopen the popup. Finally (the box is ticked with `alice`) click into the text box, delete the text and **Import** `DE` into `SAP10`, then reopen. | After the Import the next popup opens with the box ticked and `alice` filled in (the cursor does not jump into the box). After the Cancel above it still opens ticked with `alice` (Cancel does not remember the user name option). Still `alice` after the restart and after OK in the options dialog (the options dialog does not erase the import settings; the remembered window size stays too). An Import with the box ticked and empty is remembered as ticked and empty (the hint shows). | [ ] Pass  [ ] Fail |
| 8 | Special characters. Create an empty group `SAP11`. Open the popup (the box is already ticked and empty, remembered from step 7), click into the text box, type `a;b=c%d`, select `Sub1.1`, **Import** into `SAP11`. Reopen the popup. Then open Tools > SAPAutoLogon options.... | The `S26 - SNC` entry in `SAP11/Sub1.1` has the User name `a;b=c%d` exactly (the `S25 - Development` entry keeps `ALICE`). The next popup shows `a;b=c%d` exactly in the box. The options dialog still shows your SAP GUI and Business Client folders and the same "Run with maximized window" setting as before, and the SAP GUI and Business Client columns still work. | [ ] Pass  [ ] Fail |
| 9 | Remembered layout (normal-size screen). Open the popup, resize it, drag the splitter between tree and list, press **Cancel**. Reopen. Do it again and press **Import** (with a folder selected) instead of Cancel. Restart KeePass (from the console where `SAPLOGON_LSXML_FILE` is set, Setup step 3), reopen. Then maximise the popup, close it with Cancel, reopen. Then drag it to its smallest size, close, reopen; then as large as possible, close, reopen. | The popup reopens with exactly the same window size and the same splitter position (tree width) after Cancel, after Import and after a KeePass restart (the layout is remembered for Cancel and Import alike). After maximising and closing, the next popup opens at the normal (not maximised) size it had before maximising, with the same splitter position as before (the remembered splitter position is not updated while the popup is maximised, so the tree is not wider than it was). A tiny size reopens at the minimum size (about 700x520 at 100 %), never smaller; a huge size reopens no larger than the working area of the primary screen. | [ ] Pass  [ ] Fail |
| 10 | Selection still starts empty. Open the popup, wait for the tree, press **Enter**. Click into the list, then click an empty part of the tree. Press **Ctrl+F**, type `s`, then click an empty part of the tree. Press **Tab** until the focus is on the tree. | Nothing happens on Enter (the popup stays open, **Import** disabled). After the clicks, with and without a filter typed, no folder is selected or highlighted until you click a folder; **Import** stays disabled meanwhile. Giving the tree the focus never selects a folder by itself. | [ ] Pass  [ ] Fail |
| 11 | Display scaling **125 %**, then **150 %** (sign out/in if Windows asks). To get the default first-open size at each scaling: close KeePass (File > Exit); back up `%APPDATA%\KeePass\KeePass.config.xml` (for a portable scratch copy of KeePass, the configuration file `KeePass.config.xml` sits next to `KeePass.exe` instead); in the entry `SAPAutoLogon-PluginOptionString` delete the last three parts of the value, from `;importDialogWidth=` to the end of the value, e.g. `;importDialogWidth=780;importDialogHeight=560;importSplitter=250`; save; start KeePass (from the console where `SAPLOGON_LSXML_FILE` is set, Setup step 3). Create a group with a very long name (about 80 characters) and open the popup on it. Make the window narrower and wider. | The first open is about 1.25x (at 150 %: 1.5x) the size of the popup at 100 %, and it cannot be dragged smaller than about 875x650 pixels at 125 % (about 1050x780 at 150 %; if that does not fit your screen, write down what happens). The header band shows the whole subtitle, wrapped onto at least two lines inside the band (not cut off at the right edge), and re-wraps when you resize the window. The title, search row, tree, list, caption (it wraps for a long folder path), user name row and hint, question, preview and buttons are readable and not cut off or overlapping. The SAP logo in the header and in the list rows is not cut off (it may look slightly soft: the picture is 16x16, see Part C). | [ ] Pass  [ ] Fail |
| 12 | Real landscape (about 440 systems): open the popup, type into the search box (for example a system ID and then part of a folder name), select a workspace root. | Typing filters without a noticeable delay and the tree stays usable. The number in the label of the workspace root equals the N in `Import N systems into group '...'?` when you select it. Long folder names show in full (the tree gets a horizontal scrollbar) and a long folder path in the caption wraps. | [ ] Pass  [ ] Fail |
| 13 | Right-click an entry and look at **Open in SAP GUI** / **Open in Business Client**. | The two entry menu items look as before (their small icon is unchanged on purpose; only the group menu item got the `SAP.png` icon, see A4 step 1). | [ ] Pass  [ ] Fail |

---

## Part B: real-system launch tests

The connection syntax for logon groups, SNC and SAProuter was taken from SAP documentation (SAP Note 103019, "SAPshortcut: Program parameters"). It was **not** tested against a real SAP system. The unit tests only prove that the plugin builds the strings below.

**All connection and quoting logic is in one file: `SAPAutoLogon\Core\SapShortcut.cs`.** If a case fails, that is the only file to change for the syntax (the tests in `SAPAutoLogon.Tests\SapShortcutTests.cs` show every expected string). The Business Client wrapping is the one exception, see B6.

### How to test

1. Run `sapshcut.exe` by hand first, in a Command Prompt, with the line shown in each case, using your real values. (Folder is usually `C:\Program Files (x86)\SAP\FrontEnd\SAPgui\`.) Compare with what SAP GUI does.
2. Then double-click the SAP GUI cell of the same entry in KeePass (or right-click the entry > **Open in SAP GUI**; it is the same code). To see the exact command KeePass sent, open Task Manager > Details, right-click a column header, `Select columns > Command line`, and look at the `sapshcut.exe` line right after you double-click (it only lives a few seconds). The line must be the same as the expected one, with real values.
3. The password is visible there. Use a test user or change the password afterwards.

All examples below assume: user `ALICE`, password `secret`, language `EN`, "Run with maximized window" ticked. Without the tick, `-maxgui` is missing at the end. Empty fields are left out (for example no `-language=` if language is empty). The order of arguments is always: `-system -client -user -pw -language -guiparm -command -snc_name -snc_qop -maxgui`.

### B1. Direct application server

Entry: `systemId=S25`, `client=100`, `applicationServer=10.0.0.5`, `systemNumber=00`, `language=EN`.

Expected command line:

```
sapshcut.exe -system=S25 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/H/10.0.0.5/S/3200" -maxgui
```

Expected result: SAP GUI opens and logs on to S25, client 100, as ALICE without asking anything.

If it fails, try:
- `-gui="/H/10.0.0.5/S/3200"` instead of `-guiparm="..."` (same thing; SAP Note 103019 names `-gui`, the installed help screen names `-guiparm`).
- `-guiparm="10.0.0.5 00"` (the help screen of `sapshcut.exe` 8.00 shows this form: host and instance number separated by a space).

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B2. Direct, through SAProuter

Same entry plus `sapRouterString=/H/router.example.test/S/3299`.

```
sapshcut.exe -system=S25 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/H/router.example.test/S/3299/H/10.0.0.5/S/3200" -maxgui
```

The router string is simply put in front of `/H/<host>/S/32<NN>`. Router strings in SAP Logon look like `/H/host/S/3299` or `/H/host/H/ip`; a router string that ends in `/H/` has that ending removed first, so `/H/` never appears twice.

If it fails, try:
- `-gui="..."` instead of `-guiparm="..."`.
- `-sysname="<SAP Logon entry name>"` (see B7).

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B3. Logon group with message server port

Entry: `systemId=S26`, `client=100`, `messageServer=ms1.example.test`, `messageServerPort=3600`, `group=PUBLIC`, `language=EN`.

```
sapshcut.exe -system=S26 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/M/ms1.example.test/S/3600/G/PUBLIC" -maxgui
```

(With a router, `<sapRouterString>` goes in front: `-guiparm="/H/router.example.test/S/3299/M/ms1.example.test/S/3600/G/PUBLIC"`.)

If it fails, try:
- `-gui="..."` instead of `-guiparm="..."`.
- `-guiparm="/R/S26/M/ms1.example.test/G/PUBLIC"` (the `/R/<systemId>/M/<host>/G/<group>` form that SAP also documents; it has no port, and with a router the router string stays in front).
- `-sysname="<SAP Logon entry name>"` (see B7).

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B4. Logon group without message server port

Same as B3 but `messageServerPort` empty.

```
sapshcut.exe -system=S26 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/M/ms1.example.test/G/PUBLIC" -maxgui
```

(The `/S/<port>` part is left out.)

If it fails, try the same alternatives as B3: `-gui`, `/R/S26/M/ms1.example.test/G/PUBLIC`, `-sysname`. Also try typing the port `3600` (use your real port: `36` plus the instance number of the message server) into the Port field of the SAP tab; some systems need it.

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B5. SNC

Direct entry as B1 plus `sncName=p:CN=S25` and `sncType=3` (SNC level; SAP Logon `sncop="-1"` means SNC off and is stored as empty, which sends no SNC arguments at all).

```
sapshcut.exe -system=S25 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/H/10.0.0.5/S/3200" -snc_name=p:CN=S25 -snc_qop=3 -maxgui
```

An SNC name with spaces is quoted (`-snc_name="p:CN=S25, O=Corp"`). SNC arguments need `sapshcut.exe` version 659 or newer (SAP GUI 8.00 ships `sapshcut.exe` version 800) and a working SNC client library on the PC. With an empty Password field `-pw` is left out.

If it fails, try:
- `-gui` instead of `-guiparm`.
- Leave out `-snc_qop` (set the SNC level field empty) to use the system's default.
- `-sysname="<SAP Logon entry name>"`, which lets SAP Logon apply its own SNC settings (see B7).

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B6. Special characters in the password

Use a test user, set the password in KeePass, then double-click the SAP GUI cell. The expected argument and the expected result is that SAP GUI logs on, i.e. the password reaches SAP GUI unchanged.

| Password typed in KeePass | Expected `-pw` argument | Result |
|---------------------------|-------------------------|--------|
| `p w` (with a space) | `-pw="p w"` | [ ] Pass  [ ] Fail |
| `a b\"c` (space, backslash, quote, c) | `-pw="a b\\\"c"` | [ ] Pass  [ ] Fail |
| `a b\` (ends with a backslash) | `-pw="a b\\"` | [ ] Pass  [ ] Fail |

The quoting follows the standard Windows rules (a quote becomes `\"`; backslashes in front of a quote or at the very end are doubled). If `sapshcut.exe` does not follow them, the logon fails for these passwords only. Then change the password to avoid quotes and trailing backslashes, and adapt the quoting in `SapShortcut.cs` (`QuoteAlways`).

### B7. Fallback: let SAP Logon supply the connection (`-sysname`)

Use this if B2, B3, B4 or B5 fail. In a Command Prompt:

```
sapshcut.exe -sysname="Q25 - Group" -client=100 -user=ALICE -pw=secret -language=EN -maxgui
```

`<SAP Logon entry name>` is the name of the entry as shown in SAP Logon (it is also the title of an entry created from the SAP tab). SAP Logon then supplies message server, router and SNC settings itself. This only works on PCs whose SAP Logon has that entry, so it is not the default (a KeePass database is often shared between PCs). If `-sysname` works and the explicit string does not, tell the builder; the change is in `SapShortcut.cs` (`BuildArguments` / `BuildConnection`), and it needs a new field for the SAP Logon entry name (not part of the current fields).

### B8. Business Client column

Needs the Business Client path set (options dialog). The plugin runs:

```
NWBC.exe /SHORTCUT="<same arguments as the SAP GUI column>"
```

for example

```
NWBC.exe /SHORTCUT="-system=S25 -client=100 -user=ALICE -pw=secret -language=EN -guiparm="/H/10.0.0.5/S/3200" -maxgui"
```

This `/SHORTCUT="..."` wrapping is unchanged from earlier versions of the column code and is **unverified**. Note the inner quotes around the `-guiparm` value are not escaped, so this is likely to be split wrongly by Windows. Expected result: Business Client opens and logs on. If it does not, check what your Business Client version accepts (`NWBC.exe /?` or SAP's documentation for the `/SHORTCUT` parameter) and tell the builder. This is the one place that is not in `SapShortcut.cs`: the wrapping line is in `SAPAutoLogon\PluginColumn.cs` (class `PluginColumnBC`, `PerformActionInt`).

Result: [ ] Pass  [ ] Fail   Notes: ____________________

Also try the Business Client column (double-click, and right-click > **Open in Business Client**) with a test user whose password needs quoting (`p w`, then `a b\"c`, see B6). Expected per the code: `NWBC.exe /SHORTCUT="-system=... -pw="p w" ..."`; the quotes inside are not escaped, so this is the case most likely to fail.

Result: [ ] Pass  [ ] Fail   Notes: ____________________

### B9. Also quick-check

| Step | Expected | Result |
|------|----------|--------|
| Entry with a start transaction `SE38` | `-command=SE38` appears after `-guiparm`, and SAP GUI opens that transaction. | [ ] Pass  [ ] Fail |
| "Run with maximized window" unticked | No `-maxgui` at the end. | [ ] Pass  [ ] Fail |
| Entry without a password | No `-pw` argument; SAP GUI asks for the password. | [ ] Pass  [ ] Fail |
| `client` typed as `100 ` (with a trailing space) | `-client=100` (field values are trimmed; user name and password are sent exactly as stored). | [ ] Pass  [ ] Fail |

---

## Part C: known limitations

- **The password is visible on the command line** to other programs running on the same PC while `sapshcut.exe` starts. This comes with `sapshcut -pw` and cannot be avoided by the plugin.
- **Unreachable shared landscape:** the SAP Logon files are read in the background, so an unreachable share or web server no longer blocks the dialog. The list stays at "Loading SAP Logon systems..." until the read gives up (Windows' own timeout for a share can be tens of seconds; web addresses give up after about 5 seconds). Meanwhile Manual mode works.
- A complete load (no error, no warning) is kept until KeePass is restarted, so the list is not refreshed when SAP Logon files change while KeePass is running; restart KeePass. A load with an error or warning is tried again in the next dialog.
- The Business Client launch syntax is unverified (see B8).
- **Import from SAP Logon, folder semantics:** selecting a folder X creates a group named X (also for a workspace root such as `Local`) under the right-clicked group, with its sub-structure. If you prefer that selecting a folder imports only its CHILDREN directly under the right-clicked group (without a group for the selected folder itself), tell us: it is a one-line change.
- **Import: hidden folders** are taken from the file shape: SAP Logon `hidden="1"` nodes and workspaces are not shown and not imported. If SAP Logon shows a folder that the popup lacks, report it.
- **Import: read-only detection** only looks at the Windows read-only attribute of a local `.kdbx` file (KeePass has no read-only mode), so a database on a read-only share or opened from a URL is not detected.
- **Import: existing entries are never updated.** An entry that is already in the group (same title and connection values) is skipped, not changed; entries are only added.
- **Import: SAP logo as icon.** Only entries created by the import get the logo (the owner-provided 16x16 `SAP.png`, stored as it is as a custom icon once in the database; no option to switch it off). The SAP tab does not set an icon: entries made or edited there keep the default key until you change it yourself. If the icon cannot be added the import still works and the entries get the default key, without a message. The icon is only 16x16, so it may look soft at 125 % / 150 % display scaling; a bigger square version can be dropped in as `SAPAutoLogon/Resources/SAP.png` (rebuild the plugin required): a square `SAP.png` of another size is supported, the menu item and the list rows show it scaled to 16x16, and the entry icon stores the file as it is.
- **Import: order.** The folder order of the real landscape is kept in the tree and the groups, but the SAP tab's system list is sorted alphabetically.
- **Default client and language.** When SAP Logon stores no client for a system, a pick on the SAP tab (Automatic mode) and the import fill client `100`; with no language they fill `EN`. A client or language SAP Logon stores wins, and values typed in Manual mode are never changed. If you clear the Client box by hand, the entry is not launchable until a client is typed (the SAP tab shows "Not launchable yet: enter Client."). The defaults `100` / `EN` are fixed in the code; tell us if they should be configurable.
- **Import: unreachable shared landscape.** The first display of the popup on an unreachable shared landscape shows "Loading SAP Logon systems..." and then a warning; the popup does not freeze (same wait as A3 step 22).
- **Import: failure halfway.** If an import fails after it has started, a warning shows the reason but no counts, and the entries already added stay (the database is marked modified).
- **Import: default user name.** It applies only to entries the import creates for systems that have no user in SAP Logon (a stored user wins, skipped entries are never changed, the Password stays empty). The SAP tab pick does not use it (possible later extension).
- **Import: the search box only filters what is shown, never what is imported.** The search looks at a system's name, system ID, application server and message server (not the user, logon group, router or SNC fields). The preview line compares with the target group as it was when the popup opened.
- **Import: the remembered splitter position is not updated while the popup is maximised;** the position from the last time the popup was in its normal state is kept, so the tree is not too wide when the normal size comes back.
- **Import: the remembered popup size is stored in pixels** of the screen it was saved on. On another display scaling it is applied as it is and clamped to the screen (and to the minimum size).
- **The entry-menu icons were left unchanged** (**Open in SAP GUI** / **Open in Business Client**; possible later extension: use `SAP.png` there too).
- One `.plgx` package is produced by `packaging\build-plgx.ps1`; the `.dll` is the main artifact. The `.plgx` is compiled by KeePass at first start and **has not been loaded in KeePass yet**. Its sources were compiled by the builder with the .NET Framework C# compiler and the script's reference list, which is the same compiler KeePass uses; please check once that KeePass loads it (copy only the `.plgx`, not also the `.dll`, into `Plugins`, restart KeePass, Tools > Plugins lists SAPAutoLogon).
