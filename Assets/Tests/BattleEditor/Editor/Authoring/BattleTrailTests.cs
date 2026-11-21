// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System.Linq;
    using Battle;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleTrailTests
    {
        [Test]
        public void OriginalTrailUsesEightAgeOrderedAdditiveFrames()
        {
            var state = new BattleTrailState();
            Assert.That(Enumerable.Range(0, 8).Select(state.TextureAt), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 0 }));
            Assert.That(Enumerable.Range(0, 8).Select(BattleTrailState.AlphaAt), Is.EqualTo(new byte[] { 102, 110, 118, 126, 134, 142, 150, 158 }));
            state.Advance(.01);
            Assert.That(state.Frame, Is.Zero);
            state.Advance(.01);
            Assert.That(state.Frame, Is.EqualTo(1));
            state.Advance(0);
            Assert.That(state.Frame, Is.EqualTo(1));
            // Native CTrail::Update uses one if, so it does not catch up in a loop.
            state.Advance(.1);
            Assert.That(state.Frame, Is.EqualTo(2));
        }

        private static Color Pixel(RenderTexture texture)
        {
            RenderTexture previous = RenderTexture.active;
            var readback = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(texture.width / 2, texture.height / 2, 1, 1), 0, 0);
                readback.Apply();
                return readback.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
            }
        }

        private static void Fill(RenderTexture texture, Color color)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                GL.Clear(false, true, color);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        [Test]
        public void InitialCompositeResizeAndDisposePreserveTheCallersRenderTarget()
        {
            using var trail = new BattleTrailPreview { Enabled = true };
            var source = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            source.Create();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                trail.Composite(source, 64, 48);
                Assert.That(RenderTexture.active, Is.SameAs(source));
                trail.Composite(source, 96, 64);
                Assert.That(RenderTexture.active, Is.SameAs(source));
                trail.Dispose();
                Assert.That(RenderTexture.active, Is.SameAs(source));
            }
            finally
            {
                RenderTexture.active = previous;
                source.Release();
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void TrailShaderUsesOriginalDiffuseAlphaAndRepaintsDoNotAgeTheRing()
        {
            using var trail = new BattleTrailPreview { Enabled = true };
            var source = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            source.Create();
            try
            {
                Fill(source, Color.red);
                Color first = Pixel(trail.Composite(source, 64, 48));
                Assert.That(first.r, Is.EqualTo(BattleTrailState.ContributionAt(7)).Within(2f / 255));
                Assert.That(first.g, Is.Zero.Within(1f / 255));
                Color repeated = Pixel(trail.Composite(source, 64, 48));
                Assert.That(repeated.r, Is.EqualTo(first.r).Within(1f / 255));
                trail.Advance(.02, .02);
                Fill(source, Color.green);
                Color second = Pixel(trail.Composite(source, 64, 48));
                Assert.That(second.r, Is.EqualTo(BattleTrailState.ContributionAt(6)).Within(2f / 255));
                Assert.That(second.g, Is.EqualTo(BattleTrailState.ContributionAt(7)).Within(2f / 255));
                trail.Enabled = false;
                Assert.That(trail.Composite(source, 64, 48), Is.SameAs(source));
            }
            finally
            {
                source.Release();
                Object.DestroyImmediate(source);
            }
        }
    }
}
