// SPDX-License-Identifier: MPL-2.0

namespace AssCS.Overrides.Blocks;

/// <summary>
/// A comment block
/// </summary>
/// <remarks>
/// Text within a set of override brackets <c>{ }</c>
/// that does not contain an override is a Comment
/// </remarks>
public class CommentBlock(string data) : Block($"{{{data}}}", BlockType.Comment)
{
    /// <summary>
    /// Comment text, excluding the surrounding brackets <c>{ }</c>
    /// </summary>
    public string InnerText => data;
}
