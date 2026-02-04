using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Mars.autoTest.report.Word
{
    public class WordReportConfigStoryboard
    {
        public int successCount;
        public int failCount;
        public int unprCount;
        public int partialCount;

        public WordReportConfigStoryboard(string sbName, string sbDescr, DataTable dt)
        {
            StoryBoardName = sbName;
            StoryBoardDescr = sbDescr;
            StoryBoardData = dt;
        }

        public WordReportConfigStoryboard(string sbName, string sbDescr, DataTable dt, DataTable sbTestingSummayDt, DataTable sbReportSummayDt, int successCount, int failCount, int unprCount, int partialCount) : this(sbName, sbDescr, dt)
        {
            StoryBoardName = sbName;
            StoryBoardDescr = sbDescr;
            StoryBoardData = dt;
            StoryBoardTestingData = sbTestingSummayDt;
            StoryBoardReportData = sbReportSummayDt;

            this.successCount = successCount;
            this.failCount = failCount;
            this.unprCount = unprCount;
            this.partialCount = partialCount;
        }

        public WordReportConfigStoryboard(string sbName, string sbDescr,  int successCount, int failCount, int unprCount, int partialCount)
        {
            StoryBoardName = sbName;
            StoryBoardDescr = sbDescr;
  
            this.successCount = successCount;
            this.failCount = failCount;
            this.unprCount = unprCount;
            this.partialCount = partialCount;

            // Intit dataTables
            InitStoryBoardTestingData();
            InitStoryBoardReportData();
            InitStoryBoardData();
        }

        // ///   Handle DataTables

        // StoryBoardData
        
        public void InitStoryBoardData()
        {
            StoryBoardData = new DataTable();

            StoryBoardData.Columns.Add("#");
           
            StoryBoardData.Columns.Add("TC_Name");
            StoryBoardData.Columns.Add("TS_Name");
            StoryBoardData.Columns.Add("Data_Set");
            StoryBoardData.Columns.Add("Result");
            //StoryBoardData.Columns.Add("Start");
            //StoryBoardData.Columns.Add("End");
            StoryBoardData.Columns.Add("BL Start");
            StoryBoardData.Columns.Add("BL Duration");
            StoryBoardData.Columns.Add("CP Start");
            StoryBoardData.Columns.Add("CP Duration");
        }

        public void AddRowToStoryBoardData(string runOrder, string testCaseName, string tsName, string dataSet, string result, string bStart, string bDuration, string cStart, string cDuration)
        {
            DataRow newDTRow = StoryBoardData.NewRow();
            StoryBoardData.Rows.Add(newDTRow);
            
            newDTRow["#"] = runOrder;
            newDTRow["TC_Name"] = testCaseName;
            newDTRow["TS_Name"] = tsName;
            newDTRow["Data_Set"] = dataSet;
            newDTRow["Result"] = result;

            newDTRow["BL Start"] = bStart;
            newDTRow["BL Duration"] = bDuration;

            newDTRow["CP Start"] = cStart;
            newDTRow["CP Duration"] = cDuration;
            //    newDTRow["Start"] = start;
            //    newDTRow["End"] = end;
        }

        // StoryBoardTestingData
        public void InitStoryBoardTestingData()
        {
            StoryBoardTestingData = new DataTable();
            StoryBoardTestingData.Columns.Add("#");
            StoryBoardTestingData.Columns.Add("Test Metrics");
            StoryBoardTestingData.Columns.Add("Count");
        }

        public void AddRowToStoryBoardTestingData(string metrics, int count)
        {
            DataRow newDTRow = StoryBoardTestingData.NewRow();
            StoryBoardTestingData.Rows.Add(newDTRow);

            newDTRow["Test Metrics"] = metrics;
            newDTRow["Count"] = count;
            newDTRow["#"] = "" + StoryBoardTestingData.Rows.Count;
        }


        // StoryBoardReportData
        public void InitStoryBoardReportData()
        {
            StoryBoardReportData = new DataTable();

            StoryBoardReportData.Columns.Add("Result");
            StoryBoardReportData.Columns.Add("Baseline");
            StoryBoardReportData.Columns.Add("Compare");
        }

        public void AddRowToStoryBoardReportData(string result, int baseline, int compare)
        {
            DataRow newDTRow = StoryBoardReportData.NewRow();
            StoryBoardReportData.Rows.Add(newDTRow);

            if (result.Equals("SUCCESS"))
                result = "PASS";

            newDTRow["Result"] = result;
            newDTRow["Baseline"] = baseline;
            newDTRow["Compare"] = compare;
        }

        public string StoryBoardName { get; set; }
        public string StoryBoardDescr { get; set; }
        public DataTable StoryBoardData { get; set; }
        public DataTable StoryBoardTestingData { get; set; }
        public DataTable StoryBoardReportData { get; set; }
    }
}
