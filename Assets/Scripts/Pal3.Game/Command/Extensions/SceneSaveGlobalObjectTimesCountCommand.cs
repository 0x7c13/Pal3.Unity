﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    [AvailableInConsole]
    public sealed class SceneSaveGlobalObjectTimesCountCommand : ICommand
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