// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;

    public static class FileConstants
    {
        #if PAL3
        public const string EffectScnFolderName = "EffectScn";
        #elif PAL3A
        public const string EffectScnFolderName = "effectscn";
        #endif

        public const string EffectFolderName = "effect";

        public const string SfxFolderName = "snd";

        #if PAL3
        public const string ActorFolderName = "ROLE";
        #elif PAL3A
        public const string ActorFolderName = "role";
        #endif

        public const string UIFolderName = "ui";
        
        public const string CombatDataFolderName = "cbdata";

        public const string CaptionFolderName = "caption";

        public const string EmojiFolderName = "EMOTE";
        
        public const string CursorFolderName = "cursor";

        public const string MovieFolderName = "movie";

        public const string WeaponFolderName = "weapon";

        public static readonly (string cpkName, string relativePath) BaseDataCpkPathInfo = new ("basedata.cpk", "basedata");

        public static readonly (string cpkName, string relativePath) MusicCpkPathInfo = new ("music.cpk", "music");

        public static readonly List<(string cpkName, string relativePath)> MovieCpkPathInfos = new()
        {
            ("movie.cpk", "movie"),
            ("movie_end.cpk", "movie")
        };

        #if PAL3A
        public static readonly (string cpkName, string relativePath) ScnCpkPathInfo = new ("SCN.CPK", "scene");
        public static readonly (string cpkName, string relativePath) SceCpkPathInfo = new ("sce.cpk", "scene");
        #endif

        #if PAL3
        public static readonly List<(string cpkName, string relativePath)> SceneCpkPathInfos = new()
        {
            ("M01.cpk", "scene"),
            ("M02.cpk", "scene"),
            ("M03.cpk", "scene"),
            ("M04.cpk", "scene"),
            ("M05.cpk", "scene"),
            ("M06.cpk", "scene"),
            ("m08.cpk", "scene"),
            ("M09.cpk", "scene"),
            ("m10.cpk", "scene"),
            ("m11.cpk", "scene"),
            ("M15.cpk", "scene"),
            ("M16.cpk", "scene"),
            ("M17.cpk", "scene"),
            ("M18.cpk", "scene"),
            ("M19.cpk", "scene"),
            ("M20.cpk", "scene"),
            ("M21.cpk", "scene"),
            ("M22.cpk", "scene"),
            ("M23.cpk", "scene"),
            ("M24.cpk", "scene"),
            ("M25.cpk", "scene"),
            ("M26.cpk", "scene"),
            ("Q01.cpk", "scene"),
            ("Q02.cpk", "scene"),
            ("Q03.cpk", "scene"),
            ("Q04.cpk", "scene"),
            ("Q05.cpk", "scene"),
            ("Q06.cpk", "scene"),
            ("Q07.cpk", "scene"),
            ("Q08.cpk", "scene"),
            ("Q09.cpk", "scene"),
            ("Q10.cpk", "scene"),
            ("Q11.cpk", "scene"),
            ("Q12.cpk", "scene"),
            ("Q13.cpk", "scene"),
            ("Q14.cpk", "scene"),
            ("Q15.cpk", "scene"),
            ("Q16.cpk", "scene"),
            ("Q17.cpk", "scene"),
            ("T01.cpk", "scene"),
            ("T02.cpk", "scene"),
        };
        #elif PAL3A
        public static readonly List<(string cpkName, string relativePath)> SceneCpkPathInfos = new()
        {
            ("m01.cpk",    "scene"),
            ("m02.cpk",    "scene"),
            ("m03.cpk",    "scene"),
            ("m04.cpk",    "scene"),
            ("m05.cpk",    "scene"),
            ("m06.cpk",    "scene"),
            ("m07.cpk",    "scene"),
            ("m08.cpk",    "scene"),
            ("m09.cpk",    "scene"),
            ("m10.cpk",    "scene"),
            ("m11.cpk",    "scene"),
            ("m12.cpk",    "scene"),
            ("m13.cpk",    "scene"),
            ("m14.cpk",    "scene"),
            ("m15.cpk",    "scene"),
            ("m16.cpk",    "scene"),
            ("m17.cpk",    "scene"),
            ("m18.cpk",    "scene"),
            ("m19.cpk",    "scene"),
            ("m20.cpk",    "scene"),
            ("m21.cpk",    "scene"),
            ("q01.cpk",    "scene"),
            ("q02_01.cpk", "scene"),
            ("q02_02.cpk", "scene"),
            ("q02_03.cpk", "scene"),
            ("q03.cpk",    "scene"),
            ("q04.cpk",    "scene"),
            ("q05.cpk",    "scene"),
            ("q06.cpk",    "scene"),
            ("q07.cpk",    "scene"),
            ("q08.cpk",    "scene"),
            ("q09.cpk",    "scene"),
            ("q10.cpk",    "scene"),
            ("q11.cpk",    "scene"),
            ("y01.cpk",    "scene"),
        };
        #endif
    }
}