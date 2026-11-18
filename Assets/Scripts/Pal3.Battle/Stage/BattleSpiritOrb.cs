// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public readonly struct BattleSpiritPoint
    {
        public float X
        {
            get;
        }

        public float Y
        {
            get;
        }

        public BattleSpiritPoint(float x, float y)
        {
            if (!float.IsFinite(x) || !float.IsFinite(y))
                throw new ArgumentException("Spirit UI coordinates must be finite.");
            X = x;
            Y = y;
        }
    }

    public readonly struct BattleSpiritRect
    {
        public int Left
        {
            get;
        }

        public int Top
        {
            get;
        }

        public int Right
        {
            get;
        }

        public int Bottom
        {
            get;
        }

        public BattleSpiritPoint Center => new(Left + (Right - Left) / 2, Top + (Bottom - Top) / 2);

        public BattleSpiritRect(int left, int top, int right, int bottom)
        {
            if (right <= left || bottom <= top)
                throw new ArgumentException("Spirit UI rectangles must have positive dimensions.");
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool Reached(int x, int y) => x > Right || x >= Left && x < Right && y >= Top && y < Bottom;
    }

    public interface IBattleSpiritCaptureLayout
    {
        BattleSpiritPoint CaptureSpiritOrigin(string actorId, BattleMovementPoint world);

        BattleSpiritRect SpiritWheelTarget(int coreElement);
    }

    public sealed class BattleSpiritOrbSnapshot
    {
        public long Id
        {
            get;
        }

        public string Source
        {
            get;
        }

        public int Element
        {
            get;
        }

        public BattleSpiritPoint Position
        {
            get;
        }

        public BattleSpiritPoint Origin
        {
            get;
        }

        public BattleSpiritRect Destination
        {
            get;
        }

        public float Acceleration
        {
            get;
        }

        internal BattleSpiritOrbSnapshot(long id, string source, int element, BattleSpiritPoint position, BattleSpiritRect destination, float acceleration, BattleSpiritPoint origin)
        {
            Id = id;
            Source = source;
            Element = element;
            Position = position;
            Origin = origin;
            Destination = destination;
            Acceleration = acceleration;
        }
    }

    public sealed class BattleSpiritOrb
    {
        private float _velocityX, _velocityY;
        private int _x, _y;
        private readonly BattleSpiritRect _target;
        private readonly float _acceleration;

        public BattleSpiritPoint Position => new(_x, _y);

        public BattleSpiritPoint Origin
        {
            get;
        }

        public bool Arrived
        {
            get; private set;
        }

        public BattleSpiritOrb(BattleSpiritPoint origin, BattleSpiritRect target, float acceleration)
        {
            if (!float.IsFinite(acceleration) || acceleration <= 0)
                throw new ArgumentOutOfRangeException(nameof(acceleration));
            _x = BattleNativeArithmetic.TruncateToLowInt32(origin.X);
            _y = BattleNativeArithmetic.TruncateToLowInt32(origin.Y);
            Origin = new BattleSpiritPoint(_x, _y);
            _target = target;
            _acceleration = acceleration;
            float dx = target.Center.X - _x;
            float dy = target.Center.Y - _y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            _velocityX = length > 0 ? dx / length : 0;
            _velocityY = length > 0 ? dy / length : 0;
        }

        public bool Advance(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Arrived)
                return false;
            // Released UI integrates from the last integer coordinate before
            // accelerating the unit vector; dt scales acceleration, not position.
            _x = BattleNativeArithmetic.TruncateToLowInt32((float)(_x + (double)_velocityX));
            _y = BattleNativeArithmetic.TruncateToLowInt32((float)(_y + (double)_velocityY));
            _velocityX = (float)(_velocityX + (double)_acceleration * _velocityX * seconds);
            _velocityY = (float)(_velocityY + (double)_acceleration * _velocityY * seconds);
            // The published UI adds the target half-size again here. Keep its
            // half-open right-edge hole and immediate arrival from the right.
            Arrived = _target.Reached(_x + (_target.Right - _target.Left) / 2, _y + (_target.Bottom - _target.Top) / 2);
            return Arrived;
        }
    }
}
