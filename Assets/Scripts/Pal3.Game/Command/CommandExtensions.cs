// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command
{
    using System;
    using System.Reflection;
    using System.Text;
    using Core.Command;

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

            return builder.ToString().TrimEnd();
        }
    }
}