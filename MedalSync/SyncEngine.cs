namespace MedalSync;

/// <summary>
/// Core sync engine: watches the NVIDIA clips folder recursively and maintains
/// hard links in a flat sync folder for Medal.
/// 
/// Uses FileSystemWatcher which internally calls ReadDirectoryChangesW —
/// an OS-level notification mechanism that consumes zero CPU while idle.
/// </summary>
public sealed class SyncEngine : IDisposable
{
    private readonly Settings _settings;
    private FileSystemWatcher? _watcher;
    private FileSystemWatcher? _syncWatcher;
    private bool _isPaused;
    private int _syncedCount;
    private readonly object _lock = new();
    private readonly SynchronizationContext? _syncContext;
    private readonly Dictionary<string, string> _linkMap = new(StringComparer.OrdinalIgnoreCase);

    // ── Events ──────────────────────────────────────────────────────────

    public event Action<string>? StatusChanged;
    public event Action<int>? SyncCountChanged;
    public event Action<string>? LogMessage;

    // ── Properties ──────────────────────────────────────────────────────

    public bool IsPaused => _isPaused;
    public bool IsRunning => _watcher != null && _watcher.EnableRaisingEvents;
    public int SyncedCount => _syncedCount;

    // ── Constructor ─────────────────────────────────────────────────────

    public SyncEngine(Settings settings)
    {
        _settings = settings;
        _syncContext = SynchronizationContext.Current;
    }

    // ── Public API ──────────────────────────────────────────────────────

    public void Start()
    {
        // Ensure source folder exists
        if (!Directory.Exists(_settings.SourceFolder))
        {
            RaiseStatus(Loc.StatusSourceNotFound(_settings.SourceFolder));
            return;
        }

        // Ensure sync folder exists
        Directory.CreateDirectory(_settings.SyncFolder);

        // Initial sync — create hard links for all existing clips
        InitialSync();

        // Start watching for new files
        SetupWatcher();
        SetupSyncWatcher();

        RaiseStatus(Loc.StatusActive);
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        _syncWatcher?.Dispose();
        _syncWatcher = null;
        RaiseStatus(Loc.StatusStopped);
    }

