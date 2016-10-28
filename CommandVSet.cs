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
    class CommandVSet : IRocketCommand
    {
        internal static readonly string syntax = "<list> | <\"Container Name\">";
        internal static readonly string help = "Set's the active container.";
        public List<string> Aliases
        {
            get { return new List<string>() { "vs" }; }
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
            get { return "vset"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vset" }; }
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

            Dictionary<string, object[]> containers = VirtualStorage.Database.GetContainerList(player.CSteamID);
            if (containers.Count == 0)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("set_no_containers_found"), Color.red);
                return;
            }
            string defaultContainer = VirtualStorage.Database.GetDefaultContainer(player.CSteamID);
            if (command[0].Trim().ToLower() == "list")
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("set_number_owned", containers.Count), Color.cyan);
                foreach (KeyValuePair<string, object[]> container in containers)
                {
                    int count = (byte)container.Value[1];
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("set_owned_entry", defaultContainer == container.Key ? "-->" : "", container.Key, VirtualStorage.Database.ConfigContainers.ContainsKey((ushort)container.Value[0]) ? VirtualStorage.Database.ConfigContainers[(ushort)container.Value[0]].ContainerName : "-", (byte)container.Value[1]), Color.yellow);
                }
            }
            else
            {
                object[] container = containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == command[0].ToLower().Trim());
                if (container == null)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("container_name_not_found"), Color.red);
                    return;
                }
                if (!string.IsNullOrEmpty(defaultContainer))
                {
                    if (container[2].ToString().ToLower() == defaultContainer.ToLower())
                    {
                        UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("set_already_set"), Color.red);
                        return;
                    }
                }
                if (VirtualStorage.Containers.ContainsKey(player.CSteamID))
                {
                    // Close the container when setting a different one.
                    VirtualStorage.Containers[player.CSteamID].Close();
                    VirtualStorage.Containers.Remove(player.CSteamID);
                }
                VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, container[2].ToString());
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("container_set", container[2].ToString()), Color.cyan);
            }
        }
    }
}
