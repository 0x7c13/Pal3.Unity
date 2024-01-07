// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Ini
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using IniParser;
    using IniParser.Model;

    public sealed class CombatCameraConfigFileReader : IFileReader<CombatCameraConfigFile>
    {
        private const string DEFAULT_CAM_SECTION_HEADER_PREFIX = "DefaultCam";
        private const int DEFAULT_CODEPAGE = 936;

        public CombatCameraConfigFile Read(IBinaryReader reader, int _)
        {
            throw new NotImplementedException();
        }

        public CombatCameraConfigFile Read(byte[] data, int _)
        {
            var parser = new FileIniDataParser();
            using var stream = new MemoryStream(data);
            using var reader = new StreamReader(stream, Encoding.GetEncoding(DEFAULT_CODEPAGE));

            IniData iniData = parser.ReadData(reader);

            var defaultCamConfigs = new List<CombatCameraConfig>();
            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.StartsWith(DEFAULT_CAM_SECTION_HEADER_PREFIX))
                {
                    defaultCamConfigs.Add(new CombatCameraConfig()
                    {
                        GameBoxPositionX = float.Parse(iniData[section.SectionName]["x"]),
                        GameBoxPositionY = float.Parse(iniData[section.SectionName]["y"]),
                        GameBoxPositionZ = float.Parse(iniData[section.SectionName]["z"]),
                        Yaw = float.Parse(iniData[section.SectionName]["yaw"]),
                        Pitch = float.Parse(iniData[section.SectionName]["pitch"]),
                        Roll = float.Parse(iniData[section.SectionName]["roll"]),
                    });
                }
            }

            return new CombatCameraConfigFile(defaultCamConfigs.ToArray());
        }
    }
}