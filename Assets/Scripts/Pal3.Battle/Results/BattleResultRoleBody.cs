// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public readonly struct BattleResultBodyState
    {
        public int Hp
        {
            get;
        }

        public int MaxHp
        {
            get;
        }

        public bool Guarding
        {
            get;
        }

        public bool Sleeping
        {
            get;
        }

        public bool Paralyzed
        {
            get;
        }

        public BattleResultBodyState(int hp, int maxHp, bool guarding = false, bool sleeping = false, bool paralyzed = false)
        {
            if (hp < 0 || maxHp < 1)
                throw new ArgumentOutOfRangeException(nameof(hp));
            Hp = hp;
            MaxHp = maxHp;
            Guarding = guarding;
            Sleeping = sleeping;
            Paralyzed = paralyzed;
        }

        // Both published GetProperCurAction routines inspect HP rather than
        // the separate life/model flags. Guard precedes sleep and low HP.
        public int ProperAction => Hp == 0 && MaxHp / 4 > 0 ? 47 : Guarding ? 42 : Sleeping || Paralyzed ? 45 : Hp < MaxHp / 4 ? 46 : 32;
    }

    public enum BattleResultBodyOwner
    {
        None, Victory, LevelUp, External
    }

    // A role owns its task/FIFO independently of the result text timeline.
    // Stage ownership is released before role Update on the following pass.
    public sealed class BattleResultRoleBody
    {
        private readonly IReadOnlyDictionary<int, BattlePhysicalAnimation> _animations;
        private readonly BattleResultBodyState _state;
        private readonly float _activeRate;
        private int? _task;
        private bool _executed;

        private readonly BattleBodyAnimationBinding _binding;

        public BattleBodyAnimationBinding Binding => _binding.Clone();

        public float PlaybackRate
        {
            get; private set;
        }

        public float BladeSeconds { get; private set; } = .01f;

        public bool HasTask => _task.HasValue;

        public bool VictoryCompleted
        {
            get; private set;
        }

        public BattleResultBodyOwner Owner
        {
            get; private set;
        }

        public bool NormalGrab
        {
            get; private set;
        }

        public BattleResultRoleBody(string variant, BattleBodyAnimationBinding initial,
            IReadOnlyDictionary<int, BattlePhysicalAnimation> animations, BattleResultBodyState state, bool victory = false)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown game variant.", nameof(variant));
            _binding = initial?.Clone() ?? throw new ArgumentNullException(nameof(initial));
            _animations = animations ?? throw new ArgumentNullException(nameof(animations));
            _state = state;
            _activeRate = variant == "PAL3" ? 1.3f : 1;
            PlaybackRate = _binding.RequestedNativeActionIndex == 32 ? .65f : _activeRate;
            if (victory)
            {
                Owner = BattleResultBodyOwner.Victory;
                NormalGrab = true;
                if (state.Sleeping || state.Paralyzed)
                    VictoryCompleted = true;
                else
                    _task = 49;
            }
        }

        public void PrepareStagePass()
        {
            if (Owner == BattleResultBodyOwner.Victory && VictoryCompleted)
                Release();
        }

        public bool TryGrab(BattleResultBodyOwner owner, bool normalGrab)
        {
            if (owner == BattleResultBodyOwner.None || Owner != BattleResultBodyOwner.None)
                return false;
            Owner = owner;
            NormalGrab = normalGrab;
            return true;
        }

        public void Release()
        {
            _task = null;
            _executed = false;
            Owner = BattleResultBodyOwner.None;
            NormalGrab = false;
        }

        public bool TryStartLevelUp()
        {
            if (Owner != BattleResultBodyOwner.None)
            {
                if (NormalGrab)
                    return false;
                Release();
            }
            TryGrab(BattleResultBodyOwner.LevelUp, true);
            _task = 59;
            _executed = false;
            return true;
        }

        public void Step()
        {
            // Actor.Update captures this rate before DoTask can change action.
            PlaybackRate = _binding.RequestedNativeActionIndex == 32 ? .65f : _activeRate;
            _binding.PopMessage();
            if (!_task.HasValue)
            {
                int action = _state.ProperAction;
                BladeSeconds = action == 42 ? .25f : .01f;
                if (_binding.RequestedNativeActionIndex != action)
                {
                    _binding.SetRequestedNativeAction(action);
                    _binding.TryPlay(_animations.GetValueOrDefault(action), true);
                }
            }
            else if (!_executed)
            {
                BladeSeconds = .25f;
                _binding.SetRequestedNativeAction(_task.Value);
                _binding.TryPlay(_animations.GetValueOrDefault(_task.Value), false);
                _binding.ClearMessages();
                _executed = true;
            }
            else if (_binding.LastPoppedMarker == BattlePhysicalMarkerKind.Over)
            {
                if (_task == 49)
                {
                    BladeSeconds = .01f;
                    VictoryCompleted = true;
                }
                // ClearFinishedTask removes the task but does not change this
                // pass's action/clip. Proper action is chosen on the next pass.
                _task = null;
                _executed = false;
            }
            _binding.Advance(PlaybackRate);
        }
    }
}
