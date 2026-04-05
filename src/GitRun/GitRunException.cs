namespace GitRun;

public sealed class GitRunException(
	string arguments,
	int exitCode,
	string standardError = "")
		: Exception($"git {arguments} exited with code {exitCode}.")
{
	public string Arguments { get; } = arguments;

	public int ExitCode { get; } = exitCode;

	public string StandardError { get; } = standardError;
}
