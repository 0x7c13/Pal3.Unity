// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using Effect;
    using MetaData;
    using Script;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Door)]
    [ScnSceneObject(ScnSceneObjectType.AutoTrigger)]
    [ScnSceneObject(ScnSceneObjectType.DivineTreePortal)]
    public class AutoTriggerObject : SceneObject
    {
        public AutoTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<AutoTriggerObjectController>().Init(this);
            return sceneGameObject;
        }
    }

    public class AutoTriggerObjectController : MonoBehaviour,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>,
        ICommandExecutor<ScriptFinishedRunningNotification>
    {
        private AutoTriggerObject _autoTrigger;
        private bool _wasTriggered;
        private bool _isScriptRunningInProgress;
        
        private double _awakeTime;

        public void Init(AutoTriggerObject autoTriggerObject)
        {
            _awakeTime = Time.realtimeSinceStartupAsDouble;
            _autoTrigger = autoTriggerObject;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Trigger(Vector2Int playerActorTilePosition)
        {
            if (_isScriptRunningInProgress) return;

            if (_autoTrigger.Info.Times != 0xFF)
            {
                if (_autoTrigger.Info.Times <= 0) return;
                else _autoTrigger.Info.Times--;
            }
            
            // For debugging purposes
            // {
            //     var currentScene = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene();
            //     var mainStoryVarValue = ServiceLocator.Instance.Get<ScriptManager>()
            //         .GetGlobalVariables()[ScriptConstants.MainStoryVariableName];
            //     var actorMovementController = currentScene.GetActorGameObject(0).GetComponent<ActorMovementController>();
            //     
            //     Debug.LogError($"INFO: {currentScene.GetSceneInfo().CityName.ToLower()}_{currentScene.GetSceneInfo().Name.ToLower()}_{mainStoryVarValue} " +
            //                    $"| {actorMovementController.GetCurrentLayerIndex()}_{playerActorTilePosition}");
            // }

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand((int)_autoTrigger.Info.ScriptId));
            _isScriptRunningInProgress = true;
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification notification)
        {
            if (notification.LayerIndex != _autoTrigger.Info.OnLayer || notification.MovedByScript) return;

            bool isInsideTriggerArea = GameBoxInterpreter.IsPositionInsideRect(
                _autoTrigger.Info.TileMapTriggerRect, notification.Position);

            if (!isInsideTriggerArea)
            {
                _wasTriggered = false;
                return;
            }

            // TODO: This is to prevent player from entering back to previous
            // scene when holding the stick while transferring between scenes.
            // We simply disable the auto trigger for a short time window after
            // a fresh scene load.
            var disableTime = _autoTrigger.Info.Type == ScnSceneObjectType.Door ? 1f : 0.45f;
            if (Time.realtimeSinceStartupAsDouble - _awakeTime < disableTime) return;

            if (!_wasTriggered)
            {
                _wasTriggered = true;
                Trigger(notification.Position);
            }
        }

        public void Execute(ScriptFinishedRunningNotification command)
        {
            if (command.ScriptId == _autoTrigger.Info.ScriptId)
            {
                _isScriptRunningInProgress = false;
            }
        }
    }
}