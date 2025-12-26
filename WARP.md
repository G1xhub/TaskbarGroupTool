# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Development commands

Project is a .NET 8 WPF WinExe (`TaskbarGroupTool.csproj`). All commands should be run from the repo root.

### Build and run
- Build (Debug):
  - `dotnet build`
- Build (Release):
  - `dotnet build -c Release`
- Run the main desktop app (Debug configuration):
  - `dotnet run`
- Run the group popup window directly for a given group name (bypassing the main window):
  - `dotnet run -- "My Group Name"`
  - This passes the group name as the first argument; `App.xaml.cs` detects this and opens `GroupMenuWindow` instead of `MainWindow`.

### Tests and linting
- There are currently **no test projects** in this repository. If you add standard .NET test projects, you will typically run:
  - All tests: `dotnet test`
  - Single test (example filter expression): `dotnet test --filter "FullyQualifiedName~MyNamespace.MyTests.MyTestMethod"`
- No dedicated lint/format step is configured (no analyzers or tooling commands are defined in the repo). If you introduce analyzers or `dotnet format`, prefer wiring them up as `dotnet` tools or as CI steps.

## High-level architecture

### Overview
- Desktop application for Windows 11 that lets users define **taskbar groups** of applications and create **shortcuts** that open a small popup menu listing the group’s apps.
- Built with **WPF** on **.NET 8.0** as a single WinExe (`TaskbarGroupTool.csproj`).
- Core concepts:
  - **Groups** of applications (`TaskbarGroup`) persisted to JSON under `%APPDATA%/TaskbarGroupTool`.
  - **Shortcuts** (`.lnk` files) that start the app in a special group-menu mode.
  - **Usage statistics** for applications and groups.

Key folders/files:
- `App.xaml` / `App.xaml.cs` – application entry and startup mode selection.
- `MainWindow.xaml` / `.cs` – primary group management UI.
- `Windows/` – auxiliary windows (`AddApplicationWindow`, `GroupMenuWindow`, `StatisticsWindow`).
- `ViewModels/` – `MainViewModel`, `StatisticsViewModel` for binding-friendly state.
- `Models/` – POCOs for groups, icons, and usage stats.
- `Services/` – Windows integration, persistence, search, statistics, theme handling.
- `Styles/ModernStyles.xaml` – shared WPF styling resources (currently mostly used by secondary windows).

### Startup and execution modes
- Entry point: `App.xaml.cs` (`App : Application`).
- On startup, the app inspects `Environment.GetCommandLineArgs()`:
  - **Group menu mode** (popup only):
    - If a non-empty first argument exists (`args[1]`), the app:
      - Sets an AppUserModelID of the form `TaskbarGroupTool.menu.{groupName}`.
      - Instantiates `GroupMenuWindow` with the group name argument.
      - Does **not** show `MainWindow`.
  - **Main UI mode**:
    - If no such argument is present, the app:
      - Sets AppUserModelID to `TaskbarGroupTool.main`.
      - Shows `MainWindow` as the main WPF window.
- Shortcuts created by the app pass the group name as the first argument so that clicking them triggers the group menu mode.

### Data model and persistence

#### Groups (`Models/TaskbarGroup.cs`)
- `TaskbarGroup`:
  - `Name` – group display name.
  - `Applications : ObservableCollection<string>` – list of file paths to `.exe` or `.lnk` entries.
  - `Id` – GUID assigned on construction, used by some shortcut-generation paths.
- Persistence is handled by `Services/TaskbarManager.cs`:
  - Stores all groups as a JSON list at:
    - `%APPDATA%/TaskbarGroupTool/groups.json` (exact path built via `Environment.SpecialFolder.ApplicationData`).
  - `SaveGroups(List<TaskbarGroup>)` writes JSON and calls `SHChangeNotify(SHCNE_ASSOCCHANGED)` to notify Windows of changes.
  - `LoadGroups()` reads the JSON back into `List<TaskbarGroup>` (empty list if the file does not exist).

