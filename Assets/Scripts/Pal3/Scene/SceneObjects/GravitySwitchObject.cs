// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using Player;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.GravitySwitch)]
    public sealed class GravitySwitchObject : SceneObject
    {
        private const float DESCENDING_HEIGHT = 0.5f;
        private const float DESCENDING_ANIMATION_DURATION = 2.5f;

        private StandingPlatformController _platformController;

        private readonly TeamManager _teamManager;

        public GravitySwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _teamManager = ServiceLocator.Instance.Get<TeamManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // wz06.cvd
            var bounds = new Bounds
            {
                center = new Vector3(0f, 0.8f, 0f),
                size = new Vector3(4f, 1f, 4f),
            };

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set to final position if it is already activated
            if (ObjectInfo.Times == 0)
            {
                Vector3 finalPosition = sceneGameObject.transform.position;
                finalPosition.y -= DESCENDING_HEIGHT;
                sceneGameObject.transform.position = finalPosition;
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            // Check if total team members are equal to or greater than required headcount
            if (_teamManager.GetActorsInTeam().Count >= ObjectInfo.Parameters[0])
            {
                // Gravity switch can only be activated once, but there is one in PAL3 M24-3
                // scene that can be activated multiple times, we should fix it here.
                if (ObjectInfo.Times == INFINITE_TIMES_COUNT) ObjectInfo.Times = 1;

                if (!IsInteractableBasedOnTimesCount()) return;
                RequestForInteraction();
            }
            else if (ObjectInfo.Times > 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("重量不足，无法激活"));
            }
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            GameObject gravityTriggerGo = GetGameObject();
            Vector3 platformPosition = _platformController.transform.position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                _platformController.GetPlatformHeight(),
                platformPosition.z);

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            PlaySfx("we026");

            yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

            PlaySfx("wg005");

            Vector3 finalPosition = gravityTriggerGo.transform.position;
            finalPosition.y -= DESCENDING_HEIGHT;
            yield return AnimationHelper.MoveTransformAsync(gravityTriggerGo.transform,
                finalPosition,
                DESCENDING_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);

            ExecuteScriptIfAny();
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}

#endif