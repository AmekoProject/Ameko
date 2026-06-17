// SPDX-License-Identifier: MPL-2.0

using System.ComponentModel;
using System.IO.Abstractions.TestingHelpers;
using Holo.Configuration;
using Holo.IO;
using Holo.Media;
using Holo.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using static TestingUtils.TestableUri;

namespace Holo.Tests;

public class LayoutProviderTests
{
    [Test]
    public async Task Constructor()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<LayoutProvider>.Instance;
        var p = new MockPersistence();
        var provider = new LayoutProvider(fs, lg, p);

        await Assert.That(provider.Current).IsNull();
        await Assert.That(provider.Layouts).IsEmpty();
    }

    [Test]
    public async Task Reload()
    {
        var fs = new MockFileSystem();
        fs.AddFile(
            MakeTestableUri(
                fs,
                Path.Combine(Directories.DataHome, "layouts", "testing.toml")
            ).LocalPath,
            new MockFileData(ExampleLayout)
        );
        var lg = NullLogger<LayoutProvider>.Instance;
        var p = new MockPersistence();
        var provider = new LayoutProvider(fs, lg, p);

        provider.Reload();

        await Assert.That(provider.Current).IsNotNull();
        await Assert.That(provider.Current!.Name).IsEqualTo("ThunderClan Camp");
        await Assert.That(provider.Layouts).IsNotEmpty();
    }

    private const string ExampleLayout = """
        Name = "ThunderClan Camp"
        Author = "9volt"
        ColumnDefinitions = "*, 2, *"
        RowDefinitions = "0.5*, 2, *, 2, *"
        Window = { IsProjectExplorerOnLeft = true }
        [Video]
        IsVisible = true
        Column = 0
        Row = 0
        ColumnSpan = 1
        RowSpan = 3
        [Audio]
        IsVisible = true
        Column = 2
        Row = 0
        ColumnSpan = 1
        RowSpan = 1
        [Editor]
        IsVisible = true
        Column = 2
        Row = 2
        ColumnSpan = 1
        RowSpan = 1
        [Events]
        IsVisible = true
        Column = 0
        Row = 4
        ColumnSpan = 3
        RowSpan = 1
        [[Splitters]]
        IsVertical = false
        Column = 2
        Row = 1
        ColumnSpan = 1
        RowSpan = 1
        """;
}

file class MockPersistence : IPersistence
{
    private double _visualizationScaleX;
    private double _visualizationScaleY;
    private double _windowWidth;
    private double _windowHeight;
    private PersistentWindowState _windowState;
    public string LayoutName { get; set; } = "ThunderClan Camp";
    public bool UseColorRing { get; set; } = true;

    /// <inheritdoc />
    public double VisualizationScaleX
    {
        get => _visualizationScaleX;
        set => _visualizationScaleX = value;
    }

    /// <inheritdoc />
    public double VisualizationScaleY
    {
        get => _visualizationScaleY;
        set => _visualizationScaleY = value;
    }

    /// <inheritdoc />
    public string PlaygroundCs { get; set; } = string.Empty;

    /// <inheritdoc />
    public string PlaygroundJs { get; set; } = string.Empty;

    /// <inheritdoc />
    public IReadOnlyList<Uri> RecentDocuments { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<Uri> RecentProjects { get; } = [];

    /// <inheritdoc />
    public double WindowWidth
    {
        get => _windowWidth;
        set => _windowWidth = value;
    }

    /// <inheritdoc />
    public double WindowHeight
    {
        get => _windowHeight;
        set => _windowHeight = value;
    }

    /// <inheritdoc />
    public PersistentWindowState WindowState
    {
        get => _windowState;
        set => _windowState = value;
    }

    /// <inheritdoc />
    public void SetScaleForRes(int height, ScaleFactor scaleFactor) { }

    /// <inheritdoc />
    public ScaleFactor GetScaleForRes(int height)
    {
        return ScaleFactor.Default;
    }

    /// <inheritdoc />
    public void SetAudioTrackForVideo(string pathHash, int trackNumber) { }

    /// <inheritdoc />
    public int GetAudioTrackForVideo(string pathHash)
    {
        return -1;
    }

    /// <inheritdoc />
    public void AddRecentDocument(Uri document) { }

    /// <inheritdoc />
    public void ClearRecentDocuments() { }

    /// <inheritdoc />
    public void AddRecentProject(Uri project) { }

    /// <inheritdoc />
    public void ClearRecentProjects() { }

    public bool Save()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LayoutName)));
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