#### Usage statistics (`Models/UsageStatistics.cs`, `Services/StatisticsService.cs`)
- Application stats (`UsageStatistics`):
  - `ApplicationPath`, `ApplicationName`, `LaunchCount`, `LastUsed`, `UsageHistory : List<DateTime>`.
- Group stats (`GroupUsageStatistics`):
  - `GroupName`, `LaunchCount`, `LastUsed`, `UsageHistory : List<DateTime>`.
- `StatisticsService`:
  - Persists everything to `%APPDATA%/TaskbarGroupTool/statistics.json`.
  - Provides methods to record launches:
    - `RecordApplicationLaunch(path, name)`.
    - `RecordGroupLaunch(groupName)`.
  - Provides aggregate queries used by the UI:
    - `GetTopApplications(int)`, `GetTopGroups(int)`, `GetRecentlyUsedApplications(int)`, `GetTotalLaunches()`.
  - Maintains bounded histories (e.g., last 100 usage entries per app/group).

#### Configuration export, import, backups (`Services/ConfigurationService.cs`)
- Uses a simple `ExportData` wrapper object containing:
  - `Version` string (e.g. `"1.0"`).
  - `ExportDate` `DateTime`.
  - `Groups : List<TaskbarGroup>`.
- Features:
  - **Export single group** (`ExportSingleGroup(TaskbarGroup)`):
    - Prompts with a `SaveFileDialog` to write a `.tbg` JSON file.
  - **Import single group** (`ImportSingleGroup()`):
    - Reads a `.tbg` file via `OpenFileDialog`.
    - Returns the imported `TaskbarGroup` for the caller to merge/replace.
  - **Backups**:
    - `CreateBackup(List<TaskbarGroup>)` writes `Backup_yyyyMMdd_HHmmss.tbg` files under
      `%APPDATA%/TaskbarGroupTool/Backups` and keeps only the 10 most recent.
    - `GetAvailableBackups()` enumerates existing backup files ordered newest-first.
    - `RestoreBackup(string backupFile)` deserializes a backup and (with user confirmation) returns the list of groups to restore; the caller replaces the in-memory groups and persists them via `MainViewModel.SaveGroups()`.

#### Theme settings (`Services/ThemeService.cs`)
- Singleton `ThemeService.Instance` tracks a single `IsDarkMode` boolean (implements `INotifyPropertyChanged`).
- Preferences are stored in `%APPDATA%/TaskbarGroupTool/settings.json` using `System.Text.Json`.
- On first run (no settings file), `LoadThemePreference()` falls back to a Windows registry check:
  - Reads `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`.
  - Treats `0` as dark mode.

### Windows integration and shortcuts

#### Application registration (`Services/AppRegistrationService.cs`)
- Handles per-user registry setup so Windows knows about `TaskbarGroupTool.exe` and the custom shortcut file type:
  - Creates `HKCU\SOFTWARE\Classes\taskbargroup.lnk` to describe a custom link type.
  - Configures `...\shell\open\command` to invoke `TaskbarGroupTool.exe` with the clicked shortcut path as an argument.
  - Registers `TaskbarGroupTool.exe` under `HKCU\SOFTWARE\Classes\Applications\TaskbarGroupTool.exe` with appropriate open commands.
- `RegisterApplication()` is called from `TaskbarManager.CreateTaskbarShortcut` before creating shortcuts.
- `UnregisterApplication()` removes the registry keys (failures are ignored if keys are already missing).

