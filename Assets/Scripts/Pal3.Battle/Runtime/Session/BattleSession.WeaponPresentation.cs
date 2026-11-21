// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private readonly Dictionary<string, bool> _weaponDrawingEnabled = new(StringComparer.Ordinal);

        public bool WeaponDrawingEnabled(string actorId) => _weaponDrawingEnabled.GetValueOrDefault(actorId, true);

        public void SetWeaponDrawingEnabled(string actorId, bool enabled)
        {
            if (!_byId.ContainsKey(actorId ?? ""))
                throw new ArgumentException("Unknown original weapon owner.", nameof(actorId));
            _weaponDrawingEnabled[actorId] = enabled;
        }

        public BattleWeaponVisibility WeaponVisibility(string actorId)
        {
            Actor actor = _byId[actorId];
            var weapon = actor.Definition?.Equipment.FirstOrDefault(item => item.Slot == 0);
            BattleBodyAnimationBinding body = GetBodyBinding(actor);
            int action = body.RequestedNativeActionIndex;
            BattleEffectActorSnapshot effect = EffectActor(actorId);
            return BattleWeaponPresentationRules.Visible(_scenario.Variant, actor.Definition?.Identity ?? -1,
                action, weapon?.WeaponType ?? 0, IsPal3ATransformed(actorId), weapon != null && actor.Initial.Side == BattleSide.Ally && WeaponDrawingEnabled(actorId),
                effect.Controlled, effect.LeftWeapon, effect.RightWeapon);
        }

    }
}
