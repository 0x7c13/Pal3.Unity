// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    public sealed class SettingChangedNotification : ICommand
    {
        public SettingChangedNotification(string settingName)
        {
            SettingName = settingName;
        }

        public string SettingName { get; }
    }
}