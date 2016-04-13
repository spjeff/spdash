using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Administration;

namespace SPDash
{
    public class Logger : SPDiagnosticsServiceBase
    {
        //Constants
        public static string areaName = "Custom Timer Jobs";

        public enum CategoryID
        {
            None = 0,
            Processing = 100,
            Faulting = 200
        }

        //Constructors
        public Logger() : base("Custom Timer Job Logging Service", SPFarm.Local) { }
        public Logger(string Name, SPFarm Farm) : base(Name, Farm) { }

        //Properties
        private static Logger local;
        public static Logger Local
        {
            get
            {
                {
                    if (local == null)
                        local = new Logger();
                }
                return local;
            }
        }

        //Methods
        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            List<SPDiagnosticsCategory> categories = new List<SPDiagnosticsCategory>();

            uint catId0 = (uint)(int)Enum.Parse(typeof(CategoryID), CategoryID.None.ToString());
            categories.Add(new SPDiagnosticsCategory(CategoryID.None.ToString(), TraceSeverity.Verbose, EventSeverity.None, 0, catId0));

            uint catId100 = (uint)(int)Enum.Parse(typeof(CategoryID), CategoryID.Processing.ToString());
            categories.Add(new SPDiagnosticsCategory(CategoryID.Processing.ToString(), TraceSeverity.Verbose, EventSeverity.Information, 0, catId100));

            uint catId200 = (uint)(int)Enum.Parse(typeof(CategoryID), CategoryID.Faulting.ToString());
            categories.Add(new SPDiagnosticsCategory(CategoryID.Faulting.ToString(), TraceSeverity.Unexpected, EventSeverity.Error, 0, catId200));

            yield return new SPDiagnosticsArea(areaName, categories);
        }

        public static void LogError(string errorMessage)
        {
            SPDiagnosticsCategory category = Logger.Local.Areas[areaName].Categories[CategoryID.Faulting.ToString()];
            Logger.Local.WriteTrace(0, category, TraceSeverity.Unexpected, errorMessage);
        }

        public static void LogInfo(string infoMessage)
        {
            SPDiagnosticsCategory category = Logger.Local.Areas[areaName].Categories[CategoryID.Processing.ToString()];
            Logger.Local.WriteTrace(0, category, TraceSeverity.Verbose, infoMessage);
        }
    }

}
