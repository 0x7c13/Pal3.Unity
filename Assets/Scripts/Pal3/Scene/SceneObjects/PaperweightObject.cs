// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.Paperweight)]
    public sealed class PaperweightObject : SceneObject
    {
        public PaperweightObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
    }
}

#endif