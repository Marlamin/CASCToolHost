using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CASCToolHost
{
    public class Config
    {
        public static CDNConfigFile GetCDNConfig(string url, string hash)
        {
            string content;
            var cdnConfig = new CDNConfigFile();

            if (url.StartsWith("http"))
            {
                try
                {
                    content = Encoding.UTF8.GetString(CDN.Get(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error retrieving CDN config: " + e.Message);
                    return cdnConfig;
                }
            }
            else
            {
                content = File.ReadAllText(Path.Combine(url, "config", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }


            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Count(); i++)
            {
                if (cdnConfigLines[i].StartsWith("#") || cdnConfigLines[i].Length == 0) { continue; }
                var cols = cdnConfigLines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "archives":
                        var archives = cols[1].Split(' ');
                        cdnConfig.archives = new MD5Hash[archives.Length];
                        for (var j = 0; j < archives.Length; j++)
                        {
                            cdnConfig.archives[j] = archives[j].ToByteArray().ToMD5();
                        }
                        break;
                    case "archive-group":
                        cdnConfig.archiveGroup = cols[1];
                        break;
                    case "patch-archives":
                        if (cols.Length > 1)
                        {
                            var patchArchives = cols[1].Split(' ');
                            cdnConfig.patchArchives = new MD5Hash[patchArchives.Length];
                            for (var j = 0; j < patchArchives.Length; j++)
                            {
                                cdnConfig.patchArchives[j] = patchArchives[j].ToByteArray().ToMD5();
                            }
                        }
                        break;
                    case "patch-archive-group":
                        cdnConfig.patchArchiveGroup = cols[1];
                        break;
                    case "builds":
                        var builds = cols[1].Split(' ');
                        cdnConfig.builds = builds;
                        break;
                    case "file-index":
                        cdnConfig.fileIndex = cols[1];
                        break;
                    case "file-index-size":
                        cdnConfig.fileIndexSize = cols[1];
                        break;
                    case "patch-file-index":
                        cdnConfig.patchFileIndex = cols[1];
                        break;
                    case "patch-file-index-size":
                        cdnConfig.patchFileIndexSize = cols[1];
                        break;
                    case "archives-index-size":
                    case "patch-archives-index-size":
                        break;
                    default:
                        Logger.WriteLine("!!!!!!!! Unknown cdnconfig variable '" + cols[0] + "'");
                        break;
                }
            }

            return cdnConfig;
        }
        public static BuildConfigFile GetBuildConfig(string url, string hash)
        {
            string content;

            var buildConfig = new BuildConfigFile();

            if (url.StartsWith("http"))
            {
                try
                {
                    content = Encoding.UTF8.GetString(CDN.Get(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error retrieving CDN config: " + e.Message);
                    return buildConfig;
                }
            }
            else
            {
                content = File.ReadAllText(Path.Combine(url, "config", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                Console.WriteLine("Error reading build config!");
                return buildConfig;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i].StartsWith("#") || lines[i].Length == 0) { continue; }
                var cols = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "root":
                        buildConfig.root = cols[1].ToByteArray().ToMD5();
                        break;
                    case "download":
                        var downloadSplit = cols[1].Split(' ');

                        buildConfig.download = new MD5Hash[downloadSplit.Length];
                        buildConfig.download[0] = downloadSplit[0].ToByteArray().ToMD5();

                        if (downloadSplit.Length > 1)
                        {
                            buildConfig.download[1] = downloadSplit[1].ToByteArray().ToMD5();
                        }

                        break;
                    case "install":
                        var installSplit = cols[1].Split(' ');

                        buildConfig.install = new MD5Hash[installSplit.Length];
                        buildConfig.install[0] = installSplit[0].ToByteArray().ToMD5();

                        if (installSplit.Length > 1)
                        {
                            buildConfig.install[1] = installSplit[1].ToByteArray().ToMD5();
                        }
                        break;
                    case "encoding":
                        var encodingSplit = cols[1].Split(' ');

                        buildConfig.encoding = new MD5Hash[encodingSplit.Length];
                        buildConfig.encoding[0] = encodingSplit[0].ToByteArray().ToMD5();

                        if (encodingSplit.Length > 1)
                        {
                            buildConfig.encoding[1] = encodingSplit[1].ToByteArray().ToMD5();
                        }
                        break;
                    case "encoding-size":
                        var encodingSize = cols[1].Split(' ');
                        buildConfig.encodingSize = encodingSize;
                        break;
                    case "size":
                        buildConfig.size = cols[1].Split(' ');
                        break;
                    case "size-size":
                        buildConfig.sizeSize = cols[1].Split(' ');
                        break;
                    case "build-name":
                        buildConfig.buildName = cols[1];
                        break;
                    case "build-playbuild-installer":
                        buildConfig.buildPlaybuildInstaller = cols[1];
                        break;
                    case "build-product":
                        buildConfig.buildProduct = cols[1];
                        break;
                    case "build-uid":
                        buildConfig.buildUid = cols[1];
                        break;
                    case "patch":
                        buildConfig.patch = cols[1].ToByteArray().ToMD5();
                        break;
                    case "patch-size":
                        buildConfig.patchSize = cols[1];
                        break;
                    case "patch-config":
                        buildConfig.patchConfig = cols[1].ToByteArray().ToMD5();
                        break;
                    case "build-branch": // Overwatch
                        buildConfig.buildBranch = cols[1];
                        break;
                    case "build-num": // Agent
                    case "build-number": // Overwatch
                    case "build-version": // Catalog
                        buildConfig.buildNumber = cols[1];
                        break;
                    case "build-attributes": // Agent
                        buildConfig.buildAttributes = cols[1];
                        break;
                    case "build-comments": // D3
                        buildConfig.buildComments = cols[1];
                        break;
                    case "build-creator": // D3
                        buildConfig.buildCreator = cols[1];
                        break;
                    case "build-fixed-hash": // S2
                        buildConfig.buildFixedHash = cols[1];
                        break;
                    case "build-replay-hash": // S2
                        buildConfig.buildReplayHash = cols[1];
                        break;
                    case "build-t1-manifest-version":
                        buildConfig.buildManifestVersion = cols[1];
                        break;
                    case "install-size":
                        buildConfig.installSize = cols[1].Split(' ');
                        break;
                    case "download-size":
                        buildConfig.downloadSize = cols[1].Split(' ');
                        break;
                    case "build-partial-priority":
                    case "partial-priority":
                        buildConfig.partialPriority = cols[1];
                        break;
                    case "partial-priority-size":
                        buildConfig.partialPrioritySize = cols[1];
                        break;
                    default:
                        Logger.WriteLine("!!!!!!!! Unknown buildconfig variable '" + cols[0] + "'");
                        break;
                }
            }

            return buildConfig;
        }
    }
}
