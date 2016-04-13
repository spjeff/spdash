using System;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.ApplicationPages;
using System.Web.UI.WebControls;
using System.Data;
using System.IO;
using System.Web;

namespace SPDash
{
    public partial class SPDashExport : GlobalAdminPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //config
            string delimiter = ",";
            string xml = Request.QueryString["xml"];
            string fileName = xml.Replace(".xml", ".csv");
            int index = Convert.ToInt16(Request.QueryString["index"]);

            //selected index
            if (index != 0)
            {
                //XML data source library
                using (SPWeb caWeb = SPContext.Current.Web)
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
                        }
                    }

                    //prepare the output stream
                    Response.Clear();
                    Response.ContentType = "text/csv";
                    Response.AppendHeader("Content-Disposition",
                        string.Format("attachment; filename={0}", fileName));

                    //write the csv column headers
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        Response.Write(dt.Columns[i].ColumnName);
                        Response.Write((i < dt.Columns.Count - 1) ? delimiter : Environment.NewLine);
                    }

                    //write the data
                    foreach (DataRow row in dt.Rows)
                    {
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            Response.Write(row[i].ToString());
                            Response.Write((i < dt.Columns.Count - 1) ? delimiter : Environment.NewLine);
                        }
                    }

                    //send
                    Response.End();
                }
            }
        }
    }
}
