
/*A class that transfers data from a datatable format to excel.
  Creates 3 worksheets in a workbook - Main Result, Summary and the Pivot Table*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.TestFramework.DataCompare
{
    class ExcelUtil
    {
        static Microsoft.Office.Interop.Excel.Application excelApp;
        private static MLogger Logger = MLogger.GetLogger(typeof(ExcelUtil));

        public static void ExportXmlCompareResultToExcel(XmlCompareResult xcr, XmlCompareReportConfig xcrc, DataTable DiffTableVal, string filePath, XmlCompareProcessorOld xcp, XmlCompareConfig xcc)
        {
            // Create an Excel application instance
            excelApp = new Microsoft.Office.Interop.Excel.Application();

            // Create an Excel workbook instance and open it from the predefined location
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

//----------Summary (diff) worksheet
            
            // Adding the Diff Worksheet
            Microsoft.Office.Interop.Excel.Worksheet diffWorkSheet = xlWorkbook.Sheets.Add();
            diffWorkSheet.Name = "Summary";

            // populating the diff worksheet
            for (int i = 1; i < DiffTableVal.Columns.Count + 1; i++)
            {
                diffWorkSheet.Cells[1, i] = DiffTableVal.Columns[i - 1].ColumnName;
            }

            for (int j = 0; j < DiffTableVal.Rows.Count; j++)
            {
                for (int k = 0; k < DiffTableVal.Columns.Count; k++)
                {
                    diffWorkSheet.Cells[j + 2, k + 1] = DiffTableVal.Rows[j].ItemArray[k].ToString();
                }
            }

            // Autofit
            diffWorkSheet.Columns.AutoFit();
            // Left Alignment
            Microsoft.Office.Interop.Excel.Range diffrange = diffWorkSheet.UsedRange;
            diffrange.Style.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignLeft;
            // Header
            Microsoft.Office.Interop.Excel.Range diffrows = diffrange.Rows;
            int counter = 0;
            foreach (Microsoft.Office.Interop.Excel.Range row in diffrows)
            {
                if (counter == 0)
                {
                    Microsoft.Office.Interop.Excel.Range firstCell1 = row.Cells;
                    row.Font.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbGreen;
                    row.Font.Bold = true;
                    row.Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbLightGreen;
                    break;
                }
                counter++;
            }

//----------Main worksheet

            // Create Excel sheet
            Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet = xlWorkbook.Sheets.Add();
            excelWorkSheet.Name = "Result";

            // Render headers
            int colNum = 1;
            int rowNum = 1;

            foreach (string header in xcr.columnNames)
            {
                excelWorkSheet.Cells[1, colNum++] = header;
            }

            excelWorkSheet.Cells[1, colNum] = "Status";


            int statusColNum = colNum;

            // Render Data
            colNum = 1;
            rowNum = 2;

            //Test variable to check for same row
            int testrownum = 0;
            
            foreach (ResultDataRow row in xcr.resultData)
            {
                // Show only rows having errors
                if (xcrc.setShowDiffOnly && row.errDescr == null)
                    continue;
                
                string errors = "";
                if (row.errDescr != null)
                    errors += "No match";

                string colorrngstr = GetExcelAddress(rowNum, colNum);
                Microsoft.Office.Interop.Excel.Range colorrange = excelApp.Application.get_Range(colorrngstr);
                System.Drawing.Color Errorcol = System.Drawing.ColorTranslator.FromHtml("#FFAD66");
                colorrange.EntireRow.Interior.Color = System.Drawing.ColorTranslator.ToOle(Errorcol);

                if (errors == "No match")
                {
                    colorrange.EntireRow.Interior.Color = System.Drawing.ColorTranslator.ToOle(Errorcol);
                }
                else
                {
                    colorrange.EntireRow.Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbWhite;
                }
                
                foreach (string header in xcr.columnNames)
                {

                    string cellData = row.GetData(header);
                    ErrorDescriptor errDescr = row.GetErrorDescr(header);
                    excelWorkSheet.Cells[rowNum, colNum] = cellData;
                    if (errDescr != null)
                    {
                        
                        string rangeStr = GetExcelAddress(rowNum, colNum);
                        Microsoft.Office.Interop.Excel.Range rng2 = excelApp.Application.get_Range(rangeStr);
                        if (testrownum != rowNum)
                        {
                            System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#FFAD66");
                            rng2.EntireRow.Interior.Color = System.Drawing.ColorTranslator.ToOle(col);
                        }
                        rng2.Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbYellow;
                        testrownum = rowNum;

                        errors += " " + header + "(" + cellData + ")";
                    }
                    colNum++;
                }
                excelWorkSheet.Cells[rowNum, statusColNum] = errors;
                rowNum++;
                if (rowNum % 100 == 0)
                    Console.WriteLine("R" + rowNum);
                colNum = 1;
            }

            // Visual adjustments

            // Set color of header 
            Microsoft.Office.Interop.Excel.Range usedRange = excelWorkSheet.UsedRange;

            Microsoft.Office.Interop.Excel.Range rows = usedRange.Rows;

            int count = 0;

            foreach (Microsoft.Office.Interop.Excel.Range row in rows)
            {
                if (count == 0)
                {
                    Microsoft.Office.Interop.Excel.Range firstCell = row.Cells;
                    row.Font.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbBlack;
                    row.Font.Bold = true;
                    row.Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbLightGreen;
                    break;
                }

                count++;
            }

            // column width
            excelWorkSheet.Columns.AutoFit();

            // borders for cells
            usedRange.Borders.Color = System.Drawing.Color.Black.ToArgb();

            string xlfilepath = filePath + ".xlsx";

            if (File.Exists(xlfilepath))
            {
                File.Delete(xlfilepath);
            }

            //----------Pivot Table worksheet
            /* Comment out Pyvot table stuff for now
            Microsoft.Office.Interop.Excel.Worksheet PTWorkSheet = xlWorkbook.Sheets.Add();
            PTWorkSheet.Name = "Pivot Table";

            Microsoft.Office.Interop.Excel.PivotCaches pch = xlWorkbook.PivotCaches();
            excelWorkSheet.Activate();
            pch.Add(Microsoft.Office.Interop.Excel.XlPivotTableSourceType.xlDatabase, excelWorkSheet.UsedRange).CreatePivotTable(PTWorkSheet.Cells[1, 1], "PT1", Type.Missing, Type.Missing);
            Microsoft.Office.Interop.Excel.PivotTable pvt = PTWorkSheet.PivotTables("PT1") as Microsoft.Office.Interop.Excel.PivotTable;

            List<string> RowFields = new List<string>();
            RowFields = xcc.RowFields;

            List<string> ColFields = new List<string>();
            ColFields = xcc.ColumnFields;

            foreach (string field in RowFields)
            {
                Microsoft.Office.Interop.Excel.PivotField fld = ((Microsoft.Office.Interop.Excel.PivotField)pvt.PivotFields(field + "_1"));
                fld.Name = field;
                fld.Orientation = Microsoft.Office.Interop.Excel.XlPivotFieldOrientation.xlRowField;
            }

            List<string> CTwoSetsOfFields = new List<string>();

            foreach (string field in ColFields)
            {
                CTwoSetsOfFields.Add(field + "_1");
                CTwoSetsOfFields.Add(field + "_2");
            }

            foreach (string field in CTwoSetsOfFields)
            {
                Microsoft.Office.Interop.Excel.PivotField fld = ((Microsoft.Office.Interop.Excel.PivotField)pvt.PivotFields(field));
                fld.Orientation = Microsoft.Office.Interop.Excel.XlPivotFieldOrientation.xlDataField;
            }

            Microsoft.Office.Interop.Excel.PivotField dataField = pvt.DataPivotField;
            dataField.Orientation = Microsoft.Office.Interop.Excel.XlPivotFieldOrientation.xlColumnField;

            // Worksheet mods - Works
            PTWorkSheet.Move(Type.Missing, diffWorkSheet);
             * End of Pyvot table stuff
             */ 
            excelWorkSheet.Activate();

