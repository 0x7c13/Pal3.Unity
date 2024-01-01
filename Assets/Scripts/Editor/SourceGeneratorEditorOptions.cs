// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System.IO;
    using System.Text;
    using Pal3.Core.Command;
    using SourceGenerator;
    using SourceGenerator.Base;
    using UnityEditor;
    using UnityEngine;

    public static class SourceGeneratorEditorOptions
    {
        #if PAL3
        private const string GAME_VARIANT_SYMBOL = "PAL3";
        private static string OutputFileName = "ConsoleCommands.PAL3.g.cs";
        #elif PAL3A
        private const string GAME_VARIANT_SYMBOL = "PAL3A";
        private static string OutputFileName = "ConsoleCommands.PAL3A.g.cs";
        #endif

        #if PAL3
        [MenuItem("PAL3/Source Generator/Generate ConsoleCommands.PAL3.g.cs", priority = 2)]
        #elif PAL3A
        [MenuItem("PAL3A/Source Generator/Generate ConsoleCommands.PAL3A.g.cs", priority = 2)]
        #endif
        public static void GenerateConsoleCommands()
        {
            var writePath = $"Assets/Scripts/Pal3.Game/Command/{OutputFileName}";
            var nameSpace = "Pal3.Game.Command";
            var className = "ConsoleCommands";
            ISourceGenerator sourceGenerator = new ConsoleCommandsAutoGen<ICommand>();
            GenerateSourceInternal(OutputFileName, writePath, className, nameSpace, sourceGenerator, true);
        }

        private static void GenerateSourceInternal(string fileName,
            string writePath,
            string className,
            string nameSpace,
            ISourceGenerator sourceGenerator,
            bool overwrite)
        {
            if (!overwrite && File.Exists(writePath))
            {
                Debug.LogError($"[{nameof(SourceGeneratorEditorOptions)}] File already generated: {writePath}\n");
                return;
            }

            Debug.Log($"[{nameof(SourceGeneratorEditorOptions)}] Generating source file: " + writePath);

            var writer = new CodeWriter
            {
                Buffer = new StringBuilder(),
                SpacesPerIndentLevel = 4,
            };

            sourceGenerator.GenerateSourceClass(writer, className, nameSpace);

            // Final output string
            var output = $"#if {GAME_VARIANT_SYMBOL}\n\n{writer.Buffer}\n#endif";

            // Write to file
            using StreamWriter sw = new StreamWriter(writePath);
            sw.Write(output);

            Debug.Log($"[{nameof(SourceGeneratorEditorOptions)}] {fileName} generated.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}