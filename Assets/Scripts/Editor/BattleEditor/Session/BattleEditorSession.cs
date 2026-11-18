// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Battle;

    // Owns editor resources and the captured preview. The authoring draft stays outside this owner.
    public sealed partial class BattleEditorSession : IDisposable
    {
        private BattleSetup _setup;
        private BattleRandomExampleBuilder _examples;
        private readonly Random _exampleRandom = new();
        private BattleAnimationCatalog _battleCatalog;
        private bool _initialAutoAllies, _initialAutoEnemies;
        private bool _isPlaying;
        private IEnumerator<BattleSession> _replay;

        internal bool IsReplaying => _replay != null;

        private float _speed = 1;

        internal event Action PreviewReleased;

        public BattleSession Battle
        {
            get; private set;
        }

        public BattleData Data
        {
            get; private set;
        }

        public BattlePreview Preview
        {
            get; private set;
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value && ResultPresentation?.Completed != true;
                SyncAudioPlayback();
            }
        }

        public float Speed
        {
            get => _speed;
            set
            {
                if (!float.IsFinite(value) || value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _speed = value;
                SyncAudioPlayback();
            }
        }

        private void SyncAudioPlayback()
        {
            if (Preview == null)
                return;
            // Preview AudioSources own transport and pitch. Editor inspection
            // and accelerated simulation deliberately stay silent.
            bool realtime = IsPlaying && Speed == 1;
            Preview.Audio.PlaybackEnabled = realtime;
            Preview.Effects.AudioPlaybackEnabled = realtime;
        }

        public BattleScenario CapturedScenario => _setup?.CreateInstance();

        public void Load(string root, int codepage) => UseData(new BattleData(root, codepage));

        internal void UseData(BattleData candidate)
        {
            try
            {
                Stop();
                Data?.Dispose();
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
            Data = candidate;
            _examples = null;
        }

        public BattleScenario CreateRandomExample()
        {
            if (Data == null)
                throw new InvalidOperationException("Load game data first.");
            _examples ??= new BattleRandomExampleBuilder(Data);
            return _examples.Create(_exampleRandom);
        }

        public void Start(BattleScenario scenario)
        {
            if (Data == null)
                throw new InvalidOperationException("Load game data first.");
            ReplacePreview(Data.Capture(scenario));
        }

        private void ReplacePreview(BattleSetup setup)
        {
            var candidate = new BattlePreview(Data, setup);
            CommitPreview(candidate, setup, "replaced by preview");
            IsPlaying = false;
        }

        public void Restart()
        {
            if (_setup != null)
            {
                if (Battle != null)
                    StartBattle(_setup.CreateInstance(), _initialAutoAllies, _initialAutoEnemies);
                else
                    ReplacePreview(_setup);
            }
        }

        public void StartBattle(BattleScenario scenario, bool autoAllies = false, bool autoEnemies = false)
        {
            if (Data == null)
                throw new InvalidOperationException("Load game data first.");
            var setup = Data.Capture(scenario, forBattle: true);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(Data, setup);
            var catalog = new BattleAnimationCatalog(Data, setup, definitions);
            var candidate = new BattlePreview(Data, setup);
            BattleSession combat;
            try
            {
                combat = new BattleSession(setup, catalog, definitions: definitions, effects: candidate.Effects);
                candidate.BindBattle(combat, catalog);
                combat.SetAutomatic(BattleSide.Ally, autoAllies);
                combat.SetAutomatic(BattleSide.Enemy, autoEnemies);
                candidate.ApplyBattle(combat, catalog);
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
            CommitPreview(candidate, setup, "restarted");
            Battle = combat;
            _battleCatalog = catalog;
            _initialAutoAllies = autoAllies;
            _initialAutoEnemies = autoEnemies;
            IsPlaying = false;
        }

        public void StepBattle()
        {
            if (Battle == null)
                return;
            IsPlaying = false;
            if (ResultPresentation != null)
                StepResults();
            else
            {
                Battle.Step();
                SyncBattle();
            }
        }

        public void AdvanceTo(BattleEventKind boundary, int maxSteps = 30000)
        {
            if (Battle == null)
                return;
            IsPlaying = false;
            int start = Battle.Events.Count;
            for (int i = 0; i < maxSteps && Battle.Status == BattleSessionStatus.Running; i++)
            {
                long before = Battle.Tick;
                Battle.Step();
                if (Battle.Tick == before || Battle.Events.Skip(start).Any(entry => entry.Kind == boundary || boundary == BattleEventKind.Hit && entry.Kind == BattleEventKind.Miss) ||
                    Battle.WaitingActor != null && Battle.Actors.All(actor => actor.ActionId == 0))
                    break;
            }
            SyncBattle(); // Bounded inspection; never invent a victory if every attack misses.
        }

        public void SyncBattle()
        {
            if (Battle == null)
                return;
            try
            {
                if (ResultPresentation == null)
                    Preview?.ApplyBattle(Battle, _battleCatalog);
                EnsureResults();
            }
            catch (Exception ex)
            {
                Battle.Fault("presentation: " + ex.Message);
                IsPlaying = false;
                throw;
            }
            if (Battle.Status != BattleSessionStatus.Running && (ResultPresentation == null || ResultPresentation.Completed))
            {
                Preview?.Audio.Stop();
                IsPlaying = false;
            }
        }

        public void ReplayBattle()
        {
            if (Battle == null || IsReplaying)
                return;
            using IEnumerator<BattleSession> replay = ReplayFrames().GetEnumerator();
            while (replay.MoveNext())
            {
            }
        }

        internal void BeginReplay()
        {
            if (Battle != null && !IsReplaying)
                _replay = ReplayFrames().GetEnumerator();
        }

        internal void TickReplay()
        {
            IEnumerator<BattleSession> replay = _replay;
            if (replay == null)
                return;
            try
            {
                var slice = Stopwatch.StartNew();
                do
                {
                    if (!replay.MoveNext())
                    {
                        CancelReplay();
                        return;
                    }
                }
                while (slice.Elapsed.TotalMilliseconds < 6);
            }
            catch
            {
                CancelReplay();
                throw;
            }
        }

        internal void CancelReplay()
        {
            IEnumerator<BattleSession> replay = _replay;
            _replay = null;
            replay?.Dispose();
        }

        private IEnumerable<BattleSession> ReplayFrames()
        {
            var replayCatalog = new BattleAnimationCatalog(Data, _setup, Battle.Definitions);
            bool wasPlaying = IsPlaying;
            BattlePreview candidate = null;
            bool committed = false;
            try
            {
                IsPlaying = false;
                candidate = new BattlePreview(Data, _setup);
                candidate.Audio.PlaybackEnabled = false;
                candidate.Effects.AudioPlaybackEnabled = false;
                candidate.Audio.Muted = true;
                candidate.Effects.Muted = true;
                candidate.Effects.ReusePreparedMovies(Preview.Effects);
                BattleSession replay = null;
                foreach (BattleSession frame in BattleReplay.RecreateFrames(_setup, replayCatalog, Battle, candidate.Effects,
                    created => candidate.BindBattle(created, replayCatalog)))
                {
                    replay = frame;
                    yield return frame;
                }
                candidate.ApplyBattle(replay, replayCatalog);
                candidate.Audio.Muted = false;
                candidate.Effects.Muted = false;
                _replay = null;
                BattlePreview completed = candidate;
                candidate = null;
                CommitPreview(completed, _setup, "replaced by replay");
                committed = true;
                Battle = replay;
                _battleCatalog = replayCatalog;
                EnsureResults();
            }
            finally
            {
                if (!committed)
                {
                    try
                    {
                        candidate?.Dispose();
                    }
                    finally
                    {
                        IsPlaying = wasPlaying;
                    }
                }
            }
        }

        public void Tick(float elapsed)
        {
            if (!IsPlaying || Preview == null)
                return;
            if (!float.IsFinite(elapsed) || elapsed < 0 || !float.IsFinite(Speed) || Speed <= 0)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (Battle != null)
            {
                if (ResultPresentation != null)
                    AdvanceResults(Math.Min(60, (double)elapsed * Speed));
                else
                {
                    Battle.Advance(Math.Min(60, (double)elapsed * Speed));
                    SyncBattle();
                }
                return;
            }
            Preview.Sample(Preview.Time + elapsed * Speed);
            if (Preview.Time >= Preview.Duration)
                IsPlaying = false;
        }

        private void ReleasePreview()
        {
            BattlePreview previous = Preview;
            Preview = null;
            try
            {
                PreviewReleased?.Invoke();
            }
            finally
            {
                previous?.Dispose();
            }
        }

        private void CommitPreview(BattlePreview candidate, BattleSetup setup, string reason)
        {
            try
            {
                Stop(reason);
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
            Preview = candidate;
            _setup = setup;
        }

        public void Stop() => Stop("editor stop");

        private void Stop(string reason)
        {
            try
            {
                CancelReplay();
                IsPlaying = false;
                Battle?.Abort(reason);
            }
            finally
            {
                Battle = null;
                ResetResults();
                _battleCatalog = null;
                _setup = null;
                ReleasePreview();
            }
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            finally
            {
                BattleData data = Data;
                Data = null;
                data?.Dispose();
            }
        }
    }
}
