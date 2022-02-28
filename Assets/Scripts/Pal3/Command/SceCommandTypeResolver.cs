// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using SceCommands;
    using UnityEditor;

    public static class SceCommandTypeResolver
    {
        private static readonly Dictionary<string, Type> SceCommandTypeCache = new ();

        private static readonly IEnumerable<Type> CommandTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(ICommand)));

        /// <summary>
        /// Get SceCommand Type for the given command id
        /// </summary>
        /// <param name="commandId">SceCommand Id</param>
        /// <param name="parameterFlag">Parameter flag</param>
        /// <returns>Type of the command</returns>
        /// <exception cref="ArgumentException">Throw if no type found for the given id</exception>
        public static Type GetType(int commandId, ushort parameterFlag)
        {
            var hashKey = $"{commandId}_{parameterFlag}";

            if (SceCommandTypeCache.ContainsKey(hashKey))
            {
                return SceCommandTypeCache[hashKey];
            }

            foreach (var commandType in CommandTypes)
            {
                if (commandType.GetCustomAttribute(typeof(SceCommandAttribute))
                        is SceCommandAttribute attribute && attribute.Id == commandId)
                {
                    if (attribute.ParameterFlag == 0 ||
                        attribute.ParameterFlag == parameterFlag)
                    {
                        SceCommandTypeCache[hashKey] = commandType;
                        return commandType;
                    }
                }
            }

            #if UNITY_EDITOR
            EditorApplication.isPaused = true;
            #endif
            throw new Exception($"No command type found for id: {commandId}");
        }
    }
}
