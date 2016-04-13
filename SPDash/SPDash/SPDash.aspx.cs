using System;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.ApplicationPages;
using System.Web.UI.WebControls;
using System.Data;
using System.IO;
using System.Web;
using System.Drawing;
using System.Collections.Generic;

namespace SPDash
{
    //data class
    public class SPDashStyle
    {
        //data members
        public string fileName;
        public string[] keywords;
        public Color[] colors;

        //constructor
        public SPDashStyle(string fileName, string[] keywords, Color[] colors)
        {
            this.fileName = fileName;
            this.keywords = keywords;
            this.colors = colors;
        }
    }

    //interface class
    public partial class SPDash : GlobalAdminPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                //XML data source library
                string webUrl = SPContext.Current.Web.Url;
                using (SPSite caSite = new SPSite(webUrl))
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

                        //populate drop down
                        SPQuery query = new SPQuery(listSPDash.DefaultView);
                        SPListItemCollection coll = listSPDash.GetItems(query);
                        foreach (SPListItem item in coll)
                        {
                            if (item.Name != "_SPDashTimerJobConfig.xml")
                            {
                                spdashSource.Items.Add(new ListItem(item.Name.Replace(".xml", ""), item.ID.ToString()));
                            }
                        }

                        //draw first selection

                        spdashSource_SelectedIndexChanged(null, null);

                        caWeb.AllowUnsafeUpdates = false;
                    }
                }
            }

            //focus drop down
            spdashSource.Focus();
        }
        protected void spdashSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            //selected index
            if (!String.IsNullOrEmpty(spdashSource.SelectedValue))
            {
                int index = Convert.ToInt16(spdashSource.SelectedValue);
                if (index != 0)
                {
                    //XML data source library
                    string webUrl = SPContext.Current.Web.Url;
                    using (SPSite caSite = new SPSite(webUrl))
                    {
                        using (SPWeb caWeb = caSite.OpenWeb())
                        {
                            DataTable dt = new DataTable();
                            SPList listSPDash = caWeb.Lists.TryGetList("SPDash");
                            if (listSPDash != null)
                            {
                                //find XML
                                SPListItem item = listSPDash.Items.GetItemById(index);
                                if (item != null)
                                {
                                    //read XML
                                    Stream binary = item.File.OpenBinaryStream();
                                    dt.ReadXml(binary);

                                    //display
                                    spdashGrid.DataSource = dt;
                                    spdashGrid.HeaderStyle.Wrap = false;
                                    spdashGrid.DataBind();
                                    spdashLabelLastUpdated.Text = "Last updated: " + item.File.TimeLastModified.ToLocalTime().ToString();

                                    //set export link
                                    spdashExportLink.NavigateUrl = String.Format("SPDashExport.aspx?xml={0}&index={1}", item.File.Name, index);
                                }

                                //declare style guide to colorize XML display
                                List<SPDashStyle> styleGuides = new List<SPDashStyle>();
                                styleGuides.Add(new SPDashStyle("KERBEROS SPN",
                                    new string[] { "HTTP", "HOST", "SMTP", "WSMAN" },
                                    new Color[] { Color.LightGreen, Color.Yellow, Color.Pink, Color.Aqua }));
                                styleGuides.Add(new SPDashStyle("WINDOWS SERVICES",
                                    new string[] { "AUTO-RUNNING", "AUTO-STOPPED", "MANUAL-RUNNING", "MANUAL-STOPPED", "DISABLED-STOPPED" },
                                    new Color[] { Color.LightGreen, Color.Red, Color.Yellow, Color.LightGray, Color.DarkGray }));
                                styleGuides.Add(new SPDashStyle("OPEN TCP PORTS",
                                    new string[] { "OPEN", "CLOSED" },
                                    new Color[] { Color.LightGreen, Color.Red }));
                                styleGuides.Add(new SPDashStyle("WMI",
                                     new string[] { "T", "FALSE" },
                                     new Color[] { Color.LightGreen, Color.Red }));
                                styleGuides.Add(new SPDashStyle("XPATH",
                                    new string[] { "TRUE" },
                                    new Color[] { Color.LightGreen }));
                                styleGuides.Add(new SPDashStyle("IIS NTAUTHPROV",
                                    new string[] { "NEGOTIATE,NTLM", "NTLM" },
                                    new Color[] { Color.LightGreen, Color.Yellow, }));
                                styleGuides.Add(new SPDashStyle("WMI_IISWEBVIRTUALDIRSETTING",
                                    new string[] { "NEGOTIATE,NTLM", "NTLM" },
                                    new Color[] { Color.LightGreen, Color.Yellow, }));
                                styleGuides.Add(new SPDashStyle("LOCAL ADMIN",
                                  new string[] { "D", "L" },
                                  new Color[] { Color.LightGreen, Color.Yellow, }));

                                //apply color based on style guide
                                foreach (SPDashStyle guide in styleGuides)
                                {
                                    if (item.File.Name.ToUpper().StartsWith(guide.fileName))
                                    {
                                        int r = 0;
                                        foreach (GridViewRow dr in spdashGrid.Rows)
                                        {
                                            int c = 0;
                                            foreach (TableCell dc in dr.Cells)
                                            {
                                                //cell value
                                                TableCell cell = dr.Cells[c];
                                                string text = cell.Text.ToUpper();
                                                //background color
                                                int k = 0;
                                                foreach (string keyword in guide.keywords)
                                                {
                                                    if (text.StartsWith(keyword))
                                                    {
                                                        cell.BackColor = guide.colors[k];
                                                        if (cell.BackColor == Color.Red) cell.ForeColor = Color.White;
                                                    }
                                                    k++;
                                                }
                                                c++;
                                            }
                                            r++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
