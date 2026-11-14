// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Data;
    using Engine.Core.Implementation;

    public sealed class PortalEffect : GameEntityScript, IEffect
    {
        private IDisposable _effect;

        protected override void OnDisableGameEntity() => Dispose();

        public void Init(GameResourceProvider resourceProvider, uint parameter)
        {
            _effect = resourceProvider.Effects.AttachPortal(GameEntity, parameter);
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            _effect?.Dispose();
            _effect = null;
        }
    }
}
