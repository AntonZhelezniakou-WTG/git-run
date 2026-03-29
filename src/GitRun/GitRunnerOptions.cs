namespace GitRun;

public sealed class GitRunnerOptions
{
	public string? WorkingDirectory { get; set; }

	public string GitExecutable { get; set; } = "git";

	public bool ThrowOnNonZeroExitCode { get; set; } = true;
}
