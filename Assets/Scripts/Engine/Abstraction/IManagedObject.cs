// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    public interface IManagedObject
    {
        public object NativeObject { get; }

        public bool IsNativeObjectDisposed { get; }
    }
}