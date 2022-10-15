// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    public enum ModelType
    {
        Unknown       = 0,
        PolModel      = 2,
        CvdModel      = 3,
        Mv3Model      = 4,
        EffectModel   = 5,
    }

    public static class ModelTypeResolver
    {
        public static ModelType GetType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return ModelType.Unknown;
            }

            if (fileName[0] == '+')
            {
                return ModelType.EffectModel;
            }

            if (fileName.ToLower().EndsWith(".pol"))
            {
                return ModelType.PolModel;
            }

            if (fileName.ToLower().EndsWith(".cvd"))
            {
                return ModelType.CvdModel;
            }
            
            if (fileName.ToLower().EndsWith(".mv3"))
            {
                return ModelType.Mv3Model;
            }

            return ModelType.Unknown;
        }
    }
}