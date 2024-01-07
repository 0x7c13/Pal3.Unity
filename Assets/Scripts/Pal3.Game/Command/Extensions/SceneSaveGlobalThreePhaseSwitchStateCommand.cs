// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    #if PAL3A
    using Core.Command;

    [AvailableInConsole]
    public sealed class SceneSaveGlobalThreePhaseSwitchStateCommand : ICommand
    {
        public SceneSaveGlobalThreePhaseSwitchStateCommand(string cityName,
            string sceneName,
            int objectId,
            int previousState,
            int currentState)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public int PreviousState { get; }
        public int CurrentState { get; }
    }
    #endif
}