// SPDX-License-Identifier: MPL-2.0

using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Holo.Scripting.Models;

/// <summary>
/// A repository is a collection of <see cref="Package"/>s
/// and <see cref="Repositories"/> for use in Dependency Control
/// </summary>
public record Repository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Name of the repository
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short description of the repository
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Name of the person(s) responsible for maintaining the repository
    /// </summary>
    public required string Maintainer { get; init; }

    /// <summary>
    /// <see langword="true"/> if this repository is for beta package
    /// </summary>
    public required bool IsBetaChannel { get; init; }

    /// <summary>
    /// Packages hosted by this repository
    /// </summary>
    [JsonIgnore]
    public required FrozenSet<Package> Packages { get; init; }

    /// <summary>
    /// Repositories hoisted by this repository
    /// </summary>
    /// <remarks>Repository json URLs</remarks>
    [JsonIgnore]
    public required FrozenSet<string> Repositories { get; init; }

    /// <summary>
    /// Automatic property
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// Build a repository
    /// </summary>
    /// <param name="url">Url to download repository data from</param>
    /// <param name="client">HttpClient to use</param>
    /// <returns><see cref="Repository"/> object, or <see langword="null"/> on failure</returns>
    public static async Task<Repository> Build(string url, HttpClient client)
    {
        var content = await client.GetStringAsync(url);
        return Build(url, content);
    }

    /// <summary>
    /// Build a repository
    /// </summary>
    /// <param name="url">Url of the repository</param>
    /// <param name="jsonContent">JSON content to parse</param>
    /// <returns><see cref="Repository"/> object, or <see langword="null"/> on failure</returns>
    public static Repository Build(string url, string jsonContent)
    {
        var repo = JsonSerializer.Deserialize<Repository>(jsonContent, JsonOptions);
        if (repo is not null)
            repo.Url = url;
        else
            throw new NullReferenceException($"Unable to deserialize {nameof(Repository)}");
        return repo;
    }

    #region Serialization fields
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [JsonPropertyName("Repositories")]
    public HashSet<string> SerializationRepositories
    {
        get => Repositories.ToHashSet();
        init => Repositories = value.ToFrozenSet();
    }

    [JsonPropertyName("Packages")]
    public HashSet<Package> SerializationPackages
    {
        get => Packages.ToHashSet();
        init => Packages = value.ToFrozenSet();
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    #endregion Serialization fields
}
