// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Command;
    using Core.Extensions;

    /// <summary>
    /// Parser logic for parsing SceCommand in .sce script data block.
    /// </summary>
    public static class SceCommandParser
    {
        public static ICommand ParseSceCommand(BinaryReader reader, int commandId, ushort parameterFlag)
        {
            var commandType = SceCommandTypeResolver.GetType(commandId, parameterFlag);

            if (commandType == null) return null;

            var properties = commandType.GetProperties();
            var args = new object[properties.Length];
            for (var i = properties.Length - 1; i >= 0; i--)
            {
                var property = properties[i];
                args[i] = ReadPropertyValue(reader, property.PropertyType, i, parameterFlag);
            }

            return Activator.CreateInstance(commandType, args) as ICommand;
        }

        // Read property value by type, property index and parameter flag using reflection
        private static object ReadPropertyValue(BinaryReader reader, Type propertyType, int index, ushort parameterFlag)
        {
            // This is for reading user var (2 bytes Int16)
            if ((parameterFlag & (ushort) (0x0001 << index)) != 0)
            {
                return Convert.ChangeType(reader.ReadInt16(), propertyType);
            }

            // Special handling for String since we need to read the length first
            if (propertyType == typeof(string))
            {
                var length = reader.ReadUInt16();
                return reader.ReadGbkString(length);
            }

            // Special handling for List type
            if (propertyType == typeof(List<object>))
            {
                var length = reader.ReadUInt16();
                var list = new List<object>();
                for (var i = 0; i < length; i++)
                {
                    var varType = GetVariableType(reader.ReadByte());
                    list.Insert(0, ReadPropertyValue(reader, varType, index, parameterFlag));
                }
                return list;
            }

            // Let's use the power of reflection to read and parser primitives
            return BinaryReaderMethodResolver.GetMethodInfoForReadPropertyType(propertyType)
                .Invoke(reader, new object[]{});
        }

        private static Type GetVariableType(byte type)
        {
            return type switch
            {
                0 => typeof(int),
                1 => typeof(float),
                2 => typeof(int),
                3 => typeof(string),
                4 => typeof(ushort),
                _ => null
            };
        }
    }
}