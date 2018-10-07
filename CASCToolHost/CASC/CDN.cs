using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cacheDir = "H:/";
            }
            else
            {
                cacheDir = "/var/www/bnet.marlam.in/";
            }

            client = new HttpClient();
        }

        public static byte[] Get(string url, bool returnstream = true, bool redownload = false)
        {
            var uri = new Uri(url.ToLower());

            string cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    Logger.WriteLine("Downloading " + cleanname);
                    using (HttpResponseMessage response = client.GetAsync(uri).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (MemoryStream mstream = new MemoryStream())
                            using (HttpContent res = response.Content)
                            {
                                res.CopyToAsync(mstream);

                                if (isEncrypted)
                                {
                                    var cleaned = Path.GetFileNameWithoutExtension(cleanname);
                                    var decrypted = BLTE.DecryptFile(cleaned, mstream.ToArray(), decryptionKeyName);

                                    File.WriteAllBytes(cacheDir + cleanname, decrypted);
                                    return decrypted;
                                }
                                else
                                {
                                    File.WriteAllBytes(cacheDir + cleanname, mstream.ToArray());
                                }
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound && !url.StartsWith("http://client04"))
                        {
                            Logger.WriteLine("Not found on primary mirror, retrying on secondary mirror...");
                            return Get("http://client04.pdl.wow.battlenet.com.cn/" + cleanname, returnstream, redownload);
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
                return File.ReadAllBytes(cacheDir + cleanname);
            }
            else
            {
                return new byte[0];
            }
        }
    }
}