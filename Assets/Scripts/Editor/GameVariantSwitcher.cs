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
        [MenuItem("Pal3/Switch Variant/PAL3", priority = 1)]
        public static void SwitchToPal3()
        {
            SymbolsHelper.RemoveSymbol("PAL3A");
            SymbolsHelper.AddSymbol("PAL3");
            ApplyPlayerSettingsForVariant("PAL3");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pal3/Switch Variant/PAL3", true)]
        static bool ValidateSwitchToPal3()
        {
            return !SymbolsHelper.HasSymbol("PAL3");
        }

        [MenuItem("Pal3/Switch Variant/PAL3A", priority = 2)]
        public static void SwitchToPal3A()
        {
            SymbolsHelper.RemoveSymbol("PAL3");
            SymbolsHelper.AddSymbol("PAL3A");
            ApplyPlayerSettingsForVariant("PAL3A");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pal3/Switch Variant/PAL3A", true)]
        static bool ValidateSwitchToPal3A()
        {
            return !SymbolsHelper.HasSymbol("PAL3A");
        }

        private static void ApplyPlayerSettingsForVariant(string appName)
        {
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = GameConstants.CompanyName;

            foreach (BuildTargetGroup targetGroup in SymbolsHelper.GetAllSupportedTargetGroups())
            {
                PlayerSettings.SetApplicationIdentifier(targetGroup,
                    $"{GameConstants.AppIdentifierPrefix}.{appName}");
            }

            var gameIconPath = $"UI/game-icon-{appName}";
            var gameIcon = Resources.Load<Texture2D>(gameIconPath);
            if (gameIcon == null) throw new Exception($"Game icon not found: {gameIconPath}");

            foreach (NamedBuildTarget buildTarget in SymbolsHelper.GetAllSupportedNamedBuildTargets())
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
        }
    }
}