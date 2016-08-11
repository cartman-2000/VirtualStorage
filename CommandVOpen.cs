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
    class CommandVOpen : IRocketCommand
    {
        internal static readonly string syntax = "";
        internal static readonly string help = "Opens active container.";
        public List<string> Aliases
        {
            get { return new List<string>() { "vo" }; }
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
            get { return "vopen"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vopen" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            PVirtStorage pComponent = player.GetComponent<PVirtStorage>();
            string DefaultContainer = VirtualStorage.Database.GetDefaultContainer(player.CSteamID);
            ContainerManager cData = null;
            if (string.IsNullOrEmpty(DefaultContainer))
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("no_default_set"), Color.red);
                return;
            }
            if (VirtualStorage.Containers.ContainsKey(player.CSteamID))
            {
                cData = VirtualStorage.Containers[player.CSteamID];
                if (VirtualStorage.Containers[player.CSteamID].ContainerName == DefaultContainer)
                {
                    cData = VirtualStorage.Containers[player.CSteamID];
                    pComponent.cData = cData;
                    cData.Open();
                    return;
                }
            }
            else
            {
                if (!VirtualStorage.Containers.ContainsKey(player.CSteamID))
                    cData = new ContainerManager(player);
            }

            object[] cInfo = VirtualStorage.Database.GetContainerData(player.CSteamID, DefaultContainer);
            if (cInfo == null)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("open_data_not_found"), Color.red);
                return;
            }
            if (!cData.SetContainer((ushort)cInfo[0], (byte[])cInfo[1], player, (string)cInfo[2], (byte)cInfo[3], (byte)cInfo[4]))
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("open_invalid"), Color.red);
            }
            if (VirtualStorage.TryCharge(caller, VirtualStorage.Instance.Configuration.Instance.OpenChargeCost, "vs.overrideopencost"))
            {
                if (!VirtualStorage.Containers.ContainsKey(player.CSteamID))
                    VirtualStorage.Containers.Add(player.CSteamID, cData);
                if (!caller.HasPermission("vs.overrideopencost"))
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("open_charged", Math.Round(VirtualStorage.Instance.Configuration.Instance.OpenChargeCost, 2), fr34kyn01535.Uconomy.Uconomy.Instance.Configuration.Instance.MoneyName), Color.cyan);
                pComponent.cData = cData;
                cData.Open();
            }
            else
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("not_enough_credits", fr34kyn01535.Uconomy.Uconomy.Instance.Configuration.Instance.MoneyName), Color.red);
            }
        }
    }
}
