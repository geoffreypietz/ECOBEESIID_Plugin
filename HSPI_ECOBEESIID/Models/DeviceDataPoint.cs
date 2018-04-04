using Scheduler.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_ECOBEESIID.Models
{
    class DeviceDataPoint
    {
        public int dvRef { get; set; }
        public DeviceClass device { get; set; }

        public DeviceDataPoint(int dvRef, DeviceClass device)
        {
            this.dvRef = dvRef;
            this.device = device;
        }
    }
}
