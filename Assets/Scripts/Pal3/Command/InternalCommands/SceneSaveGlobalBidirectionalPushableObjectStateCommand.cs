// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneSaveGlobalBidirectionalPushableObjectStateCommand : ICommand
    {
        public SceneSaveGlobalBidirectionalPushableObjectStateCommand(string cityName,
            string sceneName,
            int objectId,
            int state)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            State = state;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public int State { get; }
    }
}