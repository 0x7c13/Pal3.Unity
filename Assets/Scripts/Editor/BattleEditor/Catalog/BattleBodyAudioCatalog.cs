// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using Battle;

    public sealed partial class BattleAnimationCatalog : IBattleBodyAudioCatalog
    {
        public BattleBodySoundProfile BodySounds(string actor)
        {
            var participant = _participants[actor];
            var properties = _data.Database.CombatActorInfos[participant.ActorId].Properties;
            return new BattleBodySoundProfile(properties?.Length > 0 ? (int)properties[0] : 0,
                properties?.Length > 1 ? (int)properties[1] : 0);
        }
    }
}
