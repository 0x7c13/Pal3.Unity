// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(63, "切换场景，" +
                    "参数：场景城市/关卡（文件）名称，场景名称")]
    public class SceneLoadCommand : ICommand
    {
        public SceneLoadCommand(string sceneCityName, string sceneName)
        {
            SceneCityName = sceneCityName;
            SceneName = sceneName;
        }

        public string SceneCityName { get; }
        public string SceneName { get; }
    }
}