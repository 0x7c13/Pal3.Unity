// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Linq;
    using Core.DataReader.Par;
    using Core.Primitives;
    using Effect.Original;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class OriginalSparTests
    {
        internal static SparKeyframe Key(float time = 0)
        {
            return new SparKeyframe
            {
                Time = time,
                Position = new GameBoxVector3(100, 200, 300),
                EmissionMinimum = 0,
                EmissionMaximum = 0,
                ShapeParameters = Array.Empty<float>(),
                PathPoints = Array.Empty<GameBoxVector3>(),
                LifeMinimum = 10,
                LifeMaximum = 10,
                SizeMinimum = 2,
                SizeMaximum = 2,
                AlphaMinimum = 1,
                AlphaMaximum = 1,
                ColorMinimum = new GameBoxVector3(1, 1, 1),
                ColorMaximum = new GameBoxVector3(1, 1, 1),
                VelocityMinimum = new GameBoxVector3(10, 0, 0),
                VelocityMaximum = new GameBoxVector3(10, 0, 0),
                Controllers = Enumerable.Range(0, 6).Select(_ => new SparController { Parameters = Array.Empty<float>() }).ToArray()
            };
        }

        private static SparGroup Group(params SparKeyframe[] keys) => new()
        {
            Capacity = 2,
            VertexCapacity = 12,
            Keyframes = keys,
            TextureFrames = new[] { new[] { 0f, 0f, 0.5f, 1f }, new[] { 0.5f, 0f, 1f, 1f } }
        };

        [Test]
        public void NativeRateOverridesFileAndUpdatesNewUnitsBeforeRendering()
        {
            SparKeyframe key = Key();
            key.Controllers[0].Kind = 1;
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.1f);
            Assert.That(emitter.Count, Is.EqualTo(2));
            Assert.That(emitter.Units[0].Age, Is.EqualTo(0.1f));
            Assert.That(emitter.Units[0].Position, Is.EqualTo(new Vector3(1, 0, 0)));
            emitter.Stop();
            Assert.That(emitter.Count, Is.Zero);
            emitter.Start();
            emitter.Step(0.1f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(1));
        }

        [TestCase(2, 2, 0, 0, 1.02f)]
        [TestCase(3, 2, 0, 0, 0.918f)]
        public void ForceAndSizeDependentDragUseSemiImplicitEuler(int kind, float force, float y, float z, float expectedX)
        {
            SparKeyframe key = Key();
            key.Controllers[0] = new SparController { Kind = kind, Parameters = new[] { force, y, z, 0.5f } };
            var emitter = new OriginalSparEmitter(Group(key), 123);
            emitter.Step(0.1f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(expectedX).Within(0.000001));
        }

        [TestCase(4, 3f)]
        [TestCase(5, 3.01f)]
        [TestCase(6, 3.99f)]
        public void AttractionUsesCurrentAgeAndKeepsTheAuthoredVelocity(int kind, float expectedX)
        {
            SparKeyframe key = Key();
            key.Shape = 2;
            key.ShapeParameters = new[] { 4f, 0, 0, 4f, 0, 0 };
            key.Controllers[0].Kind = kind;
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.1f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(expectedX).Within(0.000001));
            Assert.That(emitter.Units[0].Velocity.x, Is.EqualTo(10));
        }

        [Test]
        public void PathsAdvanceByDiscreteIndexAndDiscardOverflow()
        {
            SparKeyframe key = Key(100);
            key.HasPath = true;
            key.PathRate = 10;
            key.PathPoints = new[] { new GameBoxVector3(2, 0, 0), new GameBoxVector3(8, 0, 0) };
            SparGroup group = Group(key);
            group.Capacity = 10;
            var emitter = new OriginalSparEmitter(group, 1);
            emitter.Step(0.1f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(8));
            emitter.Stop();
            emitter.Start();
            emitter.Step(0.5f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(2));
        }

        [Test]
        public void PointShapeRotationUsesRadiansAndDoesNotRotateVelocity()
        {
            SparKeyframe key = Key(100);
            key.Shape = 3;
            key.ShapeParameters = new[] { 1f, 0, 0 };
            key.Controllers[5] = new SparController { Kind = 1, Parameters = new[] { 0f, 0, 1, Mathf.PI } };
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.5f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(0).Within(0.000001));
            Assert.That(emitter.Units[0].Position.y, Is.EqualTo(1).Within(0.000001));
            Assert.That(emitter.Units[0].Velocity.x, Is.EqualTo(10));
        }

        [Test]
        public void KeyframeBoundaryKeepsLeftControllersAndLoopDropsOvershoot()
        {
            SparKeyframe a = Key(), b = Key(1), c = Key(2);
            a.Controllers[0].Kind = 1;
            b.SizeMinimum = b.SizeMaximum = 6;
            SparGroup group = Group(a, b, c);
            group.Loop = true;
            var emitter = new OriginalSparEmitter(group, 1);
            emitter.Step(1);
            Assert.That(emitter.Units[0].Size, Is.EqualTo(6));
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(10));
            emitter.Step(0.1f);
            Assert.That(emitter.Units[0].Position.x, Is.EqualTo(10));
            emitter.Step(1.2f);
            Assert.That(emitter.Time, Is.Zero);
        }

        [TestCase(1, 1)]
        [TestCase(2, 0)]
        public void TextureFramesClampOrLoopFromAgeWithoutAnInitialFrameOffset(int kind, int expectedFrame)
        {
            SparKeyframe key = Key();
            key.FrameMinimum = key.FrameMaximum = 1;
            key.Controllers[4] = new SparController { Kind = kind, Parameters = new[] { 0.125f } };
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.5f);
            Assert.That(emitter.Units[0].Frame, Is.EqualTo(expectedFrame));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void NonzeroKeyframeFrameBoundsDoNotReachTheNativeLiveEmitter(int frameController)
        {
            // Independent native execution of 0x4c5efd -> 0x4c8509 -> 0x4c5fea
            // keeps live +cc/+d0 at zero despite serialized ranges 5..15/9..13.
            SparKeyframe first = Key(), last = Key(0.04f);
            first.FrameMinimum = 5;
            first.FrameMaximum = 15;
            last.FrameMinimum = 9;
            last.FrameMaximum = 13;
            foreach (SparKeyframe key in new[] { first, last })
                key.Controllers[4] = new SparController { Kind = frameController, Parameters = new[] { 0.125f } };
            SparGroup group = Group(first, last);
            group.Capacity = 64;
            group.TextureFrames = Enumerable.Range(0, 16).Select(i => new[] { i / 16f, 0, (i + 1) / 16f, 1 }).ToArray();
            var emitter = new OriginalSparEmitter(group, 1);
            for (int i = 0; i < 3; i++)
                emitter.Step(0.02f);
            Assert.That(emitter.Count, Is.GreaterThan(20));
            Assert.That(emitter.Units.Take(emitter.Count).Select(u => u.Frame), Is.All.Zero);
            Assert.That(new[] { first.FrameMinimum, first.FrameMaximum, last.FrameMinimum, last.FrameMaximum }, Is.EqualTo(new[] { 5, 15, 9, 13 }));
        }

        [Test]
        public void FadeAndColorControllersReplaceInitialValuesUsingLifetimeFraction()
        {
            SparKeyframe key = Key();
            key.LifeMinimum = key.LifeMaximum = 1;
            key.Controllers[2] = new SparController { Kind = 2, Parameters = new[] { 1f, 0, 0, 0, 0, 1 } };
            key.Controllers[3].Kind = 1;
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.25f);
            Assert.That(emitter.Units[0].Alpha, Is.EqualTo(0.75f));
            Assert.That(emitter.Units[0].Color, Is.EqualTo(new Vector3(0.75f, 0, 0.25f)));
        }

        [Test]
        public void NativeSwapRemovalSkipsTheMovedUnitUntilTheNextFrame()
        {
            SparKeyframe key = Key();
            key.LifeMinimum = key.LifeMaximum = 0.01f;
            var emitter = new OriginalSparEmitter(Group(key), 1);
            emitter.Step(0.1f);
            Assert.That(emitter.Count, Is.EqualTo(1));
            Assert.That(emitter.Units[0].Age, Is.Zero);
        }
    }
}
