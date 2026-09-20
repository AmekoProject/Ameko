// SPDX-License-Identifier: MPL-2.0

using AssCS.Overrides;

namespace AssCS.Tests;

public class KaraokeTests
{
    [Test]
    public async Task Parse_EmptyEvent_Returns_EmptySyl()
    {
        var @event = CreateEventWithSyllables(string.Empty);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(@"{\k0}");
    }

    [Test]
    public async Task Parse_PlaintextEvent_Returns_Syl()
    {
        const string content = "I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo($@"{{\k0}}{content}");
    }

    [Test]
    public async Task Parse_Event_With_Syl_Returns_Syl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
    }

    [Test]
    public async Task Parse_Event_With_Syls_Returns_Syls()
    {
        const string syl1 = @"{\k10}I'm whispering a lullaby ";
        const string syl2 = @"{\k25}for you to come back home";
        const string content = syl1 + syl2;

        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(25);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(syl1);
        await Assert.That(karaoke.Syllables[1].Text).IsEqualTo(syl2);
    }

    [Test]
    public async Task Parse_Event_With_Syl_And_Tags_Returns_Syl()
    {
        const string content =
            @"{\k10\fs150\fnArial}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Tags.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
    }

    [Test]
    public async Task Parse_Event_With_Syl_And_Blocks_Returns_Syl()
    {
        const string content =
            @"{\k10}I'm whispering a {\fscx105}lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Blocks.Count).IsEqualTo(3);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
    }

    [Test]
    public async Task AddSplit_Splits_PlainSyl()
    {
        const string syl1 = @"I'm whispering a lullaby ";
        const string syl2 = @"for you to come back home";
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AddSplit(0, 25);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[0].InnerText).IsEqualTo(syl1);
        await Assert.That(karaoke.Syllables[1].InnerText).IsEqualTo(syl2);
    }

    [Test]
    public async Task AddSplit_Early_Splits_PlainSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AddSplit(0, 0);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(0);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(@"{\k0}");
        await Assert.That(karaoke.Syllables[1].Text).IsEqualTo(content);
    }

    [Test]
    public async Task AddSplit_Late_Splits_PlainSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AddSplit(0, 50);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(0);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
        await Assert.That(karaoke.Syllables[1].Text).IsEqualTo(@"{\k0}");
    }

    [Test]
    public async Task AddSplit_WithBlocks_Splits_Syl()
    {
        const string syl1 = @"I'm whispering a lullaby ";
        const string syl2 = @"for you to come back home";
        const string content =
            @"{\k10}I'm whispering a {\xshad100}lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AddSplit(0, 25);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[0].Blocks.Count).IsEqualTo(3);
        await Assert.That(karaoke.Syllables[0].InnerText).IsEqualTo(syl1);
        await Assert.That(karaoke.Syllables[1].InnerText).IsEqualTo(syl2);
    }

    [Test]
    public async Task RemoveSplit_Joins_PlainSyl()
    {
        const string split = @"{\k5}I'm whispering a lullaby {\k5}for you to come back home";
        const string joined = @"I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(split);
        var karaoke = new Karaoke(@event);
        karaoke.RemoveSplit(1);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].InnerText).IsEqualTo(joined);
    }

    [Test]
    public async Task RemoveSplit_SylZero_Is_NoOp()
    {
        const string split = @"{\k5}I'm whispering a lullaby {\k5}for you to come back home";
        var @event = CreateEventWithSyllables(split);
        var karaoke = new Karaoke(@event);
        karaoke.RemoveSplit(0);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveSplit_Joins_Syl_And_KeepsTags()
    {
        const string split = @"{\k5}I'm whispering a lullaby {\k5\b1}for you to come back home";
        const string joined = @"{\k10}I'm whispering a lullaby {\b1}for you to come back home";
        var @event = CreateEventWithSyllables(split);
        var karaoke = new Karaoke(@event);
        karaoke.RemoveSplit(1);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(joined);
    }

    [Test]
    public async Task RemoveSplit_Joins_Syl_And_KeepsBlocks()
    {
        const string split = @"{\k5}I'm whispering a lullaby {\k5}for you {\b1}to come back home";
        const string joined = @"{\k10}I'm whispering a lullaby for you {\b1}to come back home";
        var @event = CreateEventWithSyllables(split);
        var karaoke = new Karaoke(@event);
        karaoke.RemoveSplit(1);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(joined);
    }

    [Test]
    public async Task Normalize_Syl_Returns_NormalizedSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Normalize();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(duration);
    }

    [Test]
    public async Task Normalize_EqualSyls_Returns_NormalizedSyls()
    {
        const string content = @"{\k10}I'm whispering a lullaby {\k25}for you to come back home";

        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Normalize();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(duration / 2);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(duration / 2);
    }

    [Test]
    public async Task Normalize_UnequalSyls_Returns_NormalizedSyls()
    {
        const string content = @"{\k10}I'm whispe{\k25}ring a lullaby for you to come back home";

        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Normalize();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo((long)(duration * 0.2));
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo((long)(duration * 0.8));
    }

    [Test]
    public async Task Distribute_Syl_Returns_DistributedSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Distribute();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(duration);
    }

    [Test]
    public async Task Distribute_EqualSyls_Returns_DistributedSyls()
    {
        const string content = @"{\k10}I'm whispering a lullaby {\k25}for you to come back home";

        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Distribute();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(duration / 2);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(duration / 2);
    }

    [Test]
    public async Task Distribute_UnequalSyls_Returns_DistributedSyls()
    {
        const string content = @"{\k10}I'm whispe{\k25}ring a lullaby for you to come back home";

        var @event = CreateEventWithSyllables(content);
        var duration = (@event.End - @event.Start).TotalCentiseconds;
        var karaoke = new Karaoke(@event);
        karaoke.Distribute();

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(duration / 2);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(duration / 2);
    }

    [Test]
    public async Task AutoSplit_EmptyEvent_Returns_EmptySyl()
    {
        var @event = CreateEventWithSyllables(string.Empty);
        var karaoke = new Karaoke(@event);
        karaoke.AutoSplit();
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(@"{\k0}");
    }

    [Test]
    public async Task AutoSplit_PlaintextEvent_Returns_Syls()
    {
        const string content = "I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AutoSplit();
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(10);
    }

    [Test]
    public async Task AutoSplit_Event_With_Syl_Returns_Syls()
    {
        const string content = @"{\k6}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AutoSplit();
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(0);
    }

    [Test]
    public async Task AutoSplit_Event_With_Blocks_Returns_Syls()
    {
        const string content = @"I'm {\b1}whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke(@event);
        karaoke.AutoSplit();
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[1].Blocks.Count).IsEqualTo(2);
    }

    private static Event CreateEventWithSyllables(string content)
    {
        return new Event(1) { Text = content };
    }
}
