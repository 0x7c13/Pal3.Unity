// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using Abstraction;
    using UnityEngine;

    public sealed class UnityMaterialFactory : IMaterialFactory
    {
        public IMaterial CreateMaterial(string shaderName)
        {
            return new UnityMaterial(Shader.Find(shaderName));
        }

        public IMaterial CreateMaterialFrom(IMaterial material)
        {
            if (material is UnityMaterial unityMaterial)
            {
                return new UnityMaterial(unityMaterial.NativeObject as Material, isClone: true);
            }

            throw new ArgumentException("Null or unsupported material type.");
        }
    }
}