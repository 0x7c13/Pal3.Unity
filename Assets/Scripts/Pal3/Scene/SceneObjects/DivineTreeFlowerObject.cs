// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.DivineTreeFlower)]
    public class DivineTreeFlowerObject : SceneObject
    {
        public DivineTreeFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo, hasModel: false)
        {
        }
    }
}