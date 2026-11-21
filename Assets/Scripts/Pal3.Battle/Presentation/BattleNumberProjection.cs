// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public readonly struct BattleNumberFrame
    {
        public string Text
        {
            get;
        }

        public BattleMovementPoint Position
        {
            get;
        }

        public int OffsetX
        {
            get;
        }

        public int OffsetY
        {
            get;
        }

        public uint Color
        {
            get;
        }

        public byte Alpha
        {
            get;
        }

        public BattleNumberFrame(string text, BattleMovementPoint position, int offsetX, int offsetY, uint color, byte alpha)
        {
            Text = text;
            Position = position;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Color = color;
            Alpha = alpha;
        }
    }

    // A read-only projection of published number activations. It owns neither
    // battle tasks nor random draws, and survives batched stepping and replay.
    public sealed class BattleNumberProjection
    {
        private sealed class Number
        {
            public BattleEvent Event;
            public int Stack;
        }

        private sealed class Stack
        {
            public long Tick;
            public float Height;
        }

        private readonly List<Number> _numbers = new();
        private readonly Dictionary<string, Stack> _stacks = new(StringComparer.Ordinal);
        private readonly string _guName;
        private int _eventCursor;
        private long _tick;

        public BattleNumberProjection(string guName = "蛊") => _guName = guName;

        public void Synchronize(IReadOnlyList<BattleEvent> events, long simulationTick)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            if (simulationTick < _tick || events.Count < _eventCursor)
                Clear();
            _tick = simulationTick;
            for (; _eventCursor < events.Count; _eventCursor++)
            {
                BattleEvent entry = events[_eventCursor];
                if (entry.Kind != BattleEventKind.NumberShown)
                    continue;
                int stack = 16;
                if (!entry.NumberIsMiss)
                {
                    if (!_stacks.TryGetValue(entry.Target, out Stack state))
                        _stacks.Add(entry.Target, state = new Stack { Tick = entry.NumberActivationTick });
                    // The native stage decays once before each simulation pass.
                    // The loop is bounded by the 160-pixel reset and zero floor.
                    for (long tick = state.Tick; tick < entry.NumberActivationTick && state.Height > 0; tick++)
                        state.Height = state.Height > 160 ? 160 : Math.Max(0, state.Height - .6f);
                    state.Tick = entry.NumberActivationTick;
                    state.Height += 10;
                    stack = Round(state.Height);
                }
                _numbers.Add(new Number { Event = entry, Stack = stack });
            }
            float now = BattleStageScheduler.TimeAt(simulationTick);
            _numbers.RemoveAll(number => (double)now - number.Event.NumberStartedTime > number.Event.DurationSeconds);
        }

        public IReadOnlyList<BattleNumberFrame> Sample()
        {
            var result = new List<BattleNumberFrame>(_numbers.Count);
            float now = BattleStageScheduler.TimeAt(_tick);
            foreach (Number number in _numbers)
            {
                BattleEvent entry = number.Event;
                double elapsed = Math.Max(0, (double)now - entry.NumberStartedTime);
                double ratio = entry.DurationSeconds > 0 ? Math.Min(1, elapsed / entry.DurationSeconds) : 1;
                byte alpha = (byte)Math.Clamp(Round((float)((1 - ratio) * 255)), 0, 255);
                int rise = Round((float)(entry.NumberIsMiss ? ratio * 32 : elapsed * 30));
                bool cost = entry.Resource == 16;
                string text = entry.NumberIsMiss ? "MISS" : (cost ? _guName + " " : string.Empty) +
                    entry.RequestedDelta.ToString(CultureInfo.InvariantCulture);
                uint color = entry.NumberIsMiss || cost ? 0x32ffffu : entry.Resource switch
                {
                    1 => 0xfbe0a2u,
                    2 => 0xfaa8d5u,
                    _ => entry.NumberIsRecovery ? 0x00ff00u : 0x3232ffu
                };
                result.Add(new BattleNumberFrame(text, entry.NumberPosition,
                    entry.NumberOffset - (cost ? 32 : 16), number.Stack - rise, color, alpha));
            }
            return result;
        }

        private static int Round(float value) => (int)Math.Round(value, MidpointRounding.ToEven);

        public void Clear()
        {
            _numbers.Clear();
            _stacks.Clear();
            _eventCursor = 0;
            _tick = 0;
        }
    }
}
