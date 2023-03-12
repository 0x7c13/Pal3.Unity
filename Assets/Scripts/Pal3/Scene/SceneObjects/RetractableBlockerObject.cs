// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.RetractableBlocker)]
    public sealed class RetractableBlockerObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public RetractableBlockerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            GameObject blockerObject = GetGameObject();
            Vector3 currentPosition = blockerObject.transform.position;
            var zOffset = GameBoxInterpreter.ToUnityDistance(ObjectInfo.Parameters[2]);
            Vector3 toPosition = currentPosition + blockerObject.transform.forward * -zOffset;

            yield return AnimationHelper.MoveTransformAsync(blockerObject.transform, toPosition, 1.5f);

            SaveCurrentPosition();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            base.Deactivate();
        }
    }
}