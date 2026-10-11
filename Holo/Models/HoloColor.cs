// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Holo.Models;

/// <summary>
/// A RGBA color
/// </summary>
/// <param name="Red">Red channel</param>
/// <param name="Green">Green channel</param>
/// <param name="Blue">Blue channel</param>
/// <param name="Alpha">Alpha channel</param>
[JsonConverter(typeof(HoloColorJsonConverter))]
public sealed record HoloColor(byte Red, byte Green, byte Blue, byte Alpha);

/// <summary>
/// Converts <see cref="HoloColor"/> values to and from hexadecimal strings.
/// </summary>
public sealed class HoloColorJsonConverter : JsonConverter<HoloColor>
{
    /// <inheritdoc />
    public override HoloColor Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a hexadecimal color string.");

        var hex =
            reader.GetString() ?? throw new JsonException("Expected a hexadecimal color string.");
        var digits = hex.AsSpan();
        digits = digits.TrimStart('#');

        if (digits.Length is not (6 or 8))
            throw new JsonException("Expected a color in #RRGGBB or #RRGGBBAA format.");

        if (
            !byte.TryParse(
                digits[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var red
            )
            || !byte.TryParse(
                digits[2..4],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var green
            )
            || !byte.TryParse(
                digits[4..6],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var blue
            )
        )
            throw new JsonException("Color contains invalid hexadecimal digits.");

        var alpha = (byte)255;
        if (
            digits.Length == 8
            && !byte.TryParse(
                digits[6..8],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out alpha
            )
        )
            throw new JsonException("Color contains invalid hexadecimal digits.");

        return new HoloColor(red, green, blue, alpha);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        HoloColor value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue($"#{value.Red:X2}{value.Green:X2}{value.Blue:X2}{value.Alpha:X2}");
    }
}
