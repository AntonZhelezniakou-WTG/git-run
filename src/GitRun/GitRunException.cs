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
    /// Initializes a new instance of <see cref="GitRunException"/>.
    /// </summary>
    /// <param name="arguments">The git arguments that were executed.</param>
    /// <param name="exitCode">The exit code returned by the git process.</param>
    public GitRunException(string arguments, int exitCode)
        : base($"git {arguments} exited with code {exitCode}.")
    {
        Arguments = arguments;
        ExitCode = exitCode;
    }
}
