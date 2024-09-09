// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Txt
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public sealed class TaskDefinitionFileReader : IFileReader<TaskDefinitionFile>
    {
        public TaskDefinitionFile Read(IBinaryReader reader, int codepage)
        {
            throw new NotImplementedException();
        }

        public TaskDefinitionFile Read(byte[] data, int codepage)
        {
            string content = Encoding.GetEncoding(codepage).GetString(data, 0, data.Length);
            List<Task> tasks = new();
            string[] taskLines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Task currentTask = new();

            foreach (string taskLine in taskLines)
            {
                if (taskLine.TrimStart().StartsWith(";")) continue;

                string[] splitLine = taskLine.Split('$');
                string tag = splitLine[0].Trim();
                string value = splitLine.Length > 1 ? splitLine[1].Trim().Trim('$', '&') : string.Empty;

                switch (tag)
                {
                    case "tname":
                        currentTask.Id = value;
                        break;
                    case "title":
                        currentTask.Title = value;
                        break;
                    case "info":
                        currentTask.Description = value;
                        break;
                    case "type":
                        currentTask.Type = value;
                        break;
                    case "last":
                        currentTask.IsLastOne = value;
                        break;
                    case "#":
                        tasks.Add(currentTask);
                        currentTask = new Task(); // reset task for next one
                        break;
                }
            }

            return new TaskDefinitionFile(tasks.ToArray());
        }
    }
}