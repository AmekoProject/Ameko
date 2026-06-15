// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions.TestingHelpers;
using Holo.Configuration;
using Holo.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holo.Tests;

public class PersistenceTests
{
    [Test]
    public async Task Constructor()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Persistence>.Instance;
        var p = new Persistence(fs, lg);

        await Assert.That(p.LayoutName).IsEqualTo("Default");
        await Assert.That(p.UseColorRing).IsFalse();
    }

    [Test]
    public async Task Parse_NotExists()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Persistence>.Instance;
        var p = Persistence.Parse(fs, lg);

        await Assert.That(p.LayoutName).IsEqualTo("Default");
        await Assert.That(p.UseColorRing).IsFalse();
    }

    [Test]
    public async Task Parse_Exists()
    {
        var fs = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                { Paths.Persistence.LocalPath, new MockFileData(ExamplePersistence) },
            }
        );
        var lg = NullLogger<Persistence>.Instance;
        var p = Persistence.Parse(fs, lg);

        await Assert.That(p.LayoutName).IsEqualTo("Upside Down");
        await Assert.That(p.UseColorRing).IsTrue();
    }

    [Test]
    public async Task Save_Exists()
    {
        var fs = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                { Paths.Persistence.LocalPath, new MockFileData(ExamplePersistence) },
            }
        );
        var lg = NullLogger<Persistence>.Instance;
        var p = new Persistence(fs, lg) { LayoutName = "Wayside School" };

        var result = p.Save();

        await Assert.That(result).IsTrue();
        await Assert.That(fs.FileExists(Paths.Persistence.LocalPath)).IsTrue();
    }

    [Test]
    public async Task Save_NotExists()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Persistence>.Instance;
        var p = new Persistence(fs, lg) { LayoutName = "Wayside School" };

        var result = p.Save();

        await Assert.That(result).IsTrue();
        await Assert.That(fs.FileExists(Paths.Persistence.LocalPath)).IsTrue();
    }

    private const string ExamplePersistence = """
        {
            "Version": 1,
            "LayoutName": "Upside Down",
            "UseColorRing": true,
            "VisualizationScaleX": 8.5,
            "VisualizationScaleY": 3,
            "PlaygroundCs": "",
            "PlaygroundJs": "",
            "ScalesForRes": {},
            "AudioTrackForVideo": {},
            "RecentDocuments": [],
            "RecentProjects": [],
            "WindowWidth": 1280.0,
            "WindowHeight": 720.0,
            "WindowState": "Normal"
        }
        """;
}
