// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System;
    using System.IO;
    using Command;
    using Newtonsoft.Json;
    using Script;
    using UnityEditor;
    using UnityEngine;

    public static class UnknownSceCommandAnalyzer
    {
        public static void AnalyzeCommand(BinaryReader reader,
            int commandId,
            ushort parameterFlag,
            int codepage)
        {
            Debug.LogError($"Unknown Command Id: {commandId} ParameterFlag: {parameterFlag}");

            var initPosition = reader.BaseStream.Position;
            var length = 0;
            var maxSearchCount = 10;

            while (maxSearchCount > 0)
            {
                reader.BaseStream.Position = initPosition;
                reader.ReadBytes(length);

                var id = reader.ReadUInt16();
                var flag = reader.ReadUInt16();

                if (flag is 0b0000 or 0b0001 or 0b0011)
                {
                    var nextCmdType = SceCommandTypeResolver.GetType(id, flag);
                    if (nextCmdType != null)
                    {
                        var command = SceCommandParser.ParseSceCommand(reader, id, flag, codepage);
                        Debug.Log($"Possible length: {length}, " +
                                  $"next command: {nextCmdType.Name}-{id}-{flag}-{JsonConvert.SerializeObject(command)}");

                        maxSearchCount--;
                    }
                }

                length++;
            }

            #if UNITY_EDITOR
            EditorApplication.isPaused = true;
            #endif

            throw new Exception($"No command type found for id: {commandId}");
        }
    }
}