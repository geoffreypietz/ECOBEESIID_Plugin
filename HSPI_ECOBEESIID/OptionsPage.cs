using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Web;
using Scheduler;
using System.Xml.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using HSPI_Ecobee_Thermostat_Plugin.Models;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Web.UI;

namespace HSPI_Ecobee_Thermostat_Plugin
{
    public class OptionsPage : PageBuilderAndMenu.clsPageBuilder
    {

        public string access_token;
        public string pin_code;
        public string expires_in;
        public string unitType;
        public string logging_type;


        public OptionsPage(string pagename) : base(pagename)
        {

        }

        // This method is called whenever a Options page control is used (buttons, textboxes, etc.)
        public override string postBackProc(string page, string data, string user, int userRights)
        {

            System.Collections.Specialized.NameValueCollection parts = null;
            parts = HttpUtility.ParseQueryString(data);

            using (var ecobee = new EcobeeConnection())
            {

                string id = parts["id"];
                Console.WriteLine(data);

                if (id == "pin_get")
                {
                    pin_code = ecobee.retrievePin();
                    divToUpdate.Add("pin_region", pin_code);
                }

                if (id == "access_button")
                {
                    var success = ecobee.retrieveAccessToken(true);

                    if (success)
                    {
                        pageCommands.Add("popmessage", "Successfully reset Access Token");
                    }
                    else
                    {
                        pageCommands.Add("popmessage", "Failed to reset Access Token");
                    }
                }
                if (id == "devices_button")
                {
                    Util.Create_Devices(ecobee);

                }

            }
            return base.postBackProc(page, data, user, userRights);
        }

        public string GetPagePlugin(string pageName, string user, int userRights, string queryString)
        {
            StringBuilder pluginSB = new StringBuilder();
            OptionsPage page = this;

            try
            {
                page.reset();

                // handle queries with special data
                /*System.Collections.Specialized.NameValueCollection parts = null;
                if ((!string.IsNullOrEmpty(queryString)))
                {
                    parts = HttpUtility.ParseQueryString(queryString);
                }
                if (parts != null)
                {
                    if (parts["myslide1"] == "myslide_name_open")
                    {
                        // handle a get for tab content
                        string name = parts["name"];
                        return ("<table><tr><td>cell1</td><td>cell2</td></tr><tr><td>cell row 2</td><td>cell 2 row 2</td></tr></table>");
                        //Return ("<div><b>content data for tab</b><br><b>content data for tab</b><br><b>content data for tab</b><br><b>content data for tab</b><br><b>content data for tab</b><br><b>content data for tab</b><br></div>")
                    }
                    if (parts["myslide1"] == "myslide_name_close")
                    {
                        return "";
                    }
                }*/

                
                //pluginSB.Append("<link rel = 'stylesheet' href = 'hspi_ecobeesiid/css/style.css' type = 'text/css' /><br>");
                page.AddHeader(pluginSB.ToString());



                //page.RefreshIntervalMilliSeconds = 5000
                // handler for our status div
                //stb.Append(page.AddAjaxHandler("/devicestatus?ref=3576", "stat"))
                pluginSB.Append(this.AddAjaxHandlerPost("action=updatetime", this.PageName));



                // page body starts here

                this.AddHeader(Util.hs.GetPageHeader(pageName, Util.IFACE_NAME, "", "", false, true));


                //pluginSB.Append(DivStart("pluginpage", ""));
                //Dim dv As DeviceClass = GetDeviceByRef(3576)
                //Dim CS As CAPIStatus
                //CS = dv.GetStatus

                // Status/Options Tabs

                // Options Tab

                pluginSB.Append(PageBuilderAndMenu.clsPageBuilder.FormStart("myform1", "testpage", "post"));
                pluginSB.AppendLine("<table class='full_width_table' cellspacing='0' width='100%' >");

                // Ecobee API Access Token
                pluginSB.Append("<tr><td class='tableheader' colspan='2'>Reset Ecobee API Access Token</td></tr>");

                pluginSB.Append("<tr><td class='tablecell'>Open the link in a new tab and sign in</td>");
                pluginSB.Append("<td class='tablecell'><a href='https://www.ecobee.com/home/ecobeeLogin.jsp'>Ecobee Login</a></td>");
                pluginSB.Append("</td></tr>");

                pluginSB.Append("<tr><td class='tablecell'>Hit the Pin-Code Retrieval button to obtain a new pin code</td>");
                pluginSB.Append("<td class='tablecell'>");
                pluginSB = BuildLinkButton(pluginSB, "pin_get", "Pin-Code Retrieval", "");
                pluginSB.Append("</td></tr>");

                pluginSB.Append("<tr><td class='tablecell'>Copy the pin-code, add an application on the MyApps page within the Ecobee portal and manually enter the pin code</td>");
                pluginSB.Append("<td class='tablecell'>");

                clsJQuery.jqScrollingRegion sr = new clsJQuery.jqScrollingRegion("pin_region");
                sr.className = "";
                sr.AddStyle("height:20px;overflow:auto;width: 35px;background: #FFFFFF;");
                sr.content = "[pin]";
                pluginSB.Append(sr.Build());

                //pluginSB = BuildTextBox(pluginSB, "pin_code", "Pin-Code", "Pin-Code", "", 200);
                pluginSB.Append("</td></tr>");
             
                pluginSB.Append("<tr><td class='tablecell'>After validating the pin code, hit the Reset/Retrieve Access-Token button to reset your access token</td>");
                pluginSB.Append("<td class='tablecell'>");
                pluginSB = BuildLinkButton(pluginSB, "access_button", "Reset/Retrieve Access-Token", "");
      
                pluginSB.Append("</td></tr>");

                pluginSB.Append("<tr><td class='tablecell'>Finally, hit the Create Homeseer devices button to add the Ecobee thermostat devices to Homeseer</td>");
                pluginSB.Append("<td class='tablecell'>");
                pluginSB = BuildLinkButton(pluginSB, "devices_button", "Create Homeseer devices", "");

                pluginSB.Append("</td></tr>");

                pluginSB.Append("</td></tr>");

                pluginSB.Append("</table><br>");
               
                pluginSB.Append(PageBuilderAndMenu.clsPageBuilder.FormEnd());

            }
            catch (Exception ex)
            {
                pluginSB.Append("Status/Options error: " + ex.Message);
            }
            pluginSB.Append("<br>");

            //pluginSB.Append(DivEnd());
            page.AddBody(pluginSB.ToString());

            return page.BuildPage();
        }

