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
        internal InteractableStorage Container { get; set; }
        private Transform Transform { get; set; }
        internal UnturnedPlayer Player { get; set; }

        internal byte[] State { get; set; }
        internal byte ItemCount { get; set; }
        internal bool WasOpen { get; set; }
        internal string ContainerName { get; set; }
        internal ushort AssetID { get; set; }
        internal byte ContainerVersion { get; set; }

        internal ContainerManager(UnturnedPlayer player)
        {
            State = new byte[] { };
            ItemCount = 0;
            Container = null;
            WasOpen = false;
            ContainerName = string.Empty;
            ContainerVersion = BarricadeManager.SAVEDATA_VERSION;
            Player = player;
        }

        internal bool SetContainer(ushort assetID, byte[] state, UnturnedPlayer player, string containerName, byte itemCount, byte containerVersion)
        {
            Asset asset = Assets.find(EAssetType.ITEM, assetID);
            bool shouldUpdate = false;
            if (asset == null || (asset is ItemBarricadeAsset && ((ItemBarricadeAsset)asset).build != EBuild.STORAGE))
                return false;
            else
            {
                State = state;
                ItemCount = itemCount;
                ContainerName = containerName;
                AssetID = assetID;
                ContainerVersion = containerVersion;
                Player = player;
                try
                {
                    // Run update, if the container version is less than the barricade manager version.
                    if (ContainerVersion < BarricadeManager.SAVEDATA_VERSION)
                    {
                        shouldUpdate = true;
                        BarricadeManager.version = containerVersion;
                        Logger.Log("Updating container.");
                    }
                    Transform = BarricadeTool.getBarricade(null, 100, Vector3.zero, new Quaternion(), AssetID, State);
                    Container = Transform.GetComponent<InteractableStorage>();
                    Container.transform.position = Vector3.zero;
                    Container.onStateRebuilt = new InteractableStorage.RebuiltStateHandler(SaveState);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error setting container.");
                    Container = null;
                    return false;
                }
                finally
                {
                    if (shouldUpdate)
                    {
                        BarricadeManager.version = BarricadeManager.SAVEDATA_VERSION;
                        if (Container != null)
                        {
                            ContainerVersion = BarricadeManager.SAVEDATA_VERSION;
                            Container.rebuildState();
                        }
                    }
                }
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
            Container.opener = Player.Player;
            Player.Inventory.isStoring = true;
            WasOpen = true;
            Player.Inventory.storage = Container;
            Player.Inventory.updateItems(PlayerInventory.STORAGE, Container.items);
            Player.Inventory.sendStorage();
            UnturnedChat.Say(Player, VirtualStorage.Instance.Translate("opening_container", ContainerName), Color.cyan);
        }

        private void SaveState(InteractableStorage storage, byte[] state, int size)
        {
            if (storage.transform == Container.transform)
            {
                ItemCount = Container.items.getItemCount();
                State = new byte[size];
                Array.Copy(state, State, size);
            }
        }

        internal void Break()
        {
            Container.transform.position = Player.IsInVehicle ? Player.CurrentVehicle.transform.position : Player.Position;
            Container.ManualOnDestroy();
            if (VirtualStorage.Containers.ContainsKey(Player.CSteamID) && VirtualStorage.Containers[Player.CSteamID].ContainerName == ContainerName)
                VirtualStorage.Containers.RemoveContainer(Player.CSteamID);
            else
            {
                Container.transform.position = Vector3.zero;
                UnityEngine.Object.Destroy(Container.transform.gameObject);
            }
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
                Player.CurrentVehicle.grantTrunkAccess(Player.Player);
            }
            WasOpen = false;
        }
    }
}