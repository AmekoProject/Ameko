// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions.TestingHelpers;
using AssCS.IO;
using static TestingUtils.TestableUri;

namespace AssCS.Tests;

public class AssWriterTests
{
    [Test]
    public async Task Write()
    {
        var writer = new StringWriter();

        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var aw = new AssWriter(CreateDoc(), consumer);

        var result = aw.Write(writer);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var reader = new StringReader(writer.ToString());
        var recreation = new AssParser().Parse(reader);

        await Assert.That(recreation).IsNotNull();
        await Assert
            .That(
                recreation
                    .StyleManager.Get("Test")
                    .IsCongruentWith(CreateDoc().StyleManager.Get("Test"))
            )
            .IsTrue();
        await Assert
            .That(recreation.EventManager.Head.IsCongruentWith(CreateDoc().EventManager.Head))
            .IsTrue();
        await Assert.That(recreation.GarbageManager.GetAll().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Write_Export()
    {
        var writer = new StringWriter();
        var consumer = new ConsumerInfo("Test Suite", "1.0", "testsuite.com");
        var aw = new AssWriter(CreateDoc(), consumer);

        var result = aw.Write(writer, true);

        await Assert.That(result).IsTrue();

        // Validate the written file
        var reader = new StringReader(writer.ToString());

        var recreation = new AssParser().Parse(reader);

        await Assert.That(recreation).IsNotNull();
        await Assert
            .That(
                recreation
                    .StyleManager.Get("Test")
                    .IsCongruentWith(CreateDoc().StyleManager.Get("Test"))
            )
            .IsTrue();
        await Assert
            .That(recreation.EventManager.Head.IsCongruentWith(CreateDoc().EventManager.Head))
            .IsTrue();
        await Assert.That(recreation.GarbageManager.GetAll().Count).IsEqualTo(0);
    }

    private static Document CreateDoc()
    {
        var doc = new Document(false);
        var s = new Style(1)
        {
            Name = "Test",
            FontFamily = "Cooper Black",
            FontSize = 14,
            Alignment = 8,
        };
        var e1 = new Event(1) { Actor = "Joe", Text = "Mama" };

        doc.StyleManager.Add(s);
        doc.EventManager.AddFirst(e1);
        doc.GarbageManager.Set("test_garbage", 25m);

        return doc;
    }
}
