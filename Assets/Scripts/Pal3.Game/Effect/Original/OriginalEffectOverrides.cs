// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Data;
    using Core.DataReader.Par;
    using UnityEngine;

    [Serializable]
    public sealed class OriginalEffectObjectOverride
    {
        public int GroupId, ObjectIndex;
        public EffectDefinition Definition;
        public EffectTask[] Tasks;
        public bool Hidden;
    }

    [Serializable]
    public sealed class OriginalParticleCurveOverride
    {
        public string File, Parameter;
        public bool Emitter;
        public int Index;
        public AnimationCurve[] Channels;
    }

    [CreateAssetMenu(menuName = "PAL3/Original effect overrides")]
    public sealed class OriginalEffectOverrides : ScriptableObject
    {
        public int Version = 1;
        public List<OriginalEffectObjectOverride> Objects = new();
        public List<OriginalParticleCurveOverride> ParticleCurves = new();

        public OriginalEffectObjectOverride Find(int group, int index) =>
            Objects.Find(o => o.GroupId == group && o.ObjectIndex == index);

        internal static bool Resolve(OriginalEffectOverrides edits, int group, int index,
            ref EffectDefinition definition, ref EffectTask[] tasks)
        {
            if (edits != null && edits.Version != 1)
                throw new NotSupportedException($"Unknown original effect override version {edits.Version}");
            OriginalEffectObjectOverride edit = edits?.Find(group, index);
            if (edit?.Hidden == true)
                return false;
            if (edit != null)
            {
                definition = edit.Definition;
                tasks = edit.Tasks ?? throw new InvalidOperationException("Missing effect override tasks");
                ValidateNumbers(new[] { definition.InitXPosition, definition.InitYPosition, definition.InitZPosition,
                    definition.InitXRotation, definition.InitYRotation, definition.InitZRotation, definition.InitSize,
                    definition.FramePerSecond, definition.RotationAngle, definition.Width, definition.DivisionCount,
                    definition.TopShrink, definition.BottomShrink, definition.TopRadius, definition.BottomRadius,
                    definition.TopRadiusIncreaseSpeed, definition.BottomRadiusIncreaseSpeed, definition.TopRiseSpeed,
                    definition.BottomRiseSpeed, definition.TextureRepeat });
                if (tasks.Length > 8)
                    throw new InvalidOperationException("An original effect object supports at most eight tasks");
                foreach (EffectTask t in tasks.Where(t => t != null && t.IsValid))
                {
                    ValidateNumbers(new[] { t.XStretchSpeed, t.SpeedX, t.SpeedY, t.SpeedZ, t.SpeedRot,
                        t.CircleAxisX, t.CircleAxisY, t.CircleAxisZ, t.CircleRadius, t.CircleSpeed,
                        t.RotAxisX, t.RotAxisY, t.RotAxisZ, t.RotSpeed, t.SpeedAlpha, t.SpeedRed,
                        t.SpeedGreen, t.SpeedBlue, t.StartTime, t.Duration, t.CreateTimeSlot, t.YStretchSpeed });
                    if (t.Duration < 0)
                        throw new InvalidOperationException("An effect task duration cannot be negative");
                }
            }
            tasks = tasks.Where(t => t != null && t.IsValid).ToArray();
            return true;
        }

        private static void ValidateNumbers(IEnumerable<float> values)
        {
            if (values.Any(v => !float.IsFinite(v)))
                throw new InvalidOperationException("Effect overrides require finite numeric parameters");
        }

        public bool HasParticleEdits(string file) => ParticleCurves.Exists(c =>
            string.Equals(OriginalEffectResources.Normalize(c.File), OriginalEffectResources.Normalize(file), StringComparison.OrdinalIgnoreCase));

        public void ApplyParticle(string path, ParticleFile file)
        {
            foreach (OriginalParticleCurveOverride edit in ParticleCurves)
            {
                if (!string.Equals(OriginalEffectResources.Normalize(edit.File), OriginalEffectResources.Normalize(path), StringComparison.OrdinalIgnoreCase))
                    continue;
                ParticleCurve curve = FindCurve(file, edit.Emitter, edit.Index, edit.Parameter);
                if (edit.Channels == null || edit.Channels.Length != curve.Channels || edit.Channels.Any(c => c == null))
                    throw new InvalidOperationException("Particle curve channel count does not match original data");
                foreach (AnimationCurve channel in edit.Channels)
                {
                    ValidateNumbers(channel.keys.SelectMany(k => new[] { k.time, k.value }));
                    // Infinite tangents are valid stepped keys. Evaluate the
                    // normalized interval to reject curves that produce NaN/Inf.
                    ValidateNumbers(Enumerable.Range(0, 101).Select(i => channel.Evaluate(i / 100f)));
                }
                curve.OverrideEvaluation = (time, channel) => channel >= 0 && channel < edit.Channels.Length ? edit.Channels[channel].Evaluate(time) : 0;
            }
        }

        public static ParticleCurve FindCurve(ParticleFile file, bool emitter, int index, string name)
        {
            if (emitter)
            {
                ParticleEmitter e = file.Emitters[index];
                return name switch
                {
                    "Direction" => e.Direction,
                    "RandomVelocity" => e.RandomVelocity,
                    "Emission" => e.Emission,
                    "Life" => e.Life,
                    "Size" => e.Size,
                    "Rotation" => e.Rotation,
                    _ => throw new ArgumentException(name)
                };
            }
            ParticleDescription p = file.Particles[index];
            return name switch
            {
                "Size" => p.Size,
                "Rotation" => p.Rotation,
                "Alpha" => p.Alpha,
                "Color" => p.Color,
                _ => throw new ArgumentException(name)
            };
        }
    }
}
