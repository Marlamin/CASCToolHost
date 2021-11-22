using System;
using System.Collections;
using System.Collections.Generic;

namespace CASCToolHost
{
    public unsafe struct MD5Hash
    {
        public fixed byte Value[16];
    }

    public struct BuildConfigFile
    {
        public MD5Hash root;
        public MD5Hash root_cdn;
        public MD5Hash[] install;
        public MD5Hash[] encoding;
        public string[] encodingSize;
    }

    public struct CDNConfigFile
    {
        public MD5Hash[] archives;
    }

    public record IndexEntry
    {
        public uint IndexID { get; init; }
        public uint Offset { get; init; }
        public uint Size { get; init; }
    }

    public struct EncodingFile
    {
        public byte version;
        public byte cKeyLength;
        public byte eKeyLength;
        public ushort cKeyPageSize;
        public ushort eKeyPageSize;
        public uint cKeyPageCount;
        public uint eKeyPageCount;
        public byte unk;
        public ulong stringBlockSize;
        public string[] stringBlockEntries;
        public EncodingHeaderEntry[] aHeaders;
        public Dictionary<MD5Hash, EncodingFileEntry> aEntries;
        public EncodingHeaderEntry[] bHeaders;
        public EncodingFileDescEntry[] bEntries;
    }

    public struct EncodingHeaderEntry
    {
        public MD5Hash firstHash;
        public MD5Hash checksum;
    }

    public struct EncodingFileEntry
    {
        public long size;
        public List<MD5Hash> eKeys;
    }

    public struct EncodingFileDescEntry
    {
        public MD5Hash key;
        public uint stringIndex;
        public ulong compressedSize;
    }

    public struct InstallFile
    {
        public byte hashSize;
        public ushort numTags;
        public uint numEntries;
        public InstallTagEntry[] tags;
        public InstallFileEntry[] entries;
    }

    public struct InstallTagEntry
    {
        public string name;
        public ushort type;
        public BitArray files;
    }

    public struct InstallFileEntry
    {
        public string name;
        public string contentHash;
        public uint size;
        public List<string> tags;
    }

    public struct BLTEChunkInfo
    {
        public bool isFullChunk;
        public int compSize;
        public int decompSize;
        public byte[] checkSum;
    }

    public struct RootFile
    {
        public MultiDictionary<ulong, RootEntry> entriesLookup;
        public MultiDictionary<uint, RootEntry> entriesFDID;
    }

    public struct RootEntry
    {
        public ContentFlags contentFlags;
        public LocaleFlags localeFlags;
        public ulong lookup;
        public uint fileDataID;
        public MD5Hash md5;
    }

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        //Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        //Unk_8 = 0x8,
        frFR = 0x10,
        deDE = 0x20,
        zhCN = 0x40,
        esES = 0x80,
        zhTW = 0x100,
        enGB = 0x200,
        enCN = 0x400,
        enTW = 0x800,
        esMX = 0x1000,
        ruRU = 0x2000,
        ptBR = 0x4000,
        itIT = 0x8000,
        ptPT = 0x10000,
        enSG = 0x20000000, // custom
        plPL = 0x40000000, // custom
        All_WoW = enUS | koKR | frFR | deDE | zhCN | esES | zhTW | enGB | esMX | ruRU | ptBR | itIT | ptPT
    }

    [Flags]
    public enum ContentFlags : uint
    {
        None = 0,
        F00000001 = 0x1,            // unused in 9.0.5
        F00000002 = 0x2,            // unused in 9.0.5
        F00000004 = 0x4,            // unused in 9.0.5
        LoadOnWindows = 0x8,        // added in 7.2.0.23436
        LoadOnMacOS = 0x10,         // added in 7.2.0.23436
        LowViolence = 0x80,         // many models have this flag
        DoNotLoad = 0x100,          // unused in 9.0.5
        F00000200 = 0x200,          // unused in 9.0.5
        F00000400 = 0x400,          // unused in 9.0.5
        UpdatePlugin = 0x800,       // UpdatePlugin.dll / UpdatePlugin.dylib only
        F00001000 = 0x1000,         // unused in 9.0.5
        F00002000 = 0x2000,         // unused in 9.0.5
        F00004000 = 0x4000,         // unused in 9.0.5
        F00008000 = 0x8000,         // unused in 9.0.5
        F00010000 = 0x10000,        // unused in 9.0.5
        F00020000 = 0x20000,        // 1173911 uses in 9.0.5        
        F00040000 = 0x40000,        // 1329023 uses in 9.0.5
        F00080000 = 0x80000,        // 682817 uses in 9.0.5
        F00100000 = 0x100000,       // 1231299 uses in 9.0.5
        F00200000 = 0x200000,       // 7398 uses in 9.0.5: updateplugin, .bls, .lua, .toc, .xsd
        F00400000 = 0x400000,       // 156302 uses in 9.0.5
        F00800000 = 0x800000,       // .skel & .wwf
        F01000000 = 0x1000000,      // unused in 9.0.5
        F02000000 = 0x2000000,      // 969369 uses in 9.0.5
        F04000000 = 0x4000000,      // 1101698 uses in 9.0.5
        Encrypted = 0x8000000,      // File is encrypted
        NoNames = 0x10000000,       // No lookup hash
        UncommonRes = 0x20000000,   // added in 7.0.3.21737
        Bundle = 0x40000000,        // unused in 9.0.5
        NoCompression = 0x80000000  // sounds have this flag
    }
}
