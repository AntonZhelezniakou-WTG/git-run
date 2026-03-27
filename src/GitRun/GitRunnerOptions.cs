namespace GitRun;

/// <summary>
/// Configuration options for <see cref="GitRunner"/>.
/// </summary>
public sealed class GitRunnerOptions
{
	/// <summary>
	/// Gets or sets the default working directory for git commands.
	/// When <see langword="null"/>, the current process working directory is used.
	/// </summary>
	public string? WorkingDirectory { get; set; }

	/// <summary>
	/// Gets or sets the path to the git executable.
	/// Defaults to <c>"git"</c>, which resolves via the system PATH.
	/// </summary>
	public string GitExecutable { get; set; } = "git";

	/// <summary>
	/// Gets or sets a value indicating whether stderr output is included in the returned sequence.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	public bool IncludeStandardError { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether the runner throws a <see cref="GitRunException"/>
	/// when the git process exits with a non-zero exit code.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	public bool ThrowOnNonZeroExitCode { get; set; } = true;
}
