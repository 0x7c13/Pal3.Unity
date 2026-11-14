// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Original;
    using Core.FileSystem;
    using Core.Command.SceCommands;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Logging;
    using UnityEngine;

    internal sealed class EffectRuntime : IDisposable
    {
        private readonly OriginalEffectResources _original;
        private readonly HashSet<int> _failures = new();
        private readonly List<IGameEntity> _playedEffects = new();
        private readonly List<PersistentEffect> _persistentEffects = new();
        private int _preloadGeneration;
        private bool _disposed;

        internal OriginalEffectResources Original => _original;

        internal EffectRuntime(ICpkFileSystem fileSystem, int codepage)
        {
            _original = new OriginalEffectResources(fileSystem, codepage);
        }

        internal IEnumerator PreloadAsync(int groupId)
        {
            if (_disposed)
                yield break;
            int generation = _preloadGeneration;
            OriginalEffectOverrides edits;
            try
            {
                edits = Resources.Load<OriginalEffectOverrides>(_original.OverridesPath);
                string[] issues = _original.Diagnose(groupId, edits);
                if (issues.Length != 0)
                {
                    Report(groupId, string.Join("\n", issues));
                    yield break;
                }
            }
            catch (Exception ex)
            {
                Report(groupId, ex.Message);
                yield break;
            }
            using var preload = _original.Preload(groupId, edits).GetEnumerator();
            while (!_disposed && generation == _preloadGeneration)
            {
                bool next;
                try
                {
                    next = preload.MoveNext();
                }
                catch (Exception ex)
                {
                    Report(groupId, ex.Message);
                    break;
                }
                if (!next)
                    break;
                yield return null;
            }
        }

        private void Report(int groupId, string message)
        {
            if (_failures.Add(groupId))
                EngineLogger.LogWarning($"Effect {groupId}: {message}");
        }

        private IGameEntity Create(int groupId, IGameEntity parent, uint portalType = 0)
        {
            if (_disposed || parent.IsNullOrDisposed())
                return null;
            GameObject root = null;
            try
            {
                var edits = Resources.Load<OriginalEffectOverrides>(_original.OverridesPath);
                string[] issues = groupId == -1 ? Array.Empty<string>() : _original.Diagnose(groupId, edits);
                if (issues.Length != 0)
                {
                    Report(groupId, string.Join("\n", issues));
                    return null;
                }
                root = new GameObject(groupId == -1 ? "Original transport" : $"Original effect {groupId}");
                root.transform.SetParent((Transform)parent.Transform.NativeObject, false);
                if (groupId == -1)
                    root.AddComponent<OriginalPortalEffect>().Initialize(_original, portalType);
                else
                {
                    var player = root.AddComponent<OriginalEffectPlayer>();
                    player.Initialize(_original, groupId, (uint)UnityEngine.Random.Range(1, int.MaxValue), overrides: edits,
                        sound: name => Pal3.Instance.Execute(new PlaySfxCommand(name, 1)));
                    player.DestroyOnComplete = true;
                    if (player.Error != null)
                        throw new InvalidOperationException(player.Error);
                }
                return new GameEntity(root);
            }
            catch (Exception ex)
            {
                if (root != null)
                {
                    root.SetActive(false);
                    OriginalEffectResources.Destroy(root);
                }
                Report(groupId, ex.Message);
                return null;
            }
        }

        internal bool Play(int groupId, IGameEntity parent, Vector3 localPosition)
        {
            if (Create(groupId, parent) is not { } instance)
                return false;
            instance.Transform.LocalPosition += localPosition;
            _playedEffects.RemoveAll(effect => effect.IsNativeObjectDisposed);
            _playedEffects.Add(instance);
            return true;
        }

        internal IDisposable AttachPortal(IGameEntity parent, uint type = 0) => Attach(-1, parent, type);

        internal IDisposable Attach(int groupId, IGameEntity parent, uint portalType = 0)
        {
            if (_disposed || parent.IsNullOrDisposed())
                return null;
            PruneDeadAttachments();
            var effect = new PersistentEffect(this, groupId, parent, portalType);
            _persistentEffects.Add(effect);
            return effect;
        }

        internal void ClearPlayedEffects()
        {
            foreach (IGameEntity effect in _playedEffects)
                Stop(effect);
            _playedEffects.Clear();
            InvalidatePreloads();
            _failures.Clear();
            if (_disposed)
                return;
            if (Application.isPlaying && Pal3.Instance != null)
                Pal3.Instance.StartCoroutine(TrimSceneRenderAssetsAfterFrame());
            else
                _original.TrimUnusedRenderAssets(LiveRoots());
        }

        private IEnumerator TrimSceneRenderAssetsAfterFrame()
        {
            // Scene objects are destroyed at the end of the leaving frame.
            yield return null;
            if (_disposed)
                yield break;
            PruneDeadAttachments();
            _original.TrimUnusedRenderAssets(LiveRoots());
        }

        private IEnumerable<GameObject> LiveRoots()
        {
            foreach (IGameEntity effect in _playedEffects)
                if (!effect.IsNullOrDisposed())
                    yield return (GameObject)effect.NativeObject;
            foreach (PersistentEffect effect in _persistentEffects)
                if (effect.Root != null)
                    yield return effect.Root;
        }

        private static void Stop(IGameEntity effect)
        {
            if (effect.IsNullOrDisposed())
                return;
            ((GameObject)effect.NativeObject).SetActive(false);
            effect.Destroy();
        }

        private void PruneDeadAttachments()
        {
            for (int i = _persistentEffects.Count - 1; i >= 0; i--)
                if (_persistentEffects[i].ParentIsDisposed)
                    _persistentEffects[i].Dispose();
        }

        internal void InvalidatePreloads()
        {
            PruneDeadAttachments();
            _preloadGeneration++;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ClearPlayedEffects();
            foreach (PersistentEffect effect in _persistentEffects.ToArray())
                effect.Dispose();
            _original.Dispose();
        }

        private sealed class PersistentEffect : IDisposable
        {
            private readonly EffectRuntime _owner;
            private readonly IGameEntity _parent;
            private IGameEntity _instance;

            internal bool ParentIsDisposed => _parent.IsNullOrDisposed();

            internal GameObject Root => _instance.IsNullOrDisposed() ? null : (GameObject)_instance.NativeObject;

            internal PersistentEffect(EffectRuntime owner, int groupId, IGameEntity parent, uint portalType)
            {
                _owner = owner;
                _parent = parent;
                _instance = owner.Create(groupId, parent, portalType);
                if (Root != null && Root.GetComponent<OriginalEffectPlayer>() is { } player)
                {
                    player.DestroyOnComplete = false;
                    player.Loop = true;
                    player.RepeatInterval = owner._original.GameVariant == "PAL3" && groupId == 275 ? 9.5f : 0;
                }
            }

            public void Dispose()
            {
                _owner._persistentEffects.Remove(this);
                Stop(_instance);
                _instance = null;
            }
        }
    }
}
