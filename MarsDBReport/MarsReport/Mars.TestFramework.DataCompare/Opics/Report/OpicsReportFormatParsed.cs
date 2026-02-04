using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.Opics.Report
{
    public class OpicsReportFormatParsed
    {
        public List<string> BreakdownHeaders;
        public List<string> ReportHeaders;
        public List<string> TotalHeaders;
        public List<string> IgnoreList;
        public List<int> ColBreak;
        public List<int> ColLength;
        public OpicsReportFormatRaw RawFmt;

        internal void Init(OpicsReportFormatRaw rawFmt)
        {

            RawFmt = rawFmt;
            var allData = (from b in rawFmt.details
                     where b.PrintStart >= 0
                     orderby b.PrintStart
                     select b).ToList();

            foreach (var data in allData)
            {
                if (data.DataType == "1")
                    data.PrintStart = data.PrintStart - data.PrintLength + 3;
            }

            // BreakdownHeaders
            BreakdownHeaders = (from b in allData
                                where b.HeaderType.ToLower().Contains("b")
                                select b.Header).ToList();

            // ReportHeaders
            ReportHeaders = (from b in allData
                             where b.HeaderType.ToLower().Contains("d")
                             select b.Header).ToList();

            // TotalHeaders
            TotalHeaders = (from b in allData
                             where b.HeaderType.ToLower().Contains("t")
                             select b.Header).ToList();

            // IgnoreList
            IgnoreList = new List<string> { "*",
                                            "          ",
                                            "Branch",
                                            "System",
                                            "_________"};
            if ((ReportHeaders.First().Length) > 0)
                IgnoreList.Add(ReportHeaders.First());


            // ColLength
            ColLength = (from b in allData
                        where b.HeaderType.ToLower().Contains("b") == false
                        select b.PrintLength).ToList();

            // ColBreak
            ColBreak = (from b in allData
                        where b.HeaderType.ToLower().Contains("b") == false
                        select b.PrintStart - 1).ToList();

            ColBreak.Add(ColLength.Last() + ColBreak.Last());

        }
    }
}