//----------Saving and closing
            xlWorkbook.SaveAs(xlfilepath);

            //pdf - don't need this for now
            //string pdffilepath = filePath + ".pdf";
            //xlWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, pdffilepath);
            //---

            xlWorkbook.Close();
            excelApp.Quit();
        }

        private static string GetExcelAddress(int rowNumber, int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName + rowNumber;
        }

        public static void ExportDataSetToExcel(DataSet ds, string filePath)
        {
            //Creae an Excel application instance
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();

            //Create an Excel workbook instance and open it from the predefined location

            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

            foreach (DataTable table in ds.Tables)
            {
                //Add a new worksheet to workbook with the Datatable name
                Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet = xlWorkbook.Sheets.Add();
                excelWorkSheet.Name = table.TableName;

                for (int i = 1; i < table.Columns.Count + 1; i++)
                {
                    excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                }

                for (int j = 0; j < table.Rows.Count; j++)
                {
                    for (int k = 0; k < table.Columns.Count; k++)
                    {
                        excelWorkSheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();
                    }
                }
            }

            xlWorkbook.SaveAs(filePath);
            xlWorkbook.Close();
            excelApp.Quit();

        }

        public static void WorkbookXLStoXLSX(string path)
        {
            Logger.logBegin("WorkbookXLStoXLSX");
            // If xlsx version of the file already exists and its timestamp is greater then the timestamp of the original
            // Then no conversion is required

            if (File.Exists(path + 'x'))
            {
                if (File.Exists(path) &&
                    File.GetLastWriteTime(path + 'x') > File.GetLastWriteTime(path))
                {
                    Logger.Info("WorkbookXLStoXLSX", "File already exists, no need to transform to xlsx format");
                    return;
                }
            }
           
            Logger.Info("WorkbookXLStoXLSX", string.Format("Path:[{0}]", path));
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = null;
            try
            {
                excelApp = new Microsoft.Office.Interop.Excel.Application();

                //Create an Excel workbook instance and open it from the predefined location
                //xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

                xlWorkbook = excelApp.Workbooks.Open(path);
                if (File.Exists(path + 'x'))
                    File.Delete(path + 'x');
                xlWorkbook.SaveAs(path + 'x', Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault);
            }
            finally
            {
                if (xlWorkbook != null)
                    xlWorkbook.Close(false, Type.Missing, Type.Missing);
                if (excelApp != null)
                    excelApp.Quit();
                Logger.logEnd("WorkbookXLStoXLSX");
            }
        }
    }
}
