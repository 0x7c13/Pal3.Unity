// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneSaveGlobalObjectTimesCountCommand : ICommand
    {
        public SceneSaveGlobalObjectTimesCountCommand(string cityName,
            string sceneName,
            int objectId,
            byte timesCount)
        {
            CityName = cityName;
            SceneName = sceneName;
            ObjectId = objectId;
            TimesCount = timesCount;
        }

        public string CityName { get; }
        public string SceneName { get; }
        public int ObjectId { get; }
        public byte TimesCount { get; }
    }
}