// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Extensions
{
    public static class UnityObjectExtensions
    {
        public static void Destroy(this UnityEngine.Object obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}