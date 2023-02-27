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
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.ElevatorDoor)]
    public sealed class ElevatorDoorObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public ElevatorDoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
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

            yield return AnimationHelper.MoveTransformAsync(doorObject.transform, toPosition, 2f);

            ToggleAndSaveSwitchState();
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