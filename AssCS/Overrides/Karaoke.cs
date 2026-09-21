// SPDX-License-Identifier: MPL-2.0

using AssCS.Overrides.Blocks;

namespace AssCS.Overrides;

/// <summary>
/// Provides methods for working with karaoke <see cref="Syllable"/>s
/// </summary>
/// <remarks>
/// Methods work on internal copy of event contents —
/// <paramref name="event"/>'s text is not updated unless/until <see cref="CommitSyllables"/> is called
/// </remarks>
public class Karaoke(Event @event)
{
    private int? _hash;
    private readonly List<Syllable> _syllables = [];
    private readonly List<Block> _blocks = [];

    /// <summary>
    /// List of syllables in the line
    /// </summary>
    public IReadOnlyList<Syllable> Syllables
    {
        get
        {
            EnsureUpToDate();
            return _syllables;
        }
    }

    /// <summary>
    /// Set the <see cref="Syllables"/>
    /// </summary>
    /// <param name="syllables">The syllables to set</param>
    /// <remarks>Side effect: updates <see cref="Event.Text"/></remarks>
    public void SetSyllables(IEnumerable<Syllable> syllables)
    {
        _syllables.Clear();
        _syllables.AddRange(syllables);
        CommitSyllables();
    }

    /// <summary>
    /// Update <see cref="Event.Text"/> with the current <see cref="Syllables"/>
    /// </summary>
    public void CommitSyllables()
    {
        @event.Text = string.Join(string.Empty, _syllables.Select(s => s.Text));
        _hash = @event.Text.GetHashCode();
        RepopulateBlocksAndSyllables();
    }

    /// <summary>
    /// Normalize the duration of syllables according to syllable text length
    /// </summary>
    public void Normalize()
    {
        EnsureUpToDate();
        var charCount = _syllables.Sum(s => s.InnerText.Length);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        foreach (var syl in _syllables)
        {
            syl.Duration = (long)(syl.InnerText.Length / (double)charCount * duration);
        }
    }

    /// <summary>
    /// Distribute syllables evenly across the event
    /// </summary>
    public void Distribute()
    {
        EnsureUpToDate();
        var duration = (@event.End - @event.Start).TotalCentiseconds / _syllables.Count;
        foreach (var syl in _syllables)
        {
            syl.Duration = duration;
        }
    }

    /// <summary>
    /// Automatically split the event into syllables
    /// </summary>
    /// <remarks>Removes all existing syllables</remarks>
    public void AutoSplit()
    {
        EnsureUpToDate();

        // Remove existing syllables
        _syllables.Clear();
        foreach (var block in _blocks.OfType<OverrideBlock>())
        {
            block.SetTags(block.Tags.Where(t => t is not OverrideTag.K));
        }

        _syllables.AddRange(ParseSyllables(_blocks)); // Generate initial syl

        int pos;
        while ((pos = _syllables.Last().InnerText.IndexOf(' ')) != -1)
        {
            AddSplit(_syllables.Count - 1, pos + 1);
        }
    }

