using System.Diagnostics;
using System.Text;

namespace DockerChallenge.Services;

/// <summary>Thin wrapper around the local <c>docker</c> CLI.</summary>
public class DockerService
{
    public record CommandResult(int ExitCode, string StdOut, string StdErr)
    {
        public bool Success => ExitCode == 0;
        public string Output => (StdOut + "\n" + StdErr).Trim();
    }

    /// <summary>Runs "docker &lt;arguments&gt;" and captures its output.</summary>
    public async Task<CommandResult> RunAsync(string arguments, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            await process.WaitForExitAsync(timeout.Token);

            return new CommandResult(process.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
        }
        catch (Exception ex)
        {
            return new CommandResult(-1, string.Empty, $"Failed to run docker: {ex.Message}");
        }
    }

    /// <summary>Runs <c>docker inspect</c> with a Go template and returns the trimmed output.</summary>
    public async Task<string> InspectAsync(string target, string format, string type = "", CancellationToken ct = default)
    {
        var typeArg = string.IsNullOrEmpty(type) ? "" : $"{type} ";
        var result = await RunAsync($"{typeArg}inspect --format \"{format}\" {target}", ct);
        return result.Success ? result.StdOut.Trim() : string.Empty;
    }

    /// <summary>Verifies the docker daemon is reachable.</summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("info --format \"{{.ServerVersion}}\"", ct);
        return result.Success && !string.IsNullOrWhiteSpace(result.StdOut);
    }
}
