// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    using System;

    /// <summary>Released light sorting also determines which equally strong lights survive.</summary>
    public static class GameBoxLightOrder
    {
        public static void Sort(Span<int> order, ReadOnlySpan<float> weights)
        {
            if (order.Length != weights.Length)
                throw new ArgumentException("Light ordering requires one weight per index.");
            for (int index = 0; index < order.Length; index++)
                order[index] = index;
            SortRange(order, weights, 0, order.Length - 1);
        }

        private static int Compare(float a, float b) => a > b ? -1 : a < b ? 1 : 0;

        private static void SortRange(Span<int> order, ReadOnlySpan<float> weights, int low, int high)
        {
            if (high <= low)
                return;
            if (high - low < 8)
            {
                for (int end = high; end > low; end--)
                {
                    int weakest = low;
                    for (int index = low + 1; index <= end; index++)
                        if (Compare(weights[order[index]], weights[order[weakest]]) > 0)
                            weakest = index;
                    Swap(order, weakest, end);
                }
                return;
            }
            Swap(order, low, low + (high - low + 1) / 2);
            float pivot = weights[order[low]];
            int left = low;
            int right = high + 1;
            while (true)
            {
                do
                {
                    left++;
                }
                while (left <= high && Compare(weights[order[left]], pivot) <= 0);
                do
                {
                    right--;
                }
                while (right > low && Compare(weights[order[right]], pivot) >= 0);
                if (right < left)
                    break;
                Swap(order, left, right);
            }
            Swap(order, low, right);
            SortRange(order, weights, low, right - 1);
            SortRange(order, weights, left, high);
        }

        private static void Swap(Span<int> order, int a, int b)
        {
            int value = order[a];
            order[a] = order[b];
            order[b] = value;
        }
    }
}
