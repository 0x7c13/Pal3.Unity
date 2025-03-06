// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using UnityEngine;

    public static class ShaderUtility
    {
        public static int GetPropertyIdByName(string propertyName)
        {
            return Shader.PropertyToID(propertyName);
        }
    }
}