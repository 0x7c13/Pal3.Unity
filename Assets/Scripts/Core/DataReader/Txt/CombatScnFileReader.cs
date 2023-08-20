// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Txt
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Gdb;
    using Nav;

    public sealed class CombatScnFileReader : IFileReader<CombatScnFile>
    {
        private const int COMBAT_SCN_FILE_CODEPAGE = 936;

        public CombatScnFile Read(IBinaryReader reader, int _)
        {
            throw new System.NotImplementedException();
        }

        public CombatScnFile Read(byte[] data, int _)
        {
            var content = Encoding.GetEncoding(COMBAT_SCN_FILE_CODEPAGE).GetString(data, 0, data.Length);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string currentCombatSceneName = string.Empty;
            Dictionary<NavFloorKind, string> currentCombatSceneMap = null;

            Dictionary<string, WuLingType> combatSceneWuLingInfo = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<NavFloorKind, string>> combatSceneMapInfo = new(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("//"))
                {
                    continue;
                }

                // Parsing BEGIN-END blocks for CombatSceneMap
                if (line.StartsWith("BEGIN,"))
                {
                    if (!line.StartsWith("BEGIN,fivemagic"))
                    {
                        currentCombatSceneName = line.Substring(6).Trim();
                        currentCombatSceneMap = new Dictionary<NavFloorKind, string>();
                    }
                }
                else if (line == "END")
                {
                    if (currentCombatSceneName != string.Empty && currentCombatSceneMap != null)
                    {
                        combatSceneMapInfo[currentCombatSceneName] = currentCombatSceneMap;
                    }
                    currentCombatSceneName = string.Empty;
                    currentCombatSceneMap = null;
                }
                else if (currentCombatSceneMap != null)
                {
                    // Parsing floor kind to scene property
                    string[] floorParts = line.Split('$');
                    NavFloorKind floorKind = Enum.Parse<NavFloorKind>(floorParts[0].Trim(), true);
                    currentCombatSceneMap[floorKind] = floorParts[1].Split('&')[0].Trim();
                }
                else
                {
                    // Parsing Scene WuLingType
                    string[] parts = line.Split('$');
                    string sceneName = parts[0].Trim();
                    WuLingType type = (WuLingType)(int.Parse(parts[1].Trim().Split('&')[0]) - 1);
                    combatSceneWuLingInfo[sceneName] = type;
                }
            }

            return new CombatScnFile(combatSceneWuLingInfo, combatSceneMapInfo);
        }
    }
}