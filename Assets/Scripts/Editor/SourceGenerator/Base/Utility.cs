// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor.SourceGenerator.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public static class Utility
    {
        public static string GetMethodArgumentDefinitionListAsString(PropertyInfo[] properties)
        {
            StringBuilder argListStrBuilder = new ();
            for (var i = 0; i < properties.Length; i++)
            {
                argListStrBuilder.Append(properties[i].PropertyType);
                argListStrBuilder.Append(" ");
                argListStrBuilder.Append(ToLowerFirstChar(properties[i].Name));
                if (i < properties.Length - 1) argListStrBuilder.Append(", ");
            }
            return argListStrBuilder.ToString();
        }

        public static string GetMethodArgumentListAsString(PropertyInfo[] properties)
        {
            StringBuilder argListStrBuilder = new ();
            for (var i = 0; i < properties.Length; i++)
            {
                argListStrBuilder.Append(ToLowerFirstChar(properties[i].Name));
                if (i < properties.Length - 1) argListStrBuilder.Append(", ");
            }
            return argListStrBuilder.ToString();
        }

        private static string ToLowerFirstChar(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLower(input[0]) + input[1..];
        }

        public static IEnumerable<Type> GetTypesOfInterface(Type interfaceType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && t.GetInterfaces().Contains(interfaceType));
        }
    }
}