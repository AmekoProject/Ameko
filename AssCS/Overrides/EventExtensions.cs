// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;
using System.Text;
using AssCS.Overrides.Blocks;

namespace AssCS.Overrides;

/// <summary>
/// Extensions on <see cref="Event"/>
/// </summary>
public static class EventExtensions
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
    /// <summary>
    /// List of syllables in the line
    /// </summary>
    /// <remarks>Triggers tag parsing - cache results when possible!</remarks>
    IReadOnlyList<Syllable2> Syllables
    {
        get
        {
            List<Syllable2> syllables = [];

            var syl = new Syllable2(
                startTime: Time.FromTime(@event.Start),
                kTag: new OverrideTag.K(0)
            );
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
                        }

                        // New syllable!
                        syllables.Add(syl);
                        syl = new Syllable2(
                            startTime: syl.Start + Time.FromMillis((long)(syl.Tag.Duration ?? 0)),
                            kTag: ob.Tags.OfType<OverrideTag.K>().Last(),
                            overrideTags: ob.Tags.Where(t => t is not OverrideTag.K)
                        );
                        break;
                }
            }

            syllables.Add(syl);
            return syllables;
        }
    }
}

/// <summary>
/// Representation of a syllable in a line of <see cref="Karaoke2"/>
/// </summary>
public class Syllable2
{
    /// <summary>
    /// Absolute start time of the syllable
    /// </summary>
    /// <remarks>
    /// Relative to <c>t=0</c>, not the start time of the containing <see cref="Event"/>
    /// </remarks>
    public Time Start { get; set; }

    /// <summary>
    /// Karaoke tag representing the syllable
    /// </summary>
    public OverrideTag.K Tag { get; set; }

    /// <summary>
    /// Override tags attached directly to the <see cref="Tag"/>
    /// </summary>
    public List<OverrideTag> OverrideTags { get; }

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
            foreach (var tag in OverrideTags)
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
    /// Construct a Syllable
    /// </summary>
    /// <param name="startTime">Absolute time the syllable starts at</param>
    /// <param name="kTag">Karaoke tag</param>
    public Syllable2(Time startTime, OverrideTag.K kTag)
    {
        Start = startTime;
        Tag = kTag;
        OverrideTags = [];
    }

    /// <summary>
    /// Construct a Syllable
    /// </summary>
    /// <param name="startTime">Absolute time the syllable starts at</param>
    /// <param name="kTag">Karaoke tag</param>
    /// <param name="overrideTags">Override tags attached to the syllable</param>
    public Syllable2(Time startTime, OverrideTag.K kTag, IEnumerable<OverrideTag> overrideTags)
    {
        Start = startTime;
        Tag = kTag;
        OverrideTags = overrideTags.ToList();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Text;
    }
}
