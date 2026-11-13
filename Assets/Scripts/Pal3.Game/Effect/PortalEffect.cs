// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Data;
    using Engine.Core.Implementation;
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
        private const int PORTAL_RAY_EFFECT_ID = 0;

        private RotatingSpriteEffect _baseEffect;
        private IDisposable _rayEffect;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider, uint _)
        {
#if PAL3
            _rayEffect = resourceProvider.OptionalVfx.AttachPortal(GameEntity);
#else
            _baseEffect = GameEntity.AddComponent<RotatingSpriteEffect>();
            _baseEffect.Init(resourceProvider,
                PORTAL_BASE_TEXTURE_NAME,
                new Vector3(PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE),
                PORTAL_ANIMATION_ROTATION_SPEED);

            _rayEffect = resourceProvider.OptionalVfx.AttachPrivate(PORTAL_RAY_EFFECT_ID, GameEntity);
#endif
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            if (_baseEffect != null)
            {
                _baseEffect.Dispose();
                _baseEffect.Destroy();
                _baseEffect = null;
            }

            _rayEffect?.Dispose();
            _rayEffect = null;
        }
    }
}
