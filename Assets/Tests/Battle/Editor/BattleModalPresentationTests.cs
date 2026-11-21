// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleModalPresentationTests
    {
        private sealed class Movies : IBattleEffectDirector, IBattleModalEffects, IBattlePresentationPreparation
        {
            internal readonly List<float> ScaledDeltas = new();
            internal int Calls;
            private readonly int _firstFrame, _lastFrame;
            private bool _skipped;

            internal Movies(int firstFrame = 1, int lastFrame = 2) =>
                (_firstFrame, _lastFrame) = (firstFrame, lastFrame);

            public bool BlocksBattleTime
            {
                get; private set;
            }

            public bool Busy
            {
                get; set;
            }

            public bool IsPreparingPlayback => BlocksBattleTime && Busy;

            public bool SkipModalPresentation()
            {
                if (!BlocksBattleTime)
                    return false;
                _skipped = true;
                BlocksBattleTime = false;
                return true;
            }

            public long Start(BattleEffectRequest request) => 1;

            public bool IsRunning(long handle) => true;

            public void Stop(long handle) => BlocksBattleTime = false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
                ScaledDeltas.Add(scaledSeconds);
                Calls++;
                BlocksBattleTime = !_skipped && Calls >= _firstFrame && Calls <= _lastFrame;
            }
        }

        private sealed class EarlyGaugeRules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => 123;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => default;
        }

        [Test]
        public void RecreateRetainsInjectedSessionRules()
        {
            var setup = new BattleSetup(BattleSessionTests.Scenario());
            var catalog = new BattleSessionTests.Catalog();
            var original = new BattleSession(setup, catalog, new EarlyGaugeRules());
            BattleSession replay = BattleReplay.Recreate(setup, catalog, original);
            Assert.That(replay.Actors.Select(actor => actor.Gauge), Is.EqualTo(new[] { 123, 123 }));
        }

        private static BattleSession Session(string variant, Movies movies, out BattleSetup setup,
            out BattleDefinitions definitions)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                System.Array.Empty<BattleAbilityDefinition>(), System.Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    NormalAttackMode = 924
                })));
            setup = new BattleSetup(scenario);
            var session = new BattleSession(setup, new BattleSessionTests.Catalog(), definitions: definitions, effects: movies);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AModalMovieSuspendsTheCurrentFrameAndResumesItOnlyOnce(string variant)
        {
            var movies = new Movies();
            BattleSession session = Session(variant, movies, out _, out _);
            session.Step();
            Assert.That(session.Tick, Is.EqualTo(1));
            Assert.That(session.SimulationTick, Is.Zero);
            Assert.That(session.WaitingActor, Is.Null);
            Assert.That(session.CanSubmit("Ally", BattleAction.Attack, "Enemy"), Does.Contain("movie"));
            session.Step();
            Assert.That(session.SimulationTick, Is.Zero);
            session.Step();
            Assert.That(session.Tick, Is.EqualTo(3));
            Assert.That(session.SimulationTick, Is.EqualTo(1));
            Assert.That(session.WaitingActor, Is.EqualTo("Ally"));
            Assert.That(session.Actors[0].PoseTick, Is.EqualTo(1));
            session.Step();
            Assert.That(session.SimulationTick, Is.EqualTo(2));
            Assert.That(movies.ScaledDeltas, Is.EqualTo(new[] { 0.02f, 0f, 0f, 0.02f }));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void PreparingAMovieConsumesNoTicksAndReplayUsesTheSameTimeline(string variant)
        {
            var movies = new Movies { Busy = true };
            BattleSession session = Session(variant, movies, out BattleSetup setup, out _);
            session.Step();
            Assert.That(session.IsPreparingPresentation, Is.True);
            session.Advance(30);
            session.Step();
            Assert.That(session.Tick, Is.EqualTo(1));
            Assert.That(movies.Calls, Is.EqualTo(1));

            movies.Busy = false;
            session.Advance(BattleSession.StepSeconds);
            Assert.That(session.Tick, Is.EqualTo(2));
            session.Step();
            Assert.That(session.SimulationTick, Is.EqualTo(1));
            BattleSession replay = BattleReplay.Recreate(setup, new BattleSessionTests.Catalog(), session, new Movies());
            Assert.That(replay.SimulationTick, Is.EqualTo(session.SimulationTick));
            Assert.Throws<InvalidOperationException>(() => BattleReplay.Recreate(
                setup, new BattleSessionTests.Catalog(), session, new Movies { Busy = true }));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SkippingAMovieRecordsOnceAndReplayResumesTheSameFrame(string variant)
        {
            var movies = new Movies(lastFrame: 100);
            BattleSession session = Session(variant, movies, out BattleSetup setup, out _);
            Assert.That(session.SkipModalPresentation(), Is.False);
            session.Step();

            Assert.That(session.SkipModalPresentation(), Is.True);
            Assert.That(session.SkipModalPresentation(), Is.False);
            Assert.That(session.WaitingActor, Is.Null);
            BattleInput skip = session.Inputs.Last();
            Assert.That(skip.Kind, Is.EqualTo(BattleInputKind.ModalPresentationSkip));
            Assert.That(skip.Tick, Is.EqualTo(1));

            var replayMovies = new Movies(lastFrame: 100);
            BattleSession pausedReplay = BattleReplay.Recreate(setup, new BattleSessionTests.Catalog(), session, replayMovies);
            Assert.That(pausedReplay.WaitingActor, Is.Null);
            session.Step();
            Assert.That(session.WaitingActor, Is.EqualTo("Ally"));
            Assert.That(session.SimulationTick, Is.EqualTo(1));

            BattleSession replay = BattleReplay.Recreate(setup, new BattleSessionTests.Catalog(), session, new Movies(lastFrame: 100));
            Assert.That(replay.SimulationTick, Is.EqualTo(1));
            Assert.That(replay.WaitingActor, Is.EqualTo(session.WaitingActor));
        }

        [TestCase(1, 0)]
        [TestCase(3, 4)]
        public void ReplayRejectsAnAbsentOrShiftedMovieEvenWithTheSameInputLog(int firstFrame, int lastFrame)
        {
            BattleSession session = Session("PAL3", new Movies(), out BattleSetup setup, out BattleDefinitions definitions);
            for (int frame = 0; frame < 5; frame++)
                session.Step();

            BattleSession uncheckedReplay = BattleReplay.Play(setup, new BattleSessionTests.Catalog(), session.Inputs, session.Tick,
                definitions: definitions, effects: new Movies(firstFrame, lastFrame));
            if (firstFrame == 3)
                Assert.That(uncheckedReplay.SimulationTick, Is.EqualTo(session.SimulationTick), "Equal final clocks can hide different pauses.");

            var mismatch = Assert.Throws<InvalidOperationException>(() => BattleReplay.Recreate(setup, new BattleSessionTests.Catalog(),
                session, new Movies(firstFrame, lastFrame)));
            Assert.That(mismatch.Message, Does.Contain("battle movies"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AReplayBindsItsEffectOwnerBeforeAdvancingTheFirstFrame(string variant)
        {
            var movies = new Movies();
            BattleSession session = Session(variant, movies, out BattleSetup setup, out BattleDefinitions definitions);
            for (int index = 0; index < 5; index++)
                session.Step();
            var replayMovies = new Movies();
            BattleSession bound = null;
            BattleSession replay = BattleReplay.Play(setup, new BattleSessionTests.Catalog(), session.Inputs, session.Tick,
                definitions: definitions, effects: replayMovies, onCreated: created =>
                {
                    Assert.That(replayMovies.Calls, Is.Zero);
                    bound = created;
                });
            Assert.That(bound, Is.SameAs(replay));
            Assert.That(replay.SimulationTick, Is.EqualTo(session.SimulationTick));
            Assert.That(replayMovies.ScaledDeltas, Is.EqualTo(movies.ScaledDeltas));
        }
    }
}
