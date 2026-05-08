// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;

namespace AssCS.Overrides.Blocks;

/// <summary>
/// An override block
/// </summary>
/// <remarks>
/// An override block is composed of a set
/// of override brackets <c>{ }</c> containing
/// one or more override tags
/// </remarks>
public class OverrideBlock(ReadOnlySpan<char> data) : Block(data.ToString(), BlockType.Override)
{
    private readonly string _data = data.ToString();

    /// <summary>
    /// Gets the list of <see cref="OverrideTag"/> objects parsed from this block.
    /// </summary>
    /// <seealso cref="SetTags"/>
    public List<OverrideTag> Tags
    {
        get => field ??= ParseTags(_data.AsSpan());
        private set;
    }

    /// <summary>
    /// Replace the <see cref="Tags"/> of this block with new <paramref name="tags"/>
    /// </summary>
    /// <param name="tags">The tags to set</param>
    public void SetTags(IEnumerable<OverrideTag> tags)
    {
        Tags = tags.ToList();
    }

    /// <summary>
    /// Text content of the block, including { }
    /// </summary>
    public override string Text
    {
        get
        {
            _text = $"{{{string.Join("", Tags.Select(t => t.ToString()))}}}";
            return _text;
        }
    }

    private static List<OverrideTag> ParseTags(ReadOnlySpan<char> data)
    {
        List<OverrideTag> tags = [];
        while (!data.IsEmpty)
        {
            var q = data.IndexOf('\\');
            if (q < 0)
                q = data.Length;
            data = data[q..];

            if (data.IsEmpty || data[0] is not '\\')
                break;

            // Get the name of the tag
            // Move in from the \, trimming any leading whitespace
            data = data[1..].TrimStart();

            var inlineCode = false; // We want to ignore inline code blocks
            for (q = 0; q < data.Length; q++)
            {
                if (data[q] is '!')
                    inlineCode = !inlineCode;
                else if (data[q] is '(' or '\\' && !inlineCode)
                    break;
            }
            if (q == 0)
                continue;

            var tagName = data[..q].ToString();
            data = data[q..];

            const int maxValidArgCount = 7;
            var args = new List<string>(maxValidArgCount + 1);
            var hasBackslashArg = false;
            // Split parenthesized arguments
            if (!data.IsEmpty && data[0] is '(')
            {
                data = data[1..];

                while (true)
                {
                    data = data.TrimStart(); // Strip whitespace, if any

                    inlineCode = false; // We want to ignore inline code blocks
                    for (q = 0; q < data.Length; q++)
                    {
                        if (data[q] is '!')
                            inlineCode = !inlineCode;
                        else if (data[q] is ',' or '\\' or ')' && !inlineCode)
                            break;
                    }

                    // Stop here if the end of the string was reached
                    if (q == data.Length)
                    {
                        args.Add(data.ToString());
                        break;
                    }

                    // Continue parsing args
                    var r = data[q];
                    if (r is ',')
                    {
                        args.Add(data[..q].ToString());
                        data = data[(q + 1)..];
                    }
                    else
                    {
                        // Swallow the rest of the string
                        // Could be either a backslash arg or last arg
                        if (r is '\\')
                        {
                            hasBackslashArg = true;
                            inlineCode = false; // We want to ignore inline code blocks
                            for (q = 0; q < data.Length; q++)
                            {
                                if (data[q] is '!')
                                    inlineCode = !inlineCode;
                                else if (data[q] is ')' && !inlineCode)
                                    break;
                            }
                        }
                        args.Add(data[..q].ToString());
                        data = data[q..]; // Consoom
                        break;
                    }
                }
            }

            // Tag/Arg parsing time
            if (IsTag(OverrideTags.XBord))
            {
                tags.Add(new OverrideTag.XBord(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.YBord))
            {
                tags.Add(new OverrideTag.YBord(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.XShad))
            {
                tags.Add(new OverrideTag.XShad(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.YShad))
            {
                tags.Add(new OverrideTag.YShad(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FaX))
            {
                tags.Add(new OverrideTag.FaX(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FaY))
            {
                tags.Add(new OverrideTag.FaY(args.Count > 0 ? args[0] : null));
            }
            else if (IsComplexTag(OverrideTags.IClip))
            {
                switch (args.Count)
                {
                    case 4:
                        tags.Add(new OverrideTag.IClip(args[0], args[1], args[2], args[3]));
                        break;
                    case 2:
                        tags.Add(new OverrideTag.IClip(args[0], args[1]));
                        break;
                    case 1:
                        tags.Add(new OverrideTag.IClip(args[0]));
                        break;
                    default:
                        tags.Add(new OverrideTag.Unknown(OverrideTags.IClip, args.ToArray()));
                        break;
                }
            }
            else if (IsTag(OverrideTags.Blur))
            {
                tags.Add(new OverrideTag.Blur(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FscX))
            {
                tags.Add(new OverrideTag.FscX(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FscY))
            {
                tags.Add(new OverrideTag.FscY(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Fsc))
            {
                tags.Add(new OverrideTag.Fsc());
            }
            else if (IsTag(OverrideTags.Fsp))
            {
                tags.Add(new OverrideTag.Fsp(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Fs))
            {
                if (args.Count == 0)
                {
                    tags.Add(new OverrideTag.Fs((string)null!, OverrideTag.Fs.FsVariant.Absolute));
                    continue;
                }
                var value = args[0];
                if (args[0].Length > 0 && args[0][0] is '+' or '-')
                    tags.Add(new OverrideTag.Fs(value, OverrideTag.Fs.FsVariant.Relative));
                else
                    tags.Add(new OverrideTag.Fs(value, OverrideTag.Fs.FsVariant.Absolute));
            }
            else if (IsTag(OverrideTags.Bord))
            {
                tags.Add(new OverrideTag.Bord(args.Count > 0 ? args[0] : null));
            }
            else if (IsComplexTag(OverrideTags.Move))
            {
                if (args.Count == 4)
                    tags.Add(new OverrideTag.Move(args[0], args[1], args[2], args[3]));
                else if (args.Count == 6)
                    tags.Add(
                        new OverrideTag.Move(args[0], args[1], args[2], args[3], args[4], args[5])
                    );
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Move, args.ToArray()));
            }
            else if (IsTag(OverrideTags.FrX))
            {
                tags.Add(new OverrideTag.FrX(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FrY))
            {
                tags.Add(new OverrideTag.FrY(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.FrZ))
            {
                tags.Add(new OverrideTag.FrZ(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Fr))
            {
                tags.Add(new OverrideTag.Fr(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Fn))
            {
                tags.Add(new OverrideTag.Fn(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Alpha))
            {
                tags.Add(new OverrideTag.Alpha(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.An))
            {
                tags.Add(new OverrideTag.An(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.A))
            {
                if (args.Count > 0)
                    tags.Add(new OverrideTag.A(args[0]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.A));
            }
            else if (IsComplexTag(OverrideTags.Pos))
            {
                if (args.Count == 2)
                    tags.Add(new OverrideTag.Pos(args[0], args[1]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Pos, args.ToArray()));
            }
            else if (IsComplexTag(OverrideTags.Fade))
            {
                if (args.Count == 7)
                    tags.Add(
                        new OverrideTag.Fade(
                            args[0],
                            args[1],
                            args[2],
                            args[3],
                            args[4],
                            args[5],
                            args[6]
                        )
                    );
                else if (args.Count == 2)
                    tags.Add(new OverrideTag.Fade(args[0], args[1]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Fade, args.ToArray()));
            }
            else if (IsComplexTag(OverrideTags.Fad))
            {
                if (args.Count == 7)
                    tags.Add(
                        new OverrideTag.Fad(
                            args[0],
                            args[1],
                            args[2],
                            args[3],
                            args[4],
                            args[5],
                            args[6]
                        )
                    );
                else if (args.Count == 2)
                    tags.Add(new OverrideTag.Fad(args[0], args[1]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Fad, args.ToArray()));
            }
            else if (IsComplexTag(OverrideTags.Org))
            {
                if (args.Count == 2)
                    tags.Add(new OverrideTag.Org(args[0], args[1]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Org, args.ToArray()));
            }
            else if (IsComplexTag(OverrideTags.T))
            {
                var blockIdx = args.Count - 1;
                if (blockIdx is < 0 or > 3 || !hasBackslashArg)
                {
                    tags.Add(new OverrideTag.Unknown(OverrideTags.T, args.ToArray()));
                    break;
                }

                // Parse inner tags
                var block = args[blockIdx].AsSpan();
                var blockTags = ParseTags(block);

                switch (blockIdx)
                {
                    case 0: // \t(\xyz)
                        tags.Add(new OverrideTag.T(blockTags));
                        break;
                    case 1: // \t(1.0,\xyz)
                        tags.Add(new OverrideTag.T(args[0], blockTags));
                        break;
                    case 2: // \t(1,10,\xyz)
                        tags.Add(
                            new OverrideTag.T(
                                args[0], // VSFilter moment or something, idk
                                args[1],
                                blockTags
                            )
                        );
                        break;
                    case 3: // \t(1,10,1.0,\xyz)
                        tags.Add(new OverrideTag.T(args[0], args[1], args[2], blockTags));
                        break;
                }
            }
            else if (IsComplexTag(OverrideTags.Clip))
            {
                switch (args.Count)
                {
                    case 4:
                        tags.Add(new OverrideTag.Clip(args[0], args[1], args[2], args[3]));
                        break;
                    case 2:
                        tags.Add(new OverrideTag.Clip(args[0], args[1]));
                        break;
                    case 1:
                        tags.Add(new OverrideTag.Clip(args[0]));
                        break;
                    default:
                        tags.Add(new OverrideTag.Unknown(OverrideTags.Clip, args.ToArray()));
                        break;
                }
            }
            else if (IsTag(OverrideTags.C))
            {
                tags.Add(new OverrideTag.C(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.C1))
            {
                tags.Add(new OverrideTag.C1(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.C2))
            {
                tags.Add(new OverrideTag.C2(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.C3))
            {
                tags.Add(new OverrideTag.C3(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.C4))
            {
                tags.Add(new OverrideTag.C4(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.A1))
            {
                tags.Add(new OverrideTag.A1(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.A2))
            {
                tags.Add(new OverrideTag.A2(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.A3))
            {
                tags.Add(new OverrideTag.A3(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.A4))
            {
                tags.Add(new OverrideTag.A4(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.R))
            {
                tags.Add(new OverrideTag.R(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Be))
            {
                tags.Add(new OverrideTag.Be(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.B))
            {
                tags.Add(new OverrideTag.B(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.I))
            {
                tags.Add(new OverrideTag.I(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Kt))
            {
                tags.Add(new OverrideTag.Kt(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Kf))
            {
                tags.Add(new OverrideTag.Kf(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.KUpper))
            {
                tags.Add(new OverrideTag.KUpper(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Ko))
            {
                tags.Add(new OverrideTag.Ko(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.K))
            {
                tags.Add(new OverrideTag.K(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Shad))
            {
                tags.Add(new OverrideTag.Shad(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.S))
            {
                tags.Add(new OverrideTag.S(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.U))
            {
                tags.Add(new OverrideTag.U(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Pbo))
            {
                if (args.Count > 0)
                    tags.Add(new OverrideTag.Pbo(args[0]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.Pbo, args.ToArray()));
            }
            else if (IsTag(OverrideTags.P))
            {
                if (args.Count > 0)
                    tags.Add(new OverrideTag.P(args[0]));
                else
                    tags.Add(new OverrideTag.Unknown(OverrideTags.P, args.ToArray()));
            }
            else if (IsTag(OverrideTags.Q))
            {
                tags.Add(new OverrideTag.Q(args.Count > 0 ? args[0] : null));
            }
            else if (IsTag(OverrideTags.Fe))
            {
                tags.Add(new OverrideTag.Fe(args.Count > 0 ? args[0] : null));
            }
            else
            {
                tags.Add(new OverrideTag.Unknown(tagName, args.ToArray()));
            }
            continue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IsTag(string query)
            {
                if (!tagName.StartsWith(query))
                    return false;

                if (tagName.Length > query.Length)
                    args.Add(tagName[query.Length..]);
                return true;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IsComplexTag(string query)
            {
                return tagName.StartsWith(query);
            }
        }
        return tags;
    }
}
