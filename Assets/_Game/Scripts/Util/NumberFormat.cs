using System.Collections.Generic;

namespace CrowdSurge
{
    public static class NumberFormat
    {
        private static readonly Dictionary<int, string> Cache = new Dictionary<int, string>(256);

        /// <summary>Allocation-free after first use of each value (labels refresh every frame).</summary>
        public static string Short(int value)
        {
            if (!Cache.TryGetValue(value, out string s))
            {
                s = value.ToString();
                if (Cache.Count < 2048) Cache[value] = s;
            }
            return s;
        }
    }
}
