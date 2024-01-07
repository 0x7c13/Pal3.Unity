// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    [AvailableInConsole]
    public sealed class SceneSaveGlobalObjectPositionCommand : ICommand
    {
        public SceneSaveGlobalObjectPositionCommand(string cityName,
            string sceneName,
            int objectId,
            float gameBoxXPosition,
            float gameBoxYPosition,
            float gameBoxZPosition)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
    }
}