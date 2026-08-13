// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Holo.IO;
using Holo.Scripting.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Scripting;

/// <summary>
/// Package Manager manages <see cref="Repository"/>s and <see cref="Package"/>s
/// </summary>
public partial class PackageManager : IPackageManager
{
    /// <summary>
    /// Base repository URL
    /// </summary>
    public string BaseRepositoryUrl => "https://dc.ameko.moe/base.json";

    /// <summary>
    /// Limit repository recursion to 2
    /// </summary>
    /// <remarks>
    /// Restricts repo discovery to the parent and its direct children.
    /// </remarks>
    private const int MaxRepositoryRecursionDepth = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly Uri ScriptingRoot = new(Path.Combine(Directories.DataHome, "scripts"));

    /// <summary>
    /// The filesystem being used
    /// </summary>
    /// <remarks>This allows for filesystem mocking to be used in tests</remarks>
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    /// <summary>
    /// The HttpClient being used
    /// </summary>
    /// <remarks>This allows for client mocking to be used in tests</remarks>
    private readonly HttpClient _httpClient;

    private Repository? _baseRepository;
    private readonly Dictionary<string, Repository> _repositoryMap;
    private readonly Dictionary<string, Package> _packageMap;
    private readonly ObservableCollection<Repository> _repositories;
    private readonly ObservableCollection<Package> _packageStore;

    private readonly ObservableCollection<Package> _installedPackages;

    /// <summary>
    /// List of available repositories
    /// </summary>
    public ReadOnlyObservableCollection<Repository> Repositories { get; }

    /// <summary>
    /// List of available packages
    /// </summary>
    public ReadOnlyObservableCollection<Package> PackageStore { get; }

    /// <summary>
    /// List of installed packages
    /// </summary>
    public ReadOnlyObservableCollection<Package> InstalledPackages { get; }

    #region Packages

    /// <summary>
    /// Determine if a package is installed
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns><see langword="true"/> if the package is installed</returns>
    public bool IsPackageInstalled(Package package)
    {
        return _fileSystem.File.Exists(PackagePath(package));
    }

