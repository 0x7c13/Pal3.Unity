// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Txt
{
    using System;

    public struct Task
    {
        public string Id;
        public string Title;
        public string Description;
        public string Type;
        public string IsLastOne;

        public bool IsMainTask => string.Equals(Type, "0", StringComparison.Ordinal);
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