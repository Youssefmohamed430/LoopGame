namespace CodeRunner.Services;

public record SandboxProcessResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    bool IsTimeout,
    long ElapsedMilliseconds
);

public interface ISandboxService
{
    Task<string> CreateSandboxAsync(CancellationToken cancellationToken = default);
    Task WriteFileAsync(string sandboxId, string relativePath, string content, CancellationToken cancellationToken = default);
    Task<SandboxProcessResult> CompileAsync(string sandboxId, string sourceFile, string outputFile, CancellationToken cancellationToken = default);
    Task<SandboxProcessResult> RunAsync(string sandboxId, string command, string input, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
}
