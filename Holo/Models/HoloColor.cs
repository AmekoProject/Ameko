// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// A RGBA color
/// </summary>
/// <param name="Red">Red channel</param>
/// <param name="Green">Green channel</param>
/// <param name="Blue">Blue channel</param>
/// <param name="Alpha">Alpha channel</param>
public sealed record HoloColor(byte Red, byte Green, byte Blue, byte Alpha);
