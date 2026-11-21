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
    using Core.DataReader.Data;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Game.Effect.Runtime;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleEffectRandomTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type NodeType(string name) => typeof(EffectPlayer).Assembly.GetType("Pal3.Game.Effect.Runtime." + name, true);

        [Test]
        [Category("LocalNativeEvidence")]
        public void ShockSharesOnlyTheOriginalEffectStreamAcrossObjectsAndGroups()
        {
            string directory = Environment.GetEnvironmentVariable("PAL3_EFFECT_RANDOM_VECTORS_ROOT");
            if (directory == null)
                Assert.Ignore("The independently executed original effect vectors are local artifacts.");
            JObject document = JObject.Parse(File.ReadAllText(Path.Combine(directory, "effect-random-native-vectors.json")));
            var hash = new Crc32Hash();
            hash.Init();
            using var resources = new EffectResources(new CpkFileSystem(Application.temporaryCachePath, hash, 936));
            int count = 0;
            foreach (JProperty variant in document.Properties())
            {
                BattleRandom random = null;
                foreach (JToken row in variant.Value["rows"])
                {
                    if ((int)row["repeat"] == 0)
                        random = new BattleRandom((int)row["seed"]);
                    var definition = new EffectDefinition
                    {
                        RotationAngle = (int)row["nodes"],
                        Width = (int)row["cascades"],
                        FrameCount = (int)row["frames"],
                        BottomRadius = .25f
                    };
                    var task = new EffectTask { StartTime = -1, Duration = 1, RequireLoops = 2 };
                    var root = new GameObject("native shock random") { hideFlags = HideFlags.HideAndDontSave };
                    object node = null;
                    try
                    {
                        node = Activator.CreateInstance(NodeType("TrailNode"), Members, null,
                        new object[] { root, definition, new[] { task }, Vector3.zero, 0f, resources, "shock", 99u, true, (Func<int>)random.Next }, null);
                        node.GetType().GetMethod("Step", Members).Invoke(node, new object[] { 0f, 0f });
                        float[] frames = (float[])node.GetType().GetField("_shockFrame", Members).GetValue(node);
                        float[] limits = (float[])node.GetType().GetField("_shockLimit", Members).GetValue(node);
                        Assert.That(frames, Is.EqualTo(row["startedFrames"].Select(value => (float)value).ToArray()));
                        Assert.That(limits, Is.EqualTo(row["limits"].Select(value => (float)value).ToArray()));
                        Assert.That(random.State, Is.EqualTo((uint)row["effectState"]));
                        Assert.That((uint)row["domainState"], Is.EqualTo(0x12345678u));
                        count++;
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
            }
            Assert.That(count, Is.EqualTo(144));
        }

        [Test]
        [Category("LocalNativeEvidence")]
        public void ExplosiveMatchesTheReleasedEffectStreamWithoutChangingCombatRandom()
        {
            string directory = Environment.GetEnvironmentVariable("PAL3_EFFECT_RANDOM_VECTORS_ROOT");
            if (directory == null)
                Assert.Ignore("The independently executed original effect vectors are local artifacts.");
            JObject document = JObject.Parse(File.ReadAllText(Path.Combine(directory, "effect-random-native-vectors.json")));
            var hash = new Crc32Hash();
            hash.Init();
            using var resources = new EffectResources(new CpkFileSystem(Application.temporaryCachePath, hash, 936));
            int count = 0;
            foreach (JProperty variant in document.Properties())
                foreach (JToken row in variant.Value["explosiveRows"])
                {
                    var random = new BattleRandom((int)row["seed"]);
                    var definition = new EffectDefinition { RotationAngle = (int)row["columns"], DivisionCount = (int)row["rows"], TopRadius = (int)row["power"] };
                    var root = new GameObject("native explosive random") { hideFlags = HideFlags.HideAndDontSave };
                    object node = null;
                    try
                    {
                        node = Activator.CreateInstance(NodeType("ExplosiveNode"), Members, null,
                        new object[] { root, definition, Array.Empty<EffectTask>(), Vector3.zero, 0f, resources, 999u, true, (Func<int>)random.Next }, null);
                        node.GetType().GetMethod("Explode", Members).Invoke(node, null);
                        Vector3[] velocity = (Vector3[])node.GetType().GetField("_velocity", Members).GetValue(node);
                        Vector3[] spin = (Vector3[])node.GetType().GetField("_spin", Members).GetValue(node);
                        Vector3 Read(JToken value) => new((float)value[0], (float)value[1], (float)value[2]);
                        Assert.That(velocity, Is.EqualTo(row["velocity"].Select(Read).ToArray()));
                        Assert.That(spin, Is.EqualTo(row["spin"].Select(Read).ToArray()));
                        Assert.That(random.State, Is.EqualTo((uint)row["effectState"]));
                        Assert.That((uint)row["domainState"], Is.EqualTo(0x12345678u));
                        count++;
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
            Assert.That(count, Is.EqualTo(48));
        }

        [Test]
        [Category("LocalGameData")]
        public void RealShockGroupsRetainOneVisualStreamWithoutChangingCombatRandom()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (root == null)
                Assert.Ignore("Original game data is required for the real-group random regression.");
            using var data = new BattleData(root, 936);
            using var preview = new ResourceModelPreview(data.Resources, false);
            using var director = new BattleEffectDirector(data, preview);
            var resources = (EffectResources)typeof(BattleEffectDirector).GetField("_resources", Members).GetValue(director);
            var source = resources.Definitions;
            int group = Enumerable.Range(0, source.Groups.Length).First(id => source.Groups[id].Objects.Any(obj =>
                source.TryGetType(obj.TypeCrc, out EffectDefinition definition) && EffectClass.Name(definition.ClassNameCrc) == "shock"));
            var scenario = BattleSessionTests.Scenario();
            scenario.Seed = 1;
            var combat = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), effects: director);
            var combatRandom = (BattleRandom)typeof(BattleSession).GetField("_random", Members).GetValue(combat);
            uint before = combatRandom.State;
            var samples = new List<float[]>();
            uint finalState = 0;
            for (int repeat = 0; repeat < 2; repeat++)
            {
                long handle = director.Start(new BattleEffectRequest(group, "Ally", new[] { "Enemy" }, default, 1));
                var players = (IDictionary)typeof(BattleEffectDirector).GetField("_players", Members).GetValue(director);
                object instance = players[handle];
                var player = (EffectPlayer)instance.GetType().GetField("Player", Members).GetValue(instance);
                var nodes = (IList)typeof(EffectPlayer).GetField("_nodes", Members).GetValue(player);
                object shock = nodes.Cast<object>().First(node => node.GetType() == NodeType("TrailNode") &&
                    (string)node.GetType().GetField("_kind", Members).GetValue(node) == "shock");
                for (int step = 1; step <= 100; step++)
                {
                    player.AdvanceTo(step * BattleSession.StepSeconds);
                    var current = ((float[])shock.GetType().GetField("_shockFrame", Members).GetValue(shock)).ToArray();
                    var random = (BattleRandom)typeof(BattleEffectDirector).GetField("_effectRandom", Members).GetValue(director);
                    if (random.State != (repeat == 0 ? 1u : finalState))
                    {
                        samples.Add(current);
                        finalState = random.State;
                        break;
                    }
                }
                director.Stop(handle);
            }
            Assert.That(samples.Count, Is.EqualTo(2));
            Assert.That(samples[1], Is.Not.EqualTo(samples[0]), "A second original group must continue the GT util random stream.");
            Assert.That(combatRandom.State, Is.EqualTo(before));
            string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR");
            if (output != null)
                File.WriteAllText(Path.Combine(output, BattleData.Variant + "-real-effect-random.json"), JsonConvert.SerializeObject(new
                {
                    group,
                    samples,
                    finalState,
                    combatRandom = before
                }, Formatting.Indented));
        }
    }
}
