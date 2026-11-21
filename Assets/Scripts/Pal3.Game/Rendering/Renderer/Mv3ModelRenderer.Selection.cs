// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System.Collections.Generic;
    using UnityEngine;

    public sealed partial class Mv3ModelRenderer
    {
        private static readonly int SelectionProperty = Shader.PropertyToID("_BattleSelection");
        private MaterialPropertyBlock _selectionProperties;
        private readonly Dictionary<MeshRenderer, bool> _selectionBlocks = new();

        // Native selection replaces body RGB only. Tag-node weapons keep their
        // own lighting; invisibility/fade and native-light MPB values survive.
        public void SetSelectionLuminance(float luminance)
        {
            if (_meshEntities == null)
                return;
            _selectionProperties ??= new MaterialPropertyBlock();
            foreach (var entity in _meshEntities)
            {
                var renderer = entity?.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;
                if (luminance < 0 && !_selectionBlocks.ContainsKey(renderer))
                    continue;
                renderer.GetPropertyBlock(_selectionProperties);
                if (luminance >= 0)
                {
                    if (!_selectionBlocks.ContainsKey(renderer))
                        _selectionBlocks.Add(renderer, _selectionProperties.isEmpty && _materialManager.NativeLighting == null);
                    _selectionProperties.SetFloat(SelectionProperty, luminance);
                    renderer.SetPropertyBlock(_selectionProperties);
                }
                else
                {
                    // An otherwise empty Toon body must rejoin the SRP Batcher.
                    // Native lighting owns an existing block and keeps its values.
                    bool owned = _selectionBlocks[renderer];
                    _selectionBlocks.Remove(renderer);
                    _selectionProperties.SetFloat(SelectionProperty, -1);
                    renderer.SetPropertyBlock(owned ? null : _selectionProperties);
                }
            }
        }
    }
}
