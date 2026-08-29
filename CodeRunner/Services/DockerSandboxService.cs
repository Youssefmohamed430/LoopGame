using System.Diagnostics;
using System.Text;
using CodeRunner.Options;
using Microsoft.Extensions.Options;

namespace CodeRunner.Services;

public class DockerSandboxService : ISandboxService
{
    private readonly CodeRunnerOptions _options;
    private readonly ILogger<DockerSandboxService> _logger;

    public DockerSandboxService(IOptions<CodeRunnerOptions> options, ILogger<DockerSandboxService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreateSandboxAsync(CancellationToken cancellationToken = default)
    {
        var sandboxId = $"shift-sandbox-{Guid.NewGuid():N}";
        _logger.LogInformation("Creating Docker sandbox container {SandboxId}", sandboxId);

        // Arguments to create and run detached container with strict security bounds
        var args = new string[]
        {
            "run", "-d",
            "--name", sandboxId,
            "--network", "none",
            "--memory", $"{_options.MemoryLimitMb}m",
            "--memory-swap", $"{_options.MemoryLimitMb}m",
            "--cpus", _options.CpuLimit.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            "--pids-limit", _options.MaxProcesses.ToString(),
            "--user", "1000:1000",
            "--security-opt", "no-new-privileges:true",
            _options.DockerImage,
            "tail", "-f", "/dev/null"
        };

        var result = await RunDockerCommandAsync(args, input: null, timeout: TimeSpan.FromSeconds(10), cancellationToken);

        if (result.ExitCode != 0)
        {
            _logger.LogError("Failed to create container {SandboxId}: {Stderr}", sandboxId, result.Stderr);
            throw new InvalidOperationException($"Failed to create Docker sandbox: {result.Stderr}");
        }

        _logger.LogInformation("Sandbox container {SandboxId} created successfully", sandboxId);
        return sandboxId;
    }

    public async Task WriteFileAsync(string sandboxId, string relativePath, string content, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Writing file {Path} inside sandbox {SandboxId}", relativePath, sandboxId);

        // Encode content in Base64 to safely pass to container shell without escaping issues
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        var targetPath = $"/workspace/{relativePath.TrimStart('/')}";

        var args = new string[]
        {
            "exec", sandboxId,
            "sh", "-c", $"echo '{base64Content}' | base64 -d > '{targetPath}'"
        };

        var result = await RunDockerCommandAsync(args, input: null, timeout: TimeSpan.FromSeconds(5), cancellationToken);

        if (result.ExitCode != 0)
        {
            _logger.LogError("Failed to write file to container {SandboxId}: {Stderr}", sandboxId, result.Stderr);
            throw new InvalidOperationException($"Failed to write source file to sandbox: {result.Stderr}");
        }
    }

    public async Task<SandboxProcessResult> CompileAsync(string sandboxId, string sourceFile, string outputFile, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compiling {SourceFile} -> {OutputFile} in sandbox {SandboxId}", sourceFile, outputFile, sandboxId);

        var src = $"/workspace/{sourceFile.TrimStart('/')}";
        var outBin = $"/workspace/{outputFile.TrimStart('/')}";

        var args = new string[]
        {
            "exec", sandboxId,
            "gcc", "-O2", "-Wall", "-std=c11", src, "-o", outBin
        };

        return await RunDockerCommandAsync(args, input: null, timeout: TimeSpan.FromSeconds(10), cancellationToken);
    }

    public async Task<SandboxProcessResult> RunAsync(string sandboxId, string command, string input, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running executable in sandbox {SandboxId} with timeout {Timeout}s", sandboxId, timeout.TotalSeconds);

        var binPath = $"/workspace/{command.TrimStart('/')}";

        var args = new string[]
        {
            "exec", "-i", sandboxId, binPath
        };

        return await RunDockerCommandAsync(args, input, timeout, cancellationToken);
    }

    public async Task DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sandboxId)) return;

        _logger.LogInformation("Destroying Docker sandbox container {SandboxId}", sandboxId);

        var args = new string[]
        {
            "rm", "-f", sandboxId
        };

        try
        {
            await RunDockerCommandAsync(args, input: null, timeout: TimeSpan.FromSeconds(5), cancellationToken);
            _logger.LogInformation("Sandbox container {SandboxId} removed", sandboxId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while removing sandbox container {SandboxId}", sandboxId);
        }
    }

    private async Task<SandboxProcessResult> RunDockerCommandAsync(
        string[] dockerArgs,
        string? input,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in dockerArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var stopwatch = Stopwatch.StartNew();
        bool isTimeout = false;

        try
        {
            process.Start();

            // Write input to STDIN if provided
            if (input != null)
            {
                using var writer = process.StandardInput;
                await writer.WriteAsync(input);
                await writer.FlushAsync();
            }
            else
            {
                process.StandardInput.Close();
            }

            var readStdoutTask = ReadStreamAsync(process.StandardOutput, stdoutBuilder, _options.MaxOutputBytes);
            var readStderrTask = ReadStreamAsync(process.StandardError, stderrBuilder, _options.MaxOutputBytes);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                isTimeout = true;
                _logger.LogWarning("Docker execution timed out after {Timeout} ms. Terminating process.", timeout.TotalMilliseconds);

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch { }
            }

            await Task.WhenAll(readStdoutTask, readStderrTask);
        }
        catch (Exception ex) when (!isTimeout)
        {
            _logger.LogError(ex, "Process execution error");
            stderrBuilder.AppendLine(ex.Message);
        }
        finally
        {
            stopwatch.Stop();
        }

        return new SandboxProcessResult(
            ExitCode: isTimeout ? -1 : (process.HasExited ? process.ExitCode : -1),
            Stdout: stdoutBuilder.ToString(),
            Stderr: stderrBuilder.ToString(),
            IsTimeout: isTimeout,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds
        );
    }

    private static async Task ReadStreamAsync(StreamReader reader, StringBuilder builder, int maxBytes)
    {
        char[] buffer = new char[4096];
        int totalRead = 0;

        while (totalRead < maxBytes)
        {
            int read = await reader.ReadAsync(buffer, 0, Math.Min(buffer.Length, maxBytes - totalRead));
            if (read == 0) break;
            builder.Append(buffer, 0, read);
            totalRead += read;
        }
    }
}
