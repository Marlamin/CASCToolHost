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

    public struct IndexEntry
    {
        public uint indexID;
        public uint offset;
        public uint size;
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
        public MD5Hash eKey;
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
        F00000001 = 0x1,
        F00000002 = 0x2,
        F00000004 = 0x4,
        F00000008 = 0x8, // added in 7.2.0.23436
        F00000010 = 0x10, // added in 7.2.0.23436
        LowViolence = 0x80, // many models have this flag
        Encrypted = 0x8000000,
        NoNames = 0x10000000,
        F20000000 = 0x20000000, // added in 21737
        Bundle = 0x40000000,
        NoCompression = 0x80000000 // sounds have this flag
    }
}
