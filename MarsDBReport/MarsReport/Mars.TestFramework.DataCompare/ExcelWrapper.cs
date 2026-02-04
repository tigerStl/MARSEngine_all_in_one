using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Microsoft.Office.Tools.Excel;
using System.Reflection;
using Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DiffPlex.DiffBuilder.Model;

namespace Mars.TestFramework.DataCompare
{
    public class ExcelWrapper
    {
        Microsoft.Office.Interop.Excel.Application excelApp;
        Microsoft.Office.Interop.Excel.Workbook xlWorkbook;
        Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet;

        int usedRowCount = 0;
        int topXmlRow = 4;
        int leftXmlColumn = 1;
        int rightXmlColumn = 3;
        int headerRow = 1;
        int diffColumn= 4;
        string NOT_EQ = " DIFF=\"NOT EQUAL \"";
        string NOT_EQ_DATA = "NOT EQUAL";

        string[] dataArray1;
        string[] dataArray2;

        internal void ProcessTextDiff(SideBySideDiffModel result)
        {
            // excelWorkSheet.Columns.AutoFit();
            excelWorkSheet.Cells[1, 1] = "File 1";
            excelWorkSheet.Cells[1, 2] = "File 2";
            excelWorkSheet.Cells[1, 3] = "Status";
            SetBackgroundColor(1, 1, XlRgbColor.rgbLightGray);
            SetBackgroundColor(1, 2, XlRgbColor.rgbLightGray);
            SetBackgroundColor(1, 3, XlRgbColor.rgbLightGray);

            int count = result.OldText.Lines.Count;
            int excelRow = 1;
            for (int rowNum = 0; rowNum < count; rowNum ++)
            {
                string data1 = result.OldText.Lines[rowNum].Text;
                string data2 = result.NewText.Lines[rowNum].Text;

                excelRow = rowNum + 2;

                if (data1 != null)
                    excelWorkSheet.Cells[excelRow, 1] = data1;
                if (data2 != null)
                    excelWorkSheet.Cells[excelRow, 2] = data2;

                // colors for when data is different
                if (result.OldText.Lines[rowNum].Type == DiffPlex.DiffBuilder.Model.ChangeType.Modified)
                {
                    SetBackgroundColor(excelRow, 1, XlRgbColor.rgbYellow);
                    SetBackgroundColor(excelRow, 2, XlRgbColor.rgbYellow);
                    SetBackgroundColor(excelRow, 3, XlRgbColor.rgbYellow);
                    
                    excelWorkSheet.Cells[excelRow, 3] = "Diff";
                }

                // colors for when data is missing
                else if (result.OldText.Lines[rowNum].Type == DiffPlex.DiffBuilder.Model.ChangeType.Imaginary)
                {
                    SetBackgroundColor(excelRow, 1, XlRgbColor.rgbAquamarine);
                    SetBackgroundColor(excelRow, 2, XlRgbColor.rgbAquamarine);
                    SetBackgroundColor(excelRow, 3, XlRgbColor.rgbAquamarine);
                    excelWorkSheet.Cells[excelRow, 3] = "Not Found";
                }
                else if (result.NewText.Lines[rowNum].Type == DiffPlex.DiffBuilder.Model.ChangeType.Imaginary)
                {
                    SetBackgroundColor(excelRow, 1, XlRgbColor.rgbAquamarine);
                    SetBackgroundColor(excelRow, 2, XlRgbColor.rgbAquamarine);
                    SetBackgroundColor(excelRow, 3, XlRgbColor.rgbAquamarine);
                    excelWorkSheet.Cells[excelRow, 3] = "Not Found";
                }


            }
            excelWorkSheet.Columns.AutoFit();
            
        }

        public void Open()
        {
            excelApp = new Microsoft.Office.Interop.Excel.Application();

            //Create an Excel workbook instance and open it from the predefined location
            xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);
            excelWorkSheet = xlWorkbook.Sheets.Add();
            excelWorkSheet.Name = "COMPARE";
        }

        public void SaveAndClose(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            xlWorkbook.SaveAs(filePath);
            
            //xlWorkbook.Close();
            //excelApp.Quit();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Clean up references to all COM objects
            // As per above, you're just using a Workbook and Excel Application instance, so release them:
            xlWorkbook.Close(false, Missing.Value, Missing.Value);
            excelApp.Quit();
            Marshal.FinalReleaseComObject(xlWorkbook);
            Marshal.FinalReleaseComObject(xlWorkbook);
        }

