using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirtualStorage
{
    class CommandVTransfer : IRocketCommand
    {
        internal static readonly string syntax = "<\"Container Name\"> <\"Playername\">";
        internal static readonly string help = "Transfers a container to another player.";
        public List<string> Aliases
        {
            get { return new List<string>() { "vtran", "vt" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public string Help
        {
            get { return help; }
        }

        public string Name
        {
            get { return "vtransfer"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vtransfer" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0 || command.Length > 2)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("vtransfer_help"));
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            string targetPlayerName = command[1].Trim();
            UnturnedPlayer targetPlayer;
            if (targetPlayerName != null)
            {
                targetPlayer = UnturnedPlayer.FromName(targetPlayerName);
                if (targetPlayer == null)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("no_player_name"));
                    return;
                }
            }
            else
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("no_player_name"));
                return;
            }
            string cName = command[0].Trim().Truncate(60);
            if (cName == string.Empty)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_no_name"), Color.red);
                return;
            }
            Dictionary<string, object[]> containers = VirtualStorage.Database.GetContainerList(player.CSteamID);
            object[] exists = containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == cName.ToLower());
            if (exists == null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("container_name_not_found"), Color.red);
                return;
            }
            Dictionary<string, object[]> targetContainers = VirtualStorage.Database.GetContainerList(targetPlayer.CSteamID);
            object[] targetExists = targetContainers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == cName.ToLower());
            if (targetExists != null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("target_has_container"));
                return;
            }
            if (targetContainers.Count > VirtualStorage.Instance.Configuration.Instance.MaxContainersPerPlayer && !(caller.HasPermission("vs.overridemax") || targetPlayer.HasPermission("vs.overridemax")))
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("target_max_reached", VirtualStorage.Instance.Configuration.Instance.MaxContainersPerPlayer), Color.red);
                return;
            }

            string defaultContainer = VirtualStorage.Database.GetDefaultContainer(player.CSteamID);
            if (!string.IsNullOrEmpty(defaultContainer) && containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == defaultContainer.ToLower()) != null)
            {
                VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, string.Empty);
                if (VirtualStorage.Containers.ContainsKey(player.CSteamID))
                {
                    // Close and remove container from player in preparation to transfer container.
                    VirtualStorage.Containers[player.CSteamID].Close();
                    VirtualStorage.Containers.Remove(player.CSteamID);
                }
            }
            VirtualStorage.Database.TransferContainer(player.CSteamID, targetPlayer.CSteamID, cName);
            UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("sent_to_player", cName, targetPlayer.CharacterName.Truncate(16)), Color.cyan);
            UnturnedChat.Say(targetPlayer, VirtualStorage.Instance.Translate("received_from_player", cName, player.CharacterName.Truncate(16)), Color.cyan);
        }
    }
}

