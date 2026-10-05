// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Holo.Configuration;

internal static class ModelDictionaryExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    extension(Dictionary<string, object?> model)
    {
        /// <summary>
        /// Retrieve a value from the dictionary
        /// </summary>
        /// <param name="defaultValue">Default value</param>
        /// <param name="propertyName">Property name key in <c>@default.Property</c> form</param>
        /// <typeparam name="T">Type of the <paramref name="defaultValue"/></typeparam>
        /// <returns>The value in the model, or the <paramref name="defaultValue"/></returns>
        public T GetOrDefault<T>(
            T defaultValue,
            [CallerArgumentExpression(nameof(defaultValue))] string? propertyName = null
        )
        {
            var separator = propertyName?.LastIndexOf('.') ?? -1;
            if (separator < 0 || separator == propertyName!.Length - 1)
                throw new ArgumentException(
                    "The default value must be a property access expression.",
                    nameof(propertyName)
                );

            var key = propertyName[(separator + 1)..].TrimStart('@');
            return model.GetOrDefault(key, defaultValue);
        }

        /// <summary>
        /// Retrieve a value from the dictionary
        /// </summary>
        /// <param name="key">Lookup key</param>
        /// <param name="defaultValue">Default value</param>
        /// <typeparam name="T">Type of the <paramref name="defaultValue"/></typeparam>
        /// <returns>The value in the model, or the <paramref name="defaultValue"/></returns>
        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (!model.TryGetValue(key, out var value) || value is not JsonElement element)
                return defaultValue;

            try
            {
                return element.Deserialize<T>(JsonOptions) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Set a value
        /// </summary>
        /// <param name="value">Value to set</param>
        /// <param name="propertyName">Property name key</param>
        /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
        public void Set<T>(
            T value,
            [CallerArgumentExpression(nameof(value))] string? propertyName = null
        )
        {
            if (propertyName is null)
                return;

            model.Set(propertyName, value);
        }

        /// <summary>
        /// Set a value
        /// </summary>
        /// <param name="key">Lookup key</param>
        /// <param name="value">Value to set</param>
        /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
        public void Set<T>(string key, T value)
        {
            model[key] = value;
        }

        /// <summary>
        /// Set the model version
        /// </summary>
        /// <param name="version">Model version</param>
        public void SetVersion(int version)
        {
            model["Version"] = version;
        }
    }
}
