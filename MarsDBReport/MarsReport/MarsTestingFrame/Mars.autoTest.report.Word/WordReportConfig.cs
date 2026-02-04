using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class WordReportConfig
    {

        public void ConfigureStoryBoard(string storyboardName, string stotyboardDescription, System.Data.DataTable sbdt)
        {
            StoryBoardConfig = new WordReportConfigStoryboard(storyboardName, stotyboardDescription, sbdt);
            TestCaseConfigList = new List<WordReportConfigTestCase>();
        }

        public void ConfigureTestCase(string testCaseName, string dataSetName, string dataSetDescr, System.Data.DataTable tcdt)
        {
            WordReportConfigTestCase tc = new WordReportConfigTestCase(testCaseName, dataSetName, dataSetDescr, tcdt);
            TestCaseConfigList.Add(tc);
        }

        public WordReportConfigStoryboard StoryBoardConfig { get; set; }

        public List<WordReportConfigTestCase> TestCaseConfigList { get; set; }

        public string TemplateFilePath { get; set; }

        public string OutputFilePath { get; set; }

        public string ProjectName { get; set; }

        public string ProjectDescription { get; set; }

        public string ReportGenDate { get; set; }

        public int MarsTSCount { get; set; }

        public int MarsTCCount { get; set; }

        public int MarsTestStepCount { get; set; }

        public int MarsBSucc { get; set; }

        public int MarsCSucc { get; set; }

        public int MarsBFail { get; set; }

        public int MarsCFail { get; set; }

        public int MarsBUnpr { get; set; }

        public int MarsCUnpr { get; set; }

        public int MarsBPartial { get; set; }

        public int MarsCPartial { get; set; }
    }
}
