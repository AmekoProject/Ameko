// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Holo.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Holo.Providers;

/// <summary>
/// Basic service for interacting with Git
/// </summary>
public class GitService(IFileSystem fileSystem, ILogger<GitService> logger) : IGitService
{
    [MemberNotNullWhen(true, nameof(WorkingDirectory))]
    private bool Ready { get; set; }
    private Uri? WorkingDirectory { get; set; }

    /// <inheritdoc />
    public void SetWorkingDirectory(Uri? path)
    {
        if (path is null)
        {
            WorkingDirectory = null;
            Ready = false;
            return;
        }
        WorkingDirectory = new Uri(fileSystem.Path.GetDirectoryName(path.LocalPath) ?? "/");
        Ready = true;
    }

    /// <inheritdoc />
    public bool IsRepository()
    {
        try
        {
            return Ready && Repository.IsValid(WorkingDirectory.LocalPath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Check if the project file is located in a directory containing a <c>.git</c> directory
    /// </summary>
    /// <returns><see langword="true"/> if a <c>.git</c> directory is present</returns>
    public bool HasGitDirectory()
    {
        if (!Ready)
            return false;

        return fileSystem.Directory.GetDirectories(WorkingDirectory.LocalPath, ".git").Length != 0;
    }

    /// <inheritdoc />
    public void InitializeRepository()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");

        try
        {
            Repository.Init(WorkingDirectory.LocalPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize git repository");
        }
    }

    /// <inheritdoc />
    public IEnumerable<GitCommit> GetRecentCommits(int count = 15)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var latest = repo.Commits.Take(count).ToList();
            return (
                from commit in latest
                let signature = commit.Author ?? commit.Committer
                let name = signature?.Name ?? "-"
                let email = signature?.Email ?? "-"
                let message = commit.MessageShort ?? "-"
                let date = commit.Committer?.When ?? DateTimeOffset.MinValue
                let isMerge = commit.Parents?.Count() > 1
                select new GitCommit(name, email, message, date, isMerge)
            ).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get recent commits");
            return [];
        }
    }

    /// <inheritdoc />
    public IEnumerable<GitStatusEntry> GetStagedFiles()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            return repo.RetrieveStatus()
                .Where(se =>
                    se.State.HasFlag(FileStatus.ModifiedInIndex)
                    || se.State.HasFlag(FileStatus.NewInIndex)
                )
                .Select(se => new GitStatusEntry(se.FilePath, true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get staged files");
            return [];
        }
    }

    /// <inheritdoc />
    public IEnumerable<GitStatusEntry> GetUnstagedFiles()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            return repo.RetrieveStatus()
                .Where(se =>
                    se.State.HasFlag(FileStatus.ModifiedInWorkdir)
                    || se.State.HasFlag(FileStatus.NewInWorkdir)
                )
                .Select(se => new GitStatusEntry(se.FilePath, false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get unstaged files");
            return [];
        }
    }

    /// <inheritdoc />
    public void StageFiles(IEnumerable<string> files)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            Commands.Stage(repo, files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stage files");
        }
    }

    /// <inheritdoc />
    public void UnstageFiles(IEnumerable<string> files)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            Commands.Unstage(repo, files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unstage files");
        }
    }

    /// <inheritdoc />
    public void Fetch()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var remoteName = repo.Head.TrackedBranch?.RemoteName ?? "origin";
            var remote = repo.Network.Remotes[remoteName];

            Commands.Fetch(
                repo,
                remote.Name,
                remote.FetchRefSpecs.Select(r => r.Specification),
                null,
                null
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch");
        }
    }

    /// <inheritdoc />
    public void Pull()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var options = new PullOptions { FetchOptions = new FetchOptions() };

            var merger = repo.Config.BuildSignature(DateTimeOffset.Now);
            Commands.Pull(repo, merger, options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pull");
        }
    }

    /// <inheritdoc />
    public void Push()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var branch = repo.Head;
            var remote = repo.Network.Remotes[
                branch.TrackedBranch?.RemoteName
                    ?? throw new InvalidOperationException("Local branch has no tracking branch")
            ];

            var refSpec = $"{branch.CanonicalName}:{branch.UpstreamBranchCanonicalName}";
            repo.Network.Push(remote, refSpec, new PushOptions());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to push");
        }
    }

    /// <inheritdoc />
    public bool CanPush()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var head = repo.Head;
            if (!head.IsTracking)
                return false;

            var tracking = head.TrackingDetails;
            return tracking.AheadBy is > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check if can push");
            return false;
        }
    }

    /// <inheritdoc />
    public GitBranch GetCurrentBranch()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var branch = repo.Head;
            return new GitBranch(branch.FriendlyName, branch.IsRemote, branch.IsTracking);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get current branch");
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<GitBranch> GetLocalBranches()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            return repo
                .Branches.Where(b => !b.IsRemote)
                .Select(b => new GitBranch(b.FriendlyName, false, b.IsTracking));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get local branches");
            return [];
        }
    }

    /// <inheritdoc />
    public IEnumerable<GitBranch> GetRemoteBranches()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            return repo
                .Branches.Where(b => b.IsRemote)
                .Select(b => new GitBranch(b.FriendlyName, true, false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get remote branches");
            return [];
        }
    }

    /// <inheritdoc />
    public GitBranch CreateBranch(string name)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var branch = repo.CreateBranch(name);
            return new GitBranch(branch.FriendlyName, false, branch.IsTracking);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create branch");
            throw;
        }
    }

    /// <inheritdoc />
    public void SwitchBranches(string name)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var branch = repo.Branches[name];
            if (branch is null)
                throw new ArgumentException($"Branch {name} not found", nameof(name));

            Commands.Checkout(repo, branch);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to switch branches");
        }
    }

    /// <inheritdoc />
    public GitCommit Commit(string message)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        if (!GetStagedFiles().Any())
            throw new InvalidOperationException("No files staged for commit");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
            var commit = repo.Commit(message, signature, signature);
            return new GitCommit(
                commit.Author.Name,
                commit.Author.Email,
                commit.MessageShort,
                commit.Author.When,
                false
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get commit");
            throw;
        }
    }

    /// <inheritdoc />
    public void Reset(ResetMode mode, string commitSha)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var commit = repo.Lookup<Commit>(commitSha);
            if (commit is null)
                throw new ArgumentException($"Commit {commitSha} not found", nameof(commitSha));

            repo.Reset(mode, commit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset commit");
        }
    }

    /// <inheritdoc />
    public void StashSave(string? message = null)
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            var author = repo.Config.BuildSignature(DateTimeOffset.Now);
            repo.Stashes.Add(author, message ?? "Stashed Changes", StashModifiers.Default);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stash changes");
        }
    }

    /// <inheritdoc />
    public void StashPop()
    {
        if (!Ready)
            throw new InvalidOperationException("Working directory not set");
        if (!IsRepository())
            throw new InvalidOperationException("Not in a repository");

        try
        {
            using var repo = new Repository(WorkingDirectory.LocalPath);
            if (!repo.Stashes.Any())
                return;

            repo.Stashes.Pop(0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stash changes");
        }
    }
}
