// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;
using Holo.Configuration;
using Holo.Scripting;
using Microsoft.Extensions.Logging;

namespace Holo.Providers;

/// <summary>
/// Provides a controlled service locator for use by
/// <see cref="HoloScript"/> and <see cref="HoloLibrary"/> instances
/// </summary>
/// <remarks>
/// This locator acts as a bridge for user scripts and libraries,
/// allowing them to access a predefined set of application services
/// since they are unable to leverage dependency injection.
/// <para>
/// Only services explicitly added to the locator are available.
/// Any attempt by a script to retrieve an unregistered or unpermitted service will
/// result in an <see cref="System.ArgumentOutOfRangeException"/>.
/// </para>
/// </remarks>
public sealed class ScriptServiceLocator
{
    private static Dictionary<Type, object>? Services { get; set; }
    private static ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Retrieves a registered service instance of the specified type.
    /// </summary>
    /// <remarks>
    /// This method is intended for use by dynamically-loaded user scripts and libraries
    /// that cannot utilize traditional dependency injection.
    /// It provides controlled access to a curated set of application services.
    /// </remarks>
    /// <param name="callerName">
    /// Automatically populated with the name of the calling member (method or property)
    /// </param>
    /// <typeparam name="T">
    /// The type of the service to retrieve. Must be a non-nullable reference type.
    /// </typeparam>
    /// <returns>
    /// An instance of the requested service, cast to type <typeparamref name="T"/>
    /// </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown if the requested service type (<typeparamref name="T"/>) is not
    /// registered or is not permitted for access by scripts through this locator
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if this method is called before the locator is initialized
    /// </exception>
    /// <example>
    /// Example usage in a <see cref="HoloScript"/>:
    /// <code>
    /// var prjProvider = ScriptServiceLocator.Get&lt;IProjectProvider&gt;();
    /// var workingSpace = prjProvider.Current.WorkingSpace;
    /// </code>
    /// </example>
    public static T Get<T>([CallerMemberName] string callerName = "")
        where T : notnull
    {
        if (Services is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ScriptServiceLocator)} is not initialized."
            );
        }

        if (Services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new ArgumentOutOfRangeException(
        // nameof(T),
        // string.Format(I18N.Other.ScriptServiceLocator_Get_UnregisteredScript, typeof(T).Name)
        );
    }

    /// <summary>
    /// Create a logger for a script
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the script</param>
    /// <returns>A logger with its category set to the <paramref name="qualifiedName"/></returns>
    /// <exception cref="InvalidOperationException">Thrown if this method is called before the locator is initialized</exception>
    public static ILogger GetLogger(string qualifiedName)
    {
        if (LoggerFactory is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ScriptServiceLocator)} is not initialized."
            );
        }
        return LoggerFactory.CreateLogger(qualifiedName);
    }

    public ScriptServiceLocator(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IGlobals globals,
        IProjectProvider projectProvider,
        IScriptConfigurationService scriptConfigurationService,
        IMessageService messageService,
        IMessageBoxService messageBoxService,
        IWindowService windowService,
        ICultureService cultureService,
        IScriptInvoker scriptInvoker
    )
    {
        LoggerFactory = loggerFactory;
        Services = new Dictionary<Type, object>
        {
            [typeof(IConfiguration)] = configuration,
            [typeof(IGlobals)] = globals,
            [typeof(IProjectProvider)] = projectProvider,
            [typeof(IScriptConfigurationService)] = scriptConfigurationService,
            [typeof(IMessageService)] = messageService,
            [typeof(IMessageBoxService)] = messageBoxService,
            [typeof(IWindowService)] = windowService,
            [typeof(ICultureService)] = cultureService,
            [typeof(IScriptInvoker)] = scriptInvoker,
        };
    }
}
