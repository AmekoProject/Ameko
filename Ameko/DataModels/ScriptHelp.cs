// SPDX-License-Identifier: GPL-3.0-only

namespace Ameko.DataModels;

public class ScriptHelp
{
    public required string QualifiedName { get; init; }
    public required string DisplayName { get; init; }
    public required string Content { get; init; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ScriptHelp other && other.QualifiedName == QualifiedName;
    }

    protected bool Equals(ScriptHelp other)
    {
        return QualifiedName == other.QualifiedName;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return QualifiedName.GetHashCode();
    }
}
