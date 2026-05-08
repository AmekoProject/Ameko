// SPDX-License-Identifier: MPL-2.0

namespace AssCS.Overrides.Blocks;

/// <summary>
/// Extensions for a list of <see cref="Block"/>s
/// </summary>
public static class BlockListExtensions
{
    /// <summary>
    /// Extensions for a list of <see cref="Block"/>s
    /// </summary>
    /// <param name="blocks">List of <see cref="Block"/>s</param>
    extension(List<Block> blocks)
    {
        /// <summary>
        /// Normalize indexes relative to plain text in the event,
        /// excluding Override, Comment, and Drawing blocks
        /// </summary>
        /// <remarks>
        /// Given <c>Hello {\b1}World{\b0}!</c>, origin index 13 ("r"),
        /// normalized index 8 will be returned.
        /// </remarks>
        /// <param name="originIndex">Original index</param>
        /// <returns>Normalized index</returns>
        public int NormalizeIndex(int originIndex)
        {
            var remaining = originIndex;
            var plainLength = 0;

            for (var i = 0; i < blocks.Count && remaining > 0; i++)
            {
                var block = blocks[i];
                var blockLength = block.Text.Length;

                if (block is OverrideBlock or CommentBlock)
                {
                    remaining -= blockLength;
                    continue;
                }

                var consumed = Math.Min(blockLength, remaining);
                plainLength += consumed;
                remaining -= consumed;
            }

            return plainLength;
        }

        /// <summary>
        /// Normalize indexes relative to the given block
        /// </summary>
        /// <remarks>
        /// Given <c>Hello {\b1}World{\b0}!</c>, origin index 13 ("r"),
        /// normalized index 2 will be returned.
        /// </remarks>
        /// <param name="plainBlockIdx">Plain text block to normalize to</param>
        /// <param name="originIndex">Original index</param>
        /// <returns>Normalized index</returns>
        public int NormalizeIndex(int plainBlockIdx, int originIndex)
        {
            var remaining = originIndex;

            for (var i = 0; i < blocks.Count && remaining > 0; i++)
            {
                var block = blocks[i];
                var blockLength = block.Text.Length;

                if (block is OverrideBlock or CommentBlock)
                {
                    remaining -= blockLength;
                    continue;
                }

                var consumed = Math.Min(blockLength, remaining);

                // If this is the block we're targeting, return index within it
                if (i == plainBlockIdx)
                    return consumed;

                remaining -= consumed;
            }

            return remaining;
        }

        /// <summary>
        /// Get the index of the block at the given index in the text
        /// </summary>
        /// <param name="originIndex">Origin index in the text</param>
        /// <returns>Index of the block at the given <paramref name="originIndex"/></returns>
        public int BlockIndexAt(int originIndex)
        {
            var remaining = originIndex;
            var blockCount = blocks.Count;

            var i = 0;
            for (; i < blockCount; i++)
            {
                var block = blocks[i];
                var hasNext = i + 1 < blockCount;

                remaining -= block.Text.Length;
                switch (remaining)
                {
                    case < 0:
                        return i;
                    case 0:
                        return i + (hasNext && IsBraced(blocks[i + 1]) ? 1 : 0);
                }
            }
            return i;

            bool IsBraced(Block block) => block is OverrideBlock or CommentBlock;
        }

        /// <summary>
        /// Get the block at the given index
        /// </summary>
        /// <param name="originIndex">Origin index in the text</param>
        /// <returns>The block at the given <paramref name="originIndex"/></returns>
        public Block GetBlockAt(int originIndex)
        {
            return blocks[BlockIndexAt(blocks, originIndex)];
        }

        /// <summary>
        /// Find the tag with the given name
        /// </summary>
        /// <param name="blockIdx">Block index to check</param>
        /// <param name="tagName">Name of the tag</param>
        /// <param name="alt">Alternate name for the tag</param>
        /// <returns>The tag, or <see langword="null"/> if not found</returns>
        public OverrideTag? FindTag(int blockIdx, string tagName, string alt = "")
        {
            if (blockIdx >= blocks.Count)
                blockIdx = blocks.Count - 1;

            for (var b = blockIdx; b >= 0; b--)
            {
                if (blocks[b] is not OverrideBlock block)
                    continue;

                for (var t = block.Tags.Count - 1; t >= 0; t--)
                {
                    if (block.Tags[t].Name == tagName || block.Tags[t].Name == alt)
                        return block.Tags[t];
                }
            }
            return null;
        }

