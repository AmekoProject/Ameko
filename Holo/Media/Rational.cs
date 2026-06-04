// SPDX-License-Identifier: MPL-2.0

namespace Holo.Media;

/// <summary>
/// A fraction
/// </summary>
public readonly struct Rational
{
    /// <summary>
    /// Numerator
    /// </summary>
    public int Numerator { get; init; }

    /// <summary>
    /// Denominator
    /// </summary>
    public int Denominator { get; init; }

    /// <summary>
    /// Calculated ratio of <see cref="Numerator"/> and <see cref="Denominator"/>
    /// </summary>
    public double Ratio => Numerator / (double)Denominator;
}
