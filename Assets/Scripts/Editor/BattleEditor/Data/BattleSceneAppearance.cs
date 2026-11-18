// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataReader.Pol;
    using Game.Rendering.Renderer;
    using Game.Rendering.Toon;

    public readonly struct BattleSurfaceOverride
    {
        public int NodeIndex
        {
            get;
        }

        public int MaterialGroupIndex
        {
            get;
        }

        public string NodeName
        {
            get;
        }

        public string Texture
        {
            get;
        }

        public PolSurfaceAppearance Appearance
        {
            get;
        }

        public BattleSurfaceOverride(int nodeIndex, int materialGroupIndex, string nodeName, string texture, PolSurfaceAppearance appearance)
        {
            if (nodeIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeIndex), "Surface override indices cannot be negative.");
            if (materialGroupIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(materialGroupIndex), "Surface override indices cannot be negative.");
            if (string.IsNullOrWhiteSpace(nodeName) || string.IsNullOrWhiteSpace(texture))
                throw new ArgumentException("A surface override requires its expected node name and texture.");
            appearance.Validate();
            NodeIndex = nodeIndex;
            MaterialGroupIndex = materialGroupIndex;
            NodeName = nodeName;
            Texture = texture;
            Appearance = appearance;
        }
    }

    /// <summary>Captured visual choices with explicit, resource-validated surface overrides.</summary>
    public sealed class BattleSceneAppearance
    {
        public ToonLightingProfile Lighting
        {
            get;
        }

        public IReadOnlyList<BattleSurfaceOverride> Surfaces
        {
            get;
        }

        public BattleSceneAppearance(ToonLightingProfile lighting, IEnumerable<BattleSurfaceOverride> surfaces = null)
        {
            lighting.Validate();
            BattleSurfaceOverride[] captured = surfaces?.ToArray() ?? Array.Empty<BattleSurfaceOverride>();
            var selected = new HashSet<(int Node, int Group)>();
            foreach (BattleSurfaceOverride surface in captured)
            {
                // A value type's default value also needs validation at this boundary.
                if (surface.NodeIndex < 0 || surface.MaterialGroupIndex < 0 ||
                    string.IsNullOrWhiteSpace(surface.NodeName) || string.IsNullOrWhiteSpace(surface.Texture))
                    throw new ArgumentException("A surface override requires nonnegative indices, a node name and a texture.", nameof(surfaces));
                surface.Appearance.Validate();
                if (!selected.Add((surface.NodeIndex, surface.MaterialGroupIndex)))
                    throw new ArgumentException("Duplicate surface override selection.", nameof(surfaces));
            }
            Lighting = lighting;
            Surfaces = Array.AsReadOnly(captured);
        }

        public Func<int, int, PolSurfaceAppearance?> ResolveSurfaces(PolFile model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (Surfaces.Count == 0)
                return null;
            var selected = new Dictionary<(int Node, int Group), PolSurfaceAppearance>();
            foreach (BattleSurfaceOverride surface in Surfaces)
            {
                int node = surface.NodeIndex;
                int group = surface.MaterialGroupIndex;
                if (model.Meshes == null || model.NodeDescriptions == null ||
                    node >= model.Meshes.Length || node >= model.NodeDescriptions.Length ||
                    !string.Equals(model.NodeDescriptions[node].Name, surface.NodeName, StringComparison.Ordinal))
                    throw new InvalidDataException($"Surface override node does not match this POL: {node} ({surface.NodeName}).");
                PolTexture[] groups = model.Meshes[node].Textures;
                if (groups == null || group >= groups.Length ||
                    groups[group].Material.TextureFileNames == null ||
                    !groups[group].Material.TextureFileNames.Contains(surface.Texture, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Surface override material does not match this POL: node {node}, group {group}, texture {surface.Texture}.");
                selected.Add((node, group), surface.Appearance);
            }
            // The resolver owns this dictionary; neither authored arrays nor mutable POL arrays are captured.
            return (node, group) => selected.TryGetValue((node, group), out PolSurfaceAppearance appearance) ? appearance : null;
        }
    }
}
