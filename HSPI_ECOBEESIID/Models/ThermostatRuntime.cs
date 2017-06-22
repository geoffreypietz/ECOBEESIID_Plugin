using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_ECOBEESIID.Models
{
    [DataContract]
    class ThermostatRuntime
    {
        [DataMember(Name = "actualTemperature")]
        public int actualTemperature { get; set; }
        [DataMember(Name = "actualHumidity")]
        public int actualHumidity { get; set; }
        [DataMember(Name = "desiredHeat")]
        public int desiredHeat { get; set; }
        [DataMember(Name = "desiredCool")]
        public int desiredCool { get; set; }
        [DataMember(Name = "desiredFanMode")]
        public string desiredFanMode { get; set; }
    }
}
