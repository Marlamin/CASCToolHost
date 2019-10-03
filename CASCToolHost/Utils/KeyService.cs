using CASCToolHost.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace CASCToolHost
{
    class KeyService
    {
        private static Dictionary<ulong, byte[]> keys = new Dictionary<ulong, byte[]>();

        public static void LoadKeys()
        {
            keys = Database.GetKnownTACTKeys();
        }

        private static Salsa20 salsa = new Salsa20();

        public static Salsa20 SalsaInstance => salsa;

        public static byte[] GetKey(ulong keyName)
        {
            keys.TryGetValue(keyName, out byte[] key);
            return key;
        }
    }
}