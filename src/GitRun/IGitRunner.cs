namespace GitRun;

public interface IGitRunner
{
	IAsyncEnumerable<string> RunAsync(
		string arguments,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<string> RunAsync(
		string arguments,
		string? workingDirectory,
		CancellationToken cancellationToken = default);

	Task<string?> ReadFirstLineAsync(
		string arguments,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default);

	Task<string?> ReadFirstLineAsync(
		string arguments,
		string? workingDirectory,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default);

	string Run(string arguments);

	string Run(string arguments, string? workingDirectory);

	string? ReadFirstLine(
		string arguments,
		Func<string, bool>? predicate = null);

	string? ReadFirstLine(
		string arguments,
		string? workingDirectory,
		Func<string, bool>? predicate = null);
}
