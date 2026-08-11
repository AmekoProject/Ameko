// SPDX-License-Identifier: MPL-2.0

using AssCS.Overrides;
using AssCS.Overrides.Blocks;

namespace AssCS.Tests;

public class OverrideBlockTests
{
    #region General

    [Test]
    public async Task EmptyBlock_HasNoTags()
    {
        const string body = @"";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Block_WithNoTags_HasNoTags()
    {
        const string body = @"Johnny Rockets Hamburgers";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Block_WithSimpleTag_HasTag()
    {
        const string body = @"\fscx100";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fscx = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.FscX>();

        await Assert.That(fscx).IsNotNull();
        await Assert.That(fscx.Value).IsEqualTo(100);
    }

    [Test]
    public async Task Block_WithComplexTag_HasTag()
    {
        const string body = @"\pos(100,200)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var pos = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Pos>();

        await Assert.That(pos).IsNotNull();
        await Assert.That(pos.X).IsEqualTo(100);
        await Assert.That(pos.Y).IsEqualTo(200);
    }

    [Test]
    public async Task Block_WithMultipleTags_HasTags()
    {
        const string body = @"\fscx100\pos(100,200)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(2);
        var fscx = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var pos = await Assert.That(block.Tags[1]).IsTypeOf<OverrideTag.Pos>();

        await Assert.That(fscx).IsNotNull();
        await Assert.That(pos).IsNotNull();
        await Assert.That(fscx.Value).IsEqualTo(100);
        await Assert.That(pos.X).IsEqualTo(100);
        await Assert.That(pos.Y).IsEqualTo(200);
    }

    [Test]
    public async Task Block_WithTagAndWhitespace_HasTag()
    {
        const string body = @"   \   fscx   100";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fscx = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.FscX>();

        await Assert.That(fscx).IsNotNull();
        await Assert.That(fscx.Value).IsEqualTo(100);
    }

    [Test]
    public async Task Block_WithMultipleTagsAndWhitespace_HasTags()
    {
        const string body = @"   \   fscx   100   \   pos   (   100   ,   200   )";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(2);
        var fscx = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var pos = await Assert.That(block.Tags[1]).IsTypeOf<OverrideTag.Pos>();

        await Assert.That(fscx).IsNotNull();
        await Assert.That(pos).IsNotNull();
        await Assert.That(fscx.Value).IsEqualTo(100);
        await Assert.That(pos.X).IsEqualTo(100);
        await Assert.That(pos.Y).IsEqualTo(200);
    }

    [Test]
    public async Task Block_WithUnneededParentheses_HasTag()
    {
        const string body = @"\i(1)";
        const string sanitized = @"\i1";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.I>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsTrue();
        await Assert.That(i.ToString()).IsEqualTo(sanitized);
    }

    [Test]
    public async Task Block_WithInlineFxTag_HasTag()
    {
        const string body = @"\-flag";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fx = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Unknown>();

        await Assert.That(fx).IsNotNull();
        await Assert.That(fx.Name).IsEqualTo("-flag");
        await Assert.That(fx.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Block_WithMixedInlineFxTag_HasTags()
    {
        const string body = @"\kf12\-flag";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(2);
        var kf = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Kf>();
        var fx = await Assert.That(block.Tags[1]).IsTypeOf<OverrideTag.Unknown>();

        await Assert.That(kf).IsNotNull();
        await Assert.That(fx).IsNotNull();

        await Assert.That(kf.Duration).IsEqualTo(12);
        await Assert.That(fx.Name).IsEqualTo("-flag");
    }

    [Test]
    public async Task Block_WithProposedInlineExtradataTag_HasTag()
    {
        const string body = @"\~aegi~ambientquad(1,2,3,4)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var tag = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Unknown>();

        await Assert.That(tag).IsNotNull();
        await Assert.That(tag.Name).IsEqualTo("~aegi~ambientquad");

        await Assert.That(tag.Args.Length).IsEqualTo(4);
        await Assert.That(tag.Args[0]).IsEqualTo("1");
        await Assert.That(tag.Args[1]).IsEqualTo("2");
        await Assert.That(tag.Args[2]).IsEqualTo("3");
        await Assert.That(tag.Args[3]).IsEqualTo("4");

        await Assert.That(tag.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Block_WithInlineCodeAndVariables_KeepsVariables()
    {
        const string body = @"\t($sstart,!$sstart+$sdur*0.3!,\c!gc(2)!)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.RawT1).IsEqualTo("$sstart");
        await Assert.That(t.RawT2).IsEqualTo("!$sstart+$sdur*0.3!");

        await Assert.That(t.Tags.Count).IsEqualTo(1);
        var c = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.C>();

        await Assert.That(c).IsNotNull();
        await Assert.That(c.RawValue).IsEqualTo("!gc(2)!");

        await Assert.That(t.ToString()).IsEqualTo(body);
    }

    #endregion General

    #region Boolean Tags

    [Test]
    public async Task Italic_Enabled()
    {
        const string body = @"\i1";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.I>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsTrue();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Italic_Disabled()
    {
        const string body = @"\i0";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.I>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsFalse();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Italic_Empty()
    {
        const string body = @"\i";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.I>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsNull();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Bold_Enabled()
    {
        const string body = @"\b1";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var b = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.B>();

        await Assert.That(b).IsNotNull();
        await Assert.That(b.Value).IsEqualTo(1);
        await Assert.That(b.BoolValue).IsTrue();
        await Assert.That(b.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Bold_Disabled()
    {
        const string body = @"\b0";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var b = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.B>();

        await Assert.That(b).IsNotNull();
        await Assert.That(b.Value).IsEqualTo(0);
        await Assert.That(b.BoolValue).IsFalse();
        await Assert.That(b.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Bold_Weighted()
    {
        const string body = @"\b100";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var b = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.B>();

        await Assert.That(b).IsNotNull();
        await Assert.That(b.Value).IsEqualTo(100);
        await Assert.That(b.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Bold_Empty()
    {
        const string body = @"\b";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var b = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.B>();

        await Assert.That(b).IsNotNull();
        await Assert.That(b.Value).IsNull();
        await Assert.That(b.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Underline_Enabled()
    {
        const string body = @"\u1";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.U>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsTrue();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Underline_Disabled()
    {
        const string body = @"\u0";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.U>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsFalse();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Underline_Empty()
    {
        const string body = @"\u";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.U>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsNull();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Strikethrough_Enabled()
    {
        const string body = @"\s1";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.S>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsTrue();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Strikethrough_Disabled()
    {
        const string body = @"\s0";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.S>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsFalse();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Strikethrough_Empty()
    {
        const string body = @"\s";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var i = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.S>();

        await Assert.That(i).IsNotNull();
        await Assert.That(i.Value).IsNull();
        await Assert.That(i.ToString()).IsEqualTo(body);
    }

    #endregion Boolean Tags

    #region Font Size

    [Test]
    public async Task FontSize_Integer()
    {
        const string body = @"\fs72";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fs = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fs>();

        await Assert.That(fs).IsNotNull();
        await Assert.That(fs.Variant).IsEqualTo(OverrideTag.Fs.FsVariant.Absolute);
        await Assert.That(fs.Value).IsEqualTo(72);

        await Assert.That(fs.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task FontSize_Decimal()
    {
        const string body = @"\fs72.5";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fs = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fs>();

        await Assert.That(fs).IsNotNull();
        await Assert.That(fs.Variant).IsEqualTo(OverrideTag.Fs.FsVariant.Absolute);
        await Assert.That(fs.Value).IsEqualTo(72.5);

        await Assert.That(fs.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task FontSize_RelativeIncrease()
    {
        const string body = @"\fs+10";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fs = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fs>();

        await Assert.That(fs).IsNotNull();
        await Assert.That(fs.Variant).IsEqualTo(OverrideTag.Fs.FsVariant.Relative);
        await Assert.That(fs.Value).IsEqualTo(10);

        await Assert.That(fs.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task FontSize_RelativeDecrease()
    {
        const string body = @"\fs-10";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var fs = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fs>();

        await Assert.That(fs).IsNotNull();
        await Assert.That(fs.Variant).IsEqualTo(OverrideTag.Fs.FsVariant.Relative);
        await Assert.That(fs.Value).IsEqualTo(-10);

        await Assert.That(fs.ToString()).IsEqualTo(body);
    }

    #endregion Font Size

    #region Transform

    [Test]
    public async Task Transform_1Param()
    {
        const string body = @"\t(\fscx120\fscy400)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Variant).IsEqualTo(OverrideTag.T.TransformVariant.BlockOnly);

        await Assert.That(t.Tags.Count).IsEqualTo(2);

        var fscx = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var fscy = await Assert.That(t.Tags[1]).IsTypeOf<OverrideTag.FscY>();

        await Assert.That(fscx!.Value).IsEqualTo(120);
        await Assert.That(fscy!.Value).IsEqualTo(400);
    }

    [Test]
    public async Task Transform_2Param()
    {
        const string body = @"\t(1.5,\fscx120\fscy400)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Variant).IsEqualTo(OverrideTag.T.TransformVariant.AccelerationOnly);

        await Assert.That(t.Acceleration).IsEqualTo(1.5);
        await Assert.That(t.Tags.Count).IsEqualTo(2);

        var fscx = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var fscy = await Assert.That(t.Tags[1]).IsTypeOf<OverrideTag.FscY>();

        await Assert.That(fscx!.Value).IsEqualTo(120);
        await Assert.That(fscy!.Value).IsEqualTo(400);
    }

    [Test]
    public async Task Transform_3Param()
    {
        const string body = @"\t(10,40,\fscx120\fscy400)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Variant).IsEqualTo(OverrideTag.T.TransformVariant.TimeOnly);

        await Assert.That(t.T1).IsEqualTo(10);
        await Assert.That(t.T2).IsEqualTo(40);
        await Assert.That(t.Tags.Count).IsEqualTo(2);

        var fscx = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var fscy = await Assert.That(t.Tags[1]).IsTypeOf<OverrideTag.FscY>();

        await Assert.That(fscx!.Value).IsEqualTo(120);
        await Assert.That(fscy!.Value).IsEqualTo(400);
    }

    [Test]
    public async Task Transform_4Param()
    {
        const string body = @"\t(10,40,1.5,\fscx120\fscy400)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Variant).IsEqualTo(OverrideTag.T.TransformVariant.Full);

        await Assert.That(t.T1).IsEqualTo(10);
        await Assert.That(t.T2).IsEqualTo(40);
        await Assert.That(t.Acceleration).IsEqualTo(1.5);
        await Assert.That(t.Tags.Count).IsEqualTo(2);

        var fscx = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.FscX>();
        var fscy = await Assert.That(t.Tags[1]).IsTypeOf<OverrideTag.FscY>();

        await Assert.That(fscx!.Value).IsEqualTo(120);
        await Assert.That(fscy!.Value).IsEqualTo(400);
    }

    [Test]
    public async Task Transform_RectClip()
    {
        const string body = @"\t(10,40,1.5,\clip(0,20,100,120))";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Tags.Count).IsEqualTo(1);

        var clip = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.Clip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.X0).IsEqualTo(0);
        await Assert.That(clip.Y0).IsEqualTo(20);
        await Assert.That(clip.X1).IsEqualTo(100);
        await Assert.That(clip.Y1).IsEqualTo(120);
    }

    [Test]
    public async Task Transform_Nested()
    {
        const string body = @"\t(\alpha1\t(\alpha2))";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Tags.Count).IsEqualTo(2);

        var alpha1 = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.Alpha>();
        var inner = await Assert.That(t.Tags[1]).IsTypeOf<OverrideTag.T>();

        await Assert.That(alpha1).IsNotNull();
        await Assert.That(alpha1.RawValue).IsEqualTo("1");

        await Assert.That(inner).IsNotNull();
        await Assert.That(inner.Tags.Count).IsEqualTo(1);
        var alpha2 = await Assert.That(inner.Tags[0]).IsTypeOf<OverrideTag.Alpha>();
        await Assert.That(alpha2).IsNotNull();
        await Assert.That(alpha2.RawValue).IsEqualTo("2");
    }

    [Test]
    public async Task Transform_Nested_RectClip()
    {
        const string body = @"\t(\t(\clip(0,20,100,120)))";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(t).IsNotNull();
        await Assert.That(t.Tags.Count).IsEqualTo(1);

        var inner = await Assert.That(t.Tags[0]).IsTypeOf<OverrideTag.T>();

        await Assert.That(inner).IsNotNull();
        await Assert.That(inner.Tags.Count).IsEqualTo(1);

        var clip = await Assert.That(inner.Tags[0]).IsTypeOf<OverrideTag.Clip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.X0).IsEqualTo(0);
        await Assert.That(clip.Y0).IsEqualTo(20);
        await Assert.That(clip.X1).IsEqualTo(100);
        await Assert.That(clip.Y1).IsEqualTo(120);
    }

    [Test]
    public async Task Transform_DoesNotConsumeTrailingTags()
    {
        const string body = @"\t(\fscx120\fscy400)\bord3";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(2);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();
        var bord = await Assert.That(block.Tags[1]).IsTypeOf<OverrideTag.Bord>();

        await Assert.That(t).IsNotNull();
        await Assert.That(bord).IsNotNull();

        await Assert.That(t.Tags.Count).IsEqualTo(2);
        await Assert.That(bord.Thickness).IsEqualTo(3);
    }

    [Test]
    public async Task Transform_Nested_DoesNotConsumeTrailingTags()
    {
        const string body = @"\t(\alpha1\t(\alpha2))\bord3";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(2);
        var t = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.T>();
        var bord = await Assert.That(block.Tags[1]).IsTypeOf<OverrideTag.Bord>();

        await Assert.That(t).IsNotNull();
        await Assert.That(bord).IsNotNull();

        await Assert.That(t.Tags.Count).IsEqualTo(2);
        await Assert.That(bord.Thickness).IsEqualTo(3);
    }

    #endregion Transform

    #region Clip

    [Test]
    public async Task Clip_Rectangle()
    {
        const string body = @"\clip(0,20,100,120)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Clip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.Clip.ClipVariant.Rectangle);

        await Assert.That(clip.X0).IsEqualTo(0);
        await Assert.That(clip.Y0).IsEqualTo(20);
        await Assert.That(clip.X1).IsEqualTo(100);
        await Assert.That(clip.Y1).IsEqualTo(120);
    }

    [Test]
    public async Task Clip_Drawing()
    {
        const string body = @"\clip(m 300 436 l 768 332 1056 596 660 700)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Clip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.Clip.ClipVariant.Drawing);
        await Assert.That(clip.Drawing).IsEqualTo("m 300 436 l 768 332 1056 596 660 700");
    }

    [Test]
    public async Task Clip_ScaledDrawing()
    {
        const string body = @"\clip(1.2,m 300 436 l 768 332 1056 596 660 700)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Clip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.Clip.ClipVariant.ScaledDrawing);
        await Assert.That(clip.Drawing).IsEqualTo("m 300 436 l 768 332 1056 596 660 700");
        await Assert.That(clip.Scale).IsEqualTo(1.2);
    }

    [Test]
    public async Task IClip_Rectangle()
    {
        const string body = @"\iclip(0,20,100,120)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.IClip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.IClip.IClipVariant.Rectangle);

        await Assert.That(clip.X0).IsEqualTo(0);
        await Assert.That(clip.Y0).IsEqualTo(20);
        await Assert.That(clip.X1).IsEqualTo(100);
        await Assert.That(clip.Y1).IsEqualTo(120);
    }

    [Test]
    public async Task IClip_Drawing()
    {
        const string body = @"\iclip(m 300 436 l 768 332 1056 596 660 700)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.IClip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.IClip.IClipVariant.Drawing);
        await Assert.That(clip.Drawing).IsEqualTo("m 300 436 l 768 332 1056 596 660 700");
    }

    [Test]
    public async Task IClip_ScaledDrawing()
    {
        const string body = @"\iclip(1.2,m 300 436 l 768 332 1056 596 660 700)";
        var block = new OverrideBlock(body);

        await Assert.That(block.Tags.Count).IsEqualTo(1);
        var clip = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.IClip>();

        await Assert.That(clip).IsNotNull();
        await Assert.That(clip.Variant).IsEqualTo(OverrideTag.IClip.IClipVariant.ScaledDrawing);
        await Assert.That(clip.Drawing).IsEqualTo("m 300 436 l 768 332 1056 596 660 700");
        await Assert.That(clip.Scale).IsEqualTo(1.2);
    }

    #endregion Clip

    #region Fade

    [Test]
    public async Task Fad_Short()
    {
        const string body = @"\fad(100,200)";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var fad = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fad>();
        await Assert.That(fad).IsNotNull();
        await Assert.That(fad.Name).IsEqualTo(OverrideTags.Fad);

        await Assert.That(fad.IsShortVariant).IsTrue();
        await Assert.That(fad.FadeInDuration).IsEqualTo(100);
        await Assert.That(fad.FadeOutDuration).IsEqualTo(200);
    }

    [Test]
    public async Task Fad_Long()
    {
        const string body = @"\fad(255,32,224,0,500,2000,2200)";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var fad = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fad>();
        await Assert.That(fad).IsNotNull();
        await Assert.That(fad.Name).IsEqualTo(OverrideTags.Fad);

        await Assert.That(fad.IsShortVariant).IsFalse();
        await Assert.That(fad.Alpha1).IsEqualTo(255);
        await Assert.That(fad.Alpha2).IsEqualTo(32);
        await Assert.That(fad.Alpha3).IsEqualTo(224);
        await Assert.That(fad.T1).IsEqualTo(0);
        await Assert.That(fad.T2).IsEqualTo(500);
        await Assert.That(fad.T3).IsEqualTo(2000);
        await Assert.That(fad.T4).IsEqualTo(2200);
    }

    [Test]
    public async Task Fade_Short()
    {
        const string body = @"\fade(100,200)";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var fade = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fade>();
        await Assert.That(fade).IsNotNull();
        await Assert.That(fade.Name).IsEqualTo(OverrideTags.Fade);

        await Assert.That(fade.IsShortVariant).IsTrue();
        await Assert.That(fade.FadeInDuration).IsEqualTo(100);
        await Assert.That(fade.FadeOutDuration).IsEqualTo(200);
    }

    [Test]
    public async Task Fade_Long()
    {
        const string body = @"\fade(255,32,224,0,500,2000,2200)";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var fade = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.Fade>();
        await Assert.That(fade).IsNotNull();
        await Assert.That(fade.Name).IsEqualTo(OverrideTags.Fade);

        await Assert.That(fade.IsShortVariant).IsFalse();
        await Assert.That(fade.Alpha1).IsEqualTo(255);
        await Assert.That(fade.Alpha2).IsEqualTo(32);
        await Assert.That(fade.Alpha3).IsEqualTo(224);
        await Assert.That(fade.T1).IsEqualTo(0);
        await Assert.That(fade.T2).IsEqualTo(500);
        await Assert.That(fade.T3).IsEqualTo(2000);
        await Assert.That(fade.T4).IsEqualTo(2200);
    }

    #endregion Fade

    #region Color

    [Test]
    public async Task Color_BGR()
    {
        const string body = @"\1c&HFF00BB&";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var c1 = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.C1>();
        await Assert.That(c1).IsNotNull();
        await Assert.That(c1.Value).IsNotNull();
        await Assert.That(c1.Value.Blue).IsEqualTo((byte)0xFF);
        await Assert.That(c1.Value.Green).IsEqualTo((byte)0x00);
        await Assert.That(c1.Value.Red).IsEqualTo((byte)0xBB);

        await Assert.That(c1.ToString()).IsEqualTo(body);
    }

    [Test]
    public async Task Alpha()
    {
        const string body = @"\1a&H11&";
        var block = new OverrideBlock(body);
        await Assert.That(block.Tags.Count).IsEqualTo(1);

        var a1 = await Assert.That(block.Tags[0]).IsTypeOf<OverrideTag.A1>();
        await Assert.That(a1).IsNotNull();
        await Assert.That(a1.Value).IsNotNull();
        await Assert.That(a1.Value.Alpha).IsEqualTo((byte)0x11);

        await Assert.That(a1.ToString()).IsEqualTo(body);
    }

    #endregion Color
}
