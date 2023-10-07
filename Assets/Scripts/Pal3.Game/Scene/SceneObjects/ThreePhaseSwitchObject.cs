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
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;

    [ScnSceneObject(SceneObjectType.ThreePhaseSwitch)]
    public sealed class ThreePhaseSwitchObject : SceneObject,
        ICommandExecutor<ThreePhaseSwitchStateChangedNotification>
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;
        private const float SWITCH_ANIMATION_DURATION = 2f;
        private const float SWITCH_ROTAION_ANGLE = 30f;

        private SceneObjectMeshCollider _meshCollider;

        private readonly SceneStateManager _sceneStateManager;

        // State is either -1, 0 or 1 (0 is the default state)
        private int _previousState;
        private int _currentState;

        public ThreePhaseSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo is {Times: > 0};
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();

            if (_sceneStateManager.TryGetSceneObjectStateOverride(SceneInfo.CityName, SceneInfo.SceneName,
                    ObjectInfo.Id, out SceneObjectStateOverride stateOverride))
            {
                if (stateOverride.ThreePhaseSwitchPreviousState.HasValue)
                {
                    _previousState = stateOverride.ThreePhaseSwitchPreviousState.Value;
                }
                if (stateOverride.ThreePhaseSwitchCurrentState.HasValue)
                {
                    _currentState = stateOverride.ThreePhaseSwitchCurrentState.Value;
                }
            }
            else
            {
                _previousState = 0;
                _currentState = ObjectInfo.Parameters[1];
            }

            var direction = ObjectInfo.Parameters[3] == -1 ? -1 : 1;
            sceneObjectGameEntity.Transform.Rotation *=
                Quaternion.Euler(0, 0, SWITCH_ROTAION_ANGLE * -_currentState * direction);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);

            return sceneObjectGameEntity;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ctx.StartedByPlayer && ctx.InitObjectId == ObjectInfo.Id)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionToNameMap[ActorActionType.Check], 1));

                int nextState = _currentState switch
                {
                    1 or -1 => 0,
                    0 when _previousState == 1 => -1,
                    0 when _previousState is -1 or 0 => 1,
                    _ => _currentState
                };

                _previousState = _currentState;
                _currentState = nextState;

                // Notify other three phase switches as well as three phase bridges
                CommandDispatcher<ICommand>.Instance.Dispatch(new ThreePhaseSwitchStateChangedNotification(
                    ObjectInfo.Id, _previousState, _currentState, ObjectInfo.Parameters[1] == 1));

                PlaySfxIfAny();
            }

            ITransform objectTransform = GetGameEntity().Transform;
            Quaternion rotation = objectTransform.Rotation;
            int direction = (_currentState - _previousState) * (ObjectInfo.Parameters[3] == -1 ? -1 : 1);
            Quaternion targetRotation = rotation * Quaternion.Euler(0, 0, SWITCH_ROTAION_ANGLE * -direction);

            yield return objectTransform.RotateAsync(targetRotation, SWITCH_ANIMATION_DURATION, AnimationCurveType.Sine);

            // Save my state
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneSaveGlobalThreePhaseSwitchStateCommand(
                SceneInfo.CityName, SceneInfo.SceneName, ObjectInfo.Id, _previousState, _currentState));
        }

        public void Execute(ThreePhaseSwitchStateChangedNotification notification)
        {
            if (notification.ObjectId == ObjectInfo.Id) return; // Ignore self

            if (_previousState != notification.PreviousState ||
                _currentState != notification.CurrentState)
            {
                // Sync state
                _previousState = notification.PreviousState;
                _currentState = notification.CurrentState;
                RequestForInteraction();
            }
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

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