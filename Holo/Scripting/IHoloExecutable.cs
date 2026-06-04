// SPDX-License-Identifier: MPL-2.0

using Holo.Scripting.Models;

namespace Holo.Scripting;

/// <summary>
/// A user script or library for Holo to use
/// </summary>
public interface IHoloExecutable
{
    /// <summary>
    /// Basic script information
    /// </summary>
    public PackageInfo Info { get; }
}
