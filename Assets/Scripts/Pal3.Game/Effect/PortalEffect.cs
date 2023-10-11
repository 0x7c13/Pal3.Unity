// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using Data;
    using Engine.Abstraction;
    using Engine.Extensions;

    using Vector3 = UnityEngine.Vector3;

    public sealed class PortalEffect : GameEntityScript, IEffect
    {
        #if PAL3
        private const string PORTAL_BASE_TEXTURE_NAME = "trans.dds";
        #elif PAL3A
        private const string PORTAL_BASE_TEXTURE_NAME = "trans.tga";
        #endif

        private const float PORTAL_DEFAULT_SIZE = 1.3f;
        private const float PORTAL_ANIMATION_ROTATION_SPEED = 5f;

        private RotatingSpriteEffect _baseEffect;
        private IGameEntity _rayEffect;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider, uint _)
        {
            _baseEffect = GameEntity.AddComponent<RotatingSpriteEffect>();
            _baseEffect.Init(resourceProvider,
                PORTAL_BASE_TEXTURE_NAME,
                new Vector3(PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE),
                PORTAL_ANIMATION_ROTATION_SPEED);

            object rayEffectPrefab = resourceProvider.GetVfxEffectPrefab(0);
            if (rayEffectPrefab != null)
            {
                _rayEffect = PrefabFactory.Instantiate(rayEffectPrefab, Transform, worldPositionStays: false);
                _rayEffect.Name = "VFX_0";
            }
        }

        public void Dispose()
        {
            if (_baseEffect != null)
            {
                _baseEffect.Dispose();
                _baseEffect.Destroy();
                _baseEffect = null;
            }

            if (_rayEffect != null)
            {
                _rayEffect.Destroy();
                _rayEffect = null;
            }
        }
    }
}