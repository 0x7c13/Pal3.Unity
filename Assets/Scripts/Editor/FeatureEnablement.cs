// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class FeatureEnablement
    {
        [MenuItem("Pal3/Extra Features/Realtime lighting and shadow/Enable")]
        public static void EnableRealtimeLightingAndShadow()
        {
            if (Resources.Load<Material>("Materials/Toon") != null)
            {
                EditorHelper.AddSymbol("RTX_ON");
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError("Failed to enable realtime lighting and shadow since RealToon material was not found.");
            }
        }

        [MenuItem("Pal3/Extra Features/Realtime lighting and shadow/Disable")]
        public static void DisableRealtimeLightingAndShadow()
        {
            EditorHelper.RemoveSymbol("RTX_ON");
            AssetDatabase.SaveAssets();
        }
    }
}