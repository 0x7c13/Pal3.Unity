// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.Collectable)]
    public class CollectableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public CollectableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(float distance)
        {
            return distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            // TODO: Impl
        }
    }
}