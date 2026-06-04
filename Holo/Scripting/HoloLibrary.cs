// SPDX-License-Identifier: MPL-2.0

using Holo.Providers;
using Holo.Scripting.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Scripting;

/// <summary>
/// Base class for user script libraries
/// </summary>
/// <remarks>
/// <para>
/// Libraries can be imported by scripts using the <c>css_include</c> directive.
/// </para><para>
/// <b>Note that CS-Script does not re-use library instances!</b>
/// If you need a library to be shared, you need to publish
/// the library on NuGet and use the <c>css_nuget</c> import directive instead.
/// </para>
/// </remarks>
public abstract class HoloLibrary : IHoloExecutable
{
    /// <summary>
    /// Basic script information
    /// </summary>
    public virtual PackageInfo Info { get; }

    /// <summary>
    /// Initialize the library
    /// </summary>
    /// <param name="info">Library information</param>
    protected HoloLibrary(PackageInfo info)
    {
        Info = info;
        Logger = ScriptServiceLocator.GetLogger(info.QualifiedName ?? GetType().Name);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// Logger instance
    /// </summary>
    protected ILogger Logger { get; }
}
