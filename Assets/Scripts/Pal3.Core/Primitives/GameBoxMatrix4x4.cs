// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    [System.Serializable]
    public struct GameBoxMatrix4x4
    {
        public float Xx, Xy, Xz, Xw;
        public float Yx, Yy, Yz, Yw;
        public float Zx, Zy, Zz, Zw;
        public float Tx, Ty, Tz, Tw;
    }
}