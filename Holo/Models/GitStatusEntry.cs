// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Modified file in a Git repository
/// </summary>
/// <param name="filePath">Path to the file</param>
/// <param name="isStaged">If the file is staged for commit</param>
public class GitStatusEntry(string filePath, bool isStaged)
{
    /// <summary>
    /// Path to the file
    /// </summary>
    public string FilePath => filePath;

    /// <summary>
    /// Name of the file, derived from the <see cref="FilePath"/>
    /// </summary>
    public string FileName => Path.GetFileName(filePath);

    /// <summary>
    /// If the file is staged for commit
    /// </summary>
    public bool IsStaged => isStaged;
}
