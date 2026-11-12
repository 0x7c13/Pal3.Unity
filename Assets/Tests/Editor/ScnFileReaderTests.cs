// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System.IO;
    using DataReader;
    using DataReader.Scn;
    using NUnit.Framework;

    public sealed class ScnFileReaderTests
    {
        [TestCase(false)] [TestCase(true)]
        public void TwoNpcAndObjectRecordsPreserveVariantFieldsAndRecordAlignment(bool unsafeReader)
        {
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write(new byte[] { 83, 67, 78, 0 }); w.Write((short)1); w.Write((short)2); w.Write(256u);
                w.Write((short)2); w.Write(1408u);
                w.FixedString("city", 32); w.FixedString("scene", 32); w.FixedString("model", 32);
                w.Write(2); w.Write(-1); w.Write(3u); w.Write(new byte[24]);
                w.At(256);
                for (byte id = 1; id <= 2; id++)
                {
                    long start = w.BaseStream.Position;
                    w.Write(id); w.Write((byte)0); w.FixedString("景天", 32); w.FixedString("texture", 34);
                    w.Write(90f); w.Write(1); w.Write(10f); w.Write(20f); w.Write(1); w.Write(2); w.Write((uint)(100 + id));
                    w.Write(30f); w.FixedString("stand", 16); w.Write(4u); w.Write(5u); w.Write(6u);
                    w.Write((byte)3); w.Write((byte)1); w.Write((ushort)0xABCD);
                    w.Write(1); for (int i = 0; i < 48; i++) w.Write((float)i);
                    w.Write(1u); w.Write(2u); w.Write(300u); w.Write(new byte[116]);
                    Assert.That(w.BaseStream.Position - start, Is.EqualTo(456));
                }
                w.At(1408);
                for (byte id = 7; id <= 8; id++) Object(w, id);
            });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            var file = new ScnFileReader().Read(r, 936);
            Assert.That(file.SceneInfo.CityName, Is.EqualTo("city")); Assert.That(file.SceneInfo.LightMap, Is.EqualTo(-1));
            Assert.That(file.NpcInfos[1].Id, Is.EqualTo(2)); Assert.That(file.NpcInfos[1].ScriptId, Is.EqualTo(102));
            Assert.That(file.NpcInfos[1].Name, Is.EqualTo("景天")); Assert.That(file.NpcInfos[1].GameBoxMoveSpeed, Is.EqualTo(300));
            foreach (var obj in file.ObjectInfos)
            {
                Assert.That(obj.Parameters, Is.EqualTo(new[] { -10, 20, 30, 40, 50, 60 }));
                Assert.That(obj.RequireItem, Is.EqualTo(0x1234)); Assert.That(obj.RequireMoney, Is.EqualTo(4321));
                Assert.That(obj.ScriptId, Is.EqualTo(1000 + obj.Id)); Assert.That(obj.LinkedObjectId, Is.EqualTo(12));
                Assert.That(obj.FailedMessage, Is.EqualTo("不能通过")); Assert.That(obj.ScriptMoved, Is.EqualTo(0x11223344));
                #if PAL3A
                Assert.That(obj.LinkedObjectGroupId, Is.EqualTo(0x3456)); Assert.That(obj.GameBoxZRotation, Is.EqualTo(45f));
                Assert.That(obj.CanOnlyBeTriggeredOnce, Is.EqualTo(1)); Assert.That(obj.Reserved.Length, Is.EqualTo(44));
                #else
                Assert.That(obj.Reserved.Length, Is.EqualTo(52));
                #endif
            }
            Assert.That(file.ObjectInfos[1].Id, Is.EqualTo(8)); Assert.That(r.Position, Is.EqualTo(data.Length));
        }

        private static void Object(BinaryWriter w, byte id)
        {
            long start = w.BaseStream.Position;
            w.Write(id); w.Write((byte)1); w.Write((byte)255); w.Write((byte)0); w.FixedString("door.cvd", 32);
            w.Write((byte)2); w.Write((byte)0); w.Write((ushort)0xABCD);
            w.Write(1f); w.Write(2f); w.Write(3f); w.Write(90f);
            for (int i = 0; i < 4; i++) w.Write(i);
            w.Write((byte)1); w.Write((byte)1); w.Write((byte)2); w.Write((byte)3);
            foreach (int parameter in new[] { -10, 20, 30, 40, 50, 60 })
            {
                #if PAL3
                w.Write(parameter);
                #else
                w.Write((float)parameter);
                #endif
            }
            #if PAL3A
            w.Write(0u);
            #endif
            w.Write((byte)255); w.Write((ushort)0x1234); w.Write((byte)0xCD);
            w.Write((ushort)4321); w.Write((ushort)40); w.Write((ushort)50); w.Write((byte)1); w.FixedString("不能通过", 16); w.Write((byte)0xCD);
            #if PAL3A
            w.Write((ushort)0x3456); w.Write((ushort)0xABCD);
            #endif
            w.Write((uint)(1000 + id)); w.Write(2); for (int i = 0; i < 48; i++) w.Write((float)i);
            w.Write((ushort)12); w.FixedString("next", 32); w.Write((byte)13); w.Write((byte)0xCD);
            for (int i = 0; i < 6; i++) w.Write((float)i);
            w.Write(30f);
            #if PAL3A
            w.Write(45f);
            #endif
            w.FixedString("snd001", 8); w.Write(3u); w.Write(4u); w.Write(0x11223344u);
            #if PAL3A
            w.Write(1u); w.Write(new byte[176]); Assert.That(w.BaseStream.Position - start, Is.EqualTo(604));
            #else
            w.Write(new byte[208]); Assert.That(w.BaseStream.Position - start, Is.EqualTo(620));
            #endif
        }
    }
}
