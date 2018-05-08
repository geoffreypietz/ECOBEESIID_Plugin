using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_Ecobee_Thermostat_Plugin.Models
{
    [DataContract]
    class ThermostatEvent
    {
        [DataMember(Name = "type")]
        public string type { get; set; }
        [DataMember(Name = "isOccupied")]
        public bool isOccupied { get; set; }
        [DataMember(Name = "coolHoldTemp")]
        public int coolHoldTemp { get; set; }
        [DataMember(Name = "heatHoldTemp")]
        public int heatHoldTemp { get; set; }
        [DataMember(Name = "isCoolOff")]
        public bool isCoolOff { get; set; }
        [DataMember(Name = "isHeatOff")]
        public bool isHeatOff { get; set; }
        [DataMember(Name = "useCelsius")]
        public bool useCelsius { get; set; }
        [DataMember(Name = "running")]
        public bool running { get; set; }
        [DataMember(Name = "fan")]
        public string fan { get; set; }
    }
}
