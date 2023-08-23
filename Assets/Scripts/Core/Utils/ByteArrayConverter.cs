// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public sealed class ByteArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bytes = (byte[])value;

            writer.WriteStartArray();

            if (bytes != null)
            {
                foreach (var bt in bytes)
                {
                    writer.WriteValue(bt);
                }
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<byte> bytes = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    bytes.Add(Convert.ToByte(reader.Value));
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }
            }

            return bytes.ToArray();
        }
    }
}