// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor.SourceGenerator.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class Utility
    {
        public static string GetMethodArgumentDefinitionListAsString(PropertyInfo[] properties)
        {
            var argListStr = String.Empty;
            for (var i = 0; i < properties.Length; i++)
            {
                argListStr += properties[i].PropertyType.ToString();
                argListStr += " ";
                argListStr += ToLowerFirstChar(properties[i].Name);
                if (i < properties.Length - 1) argListStr += ", ";
            }
            return argListStr;
        }

        public static string GetMethodArgumentListAsString(PropertyInfo[] properties)
        {
            var argListStr = String.Empty;
            for (var i = 0; i < properties.Length; i++)
            {
                argListStr += ToLowerFirstChar(properties[i].Name);
                if (i < properties.Length - 1) argListStr += ", ";
            }
            return argListStr;
        }

        public static string ToLowerFirstChar(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLower(input[0]) + input[1..];
        }

        public static Type[] GetTypesOfInterface(Type interfaceType)
        {
            var result = new List<Type>();
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                result.AddRange(asm.GetTypes()
                    .Where(t => t.IsClass && t.GetInterfaces().Contains(interfaceType)));
            }
            return result.ToArray();
        }
    }
}