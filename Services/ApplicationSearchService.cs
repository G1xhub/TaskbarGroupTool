using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarGroupTool.Services
{
    public class ApplicationSearchService
    {
        private readonly SearchCacheService _cacheService;
        private readonly SemaphoreSlim _searchSemaphore;
        
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

        public ApplicationSearchService()
        {
            _cacheService = new SearchCacheService();
            _searchSemaphore = new SemaphoreSlim(3, 3); // Limit concurrent searches
        }

        public async Task<List<SearchResult>> SearchApplicationsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _cacheService.GetCachedSearchAsync(searchTerm, 
                () => PerformSearchAsync(searchTerm, cancellationToken));
        }

        private async Task<List<SearchResult>> PerformSearchAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<SearchResult>();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SearchResult>();

            try
            {
                // Limit concurrent searches
                await _searchSemaphore.WaitAsync(cancellationToken);
                
                var searchTasks = new List<Task>
                {
                    // Startmenü durchsuchen
                    Task.Run(async () => 
                    {
                        var startMenuResults = await SearchStartMenuAsync(searchTerm, cancellationToken);
                        foreach (var result in startMenuResults)
                            results.Add(result);
                    }, cancellationToken),
                    
                    // Desktop durchsuchen
                    Task.Run(async () => 
                    {
                        var desktopResults = await SearchDesktopAsync(searchTerm, cancellationToken);
                        foreach (var result in desktopResults)
                            results.Add(result);
                    }, cancellationToken),
                    
                    // Bekannte Ordner durchsuchen
                    Task.Run(async () => 
                    {
                        var knownFolderResults = await SearchKnownFoldersAsync(searchTerm, cancellationToken);
                        foreach (var result in knownFolderResults)
                            results.Add(result);
                    }, cancellationToken),
                    
                    // Installierte Programme durchsuchen
                    Task.Run(async () => 
                    {
                        var programResults = await SearchInstalledProgramsAsync(searchTerm, cancellationToken);
                        foreach (var result in programResults)
                            results.Add(result);
                    }, cancellationToken)
                };

                // Wait for all searches to complete with timeout
                await Task.WhenAll(searchTasks).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error but don't fail completely
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }

            return results.Distinct().Take(50).ToList();
        }

        private async Task<List<SearchResult>> SearchStartMenuAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<SearchResult>();
            var startMenuPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Start Menu\\Programs")
            };

            var searchTasks = startMenuPaths.Select(async path =>
            {
                if (Directory.Exists(path))
                {
                    var pathResults = await SearchDirectoryAsync(path, searchTerm, SearchResultType.Shortcut, cancellationToken);
                    foreach (var result in pathResults)
                        results.Add(result);
                }
            });

            await Task.WhenAll(searchTasks);
            return results.ToList();
        }

        private async Task<List<SearchResult>> SearchDesktopAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktopPath))
            {
                return await SearchDirectoryAsync(desktopPath, searchTerm, SearchResultType.Shortcut, cancellationToken);
            }
            return new List<SearchResult>();
        }

        private async Task<List<SearchResult>> SearchKnownFoldersAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<SearchResult>();
            
            var searchTasks = KnownFolders.Select(async folderGuid =>
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
                            var folderResults = await SearchDirectoryAsync(folderPath, searchTerm, SearchResultType.Folder, cancellationToken);
                            foreach (var result in folderResults)
                                results.Add(result);
                        }
                    }
                }
                catch
                {
                    // Ordner nicht verfügbar, überspringen
                }
            });

            await Task.WhenAll(searchTasks);
            return results.ToList();
        }

        private async Task<List<SearchResult>> SearchInstalledProgramsAsync(string searchTerm, CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<SearchResult>();
            var programFiles = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            var searchTasks = programFiles.Select(async programPath =>
            {
                if (Directory.Exists(programPath))
                {
                    var pathResults = await SearchDirectoryAsync(programPath, searchTerm, SearchResultType.Application, cancellationToken);
                    foreach (var result in pathResults)
                        results.Add(result);
                }
            });

            await Task.WhenAll(searchTasks);
            return results.ToList();
        }

        private async Task<List<SearchResult>> SearchDirectoryAsync(string directory, string searchTerm, SearchResultType defaultType, CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<SearchResult>();

            try
            {
                // Get directories and files asynchronously
                var (directories, files) = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return (
                        Directory.GetDirectories(directory),
                        Directory.GetFiles(directory)
                    );
                }, cancellationToken);

                // Process directories in parallel
                var dirTasks = directories.Select(async dir =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var dirName = Path.GetFileName(dir);
                    if (dirName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        results.Add(new SearchResult
                        {
                            Name = dirName,
                            Path = dir,
                            Type = SearchResultType.Folder
                        });
                    }
                });

                // Process files in parallel
                var fileTasks = files.Select(async file =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var extension = Path.GetExtension(file).ToLower();

                    if (fileName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
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
                });

                // Wait for all file and directory processing to complete
                await Task.WhenAll(dirTasks.Concat(fileTasks));

                // Limited recursive search only if we haven't found enough results
                if (results.Count < 20)
                {
                    var subdirSearchTasks = directories.Take(5).Select(async subdir =>
                    {
                        try
                        {
                            var subResults = await SearchDirectoryAsync(subdir, searchTerm, defaultType, cancellationToken);
                            foreach (var result in subResults)
                                results.Add(result);
                        }
                        catch (OperationCanceledException)
                        {
                            throw; // Re-throw cancellation
                        }
                        catch
                        {
                            // Access denied, skip silently
                        }
                    });

                    await Task.WhenAll(subdirSearchTasks);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch
            {
                // Access denied or other error, skip silently
            }

            return results.ToList();
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
