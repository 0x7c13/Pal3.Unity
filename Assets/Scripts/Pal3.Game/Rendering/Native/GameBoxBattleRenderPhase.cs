// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Native
{
    using System.Collections.Generic;
    using System.Linq;
    using Engine.Extensions;
    using UnityEngine;

    // A native post-shadow scene graph must execute in URP's transparent pass,
    // including its opaque draws. Copies retain the original blend/depth state.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GameBoxBattleRenderPhase : MonoBehaviour
    {
        private sealed class Binding
        {
            public Renderer Renderer;
            public Material[] Sources;
            public Material[] Copies;

            public void Release()
            {
                if (Renderer != null)
                {
                    Material[] current = Renderer.sharedMaterials;
                    for (int i = 0; i < current.Length && i < Copies.Length; i++)
                        if (current[i] == Copies[i])
                            current[i] = Sources[i];
                    Renderer.sharedMaterials = current;
                }
                foreach (Material copy in Copies)
                    if (copy != null)
                        copy.Destroy();
            }
        }

        private readonly Dictionary<Renderer, Binding> _bindings = new();

        public IEnumerable<Material> SourceMaterials => _bindings.Values.SelectMany(binding => binding.Sources).Where(material => material != null);

        public static void Apply(GameObject root, int order)
        {
            var phase = root.GetComponent<GameBoxBattleRenderPhase>();
            if (phase == null && order > 0)
                phase = root.AddComponent<GameBoxBattleRenderPhase>();
            if (phase != null)
                phase.ApplyOrder(order);
            else
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    renderer.sortingOrder = order;
        }

        private void ApplyOrder(int order)
        {
            foreach (Renderer renderer in _bindings.Keys.Where(renderer => renderer == null || !renderer.transform.IsChildOf(transform)).ToArray())
            {
                _bindings[renderer].Release();
                _bindings.Remove(renderer);
            }
            if (order <= 0)
            {
                Release();
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                    renderer.sortingOrder = order;
                return;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingOrder = order;
                Material[] current = renderer.sharedMaterials;
                if (!_bindings.TryGetValue(renderer, out Binding binding) || binding.Copies.Length != current.Length)
                {
                    binding?.Release();
                    current = renderer.sharedMaterials;
                    binding = new Binding { Renderer = renderer, Sources = new Material[current.Length], Copies = new Material[current.Length] };
                    _bindings[renderer] = binding;
                }
                for (int i = 0; i < current.Length; i++)
                {
                    Material source = current[i] == binding.Copies[i] ? binding.Sources[i] : current[i];
                    if (!ReferenceEquals(source, binding.Sources[i]) || binding.Copies[i] == null && source != null)
                    {
                        if (binding.Copies[i] != null)
                            binding.Copies[i].Destroy();
                        binding.Sources[i] = source;
                        binding.Copies[i] = source == null ? null : new Material(source)
                        {
                            name = source.name + " (combat phase)",
                            hideFlags = HideFlags.HideAndDontSave
                        };
                    }
                    if (source != null)
                    {
                        Material copy = binding.Copies[i];
                        copy.shader = source.shader;
                        copy.CopyPropertiesFromMaterial(source);
                        copy.renderQueue = source.renderQueue <= 2500 ? 3002 : 3003;
                    }
                    current[i] = binding.Copies[i];
                }
                renderer.sharedMaterials = current;
            }
        }

        private void Release()
        {
            foreach (Binding binding in _bindings.Values)
                binding.Release();
            _bindings.Clear();
        }

        private void OnDestroy() => Release();
    }
}
