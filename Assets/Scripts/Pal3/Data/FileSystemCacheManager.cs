// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Core.FileSystem;

    /// <summary>
    /// Cache/dispose Cpk archive into memory based on scenarios.
    /// </summary>
    public class FileSystemCacheManager :
        ICommandExecutor<ScenePreLoadingNotification>
    {
        private readonly ICpkFileSystem _fileSystem;
        private string _currentSceneCityName;

        public FileSystemCacheManager(ICpkFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _fileSystem.DisposeAllInMemoryArchives();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(ScenePreLoadingNotification notification)
        {
            var newSceneCityName = notification.SceneInfo.CityName.ToLower();
            if (!newSceneCityName.Equals(_currentSceneCityName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(_currentSceneCityName))
                {
                    _fileSystem.DisposeInMemoryArchive(_currentSceneCityName + ".cpk");
                }

                _fileSystem.LoadArchiveIntoMemory(newSceneCityName + ".cpk");
                _currentSceneCityName = newSceneCityName;
            }
        }
    }
}