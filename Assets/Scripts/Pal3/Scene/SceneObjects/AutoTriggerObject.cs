// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using Effect;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Door)]
    [ScnSceneObject(ScnSceneObjectType.AutoTrigger)]
    [ScnSceneObject(ScnSceneObjectType.DivineTreePortal)]
    public class AutoTriggerObject : SceneObject
    {
        public GraphicsEffect GraphicsEffect { get; } = GraphicsEffect.None;

        public AutoTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            GraphicsEffect = EffectTypeResolver.GetEffectByName(objectInfo.Name);
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            var sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<AutoTriggerObjectController>().Init(resourceProvider, this);
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

        private Component _effectComponent;
        private double _awakeTime;

        public void Init(GameResourceProvider resourceProvider, AutoTriggerObject autoTriggerObject)
        {
            _awakeTime = Time.realtimeSinceStartupAsDouble;

            _autoTrigger = autoTriggerObject;

            if (_autoTrigger.GraphicsEffect == GraphicsEffect.None) return;

            var effectComponentType = EffectTypeResolver.GetEffectComponentType(_autoTrigger.GraphicsEffect);
            _effectComponent = gameObject.AddComponent(effectComponentType);
            (_effectComponent as IEffect)!.Init(resourceProvider, _autoTrigger.Info.EffectModelType);
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            Destroy(_effectComponent);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Trigger()
        {
            if (_isScriptRunningInProgress) return;

            if (_autoTrigger.Info.Times != 0xFF)
            {
                if (_autoTrigger.Info.Times <= 0) return;
                else _autoTrigger.Info.Times--;
            }
            
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
            var disableTime = _autoTrigger.Info.Type == ScnSceneObjectType.Door ? 1f : 0.5f;
            if (Time.realtimeSinceStartupAsDouble - _awakeTime < disableTime) return;

            if (!_wasTriggered)
            {
                _wasTriggered = true;
                Trigger();
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