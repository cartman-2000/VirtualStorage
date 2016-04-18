using Rocket.Core.Logging;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VirtualStorage
{
    public class PVirtStorage : UnturnedPlayerComponent
    {
        internal ContainerManager cData;

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
                    if (!Player.Inventory.isStoring)
                    {
                        cData.Close();
                        VirtualStorage.Database.SaveContainerToDB(cData);
                        cData = null;
                    }
                }
            }
        }
    }
}
