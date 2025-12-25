# Taskbar Grouping Tool

A Windows 11 tool for creating and managing groups in the taskbar.

## Features

- **Create Groups**: Create custom groups for your applications
- **Manage Applications**: Add or remove applications from groups
- **Taskbar Shortcuts**: Create shortcuts for groups directly in the taskbar
- **Persistent Storage**: Your group configuration is automatically saved
- **Modern UI**: Clean and intuitive user interface
- **Pop-up Menus**: Quick access to your applications through taskbar shortcuts

## Installation

1. Download the latest version
2. Extract the ZIP file
3. Run `TaskbarGroupTool.exe`

## Usage

### Creating Groups

1. Launch the application
2. Click "New Group" to create a new group
3. Enter a group name
4. Add applications using the "Add" button
5. Save your group

### Creating Taskbar Shortcuts

1. Select a group from the list
2. Click "Create Taskbar Shortcut"
3. A `.lnk` file will be created on your desktop
4. Right-click the `.lnk` file and select "Pin to taskbar"
5. Click the pinned shortcut to open the pop-up menu

### Using Pop-up Menus

When you click a pinned taskbar shortcut, a small pop-up window will appear showing all applications in that group. Click any application to launch it.

## Technical Details

- **Framework**: .NET 8.0 with WPF
- **Platform**: Windows 11
- **Architecture**: x64
- **Storage**: Groups are saved in `%APPDATA%/TaskbarGroupTool/groups.json`

## Development

### Building from Source

```bash
git clone https://github.com/yourusername/TaskbarGroupTool.git
cd TaskbarGroupTool
dotnet build
dotnet run
```

### Project Structure

- `MainWindow.xaml` - Main application window
- `GroupMenuWindow.xaml` - Pop-up menu for taskbar shortcuts
- `Services/` - Business logic services
- `Models/` - Data models
- `Windows/` - Custom windows

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Changelog

### v1.0.0

- Initial release
- Basic group management
- Taskbar shortcut creation
- Pop-up menu functionality
