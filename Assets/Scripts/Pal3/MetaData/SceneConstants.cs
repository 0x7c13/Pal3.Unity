// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using Core.DataReader.Cpk;

    public static class SceneConstants
    {
        private static readonly char PathSeparator = CpkConstants.CpkDirectorySeparatorChar;

        private static readonly string EffectScnRelativePath =
            $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
            $"{FileConstants.EffectScnFolderName}{PathSeparator}";

        public static readonly string[] SkyBoxTexturePathFormat =
        {
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_lf.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_fr.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_rt.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_bk.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_up.tga",
            EffectScnRelativePath + "skybox" + PathSeparator + "{0:00}" + PathSeparator + "{0:00}_dn.tga",
        };
    }
}