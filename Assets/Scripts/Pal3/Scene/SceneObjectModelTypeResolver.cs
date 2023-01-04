// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    public enum SceneObjectModelType
    {
        None          = 0,
        PolModel      = 2,
        CvdModel      = 3,
        EffectModel   = 4,
    }

    public static class SceneObjectModelTypeResolver
    {
        public static SceneObjectModelType GetType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return SceneObjectModelType.None;
            }

            if (fileName[0] == '+')
            {
                return SceneObjectModelType.EffectModel;
            }

            if (fileName.ToLower().EndsWith(".pol"))
            {
                return SceneObjectModelType.PolModel;
            }

            if (fileName.ToLower().EndsWith(".cvd"))
            {
                return SceneObjectModelType.CvdModel;
            }

            return SceneObjectModelType.None;
        }
    }
}