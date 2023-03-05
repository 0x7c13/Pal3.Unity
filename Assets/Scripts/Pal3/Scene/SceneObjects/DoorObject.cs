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
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Door)]
    public sealed class DoorObject : SceneObject
    {
        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        public DoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            #if PAL3A
            if (!(ObjectInfo.Parameters[1] == 1 && ObjectInfo.Parameters[2] == 1))
            #endif
            {
                // This is to prevent player from entering back to previous
                // scene when holding the stick while transferring between scenes.
                // We simply disable the auto trigger for a short time window after
                // a fresh scene load.
                var effectiveTime = Time.realtimeSinceStartupAsDouble + 1f;
                _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
                _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex, effectiveTime);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            if (_isInteractionInProgress) return; // Prevent re-entry
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3A
            // Some door objects in PAL3A have parameters[1] and parameters[2] set to 1
            // which means they should deactivate themselves when interaction triggerred.
            if (ObjectInfo.Parameters[1] == 1 && ObjectInfo.Parameters[2] == 1)
            {
                ChangeAndSaveActivationState(false);
                yield break;
            }
            #endif

            // There are doors controlled by the script for it's behaviour & animation which have
            // parameters[0] set to 1, so we are only playing the animation if parameters[0] == 0.
            if (ObjectInfo.Parameters[0] == 0 && ModelType == SceneObjectModelType.CvdModel)
            {
                var timeScale = 2f; // Make the animation 2X faster for better user experience
                var durationPercentage = 0.7f; // Just play 70% of the whole animation (good enough).
                yield return GetCvdModelRenderer().PlayAnimationAsync(timeScale, loopCount: 1, durationPercentage, true);
            }

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}