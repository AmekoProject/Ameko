// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using AssCS;

namespace Holo.Models;

/// <summary>
/// Represents an item in a <see cref="Project"/>.
/// </summary>
public abstract class ProjectItem : BindableBase
{
    /// <summary>
    /// Unique identifier for the item.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Optional display name override.
    /// </summary>
    public string? Name
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    /// The linked workspace,
    /// or <see langword="null"/> if not loaded or the item is a directory.
    /// </summary>
    public Workspace? Workspace { get; set; }

    /// <summary>
    /// File system save location.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Indicates whether the item has a file system location.
    /// </summary>
    public bool IsSavedToFileSystem => Uri is not null;

    /// <summary>
    /// Indicates whether the workspace is loaded.
    /// </summary>
    public bool IsLoaded => Workspace is not null;

    /// <summary>
    /// Child items (only applicable to directories).
    /// </summary>
    public RangeObservableCollection<ProjectItem> Children { get; init; } = [];

    /// <summary>
    /// The type of this item.
    /// </summary>
    public virtual ProjectItemType Type => ProjectItemType.Unknown;

    /// <summary>
    /// Display title for the item.
    /// </summary>
    public virtual string Title => "Untitled Item";
}

/// <summary>
/// A <see cref="ProjectItem"/> representing a <see cref="Document"/>
/// </summary>
public class DocumentItem : ProjectItem
{
    /// <inheritdoc />
    public override ProjectItemType Type => ProjectItemType.Document;

    /// <inheritdoc />
    public override string Title =>
        Name
        ?? (
            IsLoaded ? Workspace!.Title
            : IsSavedToFileSystem ? Path.GetFileName(Uri!.LocalPath)
            : $"New {Id}"
        );
}

/// <summary>
/// A <see cref="ProjectItem"/> representing a directory
/// </summary>
public class DirectoryItem : ProjectItem
{
    /// <inheritdoc />
    public override ProjectItemType Type => ProjectItemType.Directory;

    /// <inheritdoc />
    public override string Title => Name ?? "Untitled Directory";
}
