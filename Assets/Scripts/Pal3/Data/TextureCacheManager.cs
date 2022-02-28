// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using Command;
    using Command.InternalCommands;
    using UnityEngine;

    public class TextureCacheManager :
        ICommandExecutor<ScenePreLoadingNotification>
    {
        private readonly TextureCache _textureCache;
        private string _currentSceneCityName;

        public TextureCacheManager(TextureCache textureCache)
        {
            _textureCache = textureCache;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _textureCache.DisposeAllCachedTextures();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(ScenePreLoadingNotification notification)
        {
            var newSceneCityName = notification.SceneInfo.CityName;
            if (!newSceneCityName.Equals(_currentSceneCityName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(_currentSceneCityName))
                {
                    _textureCache.DisposeAllCachedTextures();

                    // Unloads assets that are not used (textures etc.)
                    Resources.UnloadUnusedAssets();
                }
                _currentSceneCityName = newSceneCityName;
            }
        }
    }
}