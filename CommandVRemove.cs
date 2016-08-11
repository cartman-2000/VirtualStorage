using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VirtualStorage
{
    class CommandVRemove : IRocketCommand
    {
        internal static readonly string syntax = "<\"Container Name\">";
        internal static readonly string help = "Removes a Container.";
        public List<string> Aliases
        {
            get { return new List<string>() { "vr" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return help; }
        }

        public string Name
        {
            get { return "vremove"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vremove" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0 || command.Length > 1)
            {
                UnturnedChat.Say(caller, Syntax + " - " + Help);
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            PVirtStorage pComponent = player.GetComponent<PVirtStorage>();


            Dictionary<string, object[]> containers = VirtualStorage.Database.GetContainerList(player.CSteamID);
            string defaultContainer = VirtualStorage.Database.GetDefaultContainer(player.CSteamID);
            object[] container = containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == command[0].ToLower().Trim());
            if (container == null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("container_name_not_found"), Color.red);
                return;
            }


            ContainerManager cData = null;
            if (VirtualStorage.Containers.ContainsKey(player.CSteamID))
            {
                cData = VirtualStorage.Containers[player.CSteamID];
                if (VirtualStorage.Containers[player.CSteamID].ContainerName.ToLower() == container[2].ToString().Trim().ToLower())
                {
                    if (VirtualStorage.Database.GetDefaultContainer(player.CSteamID).ToLower() == command[0].Trim().ToLower())
                    {
                        VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, string.Empty);
                    }
                    VirtualStorage.Database.RemoveContainerFromDB(player.CSteamID, container[2].ToString());
                    cData = VirtualStorage.Containers[player.CSteamID];
                    cData.Break();
                    pComponent.cData = null;
                    VirtualStorage.Containers.Remove(player.CSteamID);
                    return;
                }
            }
            else
            {
                if (!VirtualStorage.Containers.ContainsKey(player.CSteamID))
                    cData = new ContainerManager(player);
            }
            object[] cobject = VirtualStorage.Database.GetContainerData(player.CSteamID, container[2].ToString());
            if(!cData.SetContainer((ushort)cobject[0],(byte[])cobject[1], player, (string)cobject[2], (byte)cobject[3], (byte)cobject[4]))
            {
                if (!cData.SetContainer(VirtualStorage.Instance.Configuration.Instance.FallbackAssetID,(byte[])cobject[1], player, (string)cobject[2], (byte)cobject[3], (byte)cobject[4]))
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("open_invalid"), Color.red);
                    return;
                }
            }
            if (!string.IsNullOrEmpty(defaultContainer))
            {
                if (defaultContainer.ToLower() == command[0].Trim().ToLower())
                    VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, string.Empty);
            }
            VirtualStorage.Database.RemoveContainerFromDB(player.CSteamID, container[2].ToString());
            pComponent.cData = null;
            cData.Break();
        }
    }
}
