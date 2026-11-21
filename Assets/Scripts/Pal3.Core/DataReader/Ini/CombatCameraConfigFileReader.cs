// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Ini
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
            FileIniDataParser parser = new();
            using MemoryStream stream = new(data);
            using StreamReader reader = new(stream, Encoding.GetEncoding(DEFAULT_CODEPAGE));

            IniData iniData = parser.ReadData(reader);

            List<CombatCameraConfig> defaultCamConfigs = new();
            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.StartsWith(DEFAULT_CAM_SECTION_HEADER_PREFIX))
                {
                    defaultCamConfigs.Add(new CombatCameraConfig()
                    {
                        GameBoxPositionX = ParseFloat(section, "x"),
                        GameBoxPositionY = ParseFloat(section, "y"),
                        GameBoxPositionZ = ParseFloat(section, "z"),
                        Yaw = ParseFloat(section, "yaw"),
                        Pitch = ParseFloat(section, "pitch"),
                        Roll = ParseFloat(section, "roll"),
                    });
                }
            }

            return new CombatCameraConfigFile(defaultCamConfigs.ToArray());
        }

        private static float ParseFloat(SectionData section, string key)
        {
            string value = section.Keys[key] ?? throw new FormatException($"[{section.SectionName}] is missing {key}.");
            int comment = value.IndexOf(';');
            if (comment >= 0)
                value = value[..comment];
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}