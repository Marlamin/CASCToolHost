using System;
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
                Console.WriteLine("Error retrieving versions: " + e.Message);
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
                                Console.WriteLine("!!!!!!!! Unknown versions variable '" + friendlyName + "'");
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
                Console.WriteLine("Error retrieving CDNs file: " + e.Message);
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
                                //Console.WriteLine("!!!!!!!! Unknown cdns variable '" + friendlyName + "'");
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
                encoding.unk1 = bin.ReadByte();
                encoding.checksumSizeA = bin.ReadByte();
                encoding.checksumSizeB = bin.ReadByte();
                encoding.flagsA = bin.ReadUInt16();
                encoding.flagsB = bin.ReadUInt16();
                encoding.numEntriesA = bin.ReadUInt32(true);
                encoding.numEntriesB = bin.ReadUInt32(true);
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
                    encoding.aHeaders = new EncodingHeaderEntry[encoding.numEntriesA];

                    for (int i = 0; i < encoding.numEntriesA; i++)
                    {
                        encoding.aHeaders[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        encoding.aHeaders[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.numEntriesA * 32;
                }

                var tableAstart = bin.BaseStream.Position;

                List<EncodingFileEntry> entries = new List<EncodingFileEntry>();

                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    ushort keysCount;
                    while ((keysCount = bin.ReadUInt16()) != 0)
                    {
                        EncodingFileEntry entry = new EncodingFileEntry()
                        {
                            keyCount = keysCount,
                            size = bin.ReadUInt32(true),
                            hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", ""),
                            key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "")
                        };

                        // @TODO add support for multiple encoding keys
                        for (int key = 0; key < entry.keyCount - 1; key++)
                        {
                            bin.ReadBytes(16);
                        }

                        entries.Add(entry);
                    }

                    var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
                    if (remaining > 0) { bin.BaseStream.Position += remaining; }
                }

                encoding.aEntries = entries.ToArray();

                if (!parseTableB)
                {
                    return encoding;
                }

                /* Table B */
                if (checkStuff)
                {
                    encoding.bHeaders = new EncodingHeaderEntry[encoding.numEntriesB];

                    for (int i = 0; i < encoding.numEntriesB; i++)
                    {
                        encoding.bHeaders[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        encoding.bHeaders[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.numEntriesB * 32;
                }

                var tableBstart = bin.BaseStream.Position;

                List<EncodingFileDescEntry> b_entries = new List<EncodingFileDescEntry>();

                while (bin.BaseStream.Position < tableBstart + 4096 * encoding.numEntriesB)
                {
                    var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

                    if (remaining < 25)
                    {
                        bin.BaseStream.Position += remaining;
                        continue;
                    }

                    EncodingFileDescEntry entry = new EncodingFileDescEntry()
                    {
                        key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", ""),
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
        public static void GetIndexes(string url, string[] archives, Dictionary<string, Dictionary<string, IndexEntry>> indexDictionary)
        {
            Parallel.ForEach(archives, (archive, state, i) =>
            {

                try
                {
                    cacheLock.EnterUpgradeableReadLock();

                    if (!indexDictionary.ContainsKey(archives[i]))
                    {
                        try
                        {
                            cacheLock.EnterWriteLock();
                            indexDictionary.Add(archives[i], new Dictionary<string, IndexEntry>());
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }

                    }
                    else
                    {
                        Console.WriteLine("Index already exists!");
                        return;
                    }
                }
                finally
                {
                    cacheLock.ExitUpgradeableReadLock();
                }

                byte[] indexContent;
                if (url.StartsWith("http"))
                {
                    indexContent = CDN.Get(url + "data/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i] + ".index");
                }
                else
                {
                    indexContent = File.ReadAllBytes(Path.Combine(url, "data", "" + archives[i][0] + archives[i][1], "" + archives[i][2] + archives[i][3], archives[i] + ".index"));
                }

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                            var entry = new IndexEntry()
                            {
                                indexName = archives[i],
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };

                            cacheLock.EnterUpgradeableReadLock();
                            try
                            {
                                if (!indexDictionary[archives[i]].ContainsKey(headerHash))
                                {
                                    cacheLock.EnterWriteLock();
                                    try
                                    {
                                        indexDictionary[archives[i]].Add(headerHash, entry);
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
                        bin.ReadBytes(16);
                    }
                }
            });
        }
    }
}
