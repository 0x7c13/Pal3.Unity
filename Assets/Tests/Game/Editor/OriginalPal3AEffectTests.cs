// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Core.DataReader.Data;
    using Effect.Original;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class OriginalPal3AEffectTests
    {
        public static readonly int[] NativeGroups = { 9, 27, 29, 34, 40, 50, 91, 157, 160, 172, 173, 175, 176, 177, 178, 179, 181, 182, 186, 187, 189, 194, 198, 201, 202, 205, 206, 209, 217, 230, 231, 254, 257, 258, 259, 261, 281, 282, 283, 285, 287, 290, 292, 297, 299, 314, 316, 320, 322, 330, 331, 333, 335, 351, 355, 356, 359, 362, 365, 373, 377, 381, 385, 388, 389, 400, 424 };
        public static readonly int[] SparGroups = { 45, 164, 170, 171, 174, 211, 289, 315, 318, 323, 325, 328, 344, 345, 358, 360, 361, 367, 397, 401, 404, 407, 408, 409, 411, 570 };

        private static OriginalEffectResources Open()
        {
            string path = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT") ?? Path.GetFullPath("../Game Data/PAL3A");
            if (!Directory.Exists(path))
                Assert.Ignore("Set PAL3A_TEST_DATA_ROOT to the local PAL3A data directory");
            var crc = new Crc32Hash();
            crc.Init();
            var fs = new CpkFileSystem(path, crc, 936);
            fs.Mount("basedata/basedata.cpk", 936);
            return new OriginalEffectResources(fs);
        }

        [Test]
        public void AllNonSparScriptAndSceneGroupsSimulateWithOriginalData()
        {
            using var resources = Open();
            Assert.That(resources.GameVariant, Is.EqualTo("PAL3A"));
            Assert.That(resources.OverridesPath, Is.EqualTo("Effects/PAL3AOverrides"));
            var errors = new List<string>();
            foreach (int id in NativeGroups)
            {
                var root = new GameObject($"PAL3A effect {id}");
                try
                {
                    Assert.That(resources.Diagnose(id), Is.Empty, $"Group {id}");
                    var player = root.AddComponent<OriginalEffectPlayer>();
                    player.AutoAdvance = false;
                    player.Initialize(resources, id, 1234);
                    bool rendered = false;
                    for (int i = 0; i < 650; i++)
                    {
                        player.Advance(0.02f);
                        if (i % 10 == 0)
                        {
                            player.Render(null);
                            rendered |= player.QuadCount > 0 || root.GetComponentsInChildren<Renderer>().Any(r => r.enabled);
                        }
                        Assert.That(player.Error, Is.Null, $"Group {id}");
                    }
                    Assert.That(rendered, Is.True, $"Group {id}: no geometry");
                    player.Seek(1);
                    player.Render(null);
                    Assert.That(player.Error, Is.Null);
                }
                catch (Exception ex)
                {
                    errors.Add($"{id}: {ex.Message}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void SparGroupsAreParsedAndExplicitlyDiagnosedForFallback()
        {
            using var resources = Open();
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (int id in SparGroups)
            {
                string[] errors = resources.Diagnose(id);
                Assert.That(errors.Any(e => e.Contains("SPar parsed")), Is.True, $"Group {id}: {string.Join("; ", errors)}");
                Assert.That(errors.All(e => e.Contains("native simulation/rendering is pending")), Is.True, $"Group {id}: unexpected dependency failure");
                foreach (EffectGroupObject obj in resources.Definitions.Groups[id].Objects)
                    if (resources.Definitions.TryGetType(obj.TypeCrc, out var d) && EffectClass.Name(d.ClassNameCrc) == "S-Particle")
                    {
                        var spar = resources.Spar(d.ParticleFileName);
                        files.Add(d.ParticleFileName);
                        Assert.That(spar.Groups.All(g => g.Keyframes.Length > 0), Is.True);
                    }
            }
            Assert.That(files.Count, Is.EqualTo(28));
        }
    }
}