        public void ProcessDiffsImported(int thisCol, int otherCol)
        {
            usedRowCount = excelWorkSheet.UsedRange.Rows.Count - 1;

            excelWorkSheet.Columns.AutoFit();
            for (int i = topXmlRow; i < usedRowCount; i++)
            {
                if (i % 1000 == 0)
                    JobContext.DisplayStopwatch("Process diff for col = "+ thisCol + " row = " + i);

                string cellValue1 = (string)(excelWorkSheet.Cells[i, thisCol] as Range).Value;

                if (cellValue1.Contains(NOT_EQ))
                {
                    excelWorkSheet.Cells[i, diffColumn] = "DIFF";
                    string newValue1 = cellValue1.Replace(NOT_EQ, "");
                    excelWorkSheet.Cells[i, thisCol] = newValue1;

                    string cellValue2 = (string)(excelWorkSheet.Cells[i, otherCol] as Range).Value;
                    string newValue2 = cellValue2.Replace(NOT_EQ, "");
                    excelWorkSheet.Cells[i, otherCol] = newValue2;
                    SetBackgroundColor(i, thisCol, XlRgbColor.rgbSkyBlue);
                    SetBackgroundColor(i, otherCol, XlRgbColor.rgbSkyBlue);
                    continue;
                }

                if (cellValue1.Contains("IMPORTED"))
                {
                    // mark it as diff in extra col 
                    excelWorkSheet.Cells[i, diffColumn] = "DIFF";
                    // Remove "NOT FOUND IN OPPOSITE COL"
                    int start = cellValue1.IndexOf('<') + 1;
                    int end = cellValue1.IndexOf(' ', start);
                    string tag = "/" + cellValue1.Substring(start, end - start);
                    excelWorkSheet.Cells[i, thisCol] = " ";
                    while (cellValue1.Contains(tag) == false)
                    {
                        excelWorkSheet.Cells[i, thisCol] = " ";
                        SetBackgroundColor(i, thisCol, XlRgbColor.rgbSilver);
                        i++;
                        cellValue1 = (string)(excelWorkSheet.Cells[i, thisCol] as Range).Value;
                    }
                    excelWorkSheet.Cells[i, thisCol] = " ";
                    SetBackgroundColor(i, thisCol, XlRgbColor.rgbSilver);
                }
            }
        }


        public void ProcessDiffsImportedUsingData(int thisCol, int otherCol, string[] data)
        {
            usedRowCount = excelWorkSheet.UsedRange.Rows.Count - 1;

            excelWorkSheet.Columns.AutoFit();
            for (int dataRowNum = 1; dataRowNum < data.Length; dataRowNum++)
            {
                string dataStr = data[dataRowNum];

                //int sheetRow = dataRowNum + topXmlRow -1;
                int sheetRow = dataRowNum - 1;
                if (sheetRow % 1000 == 0)
                    JobContext.DisplayStopwatch("Process diff for col = " + thisCol + " row = " + sheetRow);

                if (data[dataRowNum].Contains(NOT_EQ_DATA))
                {
                    string cellValue1 = (string)(excelWorkSheet.Cells[topXmlRow + sheetRow, thisCol] as Range).Value;

                    excelWorkSheet.Cells[sheetRow, diffColumn] = "DIFF";
                    string newValue1 = cellValue1.Replace(NOT_EQ, "");
                    excelWorkSheet.Cells[sheetRow, thisCol] = newValue1;

                    string cellValue2 = (string)(excelWorkSheet.Cells[sheetRow, otherCol] as Range).Value;
                    string newValue2 = cellValue2.Replace(NOT_EQ, "");
                    excelWorkSheet.Cells[sheetRow, otherCol] = newValue2;
                    SetBackgroundColor(sheetRow, thisCol, XlRgbColor.rgbSkyBlue);
                    SetBackgroundColor(sheetRow, otherCol, XlRgbColor.rgbSkyBlue);
                    continue;
                }

                if (data[dataRowNum].Contains("IMPORTED"))
                {
                    string cellValue1 = (string)(excelWorkSheet.Cells[topXmlRow + sheetRow, thisCol] as Range).Value;
                    // mark it as diff in extra col 
                    excelWorkSheet.Cells[sheetRow, diffColumn] = "DIFF";
                    // Remove "NOT FOUND IN OPPOSITE COL"
                    int start = cellValue1.IndexOf('<') + 1;
                    int end = cellValue1.IndexOf(' ', start);
                    string tag = "/" + cellValue1.Substring(start, end - start);
                    excelWorkSheet.Cells[sheetRow, thisCol] = " ";
                    while (cellValue1.Contains(tag) == false)
                    {
                        excelWorkSheet.Cells[sheetRow, thisCol] = " ";
                        SetBackgroundColor(sheetRow, thisCol, XlRgbColor.rgbSilver);
                        sheetRow++;
                        dataRowNum++;
                        cellValue1 = (string)(excelWorkSheet.Cells[sheetRow, thisCol] as Range).Value;
                    }
                    excelWorkSheet.Cells[sheetRow, thisCol] = " ";
                    SetBackgroundColor(sheetRow, thisCol, XlRgbColor.rgbSilver);
                }
            }
        }



