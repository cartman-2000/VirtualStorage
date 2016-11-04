using System.Xml.Serialization;

namespace VirtualStorage
{
    public class Container
    {
        [XmlAttribute("AssetID")]
        public ushort AssetID { get; set; }
        [XmlAttribute("Name")]
        public string ContainerName { get; set; }
        [XmlAttribute("Price")]
        public decimal Price { get; set; }
        public Container() { }
        public Container(ushort assetID, string containerName, decimal price)
        {
            AssetID = assetID;
            ContainerName = containerName;
            Price = price;
        }
    }
}