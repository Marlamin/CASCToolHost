using CASCToolHost.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CASCToolHost
{
    class KeyService
    {
        private static Dictionary<ulong, byte[]> keys = new();

        public static async Task<Dictionary<ulong, byte[]>> LoadKeys()
        {
            keys = await Database.GetKnownTACTKeys();
            return keys;
        }

        private static readonly Salsa20 salsa = new();

        public static Salsa20 SalsaInstance => salsa;

        public static byte[] GetKey(ulong keyName)
        {
            keys.TryGetValue(keyName, out byte[] key);
            return key;
        }
    }
}