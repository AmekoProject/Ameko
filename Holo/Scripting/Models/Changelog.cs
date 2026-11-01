// SPDX-License-Identifier: MPL-2.0

namespace Holo.Scripting.Models;

/// <summary>
/// A log of changes made for a version of a <see cref="IHoloExecutable"/>
/// </summary>
public class Changelog
{
    /// <summary>
    /// The version described by this changelog
    /// </summary>
    public required decimal Version { get; init; }

    /// <summary>
    /// List of additions
    /// </summary>
    public string[]? Added { get; init; }

    /// <summary>
    /// List of fixes
    /// </summary>
    public string[]? Fixed { get; init; }

    /// <summary>
    /// List of changes that don't fit into the other categories
    /// </summary>
    public string[]? Changed { get; init; }

    /// <summary>
    /// List of removals
    /// </summary>
    public string[]? Removed { get; init; }

    /// <summary>
    /// List of deprecated functionality, mostly for library use
    /// </summary>
    public string[]? Deprecated { get; init; }
}
