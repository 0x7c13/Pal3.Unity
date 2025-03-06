// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    #if PAL3
    [AvailableInConsole]
    public sealed class SceneSaveGlobalBidirectionalPushableObjectStateCommand : ICommand
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
    #endif
}