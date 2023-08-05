// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Txt
{
    using System;

    public struct Task
    {
        public string TaskId;
        public string TaskTitle;
        public string TaskInfo;
        public string TaskType;
        public string IsLastOne;

        public bool IsMainTask => string.Equals(TaskType, "0", StringComparison.Ordinal);
    }

    public sealed class TaskDefinitionFile
    {
        public Task[] Tasks { get; }

        public TaskDefinitionFile(Task[] tasks)
        {
            Tasks = tasks;
        }
    }
}