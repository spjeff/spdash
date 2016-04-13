using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;
using System.Diagnostics;

namespace SPDash.Features.SPDashFeature
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("019dbfc7-91ae-4a88-9767-c042258eacbb")]
    public class SPDashFeatureEventReceiver : SPFeatureReceiver
    {
        // Uncomment the method below to handle the event raised after a feature has been activated.

        //public override void FeatureActivated(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        //public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised after a feature has been installed.

        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            try
            {
                //active feature for CA web app during install
                //SPDashFeature = beba7f1a-f972-4759-89fe-8777a539a4cb
                //SPDashTimerJobFeature = 753f6c7b-95fb-41de-977f-489689d4902c
                Logger.LogInfo("FeatureInstalled - SPDashFeature beba7f1a-f972-4759-89fe-8777a539a4cb");
                Microsoft.SharePoint.Administration.SPAdministrationWebApplication caApp = SPAdministrationWebApplication.Local;
                string caUrl = caApp.Sites[0].Url;
                string caCmd = string.Format("Start-Sleep 10; Enable-SPFeature 753f6c7b-95fb-41de-977f-489689d4902c -Url " + caUrl + "; Enable-SPFeature beba7f1a-f972-4759-89fe-8777a539a4cb -Url " + caUrl);
                Logger.LogInfo("caUrl = " + caUrl);
                Logger.LogInfo("caCmd = " + caCmd);

                //empty CA library (if found from previous install)
                using (SPSite caSite = caApp.Sites[0])
                {
                    using (SPWeb caWeb = caSite.OpenWeb())
                    {
                        caWeb.AllowUnsafeUpdates = true;
                        //attempt to find
                        SPList listSPDash = caWeb.Lists.TryGetList("SPDash");
                        if (listSPDash != null)
                        {
                            SPListItemCollection items = listSPDash.Items;
                            for (int i = items.Count - 1; i > -1; i--)
                            {
                                items.Delete(i);
                            }
                        }
                        caWeb.AllowUnsafeUpdates = false;
                    }
                }

                //start cmd
                ProcessStartInfo startinfo = new ProcessStartInfo();
                startinfo.FileName = "powershell.exe";
                startinfo.Arguments = caCmd;
                startinfo.UseShellExecute = false;
                startinfo.CreateNoWindow = true;
                Process process;
                process = Process.Start(startinfo);
                Logger.LogInfo("FeatureInstalled - OK");

                //execute first population
                Engine.main();
            }
            catch (Exception x)
            {
                Logger.LogError(String.Format("SPDash Feature Activation error: {0}", x.Message));
            }
        }


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
    }
}
