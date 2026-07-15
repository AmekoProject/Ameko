// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions;
using System.Text;
using AssCS;
using AssCS.IO;

namespace Holo.IO;

/// <summary>
/// Extensions for AssCS
/// </summary>
public static class AssCSExtensions
{
    /// <summary>
    /// Write an ass document to file
    /// </summary>
    /// <param name="fileWriter">FileWriter to use</param>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="savePath">Path to write to</param>
    /// <param name="export"><see langword="true"/> if this write is an export</param>
    /// <returns><see langword="true"/> if writing was successful</returns>
    /// <exception cref="IOException">If writing fails</exception>
    public static bool Write(
        this FileWriter fileWriter,
        IFileSystem fileSystem,
        Uri savePath,
        bool export = false
    )
    {
        var path = savePath.LocalPath;

        if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
            fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

        using var fs = fileSystem.FileStream.New(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );
        using var writer = new StreamWriter(fs, encoding: Encoding.UTF8);

        var result = fileWriter.Write(writer, export);

        writer.Flush();
        fs.SetLength(fs.Position);
        return result;
    }

    /// <summary>
    /// Parse a file into a <see cref="Document"/>
    /// </summary>
    /// <param name="fileParser">FileParser to use</param>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="savePath">Path to the file to open</param>
    /// <returns><see cref="Document"/> at the <paramref name="savePath"/></returns>
    public static Document Parse(this FileParser fileParser, IFileSystem fileSystem, Uri savePath)
    {
        var path = savePath.LocalPath;

        if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
            fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

        if (!fileSystem.File.Exists(path))
            throw new FileNotFoundException("Document not found", path);

        using var fs = fileSystem.FileStream.New(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        using var reader = new StreamReader(fs, encoding: Encoding.UTF8);

        return fileParser.Parse(reader);
    }
}
