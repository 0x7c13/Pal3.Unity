// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if UNITY_EDITOR_OSX && UNITY_STANDALONE_OSX

namespace Build.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Callbacks;
    using UnityEditor.iOS.Xcode;
    using UnityEngine;

    /// <summary>
    /// Set NSPrefersDisplaySafeAreaCompatibilityMode in XCode project settings (Info.plist)
    /// </summary>
    public static class SetDisplaySafeAreaCompatibilityModeProcessor
    {
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.StandaloneOSX) return;

            bool isBuiltProjectXCodeProject = !pathToBuiltProject.EndsWith(".app", StringComparison.OrdinalIgnoreCase);

            // Get plist
            #if PAL3
            string plistPath = pathToBuiltProject + (isBuiltProjectXCodeProject ? "/PAL3" : "/Contents") + "/Info.plist";
            #elif PAL3A
            string plistPath = pathToBuiltProject + (isBuiltProjectXCodeProject ? "/PAL3A" : "/Contents") + "/Info.plist";
            #endif

            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            PlistElementDict rootDict = plist.root;

            rootDict.SetBoolean("NSPrefersDisplaySafeAreaCompatibilityMode", false);

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());

            Debug.Log("NSPrefersDisplaySafeAreaCompatibilityMode set to NO.");
        }
    }

    public class MyPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX) return;
            if (!report.summary.outputPath.EndsWith(".app",  StringComparison.OrdinalIgnoreCase)) return;

            UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(report.summary.outputPath);
            Debug.Log($"Code re-signed for macOS app: {report.summary.outputPath}");
        }
    }
}

#endif