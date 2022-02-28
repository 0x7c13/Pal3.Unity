// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using Data;

    public interface IEffect
    {
        public void Init(GameResourceProvider resourceProvider, uint effectModelType);
    }
}