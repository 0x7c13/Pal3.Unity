// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using System.Globalization;
    using MetaData;

    public static class EffectTypeResolver
    {
        public static Type GetEffectComponentType(GraphicsEffect effect)
        {
            return effect switch
            {
                GraphicsEffect.Fire   => typeof(FireEffect),
                GraphicsEffect.Portal => typeof(PortalEffect),
                GraphicsEffect.Combat => typeof(CombatEffect),
                _ => null
            };
        }

        public static GraphicsEffect GetEffectByNameAndType(string name, uint effectModelType)
        {
            #if PAL3
            if (name == "+") return GraphicsEffect.None;

            if (!string.IsNullOrEmpty(name) && name.StartsWith('+'))
            {
                var hexName = '0' + name[^1..];
                var effect = int.Parse(hexName, NumberStyles.HexNumber);
                return (GraphicsEffect)effect;
            }
            else
            {
                return GraphicsEffect.None;
            }
            #elif PAL3A
            if (name != "+") return GraphicsEffect.None;
            else return (GraphicsEffect)effectModelType;
            #endif
        }
    }
}