﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Checks method arguments for both dotnet objects and Unity objects.
    /// </summary>
    public static class Requires
    {
        public static T IsNotNull<T>([NotNull] T instance, string paramName)
            where T : class
        {
            if (typeof(T) == typeof(UnityEngine.Object) ||
                typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
            {
                if (instance == null)
                {
                    throw new ArgumentNullException(paramName);
                }
                else
                {
                    return instance;
                }
            }

            if (System.Object.ReferenceEquals(null, instance))
            {
                throw new ArgumentNullException(paramName);
            }

            return instance;
        }

        public static string IsNotNullOrEmpty([NotNull] string value, string paramName)
        {
            IsNotNull(value, paramName);

            if (value == string.Empty)
            {
                throw new ArgumentException(paramName);
            }

            return value;
        }

        public static ICollection<T> IsNotNullOrEmpty<T>([NotNull] ICollection<T> value, string paramName)
        {
            IsNotNull(value, paramName);

            if (!value.Any())
            {
                throw new ArgumentException("Collection is empty", paramName);
            }

            return value;
        }

        public static T[] IsNotNullOrEmpty<T>([NotNull] T[] instances, string paramName)
        {
            IsNotNull(instances, paramName);

            if (instances.Length == 0)
            {
                throw new ArgumentException("Array is empty", paramName);
            }

            return instances;
        }
    }
}