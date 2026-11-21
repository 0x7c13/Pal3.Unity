// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Contract.Constants;
    using Battle;

    // Standalone battles have no story city. Reverse the native scene table, and
    // make the editor's deterministic choice explicit when that context is ambiguous.
    public static class BattleMusicResolver
    {
        public static string Resolve(BattleData data, BattleScenario scenario, ICollection<string> diagnostics)
        {
            if (!string.IsNullOrWhiteSpace(scenario.Music))
                return scenario.Music.Trim();
            var candidates = data.SceneMap.CombatSceneMapInfo
                .Where(pair => pair.Value.Values.Contains(scenario.Scene, StringComparer.OrdinalIgnoreCase))
                .Select(pair => pair.Key.Split('_')[0])
                .Where(MusicConstants.CombatMusicInfo.ContainsKey)
                .Select(city => MusicConstants.CombatMusicInfo[city])
                .GroupBy(track => track, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();
            if (candidates.Length > 0)
            {
                if (candidates.Length > 1)
                    diagnostics?.Add($"Battle scene {scenario.Scene} is shared by story areas with different music. Editor default: {candidates[0].Key}; set Music to override.");
                return candidates[0].Key;
            }
            string fallback = MusicConstants.CombatMusicInfo.Values.First();
            diagnostics?.Add($"Battle scene {scenario.Scene} has no regular story-area music mapping. Editor fallback: {fallback}; set Music to override.");
            return fallback;
        }
    }
}
