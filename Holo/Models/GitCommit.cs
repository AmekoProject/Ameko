// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// A commit in a Git repository
/// </summary>
/// <param name="author">Author's name</param>
/// <param name="email">Author's email</param>
/// <param name="message">Commit description</param>
/// <param name="date">Commit timestamp</param>
/// <param name="isMerge">If the commit is a merge commit</param>
public class GitCommit(
    string author,
    string email,
    string message,
    DateTimeOffset date,
    bool isMerge
)
{
    /// <summary>
    /// Author's name
    /// </summary>
    public string Author { get; private set; } = author;

    /// <summary>
    /// Author's email
    /// </summary>
    public string Email { get; private set; } = email;

    /// <summary>
    /// Commit description
    /// </summary>
    public string Message { get; private set; } = message;

    /// <summary>
    /// Commit timestamp
    /// </summary>
    public DateTimeOffset Date { get; private set; } = date;

    /// <summary>
    /// If the commit is a merge commit
    /// </summary>
    public bool IsMerge { get; private set; } = isMerge;
}