    /// <inheritdoc />
    public bool IsPackageInstalled(Package package, [NotNullWhen(true)] out Uri? location)
    {
        if (IsPackageInstalled(package))
        {
            location = new Uri(PackagePath(package));
            return true;
        }

        location = null;
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> IsPackageModified(Package package)
    {
        if (!IsPackageInstalled(package))
            return false;
        if (!_fileSystem.File.Exists(BackupPath(package)))
            return false;

        using var md5 = MD5.Create();
        await using var packageFs = _fileSystem.FileStream.New(
            PackagePath(package),
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        await using var backupFs = _fileSystem.FileStream.New(
            BackupPath(package),
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        var packageHash = await md5.ComputeHashAsync(packageFs);
        var backupHash = await md5.ComputeHashAsync(backupFs);
        return !packageHash.SequenceEqual(backupHash);
    }

    /// <summary>
    /// Recursively install a <see cref="Package"/> and its dependencies
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    public async Task<InstallationResult> InstallPackage(Package package)
    {
        if (IsPackageInstalled(package))
            return InstallationResult.AlreadyInstalled;
        _logger.LogInformation("Attempting to install package {Package}", package.QualifiedName);

        if (!ValidateQualifiedName(package.QualifiedName))
            return InstallationResult.InvalidName;

        foreach (
            var dependencyName in package.Dependencies.Where(dependencyName =>
                dependencyName != package.QualifiedName
            ) // Prevent recursion
        )
        {
            if (!_packageMap.TryGetValue(dependencyName, out var dependency))
            {
                _logger.LogError(
                    "Failed to find dependency {Dependency} for package {Package}",
                    dependencyName,
                    package.QualifiedName
                );
                return InstallationResult.DependencyNotFound;
            }

            // Skip already-installed dependencies
            if (IsPackageInstalled(dependency))
                continue;

            var depResult = await InstallPackage(dependency);
            if (depResult != InstallationResult.Success)
                return depResult; // Cascade failures
        }

        try
        {
            CreateDirectories(package);

            var path = PackagePath(package);
            var backup = BackupPath(package);
            var sidecar = SidecarPath(package);
            var help = HelpPath(package);

            await using var dlStream = await _httpClient.GetStreamAsync(package.Url);
            await using var packageFs = _fileSystem.FileStream.New(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None
            );
            await dlStream.CopyToAsync(packageFs);

            await using var backupFs = _fileSystem.FileStream.New(
                backup,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );

            await packageFs.FlushAsync();
            packageFs.Seek(0, SeekOrigin.Begin);
            await packageFs.CopyToAsync(backupFs);

            await using var sidecarFs = _fileSystem.FileStream.New(
                sidecar,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            await JsonSerializer.SerializeAsync(sidecarFs, package, JsonOptions);

            if (!string.IsNullOrEmpty(package.HelpUrl))
            {
                try
                {
                    await using var helpStream = await _httpClient.GetStreamAsync(package.HelpUrl);
                    await using var helpFs = _fileSystem.FileStream.New(
                        help,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    );
                    await helpStream.CopyToAsync(helpFs);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to download help file {Help}", help);
                }
            }

            AssCS.Utilities.ListExtensions.AddSorted(_installedPackages, package);
            _logger.LogInformation(
                "Successfully installed package {Package}",
                package.QualifiedName
            );
            return InstallationResult.Success;
        }
        catch (IOException e)
        {
            _logger.LogError(e, "Failed to write package {Package} to disk", package.QualifiedName);
            return InstallationResult.FilesystemFailure;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to install package {Package}", package.QualifiedName);
            return InstallationResult.Failure;
        }
    }

    /// <summary>
    /// Uninstall a <see cref="Package"/>
    /// </summary>
    /// <param name="package">Package to uninstall</param>
    /// <param name="isUpdate">Bypass dependency checking for update purposes</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    /// <remarks>Does not uninstall dependencies</remarks>
    public InstallationResult UninstallPackage(Package package, bool isUpdate = false)
    {
        CreateDirectories(package);

        if (!IsPackageInstalled(package))
            return InstallationResult.NotInstalled;
        if (
            !isUpdate && _installedPackages.Any(m => m.Dependencies.Contains(package.QualifiedName))
        )
            return InstallationResult.IsRequiredDependency;
        _logger.LogInformation("Attempting to uninstall package {Package}", package.QualifiedName);

        try
        {
            _fileSystem.File.Delete(PackagePath(package));
            _fileSystem.File.Delete(BackupPath(package));
            _fileSystem.File.Delete(SidecarPath(package));
            _fileSystem.File.Delete(HelpPath(package));
            _logger.LogInformation(
                "Successfully uninstalled package {Package}",
                package.QualifiedName
            );
            _installedPackages.Remove(package);
            return InstallationResult.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to uninstall package {Package}", package.QualifiedName);
            return InstallationResult.Failure;
        }
    }

    /// <summary>
    /// Update a package to the latest version
    /// </summary>
    /// <param name="package">Package to update</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    public async Task<InstallationResult> UpdatePackage(Package package)
    {
        _logger.LogInformation("Update for package {Package} requested...", package.QualifiedName);

        var storePackage = _packageStore.FirstOrDefault(p =>
            p.QualifiedName == package.QualifiedName
        );
        if (storePackage is null)
        {
            _logger.LogError(
                "Could not find new version of {PackageName} in the store!",
                package.QualifiedName
            );
            return InstallationResult.Failure;
        }

        var uninstallResult = UninstallPackage(package, true);
        if (uninstallResult == InstallationResult.Success)
            return await InstallPackage(storePackage);
        return uninstallResult;
    }

    /// <inheritdoc />
    public async Task<InstallationResult> RestorePackage(Package package)
    {
        if (!IsPackageInstalled(package))
            return InstallationResult.NotInstalled;
        if (!await IsPackageModified(package))
            return InstallationResult.Success;

        await using var packageFs = _fileSystem.FileStream.New(
            PackagePath(package),
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );
        await using var backupFs = _fileSystem.FileStream.New(
            BackupPath(package),
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        await backupFs.CopyToAsync(packageFs);
        return InstallationResult.Success;
    }

    /// <summary>
    /// Get a list of installed packages with available updates
    /// </summary>
    /// <returns>List of updatable packages</returns>
    public List<Package> GetUpdateCandidates()
    {
        var updatable = new List<Package>();

        foreach (var pkg in _installedPackages)
        {
            var match = _packageStore.FirstOrDefault(m => m.QualifiedName == pkg.QualifiedName);
            if (match is null)
                continue;

            if (match.Version > pkg.Version)
                updatable.Add(match);
        }
        return updatable;
    }

    /// <summary>
    /// Check if a package is up to date
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns><see langword="true"/> if the package is up to date</returns>
    /// <remarks>If the package isn't found in the <see cref="PackageStore"/>, returns <see langword="true"/></remarks>
    public bool IsPackageUpToDate(Package package)
    {
        var match = _packageStore.FirstOrDefault(m => m.QualifiedName == package.QualifiedName);
        if (match is null)
            return true;

        return match.Version <= package.Version;
    }

    /// <summary>
    /// Get the filepath for a package
    /// </summary>
    /// <param name="package">Package</param>
    /// <returns>The filepath, ending in <c>.cs</c></returns>
    public static string PackagePath(Package package)
    {
        var qName = package.QualifiedName;
        return package.Type switch
        {
            PackageType.Script => Path.Combine(ScriptingRoot.LocalPath, $"{qName}.cs"),
            PackageType.Library => Path.Combine(ScriptingRoot.LocalPath, $"{qName}.lib.cs"),
            PackageType.Scriptlet => Path.Combine(ScriptingRoot.LocalPath, $"{qName}.js"),
            _ => throw new ArgumentOutOfRangeException(nameof(package)),
        };
    }

    /// <summary>
    /// Get the filepath for a package's backup
    /// </summary>
    /// <param name="package">Package</param>
    /// <returns>The filepath, ending in <c>.cs</c></returns>
    public static string BackupPath(Package package)
    {
        var qName = package.QualifiedName;
        return package.Type switch
        {
            PackageType.Script => Path.Combine(ScriptingRoot.LocalPath, ".bak", $"{qName}.cs"),
            PackageType.Library => Path.Combine(ScriptingRoot.LocalPath, ".bak", $"{qName}.lib.cs"),
            PackageType.Scriptlet => Path.Combine(ScriptingRoot.LocalPath, ".bak", $"{qName}.js"),
            _ => throw new ArgumentOutOfRangeException(nameof(package)),
        };
    }

    /// <summary>
    /// Get the filepath for a package sidecar
    /// </summary>
    /// <param name="package">Package</param>
    /// <returns>The filepath, ending in <c>.json</c></returns>
    public static string SidecarPath(Package package)
    {
        var qName = package.QualifiedName;
        return package.Type switch
        {
            PackageType.Script => Path.Combine(
                ScriptingRoot.LocalPath,
                "packages",
                $"{qName}.json"
            ),
            PackageType.Library => Path.Combine(
                ScriptingRoot.LocalPath,
                "packages",
                $"{qName}.lib.json"
            ),
            PackageType.Scriptlet => Path.Combine(
                ScriptingRoot.LocalPath,
                "packages",
                $"{qName}.json"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(package)),
        };
    }

    /// <summary>
    /// Get the filepath for a package help doc
    /// </summary>
    /// <param name="package">Package</param>
    /// <returns>The filepath, ending in <c>.md</c></returns>
    public static string HelpPath(Package package)
    {
        var qName = package.QualifiedName;
        return package.Type switch
        {
            PackageType.Script => Path.Combine(ScriptingRoot.LocalPath, "help", $"{qName}.md"),
            PackageType.Library => Path.Combine(ScriptingRoot.LocalPath, "help", $"{qName}.lib.md"),
            PackageType.Scriptlet => Path.Combine(ScriptingRoot.LocalPath, "help", $"{qName}.md"),
            _ => throw new ArgumentOutOfRangeException(nameof(package)),
        };
    }

    /// <summary>
    /// Validate the validity of a script's Qualified Name
    /// </summary>
    /// <param name="qualifiedName">Qualified name to check</param>
    /// <returns><see langword="true"/> if the name is valid</returns>
    public static bool ValidateQualifiedName(string qualifiedName)
    {
        return QualifiedNameRegex().IsMatch(qualifiedName);
    }

    #endregion Packages

    #region Repositories

    /// <summary>
    /// Recursively collect repositories
    /// </summary>
    /// <param name="repository">Head repository</param>
    /// <param name="depth">Current recursion depth</param>
    /// <returns>List of new repositories</returns>
    private async Task<IList<Repository>> GatherRepositories(Repository repository, int depth = 1)
    {
        _logger.LogTrace(
            "Gathering repositories for repository '{RepositoryName}'",
            repository.Name
        );
        var newRepos = new List<Repository>();

        if (_repositoryMap.TryAdd(repository.Name, repository))
        {
            _repositories.Add(repository);
            newRepos.Add(repository);
        }

        foreach (var url in repository.Repositories)
        {
            try
            {
                var repo = await Repository.Build(url, _httpClient);
                if (!_repositoryMap.ContainsKey(repo.Name) && depth < MaxRepositoryRecursionDepth)
                    newRepos.AddRange(await GatherRepositories(repo, ++depth));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to build repository {Url}", url);
            }
        }
        return newRepos;
    }

    /// <summary>
    /// Populate the <see cref="PackageStore"/> and the <see cref="InstalledPackages"/> list
    /// </summary>
    /// <param name="repository">Repository to gather for</param>
    private void GatherPackages(Repository repository)
    {
        GatherPackages([repository]);
    }

    /// <summary>
    /// Populate the <see cref="PackageStore"/> and the <see cref="InstalledPackages"/> list
    /// </summary>
    /// <param name="repositories">Repositories to gather for. Defaults to <see cref="Repositories"/></param>
    private void GatherPackages(IList<Repository>? repositories = null)
    {
        _logger.LogTrace("Gathering packages...");
        foreach (var repo in repositories ?? _repositories)
        {
            _logger.LogTrace("Gathering packages in repository '{RepoName}'...", repo.Name);
            foreach (var package in repo.Packages)
            {
                package.Repository = repo.Name;
                if (_packageMap.TryGetValue(package.QualifiedName, out var conflict))
                {
                    _logger.LogWarning(
                        "Conflict between {PackageName} from '{ConflictRepository}' and '{PackageRepository}'!",
                        package.QualifiedName,
                        conflict.Repository,
                        package.Repository
                    );
                    continue;
                }
                _packageMap.Add(package.QualifiedName, package);
                AssCS.Utilities.ListExtensions.AddSorted(_packageStore, package);

                if (!IsPackageInstalled(package))
                    continue;

                var sidecarPath = SidecarPath(package);
                if (_fileSystem.File.Exists(sidecarPath))
                {
                    try
                    {
                        using var sidecarFs = _fileSystem.FileStream.New(
                            sidecarPath,
                            FileMode.Open
                        );
                        var sidecar = JsonSerializer.Deserialize<Package>(sidecarFs, JsonOptions);
                        if (sidecar is null)
                            _logger.LogWarning("Failed to read sidecar {S}", sidecarPath);
                        AssCS.Utilities.ListExtensions.AddSorted(
                            _installedPackages,
                            sidecar ?? package
                        );
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(
                            "Failed to read sidecar {S} because of {Exception}",
                            sidecarPath,
                            e
                        );
                        AssCS.Utilities.ListExtensions.AddSorted(_installedPackages, package);
                    }
                }
                else
                {
                    AssCS.Utilities.ListExtensions.AddSorted(_installedPackages, package);
                }
            }
        }
        _logger.LogTrace("Done!");
    }

    /// <summary>
    /// Load repositories from a list of URLs and populate the <see cref="PackageStore"/>
    /// </summary>
    /// <param name="repoUrls">List of <see cref="Repository"/> URLs</param>
    public async Task AddAdditionalRepositories(IList<string> repoUrls)
    {
        _logger.LogInformation(
            "Adding additional {RepoUrlsCount} user repositories...",
            repoUrls.Count
        );
        List<Repository> newRepos = [];

        foreach (var url in repoUrls)
        {
            try
            {
                var repo = await Repository.Build(url, _httpClient);
                newRepos.AddRange(await GatherRepositories(repo));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to build repository {Url}", url);
            }
        }
        if (repoUrls.Count > 1)
            GatherPackages(newRepos);
        _logger.LogInformation("Done!");
    }

    /// <inheritdoc />
    public async Task<InstallationResult> AddRepository(string repoUrl)
    {
        try
        {
            var repo = await Repository.Build(repoUrl, _httpClient);
            if (_repositoryMap.ContainsKey(repo.Name))
                return InstallationResult.AlreadyInstalled;

            _logger.LogInformation("Adding repository '{RepoName}'", repo.Name);
            _repositories.Add(repo);
            _repositoryMap.Add(repo.Name, repo);
            GatherPackages(repo);
            return InstallationResult.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to build repository {Url}", repoUrl);
            return InstallationResult.Failure;
        }
    }

    /// <inheritdoc />
    public InstallationResult RemoveRepository(string repositoryName)
    {
        if (!_repositoryMap.Remove(repositoryName, out var repo))
            return InstallationResult.NotInstalled;

        _logger.LogInformation("Removing repository '{RepositoryName}'", repositoryName);
        _repositories.Remove(repo);
        _packageMap.Clear();
        _packageStore.Clear();
        GatherPackages(_repositories);
        return InstallationResult.Success;
    }

    /// <summary>
    /// Set up the base repository
    /// </summary>
    /// <remarks>This clears Dependency Control</remarks>
    public async Task SetUpBaseRepository()
    {
        _logger.LogInformation("Setting up base repository...");
        _repositories.Clear();
        _repositoryMap.Clear();
        _installedPackages.Clear();
        _packageStore.Clear();
        _packageMap.Clear();

        try
        {
            _baseRepository = await Repository.Build(BaseRepositoryUrl, _httpClient);
            await GatherRepositories(_baseRepository);
            GatherPackages();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to build base repository!");
        }
        _logger.LogInformation("Done!");
    }

    #endregion Repositories

    /// <summary>
    /// Create required directories if they don't already exist
    /// </summary>
    /// <param name="package">Package to create directories for</param>
    private void CreateDirectories(Package package)
    {
        if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(PackagePath(package))))
        {
            _fileSystem.Directory.CreateDirectory(
                Path.GetDirectoryName(PackagePath(package)) ?? "/"
            );
        }

        if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(BackupPath(package))))
        {
            _fileSystem.Directory.CreateDirectory(
                Path.GetDirectoryName(BackupPath(package)) ?? "/"
            );
        }

        if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(SidecarPath(package))))
        {
            _fileSystem.Directory.CreateDirectory(
                Path.GetDirectoryName(SidecarPath(package)) ?? "/"
            );
        }

        if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(HelpPath(package))))
        {
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(HelpPath(package)) ?? "/");
        }
    }

    /// <summary>
    /// Instantiate a Dependency Control instance
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <param name="httpClient">HttpClient to use</param>
    /// <remarks>
    /// This constructor does not set up the base repository.
    /// <see cref="SetUpBaseRepository"/> should be called following construction
    /// </remarks>
    public PackageManager(
        IFileSystem fileSystem,
        ILogger<PackageManager> logger,
        HttpClient httpClient
    )
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _httpClient = httpClient;
        _repositoryMap = [];
        _packageMap = [];
        _repositories = [];
        _packageStore = [];

        _installedPackages = [];

        Repositories = new ReadOnlyObservableCollection<Repository>(_repositories);
        PackageStore = new ReadOnlyObservableCollection<Package>(_packageStore);

        InstalledPackages = new ReadOnlyObservableCollection<Package>(_installedPackages);

        _logger.LogInformation("Initialized Dependency Control");
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._]+$")]
    private static partial Regex QualifiedNameRegex();
}
