// SPDX-License-Identifier: GPL-3.0-only

using System;
using Ameko.Utilities;
using Ameko.ViewModels.Dialogs;
using Holo.Models;
using Holo.Scripting.Models;
using Material.Icons;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Windows;

public partial class PkgManWindowViewModel
{
    /// <summary>
    /// Install a package
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateInstallCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedStorePackage is null)
                return;

            var result = await PackageManager.InstallPackage(SelectedStorePackage);

            if (result is InstallationResult.Success)
            {
                this.RaisePropertyChanged(nameof(InstallButtonEnabled));
                this.RaisePropertyChanged(nameof(UninstallButtonEnabled));

                await _scriptService.Reload(false);
            }
            else
            {
                await _messageBoxService.ShowAsync(result);
            }
        });
    }

    /// <summary>
    /// Uninstall a package
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateUninstallCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedInstalledPackage is null)
                return;

            var result = PackageManager.UninstallPackage(SelectedInstalledPackage);

            if (result == InstallationResult.Success)
            {
                SelectedInstalledPackage = null;
                this.RaisePropertyChanged(nameof(InstallButtonEnabled));
                this.RaisePropertyChanged(nameof(UninstallButtonEnabled));

                await _scriptService.Reload(false);
            }
            else
            {
                await _messageBoxService.ShowAsync(result);
            }
        });
    }

    /// <summary>
    /// Update a package
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateUpdateCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedInstalledPackage is null)
                return;

            var result = await PackageManager.UpdatePackage(SelectedInstalledPackage);
            _updateCandidates.Clear();
            _updateCandidates.AddRange(PackageManager.GetUpdateCandidates());

            if (result is InstallationResult.Success)
            {
                // Raise all the properties because updates can result in installations
                this.RaisePropertyChanged(nameof(InstallButtonEnabled));
                this.RaisePropertyChanged(nameof(UninstallButtonEnabled));
                this.RaisePropertyChanged(nameof(UpdateButtonEnabled));
                this.RaisePropertyChanged(nameof(UpdateAllButtonEnabled));

                await _scriptService.Reload(false);
            }
            else
            {
                await _messageBoxService.ShowAsync(result);
            }
        });
    }

    /// <summary>
    /// Update all packages
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateUpdateAllCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (_updateCandidates.Count == 0)
                return;

            var finalResult = InstallationResult.Success;

            foreach (var package in _updateCandidates)
            {
                // Skip modified packages
                if (await PackageManager.IsPackageModified(package))
                {
                    _logger.LogInformation(
                        "Skipping update for {PackageName} because it has been modified",
                        package.QualifiedName
                    );
                    continue;
                }

                var result = await PackageManager.UpdatePackage(package);
                // Pick a failure, any failure
                if (
                    finalResult == InstallationResult.Success
                    && result != InstallationResult.Success
                )
                {
                    finalResult = result;
                }
            }

            _updateCandidates.Clear();
            _updateCandidates.AddRange(PackageManager.GetUpdateCandidates());

            // Raise all the properties because updates can result in installations
            this.RaisePropertyChanged(nameof(InstallButtonEnabled));
            this.RaisePropertyChanged(nameof(UninstallButtonEnabled));
            this.RaisePropertyChanged(nameof(UpdateButtonEnabled));
            this.RaisePropertyChanged(nameof(UpdateAllButtonEnabled));

            if (finalResult is not InstallationResult.Success)
                await _messageBoxService.ShowAsync(finalResult);

            await _scriptService.Reload(false);
        });
    }

    /// <summary>
    /// Refresh the Package Store
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateRefreshCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await PackageManager.SetUpBaseRepository();
            await PackageManager.AddAdditionalRepositories(_configuration.RepositoryUrls);

            _updateCandidates.Clear();
            _updateCandidates.AddRange(PackageManager.GetUpdateCandidates());

            // Clear out
            SelectedInstalledPackage = null;
            SelectedStorePackage = null;

            // Raise all the properties
            this.RaisePropertyChanged(nameof(InstallButtonEnabled));
            this.RaisePropertyChanged(nameof(UninstallButtonEnabled));
            this.RaisePropertyChanged(nameof(UpdateButtonEnabled));
            this.RaisePropertyChanged(nameof(UpdateAllButtonEnabled));

            await _messageBoxService.ShowAsync(
                I18N.PkgMan.PkgManWindow_Title,
                I18N.PkgMan.PkgMan_MsgBox_Refreshed
            );
        });
    }

    /// <summary>
    /// Add a repository
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateAddRepositoryCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var input = RepoUrlInput.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return;

            _logger.LogInformation("Adding repository {Input}", input);

            var result = await PackageManager.AddRepository(input);

            switch (result)
            {
                case InstallationResult.Success:
                    _configuration.AddRepositoryUrl(input);
                    await _messageBoxService.ShowAsync(result);
                    break;
                case InstallationResult.AlreadyInstalled:
                    _logger.LogError("Could not add repository because it was already installed.");
                    await _messageBoxService.ShowAsync(
                        I18N.PkgMan.PkgManWindow_Title,
                        I18N.PkgMan.PkgMan_Result_Repository_AlreadyInstalled
                    );
                    break;
                case InstallationResult.Failure:
                    _logger.LogError("Failed to add repository");
                    await _messageBoxService.ShowAsync(result);
                    break;
                default:
                    _logger.LogError("Invalid repository add response");
                    throw new ArgumentOutOfRangeException();
            }
            // Clean up
            RepoUrlInput = string.Empty;
        });
    }

    /// <summary>
    /// Remove a repository
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateRemoveRepositoryCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedRepository?.Url is null)
                return;

            _logger.LogInformation("Removing repository {RepositoryName}", SelectedRepository.Name);

            var result = PackageManager.RemoveRepository(SelectedRepository.Name);

            switch (result)
            {
                case InstallationResult.Success:
                    _configuration.RemoveRepositoryUrl(SelectedRepository.Url);
                    await _messageBoxService.ShowAsync(result);
                    break;
                case InstallationResult.NotInstalled:
                    _logger.LogError("Could not remove repository because it was not installed");
                    await _messageBoxService.ShowAsync(
                        I18N.PkgMan.PkgManWindow_Title,
                        I18N.PkgMan.PkgMan_Result_Repository_NotInstalled
                    );
                    break;
                default:
                    _logger.LogError("Invalid repository remove response");
                    throw new ArgumentOutOfRangeException();
            }

            try
            {
                _configuration.RemoveRepositoryUrl(SelectedRepository.Url);
                await PackageManager.SetUpBaseRepository();
                await PackageManager.AddAdditionalRepositories(_configuration.RepositoryUrls);
                await _messageBoxService.ShowAsync(InstallationResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Removing repository failed");
                await _messageBoxService.ShowAsync(
                    I18N.PkgMan.PkgManWindow_Title,
                    ex.Message,
                    MsgBoxButtonSet.Ok,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Error
                );
            }
            // Clean up
            SelectedRepository = null;
        });
    }

    /// <summary>
    /// Show the changelog for a package
    /// </summary>
    private ReactiveCommand<Package, RxVoid> CreateShowChangelogCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Package package) =>
            {
                var vm = _viewModelFactory.Create<ChangelogDialogViewModel>(
                    package.GenerateChangelog()
                );
                await ShowChangelog.Handle(vm);
            }
        );
    }

    /// <summary>
    /// Show the source for a local package
    /// </summary>
    private ReactiveCommand<Package, RxVoid> CreateViewLocalSourceCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Package package) =>
            {
                if (!PackageManager.IsPackageInstalled(package, out var location))
                    location = new Uri(package.Url); // fallback

                var vm = _viewModelFactory.Create<SourceViewerDialogViewModel>(package, location);
                await ShowSourceViewer.Handle(vm);
            }
        );
    }

    /// <summary>
    /// Show the source for a remote package
    /// </summary>
    private ReactiveCommand<Package, RxVoid> CreateViewRemoteSourceCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Package package) =>
            {
                var vm = _viewModelFactory.Create<SourceViewerDialogViewModel>(
                    package,
                    new Uri(package.Url)
                );
                await ShowSourceViewer.Handle(vm);
            }
        );
    }
}
