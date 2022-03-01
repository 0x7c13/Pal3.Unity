// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using IniParser;
    using IniParser.Model;

    public static class ActorConfigFileReader
    {
        private const string ACTION_SECTION_HEADER_PREFIX = "action_";
        private const string ACTION_SECTION_PROPERTY_NAME = "name";
        private const string ACTION_SECTION_PROPERTY_FILE = "file";

        public static ActorConfigFile Read(byte[] configData)
        {
            var parser = new FileIniDataParser();
            using var stream = new MemoryStream(configData);
            using var reader = new StreamReader(stream, Encoding.ASCII);

            IniData iniData = parser.ReadData(reader);

            var actions = new List<ActorAction>();
            foreach (var section in iniData.Sections
                         .Where(s => s.SectionName.StartsWith(ACTION_SECTION_HEADER_PREFIX)))
            {
                actions.Add(new ActorAction()
                {
                    ActionName = iniData[section.SectionName][ACTION_SECTION_PROPERTY_NAME],
                    ActionFileName = iniData[section.SectionName][ACTION_SECTION_PROPERTY_FILE],
                });
            }

            return new ActorConfigFile(actions.ToArray());
        }
    }
}