using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_ECOBEESIID.Models
{
    [DataContract]
    class Thermostat
    {
        [DataMember(Name = "identifier")]
        public string identifier { get; set; }
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "brand")]
        public string brand { get; set; }
        [DataMember(Name = "settings")]
        public ThermostatSettings settings { get; set; }
        [DataMember(Name = "runtime")]
        public ThermostatRuntime runtime { get; set; }
        [DataMember(Name = "events")]
        public List<ThermostatEvent> events { get; set; }
        [DataMember(Name = "remoteSensors")]
        public List<RemoteSensors> remoteSensors { get; set; }
    }
}
