// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions.TestingHelpers;
using AssCS.IO;
using static TestingUtils.TestableUri;

namespace AssCS.Tests;

public class TxtWriterTests
{
    [Test]
    public async Task Write()
    {
        var writer = new StringWriter();

        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var tw = new TxtWriter(consumer);

        var result = tw.Write(CreateDoc(), writer);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var reader = new StringReader(writer.ToString());

        var lines = (await reader.ReadToEndAsync()).Split('\n');
        await Assert.That(lines.Length).IsEqualTo(4 + 1); // Empty line at the end, so +1
        await Assert.That(lines[1]).StartsWith("Joe: ");
        await Assert.That(lines[2]).StartsWith("# Joe: ");
    }

    [Test]
    public async Task Write_NoComments()
    {
        var writer = new StringWriter();
        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var tw = new TxtWriter(consumer, includeComments: false);

        var result = tw.Write(CreateDoc(), writer);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var contents = writer.ToString();

        var lines = contents.Split('\n');
        await Assert.That(lines.Length).IsEqualTo(3 + 1);
        await Assert.That(lines[1]).StartsWith("Joe: ");
        await Assert.That(lines[2]).StartsWith("Tim: ");
    }

    [Test]
    public async Task Write_NoActors()
    {
        var writer = new StringWriter();
        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var tw = new TxtWriter(consumer, includeActors: false);

        var result = tw.Write(CreateDoc(), writer);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var contents = writer.ToString();

        var lines = contents.Split('\n');
        await Assert.That(lines.Length).IsEqualTo(4 + 1);
        await Assert.That(lines[1]).StartsWith("Mama");
        await Assert.That(lines[2]).StartsWith("# Mama");
    }

    [Test]
    public async Task Write_NoComments_NoActors()
    {
        var writer = new StringWriter();
        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var tw = new TxtWriter(consumer, includeComments: false, includeActors: false);

        var result = tw.Write(CreateDoc(), writer);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var contents = writer.ToString();

        var lines = contents.Split('\n');
        await Assert.That(lines.Length).IsEqualTo(3 + 1);
        await Assert.That(lines[1]).StartsWith("Mama");
        await Assert.That(lines[2]).StartsWith("Bits SO COOL");
    }

    [Test]
    public async Task StripNewlines_Big_NoSpace()
    {
        const string input = @"This is the first line.\NThis is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Big_LeftSpace()
    {
        const string input = @"This is the first line. \NThis is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Big_RightSpace()
    {
        const string input = @"This is the first line.\N This is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Big_ManySpace()
    {
        const string input = @"This is the first line.    \N  This is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Small_NoSpace()
    {
        const string input = @"This is the first line.\nThis is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Small_LeftSpace()
    {
        const string input = @"This is the first line. \nThis is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Small_RightSpace()
    {
        const string input = @"This is the first line.\n This is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task StripNewlines_Small_ManySpace()
    {
        const string input = @"This is the first line.    \n  This is the second line.";
        const string expected = @"This is the first line. This is the second line.";

        await Assert.That(TxtWriter.StripNewlines(input)).IsEqualTo(expected);
    }

    private static Document CreateDoc()
    {
        var doc = new Document(false);
        var e1 = new Event(1) { Actor = "Joe", Text = "Mama" };
        var e2 = new Event(2)
        {
            Actor = "Joe",
            Text = "Mama",
            IsComment = true,
        };
        var e3 = new Event(3) { Actor = "Tim", Text = "Bits\\NSO COOL!" };

        doc.EventManager.AddLast([e1, e2, e3]);

        return doc;
    }
}
