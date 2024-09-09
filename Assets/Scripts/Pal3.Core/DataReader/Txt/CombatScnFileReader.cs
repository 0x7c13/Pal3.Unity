// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Txt
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Contract.Enums;

    public sealed class CombatScnFileReader : IFileReader<CombatScnFile>
    {
        private const int DEFAULT_CODEPAGE = 936;

        public CombatScnFile Read(IBinaryReader reader, int _)
        {
            throw new System.NotImplementedException();
        }

        public CombatScnFile Read(byte[] data, int _)
        {
            string content = Encoding.GetEncoding(DEFAULT_CODEPAGE).GetString(data, 0, data.Length);
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string currentCombatSceneName = string.Empty;
            Dictionary<FloorType, string> currentCombatSceneMap = null;

            Dictionary<string, ElementType> combatSceneElementTypeInfo = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<FloorType, string>> combatSceneMapInfo = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
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
                        currentCombatSceneName = line[6..].Trim();
                        if (currentCombatSceneName.Contains("\t"))
                        {
                            currentCombatSceneName = currentCombatSceneName[..currentCombatSceneName.IndexOf('\t')].Trim();
                        }
                        currentCombatSceneMap = new Dictionary<FloorType, string>();
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
                    FloorType floorType = Enum.Parse<FloorType>(floorParts[0].Trim(), true);
                    currentCombatSceneMap[floorType] = floorParts[1].Split('&')[0].Trim();
                }
                else
                {
                    // Parsing Scene Element Type
                    string[] parts = line.Split('$');
                    string sceneName = parts[0].Trim();
                    combatSceneElementTypeInfo[sceneName] = (ElementType)int.Parse(parts[1].Trim().Split('&')[0]);
                }
            }

            return new CombatScnFile(combatSceneElementTypeInfo, combatSceneMapInfo);
        }
    }
}