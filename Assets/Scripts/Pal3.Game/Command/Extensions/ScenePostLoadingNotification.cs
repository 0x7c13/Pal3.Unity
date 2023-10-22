// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using Core.DataReader.Scn;

    public sealed class ScenePostLoadingNotification : ICommand
    {
        public ScenePostLoadingNotification(ScnSceneInfo sceneInfo, uint sceneScriptId)
        {
            NewSceneInfo = sceneInfo;
            SceneScriptId = sceneScriptId;
        }

        public ScnSceneInfo NewSceneInfo { get; }
        public uint SceneScriptId { get; }
    }
}