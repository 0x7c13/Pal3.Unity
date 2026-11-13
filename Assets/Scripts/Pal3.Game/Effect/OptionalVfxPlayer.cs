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
    using Original;
    using Core.FileSystem;
    using Core.Command.SceCommands;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Logging;
    using Settings;
    using UnityEngine;

    internal sealed class OptionalVfxPlayer : IDisposable
    {
        private readonly GameSettings _settings;
        private readonly OriginalEffectResources _original;
        private readonly HashSet<int> _originalFailures = new();

        internal OriginalEffectResources Original => _original;

        private readonly Dictionary<int, GameObject> _prefabs = new();
        private readonly List<IGameEntity> _playedEffects = new();
        private readonly List<PersistentEffect> _persistentEffects = new();

        private int _prefabCacheGeneration;
        private bool _disposed;

        private bool IsEnabled => !_disposed && (_original != null || _settings.OptionalVfxEnabled);

        internal OptionalVfxPlayer(GameSettings settings, ICpkFileSystem fileSystem, int codepage)
        {
            _settings = settings;
            _original = new OriginalEffectResources(fileSystem, codepage);
            _settings.OnGameSettingsChanged += OnGameSettingsChanged;
        }

        private static string GetPrefabPath(int effectGroupId) => $"Prefabs/VFX/{GameConstants.AppName}/{(effectGroupId == -1 ? 0 : effectGroupId)}";

        internal IEnumerator PreloadAsync(int effectGroupId)
        {
            if (!IsEnabled || _prefabs.ContainsKey(effectGroupId))
                yield break;

            int generation = _prefabCacheGeneration;
            bool useOriginal = false;
            OriginalEffectOverrides edits = null;
            try
            {
                edits = Resources.Load<OriginalEffectOverrides>(_original.OverridesPath);
                useOriginal = _original != null && effectGroupId >= 0 && _original.Diagnose(effectGroupId, edits).Length == 0;
            }
            catch (Exception ex)
            {
                if (_originalFailures.Add(effectGroupId))
                    EngineLogger.LogWarning(ex.Message);
            }
            if (useOriginal)
            {
                using var preload = _original.Preload(effectGroupId, edits).GetEnumerator();
                while (IsEnabled && generation == _prefabCacheGeneration)
                {
                    bool next;
                    try
                    {
                        next = preload.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        EngineLogger.LogWarning($"Original effect preload {effectGroupId}: {ex.Message}");
                        break;
                    }
                    if (!next)
                        break;
                    yield return null;
                }
                yield break;
            }
            if (!_settings.OptionalVfxEnabled)
                yield break;
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

        private IGameEntity Create(int effectGroupId, IGameEntity parent, (IGameEntity parent, Vector3 position)? privateFallback = null, bool allowOriginal = true, uint portalType = 0)
        {
            if (!IsEnabled || parent.IsNullOrDisposed())
                return null;

            if (allowOriginal && _original != null)
            {
                GameObject native = null;
                try
                {
                    var edits = Resources.Load<OriginalEffectOverrides>(_original.OverridesPath);
                    string[] issues = effectGroupId == -1 ? Array.Empty<string>() : _original.Diagnose(effectGroupId, edits);
                    if (issues.Length == 0)
                    {
                        native = new GameObject(effectGroupId == -1 ? "Original transport" : $"Original effect {effectGroupId}");
                        native.transform.SetParent((Transform)parent.Transform.NativeObject, false);
                        if (effectGroupId == -1)
                            native.AddComponent<OriginalPortalEffect>().Initialize(_original, portalType);
                        else
                        {
                            var player = native.AddComponent<OriginalEffectPlayer>();
                            player.Initialize(_original, effectGroupId, (uint)UnityEngine.Random.Range(1, int.MaxValue), overrides: edits,
                                sound: name => Pal3.Instance.Execute(new PlaySfxCommand(name, 1)));
                            player.DestroyOnComplete = true;
                            if (player.Error != null)
                                throw new InvalidOperationException(player.Error);
                        }
                        return new GameEntity(native);
                    }
                    if (_originalFailures.Add(effectGroupId))
                        EngineLogger.LogWarning(string.Join("\n", issues));
                }
                catch (Exception ex)
                {
                    if (native != null)
                    {
                        native.SetActive(false);
                        OriginalEffectResources.Destroy(native);
                    }
                    if (_originalFailures.Add(effectGroupId))
                        EngineLogger.LogWarning($"Original effect {effectGroupId}: {ex.Message}");
                }
            }
            if (!_settings.OptionalVfxEnabled)
                return null;
            if (!_prefabs.TryGetValue(effectGroupId, out GameObject prefab))
            {
                prefab = Resources.Load<GameObject>(GetPrefabPath(effectGroupId));
                CachePrefab(effectGroupId, prefab);
            }

            IGameEntity prefabParent = privateFallback.HasValue ? privateFallback.Value.parent : parent;
            return prefab == null || prefabParent.IsNullOrDisposed() ? null :
                GameEntityFactory.Create($"VFX_{effectGroupId}", prefab, prefabParent, worldPositionStays: false);
        }

        internal bool Play(int effectGroupId, IGameEntity parent, Vector3 localPosition,
            (IGameEntity parent, Vector3 position)? privateFallback = null)
        {
            if (Create(effectGroupId, parent, privateFallback) is not { } instance)
                return false;

            instance.Transform.LocalPosition += IsOriginal(instance) ? localPosition : privateFallback?.position ?? localPosition;
            _playedEffects.RemoveAll(effect => effect.IsNativeObjectDisposed);
            _playedEffects.Add(instance);
            return IsOriginal(instance);
        }

        internal IDisposable AttachPortal(IGameEntity parent, uint type = 0) => Attach(-1, parent, portalType: type);

        private static bool IsOriginal(IGameEntity instance)
        {
            if (instance.IsNullOrDisposed())
                return false;
            var go = (GameObject)instance.NativeObject;
            return go.GetComponent<OriginalEffectPlayer>() != null || go.GetComponent<OriginalPortalEffect>() != null;
        }

        internal IDisposable AttachPrivate(int effectGroupId, IGameEntity parent) => Attach(effectGroupId, parent, false);

        internal IDisposable Attach(int effectGroupId, IGameEntity parent, bool allowOriginal = true, uint portalType = 0)
        {
            if (_disposed || parent.IsNullOrDisposed())
                return null;
            PruneDeadAttachments();
            var effect = new PersistentEffect(this, effectGroupId, parent, allowOriginal, portalType);
            _persistentEffects.Add(effect);
            effect.Refresh();
            return effect;
        }

        private void OnGameSettingsChanged(string propertyName)
        {
            if (propertyName != nameof(GameSettings.OptionalVfxEnabled))
                return;

            if (!_settings.OptionalVfxEnabled)
                for (int i = _playedEffects.Count - 1; i >= 0; i--)
                    if (!IsOriginal(_playedEffects[i]))
                    {
                        Stop(_playedEffects[i]);
                        _playedEffects.RemoveAt(i);
                    }

            foreach (PersistentEffect effect in _persistentEffects.ToArray())
            {
                effect.Refresh(reconsiderPrivate: true);
            }
        }

        internal void ClearPlayedEffects()
        {
            foreach (IGameEntity effect in _playedEffects)
            {
                Stop(effect);
            }
            _playedEffects.Clear();
            if (_disposed || _original == null)
                return;
            ClearPrefabCache();
            _originalFailures.Clear();
            if (Application.isPlaying && Pal3.Instance != null)
                Pal3.Instance.StartCoroutine(TrimSceneRenderAssetsAfterFrame());
            else
                _original.TrimUnusedRenderAssets(LiveOriginalRoots());
        }

        private IEnumerator TrimSceneRenderAssetsAfterFrame()
        {
            // Scene objects are destroyed at the end of the leaving frame. Keep
            // any effects already created for the next scene while evicting old
            // render assets; parsed tables/particle data remain reusable.
            yield return null;
            if (_disposed)
                yield break;
            PruneDeadAttachments();
            _original.TrimUnusedRenderAssets(LiveOriginalRoots());
        }

        private IEnumerable<GameObject> LiveOriginalRoots()
        {
            foreach (IGameEntity effect in _playedEffects)
                if (IsOriginal(effect))
                    yield return (GameObject)effect.NativeObject;
            foreach (PersistentEffect effect in _persistentEffects)
                if (effect.OriginalRoot != null)
                    yield return effect.OriginalRoot;
        }

        private static void Stop(IGameEntity effect)
        {
            if (effect.IsNullOrDisposed())
                return;

            // Destroy is deferred to the end of the frame.
            ((GameObject)effect.NativeObject).SetActive(false);
            effect.Destroy();
        }

        private void PruneDeadAttachments()
        {
            for (int i = _persistentEffects.Count - 1; i >= 0; i--)
            {
                if (_persistentEffects[i].ParentIsDisposed)
                    _persistentEffects[i].Dispose();
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
            _original?.Dispose();
        }

        private sealed class PersistentEffect : IDisposable
        {
            private readonly OptionalVfxPlayer _player;
            private readonly int _effectGroupId;
            private readonly IGameEntity _parent;
            private IGameEntity _instance;
            private bool _disposed;
            private readonly bool _allowOriginal;
            private readonly uint _portalType;

            internal PersistentEffect(OptionalVfxPlayer player, int effectGroupId, IGameEntity parent, bool allowOriginal, uint portalType)
            {
                _player = player;
                _effectGroupId = effectGroupId;
                _parent = parent;
                _allowOriginal = allowOriginal;
                _portalType = portalType;
            }

            internal bool ParentIsDisposed => _parent.IsNullOrDisposed();
            internal GameObject OriginalRoot => IsOriginal(_instance) ? (GameObject)_instance.NativeObject : null;

            internal void Refresh(bool reconsiderPrivate = false)
            {
                if (_disposed)
                    return;
                if (ParentIsDisposed)
                {
                    Dispose();
                    return;
                }

                if (reconsiderPrivate && !IsOriginal(_instance))
                {
                    Stop(_instance);
                    _instance = null;
                }
                if (_player.IsEnabled)
                {
                    if (_instance.IsNullOrDisposed())
                    {
                        _instance = _player.Create(_effectGroupId, _parent, allowOriginal: _allowOriginal, portalType: _portalType);
                        if (!_instance.IsNullOrDisposed() && ((GameObject)_instance.NativeObject).GetComponent<OriginalEffectPlayer>() is { } native)
                        {
                            native.DestroyOnComplete = false;
                            native.Loop = true;
                            native.RepeatInterval = _player._original.GameVariant == "PAL3" && _effectGroupId == 275 ? 9.5f : 0;
                        }
                    }
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
