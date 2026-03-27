using System.Runtime.CompilerServices;

namespace GitRun;

/// <summary>
/// Provides methods to execute git commands and stream their output asynchronously.
/// </summary>
public interface IGitRunner
{
	/// <summary>
	/// Executes a git command and streams the combined stdout/stderr output line by line.
	/// </summary>
	/// <param name="arguments">
	/// The git arguments as a plaintext string, e.g. <c>"gc --prune=now --aggressive"</c>.
	/// </param>
	/// <param name="cancellationToken">Token to cancel the operation.</param>
	/// <returns>An async sequence of output lines produced by the git process.</returns>
	/// <exception cref="GitRunException">
	/// Thrown when the git process exits with a non-zero exit code.
	/// </exception>
	IAsyncEnumerable<string> RunAsync(
		string arguments,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a git command inside the specified working directory and streams output line by line.
	/// </summary>
	/// <param name="arguments">
	/// The git arguments as a plaintext string, e.g. <c>"status --short"</c>.
	/// </param>
	/// <param name="workingDirectory">
	/// The directory in which to run the git process.
	/// If <see langword="null"/>, the <see cref="GitRunnerOptions.WorkingDirectory"/> from
	/// options is used.
	/// </param>
	/// <param name="cancellationToken">Token to cancel the operation.</param>
	/// <returns>An async sequence of output lines produced by the git process.</returns>
	/// <exception cref="GitRunException">
	/// Thrown when the git process exits with a non-zero exit code.
	/// </exception>
	IAsyncEnumerable<string> RunAsync(
		string arguments,
		string? workingDirectory,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a git command and returns the first output line that satisfies the optional
	/// <paramref name="predicate"/>. The git process is terminated as soon as the matching
	/// line is found, without waiting for the command to finish.
	/// </summary>
	/// <param name="arguments">
	/// The git arguments as a plaintext string, e.g. <c>"rev-parse HEAD"</c>.
	/// </param>
	/// <param name="predicate">
	/// An optional filter applied to each output line. When <see langword="null"/>,
	/// the very first output line is returned.
	/// </param>
	/// <param name="cancellationToken">Token to cancel the operation.</param>
	/// <returns>
	/// The first matching output line, or <see langword="null"/> when the process
	/// produces no matching output.
	/// </returns>
	Task<string?> ReadFirstLineAsync(
		string arguments,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a git command inside the specified working directory and returns the first
	/// output line that satisfies the optional <paramref name="predicate"/>. The git process
	/// is terminated as soon as the matching line is found, without waiting for the command
	/// to finish.
	/// </summary>
	/// <param name="arguments">
	/// The git arguments as a plaintext string, e.g. <c>"rev-parse HEAD"</c>.
	/// </param>
	/// <param name="workingDirectory">
	/// The directory in which to run the git process.
	/// If <see langword="null"/>, the <see cref="GitRunnerOptions.WorkingDirectory"/> from
	/// options is used.
	/// </param>
	/// <param name="predicate">
	/// An optional filter applied to each output line. When <see langword="null"/>,
	/// the very first output line is returned.
	/// </param>
	/// <param name="cancellationToken">Token to cancel the operation.</param>
	/// <returns>
	/// The first matching output line, or <see langword="null"/> when the process
	/// produces no matching output.
	/// </returns>
	Task<string?> ReadFirstLineAsync(
		string arguments,
		string? workingDirectory,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default);
}
