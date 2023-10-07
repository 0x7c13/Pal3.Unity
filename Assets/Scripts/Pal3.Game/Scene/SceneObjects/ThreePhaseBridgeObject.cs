// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.ThreePhaseBridge)]
    public sealed class ThreePhaseBridgeObject : SceneObject,
        ICommandExecutor<ThreePhaseSwitchStateChangedNotification>
    {
        private const float BRIDGE_ANIMATION_DURATION = 2f;
        private const float BRIDGE_MOVEMENT_DISTANCE = 3.65f;

        private StandingPlatformController _standingPlatformController;

        private readonly SceneStateManager _sceneStateManager;

        private bool _isMovingAlongYAxis;
        private bool _isMovingTowardsNegativeAxis = true;

        public ThreePhaseBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();

            // Three-phase bridges in scene M13-3 and M13-4 are moving along Y axis
            if (sceneInfo.Is("m13", "3") ||
                sceneInfo.Is("m13", "4"))
            {
                _isMovingAlongYAxis = true;
            }
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            // Parameters[5] == 1 means not walkable
            if (ObjectInfo.Parameters[5] == 0)
            {
                _standingPlatformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(bounds, ObjectInfo.LayerIndex);
            }

            if (!_sceneStateManager.TryGetSceneObjectStateOverride(
                    SceneInfo.CityName, SceneInfo.SceneName, ObjectInfo.Id, out _) &&
                _isMovingAlongYAxis)
            {
                sceneObjectGameEntity.Transform.Position +=
                    sceneObjectGameEntity.Transform.Up * -BRIDGE_MOVEMENT_DISTANCE;
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            ITransform transform = GetGameEntity().Transform;

            Vector3 movingDirection = transform.Right;

            // Bridges that move along Y axis
            if (_isMovingAlongYAxis)
            {
                movingDirection = transform.Up;
            }

            Vector3 finalPosition = transform.Position +
                                    movingDirection * ((_isMovingTowardsNegativeAxis ? -1 : 1) * BRIDGE_MOVEMENT_DISTANCE);

            yield return transform.MoveAsync(finalPosition,
                BRIDGE_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            SaveCurrentPosition();
        }

        public void Execute(ThreePhaseSwitchStateChangedNotification notification)
        {
            _isMovingAlongYAxis = notification.IsBridgeMovingAlongYAxis;
            _isMovingTowardsNegativeAxis = notification.CurrentState > notification.PreviousState;
            RequestForInteraction();
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            if (_standingPlatformController != null)
            {
                _standingPlatformController.Destroy();
                _standingPlatformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif