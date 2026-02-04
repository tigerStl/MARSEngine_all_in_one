using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public static class ReportUserLogInfo
    {
        public static DataTable LogDataTable;

        public static string currentFolder { get;  set; }
        public static string currentCaseName { get;  set; }
        public static string currentDataSetName { get;  set; }
        public static string currentBc { get;  set; }

        public static string currentProject { get; set; }

        public static string currentSB { get; set; }

        public static void AddMessage(string folder, string project, string testCaseName, string dataSetName, string storyboard, string type, string bc, string message)
        {
            if (LogDataTable == null)
            {
                Init();
            }
                
            DataRow row = LogDataTable.NewRow();
            row["Folder"] = folder;
            row["Project"] = project;
            row["Test Case"] = testCaseName;
            row["Data Set"] = dataSetName;
            row["Storyboard"] = storyboard;
            row["Type"] = type;
            row["Baseline/Compare"] = bc;
            row["Message"] = message;

            LogDataTable.Rows.Add(row);

            Console.WriteLine("ReportUserLogInfo|" + folder + "|" + project + "|" + testCaseName + "|" + dataSetName + "|" + storyboard + "|" + type + "|" + bc + "|" + message);
        }


        public static void AddMessage(string type, string message)
        {
            if (LogDataTable == null)
            {
                Init();
            }

            DataRow row = LogDataTable.NewRow();
            row["Folder"] = currentFolder;
            row["Project"] = currentProject;
            row["Test Case"] = currentCaseName;
            row["Data Set"] = currentDataSetName;
            row["Storyboard"] = currentSB;
            row["Type"] = type;
            row["Baseline/Compare"] = currentBc;
            row["Message"] = message;

            LogDataTable.Rows.Add(row);

            Console.WriteLine("ReportUserLogInfo|" + currentFolder + "|" + currentProject + "|" + currentCaseName + "|" + currentDataSetName + "|" + currentSB + "|" + type + "|" + currentBc + "|" + message);
        }

        public static void AddMessage(string bc, string type, string message)
        {
            if (LogDataTable == null)
            {
                Init();
            }

            DataRow row = LogDataTable.NewRow();
            row["Folder"] = currentFolder;
            row["Project"] = currentProject;
            row["Test Case"] = currentCaseName;
            row["Data Set"] = currentDataSetName;
            row["Storyboard"] = currentSB;
            row["Type"] = type;
            row["Baseline/Compare"] = bc;
            row["Message"] = message;

            LogDataTable.Rows.Add(row);

            Console.WriteLine("ReportUserLogInfo|" + currentFolder + "|" + currentProject + "|" + currentCaseName + "|" + currentDataSetName + "|" + currentSB + "|" + type + "|" + bc + "|" + message);
        }


        private static void Init()
        {
            LogDataTable = new DataTable("User Log");

            LogDataTable.Columns.Add("Folder");
            LogDataTable.Columns.Add("Project");
            LogDataTable.Columns.Add("Test Case");
            LogDataTable.Columns.Add("Data Set");
            LogDataTable.Columns.Add("Storyboard");
            LogDataTable.Columns.Add("Type");
            LogDataTable.Columns.Add("Baseline/Compare");
            LogDataTable.Columns.Add("Message");

        }
    }
}
