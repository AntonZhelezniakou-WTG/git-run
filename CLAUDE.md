# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and test

```
dotnet build GitRun.slnx
dotnet test tests/GitRun.Tests/GitRun.Tests.csproj --no-build
dotnet test tests/GitRun.Tests/GitRun.Tests.csproj --no-build --filter "FullyQualifiedName~<TestName>"
```

Run the full test suite inside a Linux container (requires PowerShell 7+ and Docker/Rancher Desktop):

```powershell
pwsh ./scripts/test-linux.ps1
pwsh ./scripts/test-linux.ps1 -Filter "FullyQualifiedName~<TestName>" -Configuration Debug -Rebuild
```

## Architecture

`git-run` is a small .NET 10 library that executes git commands and streams their output. There is no CLI, UI, hosting, logging, or DI infrastructure.

**Public API** (`src/GitRun/`):

- `IGitRunner` — the contract: async streaming (`RunAsync` → `IAsyncEnumerable<string>`), first-line helpers (`ReadFirstLineAsync`/`ReadFirstLine`), and synchronous wrappers (`Run`/`ReadFirstLine`). All methods accept an optional `workingDirectory` override.
- `GitRunner` — the implementation. The namespace is `git_runs` (plural) to avoid a name/type collision with the class `GitRunner` (singular).
- `GitRunnerOptions` — configures working directory, git executable path, and whether to throw on non-zero exit codes.
- `GitRunException` — thrown on non-zero exit; carries `Arguments`, `ExitCode`, and `StandardError`.

**Internals:**

- Uses `ProcessGroups.ProcessGroup` (not raw `Process`) to ensure child processes are cleaned up on all code paths including cancellation.
- stdout is piped into an unbound `Channel<string>` for streaming; stderr is buffered into a `StringBuilder` for exception messages.
- Public methods must be safe for concurrent calls.

## Non-obvious constraints

**Project references** — the test project uses `<Reference>` + `AssemblySearchPaths`, never `ProjectReference` or `HintPath`. Build ordering is defined in `GitRun.slnx`. Always build before running tests (`--no-build` skips the redundant MSBuild invocation but requires a prior build).

**MSBuild paths** — `Directory.Build.props` defines `$(RepoRoot)` and `$(GitRunProjectDir)`. Use these instead of relative `../` paths in `.csproj`/`.props` files. Add new `$(XxxProjectDir)` properties there when new cross-project paths are needed.

**Package versions** — managed centrally in `Directory.Packages.props`. Never put versions on individual `PackageReference` items. Do not add new NuGet packages without explicit approval; the production project intentionally has no external dependencies (ProcessGroup is the sole exception).

**Formatting** — tabs only, no spaces, in all files (`.cs`, `.csproj`, `.props`, `.md`, etc.). LF line endings everywhere except `.bat`/`.cmd`.

## Changelog

Add consumer-facing bullets under `## [Unreleased]` in `CHANGELOG.md` for every functional change. Use the subsections `### Added`, `### Changed`, `### Fixed`. Do not touch versioned sections — those are managed by the release workflow.

Commit subject prefixes determine git-cliff auto-fill buckets if `[Unreleased]` is empty at release time: `Add`/`Feat` → Added; `Fix`/`Bug` → Fixed; `Remove`/`Delete`/`Drop` → Removed; `Doc`/`Chore`/`Test`/`Style` are skipped.

## Git rules

Do not create commits or push changes. Show the user the diff and let them decide.
