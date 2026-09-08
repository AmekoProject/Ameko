// SPDX-License-Identifier: GPL-3.0-only

namespace Ameko.DataModels;

/// <summary>
/// Information about a track
/// </summary>
public class TrackInformation
{
    public required int Index { get; init; }
    public required string Codec { get; init; }
    public required string Language { get; init; }
    public required string Title { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return (string.IsNullOrEmpty(Language), string.IsNullOrEmpty(Title)) switch
        {
            (true, true) => $"{Index}. {Codec}",
            (false, false) => $"{Index}. {Codec} ({Language}, {Title})",
            (true, false) => $"{Index}. {Codec} ({Title})",
            (false, true) => $"{Index}. {Codec} ({Language})",
        };
    }
}
