// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Effect.Original;
    using Core.DataReader.Data;
    using Core.Utilities;
    using Engine.Core.Implementation;
    using Settings;
    using System.Reflection;
    using UnityEditor;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class OriginalEffectTests
    {
        // Script groups plus the scene groups. Numeric IDs are integration
        // contracts, not copied original binary fixtures.
        public static readonly int[] SceneGroups = { 6,12,14,15,25,83,98,175,224,240,251,252,254,255,256,257,
            259,260,261,262,263,264,265,266,270,275,276,277,278,290,295,297,314,331,332,337,339,341,
            368,369,376,389,400,450,451,452,453,454,455,456,457,458,465,467,468,515 };

        private static OriginalEffectResources Open()
        {
            string path = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT") ?? Path.GetFullPath("../Game Data/PAL3");
            if (!Directory.Exists(path))
                Assert.Ignore("Set PAL3_TEST_DATA_ROOT to the local PAL3 data directory");
            var crc = new Crc32Hash();
            crc.Init();
            var fs = new CpkFileSystem(path, crc, 936);
            fs.Mount("basedata/basedata.cpk", 936);
            return new OriginalEffectResources(fs);
        }

        [Test]
        public void EverySceneGroupHasACompleteOriginalDependencyClosure()
        {
            using var resources = Open();
            var errors = SceneGroups.SelectMany(id => resources.Diagnose(id).Select(e => $"{id}: {e}")).ToArray();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void EverySceneGroupCreatesSimulatesRendersAndDisposes()
        {
            using var resources = Open();
            var cameraRoot = new GameObject("Effect test camera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false;
            var errors = new List<string>();
            try
            {
                foreach (int id in SceneGroups)
                {
                    var root = new GameObject($"Original effect test {id}");
                    try
                    {
                        var player = root.AddComponent<OriginalEffectPlayer>();
                        player.AutoAdvance = false;
                        player.Initialize(resources, id, 123);
                        Assert.That(player.Error, Is.Null, $"{id} initialization");
                        int maximum = 0;
                        for (int tick = 0; tick < 650; tick++)
                        {
                            player.Advance(0.02f);
                            if (tick % 10 == 0)
                            {
                                player.Render(camera);
                                maximum = Math.Max(maximum, player.QuadCount);
                            }
                            if (player.Error != null)
                                break;
                        }
                        Assert.That(player.Error, Is.Null, $"{id} simulation");
                        Assert.That(maximum, Is.GreaterThan(0), $"{id}: no visible geometry or particles");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{id}: {ex}");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraRoot);
            }
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void SeekReproducesGeometryAndDoesNotReplayAudio()
        {
            using var resources = Open();
            var root = new GameObject("Effect deterministic test");
            var cameraRoot = new GameObject("Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false;
            try
            {
                int audio = 0;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 254, 123, sound: _ => audio++);
                for (int i = 0; i < 150; i++)
                    player.Advance(0.02f);
                player.Render(camera);
                int sounds = audio;
                Assert.That(sounds, Is.GreaterThan(0), "Original audio tasks must execute during forward playback");
                Vector3[] expected = Vertices(root);
                player.Seek(1);
                player.Seek(3);
                player.Render(camera);
                Assert.That(audio, Is.EqualTo(sounds));
                Assert.That(Vertices(root), Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
            }
        }

        [Test]
        public void ParticleCurveOverridesDoNotMutateCachedOriginalData()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            try
            {
                const string path = "effect\\gbParticle\\yiwai3.par";
                Assert.That(OriginalEffectResources.RelativeTo("effect/gbParticle/yiwai3.par", "partexture/guangdianer.jpg"),
                    Is.EqualTo("effect\\gbParticle\\partexture\\guangdianer.jpg"));
                float original = resources.Particle(path).Particles[0].Size.Evaluate(0.5f);
                edits.ParticleCurves.Add(new OriginalParticleCurveOverride
                {
                    File = path,
                    Parameter = "Size",
                    Index = 0,
                    Channels = new[] { AnimationCurve.Constant(0, 1, 17) }
                });
                Assert.That(resources.Particle(path, edits).Particles[0].Size.Evaluate(0.5f), Is.EqualTo(17));
                Assert.That(resources.Particle(path).Particles[0].Size.Evaluate(0.5f), Is.EqualTo(original));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void NativeParticleRendererRetainsManuallySimulatedParticles()
        {
            using var resources = Open();
            var root = new GameObject("Native particle handoff");
            var cameraRoot = new GameObject("Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false;
            try
            {
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 175, 123);
                for (int i = 0; i < 100; i++)
                    player.Advance(0.02f);
                player.Render(camera);
                Assert.That(root.GetComponentsInChildren<ParticleSystem>().Sum(ps => ps.particleCount), Is.GreaterThan(0));
                Vector3[] positions = ParticlePositions(root);
                player.Seek(1);
                player.Seek(2);
                player.Render(camera);
                Assert.That(ParticlePositions(root), Is.EqualTo(positions), "Fixed-seed particle replay must match forward simulation");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
            }
        }

        [Test]
        public void PersistentGroup275KeepsEmittingPastItsOriginalRepeatInterval()
        {
            using var resources = Open();
            var root = new GameObject("Persistent effect test");
            try
            {
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 275);
                player.Loop = true;
                player.RepeatInterval = 9.5f;
                for (int i = 0; i < 1500; i++)
                    player.Advance(0.02f);
                Assert.That(player.Error, Is.Null);
                Assert.That(player.IsAlive, Is.True);
                Assert.That(player.Time, Is.EqualTo(30).Within(0.001f));
                Assert.That(player.NodeCount, Is.LessThan(150));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TransportSceneEffectIsIndependentOfFlexeffGroupZero()
        {
            using var resources = Open();
            var root = new GameObject("Transport test");
            try
            {
                var portal = root.AddComponent<OriginalPortalEffect>();
                portal.AutoAdvance = false;
                portal.Initialize(resources);
                portal.Sample(2);
                Assert.That(root.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.vertexCount), Is.EqualTo(13 * 4));
                portal.Sample(1);
                Vector3 vertex = root.GetComponentsInChildren<MeshFilter>().Single(m => m.name == "Transport outer cone").sharedMesh.vertices[0];
                Assert.That(vertex.x, Is.EqualTo(-1.4f * Mathf.Cos(6.4f * Mathf.Deg2Rad)).Within(0.00001f));
                Assert.That(vertex.z, Is.EqualTo(1.4f * Mathf.Sin(6.4f * Mathf.Deg2Rad)).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ZeroOrbitAxisPreservesRadiusAndInvalidTasksStayDisabled()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Orbit semantics");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = OverrideObject(resources, 15, 0);
                edits.Objects.Add(edit);
                for (int i = 1; i < resources.Definitions.Groups[15].Objects.Length; i++)
                    edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = 15, ObjectIndex = i, Hidden = true });
                edit.Definition.InitXPosition = edit.Definition.InitYPosition = edit.Definition.InitZPosition = 0;
                edit.Definition.FramePerSecond = 0;
                edit.Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10, CircleRadius = 20 } };
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 15, overrides: edits);
                player.Advance(0.04f);
                player.Render(camera);
                Assert.That(player.QuadCount, Is.GreaterThan(0), "A negative wait starts after init in the same update");
                player.Seek(0.2f);
                player.Render(camera);
                Assert.That(Vertices(root).Average(v => v.z), Is.EqualTo(1).Within(0.00001f), "Identity orbit rotation retains the radius vector");
                edit.Tasks[0].IsValid = false;
                player.Seek(0.2f);
                player.Render(camera);
                Assert.That(player.NodeCount, Is.Zero);
                Assert.That(player.QuadCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void DiagnosisIncludesCallbackOverridesAndDoesNotPoisonOriginalCache()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            try
            {
                Assert.That(resources.Diagnose(275), Is.Empty);
                var edit = OverrideObject(resources, 273, 0);
                edits.Objects.Add(edit);
                edit.Definition.TextureName = "effect\\missing-review-texture.tga";
                Assert.That(resources.Diagnose(275, edits).Any(e => e.Contains("missing-review-texture")), Is.True);
                Assert.That(resources.Diagnose(275), Is.Empty);
                edit.Hidden = true;
                Assert.That(resources.Diagnose(275, edits), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void NonFiniteOverrideParametersAreRejectedBeforeSimulation()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            try
            {
                var edit = OverrideObject(resources, 15, 0);
                edits.Objects.Add(edit);
                edit.Tasks[0].SpeedX = float.NaN;
                Assert.That(resources.Diagnose(15, edits).Any(e => e.Contains("finite")), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void TaperedMeteorStripSharesEndpointsAndInterpolatesLuminosity()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Meteor strip continuity");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = OverrideObject(resources, 240, 0);
                edits.Objects.Add(edit);
                for (int i = 1; i < resources.Definitions.Groups[240].Objects.Length; i++)
                    edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = 240, ObjectIndex = i, Hidden = true });
                Assert.That(EffectClass.Name(edit.Definition.ClassNameCrc), Is.EqualTo("meteor"));
                edit.Definition.RotationAngle = -1;
                edit.Definition.Width = 20;
                edit.Definition.DivisionCount = 16;
                edit.Definition.BottomRadius = 10;
                edit.Definition.TopRadius = 100;
                edit.Definition.TextureRepeat = 255;
                edit.Definition.BottomRiseSpeed = 100;
                edit.Definition.TopShrink = edit.Definition.BottomShrink = 0;
                edit.Tasks = new[] { new EffectTask { IsValid = true, Duration = 2 } };
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, 5, -10);
                edit.Definition.InitXPosition = edit.Definition.InitYPosition = edit.Definition.InitZPosition = 0;
                edit.Definition.TopRadiusIncreaseSpeed = edit.Definition.BottomRadiusIncreaseSpeed = edit.Definition.TopRiseSpeed = 0;
                edit.Definition.RotationAngle = 1;
                edit.Tasks[0].SpeedX = 20;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 240, overrides: edits);
                player.Advance(0.04f);
                player.Render(camera);
                Mesh head = root.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(head.vertices.Average(v => v.x), Is.EqualTo(-0.1f).Within(0.00001f), "StartMeteor captures position before the task moves the emitter");
                edit.Definition.RotationAngle = -1;
                edit.Tasks[0].SpeedX = 0;
                player.Seek(0.5f);
                player.Render(camera);
                Mesh mesh = root.GetComponentsInChildren<MeshFilter>().Single(m => m.name == "Original meteor chain").sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Color32[] colors = mesh.colors32;
                Assert.That(vertices.Length, Is.GreaterThanOrEqualTo(8));
                for (int i = 0; i + 7 < vertices.Length; i += 4)
                {
                    Assert.That(Vector3.Distance(vertices[i + 1], vertices[i + 4]), Is.LessThan(0.00001f));
                    Assert.That(Vector3.Distance(vertices[i + 2], vertices[i + 7]), Is.LessThan(0.00001f));
                }
                Assert.That(colors[0].a, Is.GreaterThan(colors[1].a));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void ParticleEmitterRunsItsInitialCycleBeforeTheDelayedTaskEnablesLooping()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Initial particle cycle");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = OverrideObject(resources, 175, 0);
                edits.Objects.Add(edit);
                for (int i = 1; i < resources.Definitions.Groups[175].Objects.Length; i++)
                    edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = 175, ObjectIndex = i, Hidden = true });
                var par = resources.Particle(edit.Definition.ParticleFileName);
                float cycle = par.Emitters.Max(e => e.CycleMin - e.CycleMax);
                edit.Tasks = new[] { new EffectTask { IsValid = true, StartTime = cycle + 1, Duration = 0.2f } };
                for (int i = 0; i < par.Emitters.Length; i++)
                    edits.ParticleCurves.Add(new OriginalParticleCurveOverride
                    {
                        File = edit.Definition.ParticleFileName,
                        Emitter = true,
                        Index = i,
                        Parameter = "Life",
                        Channels = Enumerable.Range(0, par.Emitters[i].Life.Channels)
                            .Select(_ => AnimationCurve.Constant(0, 1, 0.1f)).ToArray()
                    });
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 175, overrides: edits);
                player.Seek(0.1f);
                player.Render(camera);
                Assert.That(player.QuadCount, Is.GreaterThan(0), "The first emitter cycle does not wait for the task start time");
                player.Seek(cycle + 0.5f);
                player.Render(camera);
                Assert.That(player.QuadCount, Is.Zero, "The nonlooping initial cycle must stop and drain before the delayed task");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void CombatParticleTaskMotionMovesTheWholeCloudInWorldSpaceParFiles()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Combat particle task motion");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = OverrideObject(resources, 175, 0);
                edits.Objects.Add(edit);
                for (int i = 1; i < resources.Definitions.Groups[175].Objects.Length; i++)
                    edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = 175, ObjectIndex = i, Hidden = true });
                Assert.That(resources.Particle(edit.Definition.ParticleFileName).Space, Is.Zero);
                edit.Tasks = new[] { new EffectTask { IsValid = true, Duration = 2 } };
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 175, 123, edits);
                player.Seek(0.5f);
                player.Render(camera);
                Vector3[] stationary = ParticlePositions(root);
                edit.Tasks[0].SpeedX = 20;
                player.Seek(0.5f);
                player.Render(camera);
                Vector3[] moving = ParticlePositions(root);
                Assert.That(moving.Length, Is.GreaterThan(1));
                Assert.That(moving.Length, Is.EqualTo(stationary.Length));
                Vector3 translation = moving[0] - stationary[0];
                Assert.That(translation.x, Is.LessThan(-0.4f));
                for (int i = 1; i < moving.Length; i++)
                    Assert.That(Vector3.Distance(moving[i] - stationary[i], translation), Is.LessThan(0.00001f), "The combat wrapper translates existing particles uniformly");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void FailedTextureAndModelInitializationReleaseMaterials()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Failed resource cleanup");
            try
            {
                int before = Resources.FindObjectsOfTypeAll<Material>().Length;
                for (int i = 0; i < 3; i++)
                    Assert.Throws<FileNotFoundException>(() => resources.Material("effect\\missing-review-texture.tga", true, false));
                Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length, Is.EqualTo(before));
                var edit = OverrideObject(resources, 173, 0);
                edits.Objects.Add(edit);
                Assert.That(EffectClass.Name(edit.Definition.ClassNameCrc), Is.EqualTo("model"));
                edit.Definition.TextureName = "effect\\missing-review-model.cvd";
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                for (int i = 0; i < 3; i++)
                {
                    player.Initialize(resources, 173, overrides: edits);
                    Assert.That(player.Error, Is.Not.Null);
                }
                Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length, Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void EditorReloadKeepsWorkingCopyAndInvalidDataPathLeavesNoBrokenProvider()
        {
            using var resources = Open();
            Type type = TypeCache.GetTypesDerivedFrom<EditorWindow>().FirstOrDefault(t => t.FullName == "Editor.OriginalEffectEditor");
            Assert.That(type, Is.Not.Null);
            var window = (EditorWindow)ScriptableObject.CreateInstance(type);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            void Call(string name) => type.GetMethod(name, flags).Invoke(window, null);
            try
            {
                var working = (OriginalEffectOverrides)type.GetField("_edits", flags).GetValue(window);
                working.Objects.Add(OverrideObject(resources, 15, 0));
                Call("MarkChanged");
                Assert.That(window.hasUnsavedChanges, Is.True);
                Call("OnDisable");
                Call("OnEnable");
                Assert.That(type.GetField("_edits", flags).GetValue(window), Is.SameAs(working));
                Assert.That(window.hasUnsavedChanges, Is.True);
                type.GetField("_dataPath", flags).SetValue(window, Path.GetFullPath("Assets"));
                Call("LoadData");
                Assert.That(type.GetField("_resources", flags).GetValue(window), Is.Null);
                Assert.That((string)type.GetField("_error", flags).GetValue(window), Is.Not.Empty);
            }
            finally
            {
                window.DiscardChanges();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [TestCase(60)]
        [TestCase(120)]
        [TestCase(144)]
        public void RuntimeFrameClockMovesOnEveryFrameWhileFixedSeekingStaysRepeatable(int fps)
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Frame clock");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = SoloObject(resources, edits, 15);
                edit.Definition.FramePerSecond = 0;
                edit.Tasks = new[] { new EffectTask { IsValid = true, Duration = 5, SpeedX = 20 } };
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 15, 123, edits);
                float dt = 1f / fps;
                for (int i = 0; i < 4; i++)
                    player.AdvanceFrame(dt);
                player.Render(camera);
                Vector3 previous = Vertices(root)[0];
                for (int i = 0; i < fps / 2; i++)
                {
                    player.AdvanceFrame(dt);
                    player.Render(camera);
                    Vector3 next = Vertices(root)[0];
                    Assert.That(next.x - previous.x, Is.EqualTo(-dt).Within(0.00001f), "Rendering must not repeat a 50 Hz position on faster displays");
                    previous = next;
                }
                float pausedAt = player.Time;
                player.Paused = true;
                player.AdvanceFrame(dt);
                Assert.That(player.Time, Is.EqualTo(pausedAt));
                player.Seek(0.5f);
                player.Render(camera);
                Vector3[] expected = Vertices(root);
                player.Seek(0.2f);
                player.Seek(0.5f);
                player.Render(camera);
                Assert.That(Vertices(root), Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [TestCase(30)]
        [TestCase(50)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(144)]
        [TestCase(240)]
        public void ModelRotationAndStretchUse60FpsReferenceAcrossFrameRatesAndSeeking(int fps)
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Model frame rate");
            try
            {
                var edit = SoloObject(resources, edits, 173);
                Assert.That(EffectClass.Name(edit.Definition.ClassNameCrc), Is.EqualTo("model"));
                edit.Definition.InitSize = 1;
                edit.Definition.InitXPosition = edit.Definition.InitYPosition = edit.Definition.InitZPosition = 0;
                edit.Definition.InitXRotation = 5;
                edit.Definition.InitYRotation = -10;
                edit.Definition.InitZRotation = 15;
                edit.Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 5,
                    SpeedX = 20, SpeedY = 10, SpeedZ = 5, RotAxisX = 0.2f, RotAxisY = 0.4f, RotAxisZ = -0.1f,
                    XStretchSpeed = 0.01f, YStretchSpeed = 0.02f, CreateTimeSlot = -0.005f } };
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 173, 123, edits);
                Assert.That(player.Error, Is.Null);
                void Check(float seconds)
                {
                    player.Render(null);
                    Transform model = root.transform.GetChild(0);
                    // Assert the chosen compatibility rate independently of
                    // both the model implementation and the seek step size.
                    float referenceFrames = seconds * 60f;
                    Vector3 rotation = new Vector3(5, -10, 15) + new Vector3(0.2f, 0.4f, -0.1f) * referenceFrames;
                    Quaternion expected = Quaternion.AngleAxis(rotation.x, Vector3.right) *
                        Quaternion.AngleAxis(-rotation.y, Vector3.up) * Quaternion.AngleAxis(-rotation.z, Vector3.forward);
                    Assert.That(Quaternion.Angle(model.localRotation, expected), Is.LessThan(0.06f), $"Rotation at {seconds}s");
                    Assert.That(Vector3.Distance(model.localScale, Vector3.one + new Vector3(0.01f, 0.02f, -0.005f) * referenceFrames),
                        Is.LessThan(0.00005f), $"Stretch at {seconds}s");
                    Assert.That(Vector3.Distance(model.localPosition, new Vector3(-1, 0.5f, 0.25f) * seconds), Is.LessThan(0.00001f));
                }
                for (int i = 0; i < fps; i++)
                {
                    player.AdvanceFrame(1f / fps);
                    Check(player.Time);
                }
                Check(1);
                player.Seek(0.02f);
                Check(0.02f); // One seek step applies 1.2 reference frames.
                player.Seek(0.4f);
                player.Seek(1);
                Check(1);
                player.Seek(0);
                player.Speed = 2;
                for (int i = 0; i < fps / 2; i++)
                    player.AdvanceFrame(1f / fps);
                Check(1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void UnsupportedModelOpacityTasksAreDiagnosedInsteadOfSilentlyIgnored()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Model opacity diagnosis");
            try
            {
                var edit = SoloObject(resources, edits, 173);
                edit.Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 2, SpeedAlpha = -0.5f } };
                Assert.That(resources.Diagnose(173, edits).Any(error => error.Contains("model opacity")), Is.True);
                Assert.That(resources.Diagnose(173), Is.Empty, "Override errors must not affect the original group");
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 173, overrides: edits);
                Assert.That(player.Error, Does.Contain("model opacity"));
                Assert.That(player.NodeCount, Is.Zero);
                edit.Tasks[0].IsValid = false;
                Assert.That(resources.Diagnose(173, edits), Is.Empty, "Disabled tasks must not block a group");
                edit.Tasks[0].IsValid = true;
                edit.Tasks[0].SpeedAlpha = 0;
                Assert.That(resources.Diagnose(173, edits), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void CombatParticleScalingAndBlendFollowPal3ParticleEffectRatherThanEngineGbParticleEffect()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            var root = new GameObject("Legacy combat particle semantics");
            var cameraRoot = new GameObject("Camera");
            try
            {
                var edit = SoloObject(resources, edits, 175);
                edit.Definition.InitXPosition = edit.Definition.InitYPosition = edit.Definition.InitZPosition = 0;
                edit.Tasks = new[] { new EffectTask { IsValid = true, Duration = 2 } };
                var par = resources.Particle(edit.Definition.ParticleFileName);
                foreach (var e in par.Emitters)
                {
                    e.Shape = 0;
                    e.Position = e.Scope = e.BoxMin = e.BoxMax = default;
                    e.CycleMin = 10;
                    e.CycleMax = 0;
                    e.SpeedMultiplier = 1;
                    e.Direction.OverrideEvaluation = (_, channel) => channel == 0 ? 1 : 0;
                    e.RandomVelocity.OverrideEvaluation = (_, _) => 10;
                    e.Emission.OverrideEvaluation = (_, _) => 100;
                    e.Life.OverrideEvaluation = (_, _) => 10;
                }
                foreach (var p in par.Particles)
                {
                    p.Gravity = p.Resistance = 0;
                    p.BlendMode = 1;
                }
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false;
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                edit.Definition.InitSize = 20;
                player.Initialize(resources, 175, 123, edits);
                player.Advance(0.02f);
                player.Render(camera);
                // pal3/effect/particle.cpp: offset = velocity * dt; offset *=
                // particleMultiSize. PAL3's wrapper fixes blend to ONE / ONE.
                Assert.That(ParticlePositions(root)[0].x, Is.EqualTo(-10f * 0.02f * 20 / 20).Within(0.00001f));
                Material material = root.GetComponentInChildren<ParticleSystemRenderer>().sharedMaterial;
                Assert.That(material.GetInt("_SrcBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.One));
                Assert.That(material.GetInt("_DstBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.One));
                foreach (var e in par.Emitters)
                {
                    e.Direction.OverrideEvaluation = (_, _) => 0;
                    e.RandomVelocity.OverrideEvaluation = (_, _) => 0;
                    e.Scope = new(1, 0, 0);
                }
                edit.Definition.InitSize = 1;
                player.Initialize(resources, 175, 123, edits);
                player.Advance(0.02f);
                player.Render(camera);
                Vector3[] small = ParticlePositions(root);
                edit.Definition.InitSize = 20;
                player.Initialize(resources, 175, 123, edits);
                player.Advance(0.02f);
                player.Render(camera);
                Vector3[] large = ParticlePositions(root);
                Assert.That(large.Length, Is.GreaterThan(0));
                Assert.That(large.Length, Is.EqualTo(small.Length));
                for (int i = 0; i < small.Length; i++)
                    Assert.That(Vector3.Distance(large[i], small[i] * 400), Is.LessThan(0.00001f), "PAL3 scales emitter scope and then particle displacement");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void UnchangedOverridesReuseParticleAndDiagnosticCachesAndEditsInvalidateBoth()
        {
            using var resources = Open();
            var edits = ScriptableObject.CreateInstance<OriginalEffectOverrides>();
            try
            {
                const string path = "effect\\gbParticle\\yiwai3.par";
                var curve = new OriginalParticleCurveOverride
                {
                    File = path,
                    Parameter = "Size",
                    Index = 0,
                    Channels = new[] { AnimationCurve.Constant(0, 1, 17) }
                };
                edits.ParticleCurves.Add(curve);
                var first = resources.Particle(path, edits);
                Assert.That(resources.Particle(path, edits), Is.SameAs(first));
                curve.Channels[0] = AnimationCurve.Constant(0, 1, 19);
                var second = resources.Particle(path, edits);
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.Particles[0].Size.Evaluate(0.5f), Is.EqualTo(19));
                var edit = OverrideObject(resources, 273, 0);
                edits.Objects.Add(edit);
                edit.Definition.TextureName = "effect\\missing-first.tga";
                string[] diagnosis = resources.Diagnose(275, edits);
                Assert.That(diagnosis, Is.Not.Empty);
                Assert.That(resources.Diagnose(275, edits), Is.SameAs(diagnosis));
                edit.Definition.TextureName = "effect\\missing-second.tga";
                Assert.That(resources.Diagnose(275, edits).Any(e => e.Contains("missing-second")), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(edits);
            }
        }

        [Test]
        public void SceneCacheTrimPreservesLiveParticleAssetsAndEvictsUnusedAssets()
        {
            using var resources = Open();
            var root = new GameObject("Live render assets");
            try
            {
                var player = root.AddComponent<OriginalEffectPlayer>();
                player.AutoAdvance = false;
                player.Initialize(resources, 175);
                Material live = root.GetComponentInChildren<ParticleSystemRenderer>().sharedMaterial;
                Texture liveTexture = live.mainTexture;
                Material unused = resources.Material("effect\\q08qiao.tga", true, false);
                Texture unusedTexture = unused.mainTexture;
                resources.TrimUnusedRenderAssets(new[] { root });
                Assert.That(live != null && liveTexture != null, Is.True);
                Assert.That(unused == null && unusedTexture == null, Is.True);
                UnityEngine.Object.DestroyImmediate(root);
                resources.TrimUnusedRenderAssets(Array.Empty<GameObject>());
                Assert.That(live == null && liveTexture == null, Is.True);
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PrivateFallbackUsesActorParentWhileOriginalUsesSceneSnapshot()
        {
            string path = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT") ?? Path.GetFullPath("../Game Data/PAL3");
            if (!Directory.Exists(path))
                Assert.Ignore("PAL3 data is unavailable");
            var crc = new Crc32Hash();
            crc.Init();
            var fs = new CpkFileSystem(path, crc, 936);
            fs.Mount("basedata/basedata.cpk", 936);
            int sleepTimeout = Screen.sleepTimeout;
            using var settings = new GameSettings(new InMemoryKeyValueStore());
            settings.OptionalVfxEnabled = true;
            Type type = typeof(OriginalEffectPlayer).Assembly.GetType("Pal3.Game.Effect.OptionalVfxPlayer");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var facade = (IDisposable)Activator.CreateInstance(type, flags, null, new object[] { settings, fs, 936 }, null);
            var scene = new GameObject("Effect scene");
            var actor = new GameObject("Effect actor");
            var prefab = new GameObject("Synthetic private prefab");
            try
            {
                var prefabs = (Dictionary<int, GameObject>)type.GetField("_prefabs", flags).GetValue(facade);
                prefabs[-2] = prefab;
                var sceneEntity = new GameEntity(scene);
                var actorEntity = new GameEntity(actor);
                bool original = (bool)type.GetMethod("Play", flags).Invoke(facade, new object[] { -2, sceneEntity, new Vector3(10, 0, 0), ((Engine.Core.Abstraction.IGameEntity)actorEntity, Vector3.zero) });
                Assert.That(original, Is.False);
                Transform fallback = actor.transform.GetChild(0);
                actor.transform.position = new Vector3(7, 0, 0);
                Assert.That(fallback.position.x, Is.EqualTo(7));
                original = (bool)type.GetMethod("Play", flags).Invoke(facade, new object[] { 15, sceneEntity, new Vector3(10, 0, 0), ((Engine.Core.Abstraction.IGameEntity)actorEntity, Vector3.zero) });
                Assert.That(original, Is.True);
                Transform native = scene.transform.GetChild(0);
                actor.transform.position = new Vector3(9, 0, 0);
                Assert.That(native.position.x, Is.EqualTo(10));
            }
            finally
            {
                facade.Dispose();
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(scene);
                Screen.sleepTimeout = sleepTimeout;
            }
        }

        [Test]
        public void EditorCoalescesTimelineRequestsBeforeResimulating()
        {
            Type type = TypeCache.GetTypesDerivedFrom<EditorWindow>().First(t => t.FullName == "Editor.OriginalEffectEditor");
            var window = (EditorWindow)ScriptableObject.CreateInstance(type);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var player = (OriginalEffectPlayer)type.GetField("_player", flags).GetValue(window);
                Assert.That(player, Is.Not.Null);
                type.GetMethod("QueueSeek", flags).Invoke(window, new object[] { 1f });
                type.GetMethod("QueueSeek", flags).Invoke(window, new object[] { 2f });
                Assert.That(player.Time, Is.Zero, "Input events only queue the latest timeline request");
                type.GetField("_seekAfter", flags).SetValue(window, 0d);
                type.GetMethod("Tick", flags).Invoke(window, null);
                Assert.That(player.Time, Is.EqualTo(2).Within(0.00001f));
            }
            finally
            {
                window.DiscardChanges();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static OriginalEffectObjectOverride SoloObject(OriginalEffectResources resources, OriginalEffectOverrides edits, int group)
        {
            var edit = OverrideObject(resources, group, 0);
            edits.Objects.Add(edit);
            for (int i = 1; i < resources.Definitions.Groups[group].Objects.Length; i++)
                edits.Objects.Add(new OriginalEffectObjectOverride { GroupId = group, ObjectIndex = i, Hidden = true });
            return edit;
        }

        private static OriginalEffectObjectOverride OverrideObject(OriginalEffectResources resources, int group, int index)
        {
            EffectGroupObject source = resources.Definitions.Groups[group].Objects[index];
            resources.Definitions.TryGetType(source.TypeCrc, out EffectDefinition definition);
            return new OriginalEffectObjectOverride
            {
                GroupId = group,
                ObjectIndex = index,
                Definition = JsonUtility.FromJson<EffectDefinition>(JsonUtility.ToJson(definition)),
                Tasks = source.Tasks.Select(t => JsonUtility.FromJson<EffectTask>(JsonUtility.ToJson(t))).ToArray()
            };
        }

        private static Vector3[] Vertices(GameObject root) => root.GetComponentsInChildren<MeshFilter>().SelectMany(m => m.sharedMesh.vertices).ToArray();

        private static Vector3[] ParticlePositions(GameObject root) => root.GetComponentsInChildren<ParticleSystem>().SelectMany(ps =>
        {
            var particles = new ParticleSystem.Particle[ps.particleCount];
            ps.GetParticles(particles);
            return particles.Select(p => p.position);
        }).ToArray();
    }
}
