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
                    if (login.refresh_token != null)
                    {
                        refresh_Token = login.refresh_token;
                    }
                    
                    saveLogin();
                    return true;
                }
            }

            //only get here if we don't make it to return. So the access_token is missing. or the JSON did deserialize the object as indended
            if (200 <= ((int)response.StatusCode) && ((int)response.StatusCode) < 400) {

                string message = "";
                if (response.ErrorMessage != null)
                {
                    message = response.ErrorMessage;
                }

                switch ((int)response.StatusCode){

                    case 0:
                        Util.Log(response.StatusDescription + " " + response.StatusCode+" "+ message + " the request was successful but the access token was not parsed from the response correctly", Util.LogType.LOG_TYPE_ERROR);
                        break;

                    case 1:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Authentication failed. Invalid credentials supplied to the registration request, or invalid token. Request registration again.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 2:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Not authorized. Attempted to access resources which user is not authorized for. Ensure the thermostat identifiers requested are correct.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 3:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Processing error. General catch-all error for a number of internal errors. Additional info may be provided in the message. Check your request. Contact support if persists.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 4:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Serialization error. An internal error mapping data to or from the API transmission format. Contact support.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 5:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Invalid request format. An error mapping the request data to internal data objects. Ensure that the properties being sent match properties in the specification.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 6:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Too many thermostat in selection match criteria. Too many identifiers are specified in the Selecton.selectionMatch property. Current limit is 25 per request.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 7:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Validation error. The update request contained values out of range or too large for the field being updated. See the additional message information as to what caused the validation failure.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 8:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Invalid function. The \"type\" property of the function does not match an available function. Check your request parameters.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 9:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Invalid selection. The Selection.selectionType property contains an invalid value.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 10:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Invalid page. The page requested in the request is invalid. Occurs if the page is less than 1 or more than the number of available pages for the request.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 11:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Function error. An error occurred processing a function. Ensure required properties are provided.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 12:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Post not supported for request. The request URL does not support POST.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 13:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Get not supported for request. The request URL does not support GET.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 14:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + "	Authentication token has expired. Refresh your tokens. ", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 15:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + " Duplicate data violation.", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    case 16:
                        Util.Log(response.StatusDescription + " " + response.StatusCode + " " + message + "	Invalid token. Token has been deauthorized by user. You must re-request authorization. ", Util.LogType.LOG_TYPE_ERROR);
                        break;
                    default:
                        Util.Log(response.StatusDescription + " " + response.StatusCode, Util.LogType.LOG_TYPE_ERROR);
                        Util.Log("Invalid Refresh Token-Try resetting the token on the Options Page", Util.LogType.LOG_TYPE_ERROR);
                        break;
                }


               

            }
            else {
                Util.Log(response.StatusDescription+"  "+response.ErrorMessage, Util.LogType.LOG_TYPE_ERROR);
            } 
          
          
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
