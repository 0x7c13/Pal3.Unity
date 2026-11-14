// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using Core.DataReader.Sce;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class ResourceDataTests
    {
        [Test]
        public void UnsupportedScriptBlockDoesNotHideLaterBlocksOrDialogue()
        {
            byte[] text = Encoding.GetEncoding(936).GetBytes("景天：测试");
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((ushort)62);
                writer.Write((ushort)0);
                writer.Write((ushort)text.Length);
                writer.Write(text);
            }
            var broken = new SceScriptBlock { Id = 1, Description = "broken", ScriptData = new byte[] { 255, 255, 0, 0 }, UserVariables = Array.Empty<SceUserVarInfo>() };
            var valid = new SceScriptBlock { Id = 2, Description = "valid", ScriptData = stream.ToArray(), UserVariables = Array.Empty<SceUserVarInfo>() };
            var file = new SceFile(Array.Empty<SceIndex>(), new[] { broken, valid }, 936);
            var lines = ResourceScriptListing.Decode(valid, 936);
            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(lines[0].Opcode, Is.EqualTo(62));
            Assert.That(lines[0].VariableMask, Is.Zero);
            Assert.That(lines[0].Dialogue, Is.EqualTo("景天：测试"));
            using var output = new StringWriter();
            ResourceScriptListing.Export(file, output, true);
            Assert.That(output.ToString(), Does.Contain("ERROR in block 1"));
            Assert.That(output.ToString(), Does.Contain("BLOCK 2"));
            Assert.That(output.ToString(), Does.Contain("景天：测试"));
        }

        [TestCase(936, "仙剑三")]
        [TestCase(950, "仙劍三")]
        public void TextCopiesPreserveEncodingAndRejectUnrepresentableEdits(int codepage, string value)
        {
            var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
            try
            {
                byte[] original = Encoding.GetEncoding(codepage).GetBytes(value);
                draft.Load(original, codepage);
                Assert.That(draft.Text, Is.EqualTo(value));
                Assert.That(draft.Encode(false), Is.EqualTo(original));
                draft.Text += "😀";
                Assert.Throws<EncoderFallbackException>(() => draft.Encode(false));
                Assert.That(Encoding.UTF8.GetString(draft.Encode(true)), Is.EqualTo(draft.Text));
            }
            finally { UnityEngine.Object.DestroyImmediate(draft); }
        }

        [Test]
        public void TextBomAndUnsavedDraftSurviveSerialization()
        {
            var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
            var restored = ScriptableObject.CreateInstance<ResourceTextDraft>();
            try
            {
                byte[] data = new byte[] { 0xef, 0xbb, 0xbf, 65 };
                draft.Load(data, 936);
                Assert.That(draft.Codepage, Is.EqualTo(65001));
                Assert.That(draft.Encode(false), Is.EqualTo(data));
                draft.Text = "edited";
                draft.SourcePath = "basedata.cpk\\config.ini";
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(draft), restored);
                Assert.That(restored.Text, Is.EqualTo("edited"));
                Assert.That(restored.SourcePath, Is.EqualTo(draft.SourcePath));
                Assert.That(restored.IsDirty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(draft);
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        [Test]
        public void ShortenedTextAndForeignCatalogDoNotReuseStaleContext()
        {
            Assert.That(ResourceTextDraft.ContextAt("short", 1000), Is.Empty);
            var draft = ScriptableObject.CreateInstance<ResourceTextDraft>();
            try
            {
                draft.SourceRoot = Path.GetFullPath("Game Data/PAL3");
                Assert.That(draft.CanReadFrom(draft.SourceRoot), Is.True);
                Assert.That(draft.CanReadFrom(Path.GetFullPath("Game Data/PAL3A")), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(draft); }
        }

        [Test]
        public void UnknownDataExtensionsAreNotMisparsedAsEffects()
        {
            Assert.That(ResourceFormats.Read(new ResourceEntry("archive.cpk\\unknown.data"), new byte[4], 936), Is.Null);
            Assert.That(ResourceFormats.Read(new ResourceEntry("archive.cpk\\unknown.dat"), new byte[4], 936), Is.Null);
            Assert.That(ResourceFormats.Read(new ResourceEntry("archive.cpk\\unknown.par"), new byte[4], 936), Is.Null);
        }
    }
}
