// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Runtime
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Data;
    using Rendering.Native;
    using UnityEngine;

    // One simulation serves both clocks: real frame time in game, and fixed
    // steps for editor playback/seeking. Rendering never advances the clock.
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Pal3.Game.Effect.Original", "Pal3.Game", "OriginalEffectPlayer")]
    public sealed class EffectPlayer : MonoBehaviour, IDisposable
    {
        public const float FixedStep = 0.02f;
        private readonly List<EffectNode> _nodes = new();
        private EffectResources _resources;
        private EffectOverrides _overrides;
        private int _groupId;
        private double _time;
        private uint _seed;
        private uint _spawnSequence;
        private float _accumulator;
        private Action<string> _sound;
        private IBattleEffectContext _battleContext;
        private GameBoxNativeLighting _nativeLighting;
        private bool _disposed, _suppressEvents;
        private float _lastRepeat;
        private double _clockOrigin, _timeControlElapsed, _fallbackElapsed, _battleAbsoluteTime = double.NaN;
        private int _pendingNode = -1, _pendingNodeCount;
        private float _pendingDelta;
        private bool _pendingAlive;

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

        public void Initialize(EffectResources resources, int groupId, uint seed = 1,
            EffectOverrides overrides = null, Action<string> sound = null, IBattleEffectContext battleContext = null, double clockOrigin = 0,
            GameBoxNativeLighting nativeLighting = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EffectPlayer));
            if (overrides != null && overrides.Version != 1)
                throw new NotSupportedException($"Unknown original effect override version {overrides.Version}");
            _resources = resources;
            _groupId = groupId;
            _seed = seed == 0 ? 1 : seed;
            _overrides = overrides;
            _sound = sound;
            _battleContext = battleContext;
            _nativeLighting = nativeLighting;
            _clockOrigin = clockOrigin;
            ResetSimulation();
        }

        private void ResetSimulation(bool resetRandom = true)
        {
            ClearNodes();
            _pendingNode = -1;
            _time = _clockOrigin;
            _battleAbsoluteTime = double.NaN;
            _timeControlElapsed = _fallbackElapsed = 0;
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
                if (!EffectOverrides.Resolve(_overrides, groupId, i, ref d, ref tasks))
                    continue;
                if (_nodes.Count >= 2048)
                    throw new InvalidOperationException("Original effect object limit (2048) reached");
                obj = new EffectGroupObject { TypeCrc = obj.TypeCrc, Tasks = tasks };
                var root = new GameObject($"{groupId}:{i} {d.TypeName}") { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
                root.transform.SetParent(transform, false);
                EffectNode node = null;
                try
                {
                    string kind = EffectClass.Name(d.ClassNameCrc);
                    uint seed = _seed + _spawnSequence++ * 7919 + (uint)(groupId * 104729 + i);
                    node = kind switch
                    {
                        "action" or "camera" or "quake" or "bink" or "time" when _battleContext != null => new BattleControlNode(root, d, obj.Tasks, position, Time, _battleContext, kind, _resources.GameVariant == "PAL3A"),
                        "callback" or "audio" => new ControlEffectNode(root, d, obj.Tasks, position, Time, this, kind),
                        "quad" => new QuadNode(root, d, obj.Tasks, position, Time, _resources, _battleContext != null),
                        "S-Particle" => new SparNode(root, d, obj.Tasks, position, Time, _resources, seed),
                        "particle" => new ParticleNode(root, d, obj.Tasks, position, Time, _resources, seed, _overrides),
                        "cylinder" or "grid" => new GeometryNode(root, d, obj.Tasks, position, Time, _resources, kind, _battleContext != null),
                        "meteor" or "shock" => new TrailNode(root, d, obj.Tasks, position, Time, _resources, kind, seed, _battleContext != null, _battleContext == null ? null : _battleContext.NextEffectRandom),
                        "model" => new ModelNode(root, d, obj.Tasks, position, Time, _resources, _nativeLighting),
                        "explosive" => new ExplosiveNode(root, d, obj.Tasks, position, Time, _resources, seed, _battleContext != null, _battleContext == null ? null : _battleContext.NextEffectRandom),
                        _ => throw new NotSupportedException($"Original effect class {kind}: {d.TypeName}"),
                    };
                    if (_battleContext != null)
                    {
                        node.UseNativeClockComparisons();
                        node.SetRenderOrder(GameBoxBattleRenderOrder.Effect(_resources.GameVariant == "PAL3A", kind == "model"));
                    }
                    _nodes.Add(node);
                }
                catch
                {
                    node?.Dispose();
                    EffectResources.Destroy(root);
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

        // Battle managers receive the float projection of the system's double
        // clock. Keep the scene viewer's delta-driven API independent.
        public void AdvanceTo(double managerTime, double absoluteTime = double.NaN)
        {
            if (_disposed || Paused || Error != null || _pendingNode >= 0 || !float.IsFinite((float)managerTime))
                return;
            float projected = (float)managerTime;
            float delta = projected - (float)_time;
            bool absoluteAdvanced = double.IsFinite(absoluteTime) &&
                (!double.IsFinite(_battleAbsoluteTime) || absoluteTime > _battleAbsoluteTime);
            if (delta < 0 || delta == 0 && !absoluteAdvanced)
                return;
            _time = projected;
            if (absoluteAdvanced)
            {
                // Account for fallback frames already counted since the last real sample,
                // then anchor the next external frame to this actual timestamp.
                double elapsed = double.IsFinite(_battleAbsoluteTime) ? absoluteTime - _battleAbsoluteTime : delta;
                double remaining = elapsed - _fallbackElapsed;
                _timeControlElapsed += remaining < -0.000001 ? delta : Math.Max(0, remaining);
                _battleAbsoluteTime = absoluteTime;
                _fallbackElapsed = 0;
            }
            else if (!double.IsFinite(absoluteTime))
                AdvanceFallbackClock(delta);
            try
            {
                AdvanceNodes(0, _nodes.Count, delta, false);
            }
            catch (Exception error)
            {
                Error = error.ToString();
                ClearNodes();
                IsAlive = false;
                Debug.LogException(error);
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
            if (_disposed || Paused || Error != null || _pendingNode >= 0 || delta <= 0 || !float.IsFinite(delta))
                return 0;
            float elapsed = delta * (float.IsFinite(Speed) ? Mathf.Clamp(Speed, 0, 16) : 1);
            return float.IsFinite(elapsed) ? elapsed : 0;
        }

        private bool SimulateStep(float delta)
        {
            _time += delta;
            AdvanceFallbackClock(delta);
            try
            {
                return AdvanceNodes(0, _nodes.Count, delta, false);
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

        private void AdvanceFallbackClock(float delta)
        {
            _timeControlElapsed += delta;
            if (double.IsFinite(_battleAbsoluteTime))
                _fallbackElapsed += delta;
        }

        private bool AdvanceNodes(int first, int count, float delta, bool alive)
        {
            for (int index = first; index < count; index++)
            {
                EffectNode node = _nodes[index];
                if (_suppressEvents && node is BattleControlNode silentControl)
                {
                    if (delta > 0)
                        silentControl.StepSilently(Time, delta);
                }
                else if (node is BattleControlNode { ControlsTime: true } timeControl)
                    timeControl.StepTimeControl(Time, delta, _timeControlElapsed);
                else if (delta > 0)
                    node.Step(Time, delta);
                if (!_suppressEvents && _battleContext?.IsModalPresentationRunning == true)
                {
                    _pendingNode = index;
                    _pendingNodeCount = count;
                    _pendingDelta = delta;
                    _pendingAlive = alive;
                    IsAlive = true;
                    return false;
                }
                alive |= _nodes[index].Alive;
            }
            IsAlive = alive || _nodes.Count > count;
            for (int index = _nodes.Count - 1; index >= 0; index--)
                if (!_nodes[index].Alive)
                {
                    _nodes[index].Dispose();
                    _nodes.RemoveAt(index);
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

        public bool ResumeBattleStep()
        {
            if (_pendingNode < 0)
                return true;
            if (_battleContext?.IsModalPresentationRunning == true)
                return false;
            try
            {
                int index = _pendingNode;
                if (_nodes[index] is BattleControlNode control && !control.ResumeModal())
                    return false;
                _pendingNode = -1;
                return AdvanceNodes(index + 1, _pendingNodeCount, _pendingDelta, _pendingAlive || _nodes[index].Alive);
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                ClearNodes();
                IsAlive = false;
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
            foreach (EffectNode node in _nodes)
            {
                node.Render(camera);
                node.ApplyRenderOrder();
                QuadCount += node.QuadCount;
            }
        }

        private void Update()
        {
            if (!AutoAdvance)
                return;
            AdvanceFrame(UnityEngine.Time.deltaTime);
            if (DestroyOnComplete && !Loop && !IsAlive && Time > 0)
                EffectResources.Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (AutoAdvance)
                Render(Camera.main);
        }

        private void ClearNodes()
        {
            _pendingNode = -1;
            foreach (EffectNode node in _nodes)
                node.Dispose();
            _nodes.Clear();
            QuadCount = 0;
        }

        // Editor owners must dispose explicitly: ordinary MonoBehaviour destroy
        // callbacks are not guaranteed for objects used outside Play Mode.
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            AutoAdvance = false;
            IsAlive = false;
            _pendingNode = -1;
            _accumulator = 0;
            ClearNodes();
            _resources = null;
            _overrides = null;
            _sound = null;
            _battleContext = null;
            _nativeLighting = null;
        }

        private void OnDestroy() => Dispose();
    }
}
