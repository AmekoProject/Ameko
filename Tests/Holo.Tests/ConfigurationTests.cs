// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions.TestingHelpers;
using Holo.IO;
using Holo.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holo.Tests;

public class ConfigurationTests
{
    [Test]
    public async Task Constructor()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Configuration.Configuration>.Instance;
        var cfg = new Configuration.Configuration(fs, lg);

        await Assert.That(cfg.Cps).IsEqualTo<uint>(18);
        await Assert.That(cfg.UseSoftLinebreaks).IsFalse();
        await Assert.That(cfg.AutosaveEnabled).IsTrue();
        await Assert.That(cfg.AutosaveInterval).IsEqualTo<uint>(60);
    }

    [Test]
    public async Task Parse_NotExists()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Configuration.Configuration>.Instance;
        var cfg = Configuration.Configuration.Parse(fs, lg);

        await Assert.That(cfg).IsNotNull();
        await Assert.That(cfg.Cps).IsEqualTo<uint>(18);
        await Assert.That(cfg.UseSoftLinebreaks).IsFalse();
        await Assert.That(cfg.AutosaveEnabled).IsTrue();
        await Assert.That(cfg.AutosaveInterval).IsEqualTo<uint>(60);
    }

    [Test]
    public async Task Parse_Exists()
    {
        var fs = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                { Paths.Configuration.LocalPath, new MockFileData(ExampleConfiguration) },
            }
        );
        var lg = NullLogger<Configuration.Configuration>.Instance;
        var cfg = Configuration.Configuration.Parse(fs, lg);

        await Assert.That(cfg).IsNotNull();
        await Assert.That(cfg.Cps).IsEqualTo<uint>(12);
        await Assert.That(cfg.CpsIncludesWhitespace).IsFalse();
        await Assert.That(cfg.CpsIncludesPunctuation).IsFalse();
        await Assert.That(cfg.UseSoftLinebreaks).IsTrue();
        await Assert.That(cfg.AutosaveEnabled).IsFalse();
        await Assert.That(cfg.AutosaveInterval).IsEqualTo<uint>(120);
        await Assert.That(cfg.LineWidthIncludesWhitespace).IsTrue();
        await Assert.That(cfg.LineWidthIncludesPunctuation).IsTrue();
        await Assert.That(cfg.Theme).IsEqualTo(Theme.Light);
    }

    [Test]
    public async Task Save_Exists()
    {
        var fs = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                { Paths.Configuration.LocalPath, new MockFileData(ExampleConfiguration) },
            }
        );
        var lg = NullLogger<Configuration.Configuration>.Instance;
        var cfg = new Configuration.Configuration(fs, lg) { Cps = 12 };

        var result = cfg.Save();

        await Assert.That(result).IsTrue();
        await Assert.That(fs.FileExists(Paths.Configuration.LocalPath)).IsTrue();
    }

    [Test]
    public async Task Save_NotExists()
    {
        var fs = new MockFileSystem();
        var lg = NullLogger<Configuration.Configuration>.Instance;
        var cfg = new Configuration.Configuration(fs, lg) { Cps = 12 };

        var result = cfg.Save();

        await Assert.That(result).IsTrue();
        await Assert.That(fs.FileExists(Paths.Configuration.LocalPath)).IsTrue();
    }

    private const string ExampleConfiguration = """
        {
            "Version": 1,
            "Cps": 12,
            "CpsIncludesWhitespace": false,
            "CpsIncludesPunctuation": false,
            "UseSoftLinebreaks": true,
            "AutosaveEnabled": false,
            "SaveFrames": "WithSubtitles",
            "DefaultLayer": 0,
            "AutosaveInterval": 120,
            "AutoloadAudioTracks": true,
            "LineWidthIncludesWhitespace": true,
            "LineWidthIncludesPunctuation": true,
            "RichPresenceLevel": "Enabled",
            "Culture": "en-US",
            "SpellcheckCulture": "en_US",
            "Theme": "light",
            "PropagateFields": "NonText",
            "GridPadding": 2,
            "EditorFontSize": 0.0,
            "GridFontSize": 0.0,
            "ReferenceFontSize": 0.0,
            "RepositoryUrls": [],
            "ScriptMenuOverrides": {},
            "Timing": {
                "LeadIn": 0,
                "LeadOut": 0,
                "SnapStartEarlierThreshold": 0,
                "SnapStartLaterThreshold": 0,
                "SnapEndEarlierThreshold": 0,
                "SnapEndLaterThreshold": 0
            }
        }
        """;
}
