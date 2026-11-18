// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Battle;
    using Core.DataReader.Mv3;
    using Core.Primitives;
    using NUnit.Framework;

    public sealed class BattleAnimationMetadataTests
    {
        [Test]
        public void MetadataOwnsItsMarkersAndAggregatesTheOriginalMeshBounds()
        {
            var events = new[] { new Mv3AnimationEvent { Name = "work", GameBoxTick = 123 } };
            var meshes = new[]
            {
                new Mv3Mesh { GameBoxBoundsMin = new GameBoxVector3 { Z = -4 }, GameBoxBoundsMax = new GameBoxVector3 { Z = 3 } },
                new Mv3Mesh { GameBoxBoundsMin = new GameBoxVector3 { Z = -9 }, GameBoxBoundsMax = new GameBoxVector3 { Z = 13 } }
            };
            var source = new Mv3File(600, events, Array.Empty<Mv3TagNode>(), meshes, Array.Empty<GameBoxMaterial>());
            var metadata = new BattleAnimationMetadata(source);
            Assert.That(metadata.TotalGameBoxTicks, Is.EqualTo(600));
            Assert.That(metadata.HasBounds, Is.True);
            Assert.That((metadata.MinimumZ, metadata.MaximumZ), Is.EqualTo((-9f, 13f)));
            events[0].Name = "changed";
            meshes[1] = default;
            Assert.That(metadata.AnimationEvents[0].Name, Is.EqualTo("work"));
            Assert.That(metadata.AnimationEvents[0].GameBoxTick, Is.EqualTo(123));
            Assert.That((metadata.MinimumZ, metadata.MaximumZ), Is.EqualTo((-9f, 13f)));
            Assert.Throws<NotSupportedException>(() => ((IList<Mv3AnimationEvent>)metadata.AnimationEvents)[0] = default);
        }

        [Test]
        public void MissingBoundsRemainExplicitInsteadOfBecomingAZeroRadius()
        {
            var source = new Mv3File(0, Array.Empty<Mv3AnimationEvent>(), Array.Empty<Mv3TagNode>(),
                Array.Empty<Mv3Mesh>(), Array.Empty<GameBoxMaterial>());
            Assert.That(new BattleAnimationMetadata(source).HasBounds, Is.False);
        }

        [Test, Category("LocalGameData")]
        public void CanonicalPathsShareMetadataOnlyWithinTheirLiveDataOwner()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original local data unavailable.");
            using var first = new BattleData(root, 936);
            using var second = new BattleData(root, 936);
            BattleParticipant actor = first.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            string idle = first.ActionPath(actor, first.IdleAction(actor));
            Assert.That(first.TryActionPath(actor, "z01", out string attack), Is.True);
            BattleAnimationMetadata metadata = first.GetAnimationMetadata(idle);
            Assert.That(first.GetAnimationMetadata(idle.ToUpperInvariant().Replace('\\', '/')), Is.SameAs(metadata));
            Assert.That(first.GetAnimationMetadata(attack), Is.Not.SameAs(metadata), "Different animation paths must not alias.");
            BattleAnimationMetadata separate = second.GetAnimationMetadata(idle);
            Assert.That(separate, Is.Not.SameAs(metadata), "A new data owner must not inherit cached original data.");
            Mv3File original = first.Read(idle, new Mv3FileReader());
            Assert.That(metadata.TotalGameBoxTicks, Is.EqualTo(original.TotalGameBoxTicks));
            Assert.That(metadata.AnimationEvents.Select(marker => (marker.GameBoxTick, marker.Name)),
                Is.EqualTo(original.AnimationEvents.Select(marker => (marker.GameBoxTick, marker.Name))));
            Assert.That(metadata.MinimumZ, Is.EqualTo(original.Meshes.Min(mesh => mesh.GameBoxBoundsMin.Z)));
            Assert.That(metadata.MaximumZ, Is.EqualTo(original.Meshes.Max(mesh => mesh.GameBoxBoundsMax.Z)));
            first.Dispose();
            Assert.Throws<ObjectDisposedException>(() => first.GetAnimationMetadata(idle));
            Assert.That(second.GetAnimationMetadata(idle), Is.SameAs(separate));
        }
    }
}
