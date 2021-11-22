using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public static class CDNCache
    {
        public static HttpClient client;
        public static string cacheDir;
        public static bool isEncrypted = false;
        public static string decryptionKeyName = "";
        public const string bestCDNEU = "http://level3.blizzard.com";
        public static string backupCDN = "http://blzddist1-a.akamaihd.net";

        static CDNCache()
        {
            cacheDir = SettingsManager.cacheDir;
            client = new HttpClient();
        }

        /// <summary>
        /// Gets a file from disk, if not available on disk it downloads it first.
        /// </summary>
        /// <param name="subFolder">"config", "data" or "patch"</param>
        /// <param name="file">File</param>
        /// <param name="returnstream">Whether or not to return byte[] array</param>
        /// <param name="redownload">Whether or not to redownload the file</param>
        /// <param name="size">Size (for partial downloads)</param>
        /// <param name="offset">Offset (for partial downloads)</param>
        /// <returns></returns>
        public static async Task<byte[]> Get(string subFolder, string file, bool returnstream = true, bool redownload = false, uint size = 0, uint offset = 0, string cdn = bestCDNEU)
        {
            file = file.ToLower();

            var target = bestCDNEU + "/tpr/wow/" + subFolder + "/" + file[0] + file[1] + "/" + file[2] + file[3] + "/" + file;
            var uri = new Uri(target);
            var cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    Logger.WriteLine("WARNING! Downloading " + cleanname);
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    if (size > 0)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, offset + size);
                    }

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (MemoryStream mstream = new MemoryStream())
                            using (HttpContent res = response.Content)
                            {
                                await res.CopyToAsync(mstream);

                                if (isEncrypted)
                                {
                                    var cleaned = Path.GetFileNameWithoutExtension(cleanname);
                                    var decrypted = BLTE.DecryptFile(cleaned, mstream.ToArray(), decryptionKeyName);

                                    // Only write out if this is a full DL
                                    if (size == 0)
                                    {
                                        await File.WriteAllBytesAsync(cacheDir + cleanname, decrypted);
                                    }
                                    return decrypted;
                                }
                                else
                                {
                                    if (size == 0)
                                    {
                                        await File.WriteAllBytesAsync(cacheDir + cleanname, mstream.ToArray());
                                    }
                                    else
                                    {
                                        return mstream.ToArray();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (cdn != backupCDN)
                            {
                                Logger.WriteLine("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + target + ", trying on backup CDN..");
                                return await Get(subFolder, file, returnstream, redownload, size, offset, backupCDN);
                            }
                            else
                            {
                                throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + target + ", exhausted all CDNs.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine(e.Message);
                }
            }

            if (returnstream)
            {
                return await File.ReadAllBytesAsync(cacheDir + cleanname);
            }
            else
            {
                return new byte[0];
            }
        }
    }
}