// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Ini
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using IniParser;
    using IniParser.Model;
    using Primitives;

    public sealed class CombatConfigFileReader : IFileReader<CombatConfigFile>
    {
        private const string ROLE_POSITION_SECTION_HEADER_PREFIX = "RolePos";
        private const string FIVE_ELEMENTS_FORMATION_SECTION_HEADER_SUFFIX = "FiveLineup";
        private const string LEVEL_SECTION_HEADER = "Level";

        public CombatConfigFile Read(IBinaryReader reader, int codepage)
        {
            throw new NotImplementedException();
        }

        public CombatConfigFile Read(byte[] data, int codepage)
        {
            FileIniDataParser parser = new();
            using MemoryStream stream = new(data);
            using StreamReader reader = new(stream, Encoding.GetEncoding(codepage));

            IniData iniData = parser.ReadData(reader);

            GameBoxVector3[] actorGameBoxPositions = new GameBoxVector3[11];
            FiveElementsFormationConfig allyFormationConfig = default;
            FiveElementsFormationConfig enemyFormationConfig = default;
            int[] levelExperienceTable = new int[100];
            bool hasEnemyCenterRole = false;

            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.Equals(ROLE_POSITION_SECTION_HEADER_PREFIX,
                        StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < actorGameBoxPositions.Length; i++)
                    {
                        string x = iniData[section.SectionName][$"Role{i}-x"];
                        string z = iniData[section.SectionName][$"Role{i}-z"];

                        if (i == 10)
                        {
                            // Older user-provided layouts may only supply roles 0–9.
                            hasEnemyCenterRole = x != null || z != null;
                            if (!hasEnemyCenterRole)
                                continue;
                        }
                        actorGameBoxPositions[i] = new GameBoxVector3(ParseFloat(x, section, $"Role{i}-x"), 0f,
                            ParseFloat(z, section, $"Role{i}-z"));
                    }
                }

                if (section.SectionName.EndsWith(FIVE_ELEMENTS_FORMATION_SECTION_HEADER_SUFFIX))
                {
                    string x = iniData[section.SectionName]["x"];
                    string y = iniData[section.SectionName]["y"];
                    string z = iniData[section.SectionName]["z"];
                    string radius = iniData[section.SectionName]["radius"];
                    string roll = iniData[section.SectionName]["roll"];

                    FiveElementsFormationConfig config = new()
                    {
                        CenterGameBoxPosition = new GameBoxVector3(ParseFloat(x, section, "x"), ParseFloat(y, section, "y"), ParseFloat(z, section, "z")),
                        GameBoxRadius = ParseFloat(radius, section, "radius"),
                        RollRadians = roll == null ? 0f : ParseFloat(roll, section, "roll"),
                    };

                    if (section.SectionName.StartsWith("Enemy"))
                    {
                        enemyFormationConfig = config;
                    }
                    else
                    {
                        allyFormationConfig = config;
                    }
                }

                if (section.SectionName.Equals(LEVEL_SECTION_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        string exp = iniData[section.SectionName][i.ToString(CultureInfo.InvariantCulture)];
                        if (exp == null)
                            throw new FormatException($"[{section.SectionName}] is missing level {i}.");
                        levelExperienceTable[i] = int.Parse(exp, CultureInfo.InvariantCulture);
                    }
                }
            }

            if (!hasEnemyCenterRole)
            {
                GameBoxVector3 center = enemyFormationConfig.CenterGameBoxPosition;
                actorGameBoxPositions[10] = new GameBoxVector3(center.X, 0f, center.Z);
            }
            return new CombatConfigFile(actorGameBoxPositions,
                allyFormationConfig,
                enemyFormationConfig,
                levelExperienceTable);
        }

        private static float ParseFloat(string value, SectionData section, string key)
        {
            if (value == null)
                throw new FormatException($"[{section.SectionName}] is missing {key}.");
            int comment = value.IndexOf(';');
            if (comment >= 0)
                value = value[..comment];
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
