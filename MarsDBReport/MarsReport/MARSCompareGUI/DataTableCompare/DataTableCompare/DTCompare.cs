using ClosedXML.Excel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class DTCompare
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DTCompare));

        private DataTable dt1;
        private DataTable dt2;
        private string[] allFieldNames;
        private string[] keyFieldNames;
        private string[] showFieldNames;
        private string[] compareFieldNames;
        private string outputFlieName;
        public string outputFilter;
        public string orderBy;
        public bool applyFilter;
        Dictionary<string, ToleranceConfig> tMap;
        public XLWorkbook workbook;
        public CompareConfig compareConfig;

        public string templateFile { get; set; }

        public DTCompare(DataTable dt1, DataTable dt2, string[] allFieldNames, string[] keyFieldNames, string[] showFieldNames, string[] compareFieldNames, Dictionary<string, ToleranceConfig> tolerancMap,  string outputFlieName, XLWorkbook workbook = null)
        {
            this.dt1 = dt1;
            this.dt2 = dt2;
            this.allFieldNames = allFieldNames;
            this.keyFieldNames = keyFieldNames;
            this.showFieldNames = showFieldNames;
            this.compareFieldNames = compareFieldNames;
            this.outputFlieName = outputFlieName;
            this.tMap = tolerancMap;
            this.workbook = workbook;
            compareConfig = new CompareConfig();
        }
        

        private void FindTest(HashDataTable hdt1, HashDataTable hdt2)
        {
            var keys = hdt1.dict.Keys;

            foreach (var key in keys)
            {
                if (hdt2.dict.Keys.Contains(key))
                {
                    DataRow dr = hdt2.dict[key];
                }
                else
                {
                    Console.WriteLine("key not found");
                }
            }
        }

        public bool Compare(out bool Status, out string Message)
        {
            Logger.logBegin("Compare");
            if (dt1 == null)
            {
                Message = "DataTable for baseline was not generated";
                Status = false;
                Logger.Error("Compare", Message);
                return false;
            }
            if (dt2 == null)
            {
                Message = "DataTable for compare was not generated";
                Status = false;
                Logger.Error("Compare", Message);
                return false;
            }
            HashDataTable hdt1 = new HashDataTable(dt1, allFieldNames, keyFieldNames);
            HashDataTable hdt2 = new HashDataTable(dt2, allFieldNames, keyFieldNames);

            Status = true;
            Message = "No Errors";

           CompareResult cr = new CompareResult();

            cr.Init(allFieldNames, showFieldNames, keyFieldNames);

            // Compare Left to right
            var keys = hdt1.dict.Keys;

            foreach (var key in keys)
            {
                DataRow dr1 = hdt1.dict[key];
                CompareErrorLineItem lineItem = new CompareErrorLineItem();
                if (hdt2.dict.Keys.Contains(key))
                {
                    DataRow dr2 = hdt2.dict[key];
                    CompareTwoRows(dr1, dr2, lineItem);
                }
                else
                {
                    lineItem.ErrorStatus = CompareErrorLineItem.Status.Left;
                    lineItem.dr1 = dr1;
                }

                cr.GenerateEntry(lineItem);
            }

            //  Compare Right to left
            var keys2 = hdt2.dict.Keys;

            foreach (var key in keys2)
            {
                DataRow dr2 = hdt2.dict[key];
                if (hdt1.dict.Keys.Contains(key) == false)
                {
                    CompareErrorLineItem lineItem = new CompareErrorLineItem();
                    lineItem.ErrorStatus = CompareErrorLineItem.Status.Right;
                    lineItem.dr2 = dr2;
                    cr.GenerateEntry(lineItem);
                }
            }
            Logger.Info("Compare", "before ApplyFilters");
            ApplyFilters(cr, applyFilter, outputFilter, orderBy);
            var errors = cr.ResultDataTable.Select("REP_Status = 'FAIL'");
            Logger.Info("Compare", "before GenerateReport");
            GenerateReport(cr);
            //Console.WriteLine("DONE");

            if (errors.Length > 0)
            {
                Status = false;
                Message = "Number of Errors: " + errors.Length;
            }
            Logger.logEnd("Compare", $"Errors count|{errors?.Length}");
            return Status;
        }

        private void ApplyFilters(CompareResult cr, bool applyFilter, string outputFilter, string orderBy)
        {
            if (applyFilter)
            {
                //cr.ResultDataTable = cr.ResultDataTable.Select();
                DataTable dataTable = new DataTable();
                dataTable = cr.ResultDataTable.Clone();
                 var rowArray = cr.ResultDataTable.Select(outputFilter, orderBy);

                foreach (DataRow row in rowArray)
                {
                    dataTable.ImportRow(row);
                }

                cr.ResultDataTable = dataTable;
            }
        }

        private void GenerateReport(CompareResult cr)
        {
            Logger.logBegin("GenerateReport", $"allfields|{cr.allFieldNames}");
            XLWorkbook wb;
            if (workbook != null)
            {
                wb = workbook;
                wb.Worksheets.Add(cr.ResultDataTable, "Result");
            }
            else
            {
                wb = new XLWorkbook();
                List<string> errorColumnList = new List<string>();
                foreach (DataRow dRow in cr.ResultDataTable.Rows)
                {
                    // limit the amount of data in error field
                    if (dRow["REP_Error"].ToString().Length > 1000)
                        dRow["REP_Error"] = dRow["REP_Error"].ToString().Substring(0,1000);

                    if (dRow["REP_Error"].ToString().Length == 0)
                    {
                        dRow["Type of Difference"] = "Known";
                        dRow["Reason for Difference"] = "All expected fields match.";
                    }
                    else
                        AddErrorColumns(errorColumnList, dRow["REP_Error"].ToString());
                }

                foreach (var errCol in errorColumnList)
                    cr.ResultDataTable.Columns.Add("ERR_" + errCol);

                cr.InFirstOnlyDataTable.Columns.Add("Type of Difference", typeof(String));
                cr.InFirstOnlyDataTable.Columns.Add("Reason for Difference", typeof(String));
                cr.InFirstOnlyDataTable.Columns.Add("Notes", typeof(String));
                cr.InSecondOnlyDataTable.Columns.Add("Type of Difference", typeof(String));
                cr.InSecondOnlyDataTable.Columns.Add("Reason for Difference", typeof(String));
                cr.InSecondOnlyDataTable.Columns.Add("Notes", typeof(String));

                wb.Worksheets.Add(cr.ResultDataTable, "Result");
                wb.Worksheets.Add(cr.SummaryDataTable, "Summary");
                wb.Worksheets.Add(cr.InFirstOnlyDataTable, "In First Only");
                wb.Worksheets.Add(cr.InSecondOnlyDataTable, "In Second Only");
                // AF 2024
                RemoveLeadingSpaces(wb);
                
                var wbTemplate = new XLWorkbook(templateFile);
                wbTemplate.Worksheet(1).CopyTo(wb, "TOC");
                wb.Worksheet("TOC").Position = 1;


                var errorCount = cr.ResultDataTable.Select("REP_Status = 'FAIL'").Length;
                PopulateTOC(wb, outputFlieName, errorCount);
                wb.Worksheets.Add("Analysis Overview");
                wb.Worksheet("Analysis Overview").Position = 2;
                wb.Worksheets.Add("ISSUE EXPLANATION ");
                wb.Worksheet("ISSUE EXPLANATION ").Position = 7;
            }
            Logger.Info("GenerateReport", "before ColorizeReport");
            ColorizeReport(cr, wb);
            
            // if workbook was created externally, don't save it
            if (workbook != null)
                return;

            if (File.Exists(outputFlieName))
                File.Delete(outputFlieName);

            if (outputFlieName.ToLower().EndsWith(".xlsx") == false)
                outputFlieName += ".xlsx";
            wb.SaveAs(outputFlieName, false);
        }

        private void AddErrorColumns(List<string> errorColumnList, string errorString)
        {
            var errors = errorString.Split(("  ").ToCharArray(), StringSplitOptions.None);
            foreach(var row in errors)
            {
                var parts = row.Split(("_1").ToCharArray(), StringSplitOptions.None);
                if (parts[0].Length > 0 && errorColumnList.Contains(parts[0].ToUpper()) == false)
                    errorColumnList.Add(parts[0].ToUpper());
            }
        }

        private void PopulateTOC(XLWorkbook wb, string outputFlieName, int errorCount)
        {
            if (compareConfig.CompareTypeBaseline == "DATABASE")
            {
                RemoveRowContainingTag(wb, "FileLocationBaseline");
            }
            else
            {
                RemoveRowContainingTag(wb, "DBConnectionNameBaseline");
                RemoveRowContainingTag(wb, "DBConnectionDetailsBaseline");
                RemoveRowContainingTag(wb, "QueryIdBaseline");
                RemoveRowContainingTag(wb, "QueryBaseline");
            }

            compareConfig.Status = "Number of errors: " + errorCount;

            ReplceTagWithValue(wb, "ConfigID", compareConfig.ConfigID);
            ReplceTagWithValue(wb, "Client", compareConfig.Client);
            ReplceTagWithValue(wb, "ConnectionURL", compareConfig.ConnectionURL);
            ReplceTagWithValue(wb, "ExecutionHost", compareConfig.ExecutionHost);
            ReplceTagWithValue(wb, "ExTime", compareConfig.ExTime);
            ReplceTagWithValue(wb, "Status", compareConfig.Status);
            ReplceTagWithValue(wb, "ReportFileLocation", outputFlieName);
            ReplceTagWithValue(wb, "InstanceVersionBaseline", compareConfig.InstanceVersionBaseline);
            ReplceTagWithValue(wb, "InstanceNameBaseline", compareConfig.InstanceNameBaseline);
            ReplceTagWithValue(wb, "CompareTypeBaseline", compareConfig.CompareTypeBaseline);
            ReplceTagWithValue(wb, "DBConnectionNameBaseline", compareConfig.DBConnectionNameBaseline);
            ReplceTagWithValue(wb, "DBConnectionDetailsBaseline", compareConfig.DBConnectionDetailsBaseline);
            ReplceTagWithValue(wb, "QueryIdBaseline", compareConfig.QueryIdBaseline);
            ReplceTagWithValue(wb, "QueryBaseline", compareConfig.QueryBaseline);
            ReplceTagWithValue(wb, "FileLocationBaseline", compareConfig.FileLocationBaseline);
            ReplceTagWithValue(wb, "InstanceVersionTarget", compareConfig.InstanceVersionTarget);
            ReplceTagWithValue(wb, "InstanceNameTarget", compareConfig.InstanceNameTarget);
            ReplceTagWithValue(wb, "CompareTypeTarget", compareConfig.CompareTypeTarget);
            ReplceTagWithValue(wb, "DBConnectionNameTarget", compareConfig.DBConnectionNameTarget);
            ReplceTagWithValue(wb, "DBConnectionDetailsTarget", compareConfig.DBConnectionDetailsTarget);
            ReplceTagWithValue(wb, "QueryIdTarget", compareConfig.QueryIdTarget);
            ReplceTagWithValue(wb, "QueryTarget", compareConfig.QueryTarget);
            ReplceTagWithValue(wb, "FileLocationTarget", compareConfig.FileLocationTarget);
            ReplceTagWithValue(wb, "KeyFields", compareConfig.KeyFields);
            ReplceTagWithValue(wb, "ShowFields", compareConfig.ShowFields);
            ReplceTagWithValue(wb, "CompareFields", compareConfig.CompareFields);

           

        }

        private void RemoveLeadingSpaces(XLWorkbook wb)
        {
            RemoveLeadingSpaces(wb, "Summary");
            RemoveLeadingSpaces(wb, "In First Only");
            RemoveLeadingSpaces(wb, "In Second Only");
        }

        internal void  ReplceTagWithValue(XLWorkbook wb, string tag, string value)
        {
            try
            {
                IXLCell foundCell = wb.Worksheets.Worksheet(1).CellsUsed(cell => cell.GetString() == "[" + tag + "]").First();
                if (foundCell != null)
                    foundCell.Value = value;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ReplceTagWithValue for tag " + tag);
            }
            
        }

        internal void RemoveRowContainingTag(XLWorkbook wb, string tag)
        {
            try
            {
                IXLCell foundCell = wb.Worksheets.Worksheet(1).CellsUsed(cell => cell.GetString() == "[" + tag + "]").First();
                if (foundCell != null)
                    wb.Worksheets.Worksheet(1).Row(foundCell.Address.RowNumber).Delete(); ;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ReplceTagWithValue for tag " + tag);
            }

        }

        private void RemoveLeadingSpaces(XLWorkbook wb, string name)
        {
            var sheet = wb.Worksheet(name);
            var rows = sheet.RangeUsed().RowsUsed();
            foreach (var row in rows)
            {
                foreach (IXLCell cell in row.Cells())
                {
                    string value = cell.Value.ToString();
                    cell.Value = value.Trim();
                }
            }
        }

        private void ColorizeReport(CompareResult cr, XLWorkbook wb)
        {
            int rowNum = 0;
            IXLWorksheet ws = wb.Worksheet("Result");
            int col = 1;

            // coloration for compare headers
            int separatorCol = 0;
            while (ws.Cell(1, col).Value.ToString().Length >= 3)
            {
                if (ws.Cell(1, col).Value.ToString().EndsWith("_2"))
                    ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.AmericanRose;
                string colName = ws.Cell(1, col).Value.ToString();
                if ((ws.Cell(1, col).Value.ToString().Equals("SEPARATOR")))
                    separatorCol = col;
                col++;
            }
            if (separatorCol > 1)
                ws.Column(separatorCol).Width = 3;

            int totalRows = ws.RowsUsed().Count();
            var range1 = ws.Range(1, separatorCol, totalRows, separatorCol);
           // range1.Style.Fill.BackgroundColor = XLColor.Gray;
            

            List<int> itemIdList = (from row in  cr.ResultDataTable.AsEnumerable()
                              select row.Field<int>(cr.LINE_ITEM_ID)).ToList<int>();

            var notEqualList = (from e in cr.errorReport.errorList
                               where e.ErrorStatus != CompareErrorLineItem.Status.Equal
                               select e).ToList();

            // Convert notEqualList to a dictionary
            var notEqualDict = notEqualList.ToDictionary(e => e.LineId, e => e);

            // Todo: 
            // Should not do this if notEqualList is empty;   
            // Convert notEqualList to a Dict;   
            // Optimize the coloration block of code ; 
            // Remove AppllyDiffColor using config

            if (notEqualList.Count != 0)
            {
                var pinkFill = XLColor.FromArgb(236, 210, 219);
                var yellowFill = XLColor.FromArgb(254, 249, 185);
                var lightYellow = XLColor.LightYellow;
                var rowNumStyle = ws.Row(1);
                foreach (var itemId in itemIdList)
                {
                    rowNum++;
                    if (notEqualDict.TryGetValue(itemId, out var item))
                    {
                        var row = ws.Row(rowNum + 1);
                        row.Style.Fill.BackgroundColor = pinkFill; // special pink
                        if (item.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
                        {
                            foreach (int colNum in item.ErrorColumns)
                            {
                                ws.Cell(rowNum + 1, colNum + 1).Style.Fill.BackgroundColor = lightYellow;  // special yellow
                                ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1).Style.Fill.BackgroundColor = yellowFill;

                                // AF Rich Text
                                ApplyDiffCharColor(ws.Cell(rowNum + 1, colNum + 1), ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1));
                            }
                        }
                    }
                }
            }


                /*
                 * This block is replaced with code generated by ChatGPT
                foreach (var itemId in itemIdList)
                {
                    rowNum++;

                    var item = (from itm in notEqualList
                                where itm.LineId == itemId
                                select itm).FirstOrDefault();

                    if (item == null)
                        continue;

                    ws.Row(rowNum + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(236, 210, 219); // special pink
                    if (item.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
                    {
                        foreach (int colNum in item.ErrorColumns)
                        {
                            ws.Cell(rowNum + 1, colNum + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(254,249,185);  // special yellow
                            ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;

                            // AF Rich Text
                            ApplyDiffCharColor(ws.Cell(rowNum + 1, colNum + 1), ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1));
                        }
                    }

                
                }
                */


                         
            range1.Style.Fill.BackgroundColor = XLColor.Gray;

        }

        private void ApplyDiffCharColor(IXLCell leftCell, IXLCell rightCell)
        {
            int adjValue = 0;
            string leftText = leftCell.Value.ToString();
            string rightText = rightCell.Value.ToString();

            // Use Google diff_match_patch to find differences, generate diff map
            DiffCharMap charMap = new DiffCharMap();
            charMap.GenerateMap(leftText, rightText);

            foreach (var hlData in charMap.leftDiffData)
            {
                if (hlData.LineEnd == hlData.LineStart)
                    continue;
                string lindStr = leftText.Substring(hlData.LineStart, hlData.LineEnd - hlData.LineStart - 1);
                leftCell.RichText.Substring(hlData.LineStart, hlData.LineEnd - hlData.LineStart - adjValue).SetFontColor(XLColor.Green).SetBold();
              //  leftCell.RichText.Substring(hlData.LineEnd, hlData.LineEnd +3).SetFontColor(XLColor.Black);
                leftCell.RichText.Substring(hlData.Start, hlData.End - hlData.Start + adjValue).SetFontColor(XLColor.Red).SetBold();
               
            }

            foreach (var hlData in charMap.rightDiffData)
            {
                if (hlData.LineEnd == hlData.LineStart)
                    continue;
                rightCell.RichText.Substring(hlData.LineStart, hlData.LineEnd - hlData.LineStart - adjValue).SetFontColor(XLColor.Green).SetBold();
               // rightCell.RichText.Substring(hlData.LineEnd, hlData.LineEnd + 3).SetFontColor(XLColor.Black);
                rightCell.RichText.Substring(hlData.Start, hlData.End - hlData.Start + adjValue).SetFontColor(XLColor.Red).SetBold();
            }
        }

        private CompareErrorLineItem CompareTwoRows(DataRow dr1, DataRow dr2, CompareErrorLineItem lineItem)
        {
            lineItem.dr1 = dr1;
            lineItem.dr2 = dr2;

            for (int colNum = 0; colNum < dt1.Columns.Count; colNum++ )
            {
                string columnName = dt1.Columns[colNum].ColumnName;
               // if (compareFieldNames.Any(columnName.Contains))
                if (compareFieldNames.Contains(columnName))
                { 
                    string value1 = dr1[columnName].ToString();
                    string value2 = dr2[columnName].ToString();

                    ToleranceConfig toleranceConfig = null;
                    double dValue1;
                    double dValue2;

                    if (tMap != null && tMap.TryGetValue(columnName, out toleranceConfig) && 
                        toleranceConfig.CompareType.Length > 0 &&
                        Double.TryParse(value1, out dValue1)  &&
                        Double.TryParse(value2, out dValue2))
                    {
                        // Compare using tolerance configuration
                        bool isPercentTolerance = false;
                        if (toleranceConfig.CompareType.Equals("P"))
                            isPercentTolerance = true;

                        lineItem.ToleranceConfig = toleranceConfig;

                        try
                        {

                            if (EqualsWithTolerance(dValue1, dValue2, toleranceConfig.ToleranceValue, isPercentTolerance) == false)
                            {
                                if (value1.Equals(value2) == false)
                                {
                                    lineItem.ErrorStatus = CompareErrorLineItem.Status.NotEqual;

                                    int showFieldIndex = showFieldNames.ToList<string>().FindIndex(a => a == columnName);

                                    lineItem.ErrorColumns.Add(showFieldIndex);
                                    if (lineItem.ErrorMessage.Length + value1.Length + value2.Length > 5000)
                                    {
                                        ;
                                       // Console.WriteLine("ErrorMessage line is too long -- skipping");
                                    }
                                    else
                                    {
                                        lineItem.ErrorMessage += lineItem.ErrorMessage + columnName + "_1" + "=" + value1 + ", " + columnName + "_2" + "=" + value2 + "  "; 
                                    }
                                    
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        if (value1.Equals(value2) == false)
                        {
                            lineItem.ErrorStatus = CompareErrorLineItem.Status.NotEqual;

                            int showFieldIndex = showFieldNames.ToList<string>().FindIndex(a => a == columnName);

                            lineItem.ErrorColumns.Add(showFieldIndex);
                            try
                            {
                                if (lineItem.ErrorMessage.Length < 200) // Do not save more than 100 chars of error message
                                lineItem.ErrorMessage += lineItem.ErrorMessage + columnName + "_1" + "=" + value1 + ", " + columnName + "_2" + "=" + value2 + ";   ";
                            }
                            catch (OutOfMemoryException e)
                            {
                                Console.WriteLine("Out of Memory: {0}", e.Message);
                            }
                           
                        }
                    }
                }
            }


            return lineItem;
        }

        public static bool EqualsWithTolerance(double a, double b, double toleranceValue, bool isPercentTolerance)
        {
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);
            double epsilon = toleranceValue;

            if (isPercentTolerance)
            {
                epsilon = diff / absA * 100;
                return epsilon <= toleranceValue;
            }
            

            if (a == b)
            { // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || diff < Double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < epsilon;
            }
            else
            { // use relative error
                //return diff / (absA + absB) < epsilon;
                return diff < epsilon;
            }
        }

    }
}
