using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TaskbarGroupTool.Services
{
    public class ApplicationSearchService
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        private static readonly Guid[] KnownFolders = new[]
        {
            new Guid("374DE290-123F-4565-9164-39C4925E467B"), // Downloads
            new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"), // Desktop
            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), // Documents
            new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB"), // Pictures
            new Guid("7B0BF17B-4F68-4CA2-9F41-8C26279E1B6B"), // Music
            new Guid("18987B1D-F99B-4283-9A44-9C9B8C022511"), // Videos
        };

        public List<SearchResult> SearchApplications(string searchTerm)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return results;

            try
            {
                // Startmenü durchsuchen
                results.AddRange(SearchStartMenu(searchTerm));
                
                // Desktop durchsuchen
                results.AddRange(SearchDesktop(searchTerm));
                
                // Bekannte Ordner durchsuchen
                results.AddRange(SearchKnownFolders(searchTerm));
                
                // Installierte Programme durchsuchen
                results.AddRange(SearchInstalledPrograms(searchTerm));
            }
            catch (Exception ex)
            {
                // Fehler bei der Suche behandeln
                Console.WriteLine($"Fehler bei der Suche: {ex.Message}");
            }

            return results.Distinct().Take(50).ToList();
        }

        private List<SearchResult> SearchStartMenu(string searchTerm)
        {
            var results = new List<SearchResult>();
            var startMenuPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Start Menu\\Programs")
            };

            foreach (var path in startMenuPaths)
            {
                if (Directory.Exists(path))
                {
                    results.AddRange(SearchDirectory(path, searchTerm, SearchResultType.Shortcut));
                }
            }

            return results;
        }

        private List<SearchResult> SearchDesktop(string searchTerm)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktopPath))
            {
                return SearchDirectory(desktopPath, searchTerm, SearchResultType.Shortcut);
            }
            return new List<SearchResult>();
        }

        private List<SearchResult> SearchKnownFolders(string searchTerm)
        {
            var results = new List<SearchResult>();
            
            foreach (var folderGuid in KnownFolders)
            {
                try
                {
                    var ptr = SHGetKnownFolderPath(folderGuid, 0, IntPtr.Zero, out var path);
                    if (path != IntPtr.Zero)
                    {
                        var folderPath = Marshal.PtrToStringUni(path);
                        Marshal.FreeCoTaskMem(path);
                        
                        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                        {
                            results.AddRange(SearchDirectory(folderPath, searchTerm, SearchResultType.Folder));
                        }
                    }
                }
                catch
                {
                    // Ordner nicht verfügbar, überspringen
                }
            }

            return results;
        }

        private List<SearchResult> SearchInstalledPrograms(string searchTerm)
        {
            var results = new List<SearchResult>();
            var programFiles = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var programPath in programFiles)
            {
                if (Directory.Exists(programPath))
                {
                    results.AddRange(SearchDirectory(programPath, searchTerm, SearchResultType.Application));
                }
            }

            return results;
        }

        private List<SearchResult> SearchDirectory(string directory, string searchTerm, SearchResultType defaultType)
        {
            var results = new List<SearchResult>();

            try
            {
                // Verzeichnisse durchsuchen
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new SearchResult
                        {
                            Name = dirName,
                            Path = dir,
                            Type = SearchResultType.Folder
                        });
                    }
                }

                // Dateien durchsuchen
                foreach (var file in Directory.GetFiles(directory))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var extension = Path.GetExtension(file).ToLower();

                    if (fileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        var type = defaultType;
                        if (extension == ".exe")
                            type = SearchResultType.Application;
                        else if (extension == ".lnk")
                            type = SearchResultType.Shortcut;

                        results.Add(new SearchResult
                        {
                            Name = fileName,
                            Path = file,
                            Type = type
                        });
                    }
                }

                // Rekursive Suche in Unterverzeichnissen (begrenzte Tiefe)
                if (results.Count < 20)
                {
                    foreach (var dir in Directory.GetDirectories(directory).Take(5))
                    {
                        try
                        {
                            results.AddRange(SearchDirectory(dir, searchTerm, defaultType));
                        }
                        catch
                        {
                            // Zugriff verweigert, überspringen
                        }
                    }
                }
            }
            catch
            {
                // Zugriff verweigert oder anderer Fehler
            }

            return results;
        }

        public string GetApplicationIcon(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return path; // Für den Moment den Pfad zurückgeben
                }
            }
            catch
            {
                // Fehler bei der Icon-Extraktion
            }
            return null;
        }
    }

    public class SearchResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public SearchResultType Type { get; set; }
    }

    public enum SearchResultType
    {
        Application,
        Shortcut,
        Folder
    }
}
