// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Actor.Controllers;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using GameSystems.Team;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.GravitySwitch)]
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

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // wz06.cvd
            var bounds = new Bounds
            {
                center = new Vector3(0f, 0.8f, 0f),
                size = new Vector3(4f, 1f, 4f),
            };

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set to final position if it is already activated
            if (ObjectInfo.Times == 0)
            {
                Vector3 finalPosition = sceneObjectGameEntity.Transform.Position;
                finalPosition.y -= DESCENDING_HEIGHT;
                sceneObjectGameEntity.Transform.Position = finalPosition;
            }

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
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
                Pal3.Instance.Execute(new UIDisplayNoteCommand("重量不足，无法激活"));
            }
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            IGameEntity gravityTriggerEntity = GetGameEntity();
            Vector3 platformPosition = _platformController.Transform.Position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                _platformController.GetPlatformHeight(),
                platformPosition.z);

            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            PlaySfx("we026");

            yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

            PlaySfx("wg005");

            Vector3 finalPosition = gravityTriggerEntity.Transform.Position;
            finalPosition.y -= DESCENDING_HEIGHT;
            yield return gravityTriggerEntity.Transform.MoveAsync(finalPosition,
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
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif