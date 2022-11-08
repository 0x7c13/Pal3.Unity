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
            EditorHelper.RemoveSymbol("PAL3A");
            EditorHelper.AddSymbol("PAL3");
            ApplyPlayerSettingsForVariant("PAL3");
        }

        [MenuItem("Pal3/Switch Variant/PAL3A")]
        public static void SwitchToPal3A()
        {
            EditorHelper.RemoveSymbol("PAL3");
            EditorHelper.AddSymbol("PAL3A");
            ApplyPlayerSettingsForVariant("PAL3A");
        }
        
        private static void ApplyPlayerSettingsForVariant(string appName)
        {
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = GameConstants.CompanyName;

            foreach (BuildTargetGroup targetGroup in EditorHelper.GetAllSupportedTargetGroups())
            {
                PlayerSettings.SetApplicationIdentifier(targetGroup,
                    $"{GameConstants.AppIdentifierPrefix}.{appName}");
            }

            var gameIconPath = $"UI/game-icon-{appName}";
            var gameIcon = Resources.Load<Texture2D>(gameIconPath);
            if (gameIcon == null) throw new Exception($"Game icon not found: {gameIconPath}");
            
            foreach (NamedBuildTarget buildTarget in EditorHelper.GetAllSupportedNamedBuildTargets())
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
    }
}