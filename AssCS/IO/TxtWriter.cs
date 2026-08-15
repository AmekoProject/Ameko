// SPDX-License-Identifier: MPL-2.0

using System.Text.RegularExpressions;

namespace AssCS.IO;

/// <summary>
/// Writes a document to a text file
/// </summary>
public partial class TxtWriter(
    ConsumerInfo consumer,
    bool includeComments = true,
    bool includeActors = true
) : FileWriter
{
    /// <inheritdoc />
    public override bool Write(Document document, TextWriter writer, bool export = false)
    {
        writer.WriteLine($"# Exported by {consumer.Name} {consumer.Version} [AssCS]");

        var hasActors = document.EventManager.Actors.Count != 0;

        foreach (var line in document.EventManager.Events)
        {
            if (line.IsComment && !includeComments)
                continue;
            if (line.IsComment)
                writer.Write("# ");

            var actor =
                hasActors && includeActors && !string.IsNullOrEmpty(line.Actor)
                    ? $"{line.Actor}: "
                    : string.Empty;
            writer.Write(actor);

            writer.WriteLine(StripNewlines(line.GetStrippedText()));
        }

        return true;
    }

    /// <summary>
    /// Strips newlines from a string
    /// </summary>
    /// <param name="text">Input that may contain newlines</param>
    /// <returns>The input <paramref name="text"/> with newlines removed</returns>
    /// <remarks>
    /// <para>
    /// This function supports both "big" newlines (<c>\N</c>)
    /// and "small" newlines (<c>\n</c>).
    /// </para><para>
    /// If a newline is not adjacent to any spaces, then the newline will
    /// be replaced with a space. If, however, the newline is adjacent
    /// to one or more spaces, the set will be replaced with a single space.
    /// </para>
    /// </remarks>
    public static string StripNewlines(string text)
    {
        return NEWLINE_REGEX().Replace(text, " ");
    }

    [GeneratedRegex(@"\ *\\[Nn]\ *")]
    private static partial Regex NEWLINE_REGEX();
}
