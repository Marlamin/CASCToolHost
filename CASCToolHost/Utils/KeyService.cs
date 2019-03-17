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
            if (!File.Exists("keys.txt"))
            {
                throw new Exception("keys.txt not found!");
            }

            keys.Clear();

            var rawkeys = File.ReadAllLines("keys.txt");
            foreach(var rawkey in rawkeys)
            {
                var keysplit = rawkey.Split("  ");
                if (keysplit[0] == "key_name") continue;

                var keyname = keysplit[0];
                var keybytes = keysplit[1];

                if(keyname.Length == 16&& !keybytes.Contains('?') && keybytes.Length == 32)
                {
                    keys.Add(Convert.ToUInt64(keyname, 16), keybytes.ToByteArray());
                }
            }
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