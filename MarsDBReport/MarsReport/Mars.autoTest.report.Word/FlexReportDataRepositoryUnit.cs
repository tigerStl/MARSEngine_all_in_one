using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class FlexReportDataRepositoryUnit
    {
        public string project { get; private set; }
        public string storyboard { get; private set; }
        public WordReportConfig config { get; private set; }

        public FlexReportDataRepositoryUnit(string project, string storyboard, WordReportConfig config)
        {
            this.project = project;
            this.storyboard = storyboard;
            this.config = config;
        }
    }
}
