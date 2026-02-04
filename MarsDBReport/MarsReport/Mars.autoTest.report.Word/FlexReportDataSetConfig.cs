using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class FlexReportDataSetConfig
    {
        public string project { get; private set; }
        public string storyboard { get; private set; }
        public int storyboardRow { get; private set; }
        public string testCase { get; private set; }
        public string dataSet { get; private set; }
        public string group { get; private set; }
        public string set { get; private set; }
        public string folder { get; private set; }
        public int seq { get; private set; }
        public WordReportConfigTestCase tc { get; internal set; }
        public string expResults { get; private set; }
        public string diary { get; private set; }
        public string stepDesc { get; private set; }
        public string status { 
            get; 
            internal set; 
        }
        public string TestSteps { get; internal set; }

        public FlexReportDataSetConfig(string project, string storyboard, int storyboardRow, string testCase, string dataSet, string group, string set, string folder, int seq, string expResults , string diary, string stepDesc)
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
            this.expResults = expResults;
            this.diary = diary;
            this.stepDesc = stepDesc;
            
        }
    }
}