        /// <summary>
        /// Set the value of a tag
        /// </summary>
        /// <param name="tag">Tag to set</param>
        /// <param name="originPos">Original position</param>
        /// <exception cref="ArgumentOutOfRangeException">If the <see cref="BlockType"/> is invalid</exception>
        /// <returns>Number of characters to shift the caret</returns>
        public int SetTag(OverrideTag tag, int originPos)
        {
            var start = BlockIndexAt(blocks, originPos);
            OverrideBlock? ovr = null;
            PlainBlock? plainBlock = null;
            var insertIdx = -1;

            for (var i = start; i >= 0; i--)
            {
                switch (blocks[i].Type)
                {
                    case BlockType.Comment:
                    case BlockType.Drawing:
                        continue;
                    case BlockType.Override:
                        ovr = blocks[i] as OverrideBlock;
                        goto found;
                    case BlockType.Plain:
                        plainBlock = blocks[i] as PlainBlock;
                        insertIdx = originPos;
                        start = i;
                        goto found;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
                }
            }

            found:
            if (insertIdx < 0 && ovr is null)
                insertIdx = 0;

            var shift = tag.ToString().Length;

            // Modify existing OverrideBlock
            if (ovr is not null)
            {
                var alt = tag.Name switch
                {
                    OverrideTags.C => OverrideTags.C1,
                    OverrideTags.C1 => OverrideTags.C,
                    OverrideTags.Fr => OverrideTags.FrZ,
                    OverrideTags.FrZ => OverrideTags.Fr,
                    _ => string.Empty,
                };
                var foundTag = false;

                for (var i = ovr.Tags.Count - 1; i >= 0; i--)
                {
                    var name = ovr.Tags[i].Name;
                    if (name != tag.Name && name != alt)
                        continue;

                    shift -= ovr.Tags[i].ToString().Length;

                    if (!foundTag)
                    {
                        ovr.Tags[i] = tag;
                        foundTag = true;
                    }
                    else
                    {
                        ovr.Tags.RemoveAt(i);
                    }
                }

                if (!foundTag)
                    ovr.Tags.Add(tag);
            }
            // Add a new OverrideBlock
            else if (plainBlock is not null)
            {
                ovr = new OverrideBlock(Span<char>.Empty);
                ovr.Tags.Add(tag);

                // Split the plain block
                var normBlockIdx = NormalizeIndex(blocks, start, insertIdx);
                var left = new PlainBlock(plainBlock.Text[..normBlockIdx]);
                var right = new PlainBlock(plainBlock.Text[normBlockIdx..]);

                blocks.RemoveAt(start);
                blocks.InsertRange(start, [left, ovr, right]);
                shift += 2; // Account for {}
            }
            return shift;
        }
    }

    /// <summary>
    /// Extensions for a read-only list of <see cref="Block"/>s
    /// </summary>
    /// <param name="blocks">List of <see cref="Block"/>s</param>
    extension(IReadOnlyList<Block> blocks)
    {
        /// <summary>
        /// Normalize indexes relative to plain text in the event,
        /// excluding Override, Comment, and Drawing blocks
        /// </summary>
        /// <remarks>
        /// Given <c>Hello {\b1}World{\b0}!</c>, origin index 13 ("r"),
        /// normalized index 8 will be returned.
        /// </remarks>
        /// <param name="originIndex">Original index</param>
        /// <returns>Normalized index</returns>
        public int NormalizeIndex(int originIndex)
        {
            var remaining = originIndex;
            var plainLength = 0;

            for (var i = 0; i < blocks.Count && remaining > 0; i++)
            {
                var block = blocks[i];
                var blockLength = block.Text.Length;

                if (block is OverrideBlock or CommentBlock)
                {
                    remaining -= blockLength;
                    continue;
                }

                var consumed = Math.Min(blockLength, remaining);
                plainLength += consumed;
                remaining -= consumed;
            }

            return plainLength;
        }

        /// <summary>
        /// Normalize indexes relative to the given block
        /// </summary>
        /// <remarks>
        /// Given <c>Hello {\b1}World{\b0}!</c>, origin index 13 ("r"),
        /// normalized index 2 will be returned.
        /// </remarks>
        /// <param name="plainBlockIdx">Plain text block to normalize to</param>
        /// <param name="originIndex">Original index</param>
        /// <returns>Normalized index</returns>
        public int NormalizeIndex(int plainBlockIdx, int originIndex)
        {
            var remaining = originIndex;

            for (var i = 0; i < blocks.Count && remaining > 0; i++)
            {
                var block = blocks[i];
                var blockLength = block.Text.Length;

                if (block is OverrideBlock or CommentBlock)
                {
                    remaining -= blockLength;
                    continue;
                }

                var consumed = Math.Min(blockLength, remaining);

                // If this is the block we're targeting, return index within it
                if (i == plainBlockIdx)
                    return consumed;

                remaining -= consumed;
            }

            return remaining;
        }

        /// <summary>
        /// Get the index of the block at the given index in the text
        /// </summary>
        /// <param name="originIndex">Origin index in the text</param>
        /// <returns>Index of the block at the given <paramref name="originIndex"/></returns>
        public int BlockIndexAt(int originIndex)
        {
            var remaining = originIndex;
            var blockCount = blocks.Count;

            var i = 0;
            for (; i < blockCount; i++)
            {
                var block = blocks[i];
                var hasNext = i + 1 < blockCount;

                remaining -= block.Text.Length;
                switch (remaining)
                {
                    case < 0:
                        return i;
                    case 0:
                        return i + (hasNext && IsBraced(blocks[i + 1]) ? 1 : 0);
                }
            }

            return i;

            bool IsBraced(Block block) => block is OverrideBlock or CommentBlock;
        }

        /// <summary>
        /// Get the block at the given index
        /// </summary>
        /// <param name="originIndex">Origin index in the text</param>
        /// <returns>The block at the given <paramref name="originIndex"/></returns>
        public Block GetBlockAt(int originIndex)
        {
            return blocks[BlockIndexAt(blocks, originIndex)];
        }

        /// <summary>
        /// Find the tag with the given name
        /// </summary>
        /// <param name="blockIdx">Block index to check</param>
        /// <param name="tagName">Name of the tag</param>
        /// <param name="alt">Alternate name for the tag</param>
        /// <returns>The tag, or <see langword="null"/> if not found</returns>
        public OverrideTag? FindTag(int blockIdx, string tagName, string alt = "")
        {
            if (blockIdx >= blocks.Count)
                blockIdx = blocks.Count - 1;

            for (var b = blockIdx; b >= 0; b--)
            {
                if (blocks[b] is not OverrideBlock block)
                    continue;

                for (var t = block.Tags.Count - 1; t >= 0; t--)
                {
                    if (block.Tags[t].Name == tagName || block.Tags[t].Name == alt)
                        return block.Tags[t];
                }
            }

            return null;
        }
    }
}
