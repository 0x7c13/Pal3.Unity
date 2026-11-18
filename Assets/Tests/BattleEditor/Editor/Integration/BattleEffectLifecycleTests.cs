// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Core.DataReader.Cpk;
    using Core.DataReader.Data;
    using Core.FileSystem;
    using Engine.Core.Abstraction;
    using Game.Effect.Runtime;
    using Game.Rendering.Material;
    using Game.Rendering.Native;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // These ownership tests use isolated preview scenes and never create a window.
    public sealed class BattleEffectLifecycleTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class Actor : IEffectActor
        {
            public Vector3 Position
            {
                get; set;
            }

            public float FacingDegrees
            {
                get; set;
            }

            public float Blend { get; set; } = 1;
            public bool ActionOver => false;

            public bool Controlled;
            public int Releases;
            public string Action;

            public void SetExternalTask(bool controlled, object owner)
            {
                Controlled = controlled;
                if (!controlled)
                    Releases++;
            }

            public void SetAction(string action) => Action = action;

            public void SetLongBlade(bool enabled)
            {
            }

            public void SetWeapons(float left, float right)
            {
            }
        }

        private sealed class Film : IEffectMovie
        {
            public bool Finished
            {
                get; set;
            }

            public int Disposals;

            public void Dispose() => Disposals++;
        }

        private sealed class TestCamera : IEffectCamera
        {
            public int List
            {
                get; private set;
            }

            public void StopList() => List = 0;

            public bool StartList(int index)
            {
                List = index;
                return true;
            }

            public void SetQuake(float amplitude, float frequency, float decay)
            {
            }
        }

        private sealed class Context : IBattleEffectContext, IEffectTimeControl
        {
            public readonly Actor Role = new();
            public readonly List<Film> Films = new();
            public readonly Dictionary<object, float> Scales = new();

            public void SetTimeScale(object owner, float scale) => Scales.Add(owner, scale);

            public void ClearTimeScale(object owner) => Scales.Remove(owner);

            public IEffectCamera Camera { get; } = new TestCamera();
            public bool IsModalPresentationRunning => Films.Any(film => !film.Finished && film.Disposals == 0);

            public int NextEffectRandom() => 1;

            public IEffectActor Actor(int nativeIndex, uint modelCrc = 0) => Role;

            public IEffectMovie PlayMovie(string originalPath)
            {
                var film = new Film();
                Films.Add(film);
                return film;
            }
        }

        private static EffectResources ControlResources(bool movies)
        {
            var hash = new Crc32Hash();
            hash.Init();
            var resources = new EffectResources(new CpkFileSystem(Application.temporaryCachePath, hash, 936));
            var definitions = new EffectDefinition[1324];
            definitions[0] = new EffectDefinition { IsValid = true, TypeNameCrc = 1, ClassNameCrc = hash.Compute("action", 936) };
            definitions[1] = new EffectDefinition { IsValid = true, TypeNameCrc = 2, ClassNameCrc = hash.Compute("bink", 936), VideoFileNamePrefix = "review" };
            var objects = new List<EffectGroupObject>();
            if (movies)
                objects.Add(new EffectGroupObject
                {
                    TypeCrc = 2,
                    Tasks = new[]
                    {
                        new EffectTask { IsValid = true, XStretchSpeed = 1, StartTime = -1, Duration = 10 },
                        new EffectTask { IsValid = true, XStretchSpeed = 2, StartTime = -1, Duration = 10 },
                    }
                });
            objects.Add(ActionObject(definitions[0]));
            typeof(EffectResources).GetField("_definitions", Members).SetValue(resources,
                new EffectDefinitionFile(definitions, new[] { new EffectGroup { Objects = objects.ToArray() } }));
            return resources;
        }

        private static EffectGroupObject ActionObject(EffectDefinition definition) => new()
        {
            TypeCrc = definition.TypeNameCrc,
            Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10, XStretchSpeed = 17, SpeedX = 10 } }
        };

        [TestCase("absolute")]
        [TestCase("dispose")]
        [TestCase("delta")]
        [TestCase("frame")]
        [TestCase("seek")]
        [TestCase("manager")]
        [TestCase("mixed")]
        [TestCase("return to external clock")]
        [TestCase("external clock has caught up")]
        [TestCase("external clock catches up within float precision")]
        public void BattleTimeTaskReleasesItsScaleOnEveryClockPath(string clock)
        {
            using var resources = ControlResources(false);
            var hash = new Crc32Hash();
            hash.Init();
            resources.Definitions.EffectDefinitions[2] = new EffectDefinition
            {
                IsValid = true,
                TypeNameCrc = 3,
                ClassNameCrc = hash.Compute("time", 936)
            };
            typeof(EffectResources).GetField("_definitions", Members).SetValue(resources, new EffectDefinitionFile(
                resources.Definitions.EffectDefinitions, new[]
                {
                    new EffectGroup
                    {
                        Objects = new[]
                        {
                            new EffectGroupObject
                            {
                                TypeCrc = 3,
                                Tasks = new[] { new EffectTask
                                {
                                    IsValid = true, StartTime = -1, Duration = 2, XStretchSpeed = 0, CreateNew = true, ModifyLast = true,
                                    SpeedRot = 5 // The rotation union slot is unused by the time task.
                                } }
                            }
                        }
                    }
                }));
            Assert.That(resources.Diagnose(0, battleContext: true), Is.Empty);
            Assert.That(resources.Preload(0, battleContext: true).Distinct(), Is.EqualTo(new[] { 0 }));

            var root = new GameObject("Battle time effect");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            player.AutoAdvance = false;
            try
            {
                player.Initialize(resources, 0, battleContext: context);
                switch (clock)
                {
                    case "absolute":
                    case "dispose":
                        player.AdvanceTo(.25, 1);
                        Assert.That(context.Scales.Values, Is.EqualTo(new[] { 0f }));
                        if (clock == "dispose")
                            player.Dispose();
                        else
                        {
                            player.AdvanceTo(.25, 2.9);
                            Assert.That(context.Scales, Has.Count.EqualTo(1));
                            player.AdvanceTo(.25, 3);
                        }
                        break;
                    case "delta":
                        player.Advance(3);
                        break;
                    case "frame":
                        player.AdvanceFrame(.25f);
                        player.AdvanceFrame(2f);
                        break;
                    case "seek":
                        player.Seek(3);
                        break;
                    case "manager":
                        player.AdvanceTo(.25);
                        player.AdvanceTo(2.25);
                        break;
                    case "mixed":
                        player.AdvanceTo(.25);
                        player.AdvanceTo(.5, 100.5);
                        Assert.That(context.Scales, Has.Count.EqualTo(1));
                        player.AdvanceTo(.75);
                        player.AdvanceFrame(.25f);
                        player.AdvanceTo(1, 102.25);
                        break;
                    case "return to external clock":
                        player.AdvanceTo(.25, 1);
                        player.AdvanceFrame(.5f);
                        player.AdvanceTo(1, 1.125);
                        Assert.That(context.Scales, Has.Count.EqualTo(1));
                        player.AdvanceTo(1, 2.375);
                        break;
                    case "external clock has caught up":
                        player.AdvanceTo(.25, 1);
                        player.AdvanceFrame(.5f);
                        player.AdvanceTo(2.25, 1.75);
                        Assert.That(context.Scales, Has.Count.EqualTo(1));
                        player.AdvanceTo(2.25, 3);
                        break;
                    case "external clock catches up within float precision":
                        player.AdvanceTo(.25, 1);
                        player.AdvanceFrame(.1f);
                        player.AdvanceTo(2.5, 1.1);
                        Assert.That(context.Scales, Has.Count.EqualTo(1));
                        player.AdvanceTo(2.5, 3.1);
                        break;
                }

                Assert.That(context.Scales, Is.Empty);
                Assert.That(player.IsAlive, Is.False);
                Assert.That(player.Error, Is.Null);
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExplicitDisposalReleasesLiveActionOnceAndRejectsReinitialization()
        {
            using var resources = ControlResources(false);
            var root = new GameObject("Action disposal");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            player.AutoAdvance = false;
            try
            {
                player.Initialize(resources, 0, battleContext: context);
                player.AdvanceFrame(.02f);
                Assert.That(context.Role.Controlled, Is.True);
                player.Dispose();
                player.Dispose();
                Assert.That(context.Role.Controlled, Is.False);
                Assert.That(context.Role.Releases, Is.EqualTo(1));
                Assert.That(player.NodeCount, Is.Zero);
                Assert.That(player.IsAlive, Is.False);
                float time = player.Time;
                player.AdvanceFrame(1);
                player.AdvanceTo(1);
                Assert.That(player.ResumeBattleStep(), Is.True);
                Assert.That(player.Time, Is.EqualTo(time));
                Assert.Throws<ObjectDisposedException>(() => player.Initialize(resources, 0));
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false, 0)]
        [TestCase(true, 2)]
        public void DisposingACameraEffectStopsOnlyItsStillRunningTask(bool expired, int expectedList)
        {
            using var resources = ControlResources(false);
            var hash = new Crc32Hash();
            hash.Init();
            var definitions = resources.Definitions.EffectDefinitions;
            definitions[2] = new EffectDefinition { IsValid = true, TypeNameCrc = 3, ClassNameCrc = hash.Compute("camera", 936) };
            typeof(EffectResources).GetField("_definitions", Members).SetValue(resources, new EffectDefinitionFile(definitions, new[]
            {
                new EffectGroup
                {
                    Objects = new[]
                    {
                        new EffectGroupObject { TypeCrc = 3, Tasks = new[]
                        {
                            new EffectTask { IsValid = true, StartTime = -1, Duration = .1f, XStretchSpeed = 1 },
                            new EffectTask { IsValid = true, StartTime = 1, Duration = .1f }
                        } }
                    }
                }
            }));
            var root = new GameObject("Camera effect owner");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            var camera = (TestCamera)context.Camera;
            try
            {
                player.Initialize(resources, 0, battleContext: context);
                player.AdvanceTo(.02);
                Assert.That(camera.List, Is.EqualTo(1));
                if (expired)
                {
                    player.AdvanceTo(.25);
                    Assert.That(player.IsAlive, Is.True);
                    camera.StartList(2);
                }
                player.Dispose();
                Assert.That(camera.List, Is.EqualTo(expectedList));
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SeekingABattleEffectNeverStartsMovieOrControlsAnActor()
        {
            using var resources = ControlResources(true);
            var root = new GameObject("Modal seek");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            try
            {
                player.Initialize(resources, 0, battleContext: context);
                player.Seek(.4f);
                Assert.That(player.Time, Is.EqualTo(.4f).Within(.001f));
                Assert.That(context.Films, Is.Empty);
                Assert.That(context.Role.Action, Is.Null);
                Assert.That(context.Role.Controlled, Is.False);
                Assert.That(player.ResumeBattleStep(), Is.True);
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResumingAfterASeekDoesNotApplyAnUninitializedActorAction()
        {
            using var resources = ControlResources(false);
            var root = new GameObject("Action seek");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            try
            {
                EffectTask moving = resources.Definitions.Groups[0].Objects[0].Tasks[0];
                moving.SpeedBlue = 1;
                moving.RotAxisX = 10;
                moving.Duration = 1;
                context.Role.Position = new Vector3(40, 0, 0);
                player.Initialize(resources, 0, battleContext: context);
                player.Seek(.4f);
                player.AdvanceFrame(.02f);
                Assert.That(context.Role.Position.x, Is.EqualTo(40));
                Assert.That(context.Role.Controlled, Is.False);
                Assert.That(context.Role.Action, Is.Null);
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConsecutiveModalTasksResumeAtOneTimeAndDisposeTheirRemainingOwner(bool cancelDuringMovie)
        {
            using var resources = ControlResources(true);
            var root = new GameObject("Modal cancellation");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            player.AutoAdvance = false;
            try
            {
                player.Initialize(resources, 0, battleContext: context, clockOrigin: 1024);
                player.AdvanceTo(1024.02);
                float time = player.Time;
                Assert.That(context.Films.Count, Is.EqualTo(1));
                Assert.That(context.Role.Action, Is.Null);
                player.AdvanceTo(1024.2);
                player.Advance(.2f);
                player.AdvanceFrame(.2f);
                Assert.That(player.Time, Is.EqualTo(time));
                context.Films[0].Finished = true;
                Assert.That(player.ResumeBattleStep(), Is.False);
                Assert.That(context.Films.Count, Is.EqualTo(2));
                Assert.That(context.Role.Action, Is.Null);
                if (!cancelDuringMovie)
                {
                    context.Films[1].Finished = true;
                    Assert.That(player.ResumeBattleStep(), Is.True);
                    Assert.That(context.Role.Action, Is.EqualTo("z17"));
                    Assert.That(context.Role.Position.x, Is.EqualTo((time - 1024) * 10).Within(.000001f));
                    Assert.That(context.Role.Controlled, Is.True);
                }
                Assert.That(player.Time, Is.EqualTo(time));
                player.Dispose();
                player.Dispose();
                Assert.That(context.Role.Controlled, Is.False);
                Assert.That(context.Role.Releases, Is.EqualTo(cancelDuringMovie ? 0 : 1));
                Assert.That(context.Films.All(film => film.Disposals == 1), Is.True);
                Assert.That(player.ResumeBattleStep(), Is.True);
                Assert.That(player.NodeCount, Is.Zero);
            }
            finally
            {
                player.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private static BattleData LocalData()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required for model ownership regression.");
            return new BattleData(root, 936);
        }

        private static EffectResources DirectorResources(BattleEffectDirector director) =>
            (EffectResources)typeof(BattleEffectDirector).GetField("_resources", Members).GetValue(director);

        private static void ModelGroup(EffectResources resources, string extension)
        {
            EffectDefinitionFile definitions = resources.Definitions;
            EffectDefinition model = definitions.EffectDefinitions.First(definition => definition.IsValid &&
                EffectClass.Name(definition.ClassNameCrc) == "model" && definition.TextureName.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase));
            EffectDefinition action = definitions.EffectDefinitions.First(definition => definition.IsValid && EffectClass.Name(definition.ClassNameCrc) == "action");
            definitions.Groups[0] = new EffectGroup
            {
                Objects = new[]
                {
                    ActionObject(action),
                    new EffectGroupObject { TypeCrc = model.TypeNameCrc, Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10, SpeedAlpha = -.5f } } }
                }
            };
        }

        private static int LightBindings(GameBoxNativeLighting lighting) =>
            ((ICollection)typeof(GameBoxNativeLighting).GetField("_bindings", Members).GetValue(lighting)).Count;

        private static Material[] OwnedMaterials(EffectPlayer player)
        {
            var materials = new HashSet<Material>(player.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials));
            foreach (GameBoxBattleRenderPhase phase in player.GetComponentsInChildren<GameBoxBattleRenderPhase>())
                materials.UnionWith(phase.SourceMaterials);
            foreach (object node in (IEnumerable)typeof(EffectPlayer).GetField("_nodes", Members).GetValue(player))
            {
                if (node.GetType().GetField("_materials", Members)?.GetValue(node) is not GameMaterialManager manager)
                    continue;
                foreach (FieldInfo field in typeof(GameMaterialManager).GetFields(Members))
                    if (field.GetValue(manager) is IMaterial material)
                        materials.Add((Material)material.NativeObject);
            }
            return materials.Where(material => material != null).ToArray();
        }

        [TestCase("POL")]
        [TestCase("CVD")]
        [Category("LocalGameData")]
        public void DirectorStopReleasesLiveModelMeshesMaterialsLightingAndActorControl(string extension)
        {
            using var data = LocalData();
            using var lighting = new GameBoxNativeLighting(BattleData.Variant);
            using var preview = new ResourceModelPreview(data.Resources, false, lighting);
            using var director = new BattleEffectDirector(data, preview);
            EffectResources resources = DirectorResources(director);
            ModelGroup(resources, extension);
            long handle = director.Start(new BattleEffectRequest(0, "Ally", new[] { "Enemy" }, default, 1));
            EffectPlayer player = preview.Root.GetComponentInChildren<EffectPlayer>();
            var context = new Context();
            player.Initialize(resources, 0, battleContext: context, nativeLighting: lighting);
            player.AdvanceFrame(.02f);
            player.Render(null);
            Assert.That(player.Error, Is.Null);
            Assert.That(context.Role.Controlled, Is.True);
            Mesh[] meshes = player.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
            Material[] materials = OwnedMaterials(player);
            Assert.That(meshes, Is.Not.Empty);
            Assert.That(materials.Length, Is.GreaterThan(4));
            Assert.That(LightBindings(lighting), Is.GreaterThan(0));
            director.Stop(handle);
            director.Stop(handle);
            player.Dispose();
            Assert.That(player.NodeCount, Is.Zero);
            Assert.That(context.Role.Controlled, Is.False);
            Assert.That(context.Role.Releases, Is.EqualTo(1));
            Assert.That(meshes.All(mesh => mesh == null), Is.True);
            Assert.That(materials.All(material => material == null), Is.True);
            Assert.That(LightBindings(lighting), Is.Zero);
            Assert.That(director.IsRunning(handle), Is.False);
        }

        [TestCase("POL")]
        [TestCase("CVD")]
        [Category("LocalGameData")]
        public void FailedStartAfterModelCreationReleasesItsPrivateResources(string extension)
        {
            using var data = LocalData();
            using var lighting = new GameBoxNativeLighting(BattleData.Variant);
            using var preview = new ResourceModelPreview(data.Resources, false, lighting);
            using var director = new BattleEffectDirector(data, preview);
            ModelGroup(DirectorResources(director), extension);
            director.Preload(0);
            var materials = new HashSet<Material>(Resources.FindObjectsOfTypeAll<Material>());
            var meshes = new HashSet<Mesh>(Resources.FindObjectsOfTypeAll<Mesh>());
            typeof(BattleEffectDirector).GetField("_nextHandle", Members).SetValue(director, long.MaxValue);
            Assert.Throws<OverflowException>(() => director.Start(new BattleEffectRequest(0, "Ally", new[] { "Enemy" }, default, 1)));
            Assert.That(preview.Root.GetComponentsInChildren<EffectPlayer>(), Is.Empty);
            Assert.That(Resources.FindObjectsOfTypeAll<Material>().Except(materials), Is.Empty);
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Except(meshes), Is.Empty);
            Assert.That(LightBindings(lighting), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompositePreviewDisposesUnregisteredEffectPlayersBeforeDestroyingTheirObjects(bool wholePreview)
        {
            using var resources = ControlResources(false);
            using var preview = new ResourceModelPreview(null, false);
            GameObject root = preview.CreateObject("Composite effect");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            player.AutoAdvance = false;
            player.Initialize(resources, 0, battleContext: context);
            player.AdvanceFrame(.02f);
            Assert.That(context.Role.Controlled, Is.True);
            if (wholePreview)
                preview.Dispose();
            else
                preview.ReleaseCompositeObject(root);
            Assert.That(context.Role.Controlled, Is.False);
            Assert.That(context.Role.Releases, Is.EqualTo(1));
            Assert.That(player.NodeCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("LocalGameData")]
        public void CompositePreviewReleasesPortalMeshesWhileKeepingSharedMaterialsBorrowed(bool wholePreview)
        {
            using var data = LocalData();
            using var resources = new EffectResources(data.Resources.FileSystem, 936);
            using var preview = new ResourceModelPreview(data.Resources, false);
            GameObject root = preview.CreateObject("Composite portal");
            var portal = root.AddComponent<PortalEffectPlayer>();
            portal.AutoAdvance = false;
            portal.Initialize(resources);
            Mesh[] meshes = root.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
            Material[] materials = root.GetComponentsInChildren<Renderer>().Select(renderer => renderer.sharedMaterial).ToArray();
            Assert.That(meshes.Length, Is.EqualTo(3));
            if (wholePreview)
                preview.Dispose();
            else
                preview.ReleaseCompositeObject(root);
            portal.Dispose();
            Assert.That(meshes.All(mesh => mesh == null), Is.True);
            Assert.That(materials.All(material => material != null), Is.True);
        }
    }
}
