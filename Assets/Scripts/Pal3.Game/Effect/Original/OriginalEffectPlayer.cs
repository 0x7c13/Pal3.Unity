// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Data;
    using UnityEngine;

    // One simulation serves both clocks: real frame time in game, and fixed
    // steps for editor playback/seeking. Rendering never advances the clock.
    public sealed class OriginalEffectPlayer : MonoBehaviour
    {
        public const float FixedStep = 0.02f;
        private readonly List<OriginalEffectNode> _nodes = new();
        private OriginalEffectResources _resources;
        private OriginalEffectOverrides _overrides;
        private int _groupId;
        private double _time;
        private uint _seed;
        private uint _spawnSequence;
        private float _accumulator;
        private Action<string> _sound;
        private bool _disposed, _suppressEvents;
        private float _lastRepeat;

        public float Time => (float)_time;
        public bool AutoAdvance { get; set; } = true;

        public bool Paused
        {
            get; set;
        }

        public float Speed { get; set; } = 1;

        public bool Loop
        {
            get; set;
        }

        public bool DestroyOnComplete
        {
            get; set;
        }

        public float RepeatInterval
        {
            get; set;
        }

        public int NodeCount => _nodes.Count;

        public int QuadCount
        {
            get; private set;
        }

        public bool IsAlive
        {
            get; private set;
        }

        public string Error
        {
            get; private set;
        }

        public void Initialize(OriginalEffectResources resources, int groupId, uint seed = 1,
            OriginalEffectOverrides overrides = null, Action<string> sound = null)
        {
            if (overrides != null && overrides.Version != 1)
                throw new NotSupportedException($"Unknown original effect override version {overrides.Version}");
            _resources = resources;
            _groupId = groupId;
            _seed = seed == 0 ? 1 : seed;
            _overrides = overrides;
            _sound = sound;
            ResetSimulation();
        }

        private void ResetSimulation(bool resetRandom = true)
        {
            ClearNodes();
            _time = 0;
            _accumulator = 0;
            _lastRepeat = 0;
            Error = null;
            if (resetRandom)
                _spawnSequence = 0;
            try
            {
                AddGroup(_groupId, Vector3.zero);
                IsAlive = _nodes.Count > 0;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                ClearNodes();
                IsAlive = false;
            }
        }

        internal void AddGroup(int groupId, Vector3 position)
        {
            if (groupId < 0 || groupId >= _resources.Definitions.Groups.Length)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            EffectGroup group = _resources.Definitions.Groups[groupId];
            for (int i = 0; i < group.Objects.Length; i++)
            {
                EffectGroupObject obj = group.Objects[i];
                _resources.Definitions.TryGetType(obj.TypeCrc, out EffectDefinition d);
                EffectTask[] tasks = obj.Tasks;
                if (!OriginalEffectOverrides.Resolve(_overrides, groupId, i, ref d, ref tasks))
                    continue;
                if (_nodes.Count >= 2048)
                    throw new InvalidOperationException("Original effect object limit (2048) reached");
                obj = new EffectGroupObject { TypeCrc = obj.TypeCrc, Tasks = tasks };
                var root = new GameObject($"{groupId}:{i} {d.TypeName}") { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
                root.transform.SetParent(transform, false);
                OriginalEffectNode node = null;
                try
                {
                    string kind = EffectClass.Name(d.ClassNameCrc);
                    uint seed = _seed + _spawnSequence++ * 7919 + (uint)(groupId * 104729 + i);
                    node = kind switch
                    {
                        "callback" or "audio" => new OriginalControlNode(root, d, obj.Tasks, position, Time, this, kind),
                        "quad" => new OriginalQuadNode(root, d, obj.Tasks, position, Time, _resources),
                        "particle" => new OriginalParticleNode(root, d, obj.Tasks, position, Time, _resources, seed, _overrides),
                        "cylinder" or "grid" => new OriginalGeometryNode(root, d, obj.Tasks, position, Time, _resources, kind),
                        "meteor" or "shock" => new OriginalTrailNode(root, d, obj.Tasks, position, Time, _resources, kind, seed),
                        "model" => new OriginalModelNode(root, d, obj.Tasks, position, Time, _resources),
                        _ => throw new NotSupportedException($"Original effect class {kind}: {d.TypeName}"),
                    };
                    _nodes.Add(node);
                }
                catch
                {
                    node?.Dispose();
                    OriginalEffectResources.Destroy(root);
                    throw;
                }
            }
        }

        internal void Sound(string name)
        {
            if (!_suppressEvents)
                _sound?.Invoke(name);
        }

        public void Advance(float delta)
        {
            float elapsed = ScaledDelta(delta);
            if (elapsed <= 0)
                return;
            _accumulator += elapsed;
            while (_accumulator + 0.000001f >= FixedStep)
            {
                _accumulator -= FixedStep;
                if (!SimulateStep(FixedStep))
                    break;
            }
        }

        public void AdvanceFrame(float delta)
        {
            float elapsed = ScaledDelta(delta);
            if (elapsed <= 0)
                return;
            elapsed += _accumulator;
            _accumulator = 0;
            SimulateStep(elapsed);
        }

        private float ScaledDelta(float delta)
        {
            if (_disposed || Paused || Error != null || delta <= 0 || !float.IsFinite(delta))
                return 0;
            float elapsed = delta * (float.IsFinite(Speed) ? Mathf.Clamp(Speed, 0, 16) : 1);
            return float.IsFinite(elapsed) ? elapsed : 0;
        }

        private bool SimulateStep(float delta)
        {
            _time += delta;
            try
            {
                int count = _nodes.Count;
                bool alive = false;
                for (int i = 0; i < count; i++)
                {
                    _nodes[i].Step(Time, delta);
                    alive |= _nodes[i].Alive;
                }
                IsAlive = alive || _nodes.Count > count;
                for (int i = _nodes.Count - 1; i >= 0; i--)
                    if (!_nodes[i].Alive)
                    {
                        _nodes[i].Dispose();
                        _nodes.RemoveAt(i);
                    }
                if (Loop && RepeatInterval > 0 && Time - _lastRepeat >= RepeatInterval)
                {
                    AddGroup(_groupId, Vector3.zero);
                    _lastRepeat = Time;
                    IsAlive = true;
                }
                if (Loop && !IsAlive && RepeatInterval <= 0)
                {
                    ResetSimulation(false);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                ClearNodes();
                IsAlive = false;
                Debug.LogException(ex);
                return false;
            }
        }

        public void Seek(float time)
        {
            if (_disposed)
                return;
            time = Mathf.Clamp(float.IsFinite(time) ? time : 0, 0, 600);
            bool paused = Paused, loop = Loop;
            float speed = Speed;
            ResetSimulation();
            Paused = false;
            Loop = false;
            Speed = 1;
            _suppressEvents = true;
            try
            {
                Advance(time);
            }
            finally
            {
                _suppressEvents = false;
                Paused = paused;
                Loop = loop;
                Speed = speed;
            }
        }

        public void Render(Camera camera)
        {
            QuadCount = 0;
            foreach (OriginalEffectNode node in _nodes)
            {
                node.Render(camera);
                QuadCount += node.QuadCount;
            }
        }

        private void Update()
        {
            if (!AutoAdvance)
                return;
            AdvanceFrame(UnityEngine.Time.deltaTime);
            if (DestroyOnComplete && !Loop && !IsAlive && Time > 0)
                OriginalEffectResources.Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (AutoAdvance)
                Render(Camera.main);
        }

        private void ClearNodes()
        {
            foreach (OriginalEffectNode node in _nodes)
                node.Dispose();
            _nodes.Clear();
            QuadCount = 0;
        }

        private void OnDestroy()
        {
            _disposed = true;
            ClearNodes();
        }
    }
}
