# SAPAutoLogon

A KeePass 2.x plugin that adds an **SAP** tab to the Add/Edit Entry dialog. You pick a system from your SAP Logon list and the SAP connection fields of the entry are filled for you. Entries can then start SAP GUI or SAP Business Client straight from the KeePass entry list.

## What it does

- **SAP tab** in the Add Entry and Edit Entry dialogs, with two modes:
  - **Manual** (default): you type the SAP connection fields yourself.
  - **Automatic (from SAP Logon)**: a searchable list shows the systems from your SAP Logon. Selecting one fills the fields.
- Reads the same SAP Logon landscape files as SAP Logon itself, including the shared (central) landscape file and included files. It never changes SAP Logon files or the registry.
- Supports direct application servers, logon groups (message server), SAProuter strings and SNC.
- **SAP GUI** and **Business Client** columns in the entry list, and two items in the entry right-click menu, start SAP GUI (`sapshcut.exe`) or SAP Business Client (`NWBC.exe`).
- The values are stored as normal entry string fields, so they are also visible on the Advanced tab and the database stays readable without the plugin.

## Requirements

- Windows, with KeePass 2.x (built and checked against 2.5x/2.6x, .NET Framework 4.8)
- For launching: SAP GUI for Windows with `sapshcut.exe`, and/or SAP Business Client (`NWBC.exe`)
- For the system list: SAP Logon installed (without it the SAP tab still works in Manual mode)

## Install

1. Copy `SAPAutoLogon.dll` into the `Plugins` folder of your KeePass installation (for example `C:\Program Files\KeePass Password Safe 2\Plugins`).
2. Restart KeePass.

Before installing, remove any older SAP launch plugin that supplied `Gui`/`NBC` columns from the KeePass `Plugins` folder; otherwise both plugins load and you get two sets of menu items.

Alternatively copy `SAPAutoLogon.plgx` into the same `Plugins` folder (KeePass compiles it on the next start). See `packaging\build-plgx.ps1` for how to create it. Note: the `.plgx` has not been loaded in KeePass yet (only its sources were compile-checked); the `.dll` is the tested artifact.

The Tools menu then has an entry **SAPAutoLogon options...**.

## The SAP tab

1. Open Add Entry or Edit Entry and select the **SAP** tab. It always starts in **Manual** mode.
2. To import from SAP Logon, select **Automatic (from SAP Logon)**, type in the search box to filter (by name, system ID or server, not case sensitive) and select a system.
3. Selecting a system copies its values into the fields once. You can change every field afterwards, in both modes; a value is only overwritten when you pick a system again.
   - **Title**: set to the SAP Logon entry name, but only for a new entry. An existing entry keeps its title.
   - **User name**: filled when SAP Logon stores a user for the system.
   - **Password**: never filled and never changed.
   - A field that SAP Logon has no value for is cleared (so switching from a logon group system to a direct system leaves nothing behind), except Client and Language (see below). `transaction` is never touched by a pick.
   - **Client** and **Language**: when SAP Logon stores none for the system (or only blanks), the pick fills `100` and `EN`. A client or language that SAP Logon does store is used as it is. A pick overwrites the fields it maps, so picking a system that has no language replaces a language the entry already has with `EN` (and a client with `100`). Values you type yourself in Manual mode are never changed.
4. Press OK. The fields are saved with the entry. Cancel discards everything.

The status line at the bottom of the tab says "Ready to launch." or what is still missing for the SAP GUI / Business Client columns (for example "Not launchable yet: enter Client.", which you see when you clear the Client box by hand).

The SAP Logon files are read in the background the first time the SAP tab is shown; "Loading SAP Logon systems..." is shown meanwhile. If the local SAP Logon file cannot be read, the tab shows a short message and Automatic mode is disabled; Manual mode keeps working. If only the shared file or an included file cannot be read, the systems that could be read are listed and a warning names the missing file.

## Import from SAP Logon (group menu)

To fill a KeePass group with a whole SAP Logon folder at once, right-click a group in the group tree and choose **Import from SAP Logon** (the last item of the menu). The item shows the same SAP icon as the entries the import creates (the 16x16 `SAPAutoLogon/Resources/SAP.png`).

1. A popup opens at once and reads the SAP Logon files in the background ("Loading SAP Logon systems..." is shown meanwhile). At the top is a white header band with the SAP logo, the title and the name of the target group. Under it, the **Search:** box filters the folders and the systems live while you type (not case sensitive, by name, system ID, application server or message server); **Clear** (or Esc, but only while the search box has the focus) empties it. Ctrl+F puts the cursor in the search box; Enter in the search box moves to the tree (it never imports).

   A folder is shown when its own name matches (it then keeps all its systems and subfolders), when one of its systems matches, or when one of its subfolders is shown; typing `demo` shows the folder `Demo` with everything in it. The filter only changes what is shown: **Import always imports the whole selected folder**, also the systems the filter hides.

   The tree on the left shows the SAP Logon folders with a folder icon and the number of systems the import of that folder creates entries for, for example `Demo (6)` (the count includes the subfolders; each visible workspace is a top folder; systems that no folder contains are listed under **(no folder)**). The list on the right shows the systems directly in the selected folder with **Name**, **SID** and **Group/Server**; the caption above it names the folder path, for example `Systems in Local > Demo > Sub1`, and says `(1 of 2 match the search)` when the filter hides some of them. Warnings and errors are shown as on the SAP tab, and the shared landscape file and included files are read the same way. Hidden SAP Logon folders and workspaces are not shown.
