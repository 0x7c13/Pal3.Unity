// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class FeatureEnablement
    {
        #if PAL3
        [MenuItem("PAL3/Extra Features/Realtime lighting and shadow/Enable")]
        #elif PAL3A
        [MenuItem("PAL3A/Extra Features/Realtime lighting and shadow/Enable")]
        #endif
        public static void EnableRealtimeLightingAndShadow()
        {
            SymbolsHelper.AddSymbol("RTX_ON");
            AssetDatabase.SaveAssets();
        }

        #if PAL3
        [MenuItem("PAL3/Extra Features/Realtime lighting and shadow/Enable", true)]
        #elif PAL3A
        [MenuItem("PAL3A/Extra Features/Realtime lighting and shadow/Enable", true)]
        #endif
        static bool ValidateEnableRealtimeLightingAndShadow()
        {
            return Resources.Load<Material>("Materials/Toon") != null && !SymbolsHelper.HasSymbol("RTX_ON");
        }

        #if PAL3
        [MenuItem("PAL3/Extra Features/Realtime lighting and shadow/Disable")]
        #elif PAL3A
        [MenuItem("PAL3A/Extra Features/Realtime lighting and shadow/Disable")]
        #endif
        public static void DisableRealtimeLightingAndShadow()
        {
            SymbolsHelper.RemoveSymbol("RTX_ON");
            AssetDatabase.SaveAssets();
        }

        #if PAL3
        [MenuItem("PAL3/Extra Features/Realtime lighting and shadow/Disable", true)]
        #elif PAL3A
        [MenuItem("PAL3A/Extra Features/Realtime lighting and shadow/Disable", true)]
        #endif
        static bool ValidateDisableRealtimeLightingAndShadow()
        {
            return Resources.Load<Material>("Materials/Toon") != null && SymbolsHelper.HasSymbol("RTX_ON");
        }
    }
}