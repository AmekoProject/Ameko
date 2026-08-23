// SPDX-License-Identifier: MPL-2.0

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace AssCS;

/// <summary>
/// Manages entries to "Aegisub Project Garbage"
/// </summary>
public class GarbageManager
{
    private readonly ConcurrentDictionary<string, GarbageEntry> _data = [];

    /// <summary>
    /// Number of entries in the manager
    /// </summary>
    public int Count => _data.Count;

    /// <summary>
    /// Set a <see cref="string"/> entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    public void Set(string name, string value)
    {
        _data[name] = new GarbageEntry(name, value);
    }

    /// <summary>
    /// Set a <see cref="double"/> entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    public void Set(string name, double value)
    {
        var s = Convert.ToString(value, CultureInfo.InvariantCulture);
        _data[name] = new GarbageEntry(name, s);
    }

    /// <summary>
    /// Set a <see cref="decimal"/> entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    public void Set(string name, decimal value)
    {
        var s = Convert.ToString(value, CultureInfo.InvariantCulture);
        _data[name] = new GarbageEntry(name, s);
    }

    /// <summary>
    /// Set an <see cref="int"/> entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    public void Set(string name, int value)
    {
        var s = Convert.ToString(value);
        _data[name] = new GarbageEntry(name, s);
    }

    /// <summary>
    /// Set a <see cref="bool"/> entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    public void Set(string name, bool value)
    {
        var s = Convert.ToString(value);
        _data[name] = new GarbageEntry(name, s);
    }

    /// <summary>
    /// Set a JSON entry
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Entry value</param>
    /// <typeparam name="T">Type of the entry</typeparam>
    public void SetJson<T>(string name, T value)
    {
        var s = JsonSerializer.Serialize(value);
        _data[name] = new GarbageEntry(name, s);
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public string GetString(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: String {name} does not exist");
        return value.Value;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetString(string name, [NotNullWhen(true)] out string? value)
    {
        if (Contains(name))
        {
            value = GetString(name);
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public double GetDouble(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: Double {name} does not exist");
        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetDouble(string name, [NotNullWhen(true)] out double? value)
    {
        if (Contains(name))
        {
            value = GetDouble(name);
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public decimal GetDecimal(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: Double {name} does not exist");
        return Convert.ToDecimal(value.Value);
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetDecimal(string name, [NotNullWhen(true)] out decimal? value)
    {
        if (Contains(name))
        {
            value = GetDecimal(name);
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public int GetInt(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: Int {name} does not exist");
        return Convert.ToInt32(value.Value);
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetInt(string name, [NotNullWhen(true)] out int? value)
    {
        if (Contains(name))
        {
            value = GetInt(name);
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public bool GetBool(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: Bool {name} does not exist");
        return Convert.ToBoolean(value.Value);
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetBool(string name, [NotNullWhen(true)] out bool? value)
    {
        if (Contains(name))
        {
            value = GetBool(name);
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns>Value of the entry</returns>
    /// <exception cref="KeyNotFoundException">If the value does not exist</exception>
    public T? GetJson<T>(string name)
    {
        if (!_data.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Project Properties: Bool {name} does not exist");
        try
        {
            return JsonSerializer.Deserialize<T>(value.Value);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Get an entry by <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry</param>
    /// <param name="value">Value of the entry</param>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <returns><see langword="true"/> if the entry was found</returns>
    public bool TryGetJson<T>(string name, [NotNullWhen(true)] out T? value)
    {
        if (Contains(name))
        {
            try
            {
                value = JsonSerializer.Deserialize<T>(GetString(name));
                return value is not null;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Get all the entries
    /// </summary>
    /// <returns>Read-only collection of entries</returns>
    public ReadOnlyCollection<GarbageEntry> GetAll()
    {
        return new ReadOnlyCollection<GarbageEntry>(_data.Values.ToList());
    }

    /// <summary>
    /// Check if an entry with the given <paramref name="name"/> exists
    /// </summary>
    /// <param name="name">Name of the entry to check</param>
    /// <returns><see langword="true"/> if the entry exists</returns>
    public bool Contains(string name)
    {
        return _data.ContainsKey(name);
    }

    /// <summary>
    /// Removes the entry with the given <paramref name="name"/>
    /// </summary>
    /// <param name="name">Name of the entry to remove</param>
    /// <returns><see langword="true"/> if the entry was removed</returns>
    public bool Remove(string name)
    {
        return _data.TryRemove(name, out _);
    }

    /// <summary>
    /// Clear the entries
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }

    /// <summary>
    /// Load the default entries
    /// </summary>
    public void LoadDefault()
    {
        Clear();
        Set("video_zoom", 0.0d);
        Set("ar_value", 0.0d);
        Set("scroll_position", 0);
        Set("active_row", 0);
        Set("ar_mode", 0);
        Set("video_position", 0);
    }
}
