// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;

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

            if (fileName.EndsWith(".pol", StringComparison.OrdinalIgnoreCase))
            {
                return SceneObjectModelType.PolModel;
            }

            if (fileName.EndsWith(".cvd", StringComparison.OrdinalIgnoreCase))
            {
                return SceneObjectModelType.CvdModel;
            }

            return SceneObjectModelType.None;
        }
    }
}