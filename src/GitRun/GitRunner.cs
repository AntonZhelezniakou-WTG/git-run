using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace GitRun;

public sealed class GitRunner : IGitRunner
{
	readonly GitRunnerOptions _options;

	public GitRunner() : this(new GitRunnerOptions())
	{
	}

	public GitRunner(GitRunnerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;
	}

	public IAsyncEnumerable<string> RunAsync(
		string arguments,
		CancellationToken cancellationToken = default) =>
		RunAsync(arguments, workingDirectory: null, cancellationToken);

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

		var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
		{
			SingleWriter = true,
			SingleReader = true,
			AllowSynchronousContinuations = false,
		});

		using var process = new Process();
		process.StartInfo = startInfo;
		process.EnableRaisingEvents = true;

		process.Start();

		var stdoutTask = ReadPipeIntoChannelAsync(
			process.StandardOutput, channel.Writer, cancellationToken);

		var stderrBuffer = new StringBuilder();
		var stderrTask = ReadPipeIntoBufferAsync(
			process.StandardError, stderrBuffer, cancellationToken);

		_ = FinishWriterAsync(stdoutTask, channel.Writer);

		try
		{
			await foreach (var line in channel.Reader.ReadAllAsync(cancellationToken))
			{
				yield return line;
			}

			await stderrTask.ConfigureAwait(false);
			await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		if (_options.ThrowOnNonZeroExitCode && process.ExitCode != 0)
		{
			throw new GitRunException(arguments, process.ExitCode, stderrBuffer.ToString());
		}
	}

	public Task<string?> ReadFirstLineAsync(
		string arguments,
		Func<string, bool>? predicate = null,
		CancellationToken cancellationToken = default) =>
		ReadFirstLineAsync(arguments, workingDirectory: null, predicate, cancellationToken);

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

	static async Task ReadPipeIntoChannelAsync(
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

	static async Task ReadPipeIntoBufferAsync(
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

	public string Run(string arguments) =>
		Run(arguments, workingDirectory: null);

	public string Run(string arguments, string? workingDirectory)
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

		using var process = new Process();
		process.StartInfo = startInfo;
		process.Start();

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();

		if (_options.ThrowOnNonZeroExitCode && process.ExitCode != 0)
		{
			throw new GitRunException(arguments, process.ExitCode, stderr);
		}

		return stdout;
	}

	public string? ReadFirstLine(
		string arguments,
		Func<string, bool>? predicate = null) =>
		ReadFirstLine(arguments, workingDirectory: null, predicate);

	public string? ReadFirstLine(
		string arguments,
		string? workingDirectory,
		Func<string, bool>? predicate = null)
	{
		predicate ??= _ => true;

		using var reader = new StringReader(Run(arguments, workingDirectory));
		while (reader.ReadLine() is { } line)
		{
			if (predicate(line))
			{
				return line;
			}
		}

		return null;
	}

	static async Task FinishWriterAsync(
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
