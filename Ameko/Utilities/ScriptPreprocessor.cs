// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text.RegularExpressions;
using Semver;

namespace Ameko.Utilities;

internal static partial class ScriptPreprocessor
{
    internal static string Preprocess(string source, SemVersion version)
    {
        source.ReplaceLineEndings(Environment.NewLine);
        var lines = source.Split(Environment.NewLine);
        var depth = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            Match match;
            if ((match = IfRegex().Match(line)).Success)
            {
                var value = EvaluateCondition(match.Groups["cond"].Value, version, lineNumber);
                lines[i] = value ? "#if true" : "#if false";
                depth++;
            }
            else if (EndIfRegex().IsMatch(line))
            {
                if (depth == 0)
                    throw new FormatException($"Line {lineNumber}: $ENDIF without matching $IF.");

                lines[i] = "#endif";
                depth--;
            }
        }

        if (depth > 0)
            throw new FormatException($"Unterminated $IF: {depth} block(s) missing $ENDIF.");

        return string.Join(Environment.NewLine, lines);
    }

    private static bool EvaluateCondition(string condition, SemVersion version, int lineNumber)
    {
        // Compound conditions are AND-only, so every clause must hold.
        foreach (var clause in AndSplitRegex().Split(condition))
        {
            var match = ClauseRegex().Match(clause.Trim());
            if (!match.Success)
                throw new FormatException(
                    $"Line {lineNumber}: invalid condition '{clause.Trim()}'."
                );

            if (
                !SemVersion.TryParse(
                    match.Groups["ver"].Value,
                    SemVersionStyles.Strict,
                    out var target
                )
            )
                throw new FormatException(
                    $"Line {lineNumber}: invalid version '{match.Groups["ver"].Value}'."
                );

            var cmp = version.ComparePrecedenceTo(target);
            var ok = match.Groups["op"].Value switch
            {
                ">=" => cmp >= 0,
                "<=" => cmp <= 0,
                ">" => cmp > 0,
                "<" => cmp < 0,
                "==" => cmp == 0,
                "!=" => cmp != 0,
                _ => throw new InvalidOperationException(),
            };

            if (!ok)
                return false;
        }

        return true;
    }

    [GeneratedRegex(@"^\s*//\s*\$IF\s+(?<cond>.+?)\s*$")]
    private static partial Regex IfRegex();

    [GeneratedRegex(@"^\s*//\s*\$ENDIF\s*$")]
    private static partial Regex EndIfRegex();

    [GeneratedRegex(@"\s+AND\s+")]
    private static partial Regex AndSplitRegex();

    [GeneratedRegex(@"^HOLO_API_VERSION\s*(?<op>>=|<=|==|!=|>|<)\s*(?<ver>\S+)$")]
    private static partial Regex ClauseRegex();
}
