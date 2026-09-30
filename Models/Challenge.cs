namespace DockerChallenge.Models;

/// <summary>Result of validating a single challenge against live Docker state.</summary>
public record ValidationResult(bool Success, string Message);

public class Challenge
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }

    /// <summary>Concrete requirements the player must satisfy.</summary>
    public required string[] Requirements { get; init; }

    /// <summary>A hint revealing the shape of the command(s) needed.</summary>
    public required string Hint { get; init; }

    /// <summary>Runs live docker commands to check whether the task is complete.</summary>
    public required Func<Services.DockerService, Task<ValidationResult>> Validate { get; init; }
}
