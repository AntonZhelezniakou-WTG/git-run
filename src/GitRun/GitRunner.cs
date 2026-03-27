using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace GitRun;

/// <summary>
/// Runs git commands as a child process and streams their output asynchronously.
/// </summary>
public sealed class GitRunner : IGitRunner
{
	private readonly GitRunnerOptions _options;

	/// <summary>
	/// Initializes a new instance of <see cref="GitRunner"/> with default options.
	/// </summary>
	public GitRunner() : this(new GitRunnerOptions()) { }

	/// <summary>
	/// Initializes a new instance of <see cref="GitRunner"/> with the specified options.
	/// </summary>
	/// <param name="options">Options that control the runner behaviour.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
	public GitRunner(GitRunnerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;
	}

	/// <inheritdoc/>
	public IAsyncEnumerable<string> RunAsync(
		string arguments,
		CancellationToken cancellationToken = default) =>
		RunAsync(arguments, workingDirectory: null, cancellationToken);

	/// <inheritdoc/>
	public async IAsyncEnumerable<string> RunAsync(
		string arguments,
		string? workingDirectory,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(arguments);

		var effectiveWorkingDirectory = workingDirectory
			?? _options.WorkingDirectory
			?? Directory.GetCurrentDirectory();

		var startInfo = new ProcessStartInfo
		{
			FileName = _options.GitExecutable,
			Arguments = arguments,
			WorkingDirectory = effectiveWorkingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		// Use an unbounded channel to decouple the stdout reader thread from the consumer.
		var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
		{
			SingleWriter = true,
			SingleReader = true,
			AllowSynchronousContinuations = false,
		});

		using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

		process.Start();

		// Read stdout into the channel for the consumer.
		var stdoutTask = ReadPipeIntoChannelAsync(
			process.StandardOutput, channel.Writer, cancellationToken);

		// Capture stderr separately into a buffer.
		var stderrBuffer = new StringBuilder();
		var stderrTask = ReadPipeIntoBufferAsync(
			process.StandardError, stderrBuffer, cancellationToken);

		// Complete the channel writer once stdout is done.
		_ = FinishWriterAsync(stdoutTask, channel.Writer);

		// Yield lines as they arrive.
		await foreach (var line in channel.Reader.ReadAllAsync(cancellationToken))
		{
			yield return line;
		}

		// Ensure stderr is fully captured before checking exit code.
		await stderrTask.ConfigureAwait(false);
		await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

		if (_options.ThrowOnNonZeroExitCode && process.ExitCode != 0)
		{
			throw new GitRunException(arguments, process.ExitCode, stderrBuffer.ToString());
		}
	}

	/// <inheritdoc/>
	public Task<string?> ReadFirstLineAsync(
		string arguments,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default) =>
		ReadFirstLineAsync(arguments, workingDirectory: null, predicate, cancellationToken);

	/// <inheritdoc/>
	public async Task<string?> ReadFirstLineAsync(
		string arguments,
		string? workingDirectory,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		predicate ??= _ => true;

		await foreach (var line in RunAsync(arguments, workingDirectory, cancellationToken))
		{
			if (predicate(line))
			{
				return line;
			}
		}

		return null;
	}

	private static async Task ReadPipeIntoChannelAsync(
		TextReader reader,
		ChannelWriter<string> writer,
		CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
			if (line is null)
			{
				break;
			}

			await writer.WriteAsync(line, cancellationToken).ConfigureAwait(false);
		}
	}

	private static async Task ReadPipeIntoBufferAsync(
		TextReader reader,
		StringBuilder buffer,
		CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
			if (line is null)
			{
				break;
			}

			if (buffer.Length > 0)
			{
				buffer.AppendLine();
			}

			buffer.Append(line);
		}
	}

	private static async Task FinishWriterAsync(
		Task stdoutTask,
		ChannelWriter<string> writer)
	{
		try
		{
			await stdoutTask.ConfigureAwait(false);
			writer.Complete();
		}
		catch (Exception ex)
		{
			writer.Complete(ex);
		}
	}
}
