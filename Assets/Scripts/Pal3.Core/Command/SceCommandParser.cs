// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;
    using System.IO;
    using System.Reflection;
    using Contract.Constants;
    using DataReader;
    using Utilities;

    /// <summary>
    /// Parser logic for parsing SceCommand in .sce script data block.
    /// </summary>
    public sealed class SceCommandParser : ISceCommandParser
    {
        private readonly ISceCommandTypeResolver _sceCommandTypeResolver;

        public SceCommandParser(ISceCommandTypeResolver sceCommandTypeResolver)
        {
            _sceCommandTypeResolver = Requires.IsNotNull(sceCommandTypeResolver, nameof(sceCommandTypeResolver));
        }

        public ICommand ParseNextCommand(IBinaryReader reader,
            int codepage,
            out ushort commandId)
        {
            commandId = reader.ReadUInt16();
            ushort userVariableMask = reader.ReadUInt16();

            if (commandId > ScriptConstants.CommandIdMax)
            {
                throw new InvalidDataException($"Command Id is invalid: {commandId}");
            }

            Type commandType = _sceCommandTypeResolver.GetType(commandId, userVariableMask);

            if (commandType == null)
            {
                throw new InvalidDataException(
                    $"Command Type not found for command id: {commandId} " +
                    $"user variable mask: {Convert.ToString(userVariableMask, 2)})");
            }

            PropertyInfo[] properties = commandType.GetProperties();
            object[] args = new object[properties.Length];

            for (int i = properties.Length - 1; i >= 0; i--)
            {
                PropertyInfo property = properties[i];

                // This is for reading user variable (Always 2 bytes UInt16)
                // variableMask is used to determine if the property is 2-byte UInt16 user variable type
                if ((userVariableMask & (ushort) (1 << i)) != 0)
                {
                    // Make sure the property type is also UInt16
                    if (property.PropertyType != typeof(ushort))
                    {
                        throw new InvalidDataException(
                            $"Property type mismatch for user variable: {property.Name} " +
                            $"in command: {commandType.Name}, it should be ushort(UInt16)");
                    }

                    args[i] = reader.ReadUInt16();
                }
                else // Otherwise, read property value by type
                {
                    args[i] = ReadPropertyValue(reader, property.PropertyType, codepage);
                }
            }

            return Activator.CreateInstance(commandType, args) as ICommand;
        }

        // Read property value by type, property index and parameter flag using reflection
        private static object ReadPropertyValue(IBinaryReader reader,
            Type propertyType,
            int codepage)
        {
            // Special handling for Array type
            if (propertyType.IsArray)
            {
                ushort length = reader.ReadUInt16(); // First 2 bytes is array length
                object[] propertyArray = new object[length];
                for (var i = 0; i < length; i++)
                {
                    Type varType = GetVariableType(reader.ReadByte());
                    // Read from the end of the array to the beginning by design
                    // This is because the script data is stored in reverse order (stack manner)
                    propertyArray[^(i + 1)] = ReadPropertyValue(reader, varType, codepage);
                }
                return propertyArray;
            }

            // Special handling for String type
            if (propertyType == typeof(string))
            {
                ushort length = reader.ReadUInt16(); // First 2 bytes is string length
                return reader.ReadString(length, codepage);
            }

            // Read and parse primitives
            return reader.Read(propertyType);
        }

        private static Type GetVariableType(byte type)
        {
            return type switch
            {
                0 => typeof(int),    // Int
                1 => typeof(float),  // Float
                2 => typeof(int),    // Jump flag
                3 => typeof(string), // String
                4 => typeof(ushort), // User variable
                _ => throw new NotSupportedException($"Variable type not supported: {type}")
            };
        }
    }
}