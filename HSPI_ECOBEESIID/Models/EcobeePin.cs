using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_Ecobee_Thermostat_Plugin.Models
{
    [DataContract]
    class EcobeePin
    {
        [DataMember(Name = "ecobeePin")]
        public string ecobeePin { get; set; }
        [DataMember(Name = "code")]
        public string code { get; set; }
    }
}
