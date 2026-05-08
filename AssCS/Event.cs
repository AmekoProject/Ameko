// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using AssCS.Overrides;
using AssCS.Overrides.Blocks;
using AssCS.Utilities;

namespace AssCS;

/// <summary>
/// An event in a subtitle document
/// </summary>
/// <param name="id">ID of the event</param>
public partial class Event(int id) : BindableBase, IEntry
{
    private const string Dialogue = "Dialogue:";
    private const string Comment = "Comment:";

    private const string CodeNewlineLua = @"--[[\N]]";
    private const string CodeNewlineCSharp = @"/*\N*/";
    private static readonly string[] CodeNewlines = [CodeNewlineLua, CodeNewlineCSharp];

    private bool _isComment;
    private int _layer = Options.DefaultLayer;
    private Time _start = Time.FromSeconds(0);
    private Time _end = Time.FromSeconds(5);
    private string _style = "Default";
    private string _actor = string.Empty;
    private Margins _margins = new(0, 0, 0);
    private string _effect = string.Empty;
    private string _text = string.Empty;

    /// <summary>
    /// Event ID
    /// </summary>
    public int Id => id;

    /// <summary>
    /// If the event is a comment
    /// </summary>
    public bool IsComment
    {
        get => _isComment;
        set => SetProperty(ref _isComment, value);
    }

    /// <summary>
    /// Z-Layer of the event
    /// </summary>
    public int Layer
    {
        get => _layer;
        set => SetProperty(ref _layer, value);
    }

    /// <summary>
    /// Time the event appears on the screen
    /// </summary>
    public Time Start
    {
        get => _start;
        set => SetProperty(ref _start, value);
    }

    /// <summary>
    /// Time the event ends at
    /// </summary>
    public Time End
    {
        get => _end;
        set => SetProperty(ref _end, value);
    }

    /// <summary>
    /// Name of the style applied to this event
    /// </summary>
    [NotNull]
    public string? Style
    {
        get => _style;
        set
        {
            if (value is not null)
                SetProperty(ref _style, value);
        }
    }

    /// <summary>
    /// Name of the event's actor
    /// </summary>
    public string Actor
    {
        get => _actor;
        set => SetProperty(ref _actor, value);
    }

    /// <summary>
    /// Margins
    /// </summary>
    public Margins Margins
    {
        get => _margins;
        set => SetProperty(ref _margins, value);
    }

    /// <summary>
    /// Effect data
    /// </summary>
    public string Effect
    {
        get => _effect;
        set => SetProperty(ref _effect, value);
    }

