// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Ameko.Converters;
using Ameko.Services;
using Ameko.Utilities;
using Ameko.ViewModels.Windows;
using Ameko.Views.Windows;
using AssCS.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Holo.Configuration;
using Holo.Configuration.Keybinds;
using Holo.Providers;
using Holo.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ameko;

public class App : Application
{
    public override void Initialize()
    {
        CultureService.SetCulture();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        desktop.Exit += (_, _) => Environment.Exit(0);

        if (Program.Args.Length == 2 && Program.Args[0] == "--display-crash-report")
            InitializeAmekoForCrashReport(desktop);
        else
            InitializeAmekoForNormalOperation(desktop);
    }

    /// <summary>
    /// Prepare to run Ameko normally
    /// </summary>
    /// <param name="desktop">Application lifetime</param>
    private void InitializeAmekoForNormalOperation(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var provider = AmekoServiceProvider.Build();

        if (Program.IsInSafeMode)
        {
            provider
                .GetRequiredService<ILogger<App>>()
                .LogWarning("Ameko is running in safe mode! Some features may be unavailable.");
        }

        // Activate some key services
        _ = provider.GetRequiredService<ThemeService>();
        // May have to move this if it gets too resource-intensive
        provider.GetRequiredService<ILayoutProvider>().Reload();

        // Queue up welcome message before other services start adding messages
        provider
            .GetRequiredService<IMessageService>()
            .Enqueue(I18N.Resources.Message_Welcome, TimeSpan.FromSeconds(7));

        // Set up the tab item template
        Resources["WorkspaceTabTemplate"] = new WorkspaceTabTemplate(provider);

        // Set up the CPS warn converter
        Resources["CpsWarnConverter"] = new CpsWarnConverter(
            provider.GetRequiredService<IProjectProvider>(),
            provider.GetRequiredService<IConfiguration>()
        );

        // Create main window
        var vm = provider.GetRequiredService<MainWindowViewModel>();
        desktop.MainWindow = provider.GetRequiredService<MainWindow>();
        DataContext = vm;
        desktop.MainWindow.DataContext = vm;

        // Load window state
        var persist = provider.GetRequiredService<IPersistence>();
        desktop.MainWindow.Width = persist.WindowWidth;
        desktop.MainWindow.Height = persist.WindowHeight;
        desktop.MainWindow.WindowState = persist.WindowState.ToAvaloniaWindowState();

        base.OnFrameworkInitializationCompleted();

        // Start long process loading in the background after GUI finishes loading
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Check if there's anything to open
            if (Program.Args.Length > 0)
                InitializeStartupProject(provider, desktop.MainWindow);

            InitializeKeybindService(provider);
            InitializeScriptService(provider);
            InitializePackageManager(provider);
            InitializeDiscordRpcService(provider);
            InitializeAutosaveService(provider);
#if !DEBUG // Skip update checking on debug builds
            await InitializeUpdateService(provider);
#endif
        });
    }

    /// <summary>
    /// Prepare to display the crash report window
    /// </summary>
    /// <param name="desktop">Application lifetime</param>
    private void InitializeAmekoForCrashReport(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = new CrashReporterWindowViewModel(StringEncoder.Base64Decode(Program.Args[1]));
        desktop.MainWindow = new CrashReporterWindow { DataContext = vm };

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Initialize the <see cref="KeybindService"/> (and <see cref="KeybindRegistrar"/>)
    /// </summary>
    /// <param name="provider">Service Provider</param>
    private static void InitializeKeybindService(IServiceProvider provider)
    {
        // Commence keybind registration
        _ = provider.GetRequiredService<IKeybindService>();
    }

    /// <summary>
    /// Initialize the Discord rich presence service
    /// </summary>
    /// <param name="provider">Service Provider</param>
    private static void InitializeDiscordRpcService(IServiceProvider provider)
    {
        _ = provider.GetRequiredService<DiscordRpcService>();
    }

    /// <summary>
    /// Initialize the Autosave service
    /// </summary>
    /// <param name="provider">Service Provider</param>
    private static void InitializeAutosaveService(IServiceProvider provider)
    {
        _ = provider.GetRequiredService<AutosaveService>();
    }

    /// <summary>
    /// Initialize the Update service and check for updates
    /// </summary>
    /// <param name="provider"></param>
    private static async Task InitializeUpdateService(IServiceProvider provider)
    {
        var service = provider.GetRequiredService<UpdateService>();
        await service.CheckForUpdates();
    }

    /// <summary>
    /// Initialize the <see cref="ScriptService"/>
    /// </summary>
    /// <param name="provider">Service Provider</param>
    private static void InitializeScriptService(IServiceProvider provider)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await provider.GetRequiredService<IScriptService>().Reload(isManual: false);
            }
            catch (Exception ex)
            {
                provider
                    .GetRequiredService<ILogger<App>>()
                    .LogError(ex, "ScriptService failed to initialize.");
            }
        });
    }

    /// <summary>
    /// Initialize <see cref="PackageManager"/>
    /// </summary>
    /// <param name="provider">Service Provider</param>
    private static void InitializePackageManager(IServiceProvider provider)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var pkgMan = provider.GetRequiredService<IPackageManager>();
                await pkgMan.SetUpBaseRepository();
                await pkgMan.AddAdditionalRepositories(
                    provider.GetRequiredService<IConfiguration>().RepositoryUrls
                );
            }
            catch (Exception ex)
            {
                provider
                    .GetRequiredService<ILogger<App>>()
                    .LogError(ex, "Dependency Control failed to initialize.");
            }
        });
    }

    private void InitializeStartupProject(IServiceProvider provider, Window mainWindow)
    {
        var fs = provider.GetRequiredService<IFileSystem>();
        var projectProvider = provider.GetRequiredService<IProjectProvider>();
        var vm = provider.GetRequiredService<MainWindowViewModel>();
        List<Uri> subs = [];
        List<Uri> projects = [];
        foreach (var arg in Program.Args)
        {
            var path = arg;
            if (!Path.IsPathRooted(path)) // De-relative any relative paths
            {
                path = Path.GetFullPath(Path.Combine(fs.Directory.GetCurrentDirectory(), arg));
            }
            if (fs.File.Exists(path))
            {
                switch (Path.GetExtension(path))
                {
                    case ".ass":
                    case ".srt":
                    case ".ssa":
                        subs.Add(new Uri(path));
                        break;
                    case ".aproj":
                        projects.Add(new Uri(path));
                        break;
                    default:
                        continue;
                }
            }
        }

        if (projects.Count > 0)
        {
            projectProvider.Current = projectProvider.CreateFromFile(projects.First());
        }

        if (subs.Count > 0)
        {
            mainWindow.Opened += (_, _) =>
            {
                vm.OpenSubtitlesNoGuiCommand.Execute(subs.ToArray());
            };
        }
    }
}
