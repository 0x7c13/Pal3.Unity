// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.AutoTrigger)]
    public sealed class AutoTriggerObject : SceneObject
    {
        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        public AutoTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.TileMapTriggerRect.IsEmpty)
            {
                if (ModelType == SceneObjectModelType.CvdModel)
                {
                    GetCvdModelRenderer().LoopAnimation();
                }
            }
            else
            {
                // This is to prevent player from entering back to previous
                // scene when holding the stick while transferring between scenes.
                // We simply disable the auto trigger for a short time window after
                // a fresh scene load.
                var effectiveTime = Time.realtimeSinceStartupAsDouble + 0.4f;
                _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
                _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex, effectiveTime);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }

            #if PAL3A
            if (ObjectInfo.Parameters[2] == 1 && GraphicsEffect == GraphicsEffect.Portal)
            {
                sceneGameObject.transform.rotation *= Quaternion.Euler(180, 0, 0);
            }
            #endif

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
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
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                ExecuteScriptIfAny();
                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
            }
            else
            {
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
            }

            _isInteractionInProgress = false;
        }

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
                Object.Destroy(_triggerController);
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}