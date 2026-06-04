// SPDX-License-Identifier: MPL-2.0

namespace Holo.Scripting.Models;

/// <summary>
/// Information about an exported <see cref="HoloScript"/> method
/// </summary>
public class MethodInfo
{
    /// <summary>
    /// Name of the method
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Uniquely identifying name for the method
    /// </summary>
    /// <remarks>
    /// The name will be passed to <see cref="HoloScript.ExecuteAsync"/>
    /// </remarks>
    public required string QualifiedName { get; init; }

    /// <summary>
    /// Optional sub-menu name for categorization
    /// </summary>
    public string? Submenu { get; init; }
}
