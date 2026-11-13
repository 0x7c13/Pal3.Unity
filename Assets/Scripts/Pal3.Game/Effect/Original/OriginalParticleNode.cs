// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using Core.DataReader.Data;
    using Core.DataReader.Par;
    using Core.Primitives;
    using UnityEngine;
    using UnityEngine.Rendering;

    // sgpar's sampled curves and drag do not match Shuriken modules exactly.
    // Simulate those semantics in C#, and use the native particle renderer.
    // This is PAL3's CEffgbParticle -> ParticleEffect/particle.cpp path, not
    // gbengine9/physics/gbparteffect.cpp: the combat implementation scales both
    // scope and displacement and forces ONE/ONE blending regardless of BlendMode.
    internal sealed class OriginalParticleNode : OriginalEffectNode
    {
        private struct Unit
        {
            public Vector3 Position, Velocity;
            public float Age, Life, Size, Rotation, Direction;
            public int Row;
            public bool Alive;
        }

        private sealed class Controller
        {
            public ParticleDescription Particle;
            public ParticleEmitter Emitter;
            public ParticleShapeFile Shape;
            public ParticleSystem System;
            public ParticleSystem.Particle[] Output;
            public Unit[] Units;
            public float CycleTime, Accumulator;
            public int Count;
            public bool Emitting = true;
        }

        private readonly ParticleFile _file;
        private readonly Controller[] _controllers;
        private readonly System.Random _random;
        private float _spaceScale;
        private bool _loopEmission;

        public override int QuadCount
        {
            get
            {
                int n = 0;
                foreach (Controller c in _controllers)
                    n += c.Count;
                return n;
            }
        }

        public OriginalParticleNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth,
            OriginalEffectResources resources, uint seed, OriginalEffectOverrides edits) : base(root, d, tasks, origin, birth)
        {
            _random = new System.Random(unchecked((int)seed));
            _file = resources.Particle(d.ParticleFileName, edits);
            _spaceScale = d.InitSize;
            _controllers = new Controller[_file.Controllers.Length];
            for (int i = 0; i < _controllers.Length; i++)
            {
                ParticleController source = _file.Controllers[i];
                var c = new Controller { Particle = _file.Particles[source.ParticleIndex], Emitter = _file.Emitters[source.EmitterIndex] };
                int capacity = Mathf.Clamp(_file.Capacity, 1, 65536);
                c.Units = new Unit[capacity];
                c.Output = new ParticleSystem.Particle[capacity];
                var child = new GameObject(c.Particle.Name) { hideFlags = HideFlags.DontSave, layer = root.layer };
                child.transform.SetParent(root.transform, false);
                c.System = child.AddComponent<ParticleSystem>();
                c.System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = c.System.main;
                main.playOnAwake = false;
                main.maxParticles = capacity;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.gravityModifier = 0;
                main.startSpeed = 0;
                main.startLifetime = 10000;
                main.startSize = 1;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                c.System.useAutoRandomSeed = false;
                c.System.randomSeed = seed + (uint)i;
                var emission = c.System.emission;
                emission.enabled = false;
                var shape = c.System.shape;
                shape.enabled = false;
                var renderer = c.System.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = resources.Material(OriginalEffectResources.RelativeTo(d.ParticleFileName, c.Particle.Texture), true, false, 1);
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                renderer.minParticleSize = 0;
                renderer.maxParticleSize = 100;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortMode = ParticleSystemSortMode.None;
                var atlas = c.System.textureSheetAnimation;
                atlas.enabled = true;
                atlas.numTilesX = c.Particle.Columns;
                atlas.numTilesY = c.Particle.Rows;
                atlas.animation = ParticleSystemAnimationType.WholeSheet;
                atlas.cycleCount = 1;
                atlas.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1));
                // A stopped system can discard externally supplied particles in
                // editor preview. Keep it initialized and paused; C# owns time.
                c.System.Play(false);
                c.System.Pause(false);
                if (c.Emitter.Shape == 2)
                    c.Shape = resources.Shape(OriginalEffectResources.RelativeTo(d.ParticleFileName, c.Emitter.ShapePath));
                _controllers[i] = c;
            }
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            if (clock.StartedNow)
                _loopEmission = true;
            Position += new Vector3(t.SpeedX, t.SpeedY, t.SpeedZ) * dt;
            _spaceScale = Mathf.Max(0.001f, _spaceScale + t.XStretchSpeed * dt);
            Vector3 axis = new(t.CircleAxisX, t.CircleAxisY, t.CircleAxisZ);
            Orbit = OrbitPosition(axis, (LocalTime - clock.StartedAt) * t.CircleSpeed, t.CircleRadius);
        }

        public override void Step(float time, float dt)
        {
            foreach (Controller c in _controllers)
            {
                ParticleEmitter e = c.Emitter;
                ParticleDescription p = c.Particle;
                float cycle = Mathf.Max(0.000001f, e.CycleMin - e.CycleMax);
                c.CycleTime += dt;
                // sgpar starts with one active emitter cycle. CEffgbParticle
                // updates it before the tasks, which control future looping;
                // disabling looping lets the current cycle finish normally.
                if (c.CycleTime > cycle)
                {
                    c.Emitting = _loopEmission;
                    if (_loopEmission)
                        c.CycleTime = 0;
                }
                float phase = c.CycleTime / cycle;
                int lo = (int)(e.Emission.Evaluate(phase, 0) * e.SpeedMultiplier);
                int hi = (int)(e.Emission.Evaluate(phase, 1) * e.SpeedMultiplier);
                if (c.Emitting)
                {
                    c.Accumulator += RandomRange(lo, hi) * dt;
                    int number = Mathf.Clamp((int)c.Accumulator, 0, c.Units.Length);
                    c.Accumulator -= number;
                    for (int n = 0; n < number; n++)
                    {
                        int slot = Array.FindIndex(c.Units, u => !u.Alive);
                        if (slot < 0)
                        {
                            if (_file.Overflow == 0)
                                break;
                            slot = _file.Overflow == 2 ? _random.Next(c.Units.Length) : Oldest(c.Units);
                        }
                        c.Units[slot] = Spawn(c, phase);
                    }
                }
                c.Count = 0;
                for (int i = 0; i < c.Units.Length; i++)
                {
                    ref Unit u = ref c.Units[i];
                    if (!u.Alive)
                        continue;
                    u.Age += dt;
                    if (u.Age > u.Life)
                    {
                        u.Alive = false;
                        continue;
                    }
                    float t = u.Age / u.Life;
                    if (u.Velocity.magnitude > p.NoResistanceSpeed)
                    {
                        Vector3 drag = u.Velocity * p.Resistance;
                        if (p.SizeResistancePower != 0)
                            drag += u.Velocity.normalized * (u.Size * p.Resistance);
                        u.Velocity -= drag * dt;
                    }
                    u.Size = p.Size.Evaluate(t);
                    u.Rotation = (u.Rotation + (p.Rotation.Evaluate(t) + RandomRange(p.RotationStart, p.RotationEnd)) * dt) * u.Direction;
                    Vector3 offset = u.Velocity * dt;
                    offset.y -= p.Gravity * dt * dt / 2;
                    u.Velocity.y -= p.Gravity * dt;
                    u.Position += offset * _spaceScale;
                    c.Count++;
                }
            }
            base.Step(time, dt);
            if (!Alive)
                _loopEmission = false;
            foreach (Controller c in _controllers)
                Alive |= c.Count > 0;
        }

        private Unit Spawn(Controller c, float t)
        {
            ParticleEmitter e = c.Emitter;
            ParticleDescription p = c.Particle;
            Vector3 direction = new(e.Direction.Evaluate(t, 0), e.Direction.Evaluate(t, 1), e.Direction.Evaluate(t, 2));
            Vector3 scope = Raw(e.Scope) * _spaceScale;
            Vector3 velocity = direction * RandomRange(e.RandomVelocity.Evaluate(t, 0), e.RandomVelocity.Evaluate(t, 1))
                + new Vector3(RandomRange(-scope.x, scope.x), RandomRange(-scope.y, scope.y), RandomRange(-scope.z, scope.z));
            Vector3 position;
            if (e.Shape == 2)
                position = Raw(c.Shape.Points[_random.Next(c.Shape.Points.Length)]);
            else if (e.Shape == 1)
                position = new Vector3(RandomRange(-100, 100), RandomRange(-100, 100), RandomRange(-100, 100)).normalized
                    * RandomRange(0, _random.Next(2) == 0 ? e.Hotspot : e.Hotspot + e.Falloff);
            else
                position = new Vector3(RandomRange(e.BoxMin.X, e.BoxMax.X), RandomRange(e.BoxMin.Y, e.BoxMax.Y), RandomRange(e.BoxMin.Z, e.BoxMax.Z));
            position = (position + Raw(e.Position)) * _spaceScale;
            return new Unit
            {
                Alive = true,
                Position = position,
                Velocity = velocity,
                Direction = p.RandomRotationDirection && _random.Next(2) == 0 ? -1 : 1,
                Life = Mathf.Max(0.000001f, RandomRange(e.Life.Evaluate(t, 0), e.Life.Evaluate(t, 1))),
                Size = p.Size.Evaluate(0),
                Row = _random.Next(p.Rows)
            };
        }

        public override void Render(Camera camera)
        {
            foreach (Controller c in _controllers)
            {
                int n = 0;
                ParticleDescription p = c.Particle;
                for (int i = 0; i < c.Units.Length; i++)
                {
                    Unit u = c.Units[i];
                    if (!u.Alive)
                        continue;
                    float phase = u.Age / u.Life;
                    int frame = Mathf.Min(p.Columns - 1, (int)(phase * p.Columns));
                    // Native texture-sheet lookup uses normalized remaining life.
                    float atlasLife = (u.Row * p.Columns + frame + 0.25f) / (p.Rows * p.Columns);
                    var color = new UnityEngine.Color(p.Color.Evaluate(phase, 0) / 255, p.Color.Evaluate(phase, 1) / 255,
                        p.Color.Evaluate(phase, 2) / 255, p.Alpha.Evaluate(phase));
                    // CEffgbParticle translates the view for the entire effect,
                    // even for sgpar's world-space mode. Task motion therefore
                    // moves existing particles as well as newly emitted ones.
                    c.Output[n++] = new ParticleSystem.Particle
                    {
                        position = UnityPosition(u.Position + Position + Orbit),
                        startSize = Mathf.Abs(u.Size * _spaceScale / 20),
                        rotation = -u.Rotation,
                        startColor = color,
                        startLifetime = 1,
                        remainingLifetime = 1 - atlasLife,
                        randomSeed = (uint)(i + 1)
                    };
                }
                c.System.SetParticles(c.Output, n);
            }
        }

        private float RandomRange(float a, float b) => a + (b - a) * ((float)_random.Next(1, 10001) / 10000);

        private static Vector3 Raw(GameBoxVector3 v) => new(v.X, v.Y, v.Z);

        private static int Oldest(Unit[] units)
        {
            int result = 0;
            for (int i = 1; i < units.Length; i++)
                if (units[i].Age > units[result].Age)
                    result = i;
            return result;
        }
    }
}
