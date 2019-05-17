using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CASCToolHost.Utils
{
    public struct CASCFile
    {
        public uint id;
        public string filename;
        public string type;
    }

    public static class Database
    {
        public static string GetRootCDNByBuildConfig(string buildConfig)
        {
            var rootcdn = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT root_cdn from wow_buildconfig WHERE hash = @hash LIMIT 1";
                    cmd.Parameters.AddWithValue("@hash", buildConfig);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rootcdn = reader["root_cdn"].ToString();
                        }
                    }
                }
            }

            return rootcdn;
        }
        public static string GetCDNConfigByBuildConfig(string buildConfig)
        {
            var cdnconfig = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT cdnconfig from wow_versions WHERE buildconfig = @hash LIMIT 1";
                    cmd.Parameters.AddWithValue("@hash", buildConfig);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cdnconfig = reader["cdnconfig"].ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(cdnconfig))
                {
                    throw new FileNotFoundException("Unable to locate proper CDNConfig for BuildConfig " + buildConfig);
                }
            }

            return cdnconfig;
        }

        public static string GetFilenameByFileDataID(uint filedataid)
        {
            Logger.WriteLine("Looking up filename for " + filedataid);
            var filename = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT filename from wow_rootfiles WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", filedataid);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            filename = reader["filename"].ToString();
                        }
                    }
                }
            }

            return filename;
        }

        public static uint GetFileDataIDByFilename(string filename)
        {
            Logger.WriteLine("Looking up filedataid for " + filename);
            uint filedataid = 0;

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id from wow_rootfiles WHERE filename = @filename";
                    cmd.Parameters.AddWithValue("@filename", filename.Replace('\\', '/').ToLower());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            filedataid = uint.Parse(reader["id"].ToString());
                        }
                    }
                }
            }

            return filedataid;
        }

        public static string[] GetFiles()
        {
            var fileList = new List<string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT filename from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' AND verified = 1 ORDER BY id DESC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fileList.Add(reader["filename"].ToString());
                        }
                    }
                }
            }

            return fileList.ToArray();
        }

        public static Dictionary<uint, CASCFile> GetKnownFiles(bool includeUnverified = false)
        {
            var dict = new Dictionary<uint, CASCFile>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    if (!includeUnverified)
                    {
                        cmd.CommandText = "SELECT id, filename, type from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' AND verified = 1 ORDER BY id DESC";
                    }
                    else
                    {
                        cmd.CommandText = "SELECT id, filename, type from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' ORDER BY id DESC";
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new CASCFile { id = uint.Parse(reader["id"].ToString()), filename = reader["filename"].ToString(), type = reader["type"].ToString() };
                            dict.Add(uint.Parse(reader["id"].ToString()), row);
                        }
                    }
                }
            }

            return dict;
        }

        public static Dictionary<uint, CASCFile> GetAllFiles()
        {
            var dict = new Dictionary<uint, CASCFile>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, filename, type from wow_rootfiles ORDER BY id DESC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new CASCFile { id = uint.Parse(reader["id"].ToString()), filename = reader["filename"].ToString(), type = reader["type"].ToString() };
                            dict.Add(uint.Parse(reader["id"].ToString()), row);
                        }
                    }
                }
            }

            return dict;
        }

        public static Dictionary<ulong, string> GetKnownLookups()
        {
            var dict = new Dictionary<ulong, string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT filename, CONV(lookup, 16, 10) as lookup from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' ORDER BY id DESC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dict.Add(ulong.Parse(reader["lookup"].ToString()), reader["filename"].ToString());
                        }
                    }
                }
            }

            return dict;
        }

        public static Dictionary<uint, string> GetFilesByBuild(string buildConfig)
        {
            var config = Config.GetBuildConfig("http://cdn.blizzard.com/tpr/wow/", buildConfig);

            var rootHash = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT root_cdn FROM wow_buildconfig WHERE hash = @hash";
                    cmd.Parameters.AddWithValue("@hash", buildConfig);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rootHash = reader["root_cdn"].ToString();
                        }
                    }
                }
            }

            if (rootHash == "")
            {
                EncodingFile encoding;

                if (config.encodingSize == null || config.encodingSize.Count() < 2)
                {
                    encoding = NGDP.GetEncoding("http://cdn.blizzard.com/tpr/wow/", config.encoding[1].ToHexString(), 0);
                }
                else
                {
                    encoding = NGDP.GetEncoding("http://cdn.blizzard.com/tpr/wow/", config.encoding[1].ToHexString(), int.Parse(config.encodingSize[1]));
                }


                if (encoding.aEntries.TryGetValue(config.root, out var rootEntry))
                {
                    rootHash = rootEntry.eKey.ToHexString().ToLower();
                }
                else
                {
                    throw new KeyNotFoundException("Root encoding key not found!");
                }
            }

            return GetFilesByRoot(rootHash);
        }

        public static Dictionary<uint, string> GetFilesByRoot(string rootHash)
        {
            var root = NGDP.GetRoot("http://cdn.blizzard.com/tpr/wow/", rootHash, true);

            var fileList = new Dictionary<uint, string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, filename from wow_rootfiles WHERE filename IS NOT NULL ORDER BY id DESC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fileList.Add(uint.Parse(reader["id"].ToString()), reader["filename"].ToString());
                        }
                    }
                }
            }

            var returnNames = new Dictionary<uint, string>();
            foreach (var entry in root.entriesFDID)
            {
                if (fileList.TryGetValue(entry.Key, out string filename))
                {
                    returnNames.Add(entry.Key, filename);
                }
            }
            return returnNames;
        }
    }
}
