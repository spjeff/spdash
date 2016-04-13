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
    class Engine
    {
        //globals
        static DataTable dt;
        static int i_machine;
        static SPDashTimerJobConfig config = new SPDashTimerJobConfig();
        static string configFilename = "_SPDashTimerJobConfig.xml";
        static string configLocalFile = SPUtility.GetGenericSetupPath(@"TEMPLATE\FEATURES") + @"\SPDash_SPDashTimerJobFeature\SPDashTimerJob\_SPDashTimerJobConfig.xml";

        static public void main()
        {
            //download config XML from Central Admin site
            Microsoft.SharePoint.Administration.SPAdministrationWebApplication caApp = SPAdministrationWebApplication.Local;
            using (SPSite caSite = caApp.Sites[0])
            {
                using (SPWeb caWeb = caSite.OpenWeb())
                {
                    caWeb.AllowUnsafeUpdates = true;
                    //attempt to find
                    SPList listSPDash = caWeb.Lists.TryGetList("SPDash");
                    if (listSPDash != null)
                    {
                        SPFile configXML = caWeb.GetFile(listSPDash.RootFolder.ServerRelativeUrl + "/" + configFilename);
                        if (configXML.Exists)
                        {
                            //save locally
                            byte[] content = configXML.OpenBinary();
                            FileStream stream = new FileStream(configLocalFile, FileMode.Create);
                            BinaryWriter bw = new BinaryWriter(stream);
                            bw.Write(content);
                            bw.Close();
                            stream.Close();
                        }
                        else
                        {
                            //upload from local
                            byte[] content = File.ReadAllBytes(configLocalFile);
                            listSPDash.RootFolder.Files.Add(configFilename, content);
                        }
                    }
                    caWeb.AllowUnsafeUpdates = false;
                }
            }

            //read config
            XmlSerializer serializer = new XmlSerializer(typeof(SPDashTimerJobConfig));
            using (FileStream file = new FileStream(configLocalFile, FileMode.Open))
            {
                config = serializer.Deserialize(file) as SPDashTimerJobConfig;
                file.Close();
            }

            //gather data sources
            string[] data = { "Add Remove Programs", "WEB PART files in INETPUB", "FEATURE folder in SP hive", "File Versions", "Global Assembly Cache (GAC)", "Local Admins", "Logical Disks", "Kerberos SPN", "Open TCP Ports", "SYS files (pagefile,hiberfil)", "Windows Services", "XPath web.config", "XPath apphost.config", "WMI" };
            foreach (string d in data)
            {
                GatherData(d);
            }
        }
        static string XPathCleanColumn(string xp, int maxLength)
        {
            return xp.Substring(xp.LastIndexOf('/') + 1, xp.Length - xp.LastIndexOf('/') - 1);
        }
        static int GetUserAccountControl(DirectoryEntry anEntry)
        {
            //Get AD user account control flag for Kerberos
            int val = 0;
            if (null != anEntry)
            {
                System.DirectoryServices.PropertyCollection collProperties = anEntry.Properties;
                if ((null != collProperties) && (collProperties.Count > 0))
                {
                    object prop = anEntry.Properties["userAccountControl"];
                    if (null != prop && (prop is PropertyValueCollection))
                    {
                        object oVal = ((PropertyValueCollection)prop).Value;
                        //If property doesn't exist, than value is null
                        val = (int)oVal;
                    }
                }
            }
            return val;
        }
        static void GetWMIDataForServer(String server, String query, String wmiTargetScope, String wmiTargetFrom)
        {
            try
            {
                //WMI Connection credentials to the remote computer - not needed if the logged in account has access 
                ConnectionOptions oConn = new ConnectionOptions();
                oConn.Authentication = AuthenticationLevel.PacketPrivacy;
                oConn.Impersonation = ImpersonationLevel.Impersonate;
                oConn.Timeout = new TimeSpan(0, 0, 15);
                ManagementScope oMs = new ManagementScope("\\\\" + server + wmiTargetScope, oConn);

                //WMI Execute query
                ObjectQuery oQuery = new ObjectQuery(query);
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                ManagementObjectCollection oReturnCollection = oSearcher.Get();

                //Build out columns on first run
                if (dt.Columns.Count == 1)
                {
                    foreach (ManagementObject oReturn in oReturnCollection)
                    {
                        //Put name first if it exists
                        try
                        {
                            if (oReturn.Properties["Name"] != null) dt.Columns.Add("Name");
                            if (oReturn.Properties["ServerComment"] != null) dt.Columns.Add("ServerComment");
                        }
                        catch (System.Management.ManagementException) { }
                        catch (Exception x)
                        {
                            Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                        }
                        foreach (PropertyData p in oReturn.Properties)
                        {
                            if (p.Name.ToString().ToUpper() != "NAME" && p.Name.ToString().ToUpper() != "SERVERCOMMENT") dt.Columns.Add(p.Name);
                        }
                        break;
                    }
                }

                //Loop through found objects and add to table
                foreach (ManagementObject oReturn in oReturnCollection)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = server;
                    foreach (PropertyData p in oReturn.Properties)
                    {
                        if (dt.Columns.Contains(p.Name))
                        {
                            //set value
                            dr[p.Name] = p.Value;

                            //Reformatting
                            if (dr[p.Name].ToString().ToUpper() == "TRUE") dr[p.Name] = "T";
                            if (wmiTargetFrom == "Win32_GroupUser")
                            {
                                dr[p.Name] = p.Value.ToString().Split(',')[1].Replace("Name=\"", "").TrimEnd('"');
                            }
                        }
                    }
                    dt.Rows.Add(dr);
                };
            }
            catch (System.Management.ManagementException) { }
            catch (Exception x)
            {
                Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
            }
        }
        static void GatherKerberos(string m)
        {
            //Ensure 1 row-per-SPN (dynamic height)
            //Open connection to query AD
            DirectoryEntry obEntry = new DirectoryEntry();
            DirectorySearcher srch = new DirectorySearcher(obEntry);
            //machine=dnsHostName
            srch.Filter = "(|(dnsHostName=" + m + ")(dnsHostName=" + m + "." + Environment.GetEnvironmentVariable("USERDNSDOMAIN") + "))";
            srch.SizeLimit = 99;
            srch.PageSize = 99;
            SearchResultCollection results = srch.FindAll();
            if (results.Count == 0)
            {
                //user=samAccountName
                srch.Filter = "(samAccountName=" + m + ")";
                results = srch.FindAll();
            }

            if (results.Count > 0)
            {
                int i_spn = 0;
                foreach (SearchResult res in results)
                {
                    //open AD object
                    DirectoryEntry dn = new DirectoryEntry(res.Path);
                    //UAC
                    const int TRUSTED_FOR_DELEGATION = 0x80000;
                    int uac = GetUserAccountControl(dn);
                    if (uac == (uac | TRUSTED_FOR_DELEGATION))
                    {
                        dt.Columns[dt.Columns.Count - 1].ColumnName += " (OK)";
                    }
                    else
                    {
                        dt.Columns[dt.Columns.Count - 1].ColumnName += " (-Trust)";
                    }
                    foreach (object prop in dn.Properties["servicePrincipalName"])
                    {
                        try
                        {
                            dt.Rows[i_spn][dt.Columns.Count - 1] = prop.ToString();
                        }
                        catch (System.IndexOutOfRangeException)
                        {
                            DataRow row = dt.NewRow();
                            row[i_machine - 1] = prop.ToString();
                            dt.Rows.Add(row);
                        }
                        catch (Exception x)
                        {
                            Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                        }
                        i_spn++;
                    }
                }
            }
        }
        static void ResetDataTable()
        {
            //reset data table
            dt = new DataTable("spdash");
            dt.Columns.Add("Machine");
        }
        static void GatherData(string action)
        {
            ResetDataTable();

            //loop for all machines in the farm
            SPFarm myFarm = SPFarm.Local;
            SPServerCollection serverColl = myFarm.Servers;
            if (serverColl != null && serverColl.Count > 0)
            {
                i_machine = 0;
                foreach (SPServer spserver in serverColl)
                {
                    if (spserver.Role != SPServerRole.Invalid)
                    {
                        i_machine++;
                        string m = spserver.Address;
                        switch (action)
                        {
                            case "Local Admins":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("User");
                                }
                                dt.Columns.Add(m);

                                //Data source
                                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + m + ",Computer");
                                DirectoryEntry admGroup = localMachine.Children.Find("Administrators", "group");
                                object members = admGroup.Invoke("members", null);

                                //Ensure 1 row-per-service (dynamic height)
                                foreach (object groupMember in (IEnumerable)members)
                                {
                                    DirectoryEntry member = new DirectoryEntry(groupMember);
                                    bool local = false;
                                    if (member.Path.ToUpper().Contains(m.ToUpper())) local = true;
                                    string user = member.Path.ToUpper().Replace("WINNT://", "").Replace("/", "\\").Replace(Environment.UserDomainName + "\\" + m.ToUpper(), m.ToUpper());
                                    //Assume row does not exist
                                    bool rowExists = false;
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString() == user.ToString()) rowExists = true;
                                    }
                                    if (!rowExists)
                                    {
                                        DataRow row = dt.NewRow();
                                        row[0] = user;
                                        dt.Rows.Add(row);
                                    }
                                }

                                //Populate most recent machine column
                                foreach (object groupMember in (IEnumerable)members)
                                {
                                    DirectoryEntry member = new DirectoryEntry(groupMember);
                                    bool local = false;
                                    if (member.Path.ToUpper().Contains(m.ToUpper())) local = true;
                                    string user = member.Path.ToUpper().Replace("WINNT://", "").Replace("/", "\\").Replace(Environment.UserDomainName + "\\" + m.ToUpper(), m.ToUpper());
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString().ToUpper() == user.ToString().ToUpper())
                                        {
                                            row[dt.Columns.Count - 1] = (local ? "L" : "D");
                                        }
                                    }
                                }
                                break;

                            case "WEB PART files in INETPUB":
                                string[] webpart = Directory.GetFiles(@"\\" + m + @"\c$\inetpub\wwwroot", "*.webpart", SearchOption.AllDirectories);
                                string[] dwp = Directory.GetFiles(@"\\" + m + @"\c$\inetpub\wwwroot", "*.dwp", SearchOption.AllDirectories);
                                string[] files = new string[webpart.Length + dwp.Length];
                                Array.Copy(webpart, 0, files, 0, webpart.Length);
                                Array.Copy(dwp, 0, files, webpart.Length, dwp.Length);

                                if (i_machine == 1)
                                {
                                    dt.Columns.Add("WEB PART File");
                                }
                                foreach (string f in files)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["WEB PART File"] = f.Replace(@"\\" + m + @"\", ""); ;
                                    dt.Rows.Add(row);
                                }
                                if (files.GetUpperBound(0) == -1)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["WEB PART File"] = "None found";
                                    dt.Rows.Add(row);
                                }
                                break;

                            case "FEATURE folder in SP hive":
                                string[] dirs = Directory.GetDirectories(@"\\" + m + @"\c$\program files\common files\microsoft shared\web server extensions\14\template\features", "*", SearchOption.TopDirectoryOnly);
                                if (dt.Columns.Count == 1)
                                {
                                    dt.Columns.Add("Feature Dir");
                                }
                                foreach (string d in dirs)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["Feature Dir"] = d.Replace(@"\\" + m + @"\", ""); ;
                                    dt.Rows.Add(row);
                                }
                                break;

                            case "Global Assembly Cache (GAC)":
                                if (dt.Columns.Count == 1)
                                {
                                    dt.Columns.Add("GAC File");
                                }
                                files = Directory.GetFiles(@"\\" + m + @"\C$\windows\assembly\gac", "*.dll", SearchOption.AllDirectories);
                                foreach (string f in files)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["GAC File"] = f.Replace(@"\\" + m + @"\", "");
                                    dt.Rows.Add(row);
                                }
                                break;

                            case "Logical Disks":
                                if (i_machine == 1)
                                {
                                    dt.Columns.Add("Logical Disk");
                                    dt.Columns.Add("GB Free");
                                    dt.Columns.Add("GB Total Size");
                                }
                                ConnectionOptions opt = new ConnectionOptions();
                                ObjectQuery oQuery = new ObjectQuery("SELECT Size, FreeSpace, Name, FileSystem FROM Win32_LogicalDisk WHERE DriveType = 3");
                                ManagementScope scope = new ManagementScope("\\\\" + m + "\\root\\cimv2", opt);
                                ManagementObjectSearcher moSearcher = new ManagementObjectSearcher(scope, oQuery);
                                ManagementObjectCollection collection = moSearcher.Get();
                                foreach (ManagementObject res in collection)
                                {
                                    decimal size = Convert.ToDecimal(res["Size"]) / 1024 / 1024 / 1024;
                                    decimal freeSpace = Convert.ToDecimal(res["FreeSpace"]) / 1024 / 1024 / 1024;
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["Logical Disk"] = res["Name"];
                                    row["GB Free"] = Decimal.Round(freeSpace, 1);
                                    row["GB Total Size"] = Decimal.Round(size, 1);
                                    dt.Rows.Add(row);
                                }
                                break;

                            case "File Versions":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("File Name");
                                }
                                dt.Columns.Add(m);

                                //Local SPFarm major versoin (14,15,etc.)
                                string SPFarmVersion = SPFarm.Local.BuildVersion.Major.ToString();

                                //Target files on remote C$
                                string[] targetFiles = config.FileVersion;

                                //Ensure 1 row-per-file (fixed height)
                                foreach (string f in targetFiles)
                                {
                                    bool rowExists = false;
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString() == String.Format(f, SPFarmVersion)) rowExists = true;
                                    }
                                    if (!rowExists)
                                    {
                                        DataRow row = dt.NewRow();
                                        row[0] = String.Format(f, SPFarmVersion);
                                        dt.Rows.Add(row);
                                    }
                                }

                                //Populate most recent machine column
                                foreach (DataRow row in dt.Rows)
                                {
                                    string ver = "";
                                    //get remote C$ file version
                                    try
                                    {
                                        //reformat UNC
                                        string filepath = "\\\\" + m + "\\" + row[0].ToString();

                                        //get file version
                                        FileVersionInfo finfo = FileVersionInfo.GetVersionInfo(filepath);
                                        ver = finfo.FileVersion;
                                    }
                                    catch (System.IO.FileNotFoundException) { }
                                    catch (Exception x)
                                    {
                                        Logger.LogInfo(x.Message + "/" + x.StackTrace + "/" + x.GetType().ToString());
                                    }

                                    //display
                                    row[dt.Columns.Count - 1] = ver;
                                }
                                break;

                            case "IIS NTAuthProv":
                                //open all IIS websites, build datatable, read string 1:website
                                //show button to change (NTLM, NTLMNego, Nego, Blank) per row
                                if (i_machine == 1)
                                {
                                    dt.Columns.Add("Web ID");
                                    dt.Columns.Add("Web Title");
                                    dt.Columns.Add("NTAuthentionProviders");
                                }
                                try
                                {
                                    DirectoryEntry iisServer = new DirectoryEntry("IIS://" + m + "/W3SVC");
                                    foreach (DirectoryEntry iisWeb in iisServer.Children)
                                    {
                                        if (iisWeb.SchemaClassName == "IIsWebServer")
                                        {
                                            DirectoryEntry iisWebRoot = new DirectoryEntry("IIS://" + m + "/W3SVC/" + iisWeb.Name + "/root");
                                            DataRow row = dt.NewRow();
                                            row["Machine"] = m;
                                            row["Web ID"] = iisWeb.Name;
                                            row["Web Title"] = iisWeb.Properties["ServerComment"][0].ToString();
                                            string temp = "";
                                            foreach (PropertyValueCollection pc in iisWebRoot.Properties)
                                            {
                                                if (pc.PropertyName == "NTAuthenticationProviders") temp = pc[0].ToString();
                                            }
                                            row["NTAuthentionProviders"] = temp;
                                            dt.Rows.Add(row);
                                            iisWebRoot.Close();
                                        }
                                    }
                                    iisServer.Close();
                                }
                                catch (System.Runtime.InteropServices.COMException) { }
                                catch (Exception x)
                                {
                                    Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                }
                                break;

                            case "IIS KernelAuth":
                                if (i_machine == 1)
                                {
                                    //schema
                                    dt.Columns.Add("Web App Path");
                                    dt.Columns.Add("useKernelMode");
                                }
                                try
                                {
                                    //gather from remote XML file nodes
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load("\\\\" + m + "\\C$\\windows\\system32\\inetsrv\\config\\applicationHost.config");
                                    XmlNodeList nodeList = doc.SelectNodes("/configuration/location");
                                    foreach (XmlNode node in nodeList)
                                    {
                                        if (node.Name.ToLower() == "location")
                                        {
                                            //defaults
                                            string useKernelModePath = "";
                                            string useKernelMode = "";
                                            useKernelModePath = node.Attributes["path"].Value;
                                            XmlNode nodeAuth = node.SelectSingleNode("system.webServer/security/authentication/windowsAuthentication");
                                            if (nodeAuth != null)
                                            {
                                                XmlAttribute attrib = nodeAuth.Attributes["useKernelMode"];
                                                if (attrib != null) useKernelMode = attrib.Value.ToString().Replace("true", "TRUE");
                                            }

                                            //only record if we have values
                                            if (!String.IsNullOrEmpty(useKernelModePath) && !String.IsNullOrEmpty(useKernelMode))
                                            {
                                                DataRow row = dt.NewRow();
                                                row["Machine"] = m;
                                                row["Web App Path"] = useKernelModePath;
                                                row["useKernelMode"] = useKernelMode;
                                                dt.Rows.Add(row);
                                            }
                                        }
                                    }
                                }
                                catch (Exception x)
                                {
                                    Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                }
                                break;

                            case "Open TCP Ports":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("TCP Port");
                                    //First column labels
                                    foreach (string port in config.TcpPorts.Split(','))
                                    {
                                        DataRow row = dt.NewRow();
                                        row["TCP Port"] = port;
                                        dt.Rows.Add(row);
                                    }
                                }
                                //Machines horizontally
                                dt.Columns.Add(m);

                                //Navigate to matched cell
                                foreach (string port in config.TcpPorts.Split(','))
                                {
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString().ToUpper() == port)
                                        {
                                            //Matched cell, execute data gather
                                            TcpClient c = new TcpClient();
                                            c.SendTimeout = 200;
                                            c.ReceiveTimeout = 200;
                                            try
                                            {
                                                c.Connect(m, Convert.ToInt16(port));
                                                row[dt.Columns.Count - 1] = (c.Connected) ? "Open" : "Closed";
                                            }
                                            catch (System.Net.Sockets.SocketException)
                                            {
                                                row[dt.Columns.Count - 1] = "Closed";
                                            }
                                            catch (Exception x)
                                            {
                                                Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                            }
                                            c.Close();
                                            break;
                                        }
                                    }
                                }
                                break;

                            case "Windows Services":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("Service");
                                }
                                dt.Columns.Add(m);
                                //Ensure 1 row-per-service (dynamic height)
                                opt = new ConnectionOptions();
                                oQuery = new ObjectQuery("SELECT * FROM Win32_Service");
                                scope = new ManagementScope("\\\\" + m + "\\root\\cimv2", opt);
                                moSearcher = new ManagementObjectSearcher(scope, oQuery);
                                collection = moSearcher.Get();
                                foreach (ManagementObject res in collection)
                                {
                                    bool rowExists = false;
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString() == res["Name"].ToString()) rowExists = true;
                                    }
                                    if (!rowExists)
                                    {
                                        DataRow row = dt.NewRow();
                                        row[0] = res["Name"];
                                        dt.Rows.Add(row);
                                    }
                                }

                                //Populate most recent machine column
                                foreach (ManagementObject res in collection)
                                {
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString().ToUpper().Trim() == res["Name"].ToString().ToUpper().Trim())
                                        {
                                            row[dt.Columns.Count - 1] = res["StartMode"] + "-" + res["State"];
                                        }
                                    }
                                }
                                break;

                            case "Kerberos SPN":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                }
                                dt.Columns.Add(m);

                                //loop all Managed user accounts on first pass
                                GatherKerberos(m);
                                if (i_machine == 1)
                                {
                                    var managedAccounts = new SPFarmManagedAccountCollection(SPFarm.Local);
                                    foreach (SPManagedAccount managedAccount in managedAccounts)
                                    {
                                        string username = managedAccount.Username;
                                        if (username.Contains("\\")) username = username.Split('\\')[1];
                                        dt.Columns.Add(username);
                                        GatherKerberos(username);
                                    }
                                }
                                break;

                            case "Add Remove Programs":
                                //Schema
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("App");
                                }
                                dt.Columns.Add(m);

                                //Ensure 1 row-per-service (dynamic height)
                                RegistryKey key = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, m).OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
                                foreach (String s in key.GetSubKeyNames())
                                {
                                    //Attempt to read DisplayName if it exists
                                    object dn = key.OpenSubKey(s).GetValue("DisplayName");
                                    if (dn != null) if (dn.ToString().Length > 0)
                                        {
                                            bool rowExists = false;
                                            foreach (DataRow row in dt.Rows)
                                            {
                                                if (row[0].ToString() == dn.ToString()) rowExists = true;
                                            }
                                            if (!rowExists)
                                            {
                                                DataRow row = dt.NewRow();
                                                row[0] = dn;
                                                dt.Rows.Add(row);
                                            }
                                        }
                                }

                                //Populate most recent machine column
                                foreach (String s in key.GetSubKeyNames())
                                {
                                    object dn = key.OpenSubKey(s).GetValue("DisplayName");
                                    if (dn != null) if (dn.ToString().Length > 0)
                                            foreach (DataRow row in dt.Rows)
                                            {
                                                if (row[0].ToString().ToUpper() == dn.ToString().ToUpper())
                                                {
                                                    object ver = key.OpenSubKey(s).GetValue("DisplayVersion");
                                                    if (ver == null)
                                                    {
                                                        row[dt.Columns.Count - 1] = "X";
                                                    }
                                                    else
                                                    {
                                                        if (ver.ToString().Length > 0) row[dt.Columns.Count - 1] = ver;
                                                    }
                                                }
                                            }
                                }
                                break;

                            case "SYS files (pagefile,hiberfil)":
                                //Schema
                                string sysFile = "pagefile.sys,hiberfil.sys";
                                if (i_machine == 1)
                                {
                                    dt.Columns.Clear();
                                    dt.Columns.Add("File");
                                    //First column labels
                                    foreach (string file in sysFile.Split(','))
                                    {
                                        DataRow row = dt.NewRow();
                                        row["File"] = file;
                                        dt.Rows.Add(row);
                                    }
                                }
                                //Machines horizontally
                                dt.Columns.Add(m);

                                //Navigate to matched cell
                                foreach (string file in sysFile.Split(','))
                                {
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (row[0].ToString() == file)
                                        {
                                            //Matched cell, execute data gather
                                            string cellValue = "";

                                            //Detect all logical drive letters
                                            ConnectionOptions sys_opt = new ConnectionOptions();
                                            ObjectQuery sys_oQuery = new ObjectQuery("SELECT Size, FreeSpace, Name, FileSystem FROM Win32_LogicalDisk WHERE DriveType = 3");
                                            ManagementScope sys_scope = new ManagementScope("\\\\" + m + "\\root\\cimv2", sys_opt);
                                            ManagementObjectSearcher sys_moSearcher = new ManagementObjectSearcher(sys_scope, sys_oQuery);
                                            ManagementObjectCollection sys_collection = sys_moSearcher.Get();
                                            foreach (ManagementObject res in sys_collection)
                                            {
                                                try
                                                {
                                                    string sys_logicalDrive = res["Name"].ToString().Substring(0, 1);
                                                    if (File.Exists(String.Format(@"\\{0}\" + sys_logicalDrive + @"$\" + file, m))) cellValue += sys_logicalDrive + " ";
                                                }
                                                catch (Exception x)
                                                {
                                                    Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                                }
                                            }

                                            row[dt.Columns.Count - 1] = cellValue;
                                        }
                                    }
                                }

                                ////Add row always
                                //if (true)
                                //{
                                //    DataRow row = dt.NewRow();
                                //    row["Machine"] = m;
                                //    row["Pagefile.sys"] = sys_page;
                                //    row["Hiberfil.sys"] = sys_hiber;
                                //    dt.Rows.Add(row);
                                //}
                                break;

                            case "XPath web.config":
                                //Schema
                                int maxLength = 20;
                                string[] xpaths = config.XPathWebConfig;
                                if (i_machine == 1)
                                {
                                    dt.Columns.Add("IIS Website");
                                    foreach (string xp in xpaths)
                                    {
                                        dt.Columns.Add("Exists-" + XPathCleanColumn(xp, maxLength));
                                    }
                                }

                                //Gather data
                                string[] wssHomeDirs = Directory.GetDirectories(String.Format(@"\\{0}\" + config.FolderIISRoot, m));
                                foreach (string d in wssHomeDirs)
                                {
                                    //Assume none
                                    string[] xpathExists = new string[xpaths.Length];
                                    int xi = 0;
                                    foreach (string xp in xpaths)
                                    {
                                        //Attempt read
                                        try
                                        {
                                            XmlDocument xpathDoc = new XmlDocument();
                                            xpathDoc.Load(d + @"\web.config");
                                            XmlNode xpathNode = xpathDoc.SelectSingleNode(xp);
                                            if (xpathNode != null) xpathExists[xi] = "True";
                                        }
                                        catch (Exception x)
                                        {
                                            Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                        }
                                        xi++;
                                    }

                                    //Add row
                                    DataRow row = dt.NewRow();
                                    row["Machine"] = m;
                                    row["IIS Website"] = d;
                                    xi = 0;
                                    foreach (string xp in xpaths)
                                    {
                                        row["Exists-" + XPathCleanColumn(xp, maxLength)] = xpathExists[xi];
                                        xi++;
                                    }
                                    dt.Rows.Add(row);
                                }
                                break;

                            case "XPath apphost.config":
                                //Schema
                                maxLength = 20;
                                xpaths = config.XPathAppHostConfig;
                                if (i_machine == 1)
                                {
                                    foreach (string xp in xpaths)
                                    {
                                        dt.Columns.Add("Exists-" + XPathCleanColumn(xp, maxLength));
                                    }
                                }

                                //Assume none
                                string[] ahcXpathExists = new string[xpaths.Length];
                                int ahcXi = 0;
                                foreach (string xp in xpaths)
                                {
                                    //Attempt read
                                    try
                                    {
                                        XmlDocument xpathDoc = new XmlDocument();
                                        xpathDoc.Load(String.Format(@"\\{0}\" + config.FolderINETSRV, m) + @"\applicationHost.config");
                                        XmlNode xpathNode = xpathDoc.SelectSingleNode(xp);
                                        if (xpathNode != null) ahcXpathExists[ahcXi] = "True";
                                    }
                                    catch (Exception x)
                                    {
                                        Logger.LogError(String.Format("SPDash Timer Job error: {0}", x.Message));
                                    }
                                    ahcXi++;
                                }

                                //Add row
                                DataRow ahcRow = dt.NewRow();
                                ahcRow["Machine"] = m;
                                ahcXi = 0;
                                foreach (string xp in xpaths)
                                {
                                    ahcRow["Exists-" + XPathCleanColumn(xp, maxLength)] = ahcXpathExists[ahcXi];
                                    ahcXi++;
                                }
                                dt.Rows.Add(ahcRow);

                                break;

                            case "WMI":
                                if (i_machine == 1)
                                {
                                    //first machine only.  loop each query with machine nested 3rd tier below
                                    //(machine 1 > query loop > machine loop)
                                    foreach (string wmiConfig in config.WMIQuery)
                                    {
                                        //clear memory table
                                        ResetDataTable();

                                        //build query
                                        string[] configs = wmiConfig.Split(';');
                                        string wmiFrom = configs[0];
                                        string wmiScope = configs[1];
                                        string wmiWhere = configs[2];
                                        string wmiSelect = configs[3];
                                        string query = "SELECT " + ((String.IsNullOrEmpty(wmiSelect)) ? "*" : wmiSelect) + " FROM " + wmiFrom + " " + wmiWhere;

                                        //gather data
                                        foreach (SPServer server in serverColl)
                                        {
                                            if (server.Role != SPServerRole.Invalid)
                                            {
                                                GetWMIDataForServer(server.Address, query, wmiScope, "");
                                            }
                                        }

                                        //write to XML
                                        SaveDataTableToXML(action + "_" + wmiFrom);
                                    }
                                }
                                break;

                            //end switch
                        }
                    }
                }
            }

            //sort
            if (action == "Add Remove Programs")
            {
                DataView dvSort = new DataView(dt);
                dvSort.Sort = "App";
                dt = dvSort.ToTable();
            }

            //write to XML
            if (action != "WMI") SaveDataTableToXML(action);
        }
        static void SaveDataTableToXML(string outFilename)
        {
            //save to XML
            MemoryStream content = new MemoryStream(); ;
            dt.WriteXml(content, XmlWriteMode.WriteSchema);

            //XML data source library
            Microsoft.SharePoint.Administration.SPAdministrationWebApplication caApp = SPAdministrationWebApplication.Local;
            using (SPSite caSite = caApp.Sites[0])
            {
                using (SPWeb caWeb = caSite.OpenWeb())
                {
                    caWeb.AllowUnsafeUpdates = true;
                    //attempt to locate
                    SPList listSPDash = caWeb.Lists.TryGetList("SPDash");
                    if (listSPDash == null)
                    {
                        //create if missing
                        caWeb.Lists.Add("SPDash", "* DO NOT DELETE - storage of XML files for dashboard", SPListTemplateType.DocumentLibrary);
                        listSPDash = caWeb.Lists.TryGetList("SPDash");
                    }
                    listSPDash.RootFolder.Files.Add(outFilename + ".xml", content, true);
                    caWeb.AllowUnsafeUpdates = false;
                }
            }
        }
    }
}
