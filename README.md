# Taskbar Grouping Tool

Ein Windows 11 Tool zum Erstellen und Verwalten von Gruppen in der Taskbar.

## Funktionen

- **Gruppen erstellen**: Erstellen Sie benutzerdefinierte Gruppen für Ihre Anwendungen
- **Anwendungen verwalten**: Fügen Sie Anwendungen zu Gruppen hinzu oder entfernen Sie sie
- **Taskbar-Shortcuts**: Erstellen Sie Shortcuts für Gruppen direkt in der Taskbar
- **Persistenter Speicher**: Ihre Gruppenkonfiguration wird automatisch gespeichert
- **Modernes UI**: Saubere und intuitive Benutzeroberfläche

## Installation

1. Laden Sie die neueste Version herunter
2. Führen Sie die Setup-Datei aus
3. Starten Sie die Anwendung

## Verwendung

### Gruppe erstellen

1. Klicken Sie auf "Neue Gruppe"
2. Geben Sie einen Namen für die Gruppe ein
3. Fügen Sie Anwendungen hinzu, indem Sie auf "Hinzufügen" klicken
4. Speichern Sie die Gruppe mit "Speichern"

### Taskbar-Shortcut erstellen

1. Wählen Sie eine Gruppe aus
2. Klicken Sie auf "Taskbar-Shortcut erstellen"
3. Der Shortcut wird auf dem Desktop erstellt und kann zur Taskbar hinzugefügt werden

### Gruppe löschen

1. Wählen Sie die zu löschende Gruppe aus
2. Klicken Sie auf "Löschen"
3. Bestätigen Sie die Löschung

## Technische Details

- **Framework**: .NET 8.0 mit WPF
- **Sprache**: C#
- **Speicherort**: `%APPDATA%\TaskbarGroupTool\groups.json`
- **Abhängigkeiten**: 
  - Newtonsoft.Json
  - Microsoft.Win32.Registry
  - System.Drawing.Common

## Systemanforderungen

- Windows 11
- .NET 8.0 Runtime
- Administratorrechte für Taskbar-Integration

## Lizenz

Dieses Projekt ist Open Source und unter der MIT-Lizenz veröffentlicht.

## Unterstützung

Bei Problemen oder Fragen kontaktieren Sie bitte den Support.