#### Shortcut creation and startup (`Services/TaskbarManager.cs`, `Services/ShellLinkService.cs`)
- `TaskbarManager.CreateTaskbarShortcut(TaskbarGroup group, string iconPath = null)` is the main path used by the UI:
  - Resolves the running assembly’s `.exe` path.
  - Ensures a `Shortcuts` subfolder exists next to the `.exe`.
  - Builds a shortcut path `Shortcuts/{group.Name}.lnk`.
  - Chooses an icon:
    - Prefers the explicit `iconPath` argument.
    - Else tries `%EXE_DIR%/icons/logo.ico`.
    - Else falls back to the app path itself.
  - Calls `ShellLinkService.InstallShortcut(...)` to create a COM-based `.lnk` with:
    - Target path = app exe path.
    - AppUserModelID = `TaskbarGroupTool.menu.{group.Name}`.
    - Description = `Taskbar Group: {group.Name}`.
    - Arguments = `group.Name` (so the app starts in group menu mode).
  - Uses `SHChangeNotify(SHCNE_ASSOCCHANGED)` to inform Windows shell about changes.
- `ShellLinkService` contains the COM interop (`IShellLinkW`, `IPersistFile`, `IPropertyStore`, `PROPERTYKEY`) to set path, working directory, icon, AppUserModelID, and write the `.lnk` file.

#### Alternate integration path (`Services/TaskbarIntegrationService.cs`)
- Provides an alternative, more low-level and PowerShell-based way to create shortcuts and pin them to the taskbar; appears to be more experimental/legacy:
  - `CreateTaskbarShortcut(TaskbarGroup)` writes a `.lnk` file on the **desktop**, then calls `SHChangeNotify`.
  - `CreateWindowsShortcut` shows a manual binary `.lnk` construction and then calls `CreateShortcutWithPowerShell(...)` which generates the shortcut using a PowerShell script.
  - `PinToTaskbar(string shortcutPath)` uses another PowerShell script to call the `P&in to Taskbar` shell verb.
  - `IsPinnedToTaskbar(string shortcutPath)` checks if a shortcut is present in the user’s pinned taskbar folder under `%APPDATA%/Microsoft/Internet Explorer/Quick Launch/User Pinned/TaskBar`.
- The main flow currently used by `MainViewModel.CreateTaskbarShortcut` is via `TaskbarManager`/`ShellLinkService`, not this service.

### Application search

`Services/ApplicationSearchService.cs` encapsulates application and shortcut discovery for both the main window and `AddApplicationWindow`:
- Uses a mix of shell folders and direct directory traversals:
  - **Start Menu**: user and common start menu `Programs` folders plus the `ApplicationData` Start Menu path.
  - **Desktop**.
  - **Known folders**: Downloads, Documents, Pictures, Music, Videos (via `SHGetKnownFolderPath`).
  - **Installed programs**: `Program Files` and `Program Files (x86)`.
- `SearchApplications(string searchTerm)`:
  - Delegates to a `SearchDirectory` helper for each root.
  - Filters by substring match (case-insensitive) on file or directory names.
  - Distinguishes result types via `SearchResultType` enum: `Application`, `Shortcut`, `Folder`.
  - Limits recursion depth and total result count to keep searches responsive.
- Results are returned as `List<SearchResult>` and exposed to the UI as observable collections.

### View models and UI flow

#### Main window (`MainWindow.xaml` / `ViewModels/MainViewModel.cs`)
- `MainWindow` sets up the UI in XAML with three main panels:
  - **Groups & Search** (left): list of groups, application search box and results.
  - **Group Details** (center): group name, list of applications, icon selection, shortcut creation controls.
  - **Statistics summary** (right): top applications and groups, with refresh/detailed view buttons.
- `MainWindow` code-behind wires up event handlers and delegates to:
  - `MainViewModel` for most group and search operations.
  - `ConfigurationService` for import/export/backup/restore.
  - `StatisticsService` for high-level stats and to record group launches when shortcuts are created.
- `MainViewModel` responsibilities:
  - Holds `ObservableCollection<TaskbarGroup> Groups` and a `SelectedGroup` reference.
  - Manages `SearchTerm` and `ObservableCollection<SearchResult> SearchResults`.
  - Loads/saves groups via `TaskbarManager`.
  - Provides methods called from the window:
    - `AddNewGroup`, `DeleteSelectedGroup`, `SaveSelectedGroup`.
    - `AddApplicationToGroup`, `RemoveApplicationFromGroup`.
    - `MoveApplicationUp`, `MoveApplicationDown`.
    - `CreateTaskbarShortcut(string iconPath = null)` (wraps `TaskbarManager.CreateTaskbarShortcut` and offers to open the `Shortcuts` folder).

