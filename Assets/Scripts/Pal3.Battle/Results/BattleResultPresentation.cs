// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleResultStepKind
    {
        MusicFadeOut, WinBanner, LoseBanner, Statistics, LevelUpBody, LevelUp, SkillLearned, SkillImproved, Money, Item, Wait, MusicFadeIn, ScreenFadeOut
    }

    // A captured presentation command contains no mutable inventory or actor state.
    public sealed class BattleResultStep
    {
        public BattleResultStepKind Kind
        {
            get;
        }

        public float Duration
        {
            get;
        }

        public string Actor
        {
            get;
        }

        public int Id
        {
            get;
        }

        public int Value
        {
            get;
        }

        public bool CanSkip => Kind is not (BattleResultStepKind.MusicFadeOut or BattleResultStepKind.MusicFadeIn or BattleResultStepKind.LevelUpBody);

        public BattleResultStep(BattleResultStepKind kind, float duration, string actor = null, int id = 0, int value = 0)
        {
            if (!float.IsFinite(duration) || duration < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            Kind = kind;
            Duration = duration;
            Actor = actor;
            Id = id;
            Value = value;
        }
    }

    public interface IBattleResultBodyPort
    {
        bool TryStartLevelUp(string actor);
    }

    // Result commit stays in BattleSession. This independent 50Hz clock only
    // presents its captured report and can be replayed without settling again.
    public sealed class BattleResultPresentation
    {
        private readonly IReadOnlyList<BattleResultStep> _sequence;
        private readonly IReadOnlyList<BattleActorSnapshot> _presentationActors, _finalActors;
        private readonly IBattleResultBodyPort _bodies;
        private bool _executed;
        private float _startTime, _time;
        private double _accumulator;

        public IReadOnlyList<BattleResultStep> Sequence => _sequence;

        // A escape recovers HP only after its result tasks. The authoritative
        // final commit stays unchanged while the show retains pre-recovery HP.
        public IReadOnlyList<BattleActorSnapshot> Actors => Completed ? _finalActors : _presentationActors;

        public int Index
        {
            get; private set;
        }

        public long Tick
        {
            get; private set;
        }

        public BattleResultStep Current => Completed ? null : _sequence[Index];

        public bool Completed => Index >= _sequence.Count;

        public bool HasExecuted => _executed;

        public bool CanSkip => _executed && Current?.CanSkip == true;

        public int CameraList
        {
            get;
        }

        public bool StatisticsVisible
        {
            get; private set;
        }

        public double StatisticsSeconds
        {
            get; private set;
        }

        public float Elapsed => Math.Max(0, _time - _startTime);

        public float ScreenOpacity
        {
            get; private set;
        }

        public float MusicGain { get; private set; } = 1;

        public int MusicStopCount
        {
            get; private set;
        }

        public BattleResultPresentation(string variant, BattleResult result, IReadOnlyList<BattleEvent> events, IBattleResultBodyPort bodies = null)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown game variant.", nameof(variant));
            if (result == null || events == null || result.Status != BattleSessionStatus.Completed)
                throw new ArgumentException("A committed completed result and its full event stream are required.");
            if (result.SettlementEventStart < 0 || result.SettlementEventStart > events.Count)
                throw new ArgumentException("Settlement event range is outside the supplied stream.");
            _bodies = bodies;
            _finalActors = Array.AsReadOnly(result.Actors.ToArray());
            _presentationActors = Array.AsReadOnly(result.Actors.Select(actor =>
                variant == "PAL3A" && result.Escaped && actor.Side == BattleSide.Ally &&
                result.PreSettlementActors.First(before => before.Id == actor.Id).Hp == 0
                    ? result.PreSettlementActors.First(before => before.Id == actor.Id) : actor).ToArray());
            CameraList = result.Escaped ? 8 : result.Winner == BattleSide.Ally ? 3 : 10;
            var steps = new List<BattleResultStep>();
            void Add(BattleResultStepKind kind, float duration) => steps.Add(new BattleResultStep(kind, duration));
            bool win = result.Winner == BattleSide.Ally && !result.Escaped;
            if (!result.Escaped)
            {
                Add(BattleResultStepKind.MusicFadeOut, .5f);
                Add(win ? BattleResultStepKind.WinBanner : BattleResultStepKind.LoseBanner, win ? 3 : 5);
            }
            if (win || result.Escaped)
            {
                Add(BattleResultStepKind.Statistics, 2);
                foreach (BattleEvent entry in events.Skip(result.SettlementEventStart))
                {
                    BattleResultStep step = entry.Kind switch
                    {
                        BattleEventKind.LevelUp => new BattleResultStep(BattleResultStepKind.LevelUp, 5, entry.Source,
                            value: result.Rewards.First(reward => reward.Actor == entry.Source).LevelAfter),
                        BattleEventKind.SkillLearned => new BattleResultStep(BattleResultStepKind.SkillLearned, 5, entry.Source, entry.AbilityId),
                        BattleEventKind.SkillImproved => new BattleResultStep(BattleResultStepKind.SkillImproved, 5, entry.Source, entry.AbilityId, entry.ValueAfter),
                        BattleEventKind.ResourceChanged when entry.Resource == 10 && entry.RequestedDelta > 0 =>
                            new BattleResultStep(BattleResultStepKind.Money, 5, value: entry.RequestedDelta),
                        BattleEventKind.ItemChanged when entry.RequestedDelta > 0 =>
                            new BattleResultStep(BattleResultStepKind.Item, 4, id: entry.AbilityId, value: entry.RequestedDelta),
                        _ => null
                    };
                    if (step != null)
                    {
                        if (variant == "PAL3A" && step.Kind == BattleResultStepKind.LevelUp)
                            steps.Add(new BattleResultStep(BattleResultStepKind.LevelUpBody, 0, step.Actor, value: step.Value));
                        steps.Add(step);
                    }
                }
                Add(BattleResultStepKind.Wait, result.Escaped ? variant == "PAL3A" ? 3 : 15 : 5);
            }
            if (!result.Escaped)
            {
                if (!win)
                    Add(BattleResultStepKind.ScreenFadeOut, 1);
                Add(BattleResultStepKind.MusicFadeIn, .2f);
                if (win)
                    Add(BattleResultStepKind.ScreenFadeOut, 1);
            }
            _sequence = steps.AsReadOnly();
        }

        public void Advance(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0 || seconds > 60)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _accumulator += seconds;
            while (_accumulator + 1e-12 >= BattleSession.StepSeconds && !Completed)
            {
                _accumulator -= BattleSession.StepSeconds;
                Step();
            }
        }

        public void CloseStatistics()
        {
            StatisticsVisible = false;
            if (Current?.Kind == BattleResultStepKind.Statistics && _executed)
                FinishCurrent();
        }

        public bool SkipCurrent()
        {
            if (!CanSkip)
                return false;
            FinishCurrent();
            return true;
        }

        public void Step()
        {
            if (Completed)
                return;
            Tick++;
            _time = (float)(Tick * BattleSession.StepSeconds);
            if (StatisticsVisible)
                StatisticsSeconds += BattleSession.StepSeconds;
            if (!_executed)
            {
                if (Current.Kind == BattleResultStepKind.LevelUpBody)
                {
                    if (_bodies != null && !_bodies.TryStartLevelUp(Current.Actor))
                        return;
                    FinishCurrent();
                    if (Completed)
                        return;
                }
                _executed = true;
                if (Current.Kind == BattleResultStepKind.Statistics)
                {
                    StatisticsVisible = true;
                    StatisticsSeconds = 0;
                }
                if (Current.Kind is not (BattleResultStepKind.MusicFadeOut or BattleResultStepKind.MusicFadeIn))
                    return;
            }
            float elapsed = Elapsed;
            switch (Current.Kind)
            {
                case BattleResultStepKind.MusicFadeOut:
                    MusicGain = Math.Clamp(1 - elapsed / Current.Duration, 0, 1);
                    break;
                case BattleResultStepKind.MusicFadeIn:
                    // Both shipped callbacks use this reversed ramp, then restore
                    // the saved volume only when the strict end boundary passes.
                    MusicGain = Math.Clamp(1 - elapsed / Current.Duration, 0, 1);
                    break;
                case BattleResultStepKind.ScreenFadeOut:
                    ScreenOpacity = Math.Clamp(elapsed / Current.Duration, 0, 1);
                    break;
            }
            if (Current.Kind == BattleResultStepKind.Wait ? elapsed >= Current.Duration : elapsed > Current.Duration)
                FinishCurrent();
        }

        private void FinishCurrent()
        {
            if (Current.Kind == BattleResultStepKind.MusicFadeOut)
            {
                MusicGain = 0;
                MusicStopCount++;
            }
            if (Current.Kind == BattleResultStepKind.MusicFadeIn)
                MusicGain = 1;
            if (Current.Kind == BattleResultStepKind.ScreenFadeOut)
                ScreenOpacity = 1;
            Index++;
            _executed = false;
            _startTime = _time;
            if (Completed)
            {
                // The standalone editor retains a useful inspection viewport.
                ScreenOpacity = 0;
                StatisticsVisible = false;
            }
        }
    }
}
