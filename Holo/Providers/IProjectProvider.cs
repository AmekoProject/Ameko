// SPDX-License-Identifier: MPL-2.0

using System.ComponentModel;

namespace Holo.Providers;

/// <summary>
/// Provides methods for accessing and instantiating <see cref="Project"/>s
/// </summary>
public interface IProjectProvider : INotifyPropertyChanged
{
    /// <summary>
    /// The current project
    /// </summary>
    public Project Current { get; set; }

    /// <summary>
    /// Create a new Project
    /// </summary>
    /// <param name="isEmpty">If the project should be created
    /// without a default <see cref="Workspace"/></param>
    /// <returns>The created project</returns>
    Project Create(bool isEmpty = false);

    /// <summary>
    /// Load a project file
    /// </summary>
    /// <param name="uri">Path to the project file</param>
    /// <returns>The created project</returns>
    Project CreateFromFile(Uri uri);

    /// <summary>
    /// Load a directory as a project
    /// </summary>
    /// <param name="uri">Path to the directory</param>
    /// <returns>The created project</returns>
    Project CreateFromDirectory(Uri uri);
}
