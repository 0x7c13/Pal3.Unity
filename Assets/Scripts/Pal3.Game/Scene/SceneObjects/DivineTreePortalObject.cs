// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using System.Threading;
    using Actor.Controllers;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Coroutine;
    using Engine.Extensions;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.DivineTreePortal)]
    public sealed class DivineTreePortalObject : SceneObject
    {
        private float MOVEMENT_ANIMATION_DURATION = 3f;

        private StandingPlatformController _platformController;
        private CancellationTokenSource _cancellationTokenSource = new ();
        private bool _isInteractionInProgress;

        public DivineTreePortalObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            var bounds = new Bounds
            {
                center = new Vector3(0f, -1f, 0f),
                size = new Vector3(9f, 2f, 9f),
            };

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;
            _platformController.OnPlayerActorExited += OnPlayerActorExited;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            if (_isInteractionInProgress) return;

            #if PAL3
            Pal3.Instance.Execute(new UIDisplayNoteCommand("等待三秒后，根据当前人物，会向上或向下传送哦。"));
            #elif PAL3A
            Pal3.Instance.Execute(new UIDisplayNoteCommand("等待三秒后，会开始传送哦。"));
            #endif
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            Pal3.Instance.StartCoroutine(CountDownForInteractionAsync(_cancellationTokenSource.Token));
        }

        private void OnPlayerActorExited(object sender, IGameEntity playerActorGameEntity)
        {
            if (_isInteractionInProgress) return;
            _cancellationTokenSource.Cancel();
        }

        private IEnumerator CountDownForInteractionAsync(CancellationToken cancellationToken)
        {
            yield return CoroutineYieldInstruction.WaitForSeconds(3f);
            if (cancellationToken.IsCancellationRequested) yield break;
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3
            var upperLevelScriptId = ObjectInfo.Parameters[0];
            var lowerLevelScriptId = ObjectInfo.Parameters[1];

            bool? shouldGoUp = null;

            if (upperLevelScriptId == ScriptConstants.InvalidScriptId)
            {
                shouldGoUp = false;
            }
            if (lowerLevelScriptId == ScriptConstants.InvalidScriptId)
            {
                shouldGoUp = true;
            }

            if (!shouldGoUp.HasValue)
            {
                shouldGoUp = (PlayerActorId) ctx.PlayerActorGameEntity.GetComponent<ActorController>().GetActor().Id is
                    PlayerActorId.JingTian or PlayerActorId.XueJian;
            }
            #elif PAL3A
            bool? shouldGoUp = ObjectInfo.Parameters[0] == 1;
            #endif

            IGameEntity portalEntity = GetGameEntity();
            Vector3 platformPosition = _platformController.Transform.Position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                _platformController.GetPlatformHeight(),
                platformPosition.z);

            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            Vector3 finalPosition = portalEntity.Transform.Position;
            finalPosition.y += shouldGoUp.Value ? 10f : -10f;

            yield return portalEntity.Transform.MoveAsync(finalPosition,
                MOVEMENT_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            yield return null;

            #if PAL3
            Pal3.Instance.Execute(new ScriptExecuteCommand(shouldGoUp.Value ? upperLevelScriptId : lowerLevelScriptId));
            #elif PAL3A
            ExecuteScriptIfAny();
            #endif

            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _isInteractionInProgress = false;

            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _platformController.OnPlayerActorExited -= OnPlayerActorExited;
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}