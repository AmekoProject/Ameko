// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;

namespace Holo.Scripting;

/// <summary>
/// Provides methods for reading and writing per-script configuration.
/// </summary>
public interface IScriptConfigurationService
{
    /// <summary>
    /// Attempts to retrieve a global configuration value for the specified key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value as.</typeparam>
    /// <param name="caller">The script requesting the value.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true"/>, contains the retrieved value;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the key exists and the value was successfully retrieved;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool TryGet<T>(IHoloExecutable caller, string key, [NotNullWhen(true)] out T? value);

    /// <summary>
    /// Stores a global configuration value under the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="caller">The script storing the value.</param>
    /// <param name="key">The configuration key to write to.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(IHoloExecutable caller, string key, T value);

    /// <summary>
    /// Removes a global configuration entry for the specified key.
    /// </summary>
    /// <param name="caller">The script removing the value.</param>
    /// <param name="key">The configuration key to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the key existed and was removed;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool Remove(IHoloExecutable caller, string key);

    /// <summary>
    /// Determines whether a global configuration entry exists for the specified key.
    /// </summary>
    /// <param name="caller">The script to check against.</param>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>
    /// <see langword="true"/> if the key exists; otherwise <see langword="false"/>.
    /// </returns>
    bool Contains(IHoloExecutable caller, string key);

    /// <summary>
    /// Attempts to retrieve a project-scoped configuration value for the specified key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value as.</typeparam>
    /// <param name="caller">The script requesting the value.</param>
    /// <param name="project">The project scope to read from.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true"/>, contains the retrieved value;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the key exists and the value was successfully retrieved;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool TryGet<T>(
        IHoloExecutable caller,
        Project project,
        string key,
        [NotNullWhen(true)] out T? value
    );

    /// <summary>
    /// Stores a project-scoped configuration value under the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="caller">The script storing the value.</param>
    /// <param name="project">The project scope to write to.</param>
    /// <param name="key">The configuration key to write to.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(IHoloExecutable caller, Project project, string key, T value);

    /// <summary>
    /// Removes a project-scoped configuration entry for the specified key.
    /// </summary>
    /// <param name="caller">The script removing the value.</param>
    /// <param name="project">The project scope to remove from.</param>
    /// <param name="key">The configuration key to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the key existed and was removed;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool Remove(IHoloExecutable caller, Project project, string key);

    /// <summary>
    /// Determines whether a project-scoped configuration entry exists for the specified key.
    /// </summary>
    /// <param name="caller">The script to check against.</param>
    /// <param name="project">The project scope to check within.</param>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>
    /// <see langword="true"/> if the key exists; otherwise <see langword="false"/>.
    /// </returns>
    bool Contains(IHoloExecutable caller, Project project, string key);
}
