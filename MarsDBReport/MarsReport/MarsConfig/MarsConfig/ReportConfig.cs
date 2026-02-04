using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.MarsConfig
{
    public class ReportConfig
    {
        public string reportPath { get; set; }
        public string reportTemplatePath { get; set; }
        public string reportImagePath { get; set; }

        public string reportTableWord { get; set; }

        public ReportConfig(string reportPath, string reportTemplatePath, string reportImagePath, string reportTableWord)
        {
            this.reportPath = reportPath;
            this.reportTemplatePath = reportTemplatePath;
            this.reportImagePath = reportImagePath;
            this.reportTableWord = reportTableWord;
        }
    }
}
