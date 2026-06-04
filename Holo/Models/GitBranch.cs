// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// A branch in a Git repository
/// </summary>
/// <param name="branchName">Name of the branch</param>
/// <param name="isRemote">If the branch is a remote branch</param>
/// <param name="isTracking">If the branch is a local tracking branch</param>
public class GitBranch(string branchName, bool isRemote, bool isTracking)
{
    /// <summary>
    /// Name of the branch
    /// </summary>
    public string Name => branchName;

    /// <summary>
    /// If the branch is a remote branch
    /// </summary>
    public bool IsRemote => isRemote;

    /// <summary>
    /// If the branch is a local tracking branch
    /// </summary>
    public bool IsTracking => isTracking;
}
