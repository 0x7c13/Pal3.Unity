// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    internal sealed class PrivateAssetBuildGuard : IPreprocessBuildWithReport
    {
        private const string PrivateBuildVariable = "PAL3_ALLOW_PRIVATE_ASSETS";
        private const string StoryVfxPath = "/Resources/Prefabs/VFX/";
        private const string HovlStudioPath = "/Hovl Studio/";
        private const string RealToonPath = "/RealToon/";
        private const string LegacyToonMaterialsPath = "Assets/Resources/Materials/";
        private const string Pal3VfxReadme = "Assets/Resources/Prefabs/VFX/PAL3/README.txt";
        private const string Pal3AVfxReadme = "Assets/Resources/Prefabs/VFX/PAL3A/README.txt";

        internal static bool PrivateBuildEnabled =>
            Environment.GetEnvironmentVariable(PrivateBuildVariable) == "1" &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report) => Validate();

        internal static void Validate()
        {
            if (PrivateBuildEnabled) return;

            string privateAsset = AssetDatabase.GetAllAssetPaths().FirstOrDefault(IsPrivateAsset);
            if (privateAsset == null) return;

            throw new BuildFailedException(
                $"Local optional VFX or legacy Toon assets are installed ({privateAsset}). " +
                "Use a clean checkout for public builds. " +
                $"For a private local build, launch Unity with {PrivateBuildVariable}=1.");
        }

        private static bool IsPrivateAsset(string assetPath)
        {
            if (assetPath.IndexOf(HovlStudioPath, StringComparison.OrdinalIgnoreCase) < 0 &&
                assetPath.IndexOf(RealToonPath, StringComparison.OrdinalIgnoreCase) < 0 &&
                !assetPath.StartsWith(LegacyToonMaterialsPath, StringComparison.OrdinalIgnoreCase) &&
                assetPath.IndexOf(StoryVfxPath, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return !assetPath.Equals(Pal3VfxReadme, StringComparison.OrdinalIgnoreCase) &&
                   !assetPath.Equals(Pal3AVfxReadme, StringComparison.OrdinalIgnoreCase) &&
                   !AssetDatabase.IsValidFolder(assetPath);
        }
    }
}
