using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;

namespace SPDash
{
    public class ManageJob : LayoutsPageBase
    {
        protected DropDownList lstSchedule;
        protected Button btnOK;
        protected Label lblMessages;
        protected HyperLink JobDetailsLink;

        protected override bool RequireSiteAdministrator
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            SPContext.Current.Web.AllowUnsafeUpdates = true;

            try
            {
                if (!Page.IsPostBack)
                {
                    lstSchedule.SelectedIndex = int.Parse(AppData.Local.Entries[AppData.SELECTED_INDEX]);

                    //build link to job details
                    foreach (SPJobDefinition job in SPAdministrationWebApplication.Local.JobDefinitions)
                    {
                        if (job.Name.StartsWith("SPDash Timer Job"))
                        {
                            JobDetailsLink.NavigateUrl = String.Format("/_admin/JobEdit.aspx?JobId={0}&Source=%2Fmonitoring%2Easpx", HttpUtility.UrlEncode(job.Id.ToString()));
                        }
                    }
                }
            }
            catch (Exception x)
            {
                lblMessages.Text = x.Message;
            }

        }

        protected void OK_Click(object sender, EventArgs e)
        {
            try
            {
                SPContext.Current.Web.AllowUnsafeUpdates = true;
                SPAdministrationWebApplication caWebApp = SPAdministrationWebApplication.Local;
                AppData.Local.Entries[AppData.SELECTED_INDEX] = lstSchedule.SelectedIndex.ToString();
                AppData.Local.Update();

                //Delete any existing jobs
                string jobName = "SPDash Timer Job";
                foreach (SPJobDefinition job in caWebApp.JobDefinitions)
                {
                    if (job.Name.ToUpper().Equals(jobName.ToUpper()))
                    {
                        job.Delete();
                    }
                }

                //Schedule new job
                switch (lstSchedule.SelectedIndex)
                {
                    case 1://Immediate
                        SPOneTimeSchedule schedule1 = new SPOneTimeSchedule(DateTime.Now);
                        Worker newJob1 = new Worker(jobName, caWebApp);
                        newJob1.Schedule = schedule1;
                        newJob1.Update();
                        break;
                    case 2://Daily
                        SPDailySchedule schedule2 = new SPDailySchedule();
                        schedule2.BeginHour = 2;
                        schedule2.EndHour = 6;
                        Worker newJob2 = new Worker(jobName, caWebApp);
                        newJob2.Schedule = schedule2;
                        newJob2.Update();
                        break;
                    case 3://Weekly
                        SPWeeklySchedule schedule3 = new SPWeeklySchedule();
                        schedule3.BeginDayOfWeek = DayOfWeek.Saturday;
                        schedule3.EndDayOfWeek = DayOfWeek.Saturday;
                        schedule3.BeginHour = 2;
                        schedule3.EndHour = 6;
                        Worker newJob3 = new Worker(jobName, caWebApp);
                        newJob3.Schedule = schedule3;
                        newJob3.Update();
                        break;
                    case 4://Monthly
                        SPMonthlySchedule schedule4 = new SPMonthlySchedule();
                        schedule4.BeginDay = 1;
                        schedule4.EndDay = 1;
                        schedule4.BeginHour = 2;
                        schedule4.EndHour = 6;
                        Worker newJob4 = new Worker(jobName, caWebApp);
                        newJob4.Schedule = schedule4;
                        newJob4.Update();
                        break;
                }


                SPUtility.Redirect(caWebApp.Sites[0].Url, SPRedirectFlags.Default, HttpContext.Current);
            }
            catch (Exception x)
            {
                lblMessages.Text = x.Message;
            }
        }
    }
}
