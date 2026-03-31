namespace GitRun.Tests;

[TestFixture]
public sealed class GitRunnerTests
{
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
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public async Task RunAsync_WithWorkingDirectoryOverride_UsesOverride()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
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
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public Task RunAsync_NonZeroExitCode_ThrowsGitRunException()
	{
		try
		{
			var repoDir = CreateTempGitRepo();
			try
			{
				var runner = new GitRunner(new GitRunnerOptions
				{
					WorkingDirectory = repoDir,
					ThrowOnNonZeroExitCode = true,
				});

					Assert.ThrowsAsync<GitRunException>(async () =>
				{
					await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
				});
			}
			finally
			{
				DeleteTempRepo(repoDir);
			}

			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	[Test]
	public Task RunAsync_NonZeroExitCode_ExceptionContainsStandardError()
	{
		try
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
				DeleteTempRepo(repoDir);
			}

			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
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

				await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
		}
		finally
		{
			DeleteTempRepo(repoDir);
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
			DeleteTempRepo(repoDir);
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
	public void Run_ValidCommand_ReturnsOutput()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			var output = runner.Run("log --oneline");

			Assert.That(output, Is.Not.Null);
			Assert.That(output, Is.Not.Empty);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_WithWorkingDirectoryOverride_UsesOverride()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				ThrowOnNonZeroExitCode = true,
			});

			var output = runner.Run("log --oneline", repoDir);

			Assert.That(output, Is.Not.Null);
			Assert.That(output, Is.Not.Empty);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_NonZeroExitCode_ThrowsGitRunException()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			Assert.Throws<GitRunException>(() => runner.Run("this-command-does-not-exist"));
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_NonZeroExitCode_ExceptionContainsStandardError()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			var ex = Assert.Throws<GitRunException>(() => runner.Run("this-command-does-not-exist"));

			Assert.That(ex, Is.Not.Null);
			Assert.That(ex!.StandardError, Is.Not.Empty);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_NonZeroExitCode_DoesNotThrowWhenDisabled()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = false,
			});

			Assert.DoesNotThrow(() => runner.Run("this-command-does-not-exist"));
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Run_NullOrWhitespaceArguments_Throws(string? args)
	{
		var runner = new GitRunner();

		Assert.Catch<ArgumentException>(() => runner.Run(args!));
	}

	[Test]
	public void ReadFirstLine_ReturnsFirstLine()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = runner.ReadFirstLine("log --oneline");

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Is.Not.Empty);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void ReadFirstLine_WithPredicate_ReturnsMatchingLine()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = runner.ReadFirstLine(
				"log --oneline",
				predicate: l => l.Contains("init"));

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Does.Contain("init"));
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void ReadFirstLine_NoMatch_ReturnsNull()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
			});

			var line = runner.ReadFirstLine(
				"log --oneline",
				predicate: _ => false);

			Assert.That(line, Is.Null);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void ReadFirstLine_WithWorkingDirectory_UsesOverride()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner();

			var line = runner.ReadFirstLine("log --oneline", repoDir);

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Is.Not.Empty);
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void ReadFirstLine_WithWorkingDirectoryAndPredicate_ReturnsMatchingLine()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner();

			var line = runner.ReadFirstLine(
				"log --oneline",
				repoDir,
				predicate: l => l.Contains("init"));

			Assert.That(line, Is.Not.Null);
			Assert.That(line, Does.Contain("init"));
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
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
			DeleteTempRepo(repoDir);
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
			DeleteTempRepo(repoDir);
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
			DeleteTempRepo(repoDir);
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
			DeleteTempRepo(repoDir);
		}
	}

		[Test]
	public async Task RunAsync_ProcessGroup_CancelledMidOperation_TerminatesCleanly()
	{
		var repoDir = CreateTempGitRepoWithManyCommits(100);
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = false,
			});

			using var cts = new CancellationTokenSource();

			var task = Task.Run(async () =>
			{
				await foreach (var _ in runner.RunAsync("log --stat", cts.Token))
				{
					cts.Cancel();
				}
			});

			var completedInTime = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))) == task;

			Assert.That(completedInTime, Is.True, "RunAsync should terminate quickly after cancellation (ProcessGroup.TerminateAll was not effective)");
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_ProcessGroup_ConcurrentCalls_AllComplete()
	{
		var repoDirs = Enumerable.Range(0, 4).Select(_ => CreateTempGitRepo()).ToArray();
		try
		{
			var tasks = repoDirs.Select(repoDir => Task.Run(() =>
			{
				var runner = new GitRunner(new GitRunnerOptions
				{
					WorkingDirectory = repoDir,
					ThrowOnNonZeroExitCode = true,
				});
				return runner.Run("log --oneline");
			})).ToArray();

			Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));

			foreach (var task in tasks)
			{
				Assert.That(task.Result, Is.Not.Empty);
			}
		}
		finally
		{
			foreach (var repoDir in repoDirs)
				DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public async Task RunAsync_ProcessGroup_ConcurrentCalls_AllComplete()
	{
		var repoDirs = Enumerable.Range(0, 4).Select(_ => CreateTempGitRepo()).ToArray();
		try
		{
			var tasks = repoDirs.Select(async repoDir =>
			{
				var runner = new GitRunner(new GitRunnerOptions
				{
					WorkingDirectory = repoDir,
					ThrowOnNonZeroExitCode = true,
				});
				var lines = new List<string>();
				await foreach (var line in runner.RunAsync("log --oneline"))
					lines.Add(line);
				return lines;
			}).ToArray();

			await Task.WhenAll(tasks);

			foreach (var task in tasks)
			{
				Assert.That(task.Result, Is.Not.Empty);
			}
		}
		finally
		{
			foreach (var repoDir in repoDirs)
				DeleteTempRepo(repoDir);
		}
	}

	[Test]
	public void Run_ProcessGroup_SequentialCallsAfterCompletion_AllSucceed()
	{
		var repoDir = CreateTempGitRepo();
		try
		{
			var runner = new GitRunner(new GitRunnerOptions
			{
				WorkingDirectory = repoDir,
				ThrowOnNonZeroExitCode = true,
			});

			for (var i = 0; i < 3; i++)
			{
				var output = runner.Run("log --oneline");
				Assert.That(output, Is.Not.Empty);
			}
		}
		finally
		{
			DeleteTempRepo(repoDir);
		}
	}

	static void DeleteTempRepo(string path)
	{
		foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			File.SetAttributes(file, FileAttributes.Normal);
		}
		Directory.Delete(path, recursive: true);
	}

	static string CreateTempGitRepo()
	{
		var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(dir);
		RunGit("init", dir);
		RunGit("config user.email \"test@example.com\"", dir);
		RunGit("config user.name \"Test\"", dir);
		RunGit("commit --allow-empty -m init", dir);
		return dir;
	}

	static string CreateTempGitRepoWithManyCommits(int count)
	{
		var dir = CreateTempGitRepo();
		for (var i = 0; i < count; i++)
			RunGit($"commit --allow-empty -m commit{i}", dir);
		return dir;
	}

	static void RunGit(string args, string workingDir)
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
}
