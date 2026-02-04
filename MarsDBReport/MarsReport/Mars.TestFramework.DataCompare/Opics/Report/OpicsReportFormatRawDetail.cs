using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.Opics.Report
{
    public class OpicsReportFormatRawDetail
    {
        public int PrintStart { get; set; }
        public int PrintLength { get; set; }
        public string DataType { get; set; }
        public string DataFormat { get; set; }
        public string Header { get; set; }
        public string HeaderType { get; set; }

        public OpicsReportFormatRawDetail(int printStart, 
                                          int printLenght,
                                          string dataType,
                                          string dataFormat,
                                          string header,
                                          string headerType)
        {
            PrintStart = printStart;
            PrintLength = printLenght;
            DataType = dataType;
            DataFormat = dataFormat;
            Header = header;
            HeaderType = headerType;

        }
    }
}
