using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static CASCToolHost.CASC;

namespace CASCToolHost.Utils
{
    public static class RootCache
    {
        private static readonly MemoryCache Cache = new(new MemoryCacheOptions() { SizeLimit = 10 });
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        public static async Task<RootFile> GetOrCreate(string buildConfig, string cdnConfig = "")
        {
            if (string.IsNullOrEmpty(cdnConfig))
            {
                cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
                if (string.IsNullOrEmpty(cdnConfig))
                {
                    throw new Exception("Unable to locate CDNconfig for buildconfig " + buildConfig);
                }
            }

            if (!Cache.TryGetValue(buildConfig, out RootFile cachedRoot))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd(buildConfig, k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!Cache.TryGetValue(buildConfig, out cachedRoot))
                    {
                        // Key not in cache, load build
                        cachedRoot = await LoadRoot(buildConfig, cdnConfig);
                        Cache.Set(buildConfig, cachedRoot, new MemoryCacheEntryOptions().SetSize(1));
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }

            return cachedRoot;
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
