using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitRun;

namespace GitRun.Tests;

public sealed class GitRunnerTests
{
    // Locate a temporary directory that is a valid git repository so we can run real commands.
    private static string CreateTempGitRepo()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        RunGit("init", dir);
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

    [Fact]
    public async Task RunAsync_ValidCommand_ReturnsLines()
    {
        var repoDir = CreateTempGitRepo();
        try
        {
            var runner = new GitRunner(new GitRunnerOptions
            {
                WorkingDirectory = repoDir,
                IncludeStandardError = false,
                ThrowOnNonZeroExitCode = true,
            });

            var lines = new List<string>();
            await foreach (var line in runner.RunAsync("log --oneline"))
            {
                lines.Add(line);
            }

            Assert.NotEmpty(lines);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_WithWorkingDirectoryOverride_UsesOverride()
    {
        var repoDir = CreateTempGitRepo();
        try
        {
            // The runner has NO working directory set; we pass it explicitly.
            var runner = new GitRunner(new GitRunnerOptions
            {
                IncludeStandardError = false,
                ThrowOnNonZeroExitCode = true,
            });

            var lines = new List<string>();
            await foreach (var line in runner.RunAsync("log --oneline", repoDir))
            {
                lines.Add(line);
            }

            Assert.NotEmpty(lines);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
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

            await Assert.ThrowsAsync<GitRunException>(async () =>
            {
                // "git this-command-does-not-exist" exits with code 1.
                await foreach (var _ in runner.RunAsync("this-command-does-not-exist")) { }
            });
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
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

    [Fact]
    public async Task RunAsync_CancellationRequested_StopsEnumeration()
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

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in runner.RunAsync("log --oneline", cts.Token)) { }
            });
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }

    [Fact]
    public void GitRunException_ContainsCorrectProperties()
    {
        const string args = "gc --prune=now --aggressive";
        const int exitCode = 128;
        var ex = new GitRunException(args, exitCode);

        Assert.Equal(args, ex.Arguments);
        Assert.Equal(exitCode, ex.ExitCode);
        Assert.Contains(args, ex.Message);
        Assert.Contains("128", ex.Message);
    }

    [Fact]
    public void GitRunnerOptions_Defaults_AreCorrect()
    {
        var opts = new GitRunnerOptions();

        Assert.Equal("git", opts.GitExecutable);
        Assert.True(opts.IncludeStandardError);
        Assert.True(opts.ThrowOnNonZeroExitCode);
        Assert.Null(opts.WorkingDirectory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_NullOrWhitespaceArguments_Throws(string? args)
    {
        var runner = new GitRunner();

        // null produces ArgumentNullException (a subtype of ArgumentException);
        // empty/whitespace produces ArgumentException — ThrowsAnyAsync accepts subtypes.
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in runner.RunAsync(args!)) { }
        });
    }
}
