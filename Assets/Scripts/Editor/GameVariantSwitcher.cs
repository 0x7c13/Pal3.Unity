// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using System;
    using System.Linq;
    using Pal3.Game.Constants;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    public static class GameVariantSwitcher
    {
        #if PAL3
        [MenuItem("PAL3/Switch Variant/PAL3", priority = 1)]
        #elif PAL3A
        [MenuItem("PAL3A/Switch Variant/PAL3", priority = 1)]
        #endif
        public static void SwitchToPal3()
        {
            SymbolsHelper.RemoveSymbol("PAL3A");
            SymbolsHelper.AddSymbol("PAL3");
            ApplyPlayerSettingsForVariant("PAL3");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(GameVariantSwitcher)}] Switched to PAL3");
        }

        #if PAL3
        [MenuItem("PAL3/Switch Variant/PAL3", true)]
        #elif PAL3A
        [MenuItem("PAL3A/Switch Variant/PAL3", true)]
        #endif
        public static bool ValidateSwitchToPal3()
        {
            return !SymbolsHelper.IsSymbolDefined("PAL3");
        }

        #if PAL3
        [MenuItem("PAL3/Switch Variant/PAL3A", priority = 1)]
        #elif PAL3A
        [MenuItem("PAL3A/Switch Variant/PAL3A", priority = 1)]
        #endif
        public static void SwitchToPal3A()
        {
            SymbolsHelper.RemoveSymbol("PAL3");
            SymbolsHelper.AddSymbol("PAL3A");
            ApplyPlayerSettingsForVariant("PAL3A");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(GameVariantSwitcher)}] Switched to PAL3A");
        }

        #if PAL3
        [MenuItem("PAL3/Switch Variant/PAL3A", true)]
        #elif PAL3A
        [MenuItem("PAL3A/Switch Variant/PAL3A", true)]
        #endif
        public static bool ValidateSwitchToPal3A()
        {
            return !SymbolsHelper.IsSymbolDefined("PAL3A");
        }

        private static void ApplyPlayerSettingsForVariant(string appName)
        {
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = GameConstants.CompanyName;

            string gameIconPath = $"UI/game-icon-{appName}";
            Texture2D gameIcon = Resources.Load<Texture2D>(gameIconPath);
            if (gameIcon == null) throw new Exception($"Game icon not found: {gameIconPath}");

            // Set app icons for all supported build targets
            foreach (NamedBuildTarget target in SymbolsHelper.GetAllSupportedBuildTargets())
            {
                PlayerSettings.SetApplicationIdentifier(target,
                    $"{GameConstants.AppIdentifierPrefix}.{appName}");
                
                // Set app icon
                int[] iconSizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
                PlayerSettings.SetIcons(target,
                    Enumerable.Repeat(gameIcon, iconSizes.Length).ToArray(),
                    IconKind.Application);

                // Set iOS store icon which is required for store publishing or TestFlight
                if (target == NamedBuildTarget.iOS)
                {
                    PlayerSettings.SetIcons(target,
                        Enumerable.Repeat(gameIcon,
                            PlayerSettings.GetIconSizes(target, IconKind.Store).Length).ToArray(),
                        IconKind.Store);
                }
            }
            
            // Set default app icon (I don't know why Unity decided to use NamedBuildTarget.Unknown here but it is what it is)
            {
                int[] iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Application);
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown,
                    Enumerable.Repeat(gameIcon, iconSizes.Length).ToArray(),
                    IconKind.Application);   
            }
        }
    }
}