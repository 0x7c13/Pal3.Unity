// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using Core.DataReader.Data;
    using Core.DataReader.Par;
    using UnityEngine;

    internal sealed class OriginalSparNode : OriginalEffectNode
    {
        private readonly SparFile _file;
        private readonly OriginalSparEmitter[] _emitters;
        private readonly EffectMeshBuffer[] _meshes;
        private bool _running;
        private float _dt;

        public OriginalSparNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth,
            OriginalEffectResources resources, uint seed) : base(root, d, tasks, origin, birth)
        {
            _file = resources.Spar(d.ParticleFileName);
            _emitters = new OriginalSparEmitter[_file.Groups.Length];
            _meshes = new EffectMeshBuffer[_emitters.Length];
            try
            {
                for (int i = 0; i < _emitters.Length; i++)
                {
                    SparGroup group = _file.Groups[i];
                    _emitters[i] = new OriginalSparEmitter(group, seed + (uint)i);
                    var child = new GameObject(group.Name) { hideFlags = HideFlags.DontSave, layer = root.layer };
                    child.transform.SetParent(root.transform, false);
                    _meshes[i] = new EffectMeshBuffer(child, resources.SparMaterial(OriginalEffectResources.RelativeTo(d.ParticleFileName, group.Texture), _file.Shader), reverseWinding: true);
                }
            }
            catch
            {
                foreach (EffectMeshBuffer mesh in _meshes)
                    mesh?.Dispose();
                throw;
            }
        }

        public override int QuadCount
        {
            get
            {
                int count = 0;
                foreach (EffectMeshBuffer mesh in _meshes)
                    count += mesh.QuadCount;
                return count;
            }
        }

        public override void Step(float time, float dt)
        {
            // CEffSParticle updates the system before processing effect tasks.
            _dt = dt;
            if (_running)
                foreach (OriginalSparEmitter emitter in _emitters)
                    emitter.Step(dt);
            base.Step(time, dt);
        }

        protected override void Apply(EffectTask task, OriginalTaskClock clock, float dt)
        {
            if (clock.StartedNow)
            {
                foreach (OriginalSparEmitter emitter in _emitters)
                    emitter.Start();
                _running = true;
            }
            // SPar's adapter uses the complete frame delta, ignores stretch,
            // task rotation/color and InitSize, and applies translation at draw.
            Position += new Vector3(task.SpeedX, task.SpeedY, task.SpeedZ) * _dt;
            Orbit = OrbitPosition(new Vector3(task.CircleAxisX, task.CircleAxisY, task.CircleAxisZ),
                (LocalTime - clock.StartedAt) * task.CircleSpeed, task.CircleRadius);
            if (clock.Finished)
            {
                _running = false; // native Stop clears the pool immediately
                foreach (OriginalSparEmitter emitter in _emitters)
                    emitter.Stop();
            }
        }

        public override void Render(Camera camera)
        {
            Vector3 right = camera == null ? Vector3.right : Root.transform.InverseTransformVector(camera.transform.right);
            Vector3 up = camera == null ? Vector3.up : Root.transform.InverseTransformVector(camera.transform.up);
            Quaternion rotation = RawRotation(Rotation);
            for (int i = 0; i < _emitters.Length; i++)
            {
                EffectMeshBuffer mesh = _meshes[i];
                mesh.Clear();
                if (Visible && _running)
                {
                    OriginalSparEmitter emitter = _emitters[i];
                    for (int j = 0; j < emitter.Count; j++)
                    {
                        OriginalSparEmitter.Unit u = emitter.Units[j];
                        Vector3 center = UnityPosition(Position + Orbit + rotation * u.Position);
                        float sin = Mathf.Sin(u.Rotation), cos = Mathf.Cos(u.Rotation);
                        // Native SPar builds the quad directly in view space:
                        // positive radians, top-left (su,sv), unlike sgpar.
                        Vector3 x = (right * cos + up * sin) * (u.Size / 20);
                        Vector3 y = (-right * sin + up * cos) * (u.Size / 20);
                        float[] uv = _file.Groups[i].TextureFrames[u.Frame];
                        var color = new Color32((byte)(Mathf.Clamp01(u.Color.x) * 255), (byte)(Mathf.Clamp01(u.Color.y) * 255),
                            (byte)(Mathf.Clamp01(u.Color.z) * 255), (byte)(Mathf.Clamp01(u.Alpha) * 255));
                        mesh.Quad(center - x - y, center + x - y, center + x + y, center - x + y,
                            new Vector2(uv[0], 1 - uv[3]), new Vector2(uv[2], 1 - uv[1]), color);
                    }
                }
                mesh.Upload();
            }
        }

        public override void Dispose()
        {
            foreach (EffectMeshBuffer mesh in _meshes)
                mesh?.Dispose();
            base.Dispose();
        }
    }
}
