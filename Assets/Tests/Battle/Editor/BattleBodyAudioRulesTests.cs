// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System.Collections.Generic;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleBodyAudioRulesTests
    {
        // Frozen sound-port traces from published PAL3/PAL3A ProcAttack.
        private static IEnumerable<TestCaseData> AttackGoldens()
        {
            yield return new TestCaseData("PAL3", 0, 1, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 1, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 1, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 1, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 1, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 1, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 3, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 4, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 5, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 6, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 7, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 8, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 9, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 0, 10, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 1, 1, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 1, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 1, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 1, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 1, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 1, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 2, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 3, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 4, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 5, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 6, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 7, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 8, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 9, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, false, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 1, 10, true, new string[] { "wd028", "wd028", null, null });
            yield return new TestCaseData("PAL3", 2, 1, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 1, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 1, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 1, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 1, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 1, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 2, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 3, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 4, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 5, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 6, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 7, false, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 7, true, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 7, false, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 7, true, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 7, false, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 7, true, new string[] { "wd030", "wd030", null, null });
            yield return new TestCaseData("PAL3", 2, 8, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 8, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 8, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 8, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 8, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 8, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 9, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, false, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 2, 10, true, new string[] { null, "wd024", null, null });
            yield return new TestCaseData("PAL3", 3, 1, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 1, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 1, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 1, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 1, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 1, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 2, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 3, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 4, false, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 4, true, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 4, false, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 4, true, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 4, false, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 4, true, new string[] { "wd027", "wd027", null, null });
            yield return new TestCaseData("PAL3", 3, 5, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 5, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 5, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 5, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 5, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 5, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 6, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 7, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 8, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 9, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, false, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 3, 10, true, new string[] { "wd026", "wd026", null, null });
            yield return new TestCaseData("PAL3", 4, 1, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 1, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 1, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 1, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 1, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 1, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 2, false, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 2, true, new string[] { "wd025", "wd025", null, null });
            yield return new TestCaseData("PAL3", 4, 3, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 3, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 3, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 3, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 3, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 3, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 4, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 5, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 6, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 7, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 8, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 9, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, false, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", 4, 10, true, new string[] { "wd029", "wd029", null, null });
            yield return new TestCaseData("PAL3", -1, 1, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 1, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 1, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 1, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 1, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 1, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 2, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 3, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 4, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 5, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 6, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 7, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 8, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 9, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, false, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3", -1, 10, true, new string[] { "wd007", "wd007", null, null });
            yield return new TestCaseData("PAL3A", 0, 1, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 1, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 1, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 1, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 1, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 1, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 2, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 0, 2, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 2, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 0, 2, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 2, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 0, 2, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 3, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 4, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 5, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 6, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 7, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 8, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 9, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, false, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 0, 10, true, new string[] { "wd025", "wd025", "wd025", "wd025" });
            yield return new TestCaseData("PAL3A", 1, 1, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 1, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 1, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 1, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 1, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 1, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 2, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 3, false, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 3, true, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 3, false, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 3, true, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 3, false, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 3, true, new string[] { "wd026", "wd026", "wd026", "wd026" });
            yield return new TestCaseData("PAL3A", 1, 4, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 4, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 4, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 4, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 4, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 4, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 5, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 6, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 7, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 8, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 9, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, false, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 1, 10, true, new string[] { "wd023", "wd023", "wd023", "wd023" });
            yield return new TestCaseData("PAL3A", 2, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 5, false, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 5, true, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 5, false, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 5, true, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 5, false, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 5, true, new string[] { "wd027", "wd027", "wd027", "wd027" });
            yield return new TestCaseData("PAL3A", 2, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 2, 10, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 1, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 2, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 3, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 4, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 5, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 6, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 7, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 8, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, false, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 9, true, new string[] { "wd028", "wd028", "wd028", "wd028" });
            yield return new TestCaseData("PAL3A", 3, 10, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 3, 10, true, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 3, 10, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 3, 10, true, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 3, 10, false, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 3, 10, true, new string[] { null, null, null, null });
            yield return new TestCaseData("PAL3A", 4, 1, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 1, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 1, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 1, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 1, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 1, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 2, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 3, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 4, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 5, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 6, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 7, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 8, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 9, false, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 9, true, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 9, false, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 9, true, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 9, false, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 9, true, new string[] { "wd031", "wd031", "wd031", "wd031" });
            yield return new TestCaseData("PAL3A", 4, 10, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 10, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 10, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 10, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 10, false, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", 4, 10, true, new string[] { "wd030", "wd030", "wd030", "wd030" });
            yield return new TestCaseData("PAL3A", -1, 1, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 1, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 1, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 1, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 1, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 1, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 2, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 3, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 4, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 5, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 6, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 7, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 8, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 9, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, false, new string[] { "wd007", "wd007", "wd007", "wd007" });
            yield return new TestCaseData("PAL3A", -1, 10, true, new string[] { "wd007", "wd007", "wd007", "wd007" });
        }

        [TestCaseSource(nameof(AttackGoldens))]
        public void AttackSoundMatchesReleasedBodyMarkers(string variant, int identity, int weapon, bool transformed, string[] expected)
        {
            for (int marker = 0; marker < 4; marker++)
                Assert.That(BattleBodyAudioRules.Attack(variant, identity, weapon, (BattlePhysicalMarkerKind)marker,
                    7, transformed: transformed), Is.EqualTo(expected[marker]));
        }

        [TestCase("PAL3", 0, "we015")]
        [TestCase("PAL3", 4, "we019")]
        [TestCase("PAL3A", 0, "we002")]
        [TestCase("PAL3A", 4, "we006")]
        [TestCase("PAL3A", -1, "we038")]
        public void HurtUsesTheVariantActorIdentity(string variant, int identity, string expected)
        {
            Assert.That(BattleBodyAudioRules.Hurt(variant, identity, false, 38), Is.EqualTo(expected));
            Assert.That(BattleBodyAudioRules.Hurt(variant, identity, true, 38), Is.EqualTo(identity < 0 ? expected : "we020"));
        }

        [Test]
        public void MagicSwordAndAbsentMonsterSoundsKeepTheirNativeBoundaries()
        {
            Assert.That(BattleBodyAudioRules.Attack("PAL3", 0, 2, BattlePhysicalMarkerKind.Work, magicSword: true), Is.EqualTo("wd023"));
            Assert.That(BattleBodyAudioRules.Attack("PAL3A", -1, 0, BattlePhysicalMarkerKind.Work), Is.Null);
            Assert.That(BattleBodyAudioRules.Hurt("PAL3", -1, false), Is.Null);
            Assert.That(BattleBodyAudioRules.Attack("PAL3A", 0, 1, BattlePhysicalMarkerKind.Over), Is.Null);
        }

        [TestCase("PAL3", 0, BattleMovementKind.Run, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3", 1, BattleMovementKind.Run, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 2, BattleMovementKind.Run, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 3, BattleMovementKind.Run, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 4, BattleMovementKind.Run, 0f, 0f, 100f, "we011b", true)]
        [TestCase("PAL3", -1, BattleMovementKind.Run, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Run, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Run, -1000f, 0f, -1000f, "we011", false)]
        [TestCase("PAL3", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 1, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 2, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 3, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 4, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", -1, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 0, BattleMovementKind.Teleport, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Teleport, -1000f, 0f, -1000f, "we014", false)]
        [TestCase("PAL3", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 1, BattleMovementKind.Fly, 0f, 0f, 100f, "we012", false)]
        [TestCase("PAL3", 2, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 3, BattleMovementKind.Fly, 0f, 0f, 100f, "we013", false)]
        [TestCase("PAL3", 4, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", -1, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, -1000f, 0f, -1000f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 1, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 2, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 3, BattleMovementKind.FlyRotate, 0f, 0f, 100f, "we013", false)]
        [TestCase("PAL3", 4, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", -1, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, -1000f, 0f, -1000f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FlyRotate, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3", 1, BattleMovementKind.NpcFly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 2, BattleMovementKind.NpcFly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 3, BattleMovementKind.NpcFly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 4, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011b", true)]
        [TestCase("PAL3", -1, BattleMovementKind.NpcFly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, -1000f, 0f, -1000f, "we011", false)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3", 0, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 1, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 2, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 3, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 4, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", -1, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FastRun, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3", 0, BattleMovementKind.FastRun, -1000f, 0f, -1000f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Run, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 1, BattleMovementKind.Run, 0f, 0f, 100f, "we001", true)]
        [TestCase("PAL3A", 2, BattleMovementKind.Run, 0f, 0f, 100f, "we001", true)]
        [TestCase("PAL3A", 3, BattleMovementKind.Run, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 4, BattleMovementKind.Run, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", -1, BattleMovementKind.Run, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Run, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Run, -1000f, 0f, -1000f, "we011", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 1, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 2, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 3, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 4, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", -1, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Teleport, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Teleport, -1000f, 0f, -1000f, "we014", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Teleport, 0f, 0f, 100f, "we014", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 1, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 2, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 3, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 4, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", -1, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, -1000f, 0f, -1000f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.Fly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 1, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we001", true)]
        [TestCase("PAL3A", 2, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we001", true)]
        [TestCase("PAL3A", 3, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 4, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", -1, BattleMovementKind.NpcFly, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, -1000f, 0f, -1000f, "we011", false)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 0, BattleMovementKind.NpcFly, 0f, 0f, 100f, "we011", true)]
        [TestCase("PAL3A", 0, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 1, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 2, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 3, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 4, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", -1, BattleMovementKind.FastRun, 0f, 0f, 100f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.FastRun, 0f, 0f, 0f, null, false)]
        [TestCase("PAL3A", 0, BattleMovementKind.FastRun, -1000f, 0f, -1000f, null, false)]
        public void MovementVoiceMatchesReleasedActivation(string variant, int identity, BattleMovementKind kind, float x, float y, float z, string expected, bool loop)
        {
            var sound = BattleBodyAudioRules.Movement(variant, identity, new BattleMovementPlan(kind, default, new BattleMovementPoint(x, y, z)));
            Assert.That(sound.Sound, Is.EqualTo(expected));
            if (expected != null)
                Assert.That(sound.Loop, Is.EqualTo(loop));
        }
    }
}