    /// <summary>
    /// Split a syllable
    /// </summary>
    /// <param name="index">Index of the syllable to split</param>
    /// <param name="position">Position relative to <see cref="Syllable.InnerText"/> to split at</param>
    public void AddSplit(int index, int position)
    {
        EnsureUpToDate();
        if (index >= _syllables.Count || index < 0)
            return;
        var preSyl = _syllables[index];

        if (position > preSyl.InnerText.Length || position < 0)
            return;

        // Find the block-level index
        var blockIndex = 0;
        Block? plainBlock = null;
        var remaining = position;
        while (blockIndex < preSyl.Blocks.Count)
        {
            var block = preSyl.Blocks[blockIndex++];
            if (block is not PlainBlock)
                continue;

            if (remaining > block.Text.Length)
            {
                remaining -= block.Text.Length;
                continue;
            }

            plainBlock = block;
            break;
        }

        if (plainBlock is null)
            return;
        remaining = Math.Min(remaining, plainBlock.Text.Length);

        // Create syl and split text between preSyl and newSyl
        var newSyl = Syllable.FromTagName(preSyl.Tag.Name);
        var newText = plainBlock.Text[remaining..];
        if (!string.IsNullOrEmpty(newText))
            newSyl.Blocks.Add(new PlainBlock(newText));
        plainBlock.Text = plainBlock.Text[..remaining];

        // Move subsequent blocks to newSyl and truncate oldSyl
        var blocks = preSyl.Blocks.ToList();
        newSyl.Blocks.AddRange(blocks[blockIndex..]);
        preSyl.Blocks.Clear();
        preSyl.Blocks.AddRange(blocks[..blockIndex]);

        // Timing
        if (preSyl.Duration == 0 || string.IsNullOrWhiteSpace(newSyl.InnerText))
        {
            newSyl.Duration = 0;
        }
        else if (string.IsNullOrWhiteSpace(preSyl.InnerText))
        {
            newSyl.Duration = preSyl.Duration;
            preSyl.Duration = 0;
        }
        else
        {
            newSyl.Duration =
                (preSyl.Duration * newSyl.InnerText.Length)
                / (preSyl.InnerText.Length + newSyl.InnerText.Length);
            preSyl.Duration -= newSyl.Duration;
        }

        if (preSyl.Duration < 0)
            return;

        _syllables.Insert(index + 1, newSyl);
    }

    /// <summary>
    /// Remove a syllable split
    /// </summary>
    /// <param name="index">Index of the syllable to combine</param>
    /// <remarks>First syllable cannot be removed</remarks>
    public void RemoveSplit(int index)
    {
        EnsureUpToDate();
        if (index <= 0 || index >= _syllables.Count)
            return;

        var syl = _syllables[index];
        var pre = _syllables[index - 1];

        pre.Duration += syl.Duration;

        // Inject override block if needed
        if (syl.Tags.Count > 0)
        {
            pre.Blocks.Add(new OverrideBlock(syl.Tags));
        }

        // Move the blocks over
        pre.Blocks.AddRange(syl.Blocks);

        _syllables.RemoveAt(index);
    }

    /// <summary>
    /// Ensure local state is up-to-date
    /// </summary>
    private void EnsureUpToDate()
    {
        if (@event.Text.GetHashCode() == _hash)
            return;
        RepopulateBlocksAndSyllables();
    }

    /// <summary>
    /// Clear and re-populate the blocks and syllables lists
    /// </summary>
    private void RepopulateBlocksAndSyllables()
    {
        _hash = @event.Text.GetHashCode();
        _blocks.Clear();
        _syllables.Clear();
        _blocks.AddRange(@event.ParseBlocks());
        _syllables.AddRange(ParseSyllables(_blocks));
    }

    /// <summary>
    /// Parses the event into syllables
    /// </summary>
    /// <returns>List of syllable objects</returns>
    private static List<Syllable> ParseSyllables(IEnumerable<Block> blocks)
    {
        List<Syllable> syllables = [];

        var syl = new Syllable(new OverrideTag.K(0));
        foreach (var block in blocks)
        {
            switch (block.Type)
            {
                case BlockType.Plain:
                case BlockType.Comment:
                case BlockType.Drawing:
                    syl.Blocks.Add(block);
                    break;
                case BlockType.Override:
                    var ob = (OverrideBlock)block;
                    if (!ob.Tags.OfType<OverrideTag.K>().Any())
                    {
                        syl.Blocks.Add(block);
                        break;
                    }

                    // New syllable!
                    if (!syl.IsEmpty())
                        syllables.Add(syl);
                    syl = new Syllable(
                        kTag: ob.Tags.OfType<OverrideTag.K>().Last(),
                        overrideTags: ob.Tags.Where(t => t is not OverrideTag.K)
                    );
                    break;
                default:
                    syl.Blocks.Add(block);
                    break;
            }
        }

        syllables.Add(syl);
        return syllables;
    }
}
