// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneSaveGlobalObjectYRotationCommand : ICommand
    {
        public SceneSaveGlobalObjectYRotationCommand(string cityName,
            string sceneName,
            int objectId,
            float gameBoxYRotation)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            GameBoxYRotation = gameBoxYRotation;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public float GameBoxYRotation { get; }
    }
}