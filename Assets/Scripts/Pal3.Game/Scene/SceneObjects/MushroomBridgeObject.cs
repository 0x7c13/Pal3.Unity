// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Abstraction;
    using Engine.Extensions;
    using Engine.Services;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.MushroomBridge)]
    public sealed class MushroomBridgeObject : SceneObject
    {
        private const float ROTATION_DEGREES_PER_SECOND = 45f;

        private readonly IGameTimeProvider _gameTimeProvider;
        private StandingPlatformController _platformController;
        private readonly Tilemap _tilemap;

        public MushroomBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameTimeProvider = ServiceLocator.Instance.Get<IGameTimeProvider>();
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = new()
            {
                center = new Vector3(0f, -0.5f, 0f),
                size = new Vector3(9f, 1f, 4f),
            };

            // Add a standing platform controller to make sure the player can walk on the bridge
            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            if (ObjectInfo.SwitchState == 1)
            {
                Vector2Int centerTile = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                Vector3 centerPoint = _tilemap.GetWorldPosition(centerTile, ObjectInfo.LayerIndex);
                float toDegrees = ObjectInfo.Parameters[2];
                sceneObjectGameEntity.Transform.RotateAround(centerPoint, Vector3.up, toDegrees);
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            Vector2Int centerTile = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
            Vector3 centerPoint = ctx.CurrentScene.GetTilemap().GetWorldPosition(centerTile, ObjectInfo.LayerIndex);

            float toDegrees = ObjectInfo.Parameters[2];
            float currentDegrees = 0f;

            float direction = ObjectInfo.SwitchState == 1 ? MathF.Sign(toDegrees) : -MathF.Sign(toDegrees);
            toDegrees = MathF.Abs(toDegrees);

            ITransform entityTransform = GetGameEntity().Transform;

            while (currentDegrees < toDegrees)
            {
                float deltaDegrees = ROTATION_DEGREES_PER_SECOND * _gameTimeProvider.DeltaTime;

                if (deltaDegrees + currentDegrees > toDegrees)
                {
                    deltaDegrees = toDegrees - currentDegrees;
                }

                entityTransform.RotateAround(centerPoint, Vector3.up, deltaDegrees * direction);

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