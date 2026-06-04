// SPDX-License-Identifier: MPL-2.0

namespace Holo.Media;

/// <summary>
/// Represents the scale factor of a video viewport
/// </summary>
public class ScaleFactor
{
    /// <summary>
    /// Textual representation of the scale
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Scale multiplier
    /// </summary>
    public required double Multiplier { get; init; }

    /// <summary>
    /// Default scale
    /// </summary>
    public static ScaleFactor Default => new() { Text = "100.0%", Multiplier = 1d };

    /// <summary>
    /// Valid scale factors
    /// </summary>
    public static List<ScaleFactor> Scales =>
        Enumerable
            .Range(1, 24) // 0.125 * 24 = 3
            .Select(i =>
            {
                var scale = i * 0.125;
                return new ScaleFactor { Text = $"{scale * 100}%", Multiplier = scale };
            })
            .ToList();

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="other">The object to compare with the current object</param>
    /// <returns><see langword="true"/> if the objects are equal</returns>
    protected bool Equals(ScaleFactor other)
    {
        return Multiplier.Equals(other.Multiplier);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ScaleFactor)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Multiplier.GetHashCode();
    }
}
