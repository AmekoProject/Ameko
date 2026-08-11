// SPDX-License-Identifier: MPL-2.0

namespace AssCS.IO;

/// <summary>
/// Supports the reading and parsing of subtitle files
/// </summary>
public abstract class FileParser
{
    /// <summary>
    /// Parse the contents of a <see cref="TextReader"/> into a <see cref="Document"/>
    /// </summary>
    /// <param name="reader">Data to parse</param>
    /// <returns><see cref="Document"/> represented by the <paramref name="reader"/></returns>
    public abstract Document Parse(TextReader reader);
}
