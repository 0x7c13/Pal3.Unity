// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dat
{
    using System.IO;

    public sealed class FlexCameraFileReader : IFileReader<FlexCameraFile>
    {
        public const int RecordBytes = 204;
        public const int PoolCount = 2000;
        public const int ListCount = 100;
        public const int ListCapacity = 50;
        public const int FileBytes = RecordBytes * PoolCount + PoolCount + ListCount * ListCapacity * sizeof(int);

        public FlexCameraFile Read(IBinaryReader reader, int codepage)
        {
            if (reader.Length - reader.Position != FileBytes)
                throw new InvalidDataException("Original flexcam.dat must contain the complete 430000-byte pool, use flags and index lists.");
            var pool = new FlexCameraTask[PoolCount];
            for (int index = 0; index < pool.Length; index++)
                pool[index] = ReadTask(reader);
            var inUse = new bool[PoolCount];
            for (int index = 0; index < inUse.Length; index++)
                inUse[index] = reader.ReadBoolean();
            var lists = new int[ListCount][];
            for (int list = 0; list < lists.Length; list++)
            {
                lists[list] = reader.ReadInt32s(ListCapacity);
                foreach (int index in lists[list])
                    if (index < -1 || index >= PoolCount)
                        throw new InvalidDataException("A flexcam list references an invalid pool entry.");
            }
            return new FlexCameraFile(pool, inUse, lists);
        }

        private static FlexCameraPose ReadPose(IBinaryReader reader) =>
            new(reader.ReadGameBoxVector3(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private static FlexCameraTask ReadTask(IBinaryReader reader)
        {
            long startOffset = reader.Position;
            // The saved MSVC member-function-pointer representation occupies
            // sixteen bytes in both published files, not a single x86 pointer.
            uint[] dispatch = reader.ReadUInt32s(4);
            uint id = reader.ReadUInt32();
            bool valid = reader.ReadBoolean(), executed = reader.ReadBoolean(), finished = reader.ReadBoolean(), splice = reader.ReadBoolean();
            float start = reader.ReadSingle(), expected = reader.ReadSingle();
            FlexCameraPose from = ReadPose(reader), to = ReadPose(reader);
            int startCamera = reader.ReadInt32(), endCamera = reader.ReadInt32();
            FlexCameraFlags flags = FlexCameraFlags.None;
            for (int bit = 0; bit < 15; bit++)
                if (reader.ReadBoolean())
                    flags |= (FlexCameraFlags)(1 << bit);
            _ = reader.ReadByte(); // tagCCT alignment before nTraceRole.
            int trace = reader.ReadInt32();
            float[] parameters = reader.ReadSingles(8);
            var center = reader.ReadGameBoxVector3();
            var circleEnd = reader.ReadGameBoxVector3();
            float[] tail = reader.ReadSingles(5);
            var traceStart = reader.ReadGameBoxVector3();
            uint[] extension = reader.ReadUInt32s(2);
            if (reader.Position - startOffset != RecordBytes)
                throw new InvalidDataException("The flexcam record layout does not match tagCCT.");
            return new FlexCameraTask(dispatch, id, valid, executed, finished, splice, start, expected, from, to,
                startCamera, endCamera, flags, trace, parameters, center, circleEnd, tail, traceStart, extension);
        }
    }
}
