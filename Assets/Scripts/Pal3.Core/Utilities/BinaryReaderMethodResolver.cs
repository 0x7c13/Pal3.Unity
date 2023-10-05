// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class BinaryReaderMethodResolver
    {
        private static readonly Dictionary<Type, MethodInfo> BinaryReaderMethodInfoCache = new ();

        public static MethodInfo GetMethodInfoForReadPropertyType(Type binaryReaderType, Type readPropertyType)
        {
            // Get method info from cache to reduce reflection cost.
            if (BinaryReaderMethodInfoCache.TryGetValue(readPropertyType, out MethodInfo method))
            {
                return method;
            }

            // Find the correct reader method for the property type based on:
            // 1. Return type of the method
            // 2. Name prefix of the method (Read...)
            // 3. No parameters
            foreach (MethodInfo methodInfo in binaryReaderType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (methodInfo.ReturnType == readPropertyType &&
                    methodInfo.Name.StartsWith("Read") &&
                    !methodInfo.Name.Equals("Read") && // Filter out the specific int Read() method
                    methodInfo.GetParameters().Length == 0) // The method should have no parameters
                {
                    BinaryReaderMethodInfoCache[readPropertyType] = methodInfo;
                    return methodInfo;
                }
            }

            throw new ArgumentException($"Property type: {readPropertyType.Name} not supported");
        }
    }
}