    public void Pause()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _isPaused = true;
            RaiseStatus(Loc.StatusPaused);
        }

        if (_syncWatcher != null)
            _syncWatcher.EnableRaisingEvents = false;
    }

    public void Resume()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = true;
            _isPaused = false;
            RaiseStatus(Loc.StatusActive);
        }

        if (_syncWatcher != null)
            _syncWatcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Re-scan source folder and update sync folder.
    /// Creates missing links and removes orphaned ones.
    /// </summary>
    public void Resync()
    {
        InitialSync();
        RaiseStatus(Loc.StatusActive);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _syncWatcher?.Dispose();
    }

    // ── Initial Sync ────────────────────────────────────────────────────

    private void InitialSync()
    {
        RaiseStatus(Loc.StatusSyncing);

        int created = 0;
        int skipped = 0;
        int cleaned = 0;

        bool syncWatcherWasEnabled = _syncWatcher?.EnableRaisingEvents ?? false;
        if (_syncWatcher != null)
            _syncWatcher.EnableRaisingEvents = false;

        // Collect all valid source clips
        var sourceClips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var linkMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string ext in _settings.Extensions)
        {
            foreach (string file in Directory.EnumerateFiles(
                _settings.SourceFolder, $"*{ext}", SearchOption.AllDirectories))
            {
                // Skip files in the sync folder itself if it's a subdirectory
                string linkName = GetSyncFileName(file);
                string linkPath = Path.Combine(_settings.SyncFolder, linkName);
                sourceClips.Add(linkName);
                linkMap[linkName] = file;

                if (File.Exists(linkPath))
                {
                    skipped++;
                    continue;
                }

                if (NativeHelper.TryCreateHardLink(linkPath, file, out string? error))
                {
                    created++;
                    Log(Loc.LogLinkCreated(linkName));
                }
                else
                {
                    Log(Loc.LogLinkError(linkName, error));
                }
            }
        }

        // Clean up orphaned links (links whose originals no longer exist)
        if (Directory.Exists(_settings.SyncFolder))
        {
            foreach (string linkFile in Directory.EnumerateFiles(_settings.SyncFolder))
            {
                string fileName = Path.GetFileName(linkFile);
                if (!sourceClips.Contains(fileName))
                {
                    try
                    {
                        File.Delete(linkFile);
                        cleaned++;
                        Log(Loc.LogOrphanRemoved(fileName));
                    }
                    catch { /* best effort */ }
                }
            }
        }

        lock (_lock)
        {
            _linkMap.Clear();
            foreach (var pair in linkMap)
                _linkMap[pair.Key] = pair.Value;

            _syncedCount = _linkMap.Count;
        }

        RaiseSyncCount(_syncedCount);

        if (_syncWatcher != null)
            _syncWatcher.EnableRaisingEvents = syncWatcherWasEnabled;

        Log(Loc.LogSyncComplete(created, skipped, cleaned));
    }

    // ── FileSystemWatcher ───────────────────────────────────────────────

    private void SetupWatcher()
    {
        _watcher?.Dispose();

        _watcher = new FileSystemWatcher(_settings.SourceFolder)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
            InternalBufferSize = 64 * 1024 // 64 KB buffer for burst scenarios
        };

        // Watch for all configured extensions
        // FileSystemWatcher only supports a single filter natively,
        // so we watch *.* and filter in the handler
        _watcher.Filter = "*.*";

        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;
    }

    private void SetupSyncWatcher()
    {
        _syncWatcher?.Dispose();

        _syncWatcher = new FileSystemWatcher(_settings.SyncFolder)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true,
            Filter = "*.*"
        };

        _syncWatcher.Deleted += OnSyncFileDeleted;
        _syncWatcher.Error += OnSyncWatcherError;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsWatchedExtension(e.FullPath)) return;

        // Small delay to let the file finish writing
        Task.Run(async () =>
        {
            await WaitForFileReady(e.FullPath);
            ProcessNewFile(e.FullPath);
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsWatchedExtension(e.FullPath)) return;

        string linkName = GetSyncFileName(e.FullPath);
        string linkPath = Path.Combine(_settings.SyncFolder, linkName);

        RemoveLinkMapping(linkName);

        lock (_lock)
        {
            if (File.Exists(linkPath))
            {
                try
                {
                    File.Delete(linkPath);
                    Log(Loc.LogLinkRemoved(linkName));
                }
                catch (Exception ex)
                {
                    Log(Loc.LogRemoveError(ex.Message));
                }
            }
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Remove old link
        if (IsWatchedExtension(e.OldFullPath))
        {
            string oldLinkName = GetSyncFileName(e.OldFullPath);
            string oldLinkPath = Path.Combine(_settings.SyncFolder, oldLinkName);
            RemoveLinkMapping(oldLinkName);
            try { File.Delete(oldLinkPath); } catch { }
        }

        // Create new link
        if (IsWatchedExtension(e.FullPath))
        {
            Task.Run(async () =>
            {
                await WaitForFileReady(e.FullPath);
                ProcessNewFile(e.FullPath);
            });
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Log(Loc.LogWatcherError(e.GetException().Message));
        RaiseStatus(Loc.StatusErrorRestart);

        // Try to restart the watcher
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            try
            {
                SetupWatcher();
                RaiseStatus(Loc.StatusActive);
            }
            catch (Exception ex)
            {
                Log(Loc.LogRestartFailed(ex.Message));
                RaiseStatus(Loc.StatusErrorFailed);
            }
        });
    }

    private void OnSyncWatcherError(object sender, ErrorEventArgs e)
    {
        Log(Loc.LogWatcherError(e.GetException().Message));
    }

    private void OnSyncFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsWatchedExtension(e.FullPath)) return;

        string linkName = Path.GetFileName(e.FullPath);
        if (string.IsNullOrEmpty(linkName)) return;

        string? sourcePath = null;
        if (!TryRemoveLinkMapping(linkName, out sourcePath))
            return;

        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            sourcePath = FindSourcePathForLink(linkName);

        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            Log(Loc.LogSyncDeleteMissingSource(linkName));
            return;
        }

        try
        {
            File.Delete(sourcePath);
            Log(Loc.LogSyncDeleteSourceRemoved(linkName));
        }
        catch (Exception ex)
        {
            Log(Loc.LogSyncDeleteSourceError(linkName, ex.Message));
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void ProcessNewFile(string filePath)
    {
        lock (_lock)
        {
            string linkName = GetSyncFileName(filePath);
            string linkPath = Path.Combine(_settings.SyncFolder, linkName);

            if (File.Exists(linkPath))
            {
                AddLinkMapping(linkName, filePath, updateCount: true);
                Log(Loc.LogLinkExists(linkName));
                return;
            }

            if (NativeHelper.TryCreateHardLink(linkPath, filePath, out string? error))
            {
                AddLinkMapping(linkName, filePath, updateCount: true);
                Log(Loc.LogNewClip(linkName));
            }
            else
            {
                Log(Loc.LogLinkError(linkName, error));
            }
        }
    }

    /// <summary>
    /// Generates a flat filename from a nested clip path.
    /// Example: D:\Clips\Fortnite\2026-04-28.mp4 → Fortnite_2026-04-28.mp4
    /// Files directly in the root get no prefix.
    /// </summary>
    private string GetSyncFileName(string fullPath)
    {
        string relativePath = Path.GetRelativePath(_settings.SourceFolder, fullPath);
        string? directory = Path.GetDirectoryName(relativePath);
        string fileName = Path.GetFileName(relativePath);

        if (string.IsNullOrEmpty(directory) || directory == ".")
        {
            // File is directly in root — no prefix needed
            return fileName;
        }

        // Replace path separators with underscores for nested folders
        string prefix = directory.Replace(Path.DirectorySeparatorChar, '_')
                                 .Replace(Path.AltDirectorySeparatorChar, '_');
        return $"{prefix}_{fileName}";
    }

    private bool IsWatchedExtension(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        return _settings.Extensions.Any(e =>
            string.Equals(e, ext, StringComparison.OrdinalIgnoreCase));
    }

    private void AddLinkMapping(string linkName, string sourcePath, bool updateCount)
    {
        bool added = false;
        int newCount = 0;

        if (!_linkMap.ContainsKey(linkName))
        {
            _linkMap[linkName] = sourcePath;
            added = true;
        }
        else
        {
            _linkMap[linkName] = sourcePath;
        }

        if (updateCount && added)
        {
            _syncedCount = _linkMap.Count;
            newCount = _syncedCount;
        }

        if (updateCount && added)
            RaiseSyncCount(newCount);
    }

    private void RemoveLinkMapping(string linkName)
    {
        if (TryRemoveLinkMapping(linkName, out _))
        {
            // Mapping removal already handled count update.
        }
    }

    private bool TryRemoveLinkMapping(string linkName, out string? sourcePath)
    {
        bool removed = false;
        int newCount = 0;

        lock (_lock)
        {
            removed = _linkMap.TryGetValue(linkName, out sourcePath);
            if (removed)
            {
                _linkMap.Remove(linkName);
                _syncedCount = _linkMap.Count;
                newCount = _syncedCount;
            }
        }

        if (removed)
            RaiseSyncCount(newCount);

        return removed;
    }

    private string? FindSourcePathForLink(string linkName)
    {
        string? match = null;

        foreach (string ext in _settings.Extensions)
        {
            foreach (string file in Directory.EnumerateFiles(
                _settings.SourceFolder, $"*{ext}", SearchOption.AllDirectories))
            {
                string candidateName = GetSyncFileName(file);
                if (!string.Equals(candidateName, linkName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (match != null)
                    return null; // Ambiguous match

                match = file;
            }
        }

        return match;
    }

    /// <summary>
    /// Waits until a file is no longer locked (e.g., still being written by NVIDIA).
    /// </summary>
    private static async Task WaitForFileReady(string filePath, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return; // File is ready
            }
            catch (IOException)
            {
                await Task.Delay(1000); // Wait 1 second before retrying
            }
            catch
            {
                return; // Other error — give up
            }
        }
    }

    // ── Event Raising (thread-safe) ─────────────────────────────────────

    private void RaiseStatus(string status)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => StatusChanged?.Invoke(status), null);
        else
            StatusChanged?.Invoke(status);
    }

    private void RaiseSyncCount(int count)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => SyncCountChanged?.Invoke(count), null);
        else
            SyncCountChanged?.Invoke(count);
    }

    private void Log(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (_syncContext != null)
            _syncContext.Post(_ => LogMessage?.Invoke(line), null);
        else
            LogMessage?.Invoke(line);
    }
}
