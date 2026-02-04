using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    class FlexReportTab
    {
        /*
        public string project { get; private set; }
        public string storyboard { get; private set; }
        public int storyboardRow { get; private set; }
        public string testCase { get; private set; }
        public string dataSet { get; private set; }
        public string group { get; private set; }
        public string set { get; private set; }
        public string folder { get; private set; }
        public int seq { get; private set; }



        public FlexReportTab(string project, string storyboard, int storyboardRow, string testCase, string dataSet, string group, string set, string folder, int seq)
        {
            this.project = project;
            this.storyboard = storyboard;
            this.storyboardRow = storyboardRow;
            this.testCase = testCase;
            this.dataSet = dataSet;
            this.group = group;
            this.set = set;
            this.folder = folder;
            this.seq = seq;
        }
        */
        internal string tabName;
        internal List<FlexReportDataSetConfig> list;
        private FlexReportDataRepository repository;

        public FlexReportTab(string tabName, List<FlexReportDataSetConfig> list, FlexReportDataRepository repository)
        {
            this.tabName = tabName;
            this.list = list;
            this.repository = repository;

            foreach (var entry in list)
            {
                string key = entry.project + "___" + entry.storyboard + "___" + entry.testCase + "___" + entry.dataSet;
                if (WordReportConfig.TestCaseDict.ContainsKey(key))
                {
                    var tc = WordReportConfig.TestCaseDict[key];
                    entry.tc = tc;
                }
                else
                {
                    Console.WriteLine("FlexReportTab: key " + key + " not found");
                    ReportUserLogInfo.AddMessage("ResultDataNotFound", "Result data for " + key + " not found");
                }

            }

            Console.WriteLine("FlexReportTab dict keys");
            foreach (string dictKey in WordReportConfig.TestCaseDict.Keys)
                Console.WriteLine("key = " + dictKey);
        }
    }
}
