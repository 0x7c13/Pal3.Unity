// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public sealed partial class BattleSession
    {
        private readonly Dictionary<string, BattleMovement> _soundingMovements = new(StringComparer.Ordinal);

        private void PlaySound(string sound)
        {
            if (sound != null)
                (_effects as IBattleAudio)?.PlaySound(sound);
        }

        private void PlayAttackBodySound(Actor actor, PhysicalBodyState body, BattlePhysicalMarkerKind marker)
        {
            BattleBodySoundProfile sounds = (_catalog as IBattleBodyAudioCatalog)?.BodySounds(actor.Initial.InstanceId) ?? default;
            string sound = BattleBodyAudioRules.Attack(_scenario.Variant, body.Profile.Identity, body.Profile.WeaponType,
                marker, sounds.Attack, actor.Definition?.MagicSword != null, IsPal3ATransformed(actor.Initial.InstanceId));
            PlaySound(sound);
        }

        private void PlayHurtBodySound(Actor actor, PhysicalBodyState body)
        {
            BattleBodySoundProfile sounds = (_catalog as IBattleBodyAudioCatalog)?.BodySounds(actor.Initial.InstanceId) ?? default;
            string sound = BattleBodyAudioRules.Hurt(_scenario.Variant, body.Profile.Identity, actor.Guard, sounds.Hurt);
            PlaySound(sound);
        }

        private void AdvanceBodyMovement(Actor actor, bool alive, BattleMovementMessage? message = null)
        {
            BattleMovement movement = actor.Movement;
            if (!_soundingMovements.TryGetValue(actor.Initial.InstanceId, out BattleMovement previous) || previous != movement)
            {
                StopMovementBodySound(actor);
                _soundingMovements[actor.Initial.InstanceId] = movement;
                int identity = actor.Initial.Side == BattleSide.Enemy ? -1 : (_catalog as IBattlePhysicalTimings)?.Physical(actor.Initial.InstanceId).Identity ?? actor.Definition?.Identity ?? -1;
                BattleMovementSound sound = BattleBodyAudioRules.Movement(_scenario.Variant, identity, movement.Plan);
                if (sound.Sound != null)
                    (_effects as IBattleMovementAudio)?.PlayMovementSound(actor.Initial.InstanceId, sound.Sound, sound.Loop);
            }
            movement.Step(alive, message);
            if (movement.Finished)
                StopMovementBodySound(actor);
        }

        private void StopMovementBodySound(Actor actor)
        {
            if (!_soundingMovements.Remove(actor.Initial.InstanceId))
                return;
            try
            {
                (_effects as IBattleMovementAudio)?.StopMovementSound(actor.Initial.InstanceId);
            }
            catch (Exception exception) when (_ending)
            {
                Add(BattleEventKind.PresentationCleanupFailed, actor.Initial.InstanceId, detail: exception.Message);
            }
        }

        private void SyncBodyMovementSound(Actor actor)
        {
            if (_soundingMovements.TryGetValue(actor.Initial.InstanceId, out BattleMovement movement) && actor.Movement != movement)
                StopMovementBodySound(actor);
        }
    }
}
