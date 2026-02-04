using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class FlexReport
    {
        private List<FlexReportDataSetConfig> ReportDataSetConfigList = new List<FlexReportDataSetConfig>();
        internal List<FlexReportTab> ReportTabs = new List<FlexReportTab>();
        internal DataTable FolderInfo;

        internal List<FlexReportDataSetConfig> GetReportConfigList()
        {
            return ReportDataSetConfigList;
        }

        internal void AddFlexReportDataSetConfig(string project, string storyboard, int storyboardRow, string testCase, string dataSet, string group, string set, string folder, int seq, string expResults, string diary, string stepDesc)
        {
            FlexReportDataSetConfig dataSetConfig = new FlexReportDataSetConfig(project, storyboard, storyboardRow, testCase, dataSet, group, set, folder, seq, expResults, diary, stepDesc);
            ReportDataSetConfigList.Add(dataSetConfig);
        }

        public MarsConfig.MarsConfig mc { get; internal set; }
        public DataTable GroupInfo { get; internal set; }
        public string ReportGroup { get; internal set; }
        public DataTable SetInfo { get; internal set; }
        public string ReportSet { get; internal set; }
        public DataTable StyleInfo { get; internal set; }
        public DataTable KeywordTranslation { get; internal set; }
        public Dictionary<string, string> KeywordTranslationDict { get; internal set; }
        public string ReportMode { get; internal set; }

        internal void AddTab(string tabName, List<FlexReportDataSetConfig> list, FlexReportDataRepository repository)
        {
            FlexReportTab tab = new FlexReportTab(tabName, list, repository);
            ReportTabs.Add(tab);
        }

        internal void OrderReportEntries()
        {
            ReportDataSetConfigList = ReportDataSetConfigList.OrderBy(item => item.seq).ToList();
        }
    }
}
