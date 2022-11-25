// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Door)]
    public class DoorObject : SceneObject,
        ICommandExecutor<ScriptFinishedRunningNotification>
    {
        private TilemapAutoTriggerController _triggerController;
        private bool _isScriptRunningInProgress;
        
        public DoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _triggerController = sceneGameObject.AddComponent<TilemapAutoTriggerController>();
            
            // This is to prevent player from entering back to previous
            // scene when holding the stick while transferring between scenes.
            // We simply disable the auto trigger for a short time window after
            // a fresh scene load.
            var effectiveTime = Time.realtimeSinceStartupAsDouble + 1f;
            
            _triggerController.Init(Info.TileMapTriggerRect, Info.LayerIndex, effectiveTime);
            _triggerController.OnTriggerEntered += OnTriggerEnter;
            
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
            
            return sceneGameObject;
        }

        private void OnTriggerEnter(object sender, Vector2Int actorTilePosition)
        {
            if (_isScriptRunningInProgress) return; // Prevent re-entry

            _isScriptRunningInProgress = true;
            
            // There are doors controlled by the script for it's behaviour & animation which have
            // parameters[0] set to 1, so we are only playing the animation if parameters[0] == 0.
            if (Info.Parameters[0] == 0 && ModelType == SceneObjectModelType.CvdModel)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(0));
                
                var timeScale = 2f; // Make the animation 2X faster for better user experience
                var durationPercentage = 0.7f; // Just play 70% of the whole animation (good enough).

                GetCvdModelRenderer().PlayAnimation(timeScale, loopCount: 1, durationPercentage,
                    onFinished: () =>
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
                        ExecuteScriptIfAny();
                    });
            }
            else
            {
                ExecuteScriptIfAny();   
            }
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            
            _isScriptRunningInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnTriggerEntered -= OnTriggerEnter;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
        
        public void Execute(ScriptFinishedRunningNotification command)
        {
            if (command.ScriptId == Info.ScriptId)
            {
                _isScriptRunningInProgress = false;
            }
        }
    }
}