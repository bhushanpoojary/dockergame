using DockerChallenge.Models;

namespace DockerChallenge.Services;

/// <summary>Holds the state of the single active game session on this machine.</summary>
public class GameStateService
{
    private readonly ChallengeService _challenges;
    private readonly LeaderboardService _leaderboard;

    public GameStateService(ChallengeService challenges, LeaderboardService leaderboard)
    {
        _challenges = challenges;
        _leaderboard = leaderboard;
    }

    public string? TeamName { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsRunning => StartedAt.HasValue && CompletedAt is null;
    public bool IsComplete => CompletedAt.HasValue;

    /// <summary>Challenge id -> time (from start) at which it was solved.</summary>
    public Dictionary<int, TimeSpan> SolvedAt { get; } = new();

    /// <summary>Challenges whose hint was revealed (each incurs a one-time penalty).</summary>
    public HashSet<int> HintedChallenges { get; } = new();

    /// <summary>Time added to the clock for each hint revealed.</summary>
    public static readonly TimeSpan HintPenalty = TimeSpan.FromSeconds(30);

    public TimeSpan TotalPenalty => HintedChallenges.Count * HintPenalty;

    public int TotalChallenges => _challenges.Challenges.Count;
    public int SolvedCount => SolvedAt.Count;

    /// <summary>Raised whenever state changes so the UI can refresh.</summary>
    public event Action? OnChange;

    public bool IsSolved(int challengeId) => SolvedAt.ContainsKey(challengeId);

    public TimeSpan Elapsed =>
        StartedAt is null ? TimeSpan.Zero
        : (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value + TotalPenalty;

    /// <summary>Records that a hint was revealed, applying a one-time time penalty.</summary>
    public void RevealHint(int challengeId)
    {
        if (!IsRunning) return;
        if (HintedChallenges.Add(challengeId))
            NotifyChanged();
    }

    public void StartGame(string teamName)
    {
        TeamName = string.IsNullOrWhiteSpace(teamName) ? "Team" : teamName.Trim();
        StartedAt = DateTime.UtcNow;
        CompletedAt = null;
        SolvedAt.Clear();
        HintedChallenges.Clear();
        NotifyChanged();
    }

    /// <summary>Marks a challenge solved (idempotent) and finishes the game when all are done.</summary>
    public void MarkSolved(int challengeId)
    {
        if (!IsRunning || SolvedAt.ContainsKey(challengeId))
            return;

        SolvedAt[challengeId] = Elapsed;

        if (SolvedCount >= TotalChallenges)
            CompleteGame();

        NotifyChanged();
    }

    private void CompleteGame()
    {
        CompletedAt = DateTime.UtcNow;
        _leaderboard.Add(new LeaderboardEntry(
            TeamName ?? "Team",
            SolvedCount,
            TotalChallenges,
            Elapsed.TotalSeconds,
            DateTime.UtcNow));
    }

    /// <summary>Ends the current game early and records the partial result.</summary>
    public void GiveUp()
    {
        if (!IsRunning) return;
        CompletedAt = DateTime.UtcNow;
        _leaderboard.Add(new LeaderboardEntry(
            TeamName ?? "Team",
            SolvedCount,
            TotalChallenges,
            Elapsed.TotalSeconds,
            DateTime.UtcNow));
        NotifyChanged();
    }

    public void Reset()
    {
        TeamName = null;
        StartedAt = null;
        CompletedAt = null;
        SolvedAt.Clear();
        HintedChallenges.Clear();
        NotifyChanged();
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