        // Builds input textboxs for client-id, client-secret, pin-code, access-token
        private StringBuilder BuildTextBox(StringBuilder pluginSB, string name, string prompt, string tooltip, string initial, int width)
        {
            clsJQuery.jqTextBox tokenTextBox = new clsJQuery.jqTextBox(name, "text", initial, this.PageName, 20, false);
            tokenTextBox.promptText = prompt;
            tokenTextBox.toolTip = tooltip;
            tokenTextBox.dialogWidth = width;
            pluginSB.Append(tokenTextBox.Build());

            return pluginSB;
        }

        private StringBuilder BuildHelpButton(StringBuilder pluginSB, string oName, string oTooltip, string oLabel, string oText, string bName, string bLabel, string bUrl)
        {
            clsJQuery.jqOverlay ol = new clsJQuery.jqOverlay(oName, this.PageName, false, "events_overlay");
            ol.toolTip = oTooltip;
            ol.label = oLabel;
            clsJQuery.jqButton button = new clsJQuery.jqButton(bName, bLabel, this.PageName, true);
            button.url = bUrl;

            ol.overlayHTML = PageBuilderAndMenu.clsPageBuilder.FormStart("overlayformm", "testpage", "post");
            ol.overlayHTML += "<div>" + oText + "<br><br>" + button.Build() + "</div>";
            ol.overlayHTML += PageBuilderAndMenu.clsPageBuilder.FormEnd();
            pluginSB.Append(ol.Build());

            return pluginSB;
        }

        private StringBuilder BuildLinkButton(StringBuilder pluginSB, string bName, string bLabel, string bUrl)
        {
            clsJQuery.jqButton button = new clsJQuery.jqButton(bName, bLabel, this.PageName, true);
            //button.url = bUrl;
            pluginSB.Append(button.Build());

            return pluginSB;
        }
    }

}
