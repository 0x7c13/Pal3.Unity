// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using GameSystems.Battle;
    using GameSystems.Battle.Actor.Controllers;
    using GameSystems.Battle.Scene;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class BattleSceneTargetTests
    {
        [TestCase(ElementPosition.AllyCenter, ElementPosition.EnemyEarth)]
        [TestCase(ElementPosition.EnemyWind, ElementPosition.AllyCenter)]
        public void RandomOpponentUsesOccupiedSlotsOnTheOtherSide(ElementPosition from, ElementPosition to)
        {
            var root = new GameObject();
            try
            {
                var scene = root.AddComponent<BattleScene>();
                scene.LoadActors(new(), default);
                var actors = (Dictionary<ElementPosition, BattleActorController>)typeof(BattleScene)
                    .GetField("_battleActorControllers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scene);
                actors[from] = root.AddComponent<BattleActorController>();
                Assert.That(scene.GetRandomOpponent(from), Is.Null);

                var target = root.AddComponent<BattleActorController>();
                actors[to] = target;
                Assert.That(scene.GetRandomOpponent(from), Is.SameAs(target));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
