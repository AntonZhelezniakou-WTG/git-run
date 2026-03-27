using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitRun;

namespace GitRun.Tests;

[TestFixture]
public sealed class GitRunnerTests
{
	// Locate a temporary directory that is a valid git repository so we can run real commands.
	private static string CreateTempGitRepo()
	{
		var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(dir);
		RunGit("init", dir);
		RunGit("config user.email \"test@example.com\"", dir);
		RunGit("config user.name \"Test\"", dir);
		RunGit("commit --allow-empty -m init", dir);
		return dir;
	}

	private static void RunGit(string args, string workingDir)
	{
		var psi = new System.Diagnostics.ProcessStartInfo("git", args)
		{
			WorkingDirectory = workingDir,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		using var p = System.Diagnostics.Process.Start(psi)!;
		p.WaitForExit();
	}

	[Test]
	public async Task RunAsync_ValidCommand_ReturnsLines()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			var lines = new List<string>();
			await foreach (var line in runner.RunAsync("log --oneline"))
			{
				lines.Add(line);
			}

			Assert.That(lines, Is.Not.Empty);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task RunAsync_WithWorkingDirectoryOverride_UsesOverride()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			// The runner has NO working directory set; we pass it explicitly.
			var runner = new GitRunner(new GitRunnerOptions
			{
				ThrowOnNonZeroExitCode = true,
			});

			var lines = new List<string>();
			await foreach (var line in runner.RunAsync("log --oneline", repoDir))
			{
				lines.Add(line);
			}

			Assert.That(lines, Is.Not.Empty);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task RunAsync_NonZeroExitCode_ThrowsGitRunException()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			// "git this-command-does-not-exist" exits with code 1.
			Assert.ThrowsAsync<GitRunException>(async () =>
			{
				await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
			});
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task RunAsync_NonZeroExitCode_ExceptionContainsStandardError()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			var ex = Assert.ThrowsAsync<GitRunException>(async () =>
			{
				await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
			});

			Assert.That(ex, Is.Not.Null);
			Assert.That(ex!.StandardError, Is.Not.Empty);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task RunAsync_NonZeroExitCode_DoesNotThrowWhenDisabled()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = false,
			});

			// Should not throw even though the command fails.
			await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public void RunAsync_CancellationRequested_StopsEnumeration()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = false,
			});

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			Assert.CatchAsync<OperationCanceledException>(async () =>
			{
				await foreach (var _ in runner.RunAsync("log --oneline", cts.Token)) { }
			});
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public void GitRunException_ContainsCorrectProperties()
	{
		const string args = "gc --prune=now --aggressive";
		const int exitCode = 128;
		const string stderr = "fatal: not a git repository";
		var ex = new GitRunException(args, exitCode, stderr);

		Assert.That(ex.Arguments, Is.EqualTo(args));
		Assert.That(ex.ExitCode, Is.EqualTo(exitCode));
		Assert.That(ex.StandardError, Is.EqualTo(stderr));
		Assert.That(ex.Message, Does.Contain(args));
		Assert.That(ex.Message, Does.Contain("128"));
	}

	[Test]
	public void GitRunException_DefaultStandardError_IsEmpty()
	{
		var ex = new GitRunException("status", 1);
		Assert.That(ex.StandardError, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GitRunnerOptions_Defaults_AreCorrect()
	{
		var opts = new GitRunnerOptions();

		Assert.That(opts.GitExecutable, Is.EqualTo("git"));
		Assert.That(opts.ThrowOnNonZeroExitCode, Is.True);
		Assert.That(opts.WorkingDirectory, Is.Null);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void RunAsync_NullOrWhitespaceArguments_Throws(string? args)
	{
		var runner = new GitRunner();

		// null produces ArgumentNullException (a subtype of ArgumentException);
		// empty/whitespace produces ArgumentException — CatchAsync accepts subtypes.
		Assert.CatchAsync<ArgumentException>(async () =>
		{
			await foreach (var _ in runner.RunAsync(args!)) { }
		});
	}

	[Test]
	public async Task ReadFirstLineAsync_ReturnsFirstLine()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = await runner.ReadFirstLineAsync("log --oneline");

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Is.Not.Empty);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task ReadFirstLineAsync_WithPredicate_ReturnsMatchingLine()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = await runner.ReadFirstLineAsync(
				"log --oneline",
				predicate: l => l.Contains("init"));

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Does.Contain("init"));
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task ReadFirstLineAsync_NoMatch_ReturnsNull()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = await runner.ReadFirstLineAsync(
				"log --oneline",
				predicate: _ => false);

			Assert.That(line, Is.Null);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}

	[Test]
	public async Task ReadFirstLineAsync_WithWorkingDirectory_UsesOverride()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner();

			var line = await runner.ReadFirstLineAsync("log --oneline", repoDir);

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Is.Not.Empty);
		}
		finally
		{
			Directory.Delete(repoDir, recursive: true);
		}
	}
}

