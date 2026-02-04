using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.Opics.Report
{
    public class OpicsReportFormat
    {
        public OpicsReportFormatRaw RawFmt { get; set; }
        public OpicsReportFormatParsed ParsedFmt { get; set; }

        public bool Init(string filePath)
        {
            bool rc = false;
            string[] text = ReadFmtFile(filePath);
            if (text != null)
            {
                RawFmt = new OpicsReportFormatRaw();
                ParsedFmt = new OpicsReportFormatParsed();
                RawFmt.Init(text);
                ParsedFmt.Init(RawFmt);
            }    

            return rc;
        }

        private string[] ReadFmtFile(string filePath)
        {
            string[] text = System.IO.File.ReadAllLines(filePath);

            return text;
        }
    }
}
