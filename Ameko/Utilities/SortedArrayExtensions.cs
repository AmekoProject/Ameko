// SPDX-License-Identifier: GPL-3.0-only

namespace Ameko.Utilities;

public static class SortedArrayExtensions
{
    /// <param name="arr">Array to search</param>
    extension(long[] arr)
    {
        /// <summary>
        /// Get the index of the first element >= <paramref name="x"/>
        /// </summary>
        /// <param name="x">Target</param>
        /// <returns>Index of the first element >= <paramref name="x"/></returns>
        public int LowerBound(long x)
        {
            var lo = 0;
            var hi = arr.Length;

            while (lo < hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (arr[mid] < x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        /// <summary>
        /// Get the index of the first element > <paramref name="x"/>
        /// </summary>
        /// <param name="x">Target</param>
        /// <returns>Index of the first element > <paramref name="x"/></returns>
        public int UpperBound(long x)
        {
            var lo = 0;
            var hi = arr.Length;

            while (lo < hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (arr[mid] <= x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }

    /// <param name="arr">Array to search</param>
    extension(int[] arr)
    {
        /// <summary>
        /// Get the index of the first element >= <paramref name="x"/>
        /// </summary>
        /// <param name="x">Target</param>
        /// <returns>Index of the first element >= <paramref name="x"/></returns>
        public int LowerBound(int x)
        {
            var lo = 0;
            var hi = arr.Length;

            while (lo < hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (arr[mid] < x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        /// <summary>
        /// Get the index of the first element > <paramref name="x"/>
        /// </summary>
        /// <param name="x">Target</param>
        /// <returns>Index of the first element > <paramref name="x"/></returns>
        public int UpperBound(int x)
        {
            var lo = 0;
            var hi = arr.Length;

            while (lo < hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (arr[mid] <= x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }
}
