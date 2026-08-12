// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Ameko.Services;
using Ameko.Utilities;
using Ameko.ViewModels.Dialogs;
using Holo.Configuration;
using Holo.Providers;
using Holo.Scripting;
using Holo.Scripting.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Windows;

public partial class PkgManWindowViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IScriptService _scriptService;
    private readonly IConfiguration _configuration;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IViewModelFactory _viewModelFactory;

    private readonly ObservableCollection<Package> _updateCandidates;
    private string _repoUrlInput;

    public Interaction<ChangelogDialogViewModel, RxVoid> ShowChangelog { get; }
    public Interaction<SourceViewerDialogViewModel, RxVoid> ShowSourceViewer { get; }

    public ICommand InstallCommand { get; }
    public ICommand UninstallCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand UpdateAllCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddRepositoryCommand { get; }
    public ICommand RemoveRepositoryCommand { get; }
    public ICommand ShowChangelogCommand { get; }
    public ICommand ViewLocalSourceCommand { get; }
    public ICommand ViewRemoteSourceCommand { get; }

    public IPackageManager PackageManager { get; }

    public Package? SelectedStorePackage
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(InstallButtonEnabled));
        }
    }

    public Package? SelectedInstalledPackage
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(UninstallButtonEnabled));
            this.RaisePropertyChanged(nameof(UpdateButtonEnabled));
        }
    }

    public Repository? SelectedRepository
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(RemoveRepoButtonEnabled));
        }
    }

    public string RepoUrlInput
    {
        get => _repoUrlInput;
        set
        {
            this.RaiseAndSetIfChanged(ref _repoUrlInput, value);
            this.RaisePropertyChanged(nameof(AddRepoButtonEnabled));
        }
    }

    public bool InstallButtonEnabled =>
        SelectedStorePackage is not null
        && !PackageManager.InstalledPackages.Contains(SelectedStorePackage);

    public bool UninstallButtonEnabled => SelectedInstalledPackage is not null;

    public bool UpdateButtonEnabled =>
        SelectedInstalledPackage is not null
        && _updateCandidates.Contains(SelectedInstalledPackage);

    public bool UpdateAllButtonEnabled => _updateCandidates.Count > 0;

    public bool AddRepoButtonEnabled =>
        !string.IsNullOrEmpty(RepoUrlInput) && Uri.TryCreate(RepoUrlInput, UriKind.Absolute, out _);

    public bool RemoveRepoButtonEnabled =>
        SelectedRepository?.Url != null
        && SelectedRepository.Url != PackageManager.BaseRepositoryUrl
        && _configuration.RepositoryUrls.Contains(SelectedRepository.Url);

    public PkgManWindowViewModel(
        IPackageManager packageManager,
        IScriptService scriptService,
        IConfiguration configuration,
        IMessageBoxService messageBoxService,
        IViewModelFactory viewModelFactory,
        ILogger<PkgManWindowViewModel> logger
    )
    {
        PackageManager = packageManager;

        _scriptService = scriptService;
        _configuration = configuration;
        _messageBoxService = messageBoxService;
        _viewModelFactory = viewModelFactory;
        _logger = logger;

        _repoUrlInput = string.Empty;

        _updateCandidates = new ObservableCollection<Package>(PackageManager.GetUpdateCandidates());

        ShowChangelog = new Interaction<ChangelogDialogViewModel, RxVoid>();
        ShowSourceViewer = new Interaction<SourceViewerDialogViewModel, RxVoid>();

        InstallCommand = CreateInstallCommand();
        UninstallCommand = CreateUninstallCommand();
        UpdateCommand = CreateUpdateCommand();
        UpdateAllCommand = CreateUpdateAllCommand();
        RefreshCommand = CreateRefreshCommand();
        AddRepositoryCommand = CreateAddRepositoryCommand();
        RemoveRepositoryCommand = CreateRemoveRepositoryCommand();
        ShowChangelogCommand = CreateShowChangelogCommand();
        ViewLocalSourceCommand = CreateViewLocalSourceCommand();
        ViewRemoteSourceCommand = CreateViewRemoteSourceCommand();
    }
}
