// SPDX-License-Identifier: MPL-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using Holo.Models;

namespace Holo.Configuration.Migration;

internal static class ConfigurationMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly ConfigurationMigratorV2 V2Migrator = new();
    private static readonly ConfigurationMigratorV3 V3Migrator = new();
    private static readonly ConfigurationMigratorCurrent CurrentMigrator = new();

    public static ConfigurationModel? MigrateToCurrent(string content)
    {
        using var json = JsonDocument.Parse(content);

        if (!json.RootElement.TryGetProperty("Version", out var versionProp))
            return null;
        var version = versionProp.GetInt32();

        switch (version)
        {
            case 1:
                var v1 =
                    JsonSerializer.Deserialize<ConfigurationModelV1>(content, JsonOptions)
                    ?? throw new InvalidDataException("Configuration model deserialization failed");
                return CurrentMigrator.Migrate(V3Migrator.Migrate(V2Migrator.Migrate(v1)));
            case 2:
                var v2 =
                    JsonSerializer.Deserialize<ConfigurationModelV2>(content, JsonOptions)
                    ?? throw new InvalidDataException("Configuration model deserialization failed");
                return CurrentMigrator.Migrate(V3Migrator.Migrate(v2));
            case 3:
                var v3 =
                    JsonSerializer.Deserialize<ConfigurationModelV3>(content, JsonOptions)
                    ?? throw new InvalidDataException("Configuration model deserialization failed");
                return CurrentMigrator.Migrate(v3);
        }
        return null;
    }
}

internal interface IConfigurationMigrator<in TIn, out TOut>
    where TIn : ConfigurationModelBase
    where TOut : ConfigurationModelBase
{
    /// <summary>
    /// Migrate a <paramref name="config"/> with version
    /// <typeparamref name="TIn"/> to version <typeparamref name="TOut"/>
    /// </summary>
    /// <param name="config">Config to migrate</param>
    /// <returns>Migrated config</returns>
    TOut Migrate(TIn config);
}

/// <summary>
/// Migrate from the v1 to v2
/// </summary>
internal class ConfigurationMigratorV2
    : IConfigurationMigrator<ConfigurationModelV1, ConfigurationModelV2>
{
    /// <inheritdoc />
    public ConfigurationModelV2 Migrate(ConfigurationModelV1 config)
    {
        return new ConfigurationModelV2
        {
            Version = ConfigurationModelBase.CurrentApiVersion,
            Cps = config.Cps,
            CpsIncludesWhitespace = config.CpsIncludesWhitespace,
            CpsIncludesPunctuation = config.CpsIncludesPunctuation,
            UseSoftLinebreaks = config.UseSoftLinebreaks,
            AutosaveEnabled = config.AutosaveEnabled,
            AutosaveInterval = config.AutosaveInterval,
            AutoloadAudioTracks = config.AutoloadAudioTracks,
            LineWidthIncludesWhitespace = config.LineWidthIncludesWhitespace,
            LineWidthIncludesPunctuation = config.LineWidthIncludesPunctuation,
            RichPresenceLevel = config.RichPresenceLevel,
            SaveFrames = config.SaveFrames,
            DefaultLayer = config.DefaultLayer,
            Culture = config.Culture,
            SpellcheckCulture = config.SpellcheckCulture,
            Theme = config.Theme,
            GridPadding = config.GridPadding,
            EditorFontSize = config.EditorFontSize,
            GridFontSize = config.GridFontSize,
            ReferenceFontSize = config.ReferenceFontSize,
            PropagateFields = config.PropagateFields,
            RepositoryUrls = config.RepositoryUrls,
            ScriptMenuOverrides = config.ScriptMenuOverrides,
            Timing = config.Timing,

            // New!
            IndexCacheExpiration = 8,
        };
    }
}

/// <summary>
/// Migrate from the v2 to v3
/// </summary>
internal class ConfigurationMigratorV3
    : IConfigurationMigrator<ConfigurationModelV2, ConfigurationModelV3>
{
    /// <inheritdoc />
    public ConfigurationModelV3 Migrate(ConfigurationModelV2 config)
    {
        return new ConfigurationModelV3
        {
            Version = ConfigurationModelBase.CurrentApiVersion,
            Cps = config.Cps,
            CpsIncludesWhitespace = config.CpsIncludesWhitespace,
            CpsIncludesPunctuation = config.CpsIncludesPunctuation,
            UseSoftLinebreaks = config.UseSoftLinebreaks,
            AutosaveEnabled = config.AutosaveEnabled,
            AutosaveInterval = config.AutosaveInterval,
            IndexCacheExpiration = config.IndexCacheExpiration,
            AutoloadAudioTracks = config.AutoloadAudioTracks,
            LineWidthIncludesWhitespace = config.LineWidthIncludesWhitespace,
            LineWidthIncludesPunctuation = config.LineWidthIncludesPunctuation,
            RichPresenceLevel = config.RichPresenceLevel,
            SaveFrames = config.SaveFrames,
            DefaultLayer = config.DefaultLayer,
            Culture = config.Culture,
            SpellcheckCulture = config.SpellcheckCulture,
            Theme = config.Theme,
            GridPadding = config.GridPadding,
            EditorFontSize = config.EditorFontSize,
            GridFontSize = config.GridFontSize,
            ReferenceFontSize = config.ReferenceFontSize,
            PropagateFields = config.PropagateFields,
            RepositoryUrls = config.RepositoryUrls,
            ScriptMenuOverrides = config.ScriptMenuOverrides,
            Timing = config.Timing,

            // New!
            TimingMode = TimingMode.SnapToFrame,
        };
    }
}

/// <summary>
/// Migrate from the versioned model matching the current live model to the live model
/// </summary>
internal class ConfigurationMigratorCurrent
    : IConfigurationMigrator<ConfigurationModelV3, ConfigurationModel>
{
    /// <inheritdoc />
    public ConfigurationModel Migrate(ConfigurationModelV3 config)
    {
        return new ConfigurationModel
        {
            Version = ConfigurationModelBase.CurrentApiVersion,
            Cps = config.Cps,
            CpsIncludesWhitespace = config.CpsIncludesWhitespace,
            CpsIncludesPunctuation = config.CpsIncludesPunctuation,
            UseSoftLinebreaks = config.UseSoftLinebreaks,
            AutosaveEnabled = config.AutosaveEnabled,
            AutosaveInterval = config.AutosaveInterval,
            IndexCacheExpiration = config.IndexCacheExpiration,
            AutoloadAudioTracks = config.AutoloadAudioTracks,
            LineWidthIncludesWhitespace = config.LineWidthIncludesWhitespace,
            LineWidthIncludesPunctuation = config.LineWidthIncludesPunctuation,
            RichPresenceLevel = config.RichPresenceLevel,
            SaveFrames = config.SaveFrames,
            TimingMode = config.TimingMode,
            DefaultLayer = config.DefaultLayer,
            Culture = config.Culture,
            SpellcheckCulture = config.SpellcheckCulture,
            Theme = config.Theme,
            GridPadding = config.GridPadding,
            EditorFontSize = config.EditorFontSize,
            GridFontSize = config.GridFontSize,
            ReferenceFontSize = config.ReferenceFontSize,
            PropagateFields = config.PropagateFields,
            RepositoryUrls = config.RepositoryUrls,
            ScriptMenuOverrides = config.ScriptMenuOverrides,
            Timing = config.Timing,
        };
    }
}
