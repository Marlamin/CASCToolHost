using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        public static async Task<string> GetRootCDNByBuildConfig(string buildConfig)
        {
            var rootcdn = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT root_cdn from wow_buildconfig WHERE hash = @hash LIMIT 1";
                cmd.Parameters.AddWithValue("@hash", buildConfig);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rootcdn = reader["root_cdn"].ToString();
                }
            }

            return rootcdn;
        }
        public static async Task<string> GetCDNConfigByBuildConfig(string buildConfig)
        {
            var cdnconfig = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT cdnconfig from wow_versions WHERE buildconfig = @hash LIMIT 1";
                    cmd.Parameters.AddWithValue("@hash", buildConfig);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        cdnconfig = reader["cdnconfig"].ToString();
                    }
                }

                if (string.IsNullOrEmpty(cdnconfig))
                {
                    throw new FileNotFoundException("Unable to locate proper CDNConfig for BuildConfig " + buildConfig);
                }
            }

            return cdnconfig;
        }

        public static async Task<string> GetFilenameByFileDataID(uint filedataid)
        {
            Logger.WriteLine("Looking up filename for " + filedataid);
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT filename from wow_rootfiles WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", filedataid);
                return (string)await cmd.ExecuteScalarAsync();
            }
        }

        public static async Task<Controllers.FileTableController.DBFile> GetFileByFileDataID(uint filedataid)
        {
            var file = new Controllers.FileTableController.DBFile();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id, lookup, filename, verified, type, firstseen from wow_rootfiles WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", filedataid);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    file.ID = (uint)reader.GetInt32(0);
                    file.Lookup = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    file.Filename = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    file.Verified = reader.GetBoolean(3);
                    file.Type = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    file.FirstSeen = reader.IsDBNull(5) ? "" : reader.GetString(5);
                }
            }

            return file;
        }

        public static async Task<uint> GetFileDataIDByFilename(string filename)
        {
            Logger.WriteLine("Looking up filedataid for " + filename);
            uint filedataid = 0;

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id from wow_rootfiles WHERE filename = @filename";
                cmd.Parameters.AddWithValue("@filename", filename.Replace('\\', '/').ToLower());
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    filedataid = uint.Parse(reader["id"].ToString());
                }
            }

            return filedataid;
        }

        public static async Task<string[]> GetFiles()
        {
            var fileList = new List<string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT filename from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' AND verified = 1 ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    fileList.Add(reader["filename"].ToString());
                }
            }

            return fileList.ToArray();
        }

        public static async Task<uint[]> GetUnknownFiles()
        {
            var fileList = new List<uint>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id from wow_rootfiles WHERE filename IS NULL ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    fileList.Add(uint.Parse(reader["id"].ToString()));
                }
            }

            return fileList.ToArray();
        }

        public static async Task<Dictionary<uint, CASCFile>> GetKnownFiles(bool includeUnverified = false)
        {
            var dict = new Dictionary<uint, CASCFile>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                if (!includeUnverified)
                {
                    cmd.CommandText = "SELECT id, filename, type from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' AND verified = 1 ORDER BY id DESC";
                }
                else
                {
                    cmd.CommandText = "SELECT id, filename, type from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' ORDER BY id DESC";
                }
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new CASCFile { id = uint.Parse(reader["id"].ToString()), filename = reader["filename"].ToString(), type = reader["type"].ToString() };
                    dict.Add(uint.Parse(reader["id"].ToString()), row);
                }
            }

            return dict;
        }

        public static async Task<Dictionary<uint, CASCFile>> GetAllFiles()
        {
            var dict = new Dictionary<uint, CASCFile>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id, filename, type from wow_rootfiles ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new CASCFile { id = uint.Parse(reader["id"].ToString()), filename = reader["filename"].ToString(), type = reader["type"].ToString() };
                    dict.Add(uint.Parse(reader["id"].ToString()), row);
                }
            }

            return dict;
        }

        public static async Task<Dictionary<ulong, string>> GetKnownLookups()
        {
            var dict = new Dictionary<ulong, string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT filename, CONV(lookup, 16, 10) as lookup from wow_rootfiles WHERE filename IS NOT NULL AND filename != '' ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dict.Add(ulong.Parse(reader["lookup"].ToString()), reader["filename"].ToString());
                }
            }

            return dict;
        }

        public static async Task<Dictionary<uint, string>> GetFilesByBuild(string buildConfig)
        {
            var config = await Config.GetBuildConfig(buildConfig);

            var rootHash = "";

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT root_cdn FROM wow_buildconfig WHERE hash = @hash";
                cmd.Parameters.AddWithValue("@hash", buildConfig);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rootHash = reader["root_cdn"].ToString();
                }
            }

            if (NGDP.encodingDictionary.TryGetValue(config.root, out var rootEntry))
            {
                rootHash = rootEntry.ToHexString().ToLower();
            }

            if (rootHash == "")
            {
                EncodingFile encoding;

                if (config.encodingSize == null || config.encodingSize.Count() < 2)
                {
                    encoding = await NGDP.GetEncoding(config.encoding[1].ToHexString(), 0);
                }
                else
                {
                    encoding = await NGDP.GetEncoding(config.encoding[1].ToHexString(), int.Parse(config.encodingSize[1]));
                }

                if (encoding.aEntries.TryGetValue(config.root, out var bakRootEntry))
                {
                    rootHash = bakRootEntry.eKey.ToHexString().ToLower();
                }
                else
                {
                    throw new KeyNotFoundException("Root encoding key not found!");
                }
            }

            return await GetFilesByRoot(rootHash);
        }

        public static async Task<Dictionary<uint, string>> GetFilesByRoot(string rootHash)
        {
            var root = await NGDP.GetRoot(rootHash, true);

            var fileList = new Dictionary<uint, string>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT id, filename from wow_rootfiles WHERE filename IS NOT NULL ORDER BY id DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    fileList.Add(uint.Parse(reader["id"].ToString()), reader["filename"].ToString());
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

        public static async Task<Dictionary<ulong, byte[]>> GetKnownTACTKeys()
        {
            var keys = new Dictionary<ulong, byte[]>();
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT keyname, keybytes FROM wow_tactkey WHERE keybytes IS NOT NULL";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        keys.Add(ulong.Parse(reader["keyname"].ToString(), System.Globalization.NumberStyles.HexNumber), reader["keybytes"].ToString().ToByteArray());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return keys;
        }

        public static async Task<string> GetBuildConfigByFullBuild(string fullBuild)
        {
            string buildConfig = "";

            var buildParts = fullBuild.Split('.');
            var tactBuild = "WOW-" + buildParts[3] + "patch" + buildParts[0] + "." + buildParts[1] + "." + buildParts[2];
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT hash FROM wow_buildconfig WHERE description LIKE @description";
                    cmd.Parameters.AddWithValue("@description", tactBuild + "%");
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        buildConfig = reader["hash"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return buildConfig;
        }
    }
}