        public void ProcessDiffsByData()
        {

            ProcessDiffsImportedUsingData(leftXmlColumn, rightXmlColumn, dataArray1);
            ProcessDiffsImportedUsingData(rightXmlColumn, leftXmlColumn, dataArray2);
        }

        public void ProcessDiffsBySpreadsheet()
        {

            ProcessDiffsImported(leftXmlColumn, rightXmlColumn);
            ProcessDiffsImported(rightXmlColumn, leftXmlColumn);
        }

        private void SetBackgroundColor(int row, int col, XlRgbColor color)
        {
            var columnHeadingsRange = excelWorkSheet.Range[excelWorkSheet.Cells[row, col], excelWorkSheet.Cells[row, col]];
            columnHeadingsRange.Interior.Color = color;
        }

        private string GetExcelColumnName(int columnNumber)
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

            return columnName;
        }

        private string GetExcelCellAddress(int rowNumber, int columnNumber)
        {
            string str = "";
            string colStr = GetExcelColumnName(columnNumber);

            str = colStr + rowNumber;
            return str;
        }

        public void PopulateHeader(int columnOrder, string text)
        {
            int columnNumber;
            if (columnOrder == 1)
                columnNumber = leftXmlColumn;
            else
                columnNumber = rightXmlColumn;

            excelWorkSheet.Cells[headerRow, columnNumber] = text;
        }

        public void PasteXmlCol(int columnOrder, string fileName)
        {
            Range rng;
            int columnNumber;

            string reportContent = File.ReadAllText(fileName);

            if (columnOrder == 1)
            {
                dataArray1 = reportContent.Split('\n');
                columnNumber = leftXmlColumn;
            }
            else
            {
                dataArray2 = reportContent.Split('\n');
                columnNumber = rightXmlColumn;
            }

            rng = excelWorkSheet.get_Range(GetExcelCellAddress(topXmlRow, columnNumber), Missing.Value);

            

            Clipboard.SetData(DataFormats.Text, reportContent);
            Thread.Sleep(2000);
            rng.PasteSpecial(XlPasteType.xlPasteAll);
        }

        internal void DrawDivider()
        {

           // Microsoft.Office.Interop.Excel.Range range1 = excelWorkSheet.Range["B2"];
           // excelWorkSheet.Range["B" + usedRowCount + 5, range1].Interior.Color = XlRgbColor.rgbLightGrey;
           // excelWorkSheet.Columns[2].ColumnWidth = 1;
            //for (int i = 3; i < usedRowCount + 5; i++ )
            //    SetBackgroundColor(i, 2, XlRgbColor.rgbLightGreen);



          //  Range rng = excelWorkSheet.UsedRange.Columns[2, Type.Missing].Rows.Count;
           // Range rng = excelWorkSheet.UsedRange.Columns["B:B", Type.Missing].Rows.Count;
         //   Range rng = excelWorkSheet.UsedRange;
                
                //excelApp.get_Range(excelWorkSheet.Cells[1, 2], excelWorkSheet.Cells[usedRowCount, 2]);

        //    rng = excelWorkSheet.get_Range(excelWorkSheet.Cells[1, 1], excelWorkSheet.Cells[3, 3]);
           //rng.Interior.Color = XlRgbColor.rgbLightGreen;
        }

        public static DataSet WorkbookToDataSet(string path)
        {
            Microsoft.Office.Interop.Excel.Application excelApp;
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook;
            //Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet;
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
                    if (singleDValue[1] != null)
                         dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
                }

                ds.Tables.Add(dt);
            }

           // xlWorkbook.Close();
           // excelApp.Quit();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Clean up references to all COM objects
            // As per above, you're just using a Workbook and Excel Application instance, so release them:
            xlWorkbook.Close(false, Missing.Value, Missing.Value);
            excelApp.Quit();
            Marshal.FinalReleaseComObject(xlWorkbook);
            Marshal.FinalReleaseComObject(xlWorkbook);

            return ds;
        }
    }
}
