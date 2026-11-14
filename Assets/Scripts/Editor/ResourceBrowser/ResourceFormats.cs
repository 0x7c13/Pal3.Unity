// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using Core.DataReader;
    using Core.DataReader.Cvd;
    using Core.DataReader.Mov;
    using Core.DataReader.Scn;
    using Core.DataReader.Nav;
    using Core.DataReader.Lgt;
    using Core.DataReader.Msh;
    using Core.DataReader.Mtl;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Gdb;
    using Core.DataReader.Data;
    using Core.DataReader.Dat;
    using Core.DataReader.Par;

    public static class ResourceFormats
    {
        public static object Read(ResourceEntry entry, byte[] bytes, int codepage) => entry.Extension switch
        {
            ".sce" => Read(new SceFileReader(), bytes, codepage),
            ".gdb" => Read(new GdbFileReader(), bytes, codepage),
            ".data" when entry.Name.Equals("flexeff.data", StringComparison.OrdinalIgnoreCase) => Read(new EffectDefinitionFileReader(), bytes, codepage),
            ".dat" when entry.Name.Equals("efflinker.dat", StringComparison.OrdinalIgnoreCase) => Read(new EffectLinkerFileReader(), bytes, codepage),
            ".par" when bytes.Length >= 6 && System.Text.Encoding.ASCII.GetString(bytes, 0, 6) == "sgpar\0" => Read(new ParticleFileReader(), bytes, codepage),
            ".par" or ".spar" when bytes.Length >= 48 && System.Text.Encoding.ASCII.GetString(bytes, 8, 40) == "__PARTICLE_EFFECT(Softstar_Ltd)_EXPORT__" => Read(new SparFileReader(), bytes, codepage),
            ".csf" => Read(new ParticleShapeFileReader(), bytes, codepage),
            ".scn" => Read(new ScnFileReader(), bytes, codepage),
            ".nav" => Read(new NavFileReader(), bytes, codepage),
            ".lgt" => Read(new LgtFileReader(), bytes, codepage),
            ".pol" => Read(new PolFileReader(), bytes, codepage),
            ".cvd" => Read(new CvdFileReader(), bytes, codepage),
            ".mv3" => Read(new Mv3FileReader(), bytes, codepage),
            ".msh" => Read(new MshFileReader(), bytes, codepage),
            ".mov" => Read(new MovFileReader(), bytes, codepage),
            ".mtl" => Read(new MtlFileReader(), bytes, codepage),
            _ => null,
        };

        public static T Read<T>(IFileReader<T> reader, byte[] bytes, int codepage) => reader.Read(bytes, codepage);

        public static readonly string[] Categories = { "All", "Models", "Textures", "Scenes", "Navigation", "Scripts", "Database", "Effects", "Lights", "Text", "Audio", "Video", "Other" };

        public static string Category(string extension) => extension.ToLowerInvariant() switch
        {
            ".pol" or ".cvd" or ".mv3" or ".msh" or ".mov" or ".mtl" => "Models",
            ".dds" or ".tga" or ".bmp" or ".bm" or ".png" or ".jpg" or ".jpeg" => "Textures",
            ".scn" => "Scenes",
            ".nav" => "Navigation",
            ".sce" => "Scripts",
            ".gdb" => "Database",
            ".par" or ".spar" or ".csf" or ".data" or ".dat" => "Effects",
            ".lgt" => "Lights",
            ".ini" or ".txt" or ".gbf" or ".cfg" or ".csv" or ".xml" or ".json" or ".log" => "Text",
            ".wav" or ".mp3" or ".ogg" => "Audio",
            ".webm" or ".mp4" or ".bik" or ".avi" => "Video",
            _ => "Other",
        };
    }
}
