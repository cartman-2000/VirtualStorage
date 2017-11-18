using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Reflection;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace VirtualStorage
{
    public class ContainerManager
    {
        private static readonly byte CurrentPluginContainerVersion = 11;

        private Transform Transform { get; set; }
        private InteractableStorage Container { get; set; }
        private ItemBarricadeAsset ItemAsset { get; set; }
        internal UnturnedPlayer Player { get; set; }

        internal byte[] State { get; set; }
        internal int StateSize { get; set; }
        internal byte ItemCount { get; set; }
        internal bool WasOpen { get; set; }
        internal string ContainerName { get; set; }
        internal ushort AssetID { get; set; }
        internal byte ContainerVersion { get; set; }

        internal ContainerManager(UnturnedPlayer player)
        {
            State = new byte[] { };
            StateSize = 0;
            ItemCount = 0;
            Container = null;
            WasOpen = false;
            ContainerName = string.Empty;
            ContainerVersion = CurrentPluginContainerVersion;
            Player = player;
        }

        private bool UpdateContainer()
        {
            // here to perform State updates to the data so it'll load properly. Will also stop containers from loading if the container version isn't less than the CurrentPluginVersion set here, it's a protection measure so that container contents don't get corrupted with potentially mismatched packing and unpacking procedures.
            if (ContainerVersion < CurrentPluginContainerVersion)
            {
                try
                {
                    BarricadeManager.version = ContainerVersion;
                    Logger.Log("run update.");
                    Transform = BarricadeTool.getBarricade(Player.Player.transform, 100, Player.Position, new Quaternion(), AssetID, State);
                    Container = Transform.GetComponent<InteractableStorage>();
                    SaveState();
                    ContainerVersion = BarricadeManager.SAVEDATA_VERSION;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error updating the container.");
                    return false;
                }
                finally
                {
                    BarricadeManager.version = BarricadeManager.SAVEDATA_VERSION;
                }
            }
            else
            {
                return false;
            }
        }

        internal bool SetContainer(ushort assetID, byte[] state, UnturnedPlayer player, string containerName, byte itemCount, byte containerVersion)
        {
            ItemAsset = ((ItemBarricadeAsset)Assets.find(EAssetType.ITEM, assetID));
            if (ItemAsset == null || ItemAsset.build != EBuild.STORAGE)
                return false;
            else
            {
                State = state;
                ItemCount = itemCount;
                ContainerName = containerName;
                AssetID = assetID;
                ContainerVersion = containerVersion;
                Player = player;
                if (ContainerVersion < BarricadeManager.SAVEDATA_VERSION)
                {
                    if (!UpdateContainer())
                    {
                        return false;
                    }
                }
                Transform = BarricadeTool.getBarricade(Player.Player.transform, 100, Player.Position, new Quaternion(), AssetID, State);
                Container = Transform.GetComponent<InteractableStorage>();
                return true;
            }
        }

        internal void Open()
        {
            if (Container == null)
                return;
            if (Player.Inventory.isStorageTrunk || Player.Inventory.isStoring)
            {
                Player.Inventory.isStorageTrunk = false;
                Player.Inventory.isStoring = false;
                Player.Inventory.storage = null;
                Player.Inventory.updateItems(PlayerInventory.STORAGE, null);
                Player.Inventory.sendStorage();
            }
            Transform.localPosition = Player.Position;
            Container.opener = Player.Player;
            Player.Inventory.isStoring = true;
            WasOpen = true;
            Container.items.onStateUpdated = new StateUpdated(SaveState);
            Player.Inventory.storage = Container;
            Player.Inventory.updateItems(PlayerInventory.STORAGE, Container.items);
            Player.Inventory.sendStorage();
            UnturnedChat.Say(Player, VirtualStorage.Instance.Translate("opening_container", ContainerName), Color.cyan);
        }

        internal void SaveState()
        {
            SteamPacker.openWrite(0);
            ItemCount = Container.items.getItemCount();
            SteamPacker.write(Player.CSteamID, Player.SteamGroupID, ItemCount);
            for (byte i = 0; i < ItemCount; i++)
            {
                ItemJar I = (ItemJar)Container.items.getItem(i);
                SteamPacker.write(I.x, I.y, I.rot, I.item.id, I.item.amount, I.item.durability, I.item.metadata);
            }
            if (Container.isDisplay)
            {
                SteamPacker.write(Container.displaySkin, Container.displayMythic, Container.rot_comp);
            }
            int Size = 0;
            byte[] tmp = SteamPacker.closeWrite(out Size);
            StateSize = Size;
            State = new byte[StateSize];
            Array.Copy(tmp, State, StateSize);
        }

        internal void Break()
        {
            Transform.localPosition = Player.IsInVehicle ? Player.CurrentVehicle.transform.position : Player.Position;
            for (byte b = 0; b < ItemCount; b += 1)
            {
                ItemJar item = Container.items.getItem(b);
                ItemManager.dropItem(item.item, Transform.localPosition, false, true, true);
            }
            Container.items.clear();
            Close();
            Container = null;
            UnturnedChat.Say(Player, VirtualStorage.Instance.Translate("removing_container", ContainerName, ItemCount), Color.cyan);
        }

        internal void Close()
        {
            if (Container.opener != null)
            {
                Container.opener.inventory.isStorageTrunk = false;
                Container.opener.inventory.isStoring = false;
                Container.opener.inventory.storage = null;
                Container.opener.inventory.updateItems(PlayerInventory.STORAGE, null);
                Container.opener.inventory.sendStorage();
                Container.opener = null;
            }
            
            if (Player.IsInVehicle && Player.CurrentVehicle.checkDriver(Player.CSteamID))
            {
                // Reopen the trunk in the car, if you're currently in the drivers seat after you close the virtual storage.
                Player.CurrentVehicle.GetType().InvokeMember("grantTrunkAccess", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, Player.CurrentVehicle, new object[] { Player.Player });
            }
            WasOpen = false;
        }
    }
}