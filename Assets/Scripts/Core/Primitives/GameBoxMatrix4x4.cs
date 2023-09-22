// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Primitives
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