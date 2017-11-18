using Rocket.Core.Logging;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualStorage
{
    public class PVirtStorage : UnturnedPlayerComponent
    {
        internal ContainerManager cData;
        private bool isPrimarySeat = false;

        protected override void Load()
        {
            cData = null;
        }
        public void FixedUpdate()
        {
            if (cData != null)
            {
                if (cData.WasOpen)
                {
                    // Force close virtual container if switching from passengers to drivers seats.
                    if (!Player.Inventory.isStoring || (Player.IsInVehicle && !isPrimarySeat && Player.CurrentVehicle.checkDriver(Player.CSteamID)))
                    {
                        cData.Close();
                        VirtualStorage.Database.SaveContainerToDB(cData);
                        cData = null;
                    }
                }
            }
            if (Player.IsInVehicle)
                isPrimarySeat = Player.CurrentVehicle.checkDriver(Player.CSteamID);
        }
    }
}
