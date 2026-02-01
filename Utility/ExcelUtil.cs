
using Microsoft.Office.Interop.Excel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mars.message.Utility
{
    public class ExcelUtil
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ExcelUtil));

        public static DataSet WorkbookToDataSet(string path, string dataType)
        {
            Logger.logBegin("WorkbookToDataSet");
            Logger.Info("WorkbookToDataSet", string.Format("Path:[{0}],dataType:[{1}]", path, dataType));
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = null;
            try
            {
                DataSet ds = new DataSet();
                excelApp = new Microsoft.Office.Interop.Excel.Application();

                //Create an Excel workbook instance and open it from the predefined location
                //xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

                xlWorkbook = excelApp.Workbooks.Open(path);
                Logger.Info("WorkbookToDataSet", string.Format("SheetCount:[{0}]", xlWorkbook.Sheets == null ? -1 : xlWorkbook.Sheets.Count));
                foreach (Microsoft.Office.Interop.Excel.Worksheet sheet in xlWorkbook.Sheets)
                {
                    string sheetName = sheet.Name;
                    Logger.Info("WorkbookToDataSet", string.Format("Sheet:[{0}]", sheetName));

                    Range excelRange = sheet.UsedRange;
                    object[,] valueArray = (object[,])excelRange.get_Value(XlRangeValueDataType.xlRangeValueDefault);

                    // skip empty worksheets
                    if (valueArray == null)
                        continue;

                    System.Data.DataTable dt = new System.Data.DataTable(sheetName);

                    int startingRow = 1;
                    //Get the column names

                    if (dataType.Equals("DATA"))
                    {
                        for (int k = 0; k < valueArray.GetLength(1);)
                        {
                            //add columns to the data table.
                            string value = (string)valueArray[1, ++k];
                            //if (value == null || value.Trim().Length == 0)
                            //    break;
                            string colName;
                            if (k == 1)
                                colName = "Object Name";
                            else
                                colName = "Data Set " + (k - 1);

                            dt.Columns.Add(colName);
                        }
                        startingRow = 0;
                    }
                    else
                    {
                        for (int k = 0; k < valueArray.GetLength(1);)
                        {
                            //add columns to the data table.
                            string value = (string)valueArray[1, ++k];
                            if (value == null || value.Trim().Length == 0)
                                break;
                            dt.Columns.Add(value);
                        }
                    }


                    //Load data into data table
                    object[] singleDValue = new object[dt.Columns.Count];
                    //value array first row contains column names. so loop starts from 1 instead of 0
                    for (int i = startingRow; i < valueArray.GetLength(0); i++)
                    {
                        for (int k = 0; k < dt.Columns.Count;)
                        {
                            singleDValue[k] = valueArray[i + 1, ++k];
                        }
                        // skip comments
                        if (dataType.Equals("OBJ"))
                        {
                            if (singleDValue[1] != null && singleDValue[2] != null)
                                dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
                        }

                        else
                            dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
                    }

                    ds.Tables.Add(dt);
                }
                return ds;
                // xlWorkbook.Close();
            }
            finally
            {
                if (xlWorkbook != null)
                    xlWorkbook.Close(false, Type.Missing, Type.Missing);
                if (excelApp != null)
                    excelApp.Quit();
                Logger.logEnd("WorkbookToDataSet");
            }
        }

        private static bool GetRowIdxColIdxForExcel(string strRngInfo, ref string strRx, ref string strCol, ref string strError)
        {
            strRx = "";
            strCol = "";
            int idx = 0;
            char c;
            if (!Regex.IsMatch(strRngInfo, cnst_rangeFormatPattern))
            {
                strError = string.Format("Range format is not right, Characters and number are required. but [{0}] find ", strRngInfo);
                return false;
            }
            while (idx < strRngInfo.Length)
            {
                c = strRngInfo[idx];
                if (((c >= 'a') && (c <= 'z'))
                    || ((c >= 'A') && (c <= 'Z')))
                {
                    strCol += c;
                }
                if (c >= '0' && c <= '9')
                {
                    strRx += c;
                }
                idx++;
            }

            return string.IsNullOrEmpty(strRx) && string.IsNullOrEmpty(strCol);
        }

        private static int GetCharaterIndxForColIdx(string strIndx)
        {
            int iRslt = 0;
            int iPow = 1;
            for (int i = 0; i < strIndx.Length; i++)
            {
                char c = strIndx[i];
                if ((c >= 'a') && (c <= 'z'))
                {
                    iRslt += ((c - 'a' + 1) * iPow);

                }
                else
                    iRslt += ((c - 'A' + 1) * iPow);
                iPow *= 26;
            }
            return iRslt;
        }

        private const string cnst_rangeFormatPattern = "[a-zA-Z]{1,}[1-9]{1}[0-9]{0,}";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFilePath"></param>
        /// <param name="strRanges">format -  sheetName:Range1:Range2</param>
        /// <param name="strError"></param>
        /// <param name="objExcelApp"></param>
        /// <returns></returns>
        internal static bool CopyRangeValueToClipBoard(string strFilePath, string strRanges, ref string strError, Microsoft.Office.Interop.Excel.Application objExcelApp = null)
        {
            Logger.Info("CopyRangeValueToClipBoard", string.Format("FilePath:[{0}], strRanges:[{1}]", strFilePath, strRanges));

            if (objExcelApp == null)
            {
                objExcelApp = new Microsoft.Office.Interop.Excel.Application();
            }
            try
            {
                Workbook objWbCurrent = objExcelApp.Workbooks.Open(strFilePath);
                string[] arrRangeInfo = strRanges.Split(new string[] { ":" }, StringSplitOptions.None);
                int iRngParaCnt = arrRangeInfo.Length;
                if (iRngParaCnt < 2)
                {
                    Logger.Error("CopyRangeValueToClipBoard", strError = string.Format("Range format should be- sheetName:Range1:Range2, but the value is :[{0}]", strRanges));
                    return false;
                }
                string strSheetName = arrRangeInfo[0];
                string strRngCellIdx = strRanges.Substring(strSheetName.Length + 1);

                if (!Regex.IsMatch(arrRangeInfo[1], cnst_rangeFormatPattern, RegexOptions.IgnoreCase))
                {
                    Logger.Error("CopyRangeValueToClipBoard", strError = string.Format("Start Range Format of Range is not right"));
                }
                if (iRngParaCnt == 3)
                    if (!Regex.IsMatch(arrRangeInfo[2], cnst_rangeFormatPattern))
                    {
                        Logger.Error("CopyRangeValueToClipBoard", strError = string.Format("End Range Format of Range is not right"));
                    }

                foreach (Worksheet objSht in objWbCurrent.Sheets)
                {
                    if (string.Compare(strSheetName, objSht.Name, true) == 0)
                    {
                        //objWbCurrent.ActiveSheet(objWbCurrent.Sheets[i]);
                        //objSht = objWbCurrent.Sheets[i];

                        Range objUsedRg = objSht.UsedRange;

                        //Range objTarget = objUsedRg.Cells[strRngCellIdx]

                        string strRowIdx = "", strColIdx = "";
                        GetRowIdxColIdxForExcel(arrRangeInfo[1], ref strRowIdx, ref strColIdx, ref strError);

                        int iColIdx = GetCharaterIndxForColIdx(strColIdx), iRowIdx;
                        int.TryParse(strRowIdx, out iRowIdx);
                        Range objStartRng = objSht.Cells[iRowIdx, iColIdx];
                        Range objEndRng = null;
                        if (iRngParaCnt == 2)
                        {
                            objEndRng = objSht.Cells[objUsedRg.Rows.Count, strColIdx];
                        }
                        if (iRngParaCnt == 3)
                        {
                            GetRowIdxColIdxForExcel(arrRangeInfo[2], ref strRowIdx, ref strColIdx, ref strError);
                            iColIdx = GetCharaterIndxForColIdx(strColIdx);
                            int.TryParse(strRowIdx, out iRowIdx);
                            objEndRng = objSht.Cells[iRowIdx, iColIdx];
                        }

                        Range objRngTarget = objSht.Range[objStartRng, objEndRng];
                        objRngTarget.Copy();
                        return true;
                    }
                }
                Logger.Error("CopyRangeValueToClipBoard", strError = string.Format("No such Sheet [{0}] available for the source Excel file.", strFilePath));
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("CopyRangeValueToClipBoard", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        internal static Microsoft.Office.Interop.Excel.Application GetExcelApplication()
        {
            return new Microsoft.Office.Interop.Excel.Application();
        }

        internal static void CloseExcelApp(Microsoft.Office.Interop.Excel.Application objExcelApp)
        {
            if (objExcelApp.ActiveWorkbook != null)
            {
                objExcelApp.ActiveWorkbook.Close();
            }
            objExcelApp.Quit();
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


        public static void WorkbookXLStoXLSX(string path)
        {
            Logger.logBegin("WorkbookXLStoXLSX");
            Logger.Info("WorkbookXLStoXLSX", string.Format("Path:[{0}]", path));
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = null;
            try
            {
                excelApp = new Microsoft.Office.Interop.Excel.Application();

                //Create an Excel workbook instance and open it from the predefined location
                //xlWorkbook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);

                xlWorkbook = excelApp.Workbooks.Open(path);
                xlWorkbook.SaveAs(path + 'x', XlFileFormat.xlWorkbookDefault);
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
