using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using static CASCToolHost.CASC;

namespace CASCToolHost.Utils
{
    public static class BuildCache
    {
        private static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 15 });
        private static ConcurrentDictionary<string, SemaphoreSlim> Locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public static Build GetOrCreate(string buildConfig, string cdnConfig = "")
        {
            Build cachedBuild;

            if (string.IsNullOrEmpty(cdnConfig))
            {
                cdnConfig = Database.GetCDNConfigByBuildConfig(buildConfig);
                if (string.IsNullOrEmpty(cdnConfig))
                {
                    throw new Exception("Unable to locate CDNconfig for buildconfig " + buildConfig);
                }
            }

            if (!Cache.TryGetValue(buildConfig, out cachedBuild))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd(buildConfig, k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!Cache.TryGetValue(buildConfig, out cachedBuild))
                    {
                        // Key not in cache, load build
                        cachedBuild = LoadBuild("wowt", buildConfig);
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

        public static int GetCount()
        {
            return Cache.Count;
        }
    }
}
