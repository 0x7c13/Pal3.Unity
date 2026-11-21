// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Resource work occurs while the original screen is faded out, before the
    // actor positions or effect playback change.
    public interface IBattleEffectPreloader
    {
        void Preload(int groupId);
    }

    public enum BattleCoAttackStep
    {
        GrabSource, FadeOutToCombo, Preload, SetComboPositions, FadeInToCombo,
        ShowName, WaitForSource, Act, WaitForSourceAfterAct, FadeOutToHome,
        RestorePositions, FadeInToHome, ReleaseSource
    }

    public enum BattleCoAttackStepStatus
    {
        Ready, Running, Finished
    }

    // Original linker metadata is captured before a command changes session state.
    // CoAttack creates no ordinary role animation; the effect group owns its visuals.
    public sealed class BattleComboStageProfile
    {
        public const float FadeSeconds = 1f;
        public const float NameSeconds = 1f;

        public int FirstGroupId
        {
            get;
        }

        public int SecondGroupId
        {
            get;
        }

        public float FirstWaitSeconds
        {
            get;
        }

        public float SecondWaitSeconds
        {
            get;
        }

        public float WorkSeconds
        {
            get;
        }

        public BattleMovementPoint FirstOrigin
        {
            get;
        }

        public BattleMovementPoint SecondOrigin
        {
            get;
        }

        public BattleMovementPoint EnemyCenter
        {
            get;
        }

        public BattleComboStageProfile(int firstGroupId, int secondGroupId,
            float firstWaitSeconds, float secondWaitSeconds, float workSeconds,
            BattleMovementPoint firstOrigin = default, BattleMovementPoint secondOrigin = default,
            BattleMovementPoint enemyCenter = default)
        {
            if (firstGroupId < -1 || secondGroupId < -1 || !ValidTime(firstWaitSeconds) ||
                !ValidTime(secondWaitSeconds) || !ValidTime(workSeconds))
                throw new ArgumentException("Invalid original combo effect linker.");
            FirstGroupId = firstGroupId;
            SecondGroupId = secondGroupId;
            FirstWaitSeconds = firstWaitSeconds;
            SecondWaitSeconds = secondWaitSeconds;
            WorkSeconds = workSeconds;
            FirstOrigin = firstOrigin;
            SecondOrigin = secondOrigin;
            EnemyCenter = enemyCenter;
        }

        private static bool ValidTime(float value) => float.IsFinite(value) && value >= 0 && value <= 600;
    }

    public sealed class BattleCoAttackStepSnapshot
    {
        public BattleCoAttackStep Step
        {
            get;
        }

        public BattleCoAttackStepStatus Status
        {
            get;
        }

        public float StartTime
        {
            get;
        }

        internal BattleCoAttackStepSnapshot(BattleCoAttackStep step, BattleCoAttackStepStatus status, float startTime)
        {
            Step = step;
            Status = status;
            StartTime = startTime;
        }
    }

    public sealed class BattleCoAttackSnapshot
    {
        public long ActionId
        {
            get;
        }

        public int ComboId
        {
            get;
        }

        public string Source
        {
            get;
        }

        public string Name
        {
            get;
        }

        public bool NameVisible
        {
            get;
        }

        public bool Worked
        {
            get;
        }

        public bool Finished
        {
            get;
        }

        public IReadOnlyList<string> Targets
        {
            get;
        }

        public IReadOnlyList<BattleCoAttackStepSnapshot> Steps
        {
            get;
        }

        internal BattleCoAttackSnapshot(long actionId, BattleComboDefinition combo, string source,
            bool nameVisible, bool worked, bool finished, IEnumerable<string> targets, IEnumerable<BattleCoAttackStepSnapshot> steps)
        {
            ActionId = actionId;
            ComboId = combo.Id;
            Source = source;
            Name = combo.Name;
            NameVisible = nameVisible;
            Worked = worked;
            Finished = finished;
            Targets = Array.AsReadOnly(targets.ToArray());
            Steps = Array.AsReadOnly(steps.ToArray());
        }
    }
}
