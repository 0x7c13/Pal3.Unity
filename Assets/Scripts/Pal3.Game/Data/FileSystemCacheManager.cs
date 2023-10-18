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
            string newCityName = notification.NewSceneInfo.CityName.ToLower();
            string newSceneName = notification.NewSceneInfo.SceneName.ToLower();

            // No need to do anything if the scene city is the same
            if (newCityName.Equals(_currentCityName, StringComparison.OrdinalIgnoreCase)) return;

            // Dispose current scene cpk from memory
            if (!string.IsNullOrEmpty(_currentCityName))
            {
                string currentSceneFolderPath = $"{_currentCityName}{CpkConstants.FileExtension}" +
                                                $"{CpkConstants.DirectorySeparatorChar}{_currentSceneName}";

                if (_fileSystem.FileExists(currentSceneFolderPath, out string archiveName))
                {
                    _fileSystem.DisposeInMemoryArchive(archiveName);
                }

                EngineLogger.Log($"Disposed in-memory archive: <{archiveName}>");
            }

            // Load new scene cpk into memory
            {
                string newSceneFolderPath = $"{newCityName}{CpkConstants.FileExtension}" +
                                            $"{CpkConstants.DirectorySeparatorChar}{newSceneName}";

                Stopwatch timer = Stopwatch.StartNew();

                if (_fileSystem.FileExists(newSceneFolderPath, out string archiveName))
                {
                    _fileSystem.LoadArchiveIntoMemory(archiveName);
                }

                EngineLogger.Log($"Loaded archive <{archiveName}> into memory in {timer.ElapsedMilliseconds} ms");
            }

            _currentCityName = newCityName;
            _currentSceneName = newSceneName;
        }
    }
}