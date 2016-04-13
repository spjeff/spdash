using System;
using System.Globalization;
using System.Data;
using Microsoft.SharePoint;
using Microsoft.Office.Server.Diagnostics;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using System.Net;

using System.DirectoryServices;
using System.Collections;
using System.IO;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Serialization;

namespace SPDash
{
    //config class
    public class SPDashTimerJobConfig
    {
        public string TcpPorts;
        public string[] XPathWebConfig;
        public string[] XPathAppHostConfig;
        public string FolderIISRoot;
        public string FolderINETSRV;
        public string[] FileVersion;
        public string[] WMIQuery;
    }

    //timer job worker class
    public class Worker : SPJobDefinition
    {
        public Worker() : base() { }
        public Worker(string jobName, SPWebApplication webApplication)
            : base(jobName, webApplication, null, SPJobLockType.Job)
        { this.Title = "SPDash Timer Job"; }

        public override void Execute(Guid targetInstanceId)
        {
            try
            {
                Logger.LogInfo("SPDash Timer Job job started");
                this.UpdateProgress(1);

                //=== DO STUFF HERE ===
                Engine.main();
                //=== DO STUFF HERE ===

                this.UpdateProgress(100);
            }
            catch (Exception x)
            {
                Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
            }
            finally
            {
                Logger.LogInfo("SPDash Timer Job job Completed");
            }
        }

    }
}
