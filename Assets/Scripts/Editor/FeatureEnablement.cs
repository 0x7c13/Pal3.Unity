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
            SymbolsHelper.AddSymbol("RTX_ON");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pal3/Extra Features/Realtime lighting and shadow/Enable", true)]
        static bool ValidateEnableRealtimeLightingAndShadow()
        {
            return Resources.Load<Material>("Materials/Toon") != null && !SymbolsHelper.HasSymbol("RTX_ON");
        }

        [MenuItem("Pal3/Extra Features/Realtime lighting and shadow/Disable")]
        public static void DisableRealtimeLightingAndShadow()
        {
            SymbolsHelper.RemoveSymbol("RTX_ON");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pal3/Extra Features/Realtime lighting and shadow/Disable", true)]
        static bool ValidateDisableRealtimeLightingAndShadow()
        {
            return Resources.Load<Material>("Materials/Toon") != null && SymbolsHelper.HasSymbol("RTX_ON");
        }
    }
}