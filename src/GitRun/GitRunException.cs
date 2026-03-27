namespace GitRun;

/// <summary>
/// Thrown when a git process exits with a non-zero exit code.
/// </summary>
public sealed class GitRunException : Exception
{
	/// <summary>
	/// Gets the git arguments that were passed to the process.
	/// </summary>
	public string Arguments { get; }

	/// <summary>
	/// Gets the exit code returned by the git process.
	/// </summary>
	public int ExitCode { get; }

	/// <summary>
	/// Gets the standard error output captured from the git process.
	/// May be empty when the process produced no stderr output.
	/// </summary>
	public string StandardError { get; }

	/// <summary>
	/// Initializes a new instance of <see cref="GitRunException"/>.
	/// </summary>
	/// <param name="arguments">The git arguments that were executed.</param>
	/// <param name="exitCode">The exit code returned by the git process.</param>
	/// <param name="standardError">The stderr output captured from the git process.</param>
	public GitRunException(string arguments, int exitCode, string standardError = "")
		: base($"git {arguments} exited with code {exitCode}.")
	{
		Arguments = arguments;
		ExitCode = exitCode;
		StandardError = standardError;
	}
}
