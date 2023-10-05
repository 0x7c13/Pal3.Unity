// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Extensions;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.RetractableBlocker)]
    public sealed class RetractableBlockerObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public RetractableBlockerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            GameObject blockerObject = GetGameObject();
            Vector3 currentPosition = blockerObject.transform.position;
            var zOffset = ((float)ObjectInfo.Parameters[2]).ToUnityDistance();
            Vector3 toPosition = currentPosition + blockerObject.transform.forward * -zOffset;

            yield return blockerObject.transform.MoveAsync(toPosition, 1.5f);

            SaveCurrentPosition();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}

#endif