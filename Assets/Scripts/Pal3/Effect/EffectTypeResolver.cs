// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using System.Globalization;
    using Core.Contract.Enums;

    public static class EffectTypeResolver
    {
        public static Type GetEffectComponentType(GraphicsEffectType effect)
        {
            return effect switch
            {
                GraphicsEffectType.Fire   => typeof(FireEffect),
                GraphicsEffectType.Portal => typeof(PortalEffect),
                GraphicsEffectType.Combat => typeof(CombatEffect),
                _ => null
            };
        }

        public static GraphicsEffectType GetEffectByNameAndType(string name, uint effectModelType)
        {
            #if PAL3
            if (name == "+") return GraphicsEffectType.None;

            if (!string.IsNullOrEmpty(name) && name.StartsWith('+'))
            {
                var hexName = '0' + name[^1..];
                var effect = int.Parse(hexName, NumberStyles.HexNumber);
                return (GraphicsEffectType)effect;
            }
            else
            {
                return GraphicsEffectType.None;
            }
            #elif PAL3A
            if (name != "+") return GraphicsEffectType.None;
            else return (GraphicsEffectType)effectModelType;
            #endif
        }
    }
}