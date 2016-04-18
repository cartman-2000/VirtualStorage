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
    class CommandVName : IRocketCommand
    {
        internal static readonly string syntax = "<\"Container Name\">";
        internal static readonly string help = "Renames the container set to default, to a new name.";
        public List<string> Aliases
        {
            get { return new List<string>(); }
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
            get { return "vname"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vname" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0 || command.Length > 1)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("vname_help"));
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;

            string newName = command[0].Trim().Truncate(60);
            Dictionary<string, object[]> containers = VirtualStorage.Database.GetContainerList(player.CSteamID);
            object[] exists = containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == newName.ToLower());
            string defaultContainer = VirtualStorage.Database.GetDefaultContainer(player.CSteamID);
            if (string.IsNullOrEmpty(defaultContainer) || containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == defaultContainer.ToLower()) == null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("no_default_set"), Color.red);
                return;
            }
            if (exists != null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("matches_owned"), Color.red);
                return;
            }
            VirtualStorage.Database.RenameContainer(player.CSteamID, defaultContainer, newName);
            VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, newName);
            if (VirtualStorage.Containers.ContainsKey(player.CSteamID))
            {
                ContainerManager cData = VirtualStorage.Containers[player.CSteamID];
                cData.ContainerName = newName;
            }
            UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("name_set", newName), Color.cyan);
        }
    }
}
