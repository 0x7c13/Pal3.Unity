// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Game.Rendering.Renderer;
    using Game.Rendering.Toon;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;

    /// <summary>Authored visual choices for battle resources, not original binary fields or gameplay rules.</summary>
    public static class BattleToonLightingCatalog
    {
        private sealed class Document
        {
            public int schemaVersion = 0;
            public Entry[] profiles = null;
        }

        private sealed class Entry
        {
            public string variant = null;
            public string scene = null;
            public string label = null;
            public float[] keyEuler = null;
            public float temperature = float.NaN;
            public float[] tint = null;
            public float intensity = float.NaN;
            public float shadowStrength = float.NaN;
            public float characterFill = float.NaN;
            public float contrast = float.NaN;
            public float bakedStrength = float.NaN;
            public float contactStrength = float.NaN;
            public bool night = false;
            public bool indoors = false;
            public BackgroundSurface[] backgroundSurfaces = null;
            public SurfaceReference[] geometricNormalSurfaces = null;

            public BattleSceneAppearance Capture()
            {
                if (keyEuler == null || keyEuler.Length != 3 || tint == null || tint.Length != 3)
                    throw new InvalidDataException("A scene lighting entry requires three rotation and tint components.");
                foreach (float value in keyEuler)
                    if (!float.IsFinite(value) || Math.Abs(value) > 360)
                        throw new InvalidDataException("Scene lighting rotation is not finite or is outside one revolution.");
                if (!float.IsFinite(temperature) || temperature < 1000 || temperature > 12000)
                    throw new InvalidDataException("Scene lighting temperature must be between 1000K and 12000K.");
                foreach (float value in tint)
                    if (!float.IsFinite(value) || value < 0 || value > 1)
                        throw new InvalidDataException("Scene lighting tint must be finite and between zero and one.");
                Color kelvin = Mathf.CorrelatedColorTemperatureToRGB(temperature);
                Color color = new(kelvin.r * tint[0], kelvin.g * tint[1], kelvin.b * tint[2], 1);
                var lighting = new ToonLightingProfile(Quaternion.Euler(keyEuler[0], keyEuler[1], keyEuler[2]), color,
                    intensity, shadowStrength, characterFill, contrast, bakedStrength, contactStrength, night, indoors);
                var surfaces = new List<BattleSurfaceOverride>();
                if (backgroundSurfaces != null)
                    foreach (BackgroundSurface surface in backgroundSurfaces)
                    {
                        if (surface == null)
                            throw new InvalidDataException("A background surface cannot be null.");
                        surfaces.Add(surface.Capture(PolSurfaceAppearance.Painted(surface.brightness)));
                    }
                if (geometricNormalSurfaces != null)
                    foreach (SurfaceReference surface in geometricNormalSurfaces)
                    {
                        if (surface == null)
                            throw new InvalidDataException("A geometric-normal surface cannot be null.");
                        surfaces.Add(surface.Capture(PolSurfaceAppearance.RecalculateNormals));
                    }
                return new BattleSceneAppearance(lighting, surfaces);
            }
        }

        private class SurfaceReference
        {
            public int nodeIndex = -1;
            public int materialGroupIndex = -1;
            public string nodeName = null;
            public string texture = null;

            public BattleSurfaceOverride Capture(PolSurfaceAppearance appearance) =>
                new(nodeIndex, materialGroupIndex, nodeName, texture, appearance);
        }

        private sealed class BackgroundSurface : SurfaceReference
        {
            public float brightness = 1;
        }

        private const string ProfileAssetPath = "Assets/Scripts/Editor/BattleEditor/Data/BattleToonLightingProfiles.json";

        private static string _text;
        private static Dictionary<string, BattleSceneAppearance> _profiles;

        public static ToonLightingProfile Resolve(string variant, string scene) => ResolveScene(variant, scene).Lighting;

        public static BattleSceneAppearance ResolveScene(string variant, string scene)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown battle lighting variant.", nameof(variant));
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProfileAssetPath);
            if (asset == null)
                throw new FileNotFoundException("Battle Editor lighting profiles could not be loaded.", ProfileAssetPath);
            string text = asset.text;
            if (_profiles == null || text != _text)
            {
                Dictionary<string, BattleSceneAppearance> candidate = Parse(text);
                _profiles = candidate;
                _text = text;
            }
            return _profiles.TryGetValue(variant + "/" + scene, out BattleSceneAppearance profile) ? profile : new BattleSceneAppearance(ToonLightingProfile.Default);
        }

        private static Dictionary<string, BattleSceneAppearance> Parse(string text)
        {
            var profiles = new Dictionary<string, BattleSceneAppearance>(StringComparer.OrdinalIgnoreCase);
            var document = JsonConvert.DeserializeObject<Document>(text, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, TypeNameHandling = TypeNameHandling.None });
            if (document == null || document.schemaVersion != 1 || document.profiles == null)
                throw new InvalidDataException("Unsupported or incomplete battle lighting document.");
            foreach (Entry entry in document.profiles)
            {
                if (entry == null || entry.variant is not ("PAL3" or "PAL3A") || string.IsNullOrWhiteSpace(entry.scene))
                    throw new InvalidDataException("A scene lighting entry requires a supported variant and scene ID.");
                if (!profiles.TryAdd(entry.variant + "/" + entry.scene, entry.Capture()))
                    throw new InvalidDataException("Duplicate battle lighting profile: " + entry.variant + "/" + entry.scene);
            }
            return profiles;
        }
    }
}
