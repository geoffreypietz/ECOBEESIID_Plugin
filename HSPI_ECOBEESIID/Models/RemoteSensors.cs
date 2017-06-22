using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_ECOBEESIID.Models
{
    [DataContract]
    class RemoteSensors
    {
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "type")]
        public string type { get; set; }
        [DataMember(Name = "inUse")]
        public bool inUse { get; set; }
        [DataMember(Name = "capability")]
        public List<SensorCapabilities> capabilityList { get; set; }

    }

    class SensorCapabilities
    {
        [DataMember(Name = "id")]
        public string id { get; set; }
        [DataMember(Name = "type")]
        public string type { get; set; }
        [DataMember(Name = "value")]
        public string value { get; set; }
    }
}
