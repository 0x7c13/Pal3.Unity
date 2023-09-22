// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(126, "脚本检查迷宫开关状态并赋值给变量，" +
                     "参数：场景名，场景开关ID，变量名")]
    public class ScriptGetMazeSwitchStatusCommand : ICommand
    {
        public ScriptGetMazeSwitchStatusCommand(string sceneName, int objectId, ushort variable)
        {
            SceneName = sceneName;
            ObjectId = objectId;
            Variable = variable;
        }

        public string SceneName { get; }
        public int ObjectId { get; }
        public ushort Variable { get; }
    }
}