// SPDX-License-Identifier: MPL-2.0

using AssCS.Overrides;

namespace AssCS.Tests;

public class KaraokeTests
{
    [Test]
    public async Task Parse_EmptyEvent_Returns_EmptySyl()
    {
        var @event = CreateEventWithSyllables(string.Empty);
        var karaoke = new Karaoke2(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(@"{\k0}");
    }

    [Test]
    public async Task Parse_PlaintextEvent_Returns_Syl()
    {
        const string content = "I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo($@"{{\k0}}{content}");
    }

    [Test]
    public async Task Parse_Event_With_Syl_Returns_Syl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home";
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
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
        var karaoke = new Karaoke2(@event);
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
        var karaoke = new Karaoke2(@event);
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
        var karaoke = new Karaoke2(@event);
        await Assert.That(karaoke.Syllables.Count).IsEqualTo(1);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Blocks.Count).IsEqualTo(3);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
    }

    [Test]
    public async Task Split_Splits_PlainSyl()
    {
        const string syl1 = @"I'm whispering a lullaby ";
        const string syl2 = @"for you to come back home";
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
        karaoke.Split(0, 25);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[0].InnerText).IsEqualTo(syl1);
        await Assert.That(karaoke.Syllables[1].InnerText).IsEqualTo(syl2);
    }

    [Test]
    public async Task Split_Early_Splits_PlainSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
        karaoke.Split(0, 0);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(0);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(@"{\k0}");
        await Assert.That(karaoke.Syllables[1].Text).IsEqualTo(content);
    }

    [Test]
    public async Task Split_Late_Splits_PlainSyl()
    {
        const string content = @"{\k10}I'm whispering a lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
        karaoke.Split(0, 50);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(10);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(0);
        await Assert.That(karaoke.Syllables[0].Text).IsEqualTo(content);
        await Assert.That(karaoke.Syllables[1].Text).IsEqualTo(@"{\k0}");
    }

    [Test]
    public async Task Split_WithBlocks_Splits_Syl()
    {
        const string syl1 = @"I'm whispering a lullaby ";
        const string syl2 = @"for you to come back home";
        const string content =
            @"{\k10}I'm whispering a {\xshad100}lullaby for you to come back home"; // 50 inner chars
        var @event = CreateEventWithSyllables(content);
        var karaoke = new Karaoke2(@event);
        karaoke.Split(0, 25);

        await Assert.That(karaoke.Syllables.Count).IsEqualTo(2);
        await Assert.That(karaoke.Syllables[0].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[1].Duration).IsEqualTo(5);
        await Assert.That(karaoke.Syllables[0].Blocks.Count).IsEqualTo(3);
        await Assert.That(karaoke.Syllables[0].InnerText).IsEqualTo(syl1);
        await Assert.That(karaoke.Syllables[1].InnerText).IsEqualTo(syl2);
    }

    // Original implementation below

    [Test]
    public async Task Text_ReturnsSyllableTextWithKTags()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k10}Hi {\k20}there");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);
        await Assert.That(karaoke.Text).IsEqualTo(@"{\k10}Hi {\k20}there");
    }

    [Test]
    public async Task TagType_ReturnsCorrectTag()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\kf10}One");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);
        await Assert.That(karaoke.TagType).IsEqualTo(@"kf");
    }

    [Test]
    public async Task TagType_Set_UpdatesAllSyllables()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k10}One {\k10}Two");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);

        karaoke.TagType = @"kf";
        await Assert.That(karaoke.Text).IsEqualTo(@"{\kf10}One {\kf10}Two");
    }

    [Test]
    public async Task AddSplit_SplitsSyllable()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k20}HelloWorld");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);

        karaoke.AddSplit(0, 5); // Evenly split "HelloWorld" to "Hello" + "World"
        await Assert.That(karaoke.Text).IsEqualTo(@"{\k10}Hello{\k10}World");
    }

    [Test]
    public async Task RemoveSplit_JoinsSyllables()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k20}Hello {\k20}World");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);

        karaoke.RemoveSplit(1);
        await Assert.That(karaoke.Text).IsEqualTo(@"{\k40}Hello World");
    }

    [Test]
    public async Task SetStartTime_AdjustsStartProperly()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k10}Hi {\k10}there");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);

        var newTime = evt.Start + Time.FromCentis(5);
        karaoke.SetStartTime(1, newTime);
        await Assert.That(karaoke.Text).IsEqualTo(@"{\k5}Hi {\k15}there");
    }

    [Test]
    public async Task SetLineTimes_TruncatesCorrectly()
    {
        var karaoke = new Karaoke();
        var evt = CreateEventWithSyllables(@"{\k10}Hi {\k10}there");
        karaoke.SetLine(evt, autoSplit: false, normalize: false);

        var start = evt.Start + Time.FromMillis(5);
        var end = evt.End - Time.FromMillis(5);
        karaoke.SetLineTimes(start, end);

        await Assert.That(karaoke.Text).IsEqualTo(@"{\k10}Hi {\k490}there");
    }

    private static Event CreateEventWithSyllables(string content)
    {
        return new Event(1) { Text = content };
    }
}
