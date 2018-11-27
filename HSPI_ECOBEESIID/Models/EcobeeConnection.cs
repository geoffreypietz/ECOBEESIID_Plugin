using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;

namespace HSPI_Ecobee_Thermostat_Plugin.Models
{
    class EcobeeConnection : IDisposable
    {
        private const string apiKey = "WgZrBztZwVGDd80HMF1uBFQEuoSCd5lX";
        public string refresh_Token;
        public string access_Token;
        public string ecobeePin;
        public string code;
        public string units { get; set; }

        public EcobeeConnection()
        {
            string json = Util.hs.GetINISetting("ECOBEE", "login", "", Util.IFACE_NAME + ".ini");
            using (Login Login = getLoginInfo(json))
            {              
                refresh_Token = Login?.refresh_token;
                access_Token = Login?.access_token;
                ecobeePin = Login?.ecobeePin;
                code = Login?.code; 
            }
        }

        private RestRequest getRequestGetOrPut(Method method, string json)
        {
            var request = new RestRequest(method);
            request.RequestFormat = DataFormat.Json;
            if (json != null)
            {
                request.AddParameter("application/json", json, ParameterType.RequestBody);
            }
            return request;
        }
        
        public static Login getLoginInfo(string json)
        {
            return JsonConvert.DeserializeObject<Login>(json);
        }
        private void saveLogin()
        {
            using (var login = new Login(refresh_Token, access_Token, ecobeePin, code))
            {
                string json = JsonConvert.SerializeObject(login);
                Util.hs.SaveINISetting("ECOBEE", "login", json, Util.IFACE_NAME + ".ini");
            }
        }

        public IRestResponse sendHTTPRequest(Method method, string urlParams, string json)
        {
            var client = new RestClient("https://api.ecobee.com/" + urlParams);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            client.FollowRedirects = false;
            var request = getRequestGetOrPut(method, null);
            if (urlParams.Contains("thermostat"))
            {
                request.AddHeader("authorization", "Bearer " + access_Token);
                request.AddHeader("content-type", "application/json");
            }
            IRestResponse initial_response = client.Execute(request);

            return initial_response;
        }


        public bool refreshToken()
        {
            string urlParams;
            try
            {
                 urlParams = "token?grant_type=refresh_token&refresh_token=" + refresh_Token + "&client_id=" + apiKey;
            }
            catch
            {
                string json = Util.hs.GetINISetting("ECOBEE", "login", "", Util.IFACE_NAME + ".ini");
                using (Login Login = getLoginInfo(json))
                {
                    refresh_Token = Login?.refresh_token;
                    access_Token = Login?.access_token;
                    ecobeePin = Login?.ecobeePin;
                    code = Login?.code;
                }
                urlParams = "token?grant_type=refresh_token&refresh_token=" + refresh_Token + "&client_id=" + apiKey;

            }
            
            IRestResponse response = sendHTTPRequest(Method.POST, urlParams, null);

            LogDebug(urlParams, response);
            using (Login login = JsonConvert.DeserializeObject<Login>(response.Content))
            {
                if (login.access_token != null)
                {
                    access_Token = login.access_token;
                    refresh_Token = login.refresh_token;
                    saveLogin();
                    return true;
                }
            }

            if (200 <= ((int)response.StatusCode) && ((int)response.StatusCode) < 400) {
                Util.Log(response.StatusDescription, Util.LogType.LOG_TYPE_ERROR);

            }
            else {
                Util.Log(response.StatusDescription+"  "+response.ErrorMessage, Util.LogType.LOG_TYPE_ERROR);
            } 
          
            Util.Log("Invalid Refresh Token-Try resetting the token on the Options Page", Util.LogType.LOG_TYPE_ERROR);
            return false;


        }



        public bool retrieveAccessToken(bool pin)
        {
            string urlParams;
            try
            {
                if (pin)
                {
                    urlParams = "token?grant_type=ecobeePin&code=" + code + "&client_id=" + apiKey;
                    //Console.WriteLine(urlParams);
                }
                else
                {
                    if (refresh_Token == null)
                    {
                        string json = Util.hs.GetINISetting("ECOBEE", "login", "", Util.IFACE_NAME + ".ini");
                        using (Login Login = getLoginInfo(json))
                        {
                            refresh_Token = Login?.refresh_token;
                            access_Token = Login?.access_token;
                            ecobeePin = Login?.ecobeePin;
                            code = Login?.code;
                        }
                    }
                        urlParams = "token?grant_type=refresh_token&code=" + refresh_Token + "&client_id=" + apiKey;
                    
                }
               
                IRestResponse response = sendHTTPRequest(Method.POST, urlParams, null);
                using (Login login = JsonConvert.DeserializeObject<Login>(response.Content))
                {
                    if (login.access_token != null)
                    {
                        access_Token = login.access_token;
                        refresh_Token = login.refresh_token;
                        saveLogin();
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
        public string retrievePin()
        {
            string urlParams = "authorize?response_type=ecobeePin&client_id=" + apiKey + "&scope=smartWrite";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            IRestResponse response = sendHTTPRequest(Method.GET, urlParams, null);
            EcobeePin pin = JsonConvert.DeserializeObject<EcobeePin>(response.Content);
            if (pin.ecobeePin != null)
            {
                ecobeePin = pin.ecobeePin;
                code = pin.code;
                saveLogin();
            }
            return ecobeePin;
        }
        public EcobeeData getEcobeeData()
        {
            string urlParams = "1/thermostat?json={\"selection\":{\"selectionType\":\"registered\",\"selectionMatch\":\"\",\"includeEvents\":\"true\",\"includeSettings\":\"true\",\"includeRuntime\":\"true\",\"includeSensors\":\"true\"}}";
            IRestResponse response = sendHTTPRequest(Method.GET, urlParams, null);



            return JsonConvert.DeserializeObject<EcobeeData>(response.Content);
        }

        public string ecobeeMessage()
        {
            string urlParams = "1/thermostat?json={\"selection\":{\"selectionType\":\"registered\",\"selectionMatch\":\"\",\"includeEvents\":\"true\",\"includeSettings\":\"true\",\"includeRuntime\":\"true\",\"includeSensors\":\"true\"}}";
            IRestResponse response = sendHTTPRequest(Method.GET, urlParams, null);


            return response.StatusDescription;

        }
        public void LogDebug(String request, IRestResponse initial_response)
        {
            /*
            Util.Log(request, Util.LogType.LOG_TYPE_DEBUG);
            Util.Log(initial_response.Content, Util.LogType.LOG_TYPE_DEBUG);*/
        }
        public void LogDebug(RestRequest request, IRestResponse initial_response) {
            LogDebug(Util.RequestToString(request), initial_response);
           
        }

        public EcobeeResponse setApiJson(string json)
        {
            var client = new RestClient("https://api.ecobee.com/1/thermostat?format=json"); 
            client.FollowRedirects = false;
            var request = getRequestGetOrPut(Method.POST, json);
            request.AddHeader("authorization", "Bearer " + access_Token);
            request.AddHeader("content-type", "application/json");


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            IRestResponse initial_response = client.Execute(request);
            LogDebug(request, initial_response);




            return JsonConvert.DeserializeObject<EcobeeResponse>(initial_response.Content);
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EcobeeConnection()
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
