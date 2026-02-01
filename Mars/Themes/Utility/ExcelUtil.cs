using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
//using Microsoft.Office.Tools.Excel;
using System.Reflection;
using Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Runtime.InteropServices;



namespace Mars.Utility
{
    public class ExcelUtil
    {
        public static DataSet WorkbookToDataSet(string path)
        {
            Microsoft.Office.Interop.Excel.Application excelApp;
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook;
            DataSet ds = new DataSet();
            excelApp = new Microsoft.Office.Interop.Excel.Application();

            //Create an Excel workbook instance and open it from the predefined location
            //xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

            xlWorkbook = excelApp.Workbooks.Open(path);

            foreach (Microsoft.Office.Interop.Excel.Worksheet sheet in xlWorkbook.Sheets)
            {
                string sheetName = sheet.Name;
                Console.WriteLine(sheetName);

                Range excelRange = sheet.UsedRange;


                object[,] valueArray = (object[,])excelRange.get_Value(XlRangeValueDataType.xlRangeValueDefault);

                // skip empty worksheets
                if (valueArray == null)
                    continue;

                System.Data.DataTable dt = new System.Data.DataTable(sheetName);

                //Get the column names
                for (int k = 0; k < valueArray.GetLength(1); )
                {
                    //add columns to the data table.
                    dt.Columns.Add((string)valueArray[1, ++k]);
                }

                //Load data into data table
                object[] singleDValue = new object[valueArray.GetLength(1)];
                //value array first row contains column names. so loop starts from 1 instead of 0
                for (int i = 1; i < valueArray.GetLength(0); i++)
                {
                    for (int k = 0; k < valueArray.GetLength(1); )
                    {
                        singleDValue[k] = valueArray[i + 1, ++k];
                    }
                    // skip comments
                    if (singleDValue[1] != null && singleDValue[2] != null)
                        dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
                }

                ds.Tables.Add(dt);
            }

           // xlWorkbook.Close();
            xlWorkbook.Close(false, Type.Missing, Type.Missing);
            excelApp.Quit();
            return ds;
        }

        public static void ExportDataSetToExcel(DataSet ds, string filePath)
        {
            //Creae an Excel application instance
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();

            //Create an Excel workbook instance and open it from the predefined location
            Microsoft.Office.Interop.Excel.Workbook excelWorkBook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

            foreach (System.Data.DataTable table in ds.Tables)
            {
                //Add a new worksheet to workbook with the Datatable name
                Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet = excelWorkBook.Sheets.Add();
                excelWorkSheet.Name = table.TableName;

                for (int i = 1; i < table.Columns.Count + 1; i++)
                {
                    excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                }

                for (int j = 0; j < table.Rows.Count; j++)
                {
                    SetBackgroundColor(excelWorkSheet, j + 2, 1, XlRgbColor.rgbLightGray);

                    for (int k = 0; k < table.Columns.Count; k++)
                    {
                        excelWorkSheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();

                        if (k == 3 && table.Rows[j].ItemArray[k].ToString().Contains("FALSE"))
                        {
                            SetBackgroundColor(excelWorkSheet, j + 2, 2, XlRgbColor.rgbLightPink);
                            SetBackgroundColor(excelWorkSheet, j + 2, 3, XlRgbColor.rgbLightPink);
                            SetBackgroundColor(excelWorkSheet, j + 2, 4, XlRgbColor.rgbLightPink);
                        }
                    }
                }

                // Format coloers, fonts, etc

                // Bold font for first line
                Microsoft.Office.Interop.Excel.Range rng = (Microsoft.Office.Interop.Excel.Range)excelWorkSheet.Rows[1];
                rng.EntireRow.Font.Bold = true;

                // AutoFilt for all columns
                excelWorkSheet.Columns.AutoFit();

                // Colorize columns
                int usedRowCount = excelWorkSheet.UsedRange.Rows.Count - 1;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            excelWorkBook.SaveAs(filePath);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Clean up references to all COM objects
            // As per above, you're just using a Workbook and Excel Application instance, so release them:
            excelWorkBook.Close(false, Missing.Value, Missing.Value);
            excelApp.Quit();
            Marshal.FinalReleaseComObject(excelWorkBook);
            Marshal.FinalReleaseComObject(excelWorkBook);
        }

        public static void SetBackgroundColor(Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet, int row, int col, XlRgbColor color)
        {
            var columnHeadingsRange = excelWorkSheet.Range[excelWorkSheet.Cells[row, col], excelWorkSheet.Cells[row, col]];
            columnHeadingsRange.Interior.Color = color;
        }
 
    }
}
