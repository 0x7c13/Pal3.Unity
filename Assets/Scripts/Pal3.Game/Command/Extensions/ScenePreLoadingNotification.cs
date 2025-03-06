// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using Core.DataReader.Scn;

    public sealed class ScenePreLoadingNotification : ICommand
    {
        public ScenePreLoadingNotification(ScnSceneInfo sceneInfo)
        {
            NewSceneInfo = sceneInfo;
        }

        public ScnSceneInfo NewSceneInfo { get; }
    }
}