// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using MetaData;

    public static class EffectTypeResolver
    {
        public static Type GetEffectComponentType(GraphicsEffect effect)
        {
            return effect switch
            {
                GraphicsEffect.Fire   => typeof(FireEffect),
                GraphicsEffect.Portal => typeof(PortalEffect),
                _ => null
            };
        }

        public static GraphicsEffect GetEffectByName(string name)
        {
            if (!string.IsNullOrEmpty(name) && name.StartsWith('+'))
            {
                var hexName = '0' + name[^1..];
                var effect = int.Parse(hexName, System.Globalization.NumberStyles.HexNumber);
                return (GraphicsEffect)effect;
            }
            else
            {
                return GraphicsEffect.None;
            }
        }
    }
}