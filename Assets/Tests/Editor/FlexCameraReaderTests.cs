// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System.IO;
    using Core.DataReader;
    using Core.DataReader.Dat;
    using NUnit.Framework;

    public sealed class FlexCameraReaderTests
    {
        [Test]
        public void BothGamesUseTheSixteenByteDispatchAreaAnd204ByteRecordStride()
        {
            byte[] bytes = Empty();
            using (var writer = new BinaryWriter(new MemoryStream(bytes)))
            {
                for (int record = 0; record < 2; record++)
                {
                    writer.BaseStream.Position = record * 204;
                    for (uint word = 0; word < 4; word++)
                        writer.Write(0x10000000u + word);
                    writer.Write((uint)(700 + record));
                    writer.Write(true);
                    writer.Write(false);
                    writer.Write(true);
                    writer.Write(true);
                    for (int value = 0; value < 14; value++)
                        writer.Write(1.25f + value);
                    writer.Write(21);
                    writer.Write(22);
                    for (int bit = 0; bit < 15; bit++)
                        writer.Write(bit % 2 == 0);
                    writer.Write((byte)0xfe); // Alignment must never become part of nTraceRole.
                    writer.Write(-1);
                    for (int value = 0; value < 22; value++)
                        writer.Write(30.5f + value);
                    writer.Write(0x11223344u);
                    writer.Write(0x55667788u);
                }
                writer.BaseStream.Position = 408000;
                writer.Write(true);
                writer.BaseStream.Position = 410000;
                writer.Write(1);
            }
            FlexCameraFile file = ((IFileReader<FlexCameraFile>)new FlexCameraFileReader()).Read(bytes, 936);
            Assert.That(file.Pool.Count, Is.EqualTo(2000));
            Assert.That(file.Lists.Count, Is.EqualTo(100));
            Assert.That(file.Lists[0][0], Is.EqualTo(1));
            Assert.That(file.InUse[0], Is.True);
            Assert.That(file.InUse[1], Is.False);
            for (int record = 0; record < 2; record++)
            {
                FlexCameraTask task = file.Pool[record];
                Assert.That(task.SerializedDispatchWords, Is.EqualTo(new uint[] { 0x10000000, 0x10000001, 0x10000002, 0x10000003 }));
                Assert.That(task.Id, Is.EqualTo(700 + record));
                Assert.That(task.SavedStartTime, Is.EqualTo(1.25f));
                Assert.That(task.ExpectedSeconds, Is.EqualTo(2.25f));
                Assert.That(task.Start.Position.X, Is.EqualTo(3.25f));
                Assert.That(task.Start.Yaw, Is.EqualTo(6.25f));
                Assert.That(task.End.Roll, Is.EqualTo(14.25f));
                Assert.That(task.StartCameraIndex, Is.EqualTo(21));
                Assert.That(task.EndCameraIndex, Is.EqualTo(22));
                Assert.That((int)task.Flags, Is.EqualTo(0x5555));
                Assert.That(task.TraceActor, Is.EqualTo(-1));
                Assert.That(task.StartSpeed, Is.EqualTo(30.5f));
                Assert.That(task.CircleAngle, Is.EqualTo(36.5f));
                Assert.That(task.CircleCenter.X, Is.EqualTo(38.5f));
                Assert.That(task.TimeCompensation, Is.EqualTo(46.5f));
                Assert.That(task.StartFieldOfView, Is.EqualTo(47.5f));
                Assert.That(task.SavedTraceStart.Z, Is.EqualTo(51.5f));
                Assert.That(task.ExtensionWords, Is.EqualTo(new uint[] { 0x11223344, 0x55667788 }));
            }
        }

        [Test]
        public void TruncatedExtendedAndOutOfPoolFilesAreRejected()
        {
            var reader = (IFileReader<FlexCameraFile>)new FlexCameraFileReader();
            Assert.Throws<InvalidDataException>(() => reader.Read(new byte[FlexCameraFileReader.FileBytes - 1], 936));
            Assert.Throws<InvalidDataException>(() => reader.Read(new byte[FlexCameraFileReader.FileBytes + 1], 936));
            byte[] bytes = Empty();
            using (var writer = new BinaryWriter(new MemoryStream(bytes)))
            {
                writer.BaseStream.Position = 410000;
                writer.Write(2000);
            }
            Assert.Throws<InvalidDataException>(() => reader.Read(bytes, 936));
        }

        private static byte[] Empty()
        {
            var bytes = new byte[FlexCameraFileReader.FileBytes];
            for (int i = 410000; i < bytes.Length; i++)
                bytes[i] = 0xff;
            return bytes;
        }
    }
}
