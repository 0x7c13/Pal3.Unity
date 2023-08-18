// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build;

    public static class SymbolsHelper
    {
        public static BuildTargetGroup[] GetAllSupportedTargetGroups()
        {
            return new[]
            {
                BuildTargetGroup.Android,
                BuildTargetGroup.Standalone,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WSA,
                BuildTargetGroup.PS4,
                BuildTargetGroup.PS5,
                BuildTargetGroup.XboxOne,
                BuildTargetGroup.tvOS,
                BuildTargetGroup.Switch,
                BuildTargetGroup.GameCoreXboxOne,
                BuildTargetGroup.GameCoreXboxSeries,
                //BuildTargetGroup.VisionOS,
            };
        }

        public static NamedBuildTarget[] GetAllSupportedNamedBuildTargets()
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
                //NamedBuildTarget.VisionOS,
            };
        }

        public static bool HasSymbol(string symbol)
        {
            return GetAllSupportedTargetGroups()
                .Select(PlayerSettings.GetScriptingDefineSymbolsForGroup)
                .Select(defines => defines.Split(';').ToList())
                .All(allDefines => allDefines.Contains(symbol));
        }

        public static void AddSymbol(string symbolToAdd)
        {
            foreach (BuildTargetGroup targetGroup in GetAllSupportedTargetGroups())
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                var allDefines = defines.Split(';').ToList();
                if (!allDefines.Any(_ => string.Equals(_, symbolToAdd)))
                {
                    allDefines.Add(symbolToAdd);
                }
                allDefines.Sort();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup,
                    string.Join(";", allDefines.ToArray()));
            }
        }

        public static void RemoveSymbol(string symbolToDelete)
        {
            foreach (BuildTargetGroup targetGroup in GetAllSupportedTargetGroups())
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                var allDefines = defines.Split(';').ToList();
                var newDefines = allDefines
                    .Where(_ => !string.Equals(_, symbolToDelete)).ToList();
                newDefines.Sort();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup,
                    string.Join(";", newDefines.ToArray()));
            }
        }
    }
}