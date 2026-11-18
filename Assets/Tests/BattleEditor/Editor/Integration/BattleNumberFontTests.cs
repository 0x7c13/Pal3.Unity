// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Battle.Presentation;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleNumberFontTests
    {
        [Test]
        public void OriginalFontSuppliesEveryDamageMissAndGuCostGlyph()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            using var resources = ResourceCatalog.OpenBaseData(root, 936);
            var entry = resources.Find("basedata.cpk\\gbdata\\unicode20.tft");
            Assert.That(entry, Is.Not.Null);
            var font = new BattleNumberFont(resources.Read(entry));
            foreach (char character in "0123456789-MIS蛊")
                Assert.That(font.GlyphAlpha(character).Any(alpha => alpha != 0), Is.True, "Missing native glyph " + character);
        }
    }
}
