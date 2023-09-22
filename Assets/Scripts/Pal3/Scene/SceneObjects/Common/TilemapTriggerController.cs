// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Primitives;
    using UnityEngine;

    public class TilemapTriggerController : MonoBehaviour,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>
    {
        public event EventHandler<Vector2Int> OnPlayerActorEntered;
        public event EventHandler<Vector2Int> OnPlayerActorExited;

        private GameBoxRect _tileMapTriggerRect;
        private int _layerIndex;
        private double _effectiveTime;

        private bool _wasTriggered;
        private bool _isScriptRunningInProgress;

        public void Init(GameBoxRect tileMapTriggerRect, int layerIndex)
        {
            Init(tileMapTriggerRect, layerIndex, Time.realtimeSinceStartupAsDouble);
        }

        public void Init(GameBoxRect tileMapTriggerRect, int layerIndex, double effectiveTime)
        {
            _tileMapTriggerRect = tileMapTriggerRect;
            _layerIndex = layerIndex;
            _effectiveTime = effectiveTime;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification notification)
        {
            if (notification.LayerIndex != _layerIndex || notification.MovedByScript) return;

            bool isInsideTriggerArea = _tileMapTriggerRect.IsPointInsideRect(notification.Position.x, notification.Position.y);

            if (!isInsideTriggerArea)
            {
                if (_wasTriggered)
                {
                    _wasTriggered = false;
                    OnPlayerActorExited?.Invoke(this, notification.Position);
                }
                return;
            }

            if (Time.realtimeSinceStartupAsDouble < _effectiveTime) return;

            if (!_wasTriggered)
            {
                _wasTriggered = true;
                OnPlayerActorEntered?.Invoke(this, notification.Position);
            }
        }
    }
}