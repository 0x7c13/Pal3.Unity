// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;
    using UnityEngine;

    [AvailableInConsole]
    public class SceneSaveGlobalObjectPositionCommand : ICommand
    {
        public SceneSaveGlobalObjectPositionCommand(string cityName,
            string sceneName,
            int objectId,
            Vector3 gameBoxPosition)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            GameBoxPosition = gameBoxPosition;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public Vector3 GameBoxPosition { get; }
    }
}