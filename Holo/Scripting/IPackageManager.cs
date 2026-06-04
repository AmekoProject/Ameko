// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using Holo.Scripting.Models;

namespace Holo.Scripting;

/// <summary>
/// Provides methods for managing installed scripts and libraries
/// </summary>
public interface IPackageManager
{
    /// <summary>
    /// Base repository URL
    /// </summary>
    string BaseRepositoryUrl { get; }

    /// <summary>
    /// List of available repositories
    /// </summary>
    ReadOnlyObservableCollection<Repository> Repositories { get; }

    /// <summary>
    /// List of available packages
    /// </summary>
    ReadOnlyObservableCollection<Package> PackageStore { get; }

    /// <summary>
    /// List of installed packages
    /// </summary>
    ReadOnlyObservableCollection<Package> InstalledPackages { get; }

    /// <summary>
    /// Determine if a package is installed
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns><see langword="true"/> if the package is installed</returns>
    bool IsPackageInstalled(Package package);

    /// <summary>
    /// Recursively install a <see cref="Package"/> and its dependencies
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    Task<InstallationResult> InstallPackage(Package package);

    /// <summary>
    /// Uninstall a <see cref="Package"/>
    /// </summary>
    /// <param name="package">Package to uninstall</param>
    /// <param name="isUpdate">Bypass dependency checking for update purposes</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    /// <remarks>Does not uninstall dependencies</remarks>
    InstallationResult UninstallPackage(Package package, bool isUpdate = false);

    /// <summary>
    /// Update a package to the latest version
    /// </summary>
    /// <param name="package">Package to update</param>
    /// <returns><see cref="InstallationResult.Success"/> on success</returns>
    Task<InstallationResult> UpdatePackage(Package package);

    /// <summary>
    /// Get a list of installed packages with available updates
    /// </summary>
    /// <returns>List of updatable packages</returns>
    List<Package> GetUpdateCandidates();

    /// <summary>
    /// Check if a package is up to date
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns><see langword="true"/> if the package is up to date</returns>
    /// <remarks>If the package isn't found in the <see cref="PackageStore"/>, returns <see langword="true"/></remarks>
    bool IsPackageUpToDate(Package package);

    /// <summary>
    /// Load repositories from a list of URLs and populate the <see cref="PackageStore"/>
    /// </summary>
    /// <param name="repoUrls">List of <see cref="Repository"/> URLs</param>
    Task AddAdditionalRepositories(IList<string> repoUrls);

    /// <summary>
    /// Add a repository and populate the <see cref="PackageStore"/>
    /// </summary>
    /// <param name="repoUrl"><see cref="Repository"/> URL</param>
    /// <returns><see langword="true"/> if the addition was successful</returns>
    Task<InstallationResult> AddRepository(string repoUrl);

    /// <summary>
    /// Remove a repository and update the <see cref="PackageStore"/>
    /// </summary>
    /// <param name="repoName">Name of the repository to remove</param>
    /// <returns><see langword="true"/> if the removal was successful</returns>
    InstallationResult RemoveRepository(string repoName);

    /// <summary>
    /// Set up the base repository
    /// </summary>
    /// <remarks>This clears Dependency Control</remarks>
    Task SetUpBaseRepository();
}
