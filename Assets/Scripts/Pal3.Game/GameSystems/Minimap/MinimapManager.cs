// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Minimap
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.Utilities;
    using Engine.Abstraction;
    using Engine.Extensions;
    using Scene;
    using State;
    using UnityEngine;
    using UnityEngine.UI;

    public sealed class MinimapManager : IDisposable,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float MINIMAP_SCALE = 2.5f;

        private readonly ITransform _cameraTransform;
        private readonly SceneManager _sceneManager;

        private readonly CanvasGroup _miniMapCanvasGroup;
        private readonly RectTransform _miniMapRectTransform;
        private readonly float _miniMapWidth;
        private readonly Image _miniMapImage;
        private readonly MinimapTextureCreator _miniMapTextureCreator;

        private Texture2D[] _miniMapTextures;
        private Sprite[] _miniMapSprites;

        private Tilemap _currentTilemap;
        private int _currentLayerIndex = -1;
        private GameState _currentGameState = GameState.UI;

        public MinimapManager(ITransform cameraTransform,
            SceneManager sceneManager,
            CanvasGroup miniMapCanvasGroup,
            Image miniMapImage,
            MinimapTextureCreator minimapTextureCreator)
        {
            _cameraTransform = Requires.IsNotNull(cameraTransform, nameof(cameraTransform));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _miniMapCanvasGroup = Requires.IsNotNull(miniMapCanvasGroup, nameof(miniMapCanvasGroup));
            _miniMapImage = Requires.IsNotNull(miniMapImage, nameof(miniMapImage));
            _miniMapImage.preserveAspect = true;

            _miniMapTextureCreator = Requires.IsNotNull(minimapTextureCreator, nameof(minimapTextureCreator));

            _miniMapRectTransform = Requires.IsNotNull(_miniMapCanvasGroup.GetComponent<RectTransform>(), nameof(miniMapImage));
            _miniMapWidth = _miniMapRectTransform.rect.width;
            _miniMapCanvasGroup.alpha = 0f;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            DisposeCurrentMapAndReset();
        }

        public void LateUpdate(float deltaTime)
        {
            _miniMapRectTransform.localRotation = Quaternion.Euler(0, 0, _cameraTransform.EulerAngles.y + 180f);
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification command)
        {
            if (_currentTilemap == null) return;

            NavTileLayer layer = _currentTilemap.GetLayer(command.LayerIndex);

            // Swap the sprite if the player has moved to another layer
            if (_currentLayerIndex != command.LayerIndex)
            {
                _currentLayerIndex = command.LayerIndex;
                _miniMapImage.sprite = _miniMapSprites[_currentLayerIndex];

                float scale = MathF.Max(layer.Width, layer.Height) / _miniMapWidth * MINIMAP_SCALE;
                _miniMapImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            // NOTE: the texture is flipped vertically compared to the tilemap space
            Vector2 playerPixelPosition = new Vector2(command.TileXPosition, layer.Height - command.TileYPosition - 1);
            Vector2 textureCenter = new Vector2(layer.Width / 2f, layer.Height / 2f);
            Vector2 offset = playerPixelPosition - textureCenter;

            _miniMapImage.rectTransform.anchoredPosition = -offset * MINIMAP_SCALE;

            if (_currentGameState == GameState.Gameplay &&
                _miniMapCanvasGroup.alpha == 0f)
            {
                _miniMapCanvasGroup.alpha = 1f;
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _miniMapCanvasGroup.alpha = 0f;
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (command.NewSceneInfo.SceneType == SceneType.InDoor) return;

            _currentTilemap = _sceneManager.GetCurrentScene().GetTilemap();
            _miniMapTextures = new Texture2D[_currentTilemap.GetLayerCount()];
            _miniMapSprites = new Sprite[_currentTilemap.GetLayerCount()];

            for (var i = 0; i < _currentTilemap.GetLayerCount(); i++)
            {
                NavTileLayer tileLayer = _currentTilemap.GetLayer(i);
                _miniMapTextures[i] = _miniMapTextureCreator.CreateMinimapTexture(tileLayer);

                _miniMapSprites[i] = Sprite.Create(_miniMapTextures[i],
                    new Rect(0, 0, tileLayer.Width, tileLayer.Height),
                    new Vector2(0.5f, 0.5f));
            }

            _currentLayerIndex = -1;
        }

        private void DisposeCurrentMapAndReset()
        {
            _miniMapCanvasGroup.alpha = 0f;

            if (_miniMapSprites != null)
            {
                foreach (Sprite sprite in _miniMapSprites)
                {
                    sprite.Destroy();
                }

                _miniMapSprites = null;
            }

            if (_miniMapTextures != null)
            {
                foreach (Texture2D texture in _miniMapTextures)
                {
                    texture.Destroy();
                }

                _miniMapTextures = null;
            }

            _currentTilemap = null;
            _miniMapImage.sprite = null;
            _currentLayerIndex = -1;
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            DisposeCurrentMapAndReset();
        }

        public void Execute(GameStateChangedNotification command)
        {
            _currentGameState = command.NewState;
            _miniMapCanvasGroup.alpha = command.NewState == GameState.Gameplay && _miniMapImage.sprite != null
                ? 1f : 0f;
        }
    }
}