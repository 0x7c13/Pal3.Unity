// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Reflection;
    using System.Text;

    public static class CommandExtensions
    {
        public static string ToString(ICommand command)
        {
            var builder = new StringBuilder();
            Type type = command.GetType();
            
            builder.Append(type.Name[..^"Command".Length]);
            builder.Append(' ');
            
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                builder.Append(propertyInfo.GetValue(command));
                builder.Append(' ');
            }

            var commandStr = builder.ToString();
            if (commandStr.EndsWith(' ')) commandStr = commandStr[..^1];
            return commandStr;
        }
    }
}