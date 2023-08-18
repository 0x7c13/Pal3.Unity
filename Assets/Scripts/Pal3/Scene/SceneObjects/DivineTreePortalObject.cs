// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using System.Threading;
    using Actor.Controllers;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.DivineTreePortal)]
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

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            var bounds = new Bounds
            {
                center = new Vector3(0f, -1f, 0f),
                size = new Vector3(9f, 2f, 9f),
            };

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;
            _platformController.OnPlayerActorExited += OnPlayerActorExited;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            if (_isInteractionInProgress) return;

            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new UIDisplayNoteCommand("等待三秒后，根据当前人物，会向上或向下传送哦。"));
            #elif PAL3A
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new UIDisplayNoteCommand("等待三秒后，会开始传送哦。"));
            #endif
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            Pal3.Instance.StartCoroutine(CountDownForInteractionAsync(_cancellationTokenSource.Token));
        }

        private void OnPlayerActorExited(object sender, GameObject playerActorGameObject)
        {
            if (_isInteractionInProgress) return;
            _cancellationTokenSource.Cancel();
        }

        private IEnumerator CountDownForInteractionAsync(CancellationToken cancellationToken)
        {
            yield return new WaitForSeconds(3f);
            if (cancellationToken.IsCancellationRequested) yield break;
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

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
                shouldGoUp = (PlayerActorId) ctx.PlayerActorGameObject.GetComponent<ActorController>().GetActor().Info.Id is
                    PlayerActorId.JingTian or PlayerActorId.XueJian;
            }
            #elif PAL3A
            bool? shouldGoUp = ObjectInfo.Parameters[0] == 1;
            #endif

            GameObject portalObject = GetGameObject();
            Vector3 platformPosition = _platformController.transform.position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                _platformController.GetPlatformHeight(),
                platformPosition.z);

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            Vector3 finalPosition = portalObject.transform.position;
            finalPosition.y += shouldGoUp.Value ? 10f : -10f;

            yield return portalObject.transform.MoveAsync(finalPosition,
                MOVEMENT_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            yield return null;

            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand(shouldGoUp.Value ? upperLevelScriptId : lowerLevelScriptId));
            #elif PAL3A
            ExecuteScriptIfAny();
            #endif

            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            _cancellationTokenSource.Cancel();
            _isInteractionInProgress = false;

            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _platformController.OnPlayerActorExited -= OnPlayerActorExited;
                Object.Destroy(_platformController);
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}