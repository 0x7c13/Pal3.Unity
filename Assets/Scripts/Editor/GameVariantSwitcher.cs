// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System;
    using System.Linq;
    using Pal3.MetaData;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

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

        private static BuildTargetGroup[] GetAllSupportedTargetGroups()
        {
            return new[]
            {
                BuildTargetGroup.Android,
                BuildTargetGroup.Standalone,
                BuildTargetGroup.iOS,
            };
        }
        
        private static NamedBuildTarget[] GetAllSupportedNamedBuildTargets()
        {
            return new[]
            {
                NamedBuildTarget.Android,
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
            };
        }
        
        private static void ApplyPlayerSettingsForVariant(string appName)
        {
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = GameConstants.CompanyName;

            foreach (BuildTargetGroup targetGroup in GetAllSupportedTargetGroups())
            {
                PlayerSettings.SetApplicationIdentifier(targetGroup,
                    $"{GameConstants.AppIdentifierPrefix}.{appName}");
            }

            var gameIconPath = $"UI/game-icon-{appName}";
            var gameIcon = Resources.Load<Texture2D>(gameIconPath);
            if (gameIcon == null) throw new Exception($"Game icon not found: {gameIconPath}");
            
            foreach (NamedBuildTarget buildTarget in GetAllSupportedNamedBuildTargets())
            {
                // Set app icon
                var iconSizes = PlayerSettings.GetIconSizes(buildTarget, IconKind.Application);
                PlayerSettings.SetIcons(buildTarget,
                    Enumerable.Repeat(gameIcon, iconSizes.Length).ToArray(),
                    IconKind.Application);
                
                // Set iOS store icon which is required for store publishing or TestFlight
                if (buildTarget == NamedBuildTarget.iOS)
                {
                    PlayerSettings.SetIcons(buildTarget,
                        Enumerable.Repeat(gameIcon,
                            PlayerSettings.GetIconSizes(buildTarget, IconKind.Store).Length).ToArray(),
                        IconKind.Store);
                }
            }   

            AssetDatabase.SaveAssets();
        }

        private static void AddSymbol(string symbolToAdd)
        {
            foreach (BuildTargetGroup targetGroup in GetAllSupportedTargetGroups())
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                var allDefines = defines.Split(';').ToList();
                if (!allDefines.Any(_ => string.Equals(_, symbolToAdd)))
                {
                    allDefines.Add(symbolToAdd);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup,
                    string.Join(";", allDefines.ToArray()));
            }
        }

        private static void RemoveSymbol(string symbolToDelete)
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