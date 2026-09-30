using System.Text.Json;
using DockerChallenge.Models;

namespace DockerChallenge.Services;

/// <summary>Persists completed runs to a JSON file next to the app.</summary>
public class LeaderboardService
{
    private readonly string _path;
    private readonly object _lock = new();
    private List<LeaderboardEntry> _entries;

    public LeaderboardService(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "leaderboard.json");
        _entries = Load();
    }

    public IReadOnlyList<LeaderboardEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    /// <summary>Runs ranked best-first: most solved, then fastest time.</summary>
    public IReadOnlyList<LeaderboardEntry> Ranked =>
        Entries
            .OrderByDescending(e => e.SolvedCount)
            .ThenBy(e => e.TotalSeconds)
            .ToList();

    public void Add(LeaderboardEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            Save();
        }
    }

    /// <summary>Removes a single entry by id. Returns true if something was removed.</summary>
    public bool Remove(Guid id)
    {
        lock (_lock)
        {
            var removed = _entries.RemoveAll(e => e.Id == id) > 0;
            if (removed) Save();
            return removed;
        }
    }

    /// <summary>Clears the entire leaderboard.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            Save();
        }
    }

    private List<LeaderboardEntry> Load()
    {
        try
        {
            if (!File.Exists(_path)) return [];
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<LeaderboardEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch
        {
            // Non-fatal: leaderboard persistence is best-effort.
        }
    }
}
