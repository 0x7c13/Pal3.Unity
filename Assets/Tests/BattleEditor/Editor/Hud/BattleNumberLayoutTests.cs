// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle.Presentation;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class BattleNumberLayoutTests
    {
        [TestCase(2100, 900)]
        [TestCase(800, 600)]
        [TestCase(1200, 600)]
        [TestCase(800, 800)]
        public void NumberPlacementUsesTheWorldProjectionAndUniformSafeAreaScale(int width, int height)
        {
            var viewport = new BattleHudViewport(new Rect(43, 27, width, height), new Vector4(31, 17, 29, 23));
            var point = new Vector2(43 + width * .4f, 27 + height * .3f);
            var frame = new BattleNumberFrame("-123", default, -16, 10, 0x3232ff, 255);
            Rect bounds = BattlePreview.NumberBounds(frame, point, viewport);
            Assert.That(bounds.x, Is.EqualTo(point.x - 16 * viewport.Scale));
            Assert.That(bounds.y, Is.EqualTo(point.y - 10 * viewport.Scale));
            Assert.That(bounds.width, Is.EqualTo(40 * viewport.Scale));
            Assert.That(bounds.height, Is.EqualTo(20 * viewport.Scale));
            Assert.That(viewport.SafeRect.Contains(bounds.min), Is.True);
            Assert.That(viewport.SafeRect.Contains(bounds.max), Is.True);
        }
    }
}
