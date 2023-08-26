// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;
    using System.IO;
    using Core.DataReader.Cpk;
    using Core.DataReader.Scn;

    public static class FileConstants
    {
        public const string SfxFolderName = "snd";
        public const string MovieFolderName = "movie";

        #if PAL3
        private const string ACTOR_FOLDER_NAME = "ROLE";
        #elif PAL3A
        private const string ACTOR_FOLDER_NAME = "role";
        #endif

        #if PAL3
        private const string EFFECT_SCN_FOLDER_NAME = "EffectScn";
        #elif PAL3A
        private const string EFFECT_SCN_FOLDER_NAME = "effectscn";
        #endif

        #if PAL3
        private const string COMBAT_SCENE_FOLDER_NAME = "cbScene";
        #elif PAL3A
        private const string COMBAT_SCENE_FOLDER_NAME = "cbscene";
        #endif

        private const string SCENE_FOLDER_NAME = "scene";
        private const string EFFECT_FOLDER_NAME = "effect";
        private const string UI_FOLDER_NAME = "ui";
        private const string UI_LIBRARY_FOLDER_NAME = "UILib";
        private const string WORLD_MAP_FOLDER_NAME = "BigMap";
        private const string OBJECT_FOLDER_NAME = "object";
        private const string COMBAT_DATA_FOLDER_NAME = "cbdata";
        private const string CAPTION_FOLDER_NAME = "caption";
        private const string EMOJI_FOLDER_NAME = "EMOTE";
        private const string CURSOR_FOLDER_NAME = "cursor";
        private const string WEAPON_FOLDER_NAME = "weapon";
        private const string BASE_DATA_FOLDER_NAME = "basedata";
        private const string MUSIC_FOLDER_NAME = "music";
        private const string DATA_SCRIPT_FOLDER_NAME = "datascript";

        private const string BASE_DATA_CPK_FILE_NAME = "basedata.cpk";
        private const string MUSIC_CPK_FILE_NAME = "music.cpk";

        #if PAL3A
        private const string SCN_CPK_FILE_NAME = "SCN.CPK";
        private const string SCE_CPK_FILE_NAME = "sce.cpk";
        #endif

        public static readonly HashSet<string> MovieCpkFileNames = new()
        {
            "movie.cpk",
            "movie_end.cpk"
        };

        #if PAL3
        public static readonly HashSet<string> SceneCpkFileNames = new ()
        {
            "M01.cpk", "M02.cpk", "M03.cpk", "M04.cpk", "M05.cpk", "M06.cpk", "m08.cpk", "M09.cpk",
            "m10.cpk", "m11.cpk", "M15.cpk", "M16.cpk", "M17.cpk", "M18.cpk", "M19.cpk",
            "M20.cpk", "M21.cpk", "M22.cpk", "M23.cpk", "M24.cpk", "M25.cpk", "M26.cpk",
            "Q01.cpk", "Q02.cpk", "Q03.cpk", "Q04.cpk", "Q05.cpk", "Q06.cpk", "Q07.cpk", "Q08.cpk", "Q09.cpk",
            "Q10.cpk", "Q11.cpk", "Q12.cpk", "Q13.cpk", "Q14.cpk", "Q15.cpk", "Q16.cpk", "Q17.cpk",
            "T01.cpk", "T02.cpk"
        };
        #elif PAL3A
        public static readonly HashSet<string> SceneCpkFileNames = new ()
        {
            "m01.cpk", "m02.cpk", "m03.cpk", "m04.cpk", "m05.cpk", "m06.cpk", "m07.cpk", "m08.cpk", "m09.cpk",
            "m10.cpk", "m11.cpk", "m12.cpk", "m13.cpk", "m14.cpk", "m15.cpk", "m16.cpk", "m17.cpk", "m18.cpk", "m19.cpk",
            "m20.cpk", "m21.cpk",
            "q01.cpk", "q02_01.cpk", "q02_02.cpk", "q02_03.cpk", "q03.cpk", "q04.cpk", "q05.cpk", "q06.cpk", "q07.cpk", "q08.cpk", "q09.cpk",
            "q10.cpk", "q11.cpk",
            "y01.cpk"
        };
        #endif

        #region Platform specific file names and paths

        public static readonly string BaseDataCpkFileRelativePath =
            $"{BASE_DATA_FOLDER_NAME}{Path.DirectorySeparatorChar}{BASE_DATA_CPK_FILE_NAME}";

        public static readonly string MusicCpkFileRelativePath =
            $"{MUSIC_FOLDER_NAME}{Path.DirectorySeparatorChar}{MUSIC_CPK_FILE_NAME}";

        #if PAL3A
        public static readonly string ScnCpkFileRelativePath = $"{SCENE_FOLDER_NAME}{Path.DirectorySeparatorChar}{SCN_CPK_FILE_NAME}";
        public static readonly string SceCpkFileRelativePath = $"{SCENE_FOLDER_NAME}{Path.DirectorySeparatorChar}{SCE_CPK_FILE_NAME}";
        #endif

        public static string GetSceneCpkFileRelativePath(string sceneCpkFileName)
        {
            return $"{SCENE_FOLDER_NAME}{Path.DirectorySeparatorChar}{sceneCpkFileName}";
        }

        public static string GetMovieCpkFileRelativePath(string movieCpkFileName)
        {
            return $"{MovieFolderName}{Path.DirectorySeparatorChar}{movieCpkFileName}";
        }

        #endregion

        #region CPK file system virtual file names and paths

        private const char DIR_SEPARATOR = CpkConstants.DirectorySeparatorChar;

        public static readonly string DataScriptFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{DATA_SCRIPT_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string EffectFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{EFFECT_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string EffectScnFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{EFFECT_SCN_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string UIFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{UI_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string UISceneFolderVirtualPath =
            $"{UIFolderVirtualPath}{SCENE_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string UILibFolderVirtualPath =
            $"{UIFolderVirtualPath}{UI_LIBRARY_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string EmojiSpriteSheetFolderVirtualPath =
            $"{UIFolderVirtualPath}{EMOJI_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string CursorSpriteFolderVirtualPath =
            $"{UIFolderVirtualPath}{CURSOR_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string CaptionFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{CAPTION_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string CombatDataFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{COMBAT_DATA_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string CombatSceneFolderVirtualPath =
            $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{COMBAT_SCENE_FOLDER_NAME}{DIR_SEPARATOR}";

        public static readonly string[] SkyBoxTexturePathFormat =
        {
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_lf.tga",
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_fr.tga",
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_rt.tga",
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_bk.tga",
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_up.tga",
            EffectScnFolderVirtualPath + "skybox" + DIR_SEPARATOR + "{0:00}" + DIR_SEPARATOR + "{0:00}_dn.tga",
        };

        public static readonly string GameDatabaseFileVirtualPath = CombatDataFolderVirtualPath + $"{GameConstants.AppName}_Softstar.gdb";

        public static readonly string SystemSceFileVirtualPath = $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}init.sce";

        public static readonly string WorldMapSceFileVirtualPath = $"{UIFolderVirtualPath}{WORLD_MAP_FOLDER_NAME}{DIR_SEPARATOR}BigMap.sce";

        public static string GetWeaponModelFileVirtualPath(string weaponName)
        {
            return $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}" +
                   $"{WEAPON_FOLDER_NAME}{DIR_SEPARATOR}{weaponName}{DIR_SEPARATOR}{weaponName}.pol";
        }

        public static string GetGameItemModelFileVirtualPath(string itemName)
        {
            return $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}item" +
                   $"{DIR_SEPARATOR}{itemName}{DIR_SEPARATOR}{itemName}.pol";
        }

        public static string GetGameObjectModelFileVirtualPath(string objectFileName)
        {
            return $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}{OBJECT_FOLDER_NAME}" +
                   CpkConstants.DirectorySeparatorChar + objectFileName;
        }

        public static string GetGameObjectModelFileVirtualPath(ScnSceneInfo sceneInfo, string objectName)
        {
            return $"{sceneInfo.CityName}{CpkConstants.FileExtension}{DIR_SEPARATOR}" +
                   $"{sceneInfo.Model}{DIR_SEPARATOR}{objectName}";
        }

        public static string GetMusicFileVirtualPath(string musicName)
        {
            var separator = CpkConstants.DirectorySeparatorChar;
            return $"{MUSIC_CPK_FILE_NAME}{separator}" +
                   $"{MUSIC_FOLDER_NAME}{separator}{musicName}.mp3";
        }

        public static string GetActorFolderVirtualPath(string actorName)
        {
            return $"{BASE_DATA_CPK_FILE_NAME}{DIR_SEPARATOR}" +
                   $"{ACTOR_FOLDER_NAME}{DIR_SEPARATOR}{actorName}{DIR_SEPARATOR}";
        }

        public static string GetNavFileVirtualPath(string sceneCityName, string modelName)
        {
            #if PAL3
            var navFilePath = $"{sceneCityName}{CpkConstants.FileExtension}{DIR_SEPARATOR}" +
                              $"{modelName}{DIR_SEPARATOR}{modelName}.nav";
            #elif PAL3A
            var navFilePath = $"{SCN_CPK_FILE_NAME}{DIR_SEPARATOR}SCN{DIR_SEPARATOR}" +
                              $"{sceneCityName}{DIR_SEPARATOR}{modelName}{DIR_SEPARATOR}{modelName}.nav";
            #endif
            return navFilePath;
        }

        public static string GetScnFileVirtualPath(string sceneCityName, string sceneName)
        {
            #if PAL3
            var scnFilePath = $"{sceneCityName}{CpkConstants.FileExtension}{DIR_SEPARATOR}{sceneName}.scn";
            #elif PAL3A
            var scnFilePath = $"{SCN_CPK_FILE_NAME}{DIR_SEPARATOR}SCN{DIR_SEPARATOR}" +
                              $"{sceneCityName}{DIR_SEPARATOR}{sceneCityName}_{sceneName}.scn";
            #endif
            return scnFilePath;
        }

        public static string GetSceneSceFileVirtualPath(string sceneCityName)
        {
            #if PAL3
            var sceFilePath = $"{sceneCityName}{CpkConstants.FileExtension}{DIR_SEPARATOR}{sceneCityName}.sce";
            #elif PAL3A
            var sceFilePath = $"{SCE_CPK_FILE_NAME}{DIR_SEPARATOR}Sce{DIR_SEPARATOR}{sceneCityName}.sce";
            #endif
            return sceFilePath;
        }

        #endregion

        public static string GetActorFolderName()
        {
            return ACTOR_FOLDER_NAME;
        }
    }
}