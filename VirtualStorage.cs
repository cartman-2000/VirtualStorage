using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.API.Collections;
using Rocket.API;

namespace VirtualStorage
{
    public class VirtualStorage : RocketPlugin<VirtualStorageConfig>
    {
        public static VirtualStorage Instance;
        public static DatabaseManager Database;
        public static Dictionary<CSteamID, ContainerManager> Containers = new Dictionary<CSteamID, ContainerManager>();
        internal static bool InitialLoadPassed = false;

        protected override void Load()
        {
            Instance = this;
            Database = new DatabaseManager();
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            // Load Containers after the level loads, in initial start, to give workshop mods a chance to load into the server after the plugin loads.
            Level.onPostLevelLoaded += Database.SetupContainers;
            if (InitialLoadPassed)
                Database.SetupContainers(0);
            Instance.Configuration.Instance.LoadDefaults();
            if (Instance.Configuration.Instance.KeepaliveInterval <= 0)
            {
                Logger.LogWarning("Error: Keep alive config option must be above 0.");
                Instance.Configuration.Instance.KeepaliveInterval = 10;
            }
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            Database.Unload();
            Database = null;
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player != null)
            {
                if (Containers.ContainsKey(player.CSteamID))
                {
                    ContainerManager cData = Containers[player.CSteamID];
                    if (cData.WasOpen)
                    {
                        // Player must have disconnected when the inventory was open, save the container.
                        Database.SaveContainerToDB(cData);
                        cData.Close();
                    }
                    Containers.RemoveContainer(player.CSteamID);
                }
            }
        }

        internal static bool TryCharge(IRocketPlayer caller, decimal cost, string permission = "")
        {
            if (permission != string.Empty && caller.HasPermission(permission))
            {
                return true;
            }
            decimal curBalance = fr34kyn01535.Uconomy.Uconomy.Instance.Database.GetBalance(caller.Id);
            if (curBalance > cost)
            {
                fr34kyn01535.Uconomy.Uconomy.Instance.Database.IncreaseBalance(caller.Id, -cost);
                return true;
            }
            return false;
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    { "vbuy_help", CommandVBuy.syntax + " - " + CommandVBuy.help },
                    { "vname_help", CommandVName.syntax + " - " + CommandVName.help },
                    { "vremove_help", CommandVRemove.syntax + " - " + CommandVRemove.help },
                    { "vset_help", CommandVSet.syntax + " - " + CommandVSet.help },
                    { "vtransfer_help", CommandVTransfer.syntax + " - " + CommandVTransfer.help },
                    { "opening_container", "Opening container: {0}." },
                    { "removing_container", "Container: {0} has been removed, {1} items have been dropped." },
                    { "number_to_buy", "There are {0} containers that you can buy:" },
                    { "buy_list_entry", "Listed Name: {0}, Listed Price: {1}." },
                    { "buy_no_name", "You need a name for the container." },
                    { "buy_not_in_list", "No Listed containers found by that name." },
                    { "buy_max_reached", "Max allowed containers reached, you need to remove one to buy another. Max allowed per player: {0}" },
                    { "buy_charged", "You have bought the container: {0}, with name: {1}, for {2} {3}s." },
                    { "buy_charge_override", "You have obtained the container: {0}, with name: {1}." },
                    { "buy_default_set", "Your default container has been set to: {0}." },
                    { "not_enough_credits", "You don't have enough {0}s." },
                    { "no_default_set", "You don't have a default container set." },
                    { "matches_owned", "You already have a container by this name." },
                    { "name_set", "Your container has been set to the name of: {0}" },
                    { "open_data_not_found", "Error: Couldn't find the data for the default container to load." },
                    { "open_invalid", "Error Loading container, Invalid AssetID/Version." },
                    { "open_charged", "You have been charged {0} {1}s to open the container." },
                    { "container_name_not_found", "A container by that name wasn't found." },
                    { "set_no_containers_found", "No containers found." },
                    { "set_number_owned", "You have {0} container(s):" },
                    { "set_owned_entry", "{0}Set Name: {1}, List Name: {2}, ItemCount: {3}" },
                    { "set_already_set", "Error: Your default container is already set to this." },
                    { "container_set", "Your container has been set to: {0}" },
                    { "no_player_name", "The playername entered isn't a valid one." },
                    { "target_has_container", "The target player has a container by this name already, you will need to change the name to transfer it." },
                    { "target_max_reached", "The target player has reached the max allowed containers. Max allowed per player: {0}" },
                    { "sent_to_player", "You have sent container: {0} to player: {1}." },
                    { "received_from_player", "You have received container: {0} from player: {1}." }
                };
            }
        }
    }
}
