// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    using System;

    public static class RandomGenerator
    {
        private static readonly Random Random = new(Guid.NewGuid().GetHashCode());

        public static float Range(float minInclusive, float maxInclusive)
        {
            return (float)(Random.NextDouble() * (maxInclusive - minInclusive) + minInclusive);
        }

        public static int Range(int minInclusive, int maxExclusive)
        {
            return Random.Next(minInclusive, maxExclusive);
        }

        public static (float x, float y, float z) RandomPointInsideUnitSphere()
        {
            float theta = (float)(2 * Math.PI * Random.NextDouble());
            float phi = (float)Math.Acos(2 * Random.NextDouble() - 1);
            float r = (float)Math.Pow(Random.NextDouble(), 1.0 / 3.0);

            float x = r * (float)(Math.Sin(phi) * Math.Cos(theta));
            float y = r * (float)(Math.Sin(phi) * Math.Sin(theta));
            float z = r * (float)Math.Cos(phi);

            return (x, y, z);
        }
    }
}