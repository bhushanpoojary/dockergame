namespace DockerChallenge.Models;

/// <summary>A completed run recorded on the leaderboard.</summary>
public record LeaderboardEntry(
    string TeamName,
    int SolvedCount,
    int TotalChallenges,
    double TotalSeconds,
    DateTime CompletedAt)
{
    /// <summary>Stable identifier used to delete a specific entry.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public string FormattedTime => TimeSpan.FromSeconds(TotalSeconds).ToString(@"hh\:mm\:ss");
}
