// SPDX-License-Identifier: MPL-2.0

namespace Holo.Scripting;

/// <summary>
/// A lightweight, immutable-by-convention bag of named arguments passed to <see cref="HoloScript"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Values are stored and looked up case-insensitively. Any type can be stored as a value;
/// retrieval is type-safe via <see cref="TryGet{T}"/> and <see cref="GetOrDefault{T}"/>.
/// </para>
/// <para>
/// <see cref="With"/> always returns a new <see cref="ScriptArgs"/> instance with the
/// added or updated key, leaving the original unchanged. This makes it safe to branch
/// from a shared base — including <see cref="Empty"/> — without risk of cross-contamination:
/// </para>
/// <code>
/// var base = ScriptArgs.Empty.With("env", "prod");
/// var withTimeout = base.With("timeout", TimeSpan.FromSeconds(30));
/// var withRetry = base.With("retries", 3);
/// // base, withTimeout, and withRetry are all independent instances
/// </code>
/// </remarks>
public sealed class ScriptArgs
{
    /// <summary>
    /// A shared instance with no arguments. Safe to use as a base for <see cref="With"/> chains.
    /// </summary>
    public static readonly ScriptArgs Empty = new();

    private readonly Dictionary<string, object?> _args = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <see langword="true"/> if there are no arguments present
    /// </summary>
    public bool IsEmpty => _args.Count == 0;

    /// <summary>
    /// Adds an argument
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <param name="value">Value of the argument</param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// Returns a new <see cref="ScriptArgs"/> object containing all current entries plus the
    /// given key/value pair. If <paramref name="key"/> already exists, its value is
    /// replaced in the copy.
    /// </para>
    /// <para>This method can be used fluently:</para>
    /// <code>
    /// var args = new ScriptArgs()
    ///     .With("env", "prod")
    ///     .With("timeout", TimeSpan.FromSeconds(30))
    ///     .With("retries", 3);
    /// </code>
    /// </remarks>
    public ScriptArgs With(string key, object? value)
    {
        var copy = new ScriptArgs();

        foreach (var kvp in _args)
            copy._args[kvp.Key] = kvp.Value;
        copy._args[key] = value;

        return copy;
    }

    /// <summary>
    /// Attempts to retrieve the value for <paramref name="key"/> as type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <param name="value">Value of the argument, if it exists</param>
    /// <typeparam name="T">Type of argument</typeparam>
    /// <returns><see langword="false"/> if the key is absent or cannot be cast.</returns>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_args.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/> cast to <typeparamref name="T"/>,
    /// or <paramref name="fallback"/> if the key is absent or the value cannot be cast.
    /// </summary>
    /// <param name="key">Name of the argument</param>
    /// <param name="fallback">Fallback value, defaults to <see langword="default"/>.</param>
    /// <typeparam name="T">Type of the argument</typeparam>
    /// <returns>The value, or <paramref name="fallback"/> if the key is absent or cannot be cast.</returns>
    public T? GetOrDefault<T>(string key, T? fallback = default) =>
        TryGet<T>(key, out var value) ? value : fallback;

    /// <summary>
    /// Checks if an argument is present.
    /// </summary>
    /// <param name="key">name of the argument</param>
    /// <returns><see langword="true"/> if the argument is present.</returns>
    public bool Contains(string key) => _args.ContainsKey(key);

    /// <summary>
    /// Checks if all arguments are present
    /// </summary>
    /// <param name="keys">Key names</param>
    /// <returns><see langword="true"/> if all named arguments are present.</returns>
    public bool ContainsAll(IEnumerable<string> keys) => keys.All(key => _args.ContainsKey(key));
}
