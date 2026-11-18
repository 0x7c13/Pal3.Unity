// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleEffectRequest
    {
        public int GroupId
        {
            get;
        }

        public string Source
        {
            get;
        }

        public IReadOnlyList<string> Targets
        {
            get;
        }

        public BattleMovementPoint Origin
        {
            get;
        }

        public uint Seed
        {
            get;
        }

        public bool AbsoluteTime
        {
            get;
        }

        public BattleEffectRequest(int groupId, string source, IEnumerable<string> targets, BattleMovementPoint origin, uint seed,
            bool absoluteTime = false)
        {
            if (groupId < 0 || string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("An effect needs a group and its owning combat actor.");
            GroupId = groupId;
            Source = source;
            Targets = Array.AsReadOnly(targets?.ToArray() ?? Array.Empty<string>());
            Origin = origin;
            Seed = seed;
            AbsoluteTime = absoluteTime;
        }
    }

    // Role bodies update before the original camera controller. An optional
    // stage presentation owner receives only completed (including resumed) frames.
    public interface IBattleStagePresentation
    {
        void AdvanceStage(double scaledSeconds, double absoluteSeconds);
    }

    public interface IBattleTimeScale
    {
        // Native slow-motion effects temporarily run the stage at a fraction of normal speed (0–1).
        float TimeScale
        {
            get;
        }
    }

    // The composition root supplies the original resource player. Advancing it
    // belongs to the fixed battle loop; rendering never determines work timing.
    public interface IBattleEffectDirector
    {
        long Start(BattleEffectRequest request);

        bool IsRunning(long handle);

        void Advance(long frame, float scaledSeconds, float unscaledSeconds);

        void Stop(long handle);
    }
}
