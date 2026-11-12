// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    // Constructor parameters define protocol order. Reflection property enumeration does not.
    internal static class SceCommandSchema
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

        internal static PropertyInfo[] GetProperties(Type type) => Cache.GetOrAdd(type, Build);

        private static PropertyInfo[] Build(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length != 1 || properties.Length > 16)
                throw new InvalidDataContractException($"Invalid SCE command constructor or parameter count: {type.Name}");

            ParameterInfo[] parameters = constructors[0].GetParameters();
            if (parameters.Length != properties.Length)
                throw new InvalidDataContractException($"SCE command properties must match constructor parameters: {type.Name}");

            return parameters.Select(parameter =>
            {
                PropertyInfo property = properties.SingleOrDefault(p =>
                    string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (property == null || property.PropertyType != parameter.ParameterType ||
                    property.GetIndexParameters().Length != 0 || property.GetMethod == null)
                    throw new InvalidDataContractException($"Invalid SCE parameter {type.Name}.{parameter.Name}");
                if (property.IsDefined(typeof(SceUserVariableAttribute)) && property.PropertyType != typeof(ushort))
                    throw new InvalidDataContractException($"SCE user variable must be UInt16: {type.Name}.{property.Name}");
                return property;
            }).ToArray();
        }
    }
}
