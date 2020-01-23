using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static CASCToolHost.CASC;

namespace CASCToolHost.Utils
{
    public static class BuildCache
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 15 });
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public static async Task<Build> GetOrCreate(string buildConfig, string cdnConfig = "")
        {
            if (string.IsNullOrEmpty(cdnConfig))
            {
                cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
                if (string.IsNullOrEmpty(cdnConfig))
                {
                    throw new Exception("Unable to locate CDNconfig for buildconfig " + buildConfig);
                }
            }
            
            if (!Cache.TryGetValue(buildConfig, out Build cachedBuild))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd(buildConfig, k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!Cache.TryGetValue(buildConfig, out cachedBuild))
                    {
                        // Key not in cache, load build
                        cachedBuild = await LoadBuild("wowt", buildConfig, cdnConfig);
                        Cache.Set(buildConfig, cachedBuild, new MemoryCacheEntryOptions().SetSize(1));
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }

            return cachedBuild;
        }

        public static int Count()
        {
            return Cache.Count;
        }

        public static bool Exists(string buildConfig)
        {
            return Cache.TryGetValue(buildConfig, out _);
        }

        public static void Remove(object key)
        {
            Cache.Remove(key);
        }
    }
}
