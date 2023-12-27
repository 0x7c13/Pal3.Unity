// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using Engine.Core.Abstraction;

    public abstract class MaterialManagerBase
    {
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";

        internal IMaterialFactory MaterialFactory { get; }

        protected MaterialManagerBase(IMaterialFactory materialFactory)
        {
            MaterialFactory = materialFactory;
        }

        public IMaterial CreateOpaqueSpriteMaterial()
        {
            return MaterialFactory.CreateMaterial(SPRITE_SHADER_PATH);
        }

        public IMaterial CreateOpaqueSpriteMaterial(ITexture2D texture)
        {
            var material = MaterialFactory.CreateMaterial(SPRITE_SHADER_PATH);
            material.SetMainTexture(texture);
            return material;
        }

        public void ReturnToPool(IMaterial[] materials)
        {
            if (materials == null) return;

            foreach (IMaterial material in materials)
            {
                ReturnToPool(material);
            }
        }

        protected abstract void ReturnToPool(IMaterial material);
    }
}