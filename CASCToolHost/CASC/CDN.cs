using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public static class CDN
    {
        public static HttpClient client;
        public static string cacheDir;
        public static bool isEncrypted = false;
        public static string decryptionKeyName = "";
        public static string bestCDNEU = "";

        static CDN()
        {
            cacheDir = SettingsManager.cacheDir;
            client = new HttpClient();

            // TODO: Add check to see which CDN is better. Assuming eu.cdn.blizzard.com and that it won't rate limit to hell and back.
            bestCDNEU = "http://blzddist1-a.akamaihd.net/";
        }

        public static async Task<byte[]> Get(string url, bool returnstream = true, bool redownload = false, uint size = 0, uint offset = 0)
        {
            var uri = new Uri(url.ToLower());

            string cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    Logger.WriteLine("WARNING! Downloading " + cleanname);
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    if(size > 0)
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
                                    if(size == 0)
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
                            throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + url);
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