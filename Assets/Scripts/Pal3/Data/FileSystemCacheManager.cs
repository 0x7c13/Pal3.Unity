// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Core.DataReader.Cpk;
    using Core.FileSystem;

    /// <summary>
    /// Cache/dispose Cpk archive into memory based on scenarios.
    /// </summary>
    public sealed class FileSystemCacheManager : IDisposable,
        ICommandExecutor<ScenePreLoadingNotification>
    {
        private readonly ICpkFileSystem _fileSystem;
        private string _currentCityName;
        private string _currentSceneName;

        public FileSystemCacheManager(ICpkFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _fileSystem.DisposeAllInMemoryArchives();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(ScenePreLoadingNotification notification)
        {
            var newCityName = notification.NewSceneInfo.CityName.ToLower();
            var newSceneName = notification.NewSceneInfo.SceneName.ToLower();

            if (!newCityName.Equals(_currentCityName, StringComparison.OrdinalIgnoreCase))
            {
                // Dispose current scene cpk
                if (!string.IsNullOrEmpty(_currentCityName))
                {
                    var currentSceneFolderPath = _currentCityName + CpkConstants.FileExtension + CpkConstants.DirectorySeparator
                                                 + _currentSceneName;

                    if (_fileSystem.FileExistsInSegmentedArchive(currentSceneFolderPath, out string segmentedArchiveName))
                    {
                        _fileSystem.DisposeInMemoryArchive(segmentedArchiveName);
                    }
                    else
                    {
                        _fileSystem.DisposeInMemoryArchive(_currentCityName + CpkConstants.FileExtension);
                    }
                }

                // Load new scene cpk into memory
                {
                    var newSceneFolderPath = newCityName + CpkConstants.FileExtension + CpkConstants.DirectorySeparator
                                             + newSceneName;

                    if (_fileSystem.FileExistsInSegmentedArchive(newSceneFolderPath, out string segmentedArchiveName))
                    {
                        _fileSystem.LoadArchiveIntoMemory(segmentedArchiveName);
                    }
                    else
                    {
                        _fileSystem.LoadArchiveIntoMemory(newCityName + CpkConstants.FileExtension);
                    }
                }

                _currentCityName = newCityName;
                _currentSceneName = newSceneName;
            }
        }
    }
}