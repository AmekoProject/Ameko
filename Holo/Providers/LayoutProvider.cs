// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AssCS;
using Holo.Configuration;
using Holo.IO;
using Holo.Models;
using Microsoft.Extensions.Logging;
using Tomlet;

namespace Holo.Providers;

/// <summary>
/// Provides methods for managing UI layouts
/// </summary>
public class LayoutProvider : BindableBase, ILayoutProvider
{
    private static readonly Uri LayoutsRoot = new(Path.Combine(Directories.DataHome, "layouts"));
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IPersistence _persistence;

    private readonly ObservableCollection<Layout> _layouts;
    private Layout? _currentLayout;

    /// <inheritdoc />
    public Layout? Current
    {
        get => _currentLayout;
        set
        {
            if (value is null)
                return;
            SetProperty(ref _currentLayout, value);
            _persistence.LayoutName = value.Name;
            LayoutChanged?.Invoke(this, new ILayoutProvider.LayoutChangedEventArgs(value));
        }
    }

    /// <inheritdoc />
    public AssCS.Utilities.ReadOnlyObservableCollection<Layout> Layouts { get; }

    /// <inheritdoc />
    public void Reload()
    {
        _logger.LogInformation("Reloading Layouts...");
        if (!_fileSystem.Directory.Exists(LayoutsRoot.LocalPath))
            _fileSystem.Directory.CreateDirectory(LayoutsRoot.LocalPath);

        _layouts.Clear();

        foreach (var path in _fileSystem.Directory.EnumerateFiles(LayoutsRoot.LocalPath, "*.toml"))
        {
            try
            {
                _logger.LogDebug("Loading Layout {Path}...", path);

                using var readFs = _fileSystem.FileStream.New(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
                using var reader = new StreamReader(readFs);

                var layout = TomletMain.To<Layout>(reader.ReadToEnd());
                _layouts.Add(layout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load layout");
            }
        }
        _logger.LogInformation("Reloaded {LayoutsCount} layouts", _layouts.Count);

        if (_layouts.Any(l => l.Name == _persistence.LayoutName))
        {
            _currentLayout = _layouts.First(l => l.Name == _persistence.LayoutName);
            LayoutChanged?.Invoke(this, new ILayoutProvider.LayoutChangedEventArgs(Current));
            return;
        }

        if (_layouts.Count > 0)
        {
            Current = _layouts[0];
            LayoutChanged?.Invoke(this, new ILayoutProvider.LayoutChangedEventArgs(Current));
            return;
        }

        _logger.LogInformation("No layouts loaded! Generating default layouts...");

        foreach (
            var (layout, fileName) in new[]
            {
                (DefaultLayout, "default"),
                (DefaultRightPrjExplorer, "default-right"),
                (SubsOnlyLayout, "subs-only"),
                (VideoOnlyLayout, "video-only"),
                (PetzkuLayout, "petzku"),
                (VerticalLayout, "vertical"),
                (TikTokLayout, "tiktok"),
            }
        )
        {
            _layouts.Add(layout);

            try
            {
                using var writeFs = _fileSystem.FileStream.New(
                    Path.Combine(LayoutsRoot.LocalPath, $"{fileName}.toml"),
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                using var writer = new StreamWriter(writeFs);
                var content = TomletMain.TomlStringFrom(layout);
                writer.Write(content);
                LayoutChanged?.Invoke(this, new ILayoutProvider.LayoutChangedEventArgs(layout));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        _logger.LogInformation("Done!");
        Current = _layouts.First(l => l.Name == DefaultLayout.Name);
    }

    /// <inheritdoc />
    public event EventHandler<ILayoutProvider.LayoutChangedEventArgs>? LayoutChanged;

    /// <summary>
    /// Initialize the layout provider
    /// </summary>
    /// <param name="fileSystem">Filesystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <param name="persistence">Application persistence</param>
    public LayoutProvider(
        IFileSystem fileSystem,
        ILogger<LayoutProvider> logger,
        IPersistence persistence
    )
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _persistence = persistence;
        _layouts = [];
        Layouts = new AssCS.Utilities.ReadOnlyObservableCollection<Layout>(_layouts);
    }

    #region Default Layouts

    private static Layout DefaultLayout =>
        new()
        {
            Name = "Default",
            Author = "9volt",
            ColumnDefinitions = "1.2*, 2, *",
            RowDefinitions = "0.7*, 2, *, 2, *",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
                RowSpan = 3,
            },
            Audio = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 2,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 4,
                ColumnSpan = 3,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = false,
                    Column = 2,
                    Row = 1,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 3,
                    ColumnSpan = 3,
                },
                new Splitter
                {
                    IsVertical = true,
                    Column = 1,
                    Row = 0,
                    RowSpan = 3,
                },
            ],
        };
    private static Layout DefaultRightPrjExplorer =>
        new()
        {
            Name = "Default (Project Explorer on Right)",
            Author = "9volt",
            ColumnDefinitions = "1.2*, 2, *",
            RowDefinitions = "0.7*, 2, *, 2, *",
            Window = new WindowSection { IsProjectExplorerOnLeft = false },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
                RowSpan = 3,
            },
            Audio = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 2,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 4,
                ColumnSpan = 3,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = false,
                    Column = 2,
                    Row = 1,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 3,
                    ColumnSpan = 3,
                },
                new Splitter
                {
                    IsVertical = true,
                    Column = 1,
                    Row = 0,
                    RowSpan = 3,
                },
            ],
        };

    private static Layout SubsOnlyLayout =>
        new()
        {
            Name = "Subs Only",
            Author = "9volt",
            ColumnDefinitions = "*",
            RowDefinitions = "*, 2, 2*",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = false,
                Column = 0,
                Row = 0,
            },
            Audio = new TabSection
            {
                IsVisible = false,
                Column = 0,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 2,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 1,
                },
            ],
        };

    private static Layout VideoOnlyLayout =>
        new()
        {
            Name = "Video Only",
            Author = "9volt",
            ColumnDefinitions = "*",
            RowDefinitions = "*",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
            },
            Audio = new TabSection
            {
                IsVisible = false,
                Column = 0,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = false,
                Column = 0,
                Row = 0,
            },
            Events = new TabSection
            {
                IsVisible = false,
                Column = 0,
                Row = 0,
            },
            Splitters = [],
        };

    private static Layout PetzkuLayout =>
        new()
        {
            Name = "Petzku",
            Author = "9volt",
            ColumnDefinitions = "*, 2, *",
            RowDefinitions = "0.5*, 2, *, 2, *",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
                ColumnSpan = 3,
            },
            Audio = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 2,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 2,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 4,
                ColumnSpan = 3,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 1,
                    ColumnSpan = 3,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 3,
                    ColumnSpan = 3,
                },
                new Splitter
                {
                    IsVertical = true,
                    Column = 1,
                    Row = 2,
                },
            ],
        };

    private static Layout VerticalLayout =>
        new()
        {
            Name = "Vertical",
            Author = "9volt",
            ColumnDefinitions = "*",
            RowDefinitions = "0.2*, 2, 1.8*, 2, 0.75*, 2, *",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 2,
            },
            Audio = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 4,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 6,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 1,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 3,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 0,
                    Row = 5,
                },
            ],
        };

    private static Layout TikTokLayout =>
        new()
        {
            Name = "TikTok",
            Author = "9volt",
            ColumnDefinitions = "*, 2, 2*",
            RowDefinitions = "0.7*, 2, *, 2, *",
            Window = new WindowSection { IsProjectExplorerOnLeft = true },
            Video = new TabSection
            {
                IsVisible = true,
                Column = 0,
                Row = 0,
                RowSpan = 5,
            },
            Audio = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 0,
            },
            Editor = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 3,
            },
            Events = new TabSection
            {
                IsVisible = true,
                Column = 2,
                Row = 4,
            },
            Splitters =
            [
                new Splitter
                {
                    IsVertical = true,
                    Column = 1,
                    Row = 0,
                    RowSpan = 5,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 2,
                    Row = 1,
                },
                new Splitter
                {
                    IsVertical = false,
                    Column = 2,
                    Row = 3,
                    ColumnSpan = 1,
                    RowSpan = 1,
                },
            ],
        };

    #endregion Default Layouts
}