    /// <summary>
    /// Text content of the event
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            SetProperty(ref _text, value);
            RaisePropertyChanged(nameof(Cps));
            RaisePropertyChanged(nameof(MaxLineWidth));
        }
    }

    /// <summary>
    /// Row number in the document
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// IDs of extradata linked to this event
    /// </summary>
    public List<int> LinkedExtradatas { get; set; } = [];

    /// <summary>
    /// Characters per second
    /// </summary>
    /// <remarks>It is recommended to keep dialogue events below 18 CPS.</remarks>
    public double Cps
    {
        get
        {
            var secs = (End - Start).TotalSeconds;
            if (secs == 0)
                return 0;

            var input = NewlineHardSpaceRegex().Replace(GetStrippedText(), string.Empty);
            if (string.IsNullOrWhiteSpace(input))
                return 0;
            var charCount = 0;

            var tee = StringInfo.GetTextElementEnumerator(input);
            while (tee.MoveNext())
            {
                var element = tee.GetTextElement();
                if (true)
                    if (char.IsWhiteSpace(element[0]))
                        continue;
                if (true)
                    if (char.IsPunctuation(element[0]))
                        continue;
                charCount++;
            }
            return Math.Round(charCount / secs);
        }
    }

    /// <summary>
    /// Length (in characters) of the longest line in the event
    /// </summary>
    public int MaxLineWidth
    {
        get
        {
            var lines = GetStrippedText().Split(["\\N", "\\n"], StringSplitOptions.None);
            return lines
                .Select(line =>
                {
                    var input = NewlineHardSpaceRegex().Replace(line, string.Empty);
                    if (string.IsNullOrWhiteSpace(input))
                        return 0;
                    var charCount = 0;

                    var tee = StringInfo.GetTextElementEnumerator(input);
                    while (tee.MoveNext())
                    {
                        var element = tee.GetTextElement();
                        if (Options.LineWidthIncludesWhitespace)
                            if (char.IsWhiteSpace(element[0]))
                                continue;
                        if (Options.LineWidthIncludesPunctuation)
                            if (char.IsPunctuation(element[0]))
                                continue;
                        charCount++;
                    }
                    return charCount;
                })
                .Max();
        }
    }

    /// <summary>
    /// Make inline Lua code snippets ass-compliant
    /// by encoding newlines and indentation to comments
    /// </summary>
    /// <returns>Ass-compliant Lua code</returns>
    /// <remarks>This function reverses <see cref="TransformAssToCode"/></remarks>
    public string TransformCodeToAss()
    {
        return DetectCodeLanguage(Text) switch
        {
            CodeLanguage.Lua => Text.ReplaceLineEndings(CodeNewlineLua),
            CodeLanguage.CSharp => Text.ReplaceLineEndings(CodeNewlineCSharp),
            _ => Text.ReplaceLineEndings(CodeNewlineLua),
        };
    }

    /// <summary>
    /// Make inline Lua code snippets display
    /// across multiple lines by decoding
    /// newlines and indentation from comments
    /// </summary>
    /// <returns>Formatted Lua code</returns>
    /// <remarks>This function reverses <see cref="TransformCodeToAss"/></remarks>
    public string TransformAssToCode()
    {
        return Text.ReplaceMany(CodeNewlines, Environment.NewLine);
    }

    /// <summary>
    /// Check if this event collides with another event.
    /// Events collide if their timestamps overlap.
    /// </summary>
    /// <param name="other">Event to check against</param>
    /// <returns><see langword="true"/> if the events collide</returns>
    public bool CollidesWith(Event other)
    {
        return (Start < other.Start) ? (other.Start < End) : (Start < other.End);
    }

    /// <summary>
    /// Set fields in this event to the fields from another event
    /// </summary>
    /// <param name="fields">Fields to set</param>
    /// <param name="other">Source of new fields</param>
    public void SetFields(EventField fields, Event other)
    {
        foreach (EventField field in fields.GetSetFlags())
        {
            switch (field)
            {
                case EventField.Comment:
                    IsComment = other.IsComment;
                    break;
                case EventField.Layer:
                    Layer = other.Layer;
                    break;
                case EventField.StartTime:
                    Start = Time.FromTime(other.Start);
                    break;
                case EventField.EndTime:
                    End = Time.FromTime(other.End);
                    break;
                case EventField.Style:
                    Style = other.Style;
                    break;
                case EventField.Actor:
                    Actor = other.Actor;
                    break;
                case EventField.MarginLeft:
                    Margins.Left = other.Margins.Left;
                    break;
                case EventField.MarginRight:
                    Margins.Right = other.Margins.Right;
                    break;
                case EventField.MarginVertical:
                    Margins.Vertical = other.Margins.Vertical;
                    break;
                case EventField.MarginTop:
                    Margins.Top = other.Margins.Top;
                    break;
                case EventField.MarginBottom:
                    Margins.Bottom = other.Margins.Bottom;
                    break;
                case EventField.Effect:
                    Effect = other.Effect;
                    break;
                case EventField.Text:
                    Text = other.Text;
                    break;
                case EventField.None:
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Compare two events' fields
    /// </summary>
    /// <param name="other">Event to compare with</param>
    /// <returns>Differing fields</returns>
    public EventField Diff(Event other)
    {
        var result = EventField.None;

        if (IsComment != other.IsComment)
            result |= EventField.Comment;
        if (Layer != other.Layer)
            result |= EventField.Layer;
        if (Start != other.Start)
            result |= EventField.StartTime;
        if (End != other.End)
            result |= EventField.EndTime;
        if (Style != other.Style)
            result |= EventField.Style;
        if (Actor != other.Actor)
            result |= EventField.Actor;
        if (Margins.Left != other.Margins.Left)
            result |= EventField.MarginLeft;
        if (Margins.Right != other.Margins.Right)
            result |= EventField.MarginRight;
        if (Margins.Vertical != other.Margins.Vertical)
            result |= EventField.MarginVertical;
        if (Margins.Top != other.Margins.Top)
            result |= EventField.MarginTop;
        if (Margins.Bottom != other.Margins.Bottom)
            result |= EventField.MarginBottom;
        if (Effect != other.Effect)
            result |= EventField.Effect;
        if (Text != other.Text)
            result |= EventField.Text;
        return result;
    }

    /// <summary>
    /// Get the ass representation of this event
    /// </summary>
    /// <param name="version">Event format version</param>
    /// <returns>Ass-formatted string</returns>
    public string AsAss(AssVersion version)
    {
        var extradatas =
            LinkedExtradatas.Count > 0 ? $"{{{string.Join("=", LinkedExtradatas)}}}" : "";
        var textContent = !Effect.StartsWith("code") ? Text : TransformCodeToAss();

        var margins = version switch
        {
            AssVersion.V400 or AssVersion.V400P => string.Create(
                CultureInfo.InvariantCulture,
                $"{_margins.Left},{_margins.Right},{_margins.Vertical}"
            ),
            AssVersion.V400PP => string.Create(
                CultureInfo.InvariantCulture,
                $"{_margins.Left},{_margins.Right},{_margins.Top},{_margins.Bottom}"
            ),
            _ => throw new FormatException("Unknown event version"),
        };
        var layer = version switch
        {
            AssVersion.V400 => $"Marked={Layer}",
            _ => Layer.ToString(),
        };

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(IsComment ? Comment : Dialogue)} {layer},{Start.AsAss()},{End.AsAss()},{Style},{Actor},"
                + $"{margins},{Effect},{extradatas}"
                + $"{textContent}"
        );
    }

    /// <summary>
    /// Initialize an event from an ass-formatted string
    /// </summary>
    /// <param name="id">ID of the event to create</param>
    /// <param name="data">Ass-formatted string</param>
    /// <param name="version">Event format version</param>
    /// <returns>Event object represented by the string</returns>
    public static Event? FromAss(int id, ReadOnlySpan<char> data, AssVersion version)
    {
        data = data.TrimStart();
        var isComment = false;
        if (data.StartsWith(Comment))
        {
            isComment = true;
            data = data[Comment.Length..].TrimStart();
        }
        else if (data.StartsWith(Dialogue))
        {
            data = data[Dialogue.Length..].TrimStart();
        }
        else
        {
            return null;
        }

        var result = new Event(id)
        {
            _isComment = isComment,
            Layer = version switch
            {
                AssVersion.V400 => ParseSkip(ref data, 0), // Marked=0
                _ => ParseInt(ref data),
            },
            Start = Time.FromAss(ParseString(ref data)),
            End = Time.FromAss(ParseString(ref data)),
            Style = ParseString(ref data),
            Actor = ParseString(ref data),
            Margins = version switch
            {
                AssVersion.V400 or AssVersion.V400P => new Margins(
                    left: ParseInt(ref data),
                    right: ParseInt(ref data),
                    vertical: ParseInt(ref data)
                ),
                AssVersion.V400PP => new Margins(
                    left: ParseInt(ref data),
                    right: ParseInt(ref data),
                    top: ParseInt(ref data),
                    bottom: ParseInt(ref data)
                ),
                _ => throw new FormatException("Unknown event format"),
            },
            Effect = ParseString(ref data),
            Text = data.ToString(),
        };
        if (result.Effect.StartsWith("code"))
            result.Text = result.TransformAssToCode();
        return result;
    }

    /// <summary>
    /// Initialize a congruent event
    /// </summary>
    /// <param name="id">ID of the event to create</param>
    /// <param name="e">Base event</param>
    /// <returns>Congruent event</returns>
    public static Event FromEvent(int id, Event e)
    {
        return new Event(id)
        {
            _isComment = e.IsComment,
            _layer = e.Layer,
            _start = Time.FromTime(e.Start),
            _end = Time.FromTime(e.End),
            _style = e.Style,
            _actor = e.Actor,
            _margins =
            {
                Left = e.Margins.Left,
                Right = e.Margins.Right,
                Vertical = e.Margins.Vertical,
                Top = e.Margins.Top,
                Bottom = e.Margins.Bottom,
            },
            _effect = e.Effect,
            _text = e.Text,
        };
    }

    /// <summary>
    /// Clone this event
    /// </summary>
    /// <returns>Clone of the event</returns>
    public Event Clone()
    {
        return new Event(Id)
        {
            IsComment = IsComment,
            Layer = Layer,
            Start = Time.FromTime(Start),
            End = Time.FromTime(End),
            Style = Style,
            Actor = Actor,
            Margins =
            {
                Left = Margins.Left,
                Right = Margins.Right,
                Vertical = Margins.Vertical,
                Top = Margins.Top,
                Bottom = Margins.Bottom,
            },
            Effect = Effect,
            Text = Text,
            LinkedExtradatas = [.. LinkedExtradatas],
        };
    }

    /// <summary>
    /// Clone this event with a new ID
    /// </summary>
    /// <param name="eventId">New ID</param>
    /// <returns>Clone of the event</returns>
    public Event Clone(int eventId)
    {
        return new Event(eventId)
        {
            IsComment = IsComment,
            Layer = Layer,
            Start = Time.FromTime(Start),
            End = Time.FromTime(End),
            Style = Style,
            Actor = Actor,
            Margins =
            {
                Left = Margins.Left,
                Right = Margins.Right,
                Vertical = Margins.Vertical,
                Top = Margins.Top,
                Bottom = Margins.Bottom,
            },
            Effect = Effect,
            Text = Text,
            LinkedExtradatas = [.. LinkedExtradatas],
        };
    }

    /// <summary>
    /// Validate if a given string is a valid Event
    /// </summary>
    /// <param name="data">Data to validate</param>
    /// <returns><see langword="true"/> if the <paramref name="data"/> is valid</returns>
    public static bool ValidateAssString(string data)
    {
        var span = data.AsSpan();
        span = span.TrimStart();
        return span.StartsWith(Comment) || span.StartsWith(Dialogue);
    }

    #region Tags n stuff

    /// <summary>
    /// Parses <see cref="Text"/> into a list of <see cref="Block"/> objects.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="Block"/> objects. To apply modifications, use <see cref="SetBlocks"/>.
    /// </returns>
    public IReadOnlyList<Block> ParseBlocks()
    {
        List<Block> blocks = [];
        if (_text.Length <= 0)
        {
            blocks.Add(new PlainBlock(string.Empty));
            return blocks;
        }

        var data = _text.AsSpan();
        var drawingLevel = 0;

        while (!data.IsEmpty)
        {
            var p = data[0];

            if (p is '{' && data.IndexOf('}') is var q and >= 0)
            {
                // Comment
                if (data[..q].IndexOf('\\') < 0)
                {
                    var block = new CommentBlock(data[1..q].ToString());
                    blocks.Add(block);
                    data = data[(q + 1)..];
                }
                // Override
                else
                {
                    var block = new OverrideBlock(data[..q]);
                    blocks.Add(block);
                    data = data[(q + 1)..];

                    // Search for drawings
                    foreach (var pTag in block.Tags.OfType<OverrideTag.P>())
                    {
                        drawingLevel = pTag.Level;
                    }
                }
            }
            else if (drawingLevel != 0)
            {
                q = (p == '{' ? data[1..] : data).IndexOf('{');
                if (q < 0)
                    q = data.Length;
                blocks.Add(new DrawingBlock(data[..q].ToString(), drawingLevel));
                data = data[q..];
            }
            else
            {
                q = data.IndexOf('{');
                if (q < 0 || data[q..].IndexOf('}') < 0)
                {
                    q = data.Length;
                }

                blocks.Add(new PlainBlock(data[..q].ToString()));
                data = data[q..];
            }
        }

        return blocks;
    }

    /// <summary>
    /// Sets <see cref="Text"/> by joining the <see cref="Block.Text"/> of each block in <paramref name="blocks"/>
    /// </summary>
    /// <param name="blocks">The blocks to set</param>
    /// <remarks>
    /// <paramref name="blocks"/> must contain at least one element, as an empty event is represented by a single
    /// <see cref="PlainBlock"/> with an empty <see cref="Block.Text"/>. An empty input is a no-op.
    /// </remarks>
    public void SetBlocks(IEnumerable<Block> blocks)
    {
        var list = blocks.ToList();
        if (list.Count == 0)
            return;
        Text = string.Join(string.Empty, list.Select(b => b.Text));
    }

    /// <summary>
    /// Get the event's text content without override tags,
    /// comments, or drawings
    /// </summary>
    /// <returns>Stripped text content</returns>
    public string GetStrippedText()
    {
        var blocks = ParseBlocks();
        return string.Join(string.Empty, blocks.OfType<PlainBlock>().Select(b => b.Text));
    }

    /// <summary>
    /// Strip away all tags in the event
    /// </summary>
    /// <seealso cref="GetStrippedText"/>
    public void StripTags()
    {
        Text = GetStrippedText();
    }

    /// <summary>
    /// Toggle a tag
    /// </summary>
    /// <param name="tag">Tag to toggle</param>
    /// <param name="style">Event style</param>
    /// <param name="selStart">Start of the selection</param>
    /// <param name="selEnd">End of the selection</param>
    /// <returns>Number of characters to shift the cursor</returns>
    public int ToggleTag(string tag, Style? style, int selStart, int selEnd)
    {
        if (selStart > selEnd)
            (selStart, selEnd) = (selEnd, selStart);

        var blocks = ParseBlocks().ToList();

        // Get style state
        var state =
            style is not null
            && tag switch
            {
                "b" => style.IsBold,
                "i" => style.IsItalic,
                "u" => style.IsUnderline,
                "s" => style.IsStrikethrough,
                _ => false,
            };

        // Update to use local state
        var blockN = blocks.BlockIndexAt(selStart);

        OverrideTag? startTag = null;
        OverrideTag? endTag = null;

        switch (tag)
        {
            case "b":
                state = (blocks.FindTag(blockN, tag) as OverrideTag.B)?.Value?.Equals(1) ?? state;
                startTag = new OverrideTag.B(state ? 0 : 1);
                endTag = new OverrideTag.B(state ? 1 : 0);
                break;
            case "i":
                state = (blocks.FindTag(blockN, tag) as OverrideTag.I)?.Value ?? state;
                startTag = new OverrideTag.I(!state);
                endTag = new OverrideTag.I(state);
                break;
            case "u":
                state = (blocks.FindTag(blockN, tag) as OverrideTag.U)?.Value ?? state;
                startTag = new OverrideTag.U(!state);
                endTag = new OverrideTag.U(state);
                break;
            case "s":
                state = (blocks.FindTag(blockN, tag) as OverrideTag.S)?.Value ?? state;
                startTag = new OverrideTag.S(!state);
                endTag = new OverrideTag.S(state);
                break;
        }

        if (startTag is null || endTag is null)
            return 0;

        var shift = blocks.SetTag(startTag, selStart);
        if (selStart != selEnd)
            blocks.SetTag(endTag, selEnd + shift);

        SetBlocks(blocks);
        return shift;
    }

    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Event @event && Id == @event.Id && IsCongruentWith(@event);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Checks equality of non-<see cref="Id"/> fields
    /// </summary>
    /// <param name="obj">Event to check</param>
    /// <returns><see langword="true"/> if,
    /// excluding the <see cref="Id"/>, <paramref name="obj"/> is equal.</returns>
    public bool IsCongruentWith(Event? obj)
    {
        return obj is { } @event
            && _isComment == @event._isComment
            && _layer == @event._layer
            && _start == @event._start
            && _end == @event._end
            && _style == @event._style
            && _actor == @event._actor
            && _margins == @event._margins
            && _effect == @event._effect
            && _text == @event._text;
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    /// <param name="left">left event</param>
    /// <param name="right">right event</param>
    /// <returns><see langword="true"/> if the events are equal</returns>
    public static bool operator ==(Event? left, Event? right)
    {
        return left?.Equals(right) ?? false;
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    /// <param name="left">left event</param>
    /// <param name="right">right event</param>
    /// <returns><see langword="true"/> if the events are not equal</returns>
    public static bool operator !=(Event? left, Event? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Try to detect what language a code block is written in using heuristics
    /// </summary>
    /// <param name="data">Text content</param>
    /// <returns>Presumed language</returns>
    private static CodeLanguage DetectCodeLanguage(ReadOnlySpan<char> data)
    {
        var csScore = 0;
        var luaScore = 0;

        // C#
        if (data.Contains("=>", StringComparison.Ordinal))
            csScore += 5;
        if (data.Contains("foreach", StringComparison.Ordinal))
            csScore += 5;
        if (data.Contains("null", StringComparison.Ordinal))
            csScore += 3;
        if (data.Contains("var ", StringComparison.Ordinal))
            csScore += 2;
        if (data.Contains("int", StringComparison.Ordinal))
            csScore++;
        if (data.Contains("bool", StringComparison.Ordinal))
            csScore++;

        // Lua
        if (data.Contains("local ", StringComparison.Ordinal))
            luaScore += 5;
        if (data.Contains("end", StringComparison.Ordinal))
            luaScore += 4;
        if (data.Contains("nil", StringComparison.Ordinal))
            luaScore += 3;
        if (data.Contains("function", StringComparison.Ordinal))
            luaScore += 3;
        if (data.Contains("elseif", StringComparison.Ordinal))
            luaScore++;
        if (data.Contains(".#", StringComparison.Ordinal))
            luaScore++;

        if (csScore > luaScore)
            return CodeLanguage.CSharp;
        if (luaScore > csScore)
            return CodeLanguage.Lua;
        return CodeLanguage.Unknown;
    }

    #region Parsing Helpers

    /// <summary>
    /// Parse the event's extradata
    /// </summary>
    /// <param name="data">Ass-formatted string</param>
    /// <returns>List of extradata IDs</returns>
    private static List<int> ParseExtradata(string data)
    {
        if (data.Length < 2 || !data.StartsWith("{="))
            return [];
        var match = ExtradataRegex().Match(data);
        if (!match.Success)
            return [];

        List<int> result = [];

        for (var i = 1; i < match.Groups.Count; i++)
        {
            var rawId = match.Groups[i].Value[1..]; // =123 → 123
            var id = Convert.ToInt32(rawId);
            result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// Skip parsing, return a generic value
    /// </summary>
    /// <param name="data">Incoming data</param>
    /// <param name="result">Value to return</param>
    /// <typeparam name="T">Type of value to return</typeparam>
    /// <remarks>Having <paramref name="result"/> allows for use in <see langword="switch"/> expressions</remarks>
    private static T? ParseSkip<T>(ref ReadOnlySpan<char> data, T? result)
    {
        var q = data.IndexOf(',');
        if (q < 0)
            q = data.Length;
        data = data[(q < data.Length ? q + 1 : q)..];
        return result;
    }

    /// <summary>
    /// Parse an integer
    /// </summary>
    /// <param name="data">Incoming data</param>
    /// <returns>Resulting integer</returns>
    private static int ParseInt(ref ReadOnlySpan<char> data)
    {
        data = data.TrimStart();
        var q = data.IndexOf(',');
        if (q < 0)
            q = data.Length;
        var result = data[..q].ToString().ParseAssInt();
        data = data[(q < data.Length ? q + 1 : q)..];
        return result;
    }

    /// <summary>
    /// Parse a string
    /// </summary>
    /// <param name="data">incoming data</param>
    /// <returns>Resulting string</returns>
    private static string ParseString(ref ReadOnlySpan<char> data)
    {
        data = data.TrimStart();
        var q = data.IndexOf(',');
        if (q < 0)
            q = data.Length;
        var result = data[..q].ToString();
        data = data[(q < data.Length ? q + 1 : q)..];
        return result;
    }

    #endregion Parsing Helpers

    [GeneratedRegex(@"^\{(=\d+)+\}")]
    private static partial Regex ExtradataRegex();

    [GeneratedRegex(@"\\[Nnh]")]
    private static partial Regex NewlineHardSpaceRegex();

    private enum CodeLanguage
    {
        Lua,
        CSharp,
        Unknown,
    }
}
