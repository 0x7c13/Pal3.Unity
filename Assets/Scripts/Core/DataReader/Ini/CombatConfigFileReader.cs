// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    using System;
    using System.IO;
    using System.Text;
    using IniParser;
    using IniParser.Model;
    using UnityEngine;

    public sealed class CombatConfigFileReader : IFileReader<CombatConfigFile>
    {
        private const string ROLE_POSITION_SECTION_HEADER_PREFIX = "RolePos";

        public CombatConfigFile Read(IBinaryReader reader, int codepage)
        {
            throw new NotImplementedException();
        }

        public CombatConfigFile Read(byte[] data, int codepage)
        {
            var parser = new FileIniDataParser();
            using var stream = new MemoryStream(data);
            using var reader = new StreamReader(stream, Encoding.GetEncoding(codepage));

            IniData iniData = parser.ReadData(reader);

            var actorGameBoxPositions = new Vector3[10];
            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.StartsWith(ROLE_POSITION_SECTION_HEADER_PREFIX))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        string x = iniData[section.SectionName][$"Role{i}-x"];
                        string z = iniData[section.SectionName][$"Role{i}-z"];

                        if (x.Contains(";")) x = x[..x.IndexOf(';')].Trim();
                        if (z.Contains(";"))z = z[..z.IndexOf(';')].Trim();

                        actorGameBoxPositions[i] = new Vector3(float.Parse(x), 0f, float.Parse(z));
                    }
                }
            }

            return new CombatConfigFile(actorGameBoxPositions);
        }
    }
}