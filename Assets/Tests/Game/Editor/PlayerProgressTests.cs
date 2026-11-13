// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command.Extensions;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;
    using GameSystems.Team;
    using NUnit.Framework;

    public sealed class PlayerProgressTests
    {
        [Test]
        public void DefaultWeaponSkipsMatchingModelsThatTheActorCannotEquip()
        {
            var actor = (PlayerActorId)0;
            var items = new Dictionary<int, GameItemInfo>
            {
                [1] = Weapon((PlayerActorId)1, (WeaponType)1),
                [5] = Weapon(actor, (WeaponType)2)
            };
            using var progress = Create(items);
            Assert.That(progress.GetWeaponType(0), Is.EqualTo(2));
            // The selected default must also satisfy the explicit restore command.
            Assert.DoesNotThrow(() => progress.Execute(new PlayerSetProgressCommand(0, 10, 5)));
            Assert.That(progress.GetSaveCommands().Cast<PlayerSetProgressCommand>().Single().WeaponItemId, Is.EqualTo(5));
        }

        [Test]
        public void MissingUsableDefaultWeaponFallsBackToUnarmed()
        {
            using var progress = Create(new Dictionary<int, GameItemInfo>
            {
                [1] = Weapon((PlayerActorId)1, (WeaponType)1)
            });
            Assert.That(progress.GetWeaponType(0), Is.Zero);
            Assert.DoesNotThrow(() => progress.Execute(new PlayerSetProgressCommand(0, 10, 0)));
        }

        [Test]
        public void InvalidExplicitWeaponDoesNotOverwriteValidProgress()
        {
            using var progress = Create(new Dictionary<int, GameItemInfo>
            {
                [1] = Weapon((PlayerActorId)1, (WeaponType)1),
                [5] = Weapon((PlayerActorId)0, (WeaponType)2)
            });
            progress.Execute(new PlayerSetProgressCommand(0, 10, 5));
            Assert.Throws<ArgumentException>(() => progress.Execute(new PlayerSetProgressCommand(0, 99, 1)));
            Assert.That(progress.GetLevel(0), Is.EqualTo(10));
            Assert.That(progress.GetWeaponType(0), Is.EqualTo(2));
        }

        private static GameItemInfo Weapon(PlayerActorId actor, WeaponType type) => new()
        {
            Type = ItemType.Weapon,
            WeaponType = type,
            ModelName = ActorConstants.MainActorWeaponMap[ActorConstants.MainActorNameMap[(PlayerActorId)0]],
            ApplicableActors = new HashSet<PlayerActorId> { actor }
        };

        private static PlayerProgressManager Create(IDictionary<int, GameItemInfo> items) => new(
            new Dictionary<int, CombatActorInfo>
            {
                [ActorConstants.MainActorCombatActorIdMap[(PlayerActorId)0]] = new() { Level = 1 }
            }, items, null);
    }
}
