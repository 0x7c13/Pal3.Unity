// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.Extensions
{
    public static class UnityObjectExtensions
    {
        public static void Destroy(this UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                // Model renderers are also used in isolated editor preview scenes.
                if (!UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                    return;
                }
#endif
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}
