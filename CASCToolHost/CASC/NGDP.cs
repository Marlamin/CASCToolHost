using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public class NGDP
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
        private static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        public static VersionsFile GetVersions(string program)
        {
            string content;
            var versions = new VersionsFile();

            try
            {
                using (HttpResponseMessage response = CDN.client.GetAsync(new Uri(baseUrl + program + "/" + "versions")).Result)
                {
                    using (HttpContent res = response.Content)
                    {
                        content = res.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error retrieving versions: " + e.Message);
                return versions;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Count() > 0)
            {
                versions.entries = new VersionsEntry[lines.Count() - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Count(); c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Count(); i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Region":
                                versions.entries[i - 1].region = row[c];
                                break;
                            case "BuildConfig":
                                versions.entries[i - 1].buildConfig = row[c];
                                break;
                            case "CDNConfig":
                                versions.entries[i - 1].cdnConfig = row[c];
                                break;
                            case "Keyring":
                            case "KeyRing":
                                versions.entries[i - 1].keyRing = row[c];
                                break;
                            case "BuildId":
                                versions.entries[i - 1].buildId = row[c];
                                break;
                            case "VersionName":
                            case "VersionsName":
                                versions.entries[i - 1].versionsName = row[c].Trim('\r');
                                break;
                            case "ProductConfig":
                                versions.entries[i - 1].productConfig = row[c];
                                break;
                            default:
                                Logger.WriteLine("!!!!!!!! Unknown versions variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }
            }

            return versions;
        }
        public static CdnsFile GetCDNs(string program)
        {
            string content;

            var cdns = new CdnsFile();

            try
            {
                using (HttpResponseMessage response = CDN.client.GetAsync(new Uri(baseUrl + program + "/" + "cdns")).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (HttpContent res = response.Content)
                        {
                            content = res.ReadAsStringAsync().Result;
                        }
                    }
                    else
                    {
                        throw new Exception("Bad HTTP code while retrieving");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error retrieving CDNs file: " + e.Message);
                return cdns;
            }

            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Count() > 0)
            {
                cdns.entries = new CdnsEntry[lines.Count() - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Count(); c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Count(); i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Name":
                                cdns.entries[i - 1].name = row[c];
                                break;
                            case "Path":
                                cdns.entries[i - 1].path = row[c];
                                break;
                            case "Hosts":
                                var hosts = row[c].Split(' ');
                                cdns.entries[i - 1].hosts = new string[hosts.Count()];
                                for (var h = 0; h < hosts.Count(); h++)
                                {
                                    cdns.entries[i - 1].hosts[h] = hosts[h];
                                }
                                break;
                            case "ConfigPath":
                                cdns.entries[i - 1].configPath = row[c];
                                break;
                            default:
                                //Logger.WriteLine("!!!!!!!! Unknown cdns variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }

                foreach (var cdn in cdns.entries)
                {
                    if (cdn.name == "eu")
                    {
                        //override cdn to always use eu if present
                        var over = new CdnsFile();
                        over.entries = new CdnsEntry[1];
                        over.entries[0] = cdn;
                        // Edgecast is having issues, override for now
                        over.entries[0].hosts = new string[] { "blzddist1-a.akamaihd.net", "level3.blizzard.com" };
                        return over;
                    }
                }
            }

            return cdns;
        }
        public static RootFile GetRoot(string url, string hash, bool parseIt = false)
        {
            var root = new RootFile
            {
                entriesLookup = new MultiDictionary<ulong, RootEntry>(),
                entriesFDID = new MultiDictionary<uint, RootEntry>(),
            };

            byte[] content;

            if (url.StartsWith("http:"))
            {
                content = CDN.Get(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
            }
            else
            {
                content = File.ReadAllBytes(Path.Combine(url, "data", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            if (!parseIt) return root;

            var hasher = new Jenkins96();

            var namedCount = 0;
            var unnamedCount = 0;
            uint totalFiles = 0;
            uint namedFiles = 0;
            var newRoot = false;

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                var header = bin.ReadUInt32();
                if (header == 1296454484)
                {
                    totalFiles = bin.ReadUInt32();
                    namedFiles = bin.ReadUInt32();
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
                                //entries[i].lookup = hasher.ComputeHash("BY_FDID_" + entries[i].fileDataID);
                                entries[i].lookup = 0;
                                unnamedCount++;
                            }
                            else
                            {
                                entries[i].lookup = bin.ReadUInt64();
                                namedCount++;

                                root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            }

                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                        }
                    }
                    
                }
            }

            return root;
        }
        public static EncodingFile GetEncoding(string url, string hash, int encodingSize = 0, bool parseTableB = false, bool checkStuff = false)
        {
            var encoding = new EncodingFile();

            byte[] content;

            if (url.Substring(0, 4) == "http")
            {
                content = CDN.Get(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

                if (encodingSize != 0 && encodingSize != content.Length)
                {
                    content = CDN.Get(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash, true);

                    if (encodingSize != content.Length && encodingSize != 0)
                    {
                        throw new Exception("File corrupt/not fully downloaded! Remove " + "data / " + hash[0] + hash[1] + " / " + hash[2] + hash[3] + " / " + hash + " from cache.");
                    }
                }
            }
            else
            {
                content = File.ReadAllBytes(Path.Combine(url, "data", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
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

                Dictionary<MD5Hash, EncodingFileEntry> entries = new Dictionary<MD5Hash, EncodingFileEntry>(new MD5HashComparer());

                for (int i = 0; i < encoding.cKeyPageCount; i++)
                {
                    byte keysCount;

                    while ((keysCount = bin.ReadByte()) != 0)
                    {
                        EncodingFileEntry entry = new EncodingFileEntry()
                        {
                            size = bin.ReadInt40BE()
                        };

                        var cKey = bin.Read<MD5Hash>();

                        // @TODO add support for multiple encoding keys
                        for (int key = 0; key < keysCount; key++)
                        {
                            if (key == 0)
                            {
                                entry.eKey = bin.Read<MD5Hash>();
                            }
                            else
                            {
                                bin.ReadBytes(16);
                            }
                        }
                        entries.Add(cKey, entry);
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

                List<EncodingFileDescEntry> b_entries = new List<EncodingFileDescEntry>();

                while (bin.BaseStream.Position < tableBstart + 4096 * encoding.eKeyPageCount)
                {
                    var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

                    if (remaining < 25)
                    {
                        bin.BaseStream.Position += remaining;
                        continue;
                    }

                    EncodingFileDescEntry entry = new EncodingFileDescEntry()
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
        public static void GetIndexes(string url, MD5Hash[] archives)
        {
            Parallel.ForEach(archives, (archive, state, i) =>
            {
                uint indexID;
                string indexName = archive.ToHexString().ToLower();

                if (!File.Exists(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName)))
                {
                    Console.WriteLine("Archive " + indexName + " not found, skipping index loading!");
                    return;
                }

                try
                {
                    cacheLock.EnterUpgradeableReadLock();

                    if (!CASC.indexNames.Contains(archives[i], new MD5HashComparer()))
                    {
                        try
                        {
                            cacheLock.EnterWriteLock();
                            CASC.indexNames.Add(archive);
                            indexID = (uint)CASC.indexNames.Count - 1;
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                finally
                {
                    cacheLock.ExitUpgradeableReadLock();
                }

                var archiveLength = new FileInfo(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName)).Length;

                var indexContent = File.ReadAllBytes(Path.Combine(url, "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName + ".index"));

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    bin.BaseStream.Position = bin.BaseStream.Length - 12;
                    var entryCount = bin.ReadUInt32();
                    bin.BaseStream.Position = 0;

                    int indexEntries = indexContent.Length / 4096;

                    var entriesRead = 0;
                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = bin.Read<MD5Hash>();

                            var entry = new IndexEntry()
                            {
                                indexID = indexID,
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };

                            entriesRead++;

                            if ((entry.offset + entry.size) > archiveLength)
                            {
                                Console.WriteLine("Read index entry at " + entry.offset + " of size " + entry.size + " that goes beyond size of archive " + indexName + " " + archiveLength + ", skipping..");
                            }
                            else
                            {
                                cacheLock.EnterUpgradeableReadLock();
                                try
                                {
                                    if (!CASC.indexDictionary.ContainsKey(headerHash))
                                    {
                                        cacheLock.EnterWriteLock();
                                        try
                                        {
                                            CASC.indexDictionary.Add(headerHash, entry);
                                        }
                                        finally
                                        {
                                            cacheLock.ExitWriteLock();
                                        }
                                    }
                                }
                                finally
                                {
                                    cacheLock.ExitUpgradeableReadLock();
                                }
                            }
                        }

                        if (entriesRead == entryCount)
                            return;

                        // 16 bytes padding that rounds the chunk to 4096 bytes (index entry is 24 bytes, 24 * 170 = 4080 bytes so 16 bytes remain)
                        bin.ReadBytes(16);
                    }
                }
            });
        }
        public static void LoadAllIndexes()
        {
            Logger.WriteLine("Filtering indexes..");
            var archiveList = new List<MD5Hash>();

            foreach (var file in Directory.GetFiles(Path.Combine(CDN.cacheDir, "tpr", "wow", "data"), "*.index", SearchOption.AllDirectories))
            {
                var indexName = Path.GetFileNameWithoutExtension(file);
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var bin = new BinaryReader(stream))
                {
                    bin.BaseStream.Position = bin.BaseStream.Length - 16;
                    // 0 = loose file index, 4 = archive index, 6 = group archive index
                    if (bin.ReadChar() == 4)
                    {
                        if (!File.Exists(Path.Combine(CDN.cacheDir, "tpr", "wow", "patch", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName + ".index")))
                        {
                            if (File.Exists(Path.Combine(CDN.cacheDir, "tpr", "wow", "data", "" + indexName[0] + indexName[1], "" + indexName[2] + indexName[3], indexName)))
                            {
                                archiveList.Add(Path.GetFileNameWithoutExtension(file).ToByteArray().ToMD5());
                            }
                            else
                            {
                                Logger.WriteLine("Skipping archive index without archive " + file);
                            }
                        }
                        else
                        {
                            Logger.WriteLine("Skipping patch index " + file);
                        }
                    }
                    else
                    {
                        Logger.WriteLine("Skipping non-archive index " + file);
                    }
                }
            }

            Logger.WriteLine("Loading " + archiveList.Count.ToString() + " archive indexes..");

            GetIndexes(Path.Combine(CDN.cacheDir, "tpr", "wow"), archiveList.ToArray());
        }

        public static InstallFile GetInstall(string url, string hash, bool parseIt = false)
        {
            var install = new InstallFile();

            byte[] content = CDN.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (!parseIt) return install;

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
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
