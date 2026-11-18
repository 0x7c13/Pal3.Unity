// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using Battle.Presentation;
    using UnityEngine;

    public sealed class BattleHudTextTests
    {
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("Only an inventory description", "")]
        [TestCase("Long inventory text$Short battle text", "Short battle text")]
        [TestCase("First$Second$Last", "Last")]
        [TestCase("Inventory$主#", "主#")]
        [TestCase("Inventory$", "")]
        public void DescriptionUsesOnlyTheLastNativeSuffix(string original, string expected) =>
            Assert.That(BattleHudText.Description(original), Is.EqualTo(expected));

        [Test]
        public void NativeCellsWrapPerGlyphAndStopAtTheBottom()
        {
            var glyphs = BattleHudText.Layout("AB中文C", 32, 32, 16);
            Assert.That(glyphs.Select(glyph => (glyph.Text, glyph.Column, glyph.Row)), Is.EqualTo(new[]
            {
                ("A", 0, 0), ("B", 1, 0), ("中", 2, 0), ("文", 0, 1), ("C", 2, 1)
            }));
            Assert.That(string.Concat(BattleHudText.Layout("AB中文C", 32, 16, 16).Select(glyph => glyph.Text)), Is.EqualTo("AB中"));
            Assert.That(BattleHudText.Layout("中", 200, 15, 16), Is.Empty);
        }

        [Test]
        public void NativeEscapesUseNewlineAndEightCellTabWithoutInterpretingHash()
        {
            var glyphs = BattleHudText.Layout(@"甲\n乙\t丙#\9丁", 160, 32, 16);
            Assert.That(glyphs.Select(glyph => (glyph.Text, glyph.Column, glyph.Row)), Is.EqualTo(new[]
            {
                ("甲", 0, 0), ("乙", 0, 1), ("丙", 8, 1), ("#", 10, 1), ("丁", 11, 1)
            }));
            Assert.That(BattleHudText.Layout("甲\r\n乙\0丙", 160, 32, 16).Select(glyph => (glyph.Text, glyph.Column, glyph.Row)), Is.EqualTo(new[] { ("甲", 0, 0), ("乙", 4, 0) }));
        }

        [Test]
        public void OriginalTextRegionsKeepDescriptionGlyphsBelowTheListAndAboveStateCards()
        {
            foreach (bool pal3A in new[] { false, true })
                foreach (BattleHudPage page in new[] { BattleHudPage.Skills, BattleHudPage.Magic, BattleHudPage.Items })
                {
                    Rect list = BattleHudText.ListBounds(page, pal3A), description = BattleHudText.DescriptionBounds(page, pal3A);
                    Assert.That(description.y, Is.GreaterThanOrEqualTo(list.yMax));
                    Assert.That(description.height, Is.GreaterThanOrEqualTo(BattleHudText.DescriptionFontSize));
                    float panelY = page == BattleHudPage.Skills ? 237 : pal3A ? 258 : page == BattleHudPage.Magic ? 276 : 278;
                    Assert.That(panelY + description.y + BattleHudText.DescriptionFontSize, Is.LessThan(491));
                }
            Assert.That(BattleHudText.ListBounds(BattleHudPage.Skills, true).y, Is.EqualTo(92));
            Assert.That(BattleHudText.ListBounds(BattleHudPage.Skills, false).y, Is.EqualTo(110));
        }

        [Test]
        [Category("LocalGameData")]
        public void RealDatabaseDescriptionsKeepTheOriginalBattleSuffix()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var data = new BattleData(root, 936);
            foreach ((int id, string expected) in new[] { (316, "全 减少精"), (317, "单 解除眠乱狂"), (4101, "偷取物品或金钱") })
            {
                string original = data.Database.SkillInfos[id].Description;
                Assert.That(BattleHudText.Description(original).TrimEnd('\r', '\n'), Is.EqualTo(expected));
                Assert.That(string.Concat(BattleHudText.Layout(BattleHudText.Description(original), 193, 20, 16).Select(glyph => glyph.Text)), Is.EqualTo(expected));
                Assert.That(data.Database.SkillInfos[id].Description, Is.EqualTo(original));
            }
        }
    }
}
