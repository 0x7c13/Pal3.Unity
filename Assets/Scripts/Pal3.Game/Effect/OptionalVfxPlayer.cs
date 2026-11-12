// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Constants;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Logging;
    using Settings;
    using UnityEngine;

    internal sealed class OptionalVfxPlayer : IDisposable
    {
        private readonly GameSettings _settings;
        private readonly Dictionary<int, GameObject> _prefabs = new();
        private readonly List<IGameEntity> _playedEffects = new();
        private readonly List<PersistentEffect> _persistentEffects = new();

        private int _prefabCacheGeneration;
        private bool _disposed;
        private bool IsEnabled => !_disposed && _settings.OptionalVfxEnabled;

        internal OptionalVfxPlayer(GameSettings settings)
        {
            _settings = settings;
            _settings.OnGameSettingsChanged += OnGameSettingsChanged;
        }

        private static string GetPrefabPath(int effectGroupId) => $"Prefabs/VFX/{GameConstants.AppName}/{effectGroupId}";

        internal IEnumerator PreloadAsync(int effectGroupId)
        {
            if (!IsEnabled || _prefabs.ContainsKey(effectGroupId)) yield break;

            int generation = _prefabCacheGeneration;
            ResourceRequest request = Resources.LoadAsync<GameObject>(GetPrefabPath(effectGroupId));
            yield return request;

            if (IsEnabled && generation == _prefabCacheGeneration && !_prefabs.ContainsKey(effectGroupId))
            {
                CachePrefab(effectGroupId, request.asset as GameObject);
            }
        }

        private void CachePrefab(int effectGroupId, GameObject prefab)
        {
            _prefabs[effectGroupId] = prefab;
            if (prefab == null)
            {
                EngineLogger.LogWarning($"VFX prefab not found: {effectGroupId}");
            }
        }

        private IGameEntity Create(int effectGroupId, IGameEntity parent)
        {
            if (!IsEnabled || parent.IsNullOrDisposed()) return null;

            if (!_prefabs.TryGetValue(effectGroupId, out GameObject prefab))
            {
                prefab = Resources.Load<GameObject>(GetPrefabPath(effectGroupId));
                CachePrefab(effectGroupId, prefab);
            }

            return prefab == null ? null :
                GameEntityFactory.Create($"VFX_{effectGroupId}", prefab, parent, worldPositionStays: false);
        }

        internal void Play(int effectGroupId, IGameEntity parent, Vector3 localPosition)
        {
            if (Create(effectGroupId, parent) is not {} instance) return;

            instance.Transform.LocalPosition += localPosition;
            _playedEffects.RemoveAll(effect => effect.IsNativeObjectDisposed);
            _playedEffects.Add(instance);
        }

        internal IDisposable Attach(int effectGroupId, IGameEntity parent)
        {
            if (_disposed || parent.IsNullOrDisposed()) return null;
            PruneDeadAttachments();
            var effect = new PersistentEffect(this, effectGroupId, parent);
            _persistentEffects.Add(effect);
            effect.Refresh();
            return effect;
        }

        private void OnGameSettingsChanged(string propertyName)
        {
            if (propertyName != nameof(GameSettings.OptionalVfxEnabled)) return;

            if (!IsEnabled) ClearPlayedEffects();

            foreach (PersistentEffect effect in _persistentEffects.ToArray())
            {
                effect.Refresh();
            }
        }

        internal void ClearPlayedEffects()
        {
            foreach (IGameEntity effect in _playedEffects)
            {
                Stop(effect);
            }
            _playedEffects.Clear();
        }

        private static void Stop(IGameEntity effect)
        {
            if (effect.IsNullOrDisposed()) return;

            // Destroy is deferred to the end of the frame.
            ((GameObject)effect.NativeObject).SetActive(false);
            effect.Destroy();
        }

        private void PruneDeadAttachments()
        {
            for (int i = _persistentEffects.Count - 1; i >= 0; i--)
            {
                if (_persistentEffects[i].ParentIsDisposed) _persistentEffects[i].Dispose();
            }
        }

        internal void ClearPrefabCache()
        {
            PruneDeadAttachments();
            _prefabCacheGeneration++;
            _prefabs.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
            _settings.OnGameSettingsChanged -= OnGameSettingsChanged;
            ClearPlayedEffects();

            foreach (PersistentEffect effect in _persistentEffects.ToArray())
            {
                effect.Dispose();
            }
            ClearPrefabCache();
        }

        private sealed class PersistentEffect : IDisposable
        {
            private readonly OptionalVfxPlayer _player;
            private readonly int _effectGroupId;
            private readonly IGameEntity _parent;
            private IGameEntity _instance;
            private bool _disposed;

            internal PersistentEffect(OptionalVfxPlayer player, int effectGroupId, IGameEntity parent)
            {
                _player = player;
                _effectGroupId = effectGroupId;
                _parent = parent;
            }

            internal bool ParentIsDisposed => _parent.IsNullOrDisposed();

            internal void Refresh()
            {
                if (_disposed) return;
                if (ParentIsDisposed)
                {
                    Dispose();
                    return;
                }

                if (_player.IsEnabled)
                {
                    if (_instance.IsNullOrDisposed()) _instance = _player.Create(_effectGroupId, _parent);
                }
                else
                {
                    Stop(_instance);
                    _instance = null;
                }
            }

            public void Dispose()
            {
                _disposed = true;
                _player._persistentEffects.Remove(this);
                Stop(_instance);
                _instance = null;
            }
        }
    }
}
