// SPDX-License-Identifier: MPL-2.0

using System.Text;
using AssCS.Overrides.Blocks;

namespace AssCS.Overrides;

/// <summary>
/// Representation of a syllable in a line of <see cref="Karaoke"/>
/// </summary>
public class Syllable
{
    /// <summary>
    /// Karaoke tag representing the syllable
    /// </summary>
    public OverrideTag.K Tag { get; set; }

    /// <summary>
    /// Passthrough for <see cref="Tag"/>'s duration in centiseconds
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
    public static Syllable FromTagName(string tagName)
    {
        return new Syllable(
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
    public Syllable(OverrideTag.K kTag)
    {
        Tag = kTag;
        Tags = [];
    }

    /// <summary>
    /// Construct a Syllable
    /// </summary>
    /// <param name="kTag">Karaoke tag</param>
    /// <param name="overrideTags">Override tags attached to the syllable</param>
    public Syllable(OverrideTag.K kTag, IEnumerable<OverrideTag> overrideTags)
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
