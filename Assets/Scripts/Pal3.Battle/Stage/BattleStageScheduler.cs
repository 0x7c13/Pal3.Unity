// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public interface IBattleStageJob
    {
        // A finished subtask remains in the stage until the following pass.
        bool Advance(BattleStageContext context);
    }

    // Jobs that hold resources beyond their scheduler slot release those
    // resources when an unfinished task is removed from any cancellation path.
    public interface IBattleStageCancellableJob : IBattleStageJob
    {
        void Cancel();
    }

    public readonly struct BattleStageHandle : IEquatable<BattleStageHandle>
    {
        internal BattleStageScheduler Scheduler
        {
            get;
        }

        public int Slot
        {
            get;
        }

        public long Id
        {
            get;
        }

        internal BattleStageHandle(BattleStageScheduler scheduler, int slot, long id)
        {
            Scheduler = scheduler;
            Slot = slot;
            Id = id;
        }

        public bool Equals(BattleStageHandle other)
            => ReferenceEquals(Scheduler, other.Scheduler) && Slot == other.Slot && Id == other.Id;

        public override bool Equals(object obj) => obj is BattleStageHandle other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Scheduler, Slot, Id);

        public static bool operator ==(BattleStageHandle left, BattleStageHandle right) => left.Equals(right);

        public static bool operator !=(BattleStageHandle left, BattleStageHandle right) => !left.Equals(right);
    }

    public readonly struct BattleStageContext
    {
        public BattleStageScheduler Scheduler
        {
            get;
        }

        public BattleStageHandle Handle
        {
            get;
        }

        public long Tick
        {
            get;
        }

        public float Time
        {
            get;
        }

        public float CreatedTime
        {
            get;
        }

        internal BattleStageContext(BattleStageScheduler scheduler, BattleStageHandle handle,
            long tick, float time, float createdTime)
        {
            Scheduler = scheduler;
            Handle = handle;
            Tick = tick;
            Time = time;
            CreatedTime = createdTime;
        }
    }

    public sealed class BattleStageTaskSnapshot
    {
        public BattleStageHandle Handle
        {
            get;
        }

        public string Owner
        {
            get;
        }

        public string Kind
        {
            get;
        }

        public float CreatedTime
        {
            get;
        }

        public bool Finished
        {
            get;
        }

        internal BattleStageTaskSnapshot(BattleStageHandle handle, string owner, string kind,
            float createdTime, bool finished)
        {
            Handle = handle;
            Owner = owner;
            Kind = kind;
            CreatedTime = createdTime;
            Finished = finished;
        }
    }

    // CCBStage allocates the first empty slot and scans the live array in order.
    // A task created above the current slot can therefore run in the same pass.
    public sealed class BattleStageScheduler
    {
        public const int DefaultCapacity = 256;
        public const double StepSeconds = 0.02d;
        private readonly Task[] _tasks;
        private long _nextId;
        private bool _advancing;

        public int Capacity => _tasks.Length;

        public int Count
        {
            get; private set;
        }

        public BattleStageScheduler(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _tasks = new Task[capacity];
        }

        // This combines native AddTask/StartTask: it stores the float start time
        // and marks the task running without executing its first callback.
        public BattleStageHandle Add(string owner, string kind, IBattleStageJob job, float createdTime, bool autoDelete = true)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (kind == null)
                throw new ArgumentNullException(nameof(kind));
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            if (float.IsNaN(createdTime) || float.IsInfinity(createdTime))
                throw new ArgumentOutOfRangeException(nameof(createdTime));
            for (int slot = 0; slot < _tasks.Length; slot++)
            {
                if (_tasks[slot] != null)
                    continue;
                var handle = new BattleStageHandle(this, slot, checked(++_nextId));
                _tasks[slot] = new Task(handle, owner, kind, job, createdTime, autoDelete);
                Count++;
                return handle;
            }
            throw new InvalidOperationException("The combat stage task pool is full.");
        }

        public bool TryAdd(string owner, string kind, IBattleStageJob job, float createdTime, out BattleStageHandle handle, bool autoDelete = true)
        {
            if (Count >= Capacity)
            {
                handle = default;
                return false;
            }
            handle = Add(owner, kind, job, createdTime, autoDelete);
            return true;
        }

        public bool IsActive(BattleStageHandle handle)
            => ReferenceEquals(handle.Scheduler, this) && handle.Slot >= 0 && handle.Slot < _tasks.Length &&
                _tasks[handle.Slot]?.Handle == handle;

        public bool IsFinished(BattleStageHandle handle) => IsActive(handle) && _tasks[handle.Slot].Finished;

        public bool Cancel(BattleStageHandle handle)
        {
            if (!IsActive(handle))
                return false;
            Task task = _tasks[handle.Slot];
            _tasks[handle.Slot] = null;
            Count--;
            // Detach first so cancellation is idempotent and may reuse the slot.
            if (!task.Finished && task.Job is IBattleStageCancellableJob cancellable)
                cancellable.Cancel();
            return true;
        }

        public int CountOwner(string owner)
        {
            int count = 0;
            foreach (Task task in _tasks)
            {
                if (task != null && string.Equals(task.Owner, owner, StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        public IReadOnlyList<BattleStageTaskSnapshot> Snapshot()
        {
            var result = new List<BattleStageTaskSnapshot>(Count);
            foreach (Task task in _tasks)
            {
                if (task != null)
                    result.Add(new BattleStageTaskSnapshot(task.Handle, task.Owner, task.Kind,
                        task.CreatedTime, task.Finished));
            }
            return result.AsReadOnly();
        }

        public static float TimeAt(long tick)
        {
            if (tick < 0)
                throw new ArgumentOutOfRangeException(nameof(tick));
            return (float)(tick * StepSeconds);
        }

        public void Advance(long tick)
        {
            float time = TimeAt(tick);
            if (_advancing)
                throw new InvalidOperationException("A combat stage pass cannot run recursively.");
            _advancing = true;
            try
            {
                for (int slot = 0; slot < _tasks.Length; slot++)
                {
                    Task task = _tasks[slot];
                    if (task == null)
                        continue;
                    if (task.Finished)
                    {
                        if (task.AutoDelete)
                            Cancel(task.Handle);
                        continue;
                    }
                    bool finished = task.Job.Advance(new BattleStageContext(this, task.Handle,
                        tick, time, task.CreatedTime));
                    // A callback may cancel itself and reuse this slot. Its return
                    // value must never finish or delete the replacement task.
                    if (ReferenceEquals(_tasks[slot], task))
                        task.Finished = finished;
                }
            }
            finally
            {
                _advancing = false;
            }
        }

        private sealed class Task
        {
            internal readonly BattleStageHandle Handle;
            internal readonly string Owner;
            internal readonly string Kind;
            internal readonly IBattleStageJob Job;
            internal readonly float CreatedTime;
            internal bool Finished;
            internal readonly bool AutoDelete;

            internal Task(BattleStageHandle handle, string owner, string kind, IBattleStageJob job,
                float createdTime, bool autoDelete)
            {
                Handle = handle;
                Owner = owner;
                Kind = kind;
                Job = job;
                CreatedTime = createdTime;
                AutoDelete = autoDelete;
            }
        }
    }
}
