// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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

    public sealed class Mv3ConfigFileReader : IFileReader<Mv3ActionConfig>
    {
        private const string ACTION_SECTION_HEADER_PREFIX = "action_";

        private const string PROPERTY_NAME = "name";
        private const string PROPERTY_FILE = "file";

        public Mv3ActionConfig Read(IBinaryReader reader, int codepage)
        {
            throw new NotImplementedException();
        }

        public Mv3ActionConfig Read(byte[] data, int codepage)
        {
            FileIniDataParser parser = new ();
            using MemoryStream stream = new (data);
            using StreamReader reader = new (stream, Encoding.GetEncoding(codepage));

            IniData iniData = parser.ReadData(reader);

            List<ActorAction> actions = new();
            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.StartsWith(ACTION_SECTION_HEADER_PREFIX))
                {
                    actions.Add(new ActorAction()
                    {
                        ActionName = iniData[section.SectionName][PROPERTY_NAME],
                        ActionFileName = iniData[section.SectionName][PROPERTY_FILE],
                    });
                }
            }

            return new Mv3ActionConfig(actions.ToArray());
        }
    }
}