// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(63, "切换场景，" +
                    "参数：场景关（文件）名称，场景区块名称")]
    public class SceneLoadCommand : ICommand
    {
        public SceneLoadCommand(string sceneFileName, string sceneName)
        {
            SceneFileName = sceneFileName;
            SceneName = sceneName;
        }

        public string SceneFileName { get; }
        public string SceneName { get; }
    }
}