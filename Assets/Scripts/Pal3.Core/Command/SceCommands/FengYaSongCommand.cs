// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(164, "控制雷元戈头上鸟（风雅颂）的显示与隐藏" +
                     "参数：模型类型（0=风，1=雅，2=颂，3=关闭），动作类型（0=静止，1=飞行）")]
    public class FengYaSongCommand : ICommand
    {
        public FengYaSongCommand(
            int modelType,
            int actionType)
        {
            ModelType = modelType;
            ActionType = actionType;
        }

        public int ModelType { get; }
        public int ActionType { get; }
    }
    #endif
}