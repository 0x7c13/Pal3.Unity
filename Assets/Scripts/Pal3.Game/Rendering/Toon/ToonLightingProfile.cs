// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Toon
{
    using System;
    using UnityEngine;

    /// <summary>A captured view-local light and camera appearance, independent of saved preferences.</summary>
    public readonly struct ToonLightingProfile
    {
        public Quaternion Rotation
        {
            get;
        }

        public Color LightColor
        {
            get;
        }

        public float Intensity
        {
            get;
        }

        public float ShadowStrength
        {
            get;
        }

        public float CharacterFill
        {
            get;
        }

        public float Contrast
        {
            get;
        }

        public float BakedStrength
        {
            get;
        }

        public float ContactStrength
        {
            get;
        }

        public bool Night
        {
            get;
        }

        public bool Indoors
        {
            get;
        }

        public static ToonLightingProfile Default => new(Quaternion.Euler(58, -30, 0), new Color(1, .94f, .86f),
            1, .95f, .9f, 1, .65f, 1.25f, false, false);

        public ToonLightingProfile(Quaternion rotation, Color lightColor, float intensity, float shadowStrength,
            float characterFill, float contrast, float bakedStrength, float contactStrength, bool night, bool indoors)
        {
            Rotation = rotation;
            LightColor = lightColor;
            Intensity = intensity;
            ShadowStrength = shadowStrength;
            CharacterFill = characterFill;
            Contrast = contrast;
            BakedStrength = bakedStrength;
            ContactStrength = contactStrength;
            Night = night;
            Indoors = indoors;
            Validate();
        }

        public void Validate()
        {
            Range(Intensity, 0, 4, nameof(Intensity));
            Range(ShadowStrength, 0, 1, nameof(ShadowStrength));
            Range(CharacterFill, 0, 1, nameof(CharacterFill));
            Range(Contrast, 0, 1, nameof(Contrast));
            Range(BakedStrength, 0, 1, nameof(BakedStrength));
            Range(ContactStrength, 0, 2, nameof(ContactStrength));
            Range(LightColor.r, 0, 1, nameof(LightColor));
            Range(LightColor.g, 0, 1, nameof(LightColor));
            Range(LightColor.b, 0, 1, nameof(LightColor));
            Range(LightColor.a, 0, 1, nameof(LightColor));
            float norm = Rotation.x * Rotation.x + Rotation.y * Rotation.y + Rotation.z * Rotation.z + Rotation.w * Rotation.w;
            if (!float.IsFinite(norm) || Math.Abs(norm - 1) > .001f)
                throw new ArgumentException("The lighting rotation must be a finite unit quaternion.", nameof(Rotation));
        }

        private static void Range(float value, float minimum, float maximum, string name)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, "The lighting value is outside its supported range.");
        }
    }
}
