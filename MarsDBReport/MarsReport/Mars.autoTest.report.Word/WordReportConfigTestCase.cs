using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Mars.autoTest.report.Word
{
    public class WordReportConfigTestCase
    {
        public WordReportConfigTestCase(string tcName, string dataSetName, string dataSetDescr, DataTable tcData,  Dictionary<string, DataTable> extractedTablesDict = null, string testCaseDescr = "")
        {
            TestCaseName = tcName;
            DataSetName = dataSetName;
            DataSetDescr = dataSetDescr;
            TestCaseData = tcData;
            ExtractedTablesDict = extractedTablesDict;
            TestCaseDescr = testCaseDescr;
        }

        public string TestCaseName { get; set; }

        public string DataSetName { get; set; }

        public string DataSetDescr { get; set; }

        public DataTable TestCaseData { get; set; }

        public string TestCaseDescr { get; set; }

        public Dictionary<string, DataTable> ExtractedTablesDict { get; set; }

    }
}
