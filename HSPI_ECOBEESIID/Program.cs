using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Scheduler;
using HomeSeerAPI;

using HSCF.Communication.Scs.Communication.EndPoints.Tcp;
using HSCF.Communication.ScsServices.Client;
using HSCF.Communication.ScsServices.Service;

namespace HSPI_ECOBEESIID
{
    class Program
    {
        private static IScsServiceClient<IHSApplication> withEventsField_client;
        private static IAppCallbackAPI callback;
        public static IScsServiceClient<IHSApplication> client
        {
            get { return withEventsField_client; }
            set
            {
                if (withEventsField_client != null)
                {
                    withEventsField_client.Disconnected -= client_Disconnected;
                }
                withEventsField_client = value;
                if (withEventsField_client != null)
                {
                    withEventsField_client.Disconnected += client_Disconnected;
                }
            }
        }

        static IScsServiceClient<IAppCallbackAPI> clientCallback;
        private static HomeSeerAPI.IHSApplication host;
        // real plugin functions, user supplied
        public static HSPI plugin = new HSPI();


        public static void Main(string[] args)
        {
            string serverIp = "127.0.0.1";

            string serverCmd = null;
            foreach (string serverCmd_loopVariable in args)
            {
                serverCmd = serverCmd_loopVariable;
                string[] ch = new string[1];
                ch[0] = "=";
                string[] parts = serverCmd.Split(ch, StringSplitOptions.None);
                switch (parts[0].ToLower())
                {
                    case "server":
                        serverIp = parts[1];
                        break;
                    case "instance":
                        try
                        {
                            Util.Instance = parts[1];
                        }
                        catch (Exception)
                        {
                            Util.Instance = "";
                        }
                        break;
                }
            }

            Console.WriteLine("Plugin: " + Util.IFACE_NAME + " Instance: " + Util.Instance + " starting...");
            Console.WriteLine("Connecting to server at " + serverIp + "...");
            client = ScsServiceClientBuilder.CreateClient<IHSApplication>(new ScsTcpEndPoint(serverIp, 10400), plugin);
            clientCallback = ScsServiceClientBuilder.CreateClient<IAppCallbackAPI>(new ScsTcpEndPoint(serverIp, 10400), plugin);
            int Attempts = 1;
        TryAgain:

            try
            {
                client.Connect();
                clientCallback.Connect();

                double APIVersion = 0;

                try
                {
                    host = client.ServiceProxy;
                    APIVersion = host.APIVersion;
                    // will cause an error if not really connected
                    Console.WriteLine("Host API Version: " + APIVersion.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error getting API version from host object: " + ex.Message + "->" + ex.StackTrace);
                    //Return
                }

                try
                {
                    callback = clientCallback.ServiceProxy;
                    APIVersion = callback.APIVersion;
                    // will cause an error if not really connected
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error getting API version from callback object: " + ex.Message + "->" + ex.StackTrace);
                    return;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot connect attempt " + Attempts.ToString() + ": " + ex.Message);
                if (ex.Message.ToLower().Contains("timeout occurred."))
                {
                    Attempts += 1;
                    if (Attempts < 6)
                        goto TryAgain;
                }

                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }
                if (clientCallback != null)
                {
                    clientCallback.Dispose();
                    clientCallback = null;
                }
                wait(4);
                return;
            }

            try
            {
                // create the user object that is the real plugin, accessed from the pluginAPI wrapper
                Util.callback = callback;
                Util.hs = host;
                plugin.OurInstanceFriendlyName = Util.Instance;
                // connect to HS so it can register a callback to us
                host.Connect(Util.IFACE_NAME, Util.Instance);
                Console.WriteLine("Connected, waiting to be initialized...");
                do
                {
                    System.Threading.Thread.Sleep(10);
                } while (client.CommunicationState == HSCF.Communication.Scs.Communication.CommunicationStates.Connected & !HSPI.bShutDown);
                Console.WriteLine("Connection lost, exiting");
                // disconnect from server for good here
                client.Disconnect();
                clientCallback.Disconnect();
                wait(2);
                System.Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot connect(2): " + ex.Message);
                wait(2);
                System.Environment.Exit(0);
                return;
            }

        }

        private static void client_Disconnected(object sender, System.EventArgs e)
        {
            Console.WriteLine("Disconnected from server - client");
        }


        private static void wait(int secs)
        {
            System.Threading.Thread.Sleep(secs * 1000);
        }
    }


}

