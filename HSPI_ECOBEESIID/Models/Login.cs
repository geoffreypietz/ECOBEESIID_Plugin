using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_Ecobee_Thermostat_Plugin.Models
{
    [DataContract]
    class Login : IDisposable
    {
        [DataMember(Name = "refresh_token")]
        public string refresh_token { get; set; }
        [DataMember(Name = "access_token")]
        public string access_token { get; set; }
        [DataMember(Name = "ecobeePin")]
        public string ecobeePin { get; set; }
        [DataMember(Name = "code")]
        public string code { get; set; }


        public Login(string refresh_token, string access_token, string ecobeePin, string code)
        {
            this.refresh_token = refresh_token;
            this.access_token = access_token;
            this.ecobeePin = ecobeePin;
            this.code = code;
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Login()
        {
            Debug.Assert(Disposed, "WARNING: Object finalized without being disposed!");
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                }

                DisposeUnmanagedResources();
                Disposed = true;
            }
        }

        protected virtual void DisposeManagedResources() { }
        protected virtual void DisposeUnmanagedResources() { }
    }
}
