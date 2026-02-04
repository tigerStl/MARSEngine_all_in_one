using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data;

namespace MARS.TEMP
{
    class XmlCompareReport
    {
        private XmlCompareReportConfig xcrc;
        private XmlCompareResult xcr;
        //diff data table
        private DataTable PopulatedDataTable = new DataTable();
        //data for PT
        private XmlCompareProcessor xcp;
        //data for PT - Row and Col fields
        private XmlCompareConfig xcc;

        public XmlCompareReport(XmlCompareReportConfig xcrc)
        {
            // TODO: Complete member initialization
            this.xcrc = xcrc;
        }

        //public XmlCompareReport(XmlCompareResult xcr, XmlCompareReportConfig xcrc)
        public XmlCompareReport(XmlCompareResult xcr, XmlCompareReportConfig xcrc, DataTable PopulatedDataTable, XmlCompareProcessor xcp, XmlCompareConfig xcc)
        {
            // TODO: Complete member initialization
            this.xcr = xcr;
            this.xcrc = xcrc;
            //diff
            this.PopulatedDataTable = PopulatedDataTable;
            //for PT
            this.xcp = xcp;
            //for PT- fields
            this.xcc = xcc;
        }

        internal void ProcessReport(string excelFileName)
        {
            //ExcelUtil.ExportXmlCompareResultToExcel(xcr, xcrc, excelFileName);
            
            //new
            ExcelUtil.ExportXmlCompareResultToExcel(xcr, xcrc, PopulatedDataTable, excelFileName, xcp, xcc);
     
            //New Code - To make excel pop up to display main file
            var xlApp = new Excel.Application();
            xlApp.Visible = true;
            string xlfilepath = excelFileName + ".xlsx";
            //xlApp.Workbooks.Open(@excelFileName);
            xlApp.Workbooks.Open(@xlfilepath);
        }
    }
}
