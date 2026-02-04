using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.Opics.Report
{
    public class OpicsReportFormatRaw
    {
        public List<OpicsReportFormatRawDetail> details;
        internal void Init(string[] text)
        {
            int len = text.Length;
            details = new List<OpicsReportFormatRawDetail>();

            for (int lineNo = 3; lineNo < len; lineNo++)
            {
                string[] fields = text[lineNo].Split(',');
                OpicsReportFormatRawDetail det = new Report.OpicsReportFormatRawDetail(int.Parse(fields[2]),
                                                                                       int.Parse(fields[3]),
                                                                                       fields[4],
                                                                                       fields[5],
                                                                                       fields[6],
                                                                                       fields[7]
                                                                                       );
                details.Add(det);
            }
        }
    }
}
