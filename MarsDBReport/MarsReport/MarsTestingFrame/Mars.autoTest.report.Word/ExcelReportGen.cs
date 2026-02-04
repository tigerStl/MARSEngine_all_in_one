using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace Mars.autoTest.report.Word
{
    class ExcelReportGen
    {
        private WordReportConfig config;
        XLWorkbook wb;

        public ExcelReportGen(WordReportConfig config)
        {
            this.config = config;
        }

        internal void OpenDocument()
        {
            wb = new XLWorkbook(@"c:\temp\AtestReport.xlsx");
        }

        internal void GenerateDocument()
        {
            var ws = wb.Worksheets.Worksheet(1);
            ws.Cell("B2").Value = "Contacts";

            // First Names
            ws.Cell("B3").Value = "FName";
            ws.Cell("B4").Value = "John";
            ws.Cell("B5").Value = "Hank";
            ws.Cell("B6").SetValue("Dagny"); // Another way to set the value

            // Last Names
            ws.Cell("C3").Value = "LName";
            ws.Cell("C4").Value = "Galt";
            ws.Cell("C5").Value = "Rearden";
            ws.Cell("C6").SetValue("Taggart"); // Another way to set the value

            ws.Cell(8, 1).InsertTable(config.StoryBoardConfig.StoryBoardData);
        }

        internal void SaveDocument()
        {
            wb.Save();
        }
    }
}
