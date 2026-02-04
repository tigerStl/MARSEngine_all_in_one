using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class FlexReportDataRepository
    {
        Dictionary<string, FlexReportDataRepositoryUnit> repoDict = new Dictionary<string, FlexReportDataRepositoryUnit>();
        public void AddUnit(string project, string storyboard, WordReportConfig config)
        {

            FlexReportDataRepositoryUnit unit = new FlexReportDataRepositoryUnit(project, storyboard, config);
            repoDict.Add(project + "___" + storyboard, unit);
        }
    }
}
