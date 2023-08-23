// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Services;
    using Data;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.MushroomBridge)]
    public sealed class MushroomBridgeObject : SceneObject
    {
        private const float ROTATION_DEGREES_PER_SECOND = 45f;

        private StandingPlatformController _platformController;

        private readonly Tilemap _tilemap;

        public MushroomBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = new()
            {
                center = new Vector3(0f, -0.5f, 0f),
                size = new Vector3(9f, 1f, 4f),
            };

            // Add a standing platform controller to make sure the player can walk on the bridge
            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            if (ObjectInfo.SwitchState == 1)
            {
                Vector2Int centerTile = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                Vector3 centerPoint = _tilemap.GetWorldPosition(centerTile, ObjectInfo.LayerIndex);
                float toDegrees = ObjectInfo.Parameters[2];
                sceneGameObject.transform.RotateAround(centerPoint, Vector3.up, toDegrees);
            }

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            Transform transform = GetGameObject().transform;
            Vector2Int centerTile = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
            Vector3 centerPoint = ctx.CurrentScene.GetTilemap().GetWorldPosition(centerTile, ObjectInfo.LayerIndex);

            float toDegrees = ObjectInfo.Parameters[2];
            float currentDegrees = 0f;

            float direction = ObjectInfo.SwitchState == 1 ? Mathf.Sign(toDegrees) : -Mathf.Sign(toDegrees);
            toDegrees = Mathf.Abs(toDegrees);

            while (currentDegrees < toDegrees)
            {
                float deltaDegrees = ROTATION_DEGREES_PER_SECOND * Time.deltaTime;

                if (deltaDegrees + currentDegrees > toDegrees)
                {
                    deltaDegrees = toDegrees - currentDegrees;
                }

                transform.RotateAround(centerPoint, Vector3.up, deltaDegrees * direction);

                currentDegrees += deltaDegrees;
                yield return null;
            }
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif