// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.ThreePhaseBridge)]
    public sealed class ThreePhaseBridgeObject : SceneObject,
        ICommandExecutor<ThreePhaseSwitchStateChangedNotification>
    {
        private const float BRIDGE_ANIMATION_DURATION = 2f;
        private const float BRIDGE_MOVEMENT_DISTANCE = 3.65f;

        private StandingPlatformController _standingPlatformController;

        private readonly SceneStateManager _sceneStateManager;

        private bool _isMovingTowardsNegativeAxis = true;

        public ThreePhaseBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            // Parameters[5] == 1 means not walkable
            if (ObjectInfo.Parameters[5] == 0)
            {
                _standingPlatformController = sceneGameObject.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(bounds, ObjectInfo.LayerIndex);
            }

            // Set initial position for three phase bridges in scene M13-3 and M13-4
            if (!_sceneStateManager.TryGetSceneObjectStateOverride(
                    SceneInfo.CityName, SceneInfo.SceneName, ObjectInfo.Id, out _))
            {
                if (SceneInfo.Is("m13", "3") ||
                    SceneInfo.Is("m13", "4"))
                {
                    sceneGameObject.transform.position +=
                        sceneGameObject.transform.up * -BRIDGE_MOVEMENT_DISTANCE;
                }
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            Transform transform = GetGameObject().transform;

            Vector3 movingDirection = transform.right;

            // Bridges in scene M13-3 and M13-4 move along Y axis
            if (SceneInfo.Is("m13", "3") ||
                SceneInfo.Is("m13", "4"))
            {
                movingDirection = transform.up;
            }

            Vector3 finalPosition = transform.position +
                                    movingDirection * ((_isMovingTowardsNegativeAxis ? -1 : 1) * BRIDGE_MOVEMENT_DISTANCE);

            yield return transform.MoveAsync(finalPosition,
                BRIDGE_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            SaveCurrentPosition();
        }

        public void Execute(ThreePhaseSwitchStateChangedNotification notification)
        {
            _isMovingTowardsNegativeAxis = notification.CurrentState > notification.PreviousState;
            RequestForInteraction();
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            if (_standingPlatformController != null)
            {
                Object.Destroy(_standingPlatformController);
                _standingPlatformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif