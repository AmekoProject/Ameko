// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holo.Providers;

/// <summary>
/// Statically-accessible Logger Factory
/// </summary>
/// <remarks>
/// Best practice would be to find a good way to not need this
/// </remarks>
public class StaticLoggerFactory
{
    private static ILoggerFactory? _factory;

    /// <summary>
    /// Get a logger for type <typeparamref name="T" />
    /// </summary>
    /// <typeparam name="T">Type to get the logger for</typeparam>
    /// <returns>Logger</returns>
    public static ILogger<T> GetLogger<T>()
    {
        return _factory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    /// <summary>
    /// Get a named logger
    /// </summary>
    /// <param name="name">Name to give the logger</param>
    /// <returns>Logger</returns>
    public static ILogger GetLogger(string name)
    {
        return _factory?.CreateLogger(name) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Instantiate the Static Logger Factory
    /// </summary>
    public StaticLoggerFactory(ILoggerFactory factory)
    {
        _factory = factory;
    }
}
