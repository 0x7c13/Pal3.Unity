// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Data
{
    using System;
    using System.Collections.Generic;
    using Cpk;
    using Primitives;

    // The names describe the quad interpretation of the original unions.
    // Callback/audio/model adapters interpret these same fields by class.
    [Serializable]
    public sealed class EffectTask
    {
        public float XStretchSpeed, SpeedX, SpeedY, SpeedZ, SpeedRot;
        public float CircleAxisX, CircleAxisY, CircleAxisZ, CircleRadius, CircleSpeed;
        public float RotAxisX, RotAxisY, RotAxisZ, RotSpeed;
        public float SpeedAlpha, SpeedRed, SpeedGreen, SpeedBlue, StartTime, Duration;
        public int RequireLoops;
        public float CreateTimeSlot;
        public bool IsValid, CreateNew, ModifyLast;
        public float YStretchSpeed;

        public GameBoxVector3 Speed => new(SpeedX, SpeedY, SpeedZ);
        public GameBoxVector3 CircleAxis => new(CircleAxisX, CircleAxisY, CircleAxisZ);

        public IEnumerable<(int groupId, GameBoxVector3 position)> Callbacks()
        {
            if (XStretchSpeed != 0)
                yield return ((int)MathF.Round(XStretchSpeed), new(SpeedX, SpeedY, SpeedZ));
            if (CircleRadius != 0)
                yield return ((int)MathF.Round(CircleRadius), new(RotAxisX, RotAxisY, RotAxisZ));
            if (CircleAxisX != 0)
                yield return ((int)MathF.Round(CircleAxisX), new(CircleAxisY, CircleAxisZ, CircleSpeed));
            if (SpeedAlpha != 0)
                yield return ((int)MathF.Round(SpeedAlpha), new(SpeedRed, SpeedGreen, SpeedBlue));
        }

        public IEnumerable<(int index, float delay)> Sounds()
        {
            if (XStretchSpeed != 0)
                yield return ((int)MathF.Round(XStretchSpeed), SpeedX);
            if (SpeedY != 0)
                yield return ((int)MathF.Round(SpeedY), SpeedZ);
            if (CircleRadius != 0)
                yield return ((int)MathF.Round(CircleRadius), RotAxisX);
            if (RotAxisY != 0)
                yield return ((int)MathF.Round(RotAxisY), RotAxisZ);
        }
    }

    [Serializable]
    public sealed class EffectGroupObject
    {
        public uint TypeCrc;
        public EffectTask[] Tasks = Array.Empty<EffectTask>();
    }

    [Serializable]
    public sealed class EffectGroup
    {
        public EffectGroupObject[] Objects = Array.Empty<EffectGroupObject>();
    }

    public static class EffectClass
    {
        private static readonly Dictionary<uint, string> Names = CreateNames();

        private static Dictionary<uint, string> CreateNames()
        {
            var hash = new Crc32Hash();
            hash.Init();
            var result = new Dictionary<uint, string>();
            foreach (string name in new[] { "quad", "particle", "cylinder", "model", "callback", "audio",
                         "meteor", "shock", "grid", "explosive", "action", "camera", "time", "bink", "quake", "S-Particle" })
                result.Add(hash.Compute(name, 936), name);
            return result;
        }

        public static string Name(uint crc) => Names.TryGetValue(crc, out string name) ? name : $"unknown:{crc:X8}";
    }
}
