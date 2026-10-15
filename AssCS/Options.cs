// SPDX-License-Identifier: MPL-2.0

namespace AssCS;

/// <summary>
/// Options for calculations, defaults, etc.
/// </summary>
public static class Options
{
    /// <summary>
    /// If whitespace should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    public static bool CpsIncludesWhitespace { get; set; }

    /// <summary>
    /// If punctuation should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    public static bool CpsIncludesPunctuation { get; set; }

    /// <summary>
    /// If whitespace should be included in <see cref="Event.MaxLineWidth"/> calculation
    /// </summary>
    public static bool LineWidthIncludesWhitespace { get; set; } = true;

    /// <summary>
    /// If punctuation should be included in <see cref="Event.MaxLineWidth"/> calculation
    /// </summary>
    public static bool LineWidthIncludesPunctuation { get; set; } = true;

    /// <summary>
    /// Default layer to use when creating events
    /// </summary>
    public static int DefaultLayer { get; set; } = 0;
}
