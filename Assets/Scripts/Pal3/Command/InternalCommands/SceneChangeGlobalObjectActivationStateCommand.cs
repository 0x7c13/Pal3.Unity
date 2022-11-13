// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    [AvailableInConsole]
    public class SceneChangeGlobalObjectActivationStateCommand : ICommand
    {
        public SceneChangeGlobalObjectActivationStateCommand(string sceneObjectHashName,
            bool isEnabled)
        {
            SceneObjectHashName = sceneObjectHashName;
            IsEnabled = isEnabled;
        }

        public string SceneObjectHashName { get; }
        public bool IsEnabled { get; }
    }
}