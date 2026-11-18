// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Battle;
    using Game.Effect.Runtime;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // Native weapon skills use the ATS manager: all child effects advance on the
    // absolute clock while the ordinary combat clock is frozen.
    public sealed class BattleEffectDirector : IBattleEffectDirector, IBattleEffectPreloader, IBattleModalEffects, IBattlePresentationPreparation, IBattleStagePresentation, IBattleAudio, IBattleMovementAudio, IBattleSpiritCaptureLayout, IBattleEffectContext, IEffectTimeControl, IBattleTimeScale, IDisposable
    {
        private readonly ResourceModelPreview _preview;
        private readonly EffectResources _resources;
        private readonly EffectOverrides _overrides;
        private BattleSession _session;
        private BattleRandom _effectRandom;
        private readonly BattleMoviePlayer _movies;

        public Func<string, BattleMovementPoint, BattleSpiritPoint> SpiritProjector
        {
            get; set;
        }

        public BattleSpiritPoint CaptureSpiritOrigin(string actorId, BattleMovementPoint world) =>
            SpiritProjector?.Invoke(actorId, world) ?? throw new InvalidOperationException("A capture requires its battle camera projection.");

        public BattleSpiritRect SpiritWheelTarget(int coreElement) => BattlePreview.SpiritWheelTarget(coreElement);

        public IEffectCamera Camera
        {
            get; private set;
        }

        public bool BlocksBattleTime => _movies.BlocksBattleTime;

        public bool IsPreparingPlayback => _movies.IsPreparingPlayback;

        internal void ReusePreparedMovies(BattleEffectDirector previous) => _movies.ReusePreparedMovies(previous._movies);

        public bool SkipModalPresentation() => _movies.Skip();

        public IReadOnlyList<string> MissingMovies => _movies.MissingMovies;
        public IReadOnlyList<string> MovieDiagnostics => _movies.Diagnostics;

        public IReadOnlyCollection<string> MissingBattleTextures => _resources.MissingBattleTextures;

        public bool Muted
        {
            get => _movies.Muted; set => _movies.Muted = value;
        }

        public bool AudioPlaybackEnabled
        {
            get => _movies.AudioPlaybackEnabled; set => _movies.AudioPlaybackEnabled = value;
        }

        public void Bind(BattleSession session, IEffectCamera camera)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public IEffectActor Actor(int nativeIndex, uint modelCrc = 0)
        {
            if (_session == null)
                throw new InvalidOperationException("A combat actor effect requires a bound battle session.");
            string actor = _session.EffectActorId(nativeIndex, modelCrc);
            return actor == null ? null : new ActorPort(_session, actor);
        }

        public IEffectMovie PlayMovie(string originalPath) => _movies.Play(originalPath);

        public int NextEffectRandom() => (_effectRandom ?? throw new InvalidOperationException("No original effect has started its random stream.")).Next();

        private sealed class ActorPort : IEffectActor
        {
            private readonly BattleSession _session;
            private readonly string _actor;

            public ActorPort(BattleSession session, string actor)
            {
                _session = session;
                _actor = actor;
            }

            public Vector3 Position
            {
                get
                {
                    BattleMovementPoint p = _session.EffectActor(_actor).Position;
                    return new Vector3(p.X, p.Y, p.Z);
                }
                set => _session.SetEffectActorPosition(_actor, new BattleMovementPoint(value.x, value.y, value.z));
            }

            public float FacingDegrees
            {
                get => _session.EffectActor(_actor).FacingDegrees; set => _session.SetEffectActorFacing(_actor, value);
            }

            public float Blend
            {
                get => _session.EffectActor(_actor).Blend; set => _session.SetEffectActorBlend(_actor, value);
            }

            public bool ActionOver => _session.EffectActor(_actor).ActionOver;

            public void SetExternalTask(bool controlled, object owner) => _session.SetEffectActorControlled(_actor, controlled, owner);

            public void SetAction(string action) => _session.SetEffectActorAction(_actor, action);

            public void SetLongBlade(bool enabled) => _session.SetEffectActorBlade(_actor, enabled);

            public void SetWeapons(float left, float right) => _session.SetEffectActorWeapons(_actor, left, right);
        }

        private sealed class EffectInstance
        {
            public EffectPlayer Player;
            public bool AbsoluteTime;
        }

        private readonly Dictionary<long, EffectInstance> _players = new();
        private long _nextHandle, _lastFrame = -1;
        private bool _disposed;
        private long[] _pendingPlayers;
        private int _pendingPlayer;
        private double _scaledTime, _absoluteTime;
        private readonly Dictionary<object, float> _timeScales = new();

        public float TimeScale => _timeScales.Count == 0 ? 1f : _timeScales.Values.Min();

        void IEffectTimeControl.SetTimeScale(object owner, float scale) => _timeScales[owner] = scale;

        void IEffectTimeControl.ClearTimeScale(object owner) => _timeScales.Remove(owner);

        public bool IsModalPresentationRunning => BlocksBattleTime;

        public event Action<string> SoundRequested;

        public void PlaySound(string sound) => SoundRequested?.Invoke(sound);

        public event Action<string, string, bool> MovementSoundRequested;

        public event Action<string> MovementSoundStopped;

        public event Action BodySoundsStopped;

        public void PlayMovementSound(string actor, string sound, bool loop) => MovementSoundRequested?.Invoke(actor, sound, loop);

        public void StopMovementSound(string actor) => MovementSoundStopped?.Invoke(actor);

        public void StopBodySounds() => BodySoundsStopped?.Invoke();

        public event Action<double, double> StageAdvanced;

        public void AdvanceStage(double scaledSeconds, double absoluteSeconds) => StageAdvanced?.Invoke(scaledSeconds, absoluteSeconds);

        public BattleEffectDirector(BattleData data, ResourceModelPreview preview, IBattleMovieAudioTransport audio = null)
        {
            if (data == null || preview == null)
                throw new ArgumentNullException(data == null ? nameof(data) : nameof(preview));
            _preview = preview;
            try
            {
                _movies = new BattleMoviePlayer(data, preview, audio);
                _resources = new EffectResources(data.Resources.FileSystem, data.Resources.Codepage);
                _overrides = Resources.Load<EffectOverrides>(_resources.OverridesPath);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Preload(int groupId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BattleEffectDirector));
            string[] errors = _resources.Diagnose(groupId, _overrides, battleContext: true);
            if (errors.Length > 0)
                throw new InvalidDataException(string.Join("\n", errors));
            var visited = new HashSet<int>();
            foreach (int id in _resources.Preload(groupId, _overrides, battleContext: true))
            {
                if (!visited.Add(id))
                    continue;
                foreach (string movie in _resources.BinkMovies(id, _overrides))
                    _movies.Preload(movie);
            }
        }

        public long Start(BattleEffectRequest request)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BattleEffectDirector));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            Preload(request.GroupId);
            // The original GT util singleton is separate from combat CUtil.
            // Capture one replayable visual stream, retaining it across groups.
            _effectRandom ??= new BattleRandom(unchecked((int)request.Seed));
            GameObject root = _preview.CreateObject("Battle effect " + request.GroupId);
            EffectPlayer player = null;
            try
            {
                root.transform.localPosition = BattleAnimationCatalog.UnityPosition(request.Origin);
                player = root.AddComponent<EffectPlayer>();
                player.AutoAdvance = false;
                player.Loop = false;
                player.Initialize(_resources, request.GroupId, request.Seed, _overrides, sound => SoundRequested?.Invoke(sound), this, request.AbsoluteTime ? _absoluteTime : _scaledTime, nativeLighting: _preview.NativeLighting);
                if (player.Error != null)
                    throw new InvalidDataException(player.Error);
                long handle = checked(++_nextHandle);
                _players.Add(handle, new EffectInstance { Player = player, AbsoluteTime = request.AbsoluteTime });
                return handle;
            }
            catch
            {
                player?.Dispose();
                Object.DestroyImmediate(root);
                throw;
            }
        }

        public bool IsRunning(long handle)
        {
            if (!_players.TryGetValue(handle, out EffectInstance instance) || instance.Player == null)
                return false;
            if (instance.Player.Error != null)
                throw new InvalidDataException(instance.Player.Error);
            return instance.Player.IsAlive;
        }

        public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BattleEffectDirector));
            if (frame < _lastFrame || !float.IsFinite(scaledSeconds) || !float.IsFinite(unscaledSeconds) || scaledSeconds < 0 || unscaledSeconds < 0)
                throw new ArgumentException("Effect frames and time deltas must be ordered and finite.");
            if (frame == _lastFrame)
                return;
            _lastFrame = frame;
            _movies.Advance(ClockDelta(unscaledSeconds));
            if (BlocksBattleTime)
                return;
            if (_pendingPlayers != null)
            {
                if (_players.TryGetValue(_pendingPlayers[_pendingPlayer], out EffectInstance suspended))
                {
                    if (!suspended.Player.ResumeBattleStep())
                    {
                        if (suspended.Player.Error != null)
                            throw new InvalidDataException(suspended.Player.Error);
                        return;
                    }
                    if (!suspended.Player.IsAlive)
                        Stop(_pendingPlayers[_pendingPlayer]);
                }
                _pendingPlayer++;
            }
            else
            {
                _pendingPlayers = _players.Keys.ToArray();
                _pendingPlayer = 0;
                _scaledTime += ClockDelta(scaledSeconds);
                _absoluteTime += ClockDelta(unscaledSeconds);
            }
            for (; _pendingPlayer < _pendingPlayers.Length; _pendingPlayer++)
            {
                long handle = _pendingPlayers[_pendingPlayer];
                if (!_players.TryGetValue(handle, out EffectInstance instance))
                    continue;
                EffectPlayer player = instance.Player;
                if (player == null)
                {
                    _players.Remove(handle);
                    continue;
                }
                player.AdvanceTo(instance.AbsoluteTime ? _absoluteTime : _scaledTime, _absoluteTime);
                if (player.Error != null)
                    throw new InvalidDataException(player.Error);
                if (BlocksBattleTime)
                    return;
                if (!player.IsAlive)
                    Stop(handle);
            }
            _pendingPlayers = null;
        }

        private static double ClockDelta(float seconds) => seconds == (float)BattleSession.StepSeconds ? BattleSession.StepSeconds : seconds;

        public void Render(Camera camera)
        {
            if (_disposed || camera == null)
                return;
            _movies.Render(camera);
            foreach (EffectInstance instance in _players.Values)
                if (instance.Player != null)
                    instance.Player.Render(camera);
        }

        public void Stop(long handle)
        {
            if (_players.Remove(handle, out EffectInstance instance) && instance.Player != null)
            {
                instance.Player.Dispose();
                Object.DestroyImmediate(instance.Player.gameObject);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (long handle in _players.Keys.ToArray())
                Stop(handle);
            _movies?.Dispose();
            _resources?.Dispose();
        }
    }
}
