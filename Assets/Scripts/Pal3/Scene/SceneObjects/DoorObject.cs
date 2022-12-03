// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
        private bool _isScriptRunningInProgress;

        public DoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // This is to prevent player from entering back to previous
            // scene when holding the stick while transferring between scenes.
            // We simply disable the auto trigger for a short time window after
            // a fresh scene load.
            var effectiveTime = Time.realtimeSinceStartupAsDouble + 1f;
            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex, effectiveTime);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            if (_isScriptRunningInProgress) return; // Prevent re-entry
            _isScriptRunningInProgress = true;
            Pal3.Instance.StartCoroutine(Interact(true));
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            // There are doors controlled by the script for it's behaviour & animation which have
            // parameters[0] set to 1, so we are only playing the animation if parameters[0] == 0.
            if (ObjectInfo.Parameters[0] == 0 && ModelType == SceneObjectModelType.CvdModel)
            {
                // Just disable player input during door animation.
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(0));

                var timeScale = 2f; // Make the animation 2X faster for better user experience
                var durationPercentage = 0.7f; // Just play 70% of the whole animation (good enough).
                yield return GetCvdModelRenderer().PlayAnimation(timeScale, loopCount: 1, durationPercentage, true);

                // Re-enable player input after door animation to switch game into GamePlay
                // state because there is a logic in CameraManager which saves the current camera
                // position and rotation before entering next scene based on the GameState.
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
            }

            yield return ExecuteScriptAndWaitForFinishIfAny();
            _isScriptRunningInProgress = false;
        }

        public override void Deactivate()
        {
            _isScriptRunningInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}