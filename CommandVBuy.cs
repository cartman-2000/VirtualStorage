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
    class CommandVBuy : IRocketCommand
    {
        internal static readonly string syntax = "<list> | <\"List Name\"> <\"Container Name\">";
        internal static readonly string help = "Buys/Creates a Dynamic container.";
        public List<string> Aliases
        {
            get { return new List<string>() { "vb" }; }
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
            get { return "vbuy"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "vbuy" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0 || (command[0].Trim().ToLower() == "list" && command.Length > 1) || command.Length > 2)
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("vbuy_help"));
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;

            Dictionary<string, object[]> containers = VirtualStorage.Database.GetContainerList(player.CSteamID);
            if (command[0].Trim().ToLower() == "list")
            {
                UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("number_to_buy", VirtualStorage.Database.ConfigContainers.Count), Color.cyan);
                foreach (KeyValuePair<ushort, Container> container in VirtualStorage.Database.ConfigContainers)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_list_entry", container.Value.ContainerName, container.Value.Price), Color.yellow);
                }
            }
            else
            {
                if (command.Length != 2)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_no_name"), Color.red);
                    return;
                }
                string cName = command[1].Trim().Truncate(60);
                if (cName == string.Empty)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_no_name"), Color.red);
                    return;
                }
                if (containers.Values.FirstOrDefault(contents => contents[2].ToString().ToLower() == cName.ToLower()) != null)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("matches_owned"), Color.red);
                    return;
                }
                Container cInfo = VirtualStorage.Database.ConfigContainers.Values.FirstOrDefault(content => content.ContainerName.ToLower() == command[0].Trim().ToLower());
                if (cInfo == null)
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_not_in_list"), Color.red);
                    return;
                }
                if (containers.Count >= VirtualStorage.Instance.Configuration.Instance.MaxContainersPerPlayer && !caller.HasPermission("vs.overridemax"))
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_max_reached", VirtualStorage.Instance.Configuration.Instance.MaxContainersPerPlayer), Color.red);
                    return;
                }
                if (VirtualStorage.TryCharge(caller, cInfo.Price, "vs.overridebuycharge"))
                {
                    ContainerManager cData = new ContainerManager(player);
                    cData.ContainerName = cName;
                    cData.AssetID = cInfo.AssetID;
                    VirtualStorage.Database.SaveContainerToDB(cData);
                    if (!caller.HasPermission("vs.overridebuycharge"))
                        UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_charged", cInfo.ContainerName, cName, Math.Round(cInfo.Price, 2), fr34kyn01535.Uconomy.Uconomy.Instance.Configuration.Instance.MoneyName), Color.cyan);
                    else
                        UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_charge_override", cInfo.ContainerName, cName), Color.cyan);

                    if (string.IsNullOrEmpty(VirtualStorage.Database.GetDefaultContainer(player.CSteamID)))
                    {
                        VirtualStorage.Database.SaveDefaultContainer(player.CSteamID, cName);
                        UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("buy_default_set", cName, Color.cyan));
                    }
                }
                else
                {
                    UnturnedChat.Say(caller, VirtualStorage.Instance.Translate("not_enough_credits", fr34kyn01535.Uconomy.Uconomy.Instance.Configuration.Instance.MoneyName), Color.red);
                }
            }
        }
    }
}
