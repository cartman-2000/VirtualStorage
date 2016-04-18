using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace VirtualStorage
{
    public class DatabaseManager
    {
        public bool Initialized { get; private set; }
        private MySqlConnection Connection = null;
        private Timer KeepAlive = null;
        private int MaxRetry = 5;
        private string Table;
        private string TableData;
        public static readonly uint DatabaseSchemaVersion = 1;
        internal Dictionary<ushort, Container> ConfigContainers = new Dictionary<ushort, Container>();

        // Initialization section.
        internal DatabaseManager()
        {
            new I18N.West.CP1250();
            Initialized = false;
            Table = VirtualStorage.Instance.Configuration.Instance.DatabaseTableName;
            TableData = Table + "_data";
            CheckSchema();
            foreach (Container row in VirtualStorage.Instance.Configuration.Instance.Containers)
            {
                ItemBarricadeAsset ItemAsset = ((ItemBarricadeAsset)Assets.find(EAssetType.ITEM, row.AssetID));
                if (ItemAsset == null || ItemAsset.build != EBuild.STORAGE)
                {
                    Logger.LogWarning("Invalid Asset ID in the config, skipping, AssetID: " + row.AssetID);
                    continue;
                }
                if (ConfigContainers.ContainsKey(row.AssetID))
                {
                    Logger.LogWarning("Duplicate Asset ID in the config, skipping, AssetID: " + row.AssetID);
                    continue;
                }
                ConfigContainers.Add(row.AssetID, row);
            }
        }

        internal void Unload()
        {
            if (KeepAlive != null)
            {
                KeepAlive.Stop();
                KeepAlive.Dispose();
            }
            Connection.Dispose();
            ConfigContainers.Clear();
        }

        // Plugin/Database setup section.
        private void CheckSchema()
        {
            try
            {
                if (!CreateConnection())
                    return;
                ushort version = 0;
                MySqlCommand command = Connection.CreateCommand();
                command.CommandText = "show tables like '" + Table + "';";
                object test = command.ExecuteScalar();

                if (test == null)
                {
                    command.CommandText += "CREATE TABLE `" + Table + "` (" +
                        " `SteamID` bigint(24) unsigned NOT NULL," +
                        " `DefaultContainer` varchar(60) COLLATE utf8_unicode_ci NOT NULL," +
                        " PRIMARY KEY(`SteamID`)" +
                        ") ENGINE = MyISAM DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableData + "` (" +
                        " `SteamID` bigint(24) unsigned NOT NULL," +
                        " `ContainerName` varchar(60) COLLATE utf8_unicode_ci NOT NULL," +
                        " `AssetID` mediumint(8) unsigned NOT NULL," +
                        " `ContainerVersion` tinyint(3) unsigned NOT NULL," +
                        " `ItemCount` tinyint(3) unsigned NOT NULL," +
                        " `ContainerData` blob NOT NULL," +
                        " PRIMARY KEY (`SteamID`,`ContainerName`)" +
                        ") ENGINE = MyISAM DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";

                    command.ExecuteNonQuery();
                    CheckVersion(version, command);
                    
                }
                else
                {
                    
                    command.CommandText = "SELECT `DefaultContainer` FROM `" + Table + "` WHERE `SteamID` = 0";
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        if (ushort.TryParse(result.ToString(), out version))
                        {
                            if (version < DatabaseSchemaVersion)
                                CheckVersion(version, command);
                        }
                        else
                        {
                            Logger.LogError("Error: Database version number not found.");
                            return;
                        }
                    }
                    else
                    {
                        Logger.LogError("Error: Database version number not found.");
                        return;
                    }
                }

                if (KeepAlive == null)
                {
                    KeepAlive = new Timer(VirtualStorage.Instance.Configuration.Instance.KeepaliveInterval * 60000);
                    KeepAlive.Elapsed += delegate { CheckConnection(); };
                    KeepAlive.AutoReset = true;
                    KeepAlive.Start();
                }
                Initialized = true;
            }
            catch (MySqlException ex)
            {
                Logger.LogException(ex);
            }
        }

        private void CheckVersion(ushort version, MySqlCommand command)
        {
            ushort updatingVersion = 0;
            try
            {
                if (version < 1)
                {
                    updatingVersion = 1;
                    command.CommandText = "INSERT INTO `" + Table + "` (`SteamID`, `DefaultContainer`) VALUES ('0', '1');";
                    command.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex, "Failed in updating Database schema to version " + updatingVersion + ", you may have to do a manual update to the database schema.");
            }
        }

        // Connection handling section.
        private void CheckConnection()
        {
            try
            {
                MySqlCommand command = Connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
        }

        private bool CreateConnection(int count = 1)
        {
            try
            {
                Connection = null;
                if (VirtualStorage.Instance.Configuration.Instance.DatabasePort == 0)
                    VirtualStorage.Instance.Configuration.Instance.DatabasePort = 3306;
                Connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", VirtualStorage.Instance.Configuration.Instance.DatabaseAddress, VirtualStorage.Instance.Configuration.Instance.DatabaseName, VirtualStorage.Instance.Configuration.Instance.DatabaseUserName, VirtualStorage.Instance.Configuration.Instance.DatabasePassword, VirtualStorage.Instance.Configuration.Instance.DatabasePort));
                Connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                if (count < MaxRetry)
                {
                    return CreateConnection(count + 1);
                }
                Logger.LogException(ex, "Failed to connect to the database server!");
                return false;
            }
        }

        private bool HandleException(MySqlException ex, string msg = null)
        {
            if (ex.Number == 0)
            {
                Logger.LogException(ex, "Error: Connection lost to database server, attempting to reconnect.");
                if (CreateConnection())
                {
                    Logger.Log("Success.");
                    return true;
                }
                Logger.LogError("Reconnect Failed.");
            }
            else
            {
                Logger.LogWarning(ex.Number.ToString() + ":" + ((MySqlErrorCode)ex.Number).ToString());
                Logger.LogException(ex, msg != null ? msg : null);
            }
            return false;
        }

        // Data Gathering Section
        // Grabs the stored data for the Container.
        internal object[] GetContainerData(CSteamID steamID, string containerName)
        {
            object[] tmp = null;
            MySqlDataReader reader = null;
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant load player info from DB, plugin hasn't initialized properly.");
                    return tmp;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", steamID);
                command.Parameters.AddWithValue("@cname", containerName);
                command.CommandText = "SELECT * FROM `"+TableData+"` WHERE SteamID = @steamid AND ContainerName = @cname";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    tmp = new object[]
                    {
                        reader.GetUInt16("AssetID"),
                        reader.GetValue(reader.GetOrdinal("ContainerData")) as byte[],
                        reader.GetString("ContainerName"),
                        reader.GetByte("ItemCount"),
                        reader.GetByte("ContainerVersion"),
                    };
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return tmp;
        }

        // Grabs the default set Container for a player, if there is one.
        internal string GetDefaultContainer(CSteamID steamID)
        {
            string tmp = null;
            MySqlDataReader reader = null;

            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant load player info from DB, plugin hasn't initialized properly.");
                    return tmp;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", steamID);
                command.CommandText = "SELECT DefaultContainer FROM `"+Table+"` WHERE SteamID = @steamid";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    tmp = reader.GetString("DefaultContainer");
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return tmp;
        }

        //Grabs a list of containers that a player owns.
        internal Dictionary<string, object[]> GetContainerList(CSteamID SteamID)
        {
            Dictionary<string, object[]> tmp = new Dictionary<string, object[]>();
            MySqlDataReader reader = null;
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant load player info from DB, plugin hasn't initialized properly.");
                    return tmp;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", SteamID);
                command.CommandText = "SELECT ContainerName, AssetID, ItemCount FROM `" + TableData + "` WHERE SteamID = @steamid";
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    return tmp;
                }
                while (reader.Read())
                {
                    tmp.Add(reader.GetString("ContainerName"), new object[]
                    {
                        reader.GetUInt16("AssetID"),
                        reader.GetByte("ItemCount"),
                        reader.GetString("ContainerName"),
                    });
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return tmp;
        }

        // Data Saving section
        internal void SaveContainerToDB(ContainerManager cData, bool retry = false)
        {
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant save player info, plugin hasn't initialized properly.");
                    return;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", cData.Player.CSteamID);
                command.Parameters.AddWithValue("@cname", cData.ContainerName);
                command.Parameters.AddWithValue("@assetid", cData.AssetID);
                command.Parameters.AddWithValue("@cversion", cData.ContainerVersion);
                command.Parameters.AddWithValue("@itemcount", cData.ItemCount);
                command.Parameters.AddWithValue("@cdata", cData.State);
                command.CommandText = "INSERT INTO `" + TableData + "` (`SteamID`, `ContainerName`, `AssetID`, `ContainerVersion`, `ItemCount`, `ContainerData`) VALUES (@steamid, @cname, @assetid, @cversion, @itemcount, @cdata) ON DUPLICATE KEY UPDATE `ContainerName` = VALUES(`ContainerName`), `AssetID` = VALUES(`AssetID`), `ContainerVersion` = VALUES(`ContainerVersion`), `ItemCount` = VALUES(`ItemCount`), `ContainerData` = VALUES(`ContainerData`);";
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                if (!retry)
                {
                    if (HandleException(ex))
                        SaveContainerToDB(cData, true);
                }
            }
        }

        internal void RemoveContainerFromDB(CSteamID SteamID, string ContainerName, bool retry = false)
        {
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant save player info, plugin hasn't initialized properly.");
                    return;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", SteamID);
                command.Parameters.AddWithValue("@containername", ContainerName);
                command.CommandText = "DELETE FROM `" + TableData + "` WHERE SteamID = @steamid AND ContainerName = @containername";
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                if (!retry)
                {
                    if (HandleException(ex))
                        SaveDefaultContainer(SteamID, ContainerName, true);
                }
            }

        }

        internal void SaveDefaultContainer(CSteamID SteamID, string DefaultContainer, bool retry = false)
        {
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant save player info, plugin hasn't initialized properly.");
                    return;
                }
                if (SteamID == CSteamID.Nil)
                    return;
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", SteamID);
                command.Parameters.AddWithValue("@defaultcontainer", DefaultContainer);
                command.CommandText = "INSERT INTO `" + Table + "` (`SteamID`, `DefaultContainer`) VALUES (@steamid, @defaultcontainer) ON DUPLICATE KEY UPDATE `DefaultContainer` = VALUES(`DefaultContainer`)";
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                if (!retry)
                {
                    if (HandleException(ex))
                        SaveDefaultContainer(SteamID, DefaultContainer, true);
                }
            }
        }

        internal void RenameContainer(CSteamID SteamID, string OldName, string NewName, bool retry = false)
        {
            try
            {
                if (!Initialized)
                {
                    Logger.LogError("Error: Cant save player info, plugin hasn't initialized properly.");
                    return;
                }
                MySqlCommand command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@steamid", SteamID);
                command.Parameters.AddWithValue("@oldname", OldName);
                command.Parameters.AddWithValue("@newname", NewName);
                command.CommandText = "UPDATE `"+TableData+"` SET ContainerName = @newname WHERE SteamID = @steamid AND ContainerName = @oldname";
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                if (!retry)
                {
                    if (HandleException(ex))
                        RenameContainer(SteamID, OldName, NewName, true);
                }
            }
        }
    }
}