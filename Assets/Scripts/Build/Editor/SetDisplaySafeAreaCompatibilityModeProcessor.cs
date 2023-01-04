// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if UNITY_EDITOR_OSX

namespace Build.Editor
{
    using System.IO;
    using UnityEditor;
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

            // Get plist
            #if PAL3
            string plistPath = pathToBuiltProject + "/PAL3/Info.plist";
            #elif PAL3A
            string plistPath = pathToBuiltProject + "/PAL3A/Info.plist";
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
}

#endif