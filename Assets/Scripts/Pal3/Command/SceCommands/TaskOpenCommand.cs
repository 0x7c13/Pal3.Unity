﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(169, "开启主线或支线任务，" +
                     "参数：任务ID")]
    public class TaskOpenCommand : ICommand
    {
        public TaskOpenCommand(string taskId)
        {
            TaskId = taskId;
        }

        public string TaskId { get; }
    }
    #endif
}