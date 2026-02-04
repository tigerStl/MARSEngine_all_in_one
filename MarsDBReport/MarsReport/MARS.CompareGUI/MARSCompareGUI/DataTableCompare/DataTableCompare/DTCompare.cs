using ClosedXML.Excel;
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

        public DTCompare(DataTable dt1, DataTable dt2, string[] allFieldNames, string[] keyFieldNames, string[] showFieldNames, string[] compareFieldNames, string outputFlieName)
        {
            this.dt1 = dt1;
            this.dt2 = dt2;
            this.allFieldNames = allFieldNames;
            this.keyFieldNames = keyFieldNames;
            this.showFieldNames = showFieldNames;
            this.compareFieldNames = compareFieldNames;
            this.outputFlieName = outputFlieName;
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

        public void Compare()
        {
            HashDataTable hdt1 = new HashDataTable(dt1, allFieldNames, keyFieldNames);
            HashDataTable hdt2 = new HashDataTable(dt2, allFieldNames, keyFieldNames);

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

            ApplyFilters(cr, applyFilter, outputFilter, orderBy);

            GenerateReport(cr);
            Console.WriteLine("DONE");
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
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(cr.ResultDataTable, "Result");
            wb.Worksheets.Add(cr.SummaryDataTable, "Summary");
            wb.Worksheets.Add(cr.InFirstOnlyDataTable, "In First Only");
            wb.Worksheets.Add(cr.InSecondOnlyDataTable, "In Second Only");

            //var ws = wb.Worksheet("Result");

            ColorizeReport(cr, wb);

            /*
            var notEqualList = from e in cr.errorReport.errorList
                               where e.ErrorStatus != CompareErrorLineItem.Status.Equal
                               select e;

           
            foreach (var item in notEqualList)
            {
               // ws.Cell(item.LineNumber + 2, cr.StatusColNumber).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF00FF);
                ws.Row(item.LineNumber + 2).Style.Fill.BackgroundColor = XLColor.Pink;
                if (item.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
                {
                    foreach (int colNum in item.ErrorColumns)
                    {
                        ws.Cell(item.LineNumber + 2, colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;
                        ws.Cell(item.LineNumber + 2, cr.RightSideFirstColNumber + colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;

                        // AF Rich Text
                       ApplyDiffCharColor(ws.Cell(item.LineNumber + 2, colNum + 1), ws.Cell(item.LineNumber + 2, cr.RightSideFirstColNumber + colNum + 1));
                     }
                }
            }
            */
           

            if (File.Exists(outputFlieName))
                File.Delete(outputFlieName);
            
            wb.SaveAs(outputFlieName, false);
        }

        private void ColorizeReport(CompareResult cr, XLWorkbook wb)
        {
            int rowNum = 0;
            var ws = wb.Worksheet("Result");
            List<int> itemIdList = (from row in  cr.ResultDataTable.AsEnumerable()
                              select row.Field<int>(cr.LINE_ITEM_ID)).ToList<int>();

            var notEqualList = from e in cr.errorReport.errorList
                               where e.ErrorStatus != CompareErrorLineItem.Status.Equal
                               select e;

            foreach (var itemId in itemIdList)
            {
                rowNum++;

                var item = (from itm in notEqualList
                            where itm.LineId == itemId
                            select itm).FirstOrDefault();

                if (item == null)
                    continue;

                ws.Row(rowNum + 1).Style.Fill.BackgroundColor = XLColor.Pink;
                if (item.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
                {
                    foreach (int colNum in item.ErrorColumns)
                    {
                        ws.Cell(rowNum + 1, colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;
                        ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;

                        // AF Rich Text
                        ApplyDiffCharColor(ws.Cell(rowNum + 1, colNum + 1), ws.Cell(rowNum + 1, cr.RightSideFirstColNumber + colNum + 1));
                    }
                }

                
            }

                
           /* 
            foreach (var item in notEqualList)
            {
                // ws.Cell(item.LineNumber + 2, cr.StatusColNumber).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF00FF);
                ws.Row(item.LineNumber + 2).Style.Fill.BackgroundColor = XLColor.Pink;
                if (item.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
                {
                    foreach (int colNum in item.ErrorColumns)
                    {
                        ws.Cell(item.LineNumber + 2, colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;
                        ws.Cell(item.LineNumber + 2, cr.RightSideFirstColNumber + colNum + 1).Style.Fill.BackgroundColor = XLColor.Yellow;

                        // AF Rich Text
                        ApplyDiffCharColor(ws.Cell(item.LineNumber + 2, colNum + 1), ws.Cell(item.LineNumber + 2, cr.RightSideFirstColNumber + colNum + 1));
                    }
                }
            }
            */

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

                    if (value1.Equals(value2) == false)
                    {
                        lineItem.ErrorStatus = CompareErrorLineItem.Status.NotEqual;
                       

                        int showFieldIndex = showFieldNames.ToList<string>() .FindIndex(a => a == columnName);

                        lineItem.ErrorColumns.Add(showFieldIndex);
                        lineItem.ErrorMessage += lineItem.ErrorMessage + columnName + "_1" + "=" + value1 + ", " + columnName + "_2" + "=" + value2 + ";   ";
                       
                    }
                }
            }


            return lineItem;
        }
    }
}
