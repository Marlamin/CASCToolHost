using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CASCToolHost.Utils
{
    public static class BuildDiffCache
    {
        private static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 15 });

        private static HashSet<string> Keys = new HashSet<string>();

        public static bool Get(string from, string to, out ApiDiff diff)
        {
            var cacheKey = $"{from}_{to}";

            return Cache.TryGetValue(cacheKey, out diff);
        }

        public static void Add(string from, string to, ApiDiff diff)
        {
            var cacheKey = $"{from}_{to}";

            Keys.Add(cacheKey);

            Cache.Set(cacheKey, diff, new MemoryCacheEntryOptions().SetSize(1));
        }

        public static void Invalidate()
        {
            foreach (var key in Keys)
            {
                Cache.Remove(key);
            }

            Keys = new HashSet<string>();
        }
    }
}