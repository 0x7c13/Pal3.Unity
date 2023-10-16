// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using System;
    using System.Diagnostics;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Core.Utilities;
    using Engine.Logging;

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
            _fileSystem = Requires.IsNotNull(fileSystem, nameof(fileSystem));
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
                    var currentSceneFolderPath = $"{_currentCityName}{CpkConstants.FileExtension}" +
                                                 $"{CpkConstants.DirectorySeparatorChar}{_currentSceneName}";

                    if (_fileSystem.FileExists(currentSceneFolderPath,
                            out bool isInSegmentedArchive,
                            out string segmentedArchiveName))
                    {
                        if (isInSegmentedArchive)
                        {
                            _fileSystem.DisposeInMemoryArchive(segmentedArchiveName);
                        }
                        else
                        {
                            _fileSystem.DisposeInMemoryArchive(_currentCityName + CpkConstants.FileExtension);
                        }
                    }

                    EngineLogger.Log($"Disposed {_currentCityName} cpk in-memory archive");
                }

                // Load new scene cpk into memory
                {
                    var newSceneFolderPath = $"{newCityName}{CpkConstants.FileExtension}" +
                                             $"{CpkConstants.DirectorySeparatorChar}{newSceneName}";

                    Stopwatch timer = Stopwatch.StartNew();

                    if (_fileSystem.FileExists(newSceneFolderPath,
                            out bool isInSegmentedArchive,
                            out string segmentedArchiveName))
                    {
                        if (isInSegmentedArchive)
                        {
                            _fileSystem.LoadArchiveIntoMemory(segmentedArchiveName);
                        }
                        else
                        {
                            _fileSystem.LoadArchiveIntoMemory(newCityName + CpkConstants.FileExtension);
                        }
                    }

                    EngineLogger.Log($"Loaded {newCityName} cpk archive into memory in {timer.ElapsedMilliseconds} ms");
                }

                _currentCityName = newCityName;
                _currentSceneName = newSceneName;
            }
        }
    }
}