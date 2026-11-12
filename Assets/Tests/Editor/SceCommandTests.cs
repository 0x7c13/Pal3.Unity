// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Command;
    using Command.SceCommands;
    using DataReader;
    using NUnit.Framework;

    public sealed class SceCommandTests
    {
        internal static IEnumerable<Type> CommandTypes => typeof(ICommand).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<SceCommandAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<SceCommandAttribute>().CommandId).ThenBy(t => t.Name);

        [TestCaseSource(nameof(CommandTypes))]
        public void EveryCommandHasOneUnambiguousConstructorAndMatchingProtocolProperties(Type type)
        {
            PropertyInfo[] properties = SceCommandSchema.GetProperties(type);
            var parameters = type.GetConstructors().Single().GetParameters();
            Assert.That(properties.Length, Is.LessThanOrEqualTo(16));
            Assert.That(properties.Select(p => p.PropertyType), Is.EqualTo(parameters.Select(p => p.ParameterType)));
            Assert.That(properties.Select(p => p.Name.ToLowerInvariant()), Is.EqualTo(parameters.Select(p => p.Name.ToLowerInvariant())));
            Assert.That(properties.Select(p => p.Name), Is.EqualTo(type.GetProperties().OrderBy(p => p.MetadataToken).Select(p => p.Name)),
                "Review any declaration/constructor order change against the frozen protocol snapshot");
            foreach (var p in properties.Where(p => p.IsDefined(typeof(SceUserVariableAttribute))))
                Assert.That(p.PropertyType, Is.EqualTo(typeof(ushort)));
        }

        [Test]
        public void CommandIdAndVariableMaskAreUniqueAndResolveToEveryDefinition()
        {
            var definitions = SceCommandTypeResolver.BuildTypeMap(CommandTypes);
            var resolver = new SceCommandTypeResolver(); resolver.Init();
            Assert.That(definitions.Count, Is.EqualTo(CommandTypes.Count()));
            foreach (var definition in definitions)
                Assert.That(resolver.GetType((ushort)(definition.Key >> 16), (ushort)definition.Key), Is.EqualTo(definition.Value));
        }

        [Test]
        public void ProtocolMatchesReviewedVariantSnapshot()
        {
            #if PAL3
            const string variant = "PAL3";
            #else
            const string variant = "PAL3A";
            #endif
            string actual = string.Join("\n", CommandTypes.Select(type =>
            {
                var ps = SceCommandSchema.GetProperties(type);
                int mask = 0;
                for (int i = 0; i < ps.Length; i++) if (ps[i].IsDefined(typeof(SceUserVariableAttribute))) mask |= 1 << i;
                return $"{type.GetCustomAttribute<SceCommandAttribute>().CommandId:D3}|{mask:X4}|{type.Name}|" +
                    string.Join(",", ps.Select(p => $"{p.Name}:{p.PropertyType.Name}"));
            }));
            string expected = File.ReadAllText($"Assets/Tests/Editor/Baselines/SceCommands.{variant}.txt");
            Assert.That(actual, Is.EqualTo(expected.Replace("\r\n", "\n").TrimEnd()));
        }

        [Test]
        public void ParserUsesConstructorOrderEvenWhenPropertiesAreDeclaredInReverse()
        {
            using IBinaryReader reader = new SafeBinaryReader(new byte[] { 1, 0, 0, 0, 22, 0, 11, 0, 0, 0 });
            var parser = new SceCommandParser(new FixedResolver(typeof(ReorderedCommand)));
            var result = (ReorderedCommand)parser.ParseNextCommand(reader, 936, out _);
            Assert.That(result.First, Is.EqualTo(11)); Assert.That(result.Second, Is.EqualTo(22));
        }

        [Test]
        public void DuplicateOpcodeMaskAndAccidentalPropertiesAreRejected()
        {
            Assert.Throws<InvalidDataContractException>(() => SceCommandTypeResolver.BuildTypeMap(new[] { typeof(ScriptVarSetValueCommand), typeof(ScriptVarSetValueCommand) }));
            Assert.Throws<InvalidDataContractException>(() => SceCommandSchema.GetProperties(typeof(ExtraPropertyCommand)));
            Assert.Throws<InvalidDataContractException>(() => SceCommandSchema.GetProperties(typeof(WrongVariableCommand)));
            Assert.Throws<InvalidDataContractException>(() => SceCommandSchema.GetProperties(typeof(TooManyPropertiesCommand)));
        }

        [TestCase(false)] [TestCase(true)]
        public void ConsecutiveCommandsPreserveReverseParameterOrderMaskWidthAndVariantLayout(bool unsafeReader)
        {
            // Original scasmdecoder: parameters are stored from last to first; a masked variable is WORD.
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write((ushort)13); w.Write((ushort)1); w.Write(-123); w.Write((ushort)0x8001);
                w.Write((ushort)34); w.Write((ushort)0);
                #if PAL3A
                w.Write(1);
                #endif
                w.Write(2); w.Write(4.5f); w.Write(30f); w.Write(20f); w.Write(10f);
                w.Write((ushort)3); w.Write((ushort)0); w.Write(0x12345678);
            });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            var parser = Parser();
            var set = (ScriptVarSetValueCommand)parser.ParseNextCommand(r, 936, out _);
            Assert.That(set.Variable, Is.EqualTo(0x8001)); Assert.That(set.Value, Is.EqualTo(-123)); Assert.That(r.Position, Is.EqualTo(10));
            var move = (CameraMoveCommand)parser.ParseNextCommand(r, 936, out _);
            Assert.That(new[] { move.GameBoxXPosition, move.GameBoxYPosition, move.GameBoxZPosition, move.Duration }, Is.EqualTo(new[] { 10f, 20f, 30f, 4.5f }));
            Assert.That(move.CurveType, Is.EqualTo(2));
            #if PAL3A
            Assert.That(move.Synchronous, Is.EqualTo(1));
            #endif
            Assert.That(((ScriptRunnerGotoCommand)parser.ParseNextCommand(r, 936, out _)).Offset, Is.EqualTo(0x12345678));
            Assert.That(r.Position, Is.EqualTo(data.Length));
        }

        [TestCase(false, 936, "仙剑")] [TestCase(true, 936, "仙剑")]
        [TestCase(false, 950, "仙劍")] [TestCase(true, 950, "仙劍")]
        public void VariantListRestoresStackOrderAndAllFiveWireTypes(bool unsafeReader, int codepage, string text)
        {
            byte[] data = ProtocolTestData.Build(w =>
            {
                w.Write((ushort)65); w.Write((ushort)0); w.Write((ushort)5);
                w.Write((byte)4); w.Write((ushort)0x8002);
                w.Write((byte)3); w.Write((ushort)5); w.FixedString(text, 5, codepage);
                w.Write((byte)2); w.Write(123); w.Write((byte)1); w.Write(1.5f); w.Write((byte)0); w.Write(-7);
            });
            using IBinaryReader r = ProtocolTestData.Reader(data, unsafeReader);
            var selections = (DialogueAddSelectionsCommand)Parser().ParseNextCommand(r, codepage, out _);
            Assert.That(selections.Selections, Is.EqualTo(new object[] { -7, 1.5f, 123, text, (ushort)0x8002 }));
            Assert.That(r.Position, Is.EqualTo(data.Length));
        }

        [Test]
        public void InvalidIdsMasksListTagsAndTruncatedStringsAreRejected()
        {
            foreach (byte[] data in new[] { new byte[] { 255, 255, 0, 0 }, new byte[] { 13, 0, 2, 0 } })
            {
                using IBinaryReader r = new SafeBinaryReader(data);
                Assert.Throws<InvalidDataException>(() => Parser().ParseNextCommand(r, 936, out _));
            }
            using IBinaryReader tag = new SafeBinaryReader(new byte[] { 65, 0, 0, 0, 1, 0, 255 });
            Assert.Throws<NotSupportedException>(() => Parser().ParseNextCommand(tag, 936, out _));
            using IBinaryReader truncated = new SafeBinaryReader(new byte[] { 65, 0, 0, 0, 1, 0, 3, 10, 0, 65 });
            Assert.Throws<EndOfStreamException>(() => Parser().ParseNextCommand(truncated, 936, out _));
        }

        private static SceCommandParser Parser() { var resolver = new SceCommandTypeResolver(); resolver.Init(); return new SceCommandParser(resolver); }
        private sealed class FixedResolver : ISceCommandTypeResolver
        {
            private readonly Type _type;
            public FixedResolver(Type type) => _type = type;
            public void Init() { }
            public Type GetType(ushort commandId, ushort userVariableMask) => _type;
        }
        public sealed class ReorderedCommand : ICommand
        {
            public short Second { get; } public int First { get; }
            public ReorderedCommand(int first, short second) { First = first; Second = second; }
        }
        public sealed class ExtraPropertyCommand : ICommand
        {
            public int Value { get; } public int Extra => 0;
            public ExtraPropertyCommand(int value) => Value = value;
        }
        public sealed class WrongVariableCommand : ICommand
        {
            [SceUserVariable] public int Value { get; }
            public WrongVariableCommand(int value) => Value = value;
        }
        public sealed class TooManyPropertiesCommand : ICommand
        {
            public int P00 => 0; public int P01 => 0; public int P02 => 0; public int P03 => 0;
            public int P04 => 0; public int P05 => 0; public int P06 => 0; public int P07 => 0;
            public int P08 => 0; public int P09 => 0; public int P10 => 0; public int P11 => 0;
            public int P12 => 0; public int P13 => 0; public int P14 => 0; public int P15 => 0;
            public int P16 => 0;
        }
    }
}
