// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using System.Text;
using AssCS.Overrides;

namespace Holo.Models;

/// <summary>
/// Information about a syllable
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SyllableInfo
{
    /// <summary>
    /// Start time in milliseconds
    /// </summary>
    public long StartTime;

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long Duration;

    /// <summary>
    /// Inner text content (Excluding tags)
    /// </summary>
    public fixed byte Text[MaxSyllableTextBytes];

    private const int MaxSyllableTextBytes = 48; // Excludes null-termination

    /// <summary>
    /// Build a SyllableInfo
    /// </summary>
    /// <param name="syl">Syllable object</param>
    /// <returns>Thin SyllableInfo</returns>
    public static SyllableInfo From(Syllable syl)
    {
        var info = new SyllableInfo
        {
            StartTime = syl.Start.TotalMilliseconds,
            Duration = syl.Duration,
        };

        var span = new Span<byte>(info.Text, MaxSyllableTextBytes);
        span.Clear();

        if (Encoding.UTF8.GetByteCount(syl.Text) <= MaxSyllableTextBytes - 1)
        {
            Encoding.UTF8.GetBytes(syl.Text, span);
        }
        else
        {
            Encoding.UTF8.GetBytes(TruncateUtf8(syl.Text, MaxSyllableTextBytes - 1), span);
        }

        return info;
    }

    private static string TruncateUtf8(string text, int maxBytes)
    {
        var byteCount = 0;
        var charIndex = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (byteCount + rune.Utf8SequenceLength > maxBytes)
                break;
            byteCount += rune.Utf8SequenceLength;
            charIndex += rune.Utf16SequenceLength;
        }
        return text[..charIndex];
    }
}
