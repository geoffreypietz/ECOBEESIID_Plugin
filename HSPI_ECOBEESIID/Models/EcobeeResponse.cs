using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_Ecobee_Thermostat_Plugin.Models
{
    [DataContract]
    class EcobeeStatus
    {
        [DataMember(Name = "code")]
        public int code { get; set; }
        [DataMember(Name = "message")]
        public string message { get; set; }
    }

    [DataContract]
    class EcobeeResponse
    {
        [DataMember(Name = "status")]
        public EcobeeStatus status { get; set; }
    }
}
