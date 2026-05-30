// SPDX-License-Identifier: MPL-2.0

using System.Text.Json;
using Holo.Models;

namespace Holo.Configuration.Migration;

internal static class ProjectMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
    private static readonly ProjectMigratorV2 V2Migrator = new();
    private static readonly ProjectMigratorCurrent CurrentMigrator = new();

    /// <summary>
    /// Migrate a project to the current version
    /// </summary>
    /// <param name="content">Input project</param>
    /// <returns>Migrated project</returns>
    public static ProjectModel? MigrateToCurrent(string content)
    {
        using var json = JsonDocument.Parse(content);

        if (!json.RootElement.TryGetProperty("Version", out var versionProp))
            return null;
        var version = versionProp.GetInt32();

        switch (version)
        {
            case 1:
                var v1 =
                    JsonSerializer.Deserialize<ProjectModelV1>(content, JsonOptions)
                    ?? throw new InvalidDataException("Project model deserialization failed");
                return CurrentMigrator.Migrate(V2Migrator.Migrate(v1));
            case 2:
                var v2 =
                    JsonSerializer.Deserialize<ProjectModelV2>(content, JsonOptions)
                    ?? throw new InvalidDataException("Project model deserialization failed");
                return CurrentMigrator.Migrate(v2);
        }

        return null;
    }
}

internal interface IProjectMigrator<in TIn, out TOut>
    where TIn : ProjectModelBase
    where TOut : ProjectModelBase
{
    /// <summary>
    /// Migrate a <paramref name="project"/> with version
    /// <typeparamref name="TIn"/> to version <typeparamref name="TOut"/>
    /// </summary>
    /// <param name="project">Project to migrate</param>
    /// <returns>Migrated project</returns>
    TOut Migrate(TIn project);
}

/// <summary>
/// Migrate from V1 to V2
/// </summary>
internal class ProjectMigratorV2 : IProjectMigrator<ProjectModelV1, ProjectModelV2>
{
    /// <inheritdoc />
    public ProjectModelV2 Migrate(ProjectModelV1 project)
    {
        return new ProjectModelV2
        {
            Version = 2,
            ReferencedDocuments = project.ReferencedDocuments,
            Styles = project.Styles,
            Cps = project.Cps,
            CpsIncludesWhitespace = project.CpsIncludesWhitespace,
            CpsIncludesPunctuation = project.CpsIncludesPunctuation,
            UseSoftLinebreaks = project.UseSoftLinebreaks,
            DefaultLayer = project.DefaultLayer,
            SpellcheckCulture = project.SpellcheckCulture,
            CustomWords = project.CustomWords,
            Timing = project.Timing,
            ScriptConfiguration = project.ScriptConfiguration,

            // New in version 2
            Colors = [],
            Terms = [],
        };
    }
}

/// <summary>
/// Migrate from the versioned model matching the current live model to the live model
/// </summary>
internal class ProjectMigratorCurrent : IProjectMigrator<ProjectModelV2, ProjectModel>
{
    /// <inheritdoc />
    public ProjectModel Migrate(ProjectModelV2 project)
    {
        return new ProjectModel
        {
            Version = ProjectModelBase.CurrentApiVersion,
            ReferencedDocuments = project.ReferencedDocuments,
            Styles = project.Styles,
            Colors = project.Colors,
            Cps = project.Cps,
            CpsIncludesWhitespace = project.CpsIncludesWhitespace,
            CpsIncludesPunctuation = project.CpsIncludesPunctuation,
            UseSoftLinebreaks = project.UseSoftLinebreaks,
            DefaultLayer = project.DefaultLayer,
            SpellcheckCulture = project.SpellcheckCulture,
            CustomWords = project.CustomWords,
            Terms = project.Terms,
            Timing = project.Timing,
            ScriptConfiguration = project.ScriptConfiguration,
        };
    }
}
