// SPDX-License-Identifier: MPL-2.0

namespace AssCS.Tests;

public class GarbageManagerTests
{
    [Test]
    public async Task StringValue()
    {
        var gm = new GarbageManager();
        gm.Set("test", "test");

        await Assert.That(gm.GetString("test")).IsEqualTo("test");
    }

    [Test]
    public async Task DoubleValue()
    {
        var gm = new GarbageManager();
        gm.Set("test", 1.5d);

        await Assert.That(gm.GetDouble("test")).IsEqualTo(1.5d);
    }

    [Test]
    public async Task DecimalValue()
    {
        var gm = new GarbageManager();
        gm.Set("test", 1.5m);

        await Assert.That(gm.GetDecimal("test")).IsEqualTo(1.5m);
    }

    [Test]
    public async Task IntValue()
    {
        var gm = new GarbageManager();
        gm.Set("test", 1);

        await Assert.That(gm.GetInt("test")).IsEqualTo(1);
    }

    [Test]
    public async Task BoolValue()
    {
        var gm = new GarbageManager();
        gm.Set("test", true);

        await Assert.That(gm.GetBool("test")).IsTrue();
    }

    [Test]
    public async Task JsonValue()
    {
        int[] data = [1, 2, 3];
        var gm = new GarbageManager();
        gm.SetJson("test", data);

        await Assert.That(gm.GetJson<int[]>("test")).IsEquivalentTo(data);
    }

    [Test]
    public async Task LoadDefault()
    {
        var gm = new GarbageManager();
        gm.LoadDefault();

        await Assert.That(gm.Count).IsEqualTo(6);
    }

    [Test]
    public async Task Clear()
    {
        var gm = new GarbageManager();
        gm.LoadDefault();
        gm.Clear();

        await Assert.That(gm.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Contains()
    {
        var gm = new GarbageManager();
        await Assert.That(gm.Contains("test")).IsFalse();

        gm.Set("test", "test");
        await Assert.That(gm.Contains("test")).IsTrue();
    }
}
