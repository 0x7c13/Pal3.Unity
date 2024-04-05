// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if UNITY_EDITOR_WIN
namespace Editor.Theme
{
    using System.Runtime.InteropServices;
    using UnityEditor;

    public static class DarkMode
    {
        [DllImport("UnityEditorDarkMode.dll", EntryPoint = "DllMain")]
        private static extern void EnableUnityEditorDarkModeForWindows11();

        [InitializeOnLoadMethod]
        public static void EnableDarkMode()
        {
            if (System.Environment.OSVersion.Version.Major >= 10 &&
                System.Environment.OSVersion.Version.Minor >= 0)
            {
                EnableUnityEditorDarkModeForWindows11();
            }
        }
    }
}
#endif