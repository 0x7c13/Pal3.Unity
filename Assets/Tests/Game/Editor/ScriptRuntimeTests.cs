// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Sce;
    using NUnit.Framework;
    using GamePlay;
    using Script;
    using Script.Patcher;
    using Script.Waiter;

    public sealed class ScriptRuntimeTests
    {
        private const ushort Marker = 0x8001;
        private const ushort OtherMarker = 0x8002;
        private Host _host;

        [TearDown]
        public void TearDown() => _host?.Dispose();

        [Test]
        public void GroupTimersAdvanceConcurrently()
        {
            var script = Bytes(w => { Mode(w, 2); Wait(w, 1); Wait(w, 1.5f); Mode(w, 1); Set(w, Marker, 1); });
            _host = new Host(Block(51, script));
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            _host.Manager.Update(1.6f);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
        }

        [Test]
        public void DialogueDoesNotPostponeTimerBelowIt()
        {
            var gate = new WaitUntilCanceled();
            _host = new Host(Block(51, Bytes(w => { Mode(w, 2); Wait(w, 1); Note(w); Mode(w, 1); Set(w, Marker, 1); })));
            _host.BeforeDispatch = c => { if (c is UIDisplayNoteCommand) _host.Dispatch(new ScriptRunnerAddWaiterRequest(gate)); };
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            _host.Manager.Update(.6f);
            _host.Manager.Update(.6f);
            Assert.That(_host.Variables.Get(Marker), Is.Zero);
            gate.CancelWait();
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
        }

        [Test]
        public void AllFinishedWaitersAreRemovedInOneUpdate()
        {
            var gates = new[] { new WaitUntilCanceled(), new WaitUntilCanceled() };
            int next = 0;
            _host = new Host(Block(51, Bytes(w => { Mode(w, 2); Note(w); Note(w); Mode(w, 1); Set(w, Marker, 1); })));
            _host.BeforeDispatch = c => { if (c is UIDisplayNoteCommand) _host.Dispatch(new ScriptRunnerAddWaiterRequest(gates[next++])); };
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            foreach (var gate in gates) gate.CancelWait();
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
        }

        [Test]
        public void LocalInitializerIsAppliedAndNewInstancesStartFresh()
        {
            _host = new Host(Block(51, Bytes(w => { IfEqual(w, 0, 7); Add(w, 0, 1); Add(w, Marker, 1); }), 7));
            for (int i = 0; i < 2; i++)
            {
                Assert.That(_host.Manager.AddScript(51), Is.True);
                _host.Manager.Update(0);
            }
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(2));
        }

        [Test]
        public void ConcurrentScriptsHaveSeparateLocalsAndSharedGlobals()
        {
            _host = new Host(
                Block(51, Bytes(w => { Set(w, 0, 7); Wait(w, .5f); IfEqual(w, 0, 7); Set(w, Marker, 1); Add(w, 0x8000, 1); })),
                Block(52, Bytes(w => { Set(w, 0, 9); Wait(w, 1); IfEqual(w, 0, 9); Set(w, OtherMarker, 1); Add(w, 0x8000, 1); })));
            _host.Manager.AddScript(51);
            _host.Manager.AddScript(52);
            _host.Manager.Update(0);
            _host.Manager.Update(.6f);
            _host.Manager.Update(.6f);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(OtherMarker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(0x8000), Is.EqualTo(2));
            Assert.That(_host.Variables.All(v => (v.Key & ScriptConstants.GlobalVariableFlag) != 0), Is.True);
            Assert.Throws<InvalidOperationException>(() => _host.Variables.Get(0));
        }

        [Test]
        public void CommandExceptionRestoresVariableScopeAndExecutingFlag()
        {
            _host = new Host(Block(51, Bytes(w => { Note(w); Set(w, Marker, 1); })));
            _host.BeforeDispatch = c => { if (c is UIDisplayNoteCommand) throw new InvalidOperationException("Injected failure"); };
            _host.Manager.AddScript(51);
            Assert.Throws<InvalidOperationException>(() => _host.Manager.Update(0));
            Assert.Throws<InvalidOperationException>(() => _host.Variables.Set(0, 99));
            _host.BeforeDispatch = null;
            // Outside execution this request must not attach a permanent waiter.
            _host.Dispatch(new ScriptRunnerAddWaiterRequest(new WaitUntilCanceled()));
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
        }

        [Test]
        public void ChildCallUsesVariantSpecificWaitingAndSeparateLocals()
        {
            _host = new Host(
                Block(51, Bytes(w => { Mode(w, 2); Set(w, 0, 7); Call(w, 52); IfEqual(w, 0, 7); Set(w, Marker, 1); })),
                Block(52, Bytes(w => { Set(w, 0, 9); Wait(w, 1); Set(w, OtherMarker, 1); })));
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            #if PAL3A
            Assert.That(_host.Variables.Get(Marker), Is.Zero, "PAL3A parent pauses even in grouped execution");
            #else
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1), "PAL3 calls remain asynchronous");
            #endif
            _host.Manager.Update(0);
            _host.Manager.Update(1.1f);
            Assert.That(_host.Variables.Get(OtherMarker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
        }

        #if PAL3A
        [Test]
        public void NestedCallsResumeTheirOwnParents()
        {
            _host = new Host(
                Block(51, Bytes(w => { Call(w, 52); Set(w, Marker, 1); })),
                Block(52, Bytes(w => { Call(w, 53); Set(w, OtherMarker, 1); })),
                Block(53, Bytes(w => { Wait(w, 1); Set(w, 0x8003, 1); })));
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            _host.Manager.Update(0);
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.Zero);
            Assert.That(_host.Variables.Get(OtherMarker), Is.Zero);
            _host.Manager.Update(1.1f);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(OtherMarker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(0x8003), Is.EqualTo(1));
        }

        [Test]
        public void ExistingGroupTimersContinueWhileWaitingForChild()
        {
            _host = new Host(
                Block(51, Bytes(w => { Mode(w, 2); Wait(w, 1); Call(w, 52); Mode(w, 1); Set(w, Marker, 1); })),
                Block(52, Bytes(w => Wait(w, 2))));
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            _host.Manager.Update(0);
            _host.Manager.Update(2.1f);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
        }
        #endif

        [Test]
        public void FailedChildCallDoesNotLeaveParentWaiting()
        {
            _host = new Host(Block(51, Bytes(w => { Mode(w, 2); Call(w, 0); Set(w, Marker, 1); })));
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            Assert.That(_host.Failures, Is.EqualTo(1));
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScriptStartedByAnotherCommandDoesNotPauseItsCaller(bool objectInteraction)
        {
            _host = new Host(
                Block(51, Bytes(w =>
                {
                    Mode(w, 2);
                    if (objectInteraction) { Op(w, 124); w.Write(7); }
                    else Note(w);
                    Set(w, Marker, 1);
                })),
                Block(52, Bytes(w => { Wait(w, 1); Set(w, OtherMarker, 1); })));
            _host.BeforeDispatch = command =>
            {
                if (command is PlayerInteractWithObjectCommand || command is UIDisplayNoteCommand)
                    _host.Dispatch(new ScriptExecuteCommand(52));
            };
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1), "Only a decoded command 16 pauses a PAL3A caller");
            Assert.That(_host.Variables.Get(OtherMarker), Is.Zero);
            _host.Manager.Update(0);
            _host.Manager.Update(1.1f);
            Assert.That(_host.Variables.Get(OtherMarker), Is.EqualTo(1));
        }

        [Test]
        public void StartingSceneScriptDoesNotAdvanceOtherTimersByAnArtificialSecond()
        {
            _host = new Host(
                Block(51, Bytes(w => { Wait(w, .5f); Set(w, Marker, 1); })),
                Block(52, Bytes(Note)));
            var loader = Block(1000, Bytes(w => { Wait(w, .5f); Set(w, OtherMarker, 1); }));
            var scene = new SceFile(Array.Empty<SceIndex>(), new[] { loader }, 936);
            _host.BeforeDispatch = command =>
            {
                if (command is UIDisplayNoteCommand)
                    Assert.That(_host.Manager.TryAddSceneScript(scene, loader.Description, out _), Is.True);
            };
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            _host.Manager.AddScript(52);
            _host.Manager.Update(.25f);
            Assert.That(_host.Variables.Get(Marker), Is.Zero);
            Assert.That(_host.Variables.Get(OtherMarker), Is.Zero);
            _host.Manager.Update(.3f);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Variables.Get(OtherMarker), Is.Zero);
            _host.Manager.Update(.21f);
            Assert.That(_host.Variables.Get(OtherMarker), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResetClearsPendingAndRunningScriptsWithoutCompletionNotifications(bool startFirst)
        {
            _host = new Host(
                Block(51, Bytes(w => { Call(w, 52); Wait(w, 3); Set(w, Marker, 1); })),
                Block(52, Bytes(w => Wait(w, 3))));
            _host.Manager.AddScript(51);
            if (startFirst) _host.Manager.Update(0);
            _host.Dispatch(new ResetGameStateCommand());
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
            _host.Manager.Update(10);
            Assert.That(_host.Completions, Is.Zero);
            Assert.That(_host.Variables.Get(Marker), Is.Zero);
        }

        [Test]
        public void ResetInsideCommandStopsOldUpdateImmediately()
        {
            _host = new Host(Block(51, Bytes(w => { Note(w); Set(w, Marker, 1); })));
            _host.BeforeDispatch = c => { if (c is UIDisplayNoteCommand) _host.Dispatch(new ResetGameStateCommand()); };
            _host.Manager.AddScript(51);
            Assert.DoesNotThrow(() => _host.Manager.Update(0));
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
            _host.Manager.Update(1);
            Assert.That(_host.Variables.Get(Marker), Is.Zero);
            Assert.That(_host.Completions, Is.Zero);
        }

        [Test]
        public void ResetFromCompletionDoesNotPublishRestOfOldBatch()
        {
            _host = new Host(Block(51, Array.Empty<byte>()), Block(52, Array.Empty<byte>()));
            _host.BeforeDispatch = c =>
            {
                if (c is ScriptFinishedRunningNotification) _host.Dispatch(new ResetGameStateCommand());
            };
            _host.Manager.AddScript(51);
            _host.Manager.AddScript(52);
            Assert.DoesNotThrow(() => _host.Manager.Update(0));
            _host.Manager.Update(1);
            Assert.That(_host.Completions, Is.EqualTo(1), "Only the notification that triggered reset is delivered");
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
        }

        [Test]
        public void InterruptedUpdateDoesNotDuplicateAlreadyCollectedCompletions()
        {
            _host = new Host(Block(51, Bytes(w => { Note(w); Set(w, Marker, 1); })), Block(52, Array.Empty<byte>()));
            _host.BeforeDispatch = c => { if (c is UIDisplayNoteCommand) throw new InvalidOperationException("Interrupted batch"); };
            _host.Manager.AddScript(51);
            _host.Manager.AddScript(52); // Collected as finished before script 51 throws.
            Assert.Throws<InvalidOperationException>(() => _host.Manager.Update(0));
            _host.BeforeDispatch = null;
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(_host.Completions, Is.EqualTo(2));
            Assert.That(_host.Manager.GetNumberOfRunningScripts(), Is.Zero);
        }

        [Test]
        public void ResetCancelsExternalWaitersBeforeScriptIdsCanBeReused()
        {
            _host = new Host(Block(51, Bytes(w => Set(w, Marker, 1))));
            using var completion = new WaitUntilScriptFinished(PalScriptType.System, 51);
            _host.Manager.AddScript(51);
            _host.Dispatch(new ResetGameStateCommand());
            Assert.That(completion.keepWaiting, Is.False);
            Assert.That(completion.IsCanceled, Is.True);
            Assert.That(CommandExecutorRegistry<ICommand>.Instance.GetRegisteredExecutors<ResetGameStateCommand>()
                .Any(executor => ReferenceEquals(executor, completion)), Is.False);
            Assert.That(CommandExecutorRegistry<ICommand>.Instance.GetRegisteredExecutors<ScriptFinishedRunningNotification>()
                .Any(executor => ReferenceEquals(executor, completion)), Is.False);
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            Assert.That(_host.Variables.Get(Marker), Is.EqualTo(1));
            Assert.That(completion.IsCanceled, Is.True, "A new instance cannot complete the canceled interaction");
        }

        [Test]
        public void ResetAfterCompletionStillCancelsBeforeCoroutineConsumesResult()
        {
            _host = new Host(Block(51, Array.Empty<byte>()));
            using var completion = new WaitUntilScriptFinished(PalScriptType.System, 51);
            _host.Dispatch(new ScriptFinishedRunningNotification(51, PalScriptType.Scene));
            _host.Dispatch(new ScriptFinishedRunningNotification(52, PalScriptType.System));
            Assert.That(completion.keepWaiting, Is.True, "Type and ID must both match");
            _host.Manager.AddScript(51);
            _host.Manager.Update(0);
            Assert.That(completion.keepWaiting, Is.False);
            Assert.That(completion.IsCanceled, Is.False);
            _host.Dispatch(new ResetGameStateCommand());
            Assert.That(completion.IsCanceled, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisposingExternalWaiterRemovesBothSubscriptions(bool completeFirst)
        {
            _host = new Host(Block(51, Array.Empty<byte>()));
            var completion = new WaitUntilScriptFinished(PalScriptType.System, 51);
            if (completeFirst) _host.Dispatch(new ScriptFinishedRunningNotification(51, PalScriptType.System));
            completion.Dispose();
            Assert.That(completion.keepWaiting, Is.False);
            Assert.That(completion.IsCanceled, Is.EqualTo(!completeFirst));
            Assert.That(CommandExecutorRegistry<ICommand>.Instance.GetRegisteredExecutors<ResetGameStateCommand>()
                .Any(executor => ReferenceEquals(executor, completion)), Is.False);
            Assert.That(CommandExecutorRegistry<ICommand>.Instance.GetRegisteredExecutors<ScriptFinishedRunningNotification>()
                .Any(executor => ReferenceEquals(executor, completion)), Is.False);
        }

        [Test]
        public void ResetUnwindsNestedObjectInteractionsWithoutResumingTheirParents()
        {
            _host = new Host(Block(51, Array.Empty<byte>()));
            bool canceled = false;
            int resumed = 0, disposed = 0;
            WaitUntilScriptFinished completion = null;
            IEnumerator Child()
            {
                try
                {
                    using (completion = new WaitUntilScriptFinished(PalScriptType.System, 51))
                    {
                        yield return completion;
                        resumed++;
                    }
                }
                finally { disposed++; }
            }
            IEnumerator Parent()
            {
                try { yield return Child(); resumed++; }
                finally { disposed++; }
            }
            var interaction = PlayerGamePlayManager.RunInteractionAsync(Parent(), () => canceled);
            Assert.That(interaction.MoveNext(), Is.True);
            _host.Dispatch(new ResetGameStateCommand());
            canceled = true;
            Assert.That(interaction.MoveNext(), Is.False);
            Assert.That(completion.IsCanceled, Is.True);
            Assert.That(resumed, Is.Zero, "Neither the object nor a linked parent may run post-script actions");
            Assert.That(disposed, Is.EqualTo(2));
            Assert.That(CommandExecutorRegistry<ICommand>.Instance.GetRegisteredExecutors<ResetGameStateCommand>()
                .Any(executor => ReferenceEquals(executor, completion)), Is.False);
        }

        [Test]
        public void NestedInteractionsPreserveNormalYieldAndCompletionOrder()
        {
            object nativeYield = new();
            var order = new List<int>();
            IEnumerator Child() { order.Add(1); yield return nativeYield; order.Add(2); }
            IEnumerator Parent() { yield return Child(); order.Add(3); }
            var interaction = PlayerGamePlayManager.RunInteractionAsync(Parent(), () => false);
            Assert.That(interaction.MoveNext(), Is.True);
            Assert.That(interaction.Current, Is.SameAs(nativeYield));
            Assert.That(order, Is.EqualTo(new[] { 1 }));
            Assert.That(interaction.MoveNext(), Is.False);
            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        private sealed class Host : IDisposable,
            ICommandExecutor<ScriptFinishedRunningNotification>,
            ICommandExecutor<ScriptFailedToRunNotification>
        {
            public UserVariableManager Variables { get; } = new();
            public ScriptManager Manager { get; }
            public Action<ICommand> BeforeDispatch;
            public int Completions;
            public int Failures;
            private readonly CommandDispatcher<ICommand> _dispatcher = new(CommandExecutorRegistry<ICommand>.Instance);

            public Host(params SceScriptBlock[] blocks)
            {
                var file = new SceFile(Array.Empty<SceIndex>(), blocks, 936);
                var resolver = new SceCommandTypeResolver();
                resolver.Init();
                Manager = new ScriptManager(file, file, Variables, new SceCommandParser(resolver), new Patcher(), Dispatch);
                CommandExecutorRegistry<ICommand>.Instance.Register(this);
            }

            public void Dispatch(ICommand command)
            {
                BeforeDispatch?.Invoke(command);
                _dispatcher.TryDispatchAndExecute(command);
            }

            public void Execute(ScriptFinishedRunningNotification command) => Completions++;
            public void Execute(ScriptFailedToRunNotification command) => Failures++;
            public void Dispose()
            {
                Manager.Dispose();
                Variables.Dispose();
                CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            }
        }

        private sealed class Patcher : IPalScriptPatcher
        {
            public Dictionary<string, ICommand> PatchedCommands { get; } = new();
        }

        private static SceScriptBlock Block(uint id, byte[] bytes, int initial = 0) => new()
        {
            Id = id, Description = "Script runtime regression", ScriptData = bytes,
            UserVariables = new[] { new SceUserVarInfo { Type = 1, InitData = BitConverter.GetBytes(initial) } }
        };

        private static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) write(writer);
            return stream.ToArray();
        }

        private static void Op(BinaryWriter w, ushort id, ushort mask = 0) { w.Write(id); w.Write(mask); }
        private static void Mode(BinaryWriter w, int mode) { Op(w, 2); w.Write(mode); }
        private static void Wait(BinaryWriter w, float seconds) { Op(w, 1); w.Write(seconds); }
        private static void Set(BinaryWriter w, ushort variable, int value) { Op(w, 13, 1); w.Write(value); w.Write(variable); }
        private static void Add(BinaryWriter w, ushort variable, int value) { Op(w, 14, 1); w.Write(value); w.Write(variable); }
        private static void Call(BinaryWriter w, int script) { Op(w, 16); w.Write(script); }
        private static void Note(BinaryWriter w) { Op(w, 68); w.Write((ushort)2); w.Write((byte)'x'); w.Write((byte)0); }
        private static void IfEqual(BinaryWriter w, ushort variable, int value)
        {
            Op(w, 5); w.Write(0);
            Op(w, 8, 1); w.Write(value); w.Write(variable);
            // An incorrect condition takes an intentionally invalid jump. The test
            // must fail instead of silently succeeding with a missing marker.
            Op(w, 12); w.Write(int.MaxValue);
        }
    }
}
