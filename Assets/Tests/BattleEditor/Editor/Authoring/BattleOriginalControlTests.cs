// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Core.DataReader.Data;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Game.Effect.Runtime;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleOriginalControlTests
    {
        private sealed class Actor : IEffectActor
        {
            public Vector3 Position
            {
                get; set;
            }

            public float FacingDegrees { get; set; } = 45;

            public float Blend { get; set; } = 1;

            public bool ActionOver
            {
                get; set;
            }

            public bool Controlled, LongBlade;
            public float Left, Right;
            public string Action;

            public void SetExternalTask(bool controlled, object owner) => Controlled = controlled;

            public void SetAction(string action) => Action = action;

            public void SetLongBlade(bool enabled) => LongBlade = enabled;

            public void SetWeapons(float left, float right)
            {
                Left = left;
                Right = right;
            }
        }

        private sealed class CameraPort : IEffectCamera
        {
            public int Starts, Stops, Index;
            public Vector3 Quake;

            public void StopList() => Stops++;

            public bool StartList(int index)
            {
                Starts++;
                Index = index;
                return true;
            }

            public void SetQuake(float amplitude, float frequency, float decay) => Quake = new Vector3(amplitude, frequency, decay);
        }

        private sealed class Movie : IEffectMovie
        {
            public bool Finished
            {
                get; set;
            }

            public bool Disposed;

            public void Dispose() => Disposed = true;
        }

        private sealed class Context : IBattleEffectContext
        {
            private readonly Battle.BattleRandom _effectRandom = new(1);

            public int NextEffectRandom() => _effectRandom.Next();

            public readonly Actor Role = new();
            public readonly CameraPort Cam = new();
            public readonly Movie Film = new();
            public readonly List<string> Movies = new();

            public IEffectActor Actor(int nativeIndex, uint modelCrc = 0) => Role;

            public IEffectCamera Camera => Cam;

            public bool IsModalPresentationRunning => Movies.Count > 0 && !Film.Finished;

            public IEffectMovie PlayMovie(string originalPath)
            {
                Movies.Add(originalPath);
                return Film;
            }
        }

        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type NodeType => typeof(EffectPlayer).Assembly.GetType("Pal3.Game.Effect.Runtime.BattleControlNode", true);
        private static Type ClockType => typeof(EffectPlayer).Assembly.GetType("Pal3.Game.Effect.Runtime.EffectTaskClock", true);

        private static object Node(GameObject root, EffectTask task, Context context, string kind, bool externalNames = false) =>
            Activator.CreateInstance(NodeType, Members, null, new object[] { root, new EffectDefinition { VideoFileNamePrefix = "2DEFFECT\\t" }, new[] { task }, Vector3.zero, 0f, context, kind, externalNames }, null);

        private static bool Apply(object node, EffectTask task, float elapsed, float delta, bool started = true, bool finished = false)
        {
            object clock = Activator.CreateInstance(ClockType, true);
            ClockType.GetProperty("StartedAt", Members).SetValue(clock, 0f);
            ClockType.GetProperty("StartedNow", Members).SetValue(clock, started);
            ClockType.GetField("_finished", Members).SetValue(clock, finished);
            NodeType.BaseType.GetField("LocalTime", Members).SetValue(node, elapsed);
            NodeType.GetMethod("Apply", Members).Invoke(node, new[] { task, clock, (object)delta });
            return (bool)ClockType.GetProperty("Finished", Members).GetValue(clock);
        }

        [Test]
        [Category("LocalNativeEvidence")]
        public void CombatTaskDurationUsesThePublishedStrictBoundaryWithoutAnEarlyEpsilon()
        {
            string directory = Environment.GetEnvironmentVariable("PAL3_NATIVE_VECTORS_ROOT");
            if (directory == null)
                Assert.Ignore("Published native vectors are local analysis artifacts.");
            JObject evidence = JObject.Parse(File.ReadAllText(Path.Combine(directory, "effect-duration-native-vectors.json")));
            int count = 0;
            foreach (JProperty variant in evidence.Properties())
                foreach (JToken row in variant.Value["rows"])
                {
                    var task = new EffectTask { Duration = (float)row["config"]["duration"] };
                    object clock = Activator.CreateInstance(ClockType, true);
                    ClockType.GetField("NativeComparison", Members).SetValue(clock, true);
                    ClockType.GetField("_initialized", Members).SetValue(clock, true);
                    ClockType.GetProperty("StartedAt", Members).SetValue(clock, 0f);
                    object[] parameters = { (float)row["config"]["elapsed"], .02f, task, 0f };
                    Assert.That(ClockType.GetMethod("Tick", Members).Invoke(clock, parameters), Is.True);
                    Assert.That(ClockType.GetProperty("Finished", Members).GetValue(clock), Is.EqualTo((bool)row["finished"]), variant.Name + ": " + row["config"]);
                    count++;
                }
            Assert.That(count, Is.EqualTo(50));
        }

        [Test]
        public void ModalMovieResumesTheRemainingNodesAtTheSameEffectTime()
        {
            var hash = new Crc32Hash();
            hash.Init();
            using var resources = new EffectResources(new CpkFileSystem(Application.temporaryCachePath, hash, 936));
            var definitions = new EffectDefinition[1324];
            definitions[0] = new EffectDefinition { IsValid = true, TypeNameCrc = 1, ClassNameCrc = hash.Compute("bink", 936), VideoFileNamePrefix = "2DEFFECT\\t" };
            definitions[1] = new EffectDefinition { IsValid = true, TypeNameCrc = 2, ClassNameCrc = hash.Compute("action", 936) };
            var group = new EffectGroup
            {
                Objects = new[]
            {
                new EffectGroupObject { TypeCrc = 1, Tasks = new[] { new EffectTask { IsValid = true, XStretchSpeed = 102, StartTime = -1, Duration = 1 } } },
                new EffectGroupObject { TypeCrc = 2, Tasks = new[] { new EffectTask { IsValid = true, XStretchSpeed = 17, SpeedX = 10, StartTime = -1, Duration = 1 } } }
            }
            };
            typeof(EffectResources).GetField("_definitions", Members).SetValue(resources, new EffectDefinitionFile(definitions, new[] { group }));
            var context = new Context();
            var root = new GameObject("modal effect continuation");
            var player = root.AddComponent<EffectPlayer>();
            player.AutoAdvance = false;
            try
            {
                player.Initialize(resources, 0, battleContext: context, clockOrigin: 1000);
                player.AdvanceTo(1000.02);
                Assert.That(player.Error, Is.Null);
                Assert.That(context.IsModalPresentationRunning, Is.True);
                Assert.That(context.Role.Action, Is.Null);
                float paused = player.Time;
                Assert.That(player.ResumeBattleStep(), Is.False);
                Assert.That(player.Time, Is.EqualTo(paused));
                context.Film.Finished = true;
                Assert.That(player.ResumeBattleStep(), Is.True);
                Assert.That(player.Time, Is.EqualTo(paused));
                Assert.That(context.Role.Action, Is.EqualTo("z17"));
                Assert.That(context.Role.Position.x, Is.EqualTo(((float)1000.02 - 1000f) * 10).Within(0.000001f));
                Assert.That(player.Error, Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CameraAndQuakeDispatchTheirNativeFieldsAndCancellation()
        {
            var context = new Context();
            var root = new GameObject("original control fixture") { hideFlags = HideFlags.HideAndDontSave };
            object node = null;
            try
            {
                var camera = new EffectTask { XStretchSpeed = 54, Duration = 10 };
                node = Node(root, camera, context, "camera");
                Apply(node, camera, 0, 0.02f);
                Assert.That((context.Cam.Stops, context.Cam.Starts, context.Cam.Index), Is.EqualTo((1, 1, 54)));
                ((IDisposable)node).Dispose();
                node = null;
                Assert.That(context.Cam.Stops, Is.EqualTo(2));
                var quake = new EffectTask { XStretchSpeed = 9, SpeedX = 2, SpeedY = 80 };
                node = Node(root, quake, context, "quake");
                Apply(node, quake, 0, 0.02f);
                Assert.That(context.Cam.Quake, Is.EqualTo(new Vector3(9, 80, 2)));
            }
            finally
            {
                try
                {
                    ((IDisposable)node)?.Dispose();
                }
                finally { Object.DestroyImmediate(root); }
            }
        }

        [Test]
        public void MovieKeepsItsTaskUntilTheModalPresentationFinishes()
        {
            var context = new Context();
            var root = new GameObject("original Bink fixture") { hideFlags = HideFlags.HideAndDontSave };
            object node = null;
            try
            {
                var task = new EffectTask { XStretchSpeed = 102, SpeedX = 0.1f, Duration = 1 };
                node = Node(root, task, context, "bink");
                Assert.That(Apply(node, task, 0.02f, 0.02f), Is.False);
                Assert.That(context.Movies, Is.Empty);
                Assert.That(Apply(node, task, 0.1f, 0.02f), Is.False);
                Assert.That(context.Movies, Is.EqualTo(new[] { "2DEFFECT\\t102.bik" }));
                Assert.That(Apply(node, task, 2, 0.02f, finished: true), Is.False);
                context.Film.Finished = true;
                Assert.That(Apply(node, task, 2, 0.02f), Is.True);
                ((IDisposable)node).Dispose();
                node = null;
                Assert.That(context.Film.Disposed, Is.True);
            }
            finally
            {
                try
                {
                    ((IDisposable)node)?.Dispose();
                }
                finally { Object.DestroyImmediate(root); }
            }
        }

        [Test]
        [Category("LocalNativeEvidence")]
        public void PublishedActionControlVectorsMatchAllNativeStoresAndBoundaries()
        {
            string directory = Environment.GetEnvironmentVariable("PAL3_NATIVE_VECTORS_ROOT");
            if (directory == null)
                Assert.Ignore("Published native vectors are local analysis artifacts.");
            JObject document = JObject.Parse(File.ReadAllText(Path.Combine(directory, "action-control-native-vectors.json")));
            int count = 0;
            foreach (JProperty variant in document.Properties())
                foreach (JToken row in variant.Value["rows"])
                {
                    JToken config = row["config"];
                    Vector3 Read(JToken token) => new((float)token[0], (float)token[1], (float)token[2]);
                    Vector3 speed = Read(config["speed"]), destination = Read(config["destination"]);
                    var task = new EffectTask
                    {
                        XStretchSpeed = 17,
                        SpeedX = speed.x,
                        SpeedY = speed.y,
                        SpeedZ = speed.z,
                        RotAxisX = destination.x,
                        RotAxisY = destination.y,
                        RotAxisZ = destination.z,
                        SpeedAlpha = (float)config["alpha"],
                        SpeedRed = 1,
                        SpeedGreen = -1,
                        SpeedBlue = (bool)config["target"] ? 1 : 0,
                        Duration = (float)config["duration"],
                        RequireLoops = 1,
                        CreateNew = (bool)config["front"],
                        ModifyLast = (bool)config["byOver"],
                        YStretchSpeed = 123
                    };
                    var context = new Context();
                    context.Role.Position = Read(config["position"]);
                    context.Role.Blend = (float)config["blend"];
                    context.Role.ActionOver = (bool)config["over"];
                    object node = Node(new GameObject("native action vector"), task, context, "action", variant.Name == "PAL3A");
                    try
                    {
                        float elapsed = (float)config["elapsed"];
                        float delta = (float)((double)0.02f - Math.Max(0, (double)elapsed - task.Duration));
                        bool finished = Apply(node, task, elapsed, delta, finished: (bool)config["ended"] || elapsed >= task.Duration);
                        Vector3 expected = Read(row["position"]);
                        Assert.That(Vector3.Distance(context.Role.Position, expected), Is.LessThanOrEqualTo(0.0002f), variant.Name + " position: " + count);
                        Assert.That(context.Role.FacingDegrees, Is.EqualTo((float)row["face"]).Within(0.0002f), variant.Name + " face: " + count);
                        Assert.That(context.Role.Blend, Is.EqualTo((float)row["blend"]).Within(0.000002f));
                        Assert.That((context.Role.Left, context.Role.Right, context.Role.Controlled, finished),
                            Is.EqualTo(((float)row["left"], (float)row["right"], (bool)row["external"], (bool)row["finished"])));
                        count++;
                    }
                    finally { ((IDisposable)node).Dispose(); }
                }
            Assert.That(count, Is.EqualTo(722));
        }
    }
}
