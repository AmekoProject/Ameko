// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;
using System.Text;
using AssCS.Overrides.Blocks;

namespace AssCS.Overrides;

/// <summary>
/// Extensions on <see cref="Event"/>
/// </summary>
public static class KaraokeExtensions
{
    private static readonly ConditionalWeakTable<Event, Karaoke2> _cache = new();

    extension(Event @event)
    {
        /// <summary>
        /// Karaoke API
        /// </summary>
        public Karaoke2 Karaoke
        {
            get
            {
                if (_cache.TryGetValue(@event, out var karaoke))
                    return karaoke;

                karaoke = new Karaoke2(@event);
                _cache.Add(@event, karaoke);
                return karaoke;
            }
        }
    }
}

/// <summary>
/// Provides methods for working with karaoke <see cref="Syllable2"/>
/// </summary>
public class Karaoke2(Event @event)
{
    private int? _hash;
    private readonly List<Syllable2> _syllables = [];

    /// <summary>
    /// List of syllables in the line
    /// </summary>
    public IReadOnlyList<Syllable2> Syllables
    {
        get
        {
            var currentHash = @event.Text.GetHashCode();
            if (currentHash == _hash)
                return _syllables;
            return ParseSyllables();
        }
    }

    /// <summary>
    /// Parses the event into syllables
    /// </summary>
    /// <returns>List of syllable objects</returns>
    public IReadOnlyList<Syllable2> ParseSyllables()
    {
        _hash = @event.Text.GetHashCode();
        List<Syllable2> syllables = [];

        var syl = new Syllable2(new OverrideTag.K(0));
        foreach (var block in @event.ParseBlocks())
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
                    syl = new Syllable2(
                        kTag: ob.Tags.OfType<OverrideTag.K>().Last(),
                        overrideTags: ob.Tags.Where(t => t is not OverrideTag.K)
                    );
                    break;
            }
        }

        syllables.Add(syl);

        _syllables.Clear();
        _syllables.AddRange(syllables);
        return syllables;
    }

    /// <summary>
    /// Sets <see cref="Event.Text"/> by joining the <see cref="Syllable2.Text"/> of each syllable in <param name="syllables"></param>
    /// </summary>
    /// <param name="syllables">The syllables to set</param>
    public void SetSyllables(IEnumerable<Syllable2> syllables)
    {
        var list = syllables.ToList();
        @event.Text = string.Join(string.Empty, list.Select(s => s.Text));

        _hash = @event.Text.GetHashCode();
        ParseSyllables();
    }

    /// <summary>
    /// Normalize the duration of syllables
    /// </summary>
    public void Normalize()
    {
        if (_syllables.Count == 0)
            ParseSyllables();

        var durationCs = (@event.End - @event.Start).TotalCentiseconds / _syllables.Count;
        foreach (var syl in _syllables)
        {
            syl.Duration = durationCs;
        }
    }

    /// <summary>
    /// Automatically split the event into syllables
    /// </summary>
    public IReadOnlyList<Syllable2> AutoSplit()
    {
        if (string.IsNullOrWhiteSpace(@event.Text))
            return [];

        List<Syllable2> syllables = [];
        var syl = new Syllable2(new OverrideTag.K(0));

        var blocks = @event.ParseBlocks();
        foreach (var block in @event.ParseBlocks()) { }

        syllables.Add(syl);

        _syllables.Clear();
        _syllables.AddRange(syllables);
        return syllables;
    }

    /// <summary>
    /// Split a syllable
    /// </summary>
    /// <param name="index">Index of the syllable to split</param>
    /// <param name="position">Position relative to <see cref="Syllable2.InnerText"/> to split at</param>
    public void Split(int index, int position)
    {
        if (_syllables.Count == 0)
            ParseSyllables();

        if (index >= _syllables.Count || index < 0)
            return;
        var preSyl = _syllables[index];

        if (position > preSyl.InnerText.Length || position < 0)
            return;

        // Find the block-level index
        var remaining = position;
        Block? plainBlock = null;
        foreach (var block in preSyl.Blocks.OfType<PlainBlock>())
        {
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

        var newSyl = Syllable2.FromTagName(preSyl.Tag.Name);
        newSyl.Blocks.Add(new PlainBlock(plainBlock.Text[remaining..]));
        plainBlock.Text = plainBlock.Text[..remaining];

        if (string.IsNullOrWhiteSpace(newSyl.InnerText))
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
        if (_syllables.Count == 0)
            ParseSyllables();

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
}

/// <summary>
/// Representation of a syllable in a line of <see cref="Karaoke2"/>
/// </summary>
public class Syllable2
{
    /// <summary>
    /// Karaoke tag representing the syllable
    /// </summary>
    public OverrideTag.K Tag { get; set; }

    /// <summary>
    /// Passthrough for <see cref="Tag"/>'s duration
    /// </summary>
    /// <seealso cref="OverrideTag.K.Duration"/>
    public long Duration
    {
        get => (long)(Tag.Duration ?? 0);
        set => Tag.Duration = value;
    }

    /// <summary>
    /// Override tags attached directly to the <see cref="Tag"/>
    /// </summary>
    public List<OverrideTag> Tags { get; }

    /// <summary>
    /// Blocks included in the syllable
    /// </summary>
    public List<Block> Blocks { get; } = [];

    /// <summary>
    /// Text representation of the syllable and its contents
    /// </summary>
    /// <example><c>{\k100\b1}Hel{\b0}lo</c></example>
    public string Text
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append(Tag);
            foreach (var tag in Tags)
                sb.Append(tag);
            sb.Append('}');
            foreach (var block in Blocks)
                sb.Append(block);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Inner (stripped) text
    /// </summary>
    /// <example><c>Hello</c></example>
    public string InnerText =>
        string.Join(string.Empty, Blocks.OfType<PlainBlock>().Select(b => b.Text));

    /// <summary>
    /// Construct an empty syllable
    /// </summary>
    /// <param name="tagName">Name of the tag to create</param>
    public static Syllable2 FromTagName(string tagName)
    {
        return new Syllable2(
            tagName switch
            {
                OverrideTags.K => new OverrideTag.K(0),
                OverrideTags.Kf => new OverrideTag.Kf(0),
                OverrideTags.Ko => new OverrideTag.Ko(0),
                OverrideTags.Kt => new OverrideTag.Kt(0),
                _ => throw new ArgumentException("Invalid K-tag name", nameof(tagName)),
            }
        );
    }

    /// <summary>
    /// Construct a Syllable
    /// </summary>
    /// <param name="kTag">Karaoke tag</param>
    public Syllable2(OverrideTag.K kTag)
    {
        Tag = kTag;
        Tags = [];
    }

    /// <summary>
    /// Construct a Syllable
    /// </summary>
    /// <param name="kTag">Karaoke tag</param>
    /// <param name="overrideTags">Override tags attached to the syllable</param>
    public Syllable2(OverrideTag.K kTag, IEnumerable<OverrideTag> overrideTags)
    {
        Tag = kTag;
        Tags = overrideTags.ToList();
    }

    internal bool IsEmpty()
    {
        return Duration == 0 && Tags.Count == 0 && Blocks.Count == 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Text;
    }
}
