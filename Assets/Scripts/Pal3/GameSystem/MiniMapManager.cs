// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystem
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.Utils;
    using Scene;
    using State;
    using UnityEngine;
    using UnityEngine.UI;

    public sealed class MiniMapManager : IDisposable,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float MINIMAP_SCALE = 2.5f;

        private readonly Camera _mainCamera;
        private readonly SceneManager _sceneManager;

        private readonly CanvasGroup _miniMapCanvasGroup;
        private readonly RectTransform _miniMapRectTransform;
        private readonly float _miniMapWidth;
        private readonly Image _miniMapImage;

        private Texture2D[] _miniMapTextures;
        private Sprite[] _miniMapSprites;

        private Tilemap _currentTilemap;
        private int _currentLayerIndex = -1;
        private GameState _currentGameState = GameState.UI;

        public MiniMapManager(Camera mainCamera,
            SceneManager sceneManager,
            CanvasGroup miniMapCanvasGroup,
            Image miniMapImage)
        {
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _miniMapCanvasGroup = Requires.IsNotNull(miniMapCanvasGroup, nameof(miniMapCanvasGroup));
            _miniMapImage = Requires.IsNotNull(miniMapImage, nameof(miniMapImage));
            _miniMapImage.preserveAspect = true;

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
            _miniMapRectTransform.localRotation = Quaternion.Euler(0, 0, _mainCamera.transform.eulerAngles.y + 180f);
        }

        private Texture2D CreateMiniMapTexture(NavTileLayer layer)
        {
            Color[] colors = new Color[layer.Width * layer.Height];

            for (var i = 0; i < layer.Width; i++)
            {
                for (int j = 0; j < layer.Height; j++)
                {
                    var index = i + j * layer.Width;
                    NavTile tile = layer.Tiles[index];

                    // NOTE: the texture is flipped vertically compared to the tilemap space
                    var colorIndex = i + (layer.Height - j - 1) * layer.Width;
                    colors[colorIndex] = tile.DistanceToNearestObstacle switch
                    {
                        0 => new Color(0f, 0f, 0f, 0f),
                        1 => new Color(1f, 0.9f, 0.1f, 0.7f),
                        #if PAL3
                        _ => new Color(30f / 255f, 75f / 255f, 140f / 255f, 100f / 255f),
                        #elif PAL3A
                        _ => new Color(160f / 255f, 40f / 255f, 110f / 255f, 100f / 255f),
                        #endif
                    };
                }
            }

            Texture2D texture = new Texture2D(layer.Width, layer.Height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels(colors);
            texture.Apply(updateMipmaps: false);
            return texture;
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

                float scale = Mathf.Max(layer.Width, layer.Height) / _miniMapWidth * MINIMAP_SCALE;
                _miniMapImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            // NOTE: the texture is flipped vertically compared to the tilemap space
            Vector2 playerPixelPosition = new Vector2(command.Position.x, layer.Height - command.Position.y - 1);
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
            if (command.NewSceneInfo.SceneType == ScnSceneType.InDoor) return;

            _currentTilemap = _sceneManager.GetCurrentScene().GetTilemap();
            _miniMapTextures = new Texture2D[_currentTilemap.GetLayerCount()];
            _miniMapSprites = new Sprite[_currentTilemap.GetLayerCount()];

            for (var i = 0; i < _currentTilemap.GetLayerCount(); i++)
            {
                NavTileLayer tileLayer = _currentTilemap.GetLayer(i);
                _miniMapTextures[i] = CreateMiniMapTexture(tileLayer);

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
                    UnityEngine.Object.Destroy(sprite);
                }

                _miniMapSprites = null;
            }

            if (_miniMapTextures != null)
            {
                foreach (Texture2D texture in _miniMapTextures)
                {
                    UnityEngine.Object.Destroy(texture);
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