// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // PAL3A's second actor travels independently of its owner's body queue.
    public sealed class BattleThrownWeapon
    {
        private const float Delta = .02f;
        private BattleMovementPoint _segmentOrigin;
        private float _distance;
        private float _speed = 680f;
        private bool _initialized;

        public BattleMovementPoint Position
        {
            get; private set;
        }

        public BattleMovementPoint Direction
        {
            get; private set;
        }

        public BattleMovementPoint Rotation
        {
            get; private set;
        }

        public int State
        {
            get; private set;
        }

        public bool Active
        {
            get; private set;
        }

        public bool WeaponVisible { get; private set; } = true;

        public bool BodyFinished
        {
            get; private set;
        }

        public bool RestoreRequested
        {
            get; private set;
        }

        public bool HitThisPass
        {
            get; private set;
        }

        public float Speed => _speed;

        public float Distance => _distance;

        public BattleThrownWeapon(BattleMovementPoint source, BattleMovementPoint destination)
        {
            BattleMovementPoint direction = BattleMovement.NormalizeDirection(new BattleMovementPoint(destination.X - source.X, 36f - source.Y, destination.Z - source.Z));
            Position = new BattleMovementPoint((float)(source.X + direction.X * 68d), 36f, source.Z + direction.Z * 68f);
            _segmentOrigin = Position;
            BattleMovementPoint difference = new(destination.X - Position.X, 36f - Position.Y, destination.Z - Position.Z);
            _distance = (float)BattleMovement.Distance(default, difference);
            Direction = BattleMovement.NormalizeDirection(difference);
        }

        public void AdvanceBody(BattlePhysicalMarkerKind? marker, bool alive)
        {
            if (BodyFinished)
                return;
            _initialized = true;
            if (marker == BattlePhysicalMarkerKind.Throwed && !Active)
            {
                Active = true;
                WeaponVisible = false;
                double angle = Math.Acos(Math.Clamp(Direction.Z, -1f, 1f));
                if (Direction.X < 0)
                    angle = 6.2831854820251465d - angle;
                Rotation = new BattleMovementPoint(0, (float)(angle * 57.295780181884766d), 0);
                BodyFinished = true;
            }
            else if (marker == BattlePhysicalMarkerKind.Over && Active || !alive)
            {
                BodyFinished = true;
                if (!alive)
                    WeaponVisible = true;
            }
        }

        public void Advance(bool alive)
        {
            HitThisPass = false;
            if (!_initialized || !Active)
                return;
            if (!alive)
            {
                Active = false;
                WeaponVisible = true;
                return;
            }
            if (State == 2)
            {
                Active = false;
                RestoreRequested = true;
                return;
            }
            float vx = Direction.X * _speed;
            float vy = Direction.Y * _speed;
            if (State == 0)
                Position = new BattleMovementPoint(Position.X + vx * Delta, Position.Y + vy * Delta,
                    (float)(Position.Z + Direction.Z * (double)_speed * Delta));
            else
                Position = new BattleMovementPoint((float)(Position.X - vx * (double)Delta), Position.Y - vy * Delta,
                    Position.Z - (float)(Direction.Z * (double)_speed * Delta));
            double distance = BattleMovement.Distance(Position, _segmentOrigin);
            if (_distance <= 0)
                _distance = 1f;
            double acceleration = 680d / _distance * Delta * 200d;
            _speed = (float)(_speed + (State == 0 ? -acceleration : acceleration));
            double spin = Rotation.X + (State == 0 ? 1d : -1d) * Delta * 3600d;
            if (State == 0 ? spin > 360d : spin < 0d)
                spin += State == 0 ? -360d : 360d;
            Rotation = new BattleMovementPoint((float)spin, Rotation.Y, Rotation.Z);
            if (distance < _distance)
                return;
            _segmentOrigin = Position;
            State++;
            HitThisPass = State == 1;
        }

        public void FinishRestore()
        {
            WeaponVisible = true;
            Active = false;
            RestoreRequested = false;
        }
    }
}
