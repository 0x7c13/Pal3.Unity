// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System.Linq;
    using Pal3.MetaData;
    using UnityEditor;

    public static class GameVariantSwitcher
    {
        [MenuItem("Pal3/Switch Variant/PAL3")]
        public static void SwitchToPal3()
        {
            RemoveSymbol("PAL3A");
            AddSymbol("PAL3");
            ApplyPlayerSettingsForVariant("PAL3");
        }

        [MenuItem("Pal3/Switch Variant/PAL3A")]
        public static void SwitchToPal3A()
        {
            RemoveSymbol("PAL3");
            AddSymbol("PAL3A");
            ApplyPlayerSettingsForVariant("PAL3A");
        }

        private static void ApplyPlayerSettingsForVariant(string appName)
        {
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = GameConstants.CompanyName;
            PlayerSettings.SetApplicationIdentifier(
                EditorUserBuildSettings.selectedBuildTargetGroup, GameConstants.AppIdentifier);
            // TODO: Add icon based on variant
            //PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, icons);
            AssetDatabase.SaveAssets();
        }

        private static void AddSymbol(string symbolToAdd)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            var allDefines = defines.Split(';').ToList();
            if (!allDefines.Any(d => string.Equals(d, symbolToAdd)))
            {
                allDefines.Add(symbolToAdd);
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join (";", allDefines.ToArray()));
        }

        private static void RemoveSymbol(string symbolToDelete)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            var allDefines = defines.Split(';').ToList();
            var newDefines = allDefines
                .Where(define => !string.Equals(define, symbolToDelete)).ToList();
            newDefines.Sort();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join (";", newDefines.ToArray()));
        }
    }
}