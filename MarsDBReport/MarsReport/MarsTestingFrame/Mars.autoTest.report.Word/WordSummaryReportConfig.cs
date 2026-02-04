using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class WordSummaryReportConfig
    {
        public int MarsProjectCount { get; set; }
        public int MarsStoryboardCount { get; set; }
        public int MarsTestCaseCount { get; set; }
        public int MarsTestStepCount { get; set; }

        public string OutputFilePath { get; set; }
        public string TemplateFilePath { get; set; }
        public string ReportGenDate { get; set; }

        public int MarsCSucc { get; set; }
        public int MarsCFail { get; set; }
        public int MarsCPartial { get; set; }
        public int MarsCUnpr { get; set; }

        public int MarsBSucc { get; set; }
        public int MarsBFail { get; set; }
        public int MarsBPartial { get; set; }
        public int MarsBUnpr { get; set; }

        public DataTable ProjectSummaryData { get; set; }

        public List<WordReportConfigProject> projects = new List<WordReportConfigProject>();

        public WordSummaryReportConfig()
        {
            InitProjectSummaryData();
        }

        public WordReportConfigProject ConfigureProjectData(string projectName, string projectDescr, long projectId, DataTable projdt)
        {
            WordReportConfigProject project = new Word.WordReportConfigProject();
            project.projectName = projectName;
            project.projectDescr = projectDescr;
            project.projectId = projectId;
            project.ProjectStoryboardData = projdt;
            projects.Add(project);

            project.count = projects.Count;
            return project;
        }

        public WordReportConfigProject ConfigureProjectData(string projectName, string projectDescr, long projectId)
        {
            WordReportConfigProject project = new Word.WordReportConfigProject();
            project.projectName = projectName;
            project.projectDescr = projectDescr;
            project.projectId = projectId;
            projects.Add(project);

            project.count = projects.Count;
            return project;
        }


        // ProjectSummaryData
        public void InitProjectSummaryData()
        {
            ProjectSummaryData = new DataTable();

            ProjectSummaryData.Columns.Add("#");
            ProjectSummaryData.Columns.Add("Project Name");
            ProjectSummaryData.Columns.Add("Project Description");
        }

        public void AddRowToProjectSummaryData(string projectName, string projectDescr)
        {
            DataRow newDTRow = ProjectSummaryData.NewRow();
            ProjectSummaryData.Rows.Add(newDTRow);

            newDTRow["#"] = "" + ProjectSummaryData.Rows.Count;
            newDTRow["Project Name"] = projectName;
            newDTRow["Project Description"] = projectDescr;
        }
    }
}
