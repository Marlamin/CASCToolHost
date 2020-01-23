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

        static CDN()
        {
            cacheDir = SettingsManager.cacheDir;
            client = new HttpClient();
        }

        public static async Task<byte[]> Get(string url, bool returnstream = true, bool redownload = false)
        {
            var uri = new Uri(url.ToLower());

            string cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    Logger.WriteLine("WARNING! Downloading " + cleanname);
                    using (HttpResponseMessage response = await client.GetAsync(uri))
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

                                    await File.WriteAllBytesAsync(cacheDir + cleanname, decrypted);
                                    return decrypted;
                                }
                                else
                                {
                                    await File.WriteAllBytesAsync(cacheDir + cleanname, mstream.ToArray());
                                }
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound && !url.StartsWith("http://client04"))
                        {
                            Logger.WriteLine("Not found on primary mirror, retrying on secondary mirror...");
                            return await Get("http://client04.pdl.wow.battlenet.com.cn/" + cleanname, returnstream, redownload);
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