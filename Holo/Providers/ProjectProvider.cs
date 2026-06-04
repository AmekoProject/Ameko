// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions;
using AssCS;
using Microsoft.Extensions.Logging;

namespace Holo.Providers;

/// <summary>
/// Provides methods for accessing and instantiating <see cref="Project"/>s
/// </summary>
public class ProjectProvider(
    IFileSystem fileSystem,
    ILoggerFactory loggerFactory,
    IWorkspaceFactory workspaceFactory
) : BindableBase, IProjectProvider
{
    /// <inheritdoc />
    public Project Current
    {
        get;
        set => SetProperty(ref field, value);
    } = new(fileSystem, loggerFactory.CreateLogger<Project>(), workspaceFactory);

    /// <inheritdoc />
    public Project Create(bool isEmpty = false)
    {
        return new Project(
            fileSystem,
            loggerFactory.CreateLogger<Project>(),
            workspaceFactory,
            isEmpty
        );
    }

    /// <inheritdoc />
    public Project CreateFromFile(Uri uri)
    {
        return new Project(
            fileSystem,
            loggerFactory.CreateLogger<Project>(),
            workspaceFactory,
            uri
        );
    }

    /// <inheritdoc />
    public Project CreateFromDirectory(Uri uri)
    {
        return new Project(
            fileSystem,
            loggerFactory.CreateLogger<Project>(),
            workspaceFactory,
            uri
        );
    }
}
