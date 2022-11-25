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
    
    [ScnSceneObject(ScnSceneObjectType.AutoTrigger)]
    [ScnSceneObject(ScnSceneObjectType.DivineTreePortal)]
    public class AutoTriggerObject : SceneObject,
        ICommandExecutor<ScriptFinishedRunningNotification>
    {
        private TilemapAutoTriggerController _triggerController;
        private bool _isScriptRunningInProgress;
        
        public AutoTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _triggerController = sceneGameObject.AddComponent<TilemapAutoTriggerController>();
            
            // This is to prevent player from entering back to previous
            // scene when holding the stick while transferring between scenes.
            // We simply disable the auto trigger for a short time window after
            // a fresh scene load.
            var effectiveTime = Time.realtimeSinceStartupAsDouble + 0.4f;
            
            _triggerController.Init(Info.TileMapTriggerRect, Info.LayerIndex, effectiveTime);
            _triggerController.OnTriggerEntered += OnTriggerEntered;
            
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
            
            return sceneGameObject;
        }

        private void OnTriggerEntered(object sender, Vector2Int actorTilePosition)
        {
            if (_isScriptRunningInProgress) return; // Prevent re-entry

            _isScriptRunningInProgress = true;

            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int)Info.ScriptId));
        }

        public void Execute(ScriptFinishedRunningNotification command)
        {
            if (command.ScriptId == Info.ScriptId)
            {
                _isScriptRunningInProgress = false;
            }
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            _isScriptRunningInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnTriggerEntered -= OnTriggerEntered;
                Object.Destroy(_triggerController);
            }
            
            base.Deactivate();
        }
    }
}