2. Select a folder. The bold line under the lists asks for confirmation, for example `Import 6 systems into group 'SAP'?`, and a grey preview line below it says what the import will do to the target group, for example `6 entries and 6 groups will be created.` (or `0 entries and 0 groups will be created, 6 entries already present will be skipped.` when the group already holds them). The count includes the systems of all subfolders. **Import** stays disabled until a folder is selected: when the popup has loaded, no folder is selected (also while the tree has the keyboard focus), so Enter does nothing until you pick a folder with the mouse or the arrow keys. When SAP Logon has no SAP GUI systems at all (even if it has folders), the popup says "No SAP GUI systems found in the SAP Logon files." and **Import** stays disabled. When the search matches nothing, the tree is empty, the popup says "No folder or system matches the search." and **Import** is disabled until you change or clear the search.
3. Optional: tick **Set user name for imported entries** and type a user name. It becomes the **User name** of the entries this import creates for systems that have no user in SAP Logon; a user that SAP Logon stores always wins, entries that are skipped because they already exist are never changed, and the Password stays empty (it is a user name only; the default user name is stored in plain text in KeePass's configuration file, `KeePass.config.xml`, key `SAPAutoLogon-PluginOptionString`, so never type a password there). The preview line then adds, for example, `User name 'alice' will be set for 5 entries that have no user in SAP Logon.` (it counts only the entries the import creates). An empty or blank name sets nothing (a hint under the box says so). The tick and the name are remembered after an **Import** and filled in the next time the popup opens; **Cancel** does not remember them.
4. Press **Import** (or Enter on the tree or the list while a folder is selected). **Cancel**, Esc and the X change nothing. In the search box Esc first clears the text; Esc again (or Esc on any other control) closes the popup.

The popup remembers its window size and the position of the splitter between tree and list, after **Import** and also after **Cancel**, and opens the same way next time (never smaller than its minimum size and never larger than the screen).

What is created below the right-clicked group:

- Selecting folder X creates a group named X (also for a top folder such as `Local`) with X's subfolders as nested groups and the systems as entries in the group of their folder. Folders without a name get the name `(unnamed)`. Subfolders with the same name are merged into one group.
- Each entry gets the title (the SAP Logon entry name), the SAP fields it has a value for (see "Entry fields"; only non-empty ones are stored), the **User name** when SAP Logon stores one, otherwise the default user name when the box is ticked (otherwise none), and an empty **Password**. `transaction` is not set. An entry whose system has no client (or no language) in SAP Logon gets client `100` (language `EN`), so it can be launched at once; a client or language that SAP Logon stores is used as it is.
- The entries the import creates get the SAP logo as their icon (the left of the Title column), instead of the default key. The logo is the 16x16 picture `SAPAutoLogon/Resources/SAP.png`, stored as it is once in the database as a custom icon (about half a KB) and all imported entries share it; running the import again adds no second copy. You can change the icon of any entry at any time (Edit Entry, Icon button) and the import never changes it again. Existing entries and groups keep their icons, and entries created through the SAP tab keep the default key. There is no option to switch this off.
- Nothing existing is changed. A group with the same name is reused, and an entry is skipped when the group already has one with the same title and the same values for every connection field SAP Logon stores for that system (system ID, client, server, instance, message server, logon group). Fields SAP Logon leaves empty (typically the client) do not count, and the defaults `100` / `EN` are not part of the comparison either, so entries whose client you changed by hand (for example `200` instead of the default `100`) are still skipped. So running the same import again adds nothing, also after you completed the imported entries.
- When it is done a message gives the result, for example `6 entries added, 0 skipped (already present), 6 groups created`. The database is marked as modified but not saved; there is no undo in KeePass, so save (or close without saving) as usual.

The menu item is greyed out when no database is open or the workspace is locked, when the database file has the Windows read-only attribute, when no group is selected, and for the Recycle Bin and the groups inside it. The popup is closed (as Cancel) when KeePass locks the workspace. It uses the same SAP Logon read as the SAP tab: the result is kept for the KeePass session, a read with an error or a warning is tried again the next time.

## Entry fields

| Field (string field name) | Meaning |
|---------------------------|---------|
| `systemId` | System ID |
| `applicationServer` | Application server (direct connection) |
| `systemNumber` | Instance (system) number, for example `00` |
| `messageServer` | Message server host (logon group) |
| `messageServerPort` | Message server port (optional) |
| `group` | Logon group |
| `sapRouterString` | SAProuter string, for example `/H/router.example.test/S/3299` |
| `sncName` | SNC partner name |
| `sncType` | SNC level (quality of protection); empty means no SNC |
| `client` | Client; `100` when SAP Logon stores none (see below) |
| `language` | Logon language; `EN` when SAP Logon stores none (see below) |
| `transaction` | Start transaction (manual only) |
| entry **User name** | SAP user |
| entry **Password** | SAP password (never filled by the plugin) |

When a system is filled in from SAP Logon (a pick on the SAP tab in Automatic mode, or the group import) and SAP Logon stores no client, `client` becomes `100`; with no language, `language` becomes `EN`. A value SAP Logon stores always wins, and values typed in Manual mode are never changed. The defaults are fixed in the code, not options.

The field names are fixed. They follow the names used by SAP ADT for VS Code, so values can be compared with an ADT destination.

## Launching

The entry list can show two columns: **SAP GUI** and **Business Client**. The plugin adds them by itself the first time KeePass starts with it: they appear at the end of the list (140 pixels wide). If the list still has the previous Gui/NBC columns, these are renamed in place to SAP GUI and Business Client and keep their position; they keep their width when it is already at least 140 pixels, otherwise they are widened to 140. This happens only once: if you remove the columns later (View > Configure Columns), they stay removed, and you can turn them on again in View > Configure Columns at any time. The cell shows `▶ Click-To-Open` (in both columns) for an entry that has a complete connection (see below) when the matching program (`sapshcut.exe` or `NWBC.exe`) exists in the folder set in the options; otherwise the cell is empty.

To start the program:

- double-click the cell (KeePass runs a column's action only on a double-click, a single click just selects the entry),
- right-click the entry and choose **Open in SAP GUI** or **Open in Business Client**. The two items are always at the end of the entry menu; they are enabled only when exactly one entry is selected, the entry has a complete connection and the program exists, otherwise they are greyed out, or
- press **Ctrl+Shift+G** (SAP GUI) or **Ctrl+Shift+N** (Business Client). The two menu items show these keys on the right.

The keys act only while the KeePass main window is active and exactly one entry is selected that has a complete connection and whose program exists. In every other case (no or several entries selected, entry not launchable, program folder missing, a dialog or the import window open, database locked) nothing happens and no message is shown; KeePass handles the key as before. When a program is started, the key is not passed on to the entry list. Holding the keys down starts the program only once; the repeated key presses are swallowed too (no beep). The keys also work while the quick-find box of the toolbar has the focus or a main-menu dropdown is open, because the main window is still the active window.

After you close the options dialog with OK, the entry list is redrawn, so the cells and menu items follow the new program folders at once.

An entry is **launchable** when it has `systemId`, `client` and either:

- `applicationServer` and `systemNumber` (direct), or
- `messageServer` and `group` (logon group).

For SAP GUI the plugin starts `sapshcut.exe` with the system, client, user, password, language, connection string (including SAProuter), SNC settings, start transaction, and "maximized window" if enabled in the options. Values with spaces or special characters are quoted.

The folders of `sapshcut.exe` and `NWBC.exe` are detected automatically. The options dialog (Tools > SAPAutoLogon options...) shows them; they can be edited (a trailing backslash is optional), and **Detect** looks them up again. It also has the "Run with maximized window" setting.

Note: `sapshcut -pw` passes the password on the command line, where other programs running on the same PC can see it while the program is starting. This is how `sapshcut.exe` works.

## Where SAP Logon data is read from

Same order as SAP ADT for VS Code uses; the first match wins:

1. The SAP Logon registry settings of the current user (`HKCU\Software\SAP\SAPLogon\LandscapeFilesLastUsed`): local file and shared (global) file.
2. The environment variable `SAPLOGON_LSXML_FILE` (a landscape XML file; a sibling `SAPUILandscapeGlobal.xml` is used as the global file if present).
3. SAP Logon's Options registry key (`PathConfigFilesLocal`, plus the shared file settings), looked up in HKCU, HKLM and HKLM 32-bit view.
4. `%APPDATA%\SAP\Common\SAPUILandscape.xml`.

`%VAR%` in paths is expanded. `<Include>` entries (`file:///` and `http(s)://`) are followed. Services, message servers and routers from all files are merged. Only SAP GUI systems (`Service type="SAPGUI"`) are listed. The files are read in the background when the SAP tab is first shown in a dialog. A complete read is kept for the KeePass session; a read with an error or a warning is tried again in the next dialog.

## Not included

- No HTTP/HTTPS (Fiori, URL) entries and no other SAP Logon service types in the list.
- No writing back to SAP Logon, no update of existing entries by the import (existing entries are skipped, never changed).
- No default user name on the SAP tab (a pick there fills only the user that SAP Logon stores).
- Windows only (SAP GUI for Windows).

## Licence

GNU Affero General Public License v3.0 (AGPL-3.0). See [LICENSE](LICENSE).

---

Based on KPSAPLunch by David Suchopar (https://github.com/arteeecz/KeePass-plugins), AGPL-3.0.
