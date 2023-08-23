// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Animation;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Data;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.ElevatorDoor)]
    public sealed class ElevatorDoorObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public ElevatorDoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            // Add collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            PlaySfx("wg005");

            GameObject doorObject = GetGameObject();
            Vector3 currentPosition = doorObject.transform.position;
            Vector3 toPosition = currentPosition +
                (ObjectInfo.SwitchState == 0 ? Vector3.down : Vector3.up) * (GetMeshBounds().size.y + 0.5f);

            yield return doorObject.transform.MoveAsync(toPosition, 2f);

            FlipAndSaveSwitchState();
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