// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System.IO;
    using System.Text;
    using Pal3.Command;
    using SourceGenerator;
    using SourceGenerator.Base;
    using UnityEditor;
    using UnityEngine;

    public static class SourceGeneratorEditorOptions
    {
        #if PAL3
        private const string GAME_VARIANT_SYMBOL = "PAL3";
        private static string OutputFileName = $"Pal3DebugConsoleCommands";
        #elif PAL3A
        private const string GAME_VARIANT_SYMBOL = "PAL3A";
        private static string OutputFileName = $"Pal3ADebugConsoleCommands";
        #endif

        [MenuItem("Pal3/Source Generator/Generate DebugConsoleCommands.cs")]
        public static void GenerateDebugConsoleCommands()
        {
            var writePath = $"Assets/Scripts/PAL3/Command/{OutputFileName}.cs";
            var nameSpace = "Pal3.Command";
            ISourceGenerator sourceGenerator = new DebugCommandsAutoGen<ICommand>();
            GenerateSourceInternal(OutputFileName, writePath, nameSpace, sourceGenerator, true);
        }

        private static void GenerateSourceInternal(string fileName,
            string writePath,
            string nameSpace,
            ISourceGenerator sourceGenerator,
            bool overwrite)
        {
            if (!overwrite && File.Exists(writePath))
            {
                Debug.LogError($"File already generated: {writePath}\n");
                return;
            }

            Debug.Log("Generating source file: " + writePath);

            var writer = new CodeWriter
            {
                Buffer = new StringBuilder(),
                SpacesPerIndentLevel = 4,
            };

            sourceGenerator.GenerateSourceClass(writer, fileName, nameSpace);

            // Final output string
            var output = $"#if {GAME_VARIANT_SYMBOL}\n\n{writer.Buffer}\n#endif";

            // Write to file
            using StreamWriter sw = new StreamWriter(writePath);
            sw.Write(output);

            Debug.Log($"{fileName}.cs generated.");

            AssetDatabase.Refresh();
        }
    }
}