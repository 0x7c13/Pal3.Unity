// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneChangeGlobalObjectSwitchStateCommand : ICommand
    {
        public SceneChangeGlobalObjectSwitchStateCommand(string cityName,
            string sceneName,
            int objectId,
            int switchState)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            SwitchState = switchState;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public int SwitchState { get; }
    }
}