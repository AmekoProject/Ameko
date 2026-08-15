// SPDX-License-Identifier: MPL-2.0

namespace AssCS.IO;

/// <summary>
/// Supports the writing of subtitle files
/// </summary>
public abstract class FileWriter
{
    /// <summary>
    /// Write an ass document to a TextWriter
    /// </summary>
    /// <param name="document">Document to write</param>
    /// <param name="writer">Writer to write to</param>
    /// <param name="export"><see langword="true"/> if this write is an export</param>
    /// <returns><see langword="true"/> if writing was successful</returns>
    public abstract bool Write(Document document, TextWriter writer, bool export = false);

    /// <summary>
    /// Write an ass document to string
    /// </summary>
    /// <param name="document">Document to write</param>
    /// <param name="export"><see langword="true"/> if this write is an export</param>
    /// <returns>Text representation of the file</returns>
    public string Write(Document document, bool export = false)
    {
        using var writer = new StringWriter();
        Write(document, writer, export);
        return writer.ToString();
    }
}
