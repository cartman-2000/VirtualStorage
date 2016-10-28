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
            // Load the container data, if it isn't already loaded.
            if (VirtualStorage.Containers.ContainsKey(player.CSteamID) && VirtualStorage.Containers[player.CSteamID].ContainerName.ToLower() == container[2].ToString().Trim().ToLower())
            {
                cData = VirtualStorage.Containers[player.CSteamID];
                VirtualStorage.Containers.Remove(player.CSteamID);
            }

            if (cData == null)
            {
                object[] cObject = VirtualStorage.Database.GetContainerData(player.CSteamID, container[2].ToString());
                cData = new ContainerManager(player);
                if (!cData.SetContainer((ushort)cObject[0], (byte[])cObject[1], player, (string)cObject[2], (byte)cObject[3], (byte)cObject[4]))
                {
                    if (!cData.SetContainer(VirtualStorage.Instance.Configuration.Instance.FallbackAssetID, (byte[])cObject[1], player, (string)cObject[2], (byte)cObject[3], (byte)cObject[4]))
                    {
                        UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("open_invalid"), Color.red);
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(defaultContainer))
            {
                if (defaultContainer.ToLower() == command[0].Trim().ToLower())
                    VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, string.Empty);
            }
            VirtualStorage.Database.RemoveContainerFromDB(player.CSteamID, container[2].ToString());
            cData.Break();
        }
    }
}
