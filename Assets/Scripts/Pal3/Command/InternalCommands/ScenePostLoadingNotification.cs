// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Core.DataReader.Scn;

    public class ScenePostLoadingNotification : ICommand
    {
        public ScenePostLoadingNotification(ScnSceneInfo sceneInfo)
        {
            SceneInfo = sceneInfo;
        }

        public ScnSceneInfo SceneInfo { get; }
    }
}