// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneSaveGlobalObjectActivationStateCommand : ICommand
    {
        public SceneSaveGlobalObjectActivationStateCommand(string cityName,
            string sceneName,
            int objectId,
            bool isActivated)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            IsActivated = isActivated;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public bool IsActivated { get; }
    }
}