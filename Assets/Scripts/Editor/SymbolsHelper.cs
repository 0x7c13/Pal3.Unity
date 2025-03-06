// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System.Linq;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Build;

    public static class SymbolsHelper
    {
        public static NamedBuildTarget[] GetAllSupportedBuildTargets()
        {
            return new[]
            {
                NamedBuildTarget.Android,
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.PS4,
                NamedBuildTarget.XboxOne,
                NamedBuildTarget.tvOS,
                NamedBuildTarget.NintendoSwitch,
                NamedBuildTarget.WindowsStoreApps,
                NamedBuildTarget.VisionOS,
            };
        }

        public static bool IsSymbolDefined(string symbol)
        {
            return GetAllSupportedBuildTargets()
                .Select(PlayerSettings.GetScriptingDefineSymbols)
                .Select(defines => defines.Split(';').ToList())
                .All(allDefines => allDefines.Contains(symbol));
        }

        public static void AddSymbol(string symbolToAdd)
        {
            foreach (NamedBuildTarget target in GetAllSupportedBuildTargets())
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                List<string> allDefines = defines.Split(';').ToList();
                if (!allDefines.Contains(symbolToAdd))
                {
                    allDefines.Add(symbolToAdd);
                }
                allDefines.Sort();
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", allDefines));
            }
        }

        public static void RemoveSymbol(string symbolToDelete)
        {
            foreach (NamedBuildTarget target in GetAllSupportedBuildTargets())
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                List<string> allDefines = defines.Split(';').ToList();
                List<string> newDefines = allDefines.Where(d => !string.Equals(d, symbolToDelete)).ToList();
                newDefines.Sort();
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", newDefines));
            }
        }
    }
}