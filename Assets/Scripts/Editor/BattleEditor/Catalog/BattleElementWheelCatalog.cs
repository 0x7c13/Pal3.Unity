// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Engine.Extensions;

    public sealed partial class BattleAnimationCatalog : IBattleElementWheelTimings
    {
        private readonly Dictionary<(string Actor, string Model, int Skill), BattleAbilityStageProfile> _elementWheelStages = new();
        private readonly Dictionary<(string Actor, string Model, int Skill), AttackTiming> _elementWheelTimings = new();
        private readonly Dictionary<(string Actor, string Model, int Skill), BattlePhysicalAnimation> _elementWheelBodies = new();

        public BattleAbilityStageProfile ElementWheelStage(string actor, BattleAbilityDefinition ability)
        {
            if (BattleData.Variant != "PAL3A" || ability == null || ability.Id is < 264 or > 268 ||
                !_participants.TryGetValue(actor, out BattleParticipant participant) || PhysicalIdentity(participant) != 0)
                throw new ArgumentException("The original element wheel requires Nangong Huang and its field skill.");
            var key = (actor, _data.Model(participant), ability.Id);
            if (_elementWheelStages.TryGetValue(key, out BattleAbilityStageProfile captured))
                return captured;
            if (!_linkers.SkillIdToEffectGroupMap.TryGetValue(ability.Id, out var groups) || groups.Length != 2)
                throw new NotSupportedException("The element wheel needs its two original linker groups.");
            int bodyTicks = 0;
            bool retainedBody = !_data.TryActionPath(participant, "z24", out string path);
            if (!retainedBody)
            {
                BattleAnimationMetadata body = _data.GetAnimationMetadata(path);
                bodyTicks = ToTicks(body.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
            }
            captured = new BattleAbilityStageProfile(BattleAbilityStageKind.ElementWheel, CaptureAbilityBinding(groups[0]), CaptureAbilityBinding(groups[1]),
                groups[0].WorkTimeInSeconds, BattleAbilityWorkBoundary.WorkTime, bodyTicks, false, retainsBody: retainedBody);
            _elementWheelStages.Add(key, captured);
            return captured;
        }

        public AttackTiming ElementWheel(string actor, BattleAbilityDefinition ability)
        {
            BattleAbilityStageProfile profile = ElementWheelStage(actor, ability);
            var key = (actor, _data.Model(_participants[actor]), ability.Id);
            if (_elementWheelTimings.TryGetValue(key, out AttackTiming captured))
                return captured;
            int hit = Math.Max(0, checked((int)Math.Ceiling(profile.WorkSeconds / BattleSession.StepSeconds)));
            captured = new AttackTiming(0, Math.Max(1, Math.Max(hit, profile.BodyAnimationTicks)), new[] { hit },
                bodyAnimationTicks: profile.BodyAnimationTicks, nativeWorkTimeSeconds: profile.WorkSeconds);
            _elementWheelTimings.Add(key, captured);
            return captured;
        }

        public BattlePhysicalAnimation ElementWheelBody(string actor, BattleAbilityDefinition ability)
        {
            BattleAbilityStageProfile profile = ElementWheelStage(actor, ability);
            var key = (actor, _data.Model(_participants[actor]), ability.Id);
            if (_elementWheelBodies.TryGetValue(key, out BattlePhysicalAnimation captured))
                return captured;
            captured = profile.RetainsBody ? null : CompilePhysicalAnimation(_participants[actor], "z24");
            _elementWheelBodies.Add(key, captured);
            return captured;
        }
    }
}
