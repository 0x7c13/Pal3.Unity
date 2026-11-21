// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.PlayMode.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using Core.DataReader.Cpk;
    using Core.DataReader.Data;
    using Core.FileSystem;
    using Effect.Runtime;
    using NUnit.Framework;
    using Rendering.Native;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class EffectLifetimeTests
    {
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

            public void SetExternalTask(bool controlled, object owner)
            {
                Controlled = controlled;
                if (!controlled)
                    Releases++;
            }

            public void SetAction(string action)
            {
            }

            public void SetLongBlade(bool enabled)
            {
            }

            public void SetWeapons(float left, float right)
            {
            }
        }

        private sealed class Context : IBattleEffectContext
        {
            public readonly Actor Role = new();

            public IEffectCamera Camera => null;
            public bool IsModalPresentationRunning => false;

            public int NextEffectRandom() => 1;

            public IEffectActor Actor(int nativeIndex, uint modelCrc = 0) => Role;

            public IEffectMovie PlayMovie(string originalPath) => throw new NotSupportedException();
        }

        [UnityTest]
        [Category("LocalGameData")]
        public IEnumerator RuntimeDestructionFallsBackToDisposingLiveActionAndOriginalModel()
        {
#if PAL3
            const string variant = "PAL3";
#else
            const string variant = "PAL3A";
#endif
            string directory = Environment.GetEnvironmentVariable(variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(directory))
                Assert.Ignore("Original game data is required for the runtime destruction regression.");
            var hash = new Crc32Hash();
            hash.Init();
            var fileSystem = new CpkFileSystem(directory, hash, 936);
            string archive = Directory.EnumerateFiles(Path.Combine(directory, "basedata")).Single(file =>
                Path.GetFileName(file).Equals("basedata.cpk", StringComparison.OrdinalIgnoreCase));
            fileSystem.Mount(Path.GetRelativePath(directory, archive), 936);
            using var resources = new EffectResources(fileSystem, 936);
            using var lighting = new GameBoxNativeLighting(variant);
            EffectDefinitionFile definitions = resources.Definitions;
            EffectDefinition action = definitions.EffectDefinitions.First(definition => definition.IsValid && EffectClass.Name(definition.ClassNameCrc) == "action");
            EffectDefinition model = definitions.EffectDefinitions.First(definition => definition.IsValid && EffectClass.Name(definition.ClassNameCrc) == "model" && definition.TextureName.EndsWith(".pol", StringComparison.OrdinalIgnoreCase));
            definitions.Groups[0] = new EffectGroup
            {
                Objects = new[]
                {
                    new EffectGroupObject { TypeCrc = action.TypeNameCrc, Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10, XStretchSpeed = 17 } } },
                    new EffectGroupObject { TypeCrc = model.TypeNameCrc, Tasks = new[] { new EffectTask { IsValid = true, StartTime = -1, Duration = 10 } } }
                }
            };
            var root = new GameObject("Runtime effect cleanup");
            var player = root.AddComponent<EffectPlayer>();
            var context = new Context();
            player.AutoAdvance = false;
            try
            {
                player.Initialize(resources, 0, battleContext: context, nativeLighting: lighting);
                player.AdvanceFrame(.02f);
                Assert.That(player.Error, Is.Null);
                Assert.That(context.Role.Controlled, Is.True);
                Mesh[] meshes = root.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
                Material[] materials = root.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials).ToArray();
                Assert.That(meshes, Is.Not.Empty);
                yield return null;
                // Exercise Unity's real runtime callback; no explicit owner Dispose.
                Object.Destroy(root);
                yield return null;
                yield return null;
                Assert.That(root == null, Is.True);
                Assert.That(player.NodeCount, Is.Zero);
                Assert.That(player.IsAlive, Is.False);
                Assert.That(context.Role.Controlled, Is.False);
                Assert.That(context.Role.Releases, Is.EqualTo(1));
                Assert.That(meshes.All(mesh => mesh == null), Is.True);
                Assert.That(materials.All(material => material == null), Is.True);
                player.Dispose();
                Assert.That(context.Role.Releases, Is.EqualTo(1));
            }
            finally
            {
                player.Dispose();
                if (root != null)
                    Object.Destroy(root);
            }
        }
    }
}
