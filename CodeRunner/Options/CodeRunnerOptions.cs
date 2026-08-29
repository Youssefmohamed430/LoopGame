namespace CodeRunner.Options;

public class CodeRunnerOptions
{
    public const string SectionName = "CodeRunner";

    public string DockerImage { get; set; } = "shift-c-runner:latest";
    public int TimeoutSeconds { get; set; } = 5;
    public int MemoryLimitMb { get; set; } = 128;
    public double CpuLimit { get; set; } = 0.5;
    public int MaxProcesses { get; set; } = 64;
    public int MaxOutputBytes { get; set; } = 1024 * 1024; // 1 MB limit
}
