// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;

    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;

    [ScnSceneObject(SceneObjectType.AutoTrigger)]
    public sealed class AutoTriggerObject : SceneObject
    {
        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        public AutoTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.TileMapTriggerRect.IsEmpty)
            {
                if (ModelType == SceneObjectModelType.CvdModel)
                {
                    GetCvdModelRenderer().LoopAnimation();
                }
            }
            else
            {
                double effectiveTime = GameTimeProvider.Instance.RealTimeSinceStartup;

                // This is to prevent player from entering back to previous
                // scene when holding the stick while transferring between scenes.
                // We simply disable the auto trigger for a short time window after
                // a fresh scene load if the auto trigger is used for scene transfer.
                if (ObjectInfo.ScriptId != ScriptConstants.InvalidScriptId &&
                    ObjectInfo.ScriptId < ScriptConstants.PortalScriptIdMax)
                {
                    effectiveTime += 1f; // 1 second
                }

                _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
                _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex, effectiveTime);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }

            #if PAL3A
            if (ObjectInfo.Parameters[2] == 1 && GraphicsEffectType == GraphicsEffectType.Portal)
            {
                sceneObjectGameEntity.Transform.Rotation *= Quaternion.Euler(180, 0, 0);
            }
            #endif

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            if (_isInteractionInProgress) return; // Prevent re-entry
            _isInteractionInProgress = true;

            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.LinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                ExecuteScriptIfAny();
                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
            }
            else
            {
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
            }

            _isInteractionInProgress = false;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted()
        {
            // If object has a linked object, we should go to cutscene
            // else we should not.
            return ObjectInfo.LinkedObjectId != INVALID_SCENE_OBJECT_ID;
        }

        public override void Deactivate()
        {
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}