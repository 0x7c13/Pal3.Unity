// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private readonly HashSet<long> _effectHandles = new();

        private long StartEffect(BattleEffectRequest request)
        {
            if (_effects == null)
                throw new InvalidOperationException("This battle has no effect director.");
            long handle = _effects.Start(request);
            if (handle <= 0 || !_effectHandles.Add(handle))
                throw new InvalidOperationException("An effect director must return a unique positive handle.");
            return handle;
        }

        private void PruneEffects()
        {
            if (_effects != null)
                _effectHandles.RemoveWhere(handle => !_effects.IsRunning(handle));
        }

        private void StopEffect(long handle)
        {
            if (!_effectHandles.Remove(handle))
                return;
            try
            {
                _effects.Stop(handle);
            }
            catch (Exception exception) when (_ending)
            {
                Add(BattleEventKind.PresentationCleanupFailed, detail: $"Effect {handle}: {exception.Message}");
            }
        }

        private void StopEffects()
        {
            _soundingMovements.Clear();
            try
            {
                (_effects as IBattleMovementAudio)?.StopBodySounds();
            }
            catch (Exception exception)
            {
                Add(BattleEventKind.PresentationCleanupFailed, detail: "Body sounds: " + exception.Message);
            }
            foreach (long handle in _effectHandles.ToArray())
                StopEffect(handle);
            _absoluteTimeActor = null;
        }
    }
}
