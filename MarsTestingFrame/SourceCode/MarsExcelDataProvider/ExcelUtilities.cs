using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace MarsExcelDataProvider
{
    public class ExcelUtilities
    {
        public static void ExportDataSetToExcel(DataSet ds, string filePath, bool includeHeaders)
        {
            Excel.Application excelApp = new Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook excelWorkBook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

            excelWorkBook.Sheets[1].Name = "Extra";

            foreach (DataTable table in ds.Tables)
            {
                Excel.Worksheet excelWorkSheet = excelWorkBook.Sheets.Add();
                excelWorkSheet.Name = table.TableName;

                int startingRowNum = 1;
                if (includeHeaders)
                {
                    for (int i = 1; i < table.Columns.Count + 1; i++)
                    {
                        excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                    }
                    startingRowNum = 2;
                }

                for (int rowNum = 0; rowNum < table.Rows.Count; rowNum++)
                {
                    for (int colNum = 0; colNum < table.Columns.Count; colNum++)
                    {
                        excelWorkSheet.Cells[rowNum + startingRowNum, colNum + 1] = table.Rows[rowNum].ItemArray[colNum].ToString();

                    }
                }
                excelWorkSheet.Columns.AutoFit();
                excelWorkSheet.UsedRange.NumberFormat = "@";
            }

            excelWorkBook.Sheets["Extra"].Delete();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            excelWorkBook.SaveAs(filePath, Excel.XlFileFormat.xlAddIn);
            excelWorkBook.Close();
            excelApp.Quit();

        }

        public static DataSet ImportExcelToDS(string path)
        {
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook excelWorkBook = null;
            DataSet ds = new DataSet();
            try
            {
                excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelWorkBook = excelApp.Workbooks.Open(path);                

                foreach (Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet in excelWorkBook.Worksheets)
                {
                    int rows = excelWorkSheet.UsedRange.Rows.Count;
                    int cols = excelWorkSheet.UsedRange.Columns.Count;
                    DataTable dt = new DataTable();

                    dt.TableName = excelWorkSheet.Name;
                    int noofrow = 1;

                    //If 1st Row Contains unique Headers for datatable include this part else remove it
                    
                    for (int col = 1; col <= cols; col++)
                    {
                        string colname = excelWorkSheet.Cells[1, col].Text;
                        dt.Columns.Add(colname);
                        noofrow = 2;
                    }
                   
                    for (int row = noofrow; row <= rows; row++)
                    {
                        DataRow dataRow = dt.NewRow();
                        for (int col = 1; col <= cols; col++)
                        {
                            dataRow[col - 1] = excelWorkSheet.Cells[row, col].Text;
                        }
                        dt.Rows.Add(dataRow);
                    }
                    ds.Tables.Add(dt);
                }
                excelWorkBook.Close();
                excelApp.Quit();
            }
            catch (Exception )
            {
                excelWorkBook.Saved = true;
                excelWorkBook.Close();
                excelApp.Quit();
            }
            return ds;
        }
 
    }
}
