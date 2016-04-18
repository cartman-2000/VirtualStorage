using System;
using Rocket.API;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace VirtualStorage
{
    public class VirtualStorageConfig : IRocketPluginConfiguration
    {
        public string DatabaseAddress = "localhost";
        public ushort DatabasePort = 3306;
        public string DatabaseUserName = "unturned";
        public string DatabasePassword = "password";
        public string DatabaseName = "unturned";
        public string DatabaseTableName = "virtualstorage";
        public int KeepaliveInterval = 10;
        public int MaxContainersPerPlayer = 4;
        public decimal OpenChargeCost = 50.0m;
        public ushort FallbackAssetID = 328;


        [XmlArray("Containers"), XmlArrayItem(ElementName = "Container")]
        public List<Container> Containers = new List<Container>();


        public void LoadDefaults()
        {
            if (Containers.Count == 0)
            {
                Containers = new List<Container>()
                {
                    new Container(368, "Crate", 100.0m),
                    new Container(328, "Locker", 200.0m),
                };
            }
        }

    }
}