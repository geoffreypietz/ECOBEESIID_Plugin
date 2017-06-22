using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_ECOBEESIID.Models
{
    [DataContract]
    class ThermostatSettings
    {
        [DataMember(Name = "hvacMode")]
        public string hvac_mode { get; set; }
        [DataMember(Name = "humidity")]
        public string humidity { get; set; }
        [DataMember(Name = "fanMinOnTime")]
        public int fanMinOnTime { get; set; }
        [DataMember(Name = "useCelsius")]
        public bool useCelsius { get; set; }
        [DataMember(Name = "heatRangeHigh")]
        public int heatRangeHigh { get; set; }
        [DataMember(Name = "heatRangeLow")]
        public int heatRangeLow { get; set; }
        [DataMember(Name = "coolRangeHigh")]
        public int coolRangeHigh { get; set; }
        [DataMember(Name = "coolRangeLow")]
        public int coolRangeLow { get; set; }
    }
}
