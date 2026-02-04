using System;
using System.Collections.Generic;
using System.Data;


namespace Mars.autoTest.report.Word
{
    public class WordReportConfigProject
    {
        public string projectDescr { get; internal set; }
        public long projectId { get; internal set; }
        public string projectName { get; internal set; }
        public DataTable ProjectStoryboardData { get; internal set; }
        public int count { get; internal set; }
       

        public List<WordReportConfigStoryboard> sbList = new List<WordReportConfigStoryboard>();

        public WordReportConfigProject()
        {
            InitProjectStoryboardData();
        }

        public void ConfigureStoryboard(string sbName, string sbDescr, long sbId, DataTable sbdt)
        {
            WordReportConfigStoryboard sb = new WordReportConfigStoryboard(sbName, sbDescr, sbdt);
            sbList.Add(sb);
        }

        public WordReportConfigStoryboard ConfigureStoryboard(string sbName, string sbDescr, long sbId, DataTable sbdt, DataTable sbTestingSummayDt, DataTable sbReportSummayDt, int successCount, int failCount, int unprCount, int partialCount)
        {
            WordReportConfigStoryboard sb = new WordReportConfigStoryboard(sbName, sbDescr, sbdt, sbTestingSummayDt, sbReportSummayDt, successCount, failCount, unprCount, partialCount);
            sbList.Add(sb);

            return sb;
        }

        public WordReportConfigStoryboard ConfigureStoryboard(string sbName, string sbDescr, long sbId,  int successCount, int failCount, int unprCount, int partialCount)
        {
            WordReportConfigStoryboard sb = new WordReportConfigStoryboard(sbName, sbDescr, successCount, failCount, unprCount, partialCount);
            sbList.Add(sb);

            return sb;
        }

        // ProjectStoryboardData
        public void InitProjectStoryboardData()
        {
            ProjectStoryboardData = new DataTable();
            ProjectStoryboardData.Columns.Add("#");
            ProjectStoryboardData.Columns.Add("SB Name");
            ProjectStoryboardData.Columns.Add("SB Description");

        }

        public void AddRowToProjectStoryboardData(string sbName, string sbDescr)
        {
            DataRow newDTRow = ProjectStoryboardData.NewRow();
            ProjectStoryboardData.Rows.Add(newDTRow);

            newDTRow["SB Name"] = sbName;
            newDTRow["SB Description"] = sbDescr;
            newDTRow["#"] = "" + ProjectStoryboardData.Rows.Count;
        }
    }
}