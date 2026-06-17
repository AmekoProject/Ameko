// SPDX-License-Identifier: MPL-2.0

using Holo.Media;

namespace Holo.Tests;

public class VideoInfoTests
{
    private static VideoInfo Info =>
        new(
            "video.mkv",
            12,
            new Rational { Numerator = 16, Denominator = 9 },
            [0, 42, 83, 125, 167, 209, 250, 292, 334, 375, 417, 459],
            [21, 63, 104, 146, 188, 230, 271, 313, 355, 396, 438, 480],
            [42, 41, 42, 42, 42, 41, 42, 42, 41, 42, 42, 41],
            [0],
            1920,
            1080
        );

    [Test]
    public async Task FrameFromMillis_Exact()
    {
        var info = Info;
        var frame = info.FrameFromMillis(125);
        await Assert.That(frame).IsEqualTo(3);
    }

    [Test]
    public async Task FrameFromMillis_Between()
    {
        var info = Info;
        var frame = info.FrameFromMillis(126);
        await Assert.That(frame).IsEqualTo(3);
    }

    [Test]
    public async Task FrameFromMillis_OutOfRange_Small()
    {
        var info = Info;
        var frame = info.FrameFromMillis(-5);
        await Assert.That(frame).IsEqualTo(0);
    }

    [Test]
    public async Task FrameFromMillis_OutOfRange_Large()
    {
        var info = Info;
        var frame = info.FrameFromMillis(5000);
        await Assert.That(frame).IsEqualTo(11);
    }

    [Test]
    public async Task MillisecondsFromFrame()
    {
        var info = Info;
        var ms = info.MillisecondsFromFrame(3);
        await Assert.That(ms).IsEqualTo(125);
    }

    [Test]
    public async Task MillisecondsFromFrame_OutOfRange_Small()
    {
        var info = Info;
        var ms = info.MillisecondsFromFrame(-5);
        await Assert.That(ms).IsEqualTo(0);
    }

    [Test]
    public async Task MillisecondsFromFrame_OutOfRange_Large()
    {
        var info = Info;
        var ms = info.MillisecondsFromFrame(5000);
        await Assert.That(ms).IsEqualTo(459);
    }
}
