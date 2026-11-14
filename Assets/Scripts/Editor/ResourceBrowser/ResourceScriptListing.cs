// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.DataReader;
    using Core.DataReader.Sce;
    using Newtonsoft.Json;

    public sealed class ResourceScriptLine
    {
        public long Offset;
        public ushort Opcode, VariableMask;
        public string Text, Dialogue;
        public bool IsError;
    }

    public static class ResourceScriptListing
    {
        private static readonly Lazy<SceCommandParser> Parser = new(() =>
        {
            var resolver = new SceCommandTypeResolver();
            resolver.Init();
            return new SceCommandParser(resolver);
        });

        public static List<ResourceScriptLine> Decode(SceScriptBlock block, int codepage)
        {
            var lines = new List<ResourceScriptLine>();
            using IBinaryReader reader = new SafeBinaryReader(block.ScriptData);
            while (reader.Position < reader.Length)
            {
                var line = new ResourceScriptLine { Offset = reader.Position };
                try
                {
                    line.Opcode = reader.ReadUInt16();
                    line.VariableMask = reader.ReadUInt16();
                    reader.Seek(line.Offset, SeekOrigin.Begin);
                    ICommand command = Parser.Value.ParseNextCommand(reader, codepage, out _);
                    line.Dialogue = command switch
                    {
                        DialogueRenderTextCommand text => text.DialogueText,
                        DialogueRenderTextWithTimeLimitCommand timed => timed.DialogueText,
                        _ => null,
                    };
                    line.Text = $"{line.Offset:X6}  op={line.Opcode} mask=0x{line.VariableMask:X4}  {command.GetType().Name} {JsonConvert.SerializeObject(command)}";
                }
                catch (Exception ex)
                {
                    line.IsError = true;
                    line.Text = $"{line.Offset:X6}  ERROR in block {block.Id}: {ex.Message} (remaining bytes were not decoded)";
                }
                lines.Add(line);
                if (line.IsError)
                    break;
            }
            return lines;
        }

        public static void Export(SceFile file, TextWriter writer, bool dialogueOnly)
        {
            writer.WriteLine("Decoded inspection listing. Offsets are block-relative; mask bits identify variable parameters. This is not compilable SCE source.");
            foreach (var block in file.ScriptBlocks.Values.OrderBy(b => b.Id))
            {
                writer.WriteLine($"\nBLOCK {block.Id} · {block.Description}");
                if (!dialogueOnly)
                {
                    for (int i = 0; i < block.UserVariables.Length; i++)
                        writer.WriteLine($"LOCAL {i} type={block.UserVariables[i].Type} bytes={BitConverter.ToString(block.UserVariables[i].InitData)}");
                }
                foreach (ResourceScriptLine line in Decode(block, file.Codepage))
                {
                    if (line.IsError || !dialogueOnly)
                        writer.WriteLine(line.Text);
                    else if (line.Dialogue != null)
                        writer.WriteLine($"{line.Offset:X6}  {line.Dialogue}");
                }
            }
        }
    }
}
