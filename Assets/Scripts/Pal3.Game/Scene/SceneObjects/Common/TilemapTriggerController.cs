// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects.Common
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Primitives;
    using Engine.Core.Implementation;
    using Engine.Services;

    public sealed class TilemapTriggerController : GameEntityScript,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>
    {
        public event EventHandler<(int x, int y)> OnPlayerActorEntered;
        public event EventHandler<(int x, int y)> OnPlayerActorExited;

        private GameBoxRect _tileMapTriggerRect;
        private int _layerIndex;
        private double _effectiveTime;

        private bool _wasTriggered;
        private bool _isScriptRunningInProgress;

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Init(GameBoxRect tileMapTriggerRect, int layerIndex)
        {
            Init(tileMapTriggerRect, layerIndex, GameTimeProvider.Instance.RealTimeSinceStartup);
        }

        public void Init(GameBoxRect tileMapTriggerRect, int layerIndex, double effectiveTime)
        {
            _tileMapTriggerRect = tileMapTriggerRect;
            _layerIndex = layerIndex;
            _effectiveTime = effectiveTime;
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification notification)
        {
            if (notification.LayerIndex != _layerIndex || notification.MovedByScript) return;

            bool isInsideTriggerArea = _tileMapTriggerRect.IsPointInsideRect(notification.TileXPosition,
                notification.TileYPosition);

            if (!isInsideTriggerArea)
            {
                if (_wasTriggered)
                {
                    _wasTriggered = false;
                    OnPlayerActorExited?.Invoke(this, (notification.TileXPosition, notification.TileYPosition));
                }
                return;
            }

            if (GameTimeProvider.Instance.RealTimeSinceStartup < _effectiveTime) return;

            if (!_wasTriggered)
            {
                _wasTriggered = true;
                OnPlayerActorEntered?.Invoke(this, (notification.TileXPosition, notification.TileYPosition));
            }
        }
    }
}