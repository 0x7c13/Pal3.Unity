// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public readonly struct BattlePhysicalAnchor
    {
        public int First
        {
            get;
        }

        public int? Second
        {
            get;
        }

        public BattlePhysicalAnchor(int first, int? second = null)
        {
            if (first is < 0 or > 10 || second is < 0 or > 10)
                throw new ArgumentException("Invalid original attack anchor.");
            First = first;
            Second = second;
        }
    }

    public static class BattlePhysicalGeometry
    {
        public static BattlePhysicalAnchor Anchor(IReadOnlyList<int> selected, IReadOnlyDictionary<int, BattleSlot> slots, int sceneElement)
        {
            if (selected == null || slots == null)
                throw new ArgumentNullException(selected == null ? nameof(selected) : nameof(slots));
            int[] indices = selected.OrderBy(index => index).ToArray();
            if (indices.Length == 0 || indices.Any(index => index is < 0 or > 10))
                throw new ArgumentException("The original normal attack requires a target mask.");
            if (indices.Length == 1)
                return new BattlePhysicalAnchor(indices[0]);
            bool Has(int index) => Array.IndexOf(indices, index) >= 0;
            if (indices[0] >= 5)
            {
                if (Has(8) && Has(9))
                    return new BattlePhysicalAnchor(8, 9);
                if (Has(5) && Has(7))
                    return new BattlePhysicalAnchor(10);
                if (Has(6) && !Has(10))
                    return new BattlePhysicalAnchor(6);
                if (Has(5) && Has(10) && !Has(7))
                    return new BattlePhysicalAnchor(5, 10);
                if (!Has(5) && Has(10) && Has(7))
                    return new BattlePhysicalAnchor(10, 7);
                if (Has(8) && Has(5))
                    return new BattlePhysicalAnchor(8);
                if (Has(9) && Has(7))
                    return new BattlePhysicalAnchor(9);
                if (Has(10) && Has(6))
                    return new BattlePhysicalAnchor(10);
                return new BattlePhysicalAnchor(indices[0]);
            }
            int first = indices[0], second = indices[1];
            if (second < 5 && BattleTargetRules.Aligned(BattleSide.Ally, slots[first], slots[second], true, sceneElement))
            {
                if (indices.Length == 2)
                    return new BattlePhysicalAnchor(first, second);
                foreach (int index in indices.Take(3))
                    if (slots[index] == BattleSlot.Center)
                        return new BattlePhysicalAnchor(index);
            }
            return new BattlePhysicalAnchor(slots[first] == BattleSlot.Center || BattleTargetRules.IsFrontAllySlot(slots[first], sceneElement) ? first : second);
        }

        public static BattleMovementPoint Approach(BattleMovementPoint source, float sourceRadius, BattleMovementPoint firstHome,
            float targetRadius, BattleMovementPoint? secondHome = null, bool spear = false, bool doubleRadius = false)
        {
            BattleMovementPoint destination = secondHome.HasValue
                ? new BattleMovementPoint((firstHome.X + secondHome.Value.X) / 2f, (firstHome.Y + secondHome.Value.Y) / 2f, (firstHome.Z + secondHome.Value.Z) / 2f)
                : firstHome;
            BattleMovementPoint direction = BattleMovement.NormalizeDirection(new BattleMovementPoint(source.X - destination.X,
                source.Y - destination.Y, source.Z - destination.Z));
            float distance = secondHome.HasValue ? 40f : sourceRadius + targetRadius + (spear ? 80f : 0f);
            var offset = new BattleMovementPoint(direction.X * distance, direction.Y * distance, direction.Z * distance);
            if (doubleRadius && !secondHome.HasValue)
                offset = new BattleMovementPoint(offset.X * 2f, offset.Y * 2f, offset.Z * 2f);
            return new BattleMovementPoint(destination.X + offset.X, destination.Y + offset.Y, destination.Z + offset.Z);
        }

        public static BattleMovementPoint ProtectPosition(BattleMovementPoint targetHome, BattleMovementPoint attackerHome, float distance = 40f)
        {
            BattleMovementPoint direction = BattleMovement.NormalizeDirection(new BattleMovementPoint(attackerHome.X - targetHome.X,
                attackerHome.Y - targetHome.Y, attackerHome.Z - targetHome.Z));
            return new BattleMovementPoint(targetHome.X + direction.X * distance, targetHome.Y + direction.Y * distance, targetHome.Z + direction.Z * distance);
        }

        public static BattleMovementPoint ProtectRunTarget(BattleMovementPoint source, BattleMovementPoint targetHome, BattleMovementPoint attackerHome)
        {
            BattleMovementPoint destination = ProtectPosition(targetHome, attackerHome, 32f);
            if (BattleMovement.Distance(source, destination) < 4f)
                return destination;
            BattleMovementPoint direction = BattleMovement.NormalizeDirection(new BattleMovementPoint(source.X - destination.X,
                source.Y - destination.Y, source.Z - destination.Z));
            return new BattleMovementPoint(destination.X + direction.X * 10f, destination.Y + direction.Y * 10f, destination.Z + direction.Z * 10f);
        }

        public static bool AtProtectPosition(int guardianIndex, BattleMovementPoint source, BattleMovementPoint targetHome, BattleMovementPoint attackerHome)
            => guardianIndex == 0 && BattleMovement.Distance(source, ProtectPosition(targetHome, attackerHome)) <= 20f;
    }
}
