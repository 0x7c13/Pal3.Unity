// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Interface for managed objects that wrap native objects.
    /// </summary>
    public interface IManagedObject
    {
        /// <summary>
        /// Gets the underlying native object.
        /// </summary>
        public object NativeObject { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying native object has been disposed.
        /// </summary>
        public bool IsNativeObjectDisposed { get; }

        /// <summary>
        /// Destroys the native object associated with this managed object.
        /// </summary>
        public void Destroy();

        /// <summary>
        /// Determines whether the specified managed object is equal to the current managed object.
        /// </summary>
        public bool Equals(IManagedObject other)
        {
            if (other == null) return false;
            else return NativeObject == other.NativeObject;
        }
    }
}