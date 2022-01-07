using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public static class NGDP
    {
        private static readonly ReaderWriterLockSlim indexCacheLock = new();
        private static readonly ReaderWriterLockSlim encodingCacheLock = new();

        public static Dictionary<MD5Hash, List<MD5Hash>> encodingDictionary = new(new MD5HashComparer());

        public static async Task<RootFile> GetRoot(string hash, bool parseIt = false)
        {
            var root = new RootFile
            {
                entriesLookup = new MultiDictionary<ulong, RootEntry>(),
                entriesFDID = new MultiDictionary<uint, RootEntry>(),
            };

            byte[] content = await CDNCache.Get("data", hash);
            if (!parseIt) return root;

            var newRoot = false;

            using (var ms = new MemoryStream(BLTE.Parse(content)))
            using (var bin = new BinaryReader(ms))
            {
                var header = bin.ReadUInt32();
                if (header == 1296454484)
                {
                    uint totalFiles = bin.ReadUInt32();
                    uint namedFiles = bin.ReadUInt32();
                    newRoot = true;
                }
                else
                {
                    bin.BaseStream.Position = 0;
                }

                while (bin.BaseStream.Position < bin.BaseStream.Length)
                {
                    var count = bin.ReadUInt32();
                    var contentFlags = (ContentFlags)bin.ReadUInt32();
                    var localeFlags = (LocaleFlags)bin.ReadUInt32();

                    var entries = new RootEntry[count];
                    var filedataIds = new int[count];

                    var fileDataIndex = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        entries[i].localeFlags = localeFlags;
                        entries[i].contentFlags = contentFlags;

                        filedataIds[i] = fileDataIndex + bin.ReadInt32();
                        entries[i].fileDataID = (uint)filedataIds[i];
                        fileDataIndex = filedataIds[i] + 1;
                    }

                    if (!newRoot)
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = bin.Read<MD5Hash>();
                            entries[i].lookup = bin.ReadUInt64();
                            root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = bin.Read<MD5Hash>();
                        }

                        for (var i = 0; i < count; ++i)
                        {
                            if (contentFlags.HasFlag(ContentFlags.NoNames))
                            {
                                entries[i].lookup = 0;
                            }
                            else
                            {
                                entries[i].lookup = bin.ReadUInt64();
                                root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            }

                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                        }
                    }
                }
            }

            return root;
        }
        public static async Task<EncodingFile> GetEncoding(string hash, int encodingSize = 0, bool parseTableB = false, bool checkStuff = false)
        {
            var encoding = new EncodingFile();
            hash = hash.ToLower();
            byte[] content = await CDNCache.Get("data", hash);
            if (encodingSize != 0 && encodingSize != content.Length)
            {
                // Re-download file, not expected size.
                content = await CDNCache.Get("data", hash, true, true);

                if (encodingSize != content.Length && encodingSize != 0)
                {
                    throw new Exception("File corrupt/not fully downloaded! Remove " + "data / " + hash[0] + hash[1] + " / " + hash[2] + hash[3] + " / " + hash + " from cache.");
                }
            }

            using (var bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN") { throw new Exception("Error while parsing encoding file. Did BLTE header size change?"); }
                encoding.version = bin.ReadByte();
                encoding.cKeyLength = bin.ReadByte();
                encoding.eKeyLength = bin.ReadByte();
                encoding.cKeyPageSize = bin.ReadUInt16(true);
                encoding.eKeyPageSize = bin.ReadUInt16(true);
                encoding.cKeyPageCount = bin.ReadUInt32(true);
                encoding.eKeyPageCount = bin.ReadUInt32(true);
                encoding.stringBlockSize = bin.ReadUInt40(true);

                var headerLength = bin.BaseStream.Position;

                if (parseTableB)
                {
                    var stringBlockEntries = new List<string>();

                    while ((bin.BaseStream.Position - headerLength) != (long)encoding.stringBlockSize)
                    {
                        stringBlockEntries.Add(bin.ReadCString());
                    }

                    encoding.stringBlockEntries = stringBlockEntries.ToArray();
                }
                else
                {
                    bin.BaseStream.Position += (long)encoding.stringBlockSize;
                }

                /* Table A */
                if (checkStuff)
                {
                    encoding.aHeaders = new EncodingHeaderEntry[encoding.cKeyPageCount];

                    for (int i = 0; i < encoding.cKeyPageCount; i++)
                    {
                        encoding.aHeaders[i].firstHash = bin.Read<MD5Hash>();
                        encoding.aHeaders[i].checksum = bin.Read<MD5Hash>();
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.cKeyPageCount * 32;
                }

                var tableAstart = bin.BaseStream.Position;

                var entries = new Dictionary<MD5Hash, EncodingFileEntry>(new MD5HashComparer());

                for (int i = 0; i < encoding.cKeyPageCount; i++)
                {
                    byte keysCount;

                    while ((keysCount = bin.ReadByte()) != 0)
                    {
                        var entry = new EncodingFileEntry()
                        {
                            size = bin.ReadInt40BE(),
                            eKeys = new List<MD5Hash>()
                        };

                        var cKey = bin.Read<MD5Hash>();

                        for (int key = 0; key < keysCount; key++)
                        {
                            entry.eKeys.Add(bin.Read<MD5Hash>());
                        }

                        entries.Add(cKey, entry);

                        try
                        {
                            encodingCacheLock.EnterUpgradeableReadLock();

                            if (!encodingDictionary.ContainsKey(cKey))
                            {
                                try
                                {
                                    encodingCacheLock.EnterWriteLock();
                                    encodingDictionary.Add(cKey, entry.eKeys);
                                }
                                finally
                                {
                                    encodingCacheLock.ExitWriteLock();
                                }
                            }
                        }
                        finally
                        {
                            encodingCacheLock.ExitUpgradeableReadLock();
                        }
                    }

                    var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
                    if (remaining > 0) { bin.BaseStream.Position += remaining; }
                }

                encoding.aEntries = entries;

                if (!parseTableB)
                {
                    return encoding;
                }

                /* Table B */
                if (checkStuff)
                {
                    encoding.bHeaders = new EncodingHeaderEntry[encoding.eKeyPageCount];

                    for (int i = 0; i < encoding.eKeyPageCount; i++)
                    {
                        encoding.bHeaders[i].firstHash = bin.Read<MD5Hash>();
                        encoding.bHeaders[i].checksum = bin.Read<MD5Hash>();
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.eKeyPageCount * 32;
                }

                var tableBstart = bin.BaseStream.Position;

                var b_entries = new List<EncodingFileDescEntry>();

                while (bin.BaseStream.Position < tableBstart + 4096 * encoding.eKeyPageCount)
                {
                    var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

                    if (remaining < 25)
                    {
                        bin.BaseStream.Position += remaining;
                        continue;
                    }

                    var entry = new EncodingFileDescEntry()
                    {
                        key = bin.Read<MD5Hash>(),
                        stringIndex = bin.ReadUInt32(true),
                        compressedSize = bin.ReadUInt40(true)
                    };

                    if (entry.stringIndex == uint.MaxValue) break;

                    b_entries.Add(entry);
                }

                encoding.bEntries = b_entries.ToArray();
            }

            return encoding;
        }
        public static async Task<int> GetIndexes(string url, MD5Hash[] archives)
        {
            var tasks = archives.Select(async archive =>
           {
               uint indexID;
               string indexName = archive.ToHexString().ToLower();

               try
               {
                   indexCacheLock.EnterUpgradeableReadLock();

                   if (!CASC.indexNames.Contains(archive, new MD5HashComparer()))
                   {
                       try
                       {
                           indexCacheLock.EnterWriteLock();
                           CASC.indexNames.Add(archive);
                           indexID = (uint)CASC.indexNames.Count - 1;
                       }
                       finally
                       {
                           indexCacheLock.ExitWriteLock();
                       }
                   }
                   else
                   {
                       return;
                   }
               }
               finally
               {
                   indexCacheLock.ExitUpgradeableReadLock();
               }

               long archiveLength = 0;
               if (File.Exists(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName)))
               {
                   archiveLength = new FileInfo(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName)).Length;
               }
               else
               {
                   Console.WriteLine("WARNING! Archive " + indexName + " not found, skipping bound checks!");
               }

               using var indexContent = new MemoryStream(await CDNCache.Get("data", indexName + ".index"));
               using var bin = new BinaryReader(indexContent);
               bin.BaseStream.Position = bin.BaseStream.Length - 12;
               var entryCount = bin.ReadUInt32();
               bin.BaseStream.Position = 0;

               var indexEntries = indexContent.Length / 4096;

               var entriesRead = 0;
               for (var b = 0; b < indexEntries; b++)
               {
                   for (var bi = 0; bi < 170; bi++)
                   {
                       var headerHash = bin.Read<MD5Hash>();

                       var entry = new IndexEntry()
                       {
                           IndexID = indexID,
                           Size = bin.ReadUInt32(true),
                           Offset = bin.ReadUInt32(true)
                       };

                       entriesRead++;

                       if (archiveLength > 0 && (entry.Offset + entry.Size) > archiveLength)
                       {
                           Console.WriteLine("Read index entry at " + bin.BaseStream.Position + "index entry of archive offset " + entry.Offset + ", size " + entry.Size + " that goes beyond size of archive " + indexName + " " + archiveLength + ", skipping..");
                       }
                       else
                       {
                           indexCacheLock.EnterUpgradeableReadLock();
                           try
                           {
                               if (!CASC.indexDictionary.ContainsKey(headerHash))
                               {
                                   indexCacheLock.EnterWriteLock();
                                   try
                                   {
                                       CASC.indexDictionary.Add(headerHash, entry);
                                   }
                                   finally
                                   {
                                       indexCacheLock.ExitWriteLock();
                                   }
                               }
                               else
                               {
                                   var currentKey = CASC.indexDictionary[headerHash];
                                   var currentIndex = CASC.indexNames[(int)currentKey.IndexID].ToHexString().ToLower();
                                   var currentIndexTimestamp = new FileInfo(Path.Combine(url, "data", "" + currentIndex[0] + currentIndex[1], "" + currentIndex[2] + currentIndex[3], currentIndex + ".index")).CreationTime;
                                   var newIndexTimestamp = new FileInfo(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName + ".index")).CreationTime;
                                   if(newIndexTimestamp > currentIndexTimestamp)
                                   {
                                       Console.WriteLine("Duplicate index key for " + headerHash.ToHexString().ToLower() + ", using new entry since index " + indexName + " is newer than " + currentIndex);
                                       indexCacheLock.EnterWriteLock();
                                       try
                                       {
                                           CASC.indexDictionary.Remove(headerHash);
                                           CASC.indexDictionary.Add(headerHash, entry);
                                       }
                                       finally
                                       {
                                           indexCacheLock.ExitWriteLock();
                                       }
                                   }
                               }
                           }
                           finally
                           {
                               indexCacheLock.ExitUpgradeableReadLock();
                           }
                       }

                       if (entriesRead == entryCount)
                           return;
                   }

                   // 16 bytes padding that rounds the chunk to 4096 bytes (index entry is 24 bytes, 24 * 170 = 4080 bytes so 16 bytes remain)
                   bin.ReadBytes(16);
               }
           });

            await Task.WhenAll(tasks);

            return archives.Length;
        }

        public static async Task<InstallFile> GetInstall(string hash, bool parseIt = false)
        {
            var install = new InstallFile();

            byte[] content = await CDNCache.Get("data", hash);

            if (!parseIt) return install;

            using (var bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN") { throw new Exception("Error while parsing install file. Did BLTE header size change?"); }

                bin.ReadByte();

                install.hashSize = bin.ReadByte();
                if (install.hashSize != 16) throw new Exception("Unsupported install hash size!");

                install.numTags = bin.ReadUInt16(true);
                install.numEntries = bin.ReadUInt32(true);

                int bytesPerTag = ((int)install.numEntries + 7) / 8;

                install.tags = new InstallTagEntry[install.numTags];

                for (var i = 0; i < install.numTags; i++)
                {
                    install.tags[i].name = bin.ReadCString();
                    install.tags[i].type = bin.ReadUInt16(true);

                    var filebits = bin.ReadBytes(bytesPerTag);

                    for (int j = 0; j < bytesPerTag; j++)
                        filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

                    install.tags[i].files = new BitArray(filebits);
                }

                install.entries = new InstallFileEntry[install.numEntries];

                for (var i = 0; i < install.numEntries; i++)
                {
                    install.entries[i].name = bin.ReadCString();
                    install.entries[i].contentHash = BitConverter.ToString(bin.ReadBytes(install.hashSize)).Replace("-", "").ToLower();
                    install.entries[i].size = bin.ReadUInt32(true);
                    install.entries[i].tags = new List<string>();
                    for (var j = 0; j < install.numTags; j++)
                    {
                        if (install.tags[j].files[i] == true)
                        {
                            install.entries[i].tags.Add(install.tags[j].type + "=" + install.tags[j].name);
                        }
                    }
                }
            }

            return install;
        }
    }
}
