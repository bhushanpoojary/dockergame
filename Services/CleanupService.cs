namespace DockerChallenge.Services;

/// <summary>Removes the Docker resources created during a game so the next team starts clean.</summary>
public class CleanupService
{
    private readonly DockerService _docker;

    // Keep these in sync with the resources created in ChallengeService.
    private static readonly string[] Containers = ["web", "envbox", "datastore", "resilient"];
    private static readonly string[] Volumes = ["appdata"];
    private static readonly string[] Networks = ["appnet"];
    private static readonly string[] Images = ["challenge-app:1.0"];

    public CleanupService(DockerService docker) => _docker = docker;

    public record CleanupReport(List<string> Removed, List<string> Skipped)
    {
        public bool Any => Removed.Count > 0;
    }

    /// <summary>Force-removes all known challenge artifacts. Missing resources are ignored.</summary>
    public async Task<CleanupReport> CleanupAsync(CancellationToken ct = default)
    {
        var removed = new List<string>();
        var skipped = new List<string>();

        foreach (var c in Containers)
            await Remove($"container {c}", () => _docker.RunAsync($"rm -f {c}", ct), removed, skipped);

        // Networks/volumes can only be removed once dependent containers are gone.
        foreach (var n in Networks)
            await Remove($"network {n}", () => _docker.RunAsync($"network rm {n}", ct), removed, skipped);

        foreach (var v in Volumes)
            await Remove($"volume {v}", () => _docker.RunAsync($"volume rm -f {v}", ct), removed, skipped);

        foreach (var i in Images)
            await Remove($"image {i}", () => _docker.RunAsync($"rmi -f {i}", ct), removed, skipped);

        return new CleanupReport(removed, skipped);
    }

    private static async Task Remove(
        string label,
        Func<Task<DockerService.CommandResult>> action,
        List<string> removed,
        List<string> skipped)
    {
        var result = await action();
        if (result.Success && !string.IsNullOrWhiteSpace(result.StdOut))
            removed.Add(label);
        else
            skipped.Add(label);
    }
}
