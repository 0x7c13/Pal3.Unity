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
            int isActive)
        {
            SceneObjectHashName = sceneObjectHashName;
            IsActive = isActive;
        }

        public string SceneObjectHashName { get; }
        public int IsActive { get; }
    }
}