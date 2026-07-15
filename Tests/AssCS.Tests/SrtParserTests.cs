// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions.TestingHelpers;
using AssCS.IO;
using static TestingUtils.TestableUri;

namespace AssCS.Tests;

public class SrtParserTests
{
    [Test]
    public async Task Parse()
    {
        var reader = new StringReader(File1);

        var sp = new SrtParser();

        var doc = sp.Parse(reader);

        await Assert.That(doc).IsNotNull();
        await Assert.That(doc.StyleManager.Styles.Count).IsEqualTo(1);
        await Assert.That(doc.EventManager.Events.Count).IsEqualTo(5);

        var last = doc.EventManager.Tail;
        await Assert.That(last).IsNotNull();
        await Assert.That(last.Start.TotalMilliseconds).IsEqualTo(17260);
        await Assert.That(last.End.TotalMilliseconds).IsEqualTo(18100);
        await Assert.That(last.Text).IsEqualTo("（風太郎）あっ");

        var three = doc.EventManager.Get(2);
        await Assert.That(three).IsNotNull();
        await Assert.That(three.Text).IsEqualTo(@"（スタッフ）\N起きてください 新郎様");
    }

    [Test]
    public async Task ParseWithTags()
    {
        var reader = new StringReader(File2);

        var sp = new SrtParser();

        var doc = sp.Parse(reader);

        await Assert.That(doc).IsNotNull();
        await Assert.That(doc.EventManager.Events.Count).IsEqualTo(1);
        var last = doc.EventManager.Tail;

        await Assert
            .That(last.Text)
            .IsEqualTo(
                @"Hey {\i1}dear {\b1}my{\b0\i0} {\u1}friends{\u0}\N{\fnArial\fs32\c&H4400FF&}So cool{\fn\fs\c}"
            );
    }

    [Test]
    public async Task ExpectedSubtitleIndex()
    {
        var reader = new StringReader("Hello");

        var sp = new SrtParser();

        await Assert
            .That(() => sp.Parse(reader))
            .Throws<FormatException>()
            .WithMessage("Expected subtitle index at line 1");
    }

    [Test]
    public async Task ExpectedTimestamps()
    {
        var reader = new StringReader("1\nHello");

        var sp = new SrtParser();

        await Assert
            .That(() => sp.Parse(reader))
            .Throws<FormatException>()
            .WithMessage("Expected timestamps at line 2");
    }

    [Test]
    public async Task UnexpectedEndOfFile()
    {
        var reader = new StringReader("1\n00:00:00,620 --> 00:00:05,630");

        var sp = new SrtParser();

        await Assert
            .That(() => sp.Parse(reader))
            .Throws<FormatException>()
            .WithMessage("Unexpected end of SRT file");
    }

    private const string File1 = """
        1
        00:00:00,620 --> 00:00:05,630
        （教会の鐘の音）

        2
        00:00:07,130 --> 00:00:09,010
        （風太郎(ふうたろう)）夢を見ていた

        3
        00:00:11,420 --> 00:00:13,680
        （スタッフ）
        起きてください 新郎様

        4
        00:00:13,800 --> 00:00:16,260
        新婦様の準備が整いましたよ

        5
        00:00:17,260 --> 00:00:18,100
        （風太郎）あっ
        """;

    private const string File2 = """
        1
        00:00:00,620 --> 00:00:05,630
        Hey <i>dear <b>my</b></i> <u>friends</u>
        <font face="Arial" size="32" color='#FF0044'>So cool</font>
        """;
}
