// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // PAL3 C3DSpark::CreateOne/Update. Its C-runtime random stream belongs to
    // presentation and must never consume combat CUtil random draws.
    public sealed class BattleWeaponSpark
    {
        public BattleMovementPoint Position
        {
            get; private set;
        }

        public BattleMovementPoint Velocity
        {
            get; private set;
        }

        public float Luminosity { get; private set; } = 200;

        public const float Size = 8;

        public bool Alive => Position.Y > -Size && Luminosity > 1 && Luminosity <= 255;

        public byte Alpha => (byte)Math.Clamp((int)Math.Round(Luminosity, MidpointRounding.ToEven), 0, 255);

        public BattleWeaponSpark(BattleMovementPoint position, BattleRandom visualRandom)
        {
            if (visualRandom == null)
                throw new ArgumentNullException(nameof(visualRandom));
            Position = position;
            int power = visualRandom.Next() % 10;
            int degrees = visualRandom.Next() % 5760 / 16;
            double angle = degrees * Math.PI / 180;
            Velocity = new BattleMovementPoint(power * (float)Math.Sin(angle), -10, power * (float)Math.Cos(angle));
        }

        public void Advance(float delta)
        {
            if (!float.IsFinite(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            Velocity = new BattleMovementPoint(Velocity.X, Velocity.Y - delta * 20, Velocity.Z);
            Position = new BattleMovementPoint(Position.X + delta * Velocity.X, Position.Y + delta * Velocity.Y, Position.Z + delta * Velocity.Z);
            Luminosity -= delta * 250;
        }
    }
}
