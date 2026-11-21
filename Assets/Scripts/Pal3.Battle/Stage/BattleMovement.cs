// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // Raw GameBox scene/model units. Presentation owns conversion to Unity coordinates.
    public readonly struct BattleMovementPoint
    {
        public float X
        {
            get;
        }

        public float Y
        {
            get;
        }

        public float Z
        {
            get;
        }

        public BattleMovementPoint(float x, float y, float z)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) ||
                float.IsNaN(z) || float.IsInfinity(z))
                throw new ArgumentException("Movement coordinates must be finite.");
            X = x;
            Y = y;
            Z = z;
        }
    }

    public enum BattleMovementMessage
    {
        None, FlyUp, FlyDown, Over
    }

    public enum BattleMovementKind
    {
        Run,
        Fly,
        Teleport,
        FlyRotate,
        NpcFly,
        FastRun,
        EnemyFlee
    }

    // One native role task, without assumptions about the gap to the next queued task.
    // Animation ticks are zero-based task calls: the first Step consumes call 0.
    public sealed class BattleMovementPlan
    {
        public BattleMovementKind Kind
        {
            get;
        }

        public BattleMovementPoint From
        {
            get;
        }

        public BattleMovementPoint To
        {
            get;
        }

        public int? FlyUpTick
        {
            get;
        }

        public int? FlyDownTick
        {
            get;
        }

        public int? OverTick
        {
            get;
        }

        public float TargetBlend
        {
            get;
        }

        public float InitialBlend
        {
            get;
        }

        public BattleMovementPlan(BattleMovementKind kind, BattleMovementPoint from, BattleMovementPoint to,
            int? flyUpTick = null, int? flyDownTick = null, int? overTick = null,
            float targetBlend = 1f, float initialBlend = 1f)
        {
            if (!Enum.IsDefined(typeof(BattleMovementKind), kind) || flyUpTick < 0 || flyDownTick < 0 || overTick < 0 ||
                float.IsNaN(targetBlend) || targetBlend < 0f || targetBlend > 1f ||
                float.IsNaN(initialBlend) || initialBlend < 0f || initialBlend > 1f)
                throw new ArgumentException("Invalid native movement plan.");
            Kind = kind;
            From = from;
            To = to;
            FlyUpTick = flyUpTick;
            FlyDownTick = flyDownTick;
            OverTick = overTick;
            TargetBlend = targetBlend;
            InitialBlend = initialBlend;
        }
    }

    public sealed class BattleMovementSnapshot
    {
        public BattleMovementPlan Plan
        {
            get;
        }

        public int ElapsedTicks
        {
            get;
        }

        public double ElapsedSeconds => ElapsedTicks * BattleMovement.StepSeconds;

        public BattleMovementPoint Position
        {
            get;
        }

        public float Blend
        {
            get;
        }

        public bool Finished
        {
            get;
        }

        internal BattleMovementSnapshot(BattleMovement movement)
        {
            Plan = movement.Plan;
            ElapsedTicks = movement.ElapsedTicks;
            Position = movement.Position;
            Blend = movement.Blend;
            Finished = movement.Finished;
        }
    }

    public sealed class BattleMovement
    {
        public const double StepSeconds = 0.02d;
        private const double DegreesPerRadian = 57.295780181884766d;
        private const double TwoPi = 6.2831854820251465d;
        private const double DirectionThreshold = 1.5707963705062866d;
        private readonly bool _isPal3A;
        private bool _initialized;
        private bool _flying;
        private bool _fadingOut;
        private BattleMovementPoint _velocity;
        private float _initialDirection;
        private bool _headingAssigned;
        private float _travelDirection;
        private BattleMovementMessage? _message;
        private float _targetBlend;

        public BattleMovementPlan Plan
        {
            get;
        }

        public int ElapsedTicks
        {
            get; private set;
        }

        public double ElapsedSeconds => ElapsedTicks * StepSeconds;

        public BattleMovementPoint Position
        {
            get; private set;
        }

        public float Blend
        {
            get; private set;
        }

        public bool Finished
        {
            get; private set;
        }

        public BattleMovement(string variant, BattleMovementPlan plan)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _isPal3A = variant == "PAL3A";
            Position = plan.From;
            Blend = plan.InitialBlend;
            _targetBlend = plan.TargetBlend;
        }

        public float? FacingDegrees => _headingAssigned ? _initialDirection : null;

        public static float FacingBetween(BattleMovementPoint from, BattleMovementPoint to) => InitialHeading(Normalize(new BattleMovementPoint(to.X - from.X, to.Y - from.Y, to.Z - from.Z)));

        public static double Distance(BattleMovementPoint first, BattleMovementPoint second) => Length(Difference(first, second));

        public static BattleMovementPoint NormalizeDirection(BattleMovementPoint vector) => Normalize(vector);

        public void SetHeading(float degrees)
        {
            if (!float.IsFinite(degrees))
                throw new ArgumentOutOfRangeException(nameof(degrees));
            _initialDirection = degrees;
            _headingAssigned = true;
        }

        public BattleMovementSnapshot Capture() => new(this);

        // Original SetPos writes the shared body position without replacing the
        // currently executing task, its destination, velocity or marker state.
        public void Relocate(BattleMovementPoint position) => Position = position;

        // Session cancellation is not another original task update.
        public void Stop() => Finished = true;

        public void Step(bool alive = true) => Step(alive, null);

        public void Step(bool alive, BattleMovementMessage? message)
        {
            _message = message;
            if (Finished)
                return;
            int call = ElapsedTicks++;
            bool first = !_initialized;
            BattleMovementPoint difference = Difference(Plan.To, Position);
            if (first)
            {
                _initialized = true;
                double length = Length(difference);
                if (Plan.Kind != BattleMovementKind.EnemyFlee && length < (Plan.Kind == BattleMovementKind.FastRun ? 4f : 2f) && !(_isPal3A && Plan.Kind == BattleMovementKind.Fly))
                {
                    Position = Plan.To;
                    if (Plan.Kind == BattleMovementKind.Teleport)
                        Blend = 1f;
                    Finished = true;
                    return;
                }
                BattleMovementPoint direction = Normalize(difference);
                _travelDirection = InitialHeading(direction);
                if (Plan.Kind != BattleMovementKind.FlyRotate)
                    SetHeading(_travelDirection);
                float speed = Plan.Kind switch
                {
                    BattleMovementKind.Fly or BattleMovementKind.FlyRotate => (float)length * 2f,
                    BattleMovementKind.NpcFly => 570f,
                    BattleMovementKind.FastRun => 480f,
                    _ => 380f
                };
                if (_isPal3A && Plan.Kind == BattleMovementKind.Fly)
                {
                    // PAL3A keeps length * (1/30 float) * 50 in x87 until each component store.
                    double flightSpeed = (float)length * (double)(1f / 30f) * 50f;
                    _velocity = new BattleMovementPoint((float)(direction.X * flightSpeed), (float)(direction.Y * flightSpeed), (float)(direction.Z * flightSpeed));
                }
                else
                    _velocity = Scale(direction, speed);
                _fadingOut = true;
            }
            switch (Plan.Kind)
            {
                case BattleMovementKind.Run:
                case BattleMovementKind.EnemyFlee:
                case BattleMovementKind.NpcFly:
                case BattleMovementKind.FastRun:
                    StepRun(alive);
                    break;
                case BattleMovementKind.FlyRotate:
                    StepFlyRotate(difference, call, first, alive);
                    break;
                case BattleMovementKind.Fly:
                    if (_isPal3A)
                        StepPal3AFlight(call, first, alive);
                    else
                        StepFly(difference, call, first, alive);
                    break;
                case BattleMovementKind.Teleport:
                    StepTeleport(alive);
                    break;
                default:
                    throw new InvalidOperationException("Unknown movement kind.");
            }
        }

        public void SetTargetBlend(float blend)
        {
            if (!float.IsFinite(blend) || blend is < 0f or > 1f)
                throw new ArgumentOutOfRangeException(nameof(blend));
            _targetBlend = blend;
        }

        private bool HasMessage(BattleMovementMessage message, int? scheduled, int call) =>
            _message.HasValue ? _message.Value == message : scheduled == call;

        private void StepRun(bool alive)
        {
            Integrate();
            BattleMovementPoint remaining = Difference(Plan.To, Position);
            if (Plan.Kind != BattleMovementKind.EnemyFlee && Length(remaining) < (Plan.Kind == BattleMovementKind.FastRun ? 4f : 2f) || PassedTarget(remaining))
            {
                Position = Plan.To;
                Finished = true;
            }
            if (!alive)
                Finished = true;
        }

        private void StepFly(BattleMovementPoint difference, int call, bool first, bool alive)
        {
            // Original activation resets dwMsg=0 even when an animation marker arrived.
            bool up = !first && HasMessage(BattleMovementMessage.FlyUp, Plan.FlyUpTick, call);
            bool down = !first && HasMessage(BattleMovementMessage.FlyDown, Plan.FlyDownTick, call) && !up;
            bool over = !first && HasMessage(BattleMovementMessage.Over, Plan.OverTick, call) && !up && !down;
            if (up)
                _flying = true;
            double distanceBeforeStep = Length(difference);
            if (_flying && distanceBeforeStep > 2f)
                Integrate();
            if (down && distanceBeforeStep <= 2f)
                _flying = false;
            // The direction test deliberately uses the pre-integration vector too.
            if (PassedTarget(difference))
            {
                Position = Plan.To;
                _flying = false;
            }
            if (over)
            {
                Position = Plan.To;
                Finished = true;
            }
            if (!alive)
                Finished = true;
        }

        private void StepPal3AFlight(int call, bool first, bool alive)
        {
            bool up = !first && HasMessage(BattleMovementMessage.FlyUp, Plan.FlyUpTick, call);
            bool down = !first && HasMessage(BattleMovementMessage.FlyDown, Plan.FlyDownTick, call) && !up;
            bool over = !first && HasMessage(BattleMovementMessage.Over, Plan.OverTick, call) && !up && !down;
            if (up)
                _flying = true;
            if (down)
                _flying = false;
            if (_flying)
            {
                Integrate();
                if (Length(Difference(Plan.To, Position)) < 8f)
                {
                    Position = Plan.To;
                    _flying = false;
                }
            }
            if (over)
            {
                Position = Plan.To;
                Finished = true;
            }
            if (!alive)
                Finished = true;
        }

        private void StepFlyRotate(BattleMovementPoint difference, int call, bool first, bool alive)
        {
            if (_isPal3A)
                throw new NotSupportedException("PAL3A's thrown weapon is a separate actor, not body movement.");
            bool up = !first && HasMessage(BattleMovementMessage.FlyUp, Plan.FlyUpTick, call);
            bool down = !first && HasMessage(BattleMovementMessage.FlyDown, Plan.FlyDownTick, call) && !up;
            bool over = !first && HasMessage(BattleMovementMessage.Over, Plan.OverTick, call) && !up && !down;
            if (up)
                _flying = true;
            double distance = Length(difference);
            if (_flying && distance > 2f)
            {
                Integrate();
                SetHeading((float)(_initialDirection + StepSeconds * 720f));
            }
            if (down && distance <= 2f)
                _flying = false;
            if (PassedTarget(difference, _travelDirection))
            {
                Position = Plan.To;
                _flying = false;
            }
            if (over)
            {
                Position = Plan.To;
                Finished = true;
            }
            if (!alive)
                Finished = true;
        }

        private void StepTeleport(bool alive)
        {
            double next = Blend + (_fadingOut ? -1d : 1d) * StepSeconds * 2.5d;
            float stored = (float)next;
            if (_fadingOut)
            {
                if (next < 0d)
                {
                    _fadingOut = false;
                    Position = Plan.To;
                    if (_isPal3A)
                        stored = 0f;
                }
            }
            else if ((_isPal3A ? stored : next) >= _targetBlend)
            {
                stored = _targetBlend;
                Finished = true;
            }
            if (!alive)
            {
                stored = _targetBlend;
                Finished = true;
            }
            Blend = stored >= 1f ? 1f : stored;
        }

        private void Integrate()
        {
            // PAL3 retains Z*dt in x87 through the addition. X/Y and all PAL3A
            // components are stored as float before adding the previous position.
            float x = (float)(Position.X + (float)(_velocity.X * StepSeconds));
            float y = (float)(Position.Y + (float)(_velocity.Y * StepSeconds));
            float z = _isPal3A ? (float)(Position.Z + (float)(_velocity.Z * StepSeconds))
                : (float)(Position.Z + _velocity.Z * StepSeconds);
            Position = new BattleMovementPoint(x, y, z);
        }

        private bool PassedTarget(BattleMovementPoint difference) => PassedTarget(difference, _initialDirection);

        private static bool PassedTarget(BattleMovementPoint difference, float originalDirection)
        {
            BattleMovementPoint direction = Normalize(difference);
            float angle = (float)Math.Acos(Math.Max(-1d, Math.Min(1d, direction.Z)));
            double radians = direction.X < 0f ? TwoPi - angle : angle;
            // The original compares a degree difference with the radian constant PI/2.
            return Math.Abs(radians * DegreesPerRadian - originalDirection) >= DirectionThreshold;
        }

        private static float InitialHeading(BattleMovementPoint direction)
        {
            float angle = (float)Math.Acos(Math.Max(-1d, Math.Min(1d, direction.Z)));
            if (direction.X < 0f)
                angle = (float)(TwoPi - angle);
            return (float)(angle * DegreesPerRadian);
        }

        private static BattleMovementPoint Difference(BattleMovementPoint left, BattleMovementPoint right)
            => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        private static BattleMovementPoint Scale(BattleMovementPoint point, float scale)
            => new(point.X * scale, point.Y * scale, point.Z * scale);

        private static double Length(BattleMovementPoint point)
            => Math.Sqrt((double)point.X * point.X + (double)point.Y * point.Y + (double)point.Z * point.Z);

        private static BattleMovementPoint Normalize(BattleMovementPoint point)
        {
            double length = Math.Sqrt((double)point.X * point.X + (double)point.Y * point.Y + (double)point.Z * point.Z);
            return length < (double)0.005f ? new BattleMovementPoint(0f, 1f, 0f)
                : new BattleMovementPoint((float)(point.X / length), (float)(point.Y / length), (float)(point.Z / length));
        }
    }
}