#### Group menu popup (`Windows/GroupMenuWindow.*`)
- Used when the app is started with a group name argument (e.g., from a `.lnk` created by `TaskbarManager`).
- Looks up the matching `TaskbarGroup` via `TaskbarManager.LoadGroups()`; if not found, creates an empty group with that name.
- Dynamically builds a vertical stack of menu items (icons + app names) for each application path in the group:
  - Extracts per-app icons via `System.Drawing.Icon.ExtractAssociatedIcon` and converts them to WPF `ImageSource`.
  - Applies hover and click behaviors in code-behind.
- `LaunchApplication(string appPath)`:
  - Starts the application via `ProcessStartInfo` with `UseShellExecute = true`.
  - Calls `StatisticsService.RecordApplicationLaunch` and `RecordGroupLaunch` for tracking.
  - Closes the window after launch.
- Auto-closes when deactivated or when the user presses `Esc`.
- Computes its own size based on the number of applications and positions itself relative to the taskbar and current mouse position using Win32 APIs and `System.Windows.Forms.Screen`.

#### Add application dialog (`Windows/AddApplicationWindow.*`)
- Modal dialog to add applications to the currently selected group.
- Uses its own `ApplicationSearchService` instance and maintains a local `ObservableCollection<SearchResult>` for results.
- Offers two paths:
  - Search via a text box (press Enter or click Search).
  - Browse filesystem via `OpenFileDialog` to pick `.exe` or `.lnk` files.
- Adds selected applications’ paths to the provided `TaskbarGroup.Applications` collection.

#### Statistics window (`Windows/StatisticsWindow.*`, `ViewModels/StatisticsViewModel.cs`)
- Separate window that provides a richer view of usage metrics:
  - **Top Applications** and **Top Groups** lists (with launch count and last used time).
  - **Recently Used** applications list.
  - **Summary** card with `TotalLaunches`.
- `StatisticsViewModel`:
  - Wraps `StatisticsService` and exposes observable collections `TopApplications`, `TopGroups`, `RecentlyUsed`, and integer `TotalLaunches`.
  - Provides `RefreshCommand` and `ClearStatisticsCommand` (via an internal `RelayCommand` implementation) bound to the window’s buttons.
  - `ClearStatisticsCommand` confirms with the user and then calls `StatisticsService.ClearStatistics()`.

### Styling and resources
- `Styles/ModernStyles.xaml` defines generic styles for `Button`, `TextBox`, `ListBox`, `ListBoxItem`, and a `HeaderStyle` for `TextBlock`.
  - These are primarily used by secondary windows such as `AddApplicationWindow` and `StatisticsWindow`.
- `MainWindow.xaml` currently defines its own inline styles and brushes for a more modern dashboard-like interface (light and dark color palettes, custom button styles, card style, etc.), independent of `ModernStyles.xaml`.

### Notes about data locations and side effects
- The application creates and modifies the following on the user’s machine:
  - `%APPDATA%/TaskbarGroupTool/groups.json` – saved groups.
  - `%APPDATA%/TaskbarGroupTool/statistics.json` – usage statistics.
  - `%APPDATA%/TaskbarGroupTool/Backups/Backup_*.tbg` – automatic backups.
  - `%APPDATA%/TaskbarGroupTool/settings.json` – theme preferences.
  - Registry keys under `HKCU\SOFTWARE\Classes` and `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (for startup registration).
  - A `Shortcuts` directory located next to the built `.exe`, containing `.lnk` files for groups.
- When modifying behavior around persistence, shortcuts, or registry access, check the relevant service classes (`TaskbarManager`, `AppRegistrationService`, `ConfigurationService`, `StatisticsService`, `ThemeService`) to keep data format and side effects consistent.
