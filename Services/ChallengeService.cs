using DockerChallenge.Models;

namespace DockerChallenge.Services;

/// <summary>Defines the ordered set of hands-on Docker challenges and how each is validated.</summary>
public class ChallengeService
{
    public IReadOnlyList<Challenge> Challenges { get; }

    public ChallengeService()
    {
        Challenges = BuildChallenges();
    }

    private static List<Challenge> BuildChallenges() =>
    [
        new Challenge
        {
            Id = 1,
            Title = "Warm-up: Pull an Image",
            Description = "Every Docker journey starts with an image. Pull the official Alpine Linux image to your local machine.",
            Requirements =
            [
                "The image alpine:latest must be present locally."
            ],
            Hint = "docker pull alpine:latest",
            Validate = async docker =>
            {
                var res = await docker.RunAsync("images -q alpine:latest");
                return string.IsNullOrWhiteSpace(res.StdOut)
                    ? new ValidationResult(false, "No local alpine:latest image found yet. Try pulling it.")
                    : new ValidationResult(true, "Alpine image is present. Nice start!");
            }
        },

        new Challenge
        {
            Id = 2,
            Title = "Run a Web Server",
            Description = "Spin up an Nginx web server in the background and expose it so a browser on the host can reach it.",
            Requirements =
            [
                "A running container named 'web'.",
                "It must use an nginx image.",
                "Host port 8080 must map to container port 80."
            ],
            Hint = "docker run -d --name web -p 8080:80 nginx",
            Validate = async docker =>
            {
                var running = await docker.InspectAsync("web", "{{.State.Running}}");
                if (running != "true")
                    return new ValidationResult(false, "Container 'web' is not running.");

                var image = await docker.InspectAsync("web", "{{.Config.Image}}");
                if (!image.Contains("nginx", StringComparison.OrdinalIgnoreCase))
                    return new ValidationResult(false, $"Container 'web' is not using nginx (found '{image}').");

                var portRes = await docker.RunAsync("port web 80");
                return portRes.StdOut.Contains(":8080")
                    ? new ValidationResult(true, "Nginx is live on port 8080!")
                    : new ValidationResult(false, "Container port 80 is not mapped to host port 8080.");
            }
        },

        new Challenge
        {
            Id = 3,
            Title = "Environment Variables",
            Description = "Applications are configured through environment variables. Launch a container that carries a specific variable.",
            Requirements =
            [
                "A container named 'envbox'.",
                "It must have the environment variable APP_ENV=production.",
                "Tip: use 'alpine sleep 1d' as the command so it stays alive."
            ],
            Hint = "docker run -d --name envbox -e APP_ENV=production alpine sleep 1d",
            Validate = async docker =>
            {
                var exists = await docker.InspectAsync("envbox", "{{.Id}}");
                if (string.IsNullOrEmpty(exists))
                    return new ValidationResult(false, "Container 'envbox' does not exist.");

                var env = await docker.InspectAsync("envbox", "{{range .Config.Env}}{{println .}}{{end}}");
                return env.Contains("APP_ENV=production")
                    ? new ValidationResult(true, "APP_ENV=production is set correctly.")
                    : new ValidationResult(false, "Could not find APP_ENV=production in the container environment.");
            }
        },

        new Challenge
        {
            Id = 4,
            Title = "Persist Data with a Volume",
            Description = "Containers are ephemeral, but data shouldn't be. Create a named volume and mount it into a container.",
            Requirements =
            [
                "A named volume called 'appdata'.",
                "A container named 'datastore' that mounts 'appdata' at /data.",
                "Tip: use 'alpine sleep 1d' so the container keeps running."
            ],
            Hint = "docker volume create appdata  →  docker run -d --name datastore -v appdata:/data alpine sleep 1d",
            Validate = async docker =>
            {
                var vol = await docker.InspectAsync("appdata", "{{.Name}}", type: "volume");
                if (vol != "appdata")
                    return new ValidationResult(false, "Named volume 'appdata' was not found.");

                var exists = await docker.InspectAsync("datastore", "{{.Id}}");
                if (string.IsNullOrEmpty(exists))
                    return new ValidationResult(false, "Container 'datastore' does not exist.");

                var mounts = await docker.InspectAsync("datastore",
                    "{{range .Mounts}}{{.Name}}:{{.Destination}}{{println}}{{end}}");
                return mounts.Contains("appdata:/data")
                    ? new ValidationResult(true, "Volume 'appdata' is mounted at /data.")
                    : new ValidationResult(false, "'appdata' is not mounted at /data in 'datastore'.");
            }
        },

        new Challenge
        {
            Id = 5,
            Title = "Custom Network",
            Description = "Containers talk to each other over networks. Create your own isolated bridge network.",
            Requirements =
            [
                "A user-defined network named 'appnet'.",
                "It must use the 'bridge' driver."
            ],
            Hint = "docker network create appnet",
            Validate = async docker =>
            {
                var driver = await docker.InspectAsync("appnet", "{{.Driver}}", type: "network");
                if (string.IsNullOrEmpty(driver))
                    return new ValidationResult(false, "Network 'appnet' was not found.");
                return driver == "bridge"
                    ? new ValidationResult(true, "Bridge network 'appnet' is ready.")
                    : new ValidationResult(false, $"Network 'appnet' uses '{driver}' driver, expected 'bridge'.");
            }
        },

        new Challenge
        {
            Id = 6,
            Title = "Build Your Own Image",
            Description = "Time to build. Create a Dockerfile and build an image with a specific tag. It doesn't need to do much \u2014 keep it simple.",
            Requirements =
            [
                "An image tagged challenge-app:1.0 must exist locally.",
                "Build it from a Dockerfile you write (any base image is fine)."
            ],
            Hint = "Create a file named 'Dockerfile' containing:\n\nFROM alpine\nCMD [\"echo\", \"hi\"]\n\nthen run:\n\ndocker build -t challenge-app:1.0 .",
            Validate = async docker =>
            {
                var res = await docker.RunAsync("images -q challenge-app:1.0");
                return string.IsNullOrWhiteSpace(res.StdOut)
                    ? new ValidationResult(false, "No image tagged challenge-app:1.0 found. Build one from a Dockerfile.")
                    : new ValidationResult(true, "Image challenge-app:1.0 built successfully!");
            }
        },

        new Challenge
        {
            Id = 7,
            Title = "Self-Healing Container",
            Description = "Production containers should recover from crashes. Run a container with an automatic restart policy.",
            Requirements =
            [
                "A container named 'resilient'.",
                "It must have restart policy set to 'always'.",
                "Tip: use 'alpine sleep 1d' so it stays alive."
            ],
            Hint = "docker run -d --name resilient --restart always alpine sleep 1d",
            Validate = async docker =>
            {
                var exists = await docker.InspectAsync("resilient", "{{.Id}}");
                if (string.IsNullOrEmpty(exists))
                    return new ValidationResult(false, "Container 'resilient' does not exist.");

                var policy = await docker.InspectAsync("resilient", "{{.HostConfig.RestartPolicy.Name}}");
                return policy == "always"
                    ? new ValidationResult(true, "Restart policy 'always' is set. You made it self-healing!")
                    : new ValidationResult(false, $"Restart policy is '{policy}', expected 'always'.");
            }
        },
    ];
}
