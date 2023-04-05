// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if UNITY_EDITOR_OSX && UNITY_IOS

namespace Build.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.iOS.Xcode;
    using UnityEngine;

    /// <summary>
    /// Enable iOS UIFileSharing in XCode project settings (Info.plist)
    /// </summary>
    public static class EnableFileSharingPostBuildProcessor
    {
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS) return; // Only for iOS

            // Get plist
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            PlistElementDict rootDict = plist.root;

            rootDict.SetBoolean("UIFileSharingEnabled", true);

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());

            Debug.Log("UIFileSharingEnabled set to YES.");
        }
    }
}

#endif