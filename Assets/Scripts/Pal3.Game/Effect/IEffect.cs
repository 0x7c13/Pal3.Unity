﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Data;

    public interface IEffect : IDisposable
    {
        public void Init(GameResourceProvider resourceProvider, uint effectParameter);
    }
}