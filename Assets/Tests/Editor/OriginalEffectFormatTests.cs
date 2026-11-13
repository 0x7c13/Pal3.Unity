// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using DataReader;
    using DataReader.Cpk;
    using DataReader.Cpk.Lzo;
    using DataReader.Data;
    using DataReader.Par;
    using NUnit.Framework;

    public sealed class OriginalEffectFormatTests
    {
        [Test]
        public void FlexeffReadsFloatSizeTaskPaddingAndFirstValidDuplicate()
        {
            byte[] bytes = SyntheticTable();
            var file = ((IFileReader<EffectDefinitionFile>)new EffectDefinitionFileReader()).Read(bytes, 936);
            Assert.That(file.Groups.Length, Is.EqualTo(560));
            Assert.That(file.TryGetType(123, out EffectDefinition d), Is.True);
            Assert.That(d.InitSize, Is.EqualTo(1.25f));
            EffectTask task = file.Groups[0].Objects.Single().Tasks.Single();
            Assert.That(task.SpeedX, Is.EqualTo(12.5f));
            Assert.That(task.YStretchSpeed, Is.EqualTo(-2f));
            Assert.That(task.Duration, Is.EqualTo(3.5f));
        }

        [Test]
        public void OrdinaryLzoRejectsTrailingBytesButExplicitPrefixReportsConsumedLength()
        {
            byte[] stream = { 21, 10, 20, 30, 40, 17, 0, 0, 99 };
            var output = new byte[4];
            Assert.Throws<OverflowException>(() => MiniLzo.Decompress(stream, output));
            Assert.That(MiniLzo.DecompressPrefix(stream, output), Is.EqualTo(8));
            Assert.That(output, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
        }

        [Test]
        public void CallbackAndSoundUnionsRetainOriginalFieldOrder()
        {
            var task = new EffectTask
            {
                XStretchSpeed = 10,
                SpeedX = 1,
                SpeedY = 2,
                SpeedZ = 3,
                CircleRadius = 20,
                RotAxisX = 4,
                RotAxisY = 5,
                RotAxisZ = 6,
                CircleAxisX = 30,
                CircleAxisY = 7,
                CircleAxisZ = 8,
                CircleSpeed = 9,
                SpeedAlpha = 40,
                SpeedRed = 11,
                SpeedGreen = 12,
                SpeedBlue = 13
            };
            Assert.That(task.Callbacks().Select(c => c.groupId), Is.EqualTo(new[] { 10, 20, 30, 40 }));
            Assert.That(task.Callbacks().Last().position.Z, Is.EqualTo(13));
            Assert.That(task.Sounds().Select(s => s.delay), Is.EqualTo(new[] { 1f, 3f, 4f, 6f }));
        }

        [Test]
        public void ParticleCurvePreservesReversingSamplesAndUsesOriginalBracketLookup()
        {
            var points = Enumerable.Range(0, 101).Select(i => new Primitives.GameBoxVector3(i / 100f, i / 100f * 2, 0)).ToArray();
            var curve = new ParticleCurve { Channels = 1, Samples = new[] { points, points, points } };
            Assert.That(curve.Evaluate(0.325f), Is.EqualTo(0.65f).Within(0.00001f));
            Assert.That(curve.Evaluate(1.1f), Is.Zero);
            points[43].X = 0.41f;
            Assert.That(curve.Evaluate(0.425f), Is.EqualTo(0.87f).Within(0.00001f));
        }

        [Test]
        public void TruncatedCsfAndUnknownParticleVersionAreRejected()
        {
            Assert.Throws<EndOfStreamException>(() => ((IFileReader<ParticleShapeFile>)new ParticleShapeFileReader()).Read(new byte[] { 99, 115, 102, 0 }, 936));
            byte[] par = { 115, 103, 112, 97, 114, 0, 2, 0, 1, 0 };
            Assert.Throws<InvalidDataException>(() => ((IFileReader<ParticleFile>)new ParticleFileReader()).Read(par, 936));
        }

        private static byte[] SyntheticTable()
        {
            var bytes = new byte[EffectDefinitionFileReader.Pal3Size];
            using var stream = new MemoryStream(bytes);
            using var w = new BinaryWriter(stream);
            for (int i = 0; i < 2; i++)
            {
                stream.Position = i * 280;
                w.Write(0x71756164u);
                stream.Position = i * 280 + 36;
                w.Write(123u);
                stream.Position = i * 280 + 200;
                w.Write(i == 0 ? 1.25f : 7f);
                stream.Position = i * 280 + 212;
                w.Write((byte)1);
            }
            stream.Position = 1324 * 280;
            w.Write(1);
            w.Write(123u);
            long task = 1324 * 280 + 68;
            stream.Position = task + 4;
            w.Write(12.5f);
            stream.Position = task + 76;
            w.Write(3.5f);
            stream.Position = task + 88;
            w.Write((byte)1);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write((byte)0xCC);
            w.Write(-2f);
            return bytes;
        }
    }
}
