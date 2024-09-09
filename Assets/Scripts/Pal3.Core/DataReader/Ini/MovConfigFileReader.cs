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

    public sealed class MovConfigFileReader : IFileReader<MovActionConfig>
    {
        private const string ACTOR_SECTION_HEADER = "actor";
        private const string ACTION_SECTION_HEADER_PREFIX = "action_";
        private const string MATERIAL_SECTION_HEADER_PREFIX = "material_";
        private const string PROPERTY_NAME = "name";
        private const string PROPERTY_FILE = "file";
        private const string PROPERTY_EFFECT = "effect";
        private const string PROPERTY_MESH = "mesh";
        private const string PROPERTY_MATERIAL = "material";

        public MovActionConfig Read(IBinaryReader reader, int codepage)
        {
            throw new NotImplementedException();
        }

        public MovActionConfig Read(byte[] data, int codepage)
        {
            FileIniDataParser parser = new();
            using MemoryStream stream = new(data);
            using StreamReader reader = new(stream, Encoding.GetEncoding(codepage));

            IniData iniData = parser.ReadData(reader);

            BoneActor? actor = null;
            List<ActorAction> actions = new ();
            List<BoneMaterial> materials = new ();

            foreach (SectionData section in iniData.Sections)
            {
                if (string.Equals(section.SectionName, ACTOR_SECTION_HEADER, StringComparison.Ordinal))
                {
                    actor = new BoneActor()
                    {
                        MeshFileName = iniData[section.SectionName][PROPERTY_MESH],
                        MaterialFileName = iniData[section.SectionName][PROPERTY_MATERIAL],
                        EffectFileName = iniData[section.SectionName][PROPERTY_EFFECT],
                    };
                }

                if (section.SectionName.StartsWith(ACTION_SECTION_HEADER_PREFIX))
                {
                    actions.Add(new ActorAction()
                    {
                        ActionName = iniData[section.SectionName][PROPERTY_NAME],
                        ActionFileName = iniData[section.SectionName][PROPERTY_FILE],
                    });
                }

                if (section.SectionName.StartsWith(MATERIAL_SECTION_HEADER_PREFIX))
                {
                    materials.Add(new BoneMaterial()
                    {
                        MaterialName = iniData[section.SectionName][PROPERTY_NAME],
                        MaterialFileName = iniData[section.SectionName][PROPERTY_FILE],
                        EffectFileName = iniData[section.SectionName][PROPERTY_EFFECT],
                    });
                }
            }

            if (actor == null) throw new InvalidDataException("Actor section is missing in MOV config");

            return new MovActionConfig(actor.Value, actions.ToArray(), materials.ToArray());
        }
    }
}