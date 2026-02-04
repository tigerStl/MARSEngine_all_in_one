using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Microsoft.Office;
using MSWord = Microsoft.Office.Interop.Word;

using Microsoft.Office.Interop.Word;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Graph;
using Graph = Microsoft.Office.Interop.Graph;
using System.IO;
using System.Runtime.InteropServices;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.autoTest.report.Word
{
    public class WordWrapper
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(WordReportGen));
        public static bool SAVE_AS_PDF = false;

        string topLeft = "A1";
        string bottomRight = "D4";
        string graphTitle = "Test Performance";

        List<Excel.Application> excelAppList = new List<Excel.Application>();
        List<Excel.Workbook> excelWorkbookList = new List<Workbook>();

        object missing = System.Reflection.Missing.Value;
        MSWord.Application wordApp = null;
        Document aDoc = null;

        public List<MSWord.Range> pictureRangeList = new List<MSWord.Range>();

        public void OpenDocument(string filePath)
        {
            wordApp = new MSWord.Application();
            wordApp.Visible = false;

            aDoc = wordApp.Documents.OpenNoRepairDialog(filePath);
            aDoc.ShowSpellingErrors = false;
        }

        public void SaveDocument(string filePath)
        {

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            Logger.Info("SaveDocument", "Save Word doc");
            aDoc.SaveAs(filePath,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing,
                         ref missing);

            if (SAVE_AS_PDF == true)
            {
                object outputFileName = filePath.Replace(".docx", ".pdf");
                object fileFormat = WdSaveFormat.wdFormatPDF;

                // Save document into PDF Format

                if (File.Exists((string)outputFileName))
                {
                    Logger.Info("SaveDocument", "Remove pdf doc");
                    File.Delete((string)outputFileName);
                }

                Logger.Info("SaveDocument", "Save PDF doc");
                aDoc.SaveAs(outputFileName,
                              ref fileFormat,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing,
                              ref missing);
            }
            else
                Logger.Info("SaveDocument", "Save as PDF is not in this version");
            Logger.Info("SaveDocument", "Close Word Doc");

            ((Microsoft.Office.Interop.Word._Document)aDoc).Close(ref missing, ref missing, ref missing);


            Logger.Info("SaveDocument", "Quit Word App");
            ((Microsoft.Office.Interop.Word._Application)wordApp).Quit(ref missing, ref missing, ref missing);
            Logger.Info("SaveDocument", "Release Word App Start");
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(wordApp);
            Logger.Info("SaveDocument", "Release Word App End");
        }

        public void ReplaceTag(string textToFind, string textReplacement)
        {
            ReplaceWord("[" + textToFind + "]", textReplacement);
        }

        public void ReplaceWord(string textToFind, string textReplacement)
        {
            object findText = textToFind;
            foreach (Microsoft.Office.Interop.Word.Range tmpRange in aDoc.StoryRanges)
            {
                tmpRange.Find.Text = textToFind;
                tmpRange.Find.Replacement.Text = textReplacement;
                // tmpRange.Find.Replacement.ParagraphFormat.Alignment =
                //     Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;



                tmpRange.Find.Wrap = Microsoft.Office.Interop.Word.WdFindWrap.wdFindContinue;
                object replaceAll = Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll;

                tmpRange.Find.Execute(ref missing, ref missing, ref missing,
                    ref missing, ref missing, ref missing, ref missing,
                    ref missing, ref missing, ref missing, ref replaceAll,
                    ref missing, ref missing, ref missing, ref missing);
            }
        }

        public MSWord.Range GetLocation(string wordToFind)
        {
            MSWord.Range range = aDoc.Range(0, 0);
            if (range.Find.Execute(wordToFind))
            {
                return range;
            }
            return null;
        }

        ///

        public void CreatePieChartExcel(MSWord.Range chartLocation,
            int passedCount, int failedCount, int partialCount, int unprocessedCount)
        {
            // Open Excel and get first worksheet.
            var application = new Excel.Application();
            application.Visible = false;

            var workbook = application.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);

            excelAppList.Add(application);
            excelWorkbookList.Add(workbook);
            var worksheet = workbook.Worksheets[1] as
                Microsoft.Office.Interop.Excel.Worksheet;
            /*
            worksheet.Cells[1, 1] = "PASS";
            worksheet.Cells[2, 1] = passedCount;
            worksheet.Cells[1, 2] = "FAIL";
            worksheet.Cells[2, 2] = failedCount;
            worksheet.Cells[1, 3] = "UNPROCESSED";
            worksheet.Cells[2, 3] = unprocessedCount;
            worksheet.Cells[1, 4] = "PARTIAL";
            worksheet.Cells[2, 4] = partialCount;
            */


            int colCount = 0;
            // populate data for chart
            if (passedCount > 0)
            {
                worksheet.Cells[1, colCount + 1] = "PASS";
                worksheet.Cells[2, colCount + 1] = passedCount;
                colCount++;
            }

            if (failedCount > 0)
            {
                worksheet.Cells[1, colCount + 1] = "FAIL";
                worksheet.Cells[2, colCount + 1] = failedCount;
                colCount++;
            }

            if (unprocessedCount > 0)
            {
                worksheet.Cells[1, colCount + 1] = "UNPROCESSED";
                worksheet.Cells[2, colCount + 1] = unprocessedCount;
                colCount++;
            }

            if (partialCount > 0)
            {
                worksheet.Cells[1, colCount + 1] = "PARTIAL";
                worksheet.Cells[2, colCount + 1] = partialCount;
                colCount++;
            }

            string rangeLetter = "";

            if (colCount == 1)
            {
                rangeLetter = "A";
            }
            else if (colCount == 2)
            {
                rangeLetter = "B";
            }
            else if (colCount == 3)
            {
                rangeLetter = "C";
            }
            else if (colCount == 4)
            {
                rangeLetter = "D";
            }


            bottomRight = rangeLetter + 2;

            // Add chart.
            var charts = worksheet.ChartObjects() as
                Microsoft.Office.Interop.Excel.ChartObjects;
            var chartObject = charts.Add(60, 10, 600, 300) as
                Microsoft.Office.Interop.Excel.ChartObject;
            var chart = chartObject.Chart;

            // Set chart range.
            var range = worksheet.get_Range(topLeft, bottomRight);
            chart.SetSourceData(range);

            // Set chart properties.
            chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xl3DPieExploded;
            chart.Elevation = 30;
            chart.ClearToMatchStyle();
            // chart.ChartStyle = 259;
            // chart.ChartStyle = 34;

            chart.HasTitle = true;
            chart.ChartTitle.Text = " Test Performance Chart";


            /*
            chart.ApplyDataLabels(
        Excel.XlDataLabelsType.xlDataLabelsShowLabel,
        true, true, 
        true, false, true,
        false, false,  false, false);
        

            chart.ApplyDataLabels(Excel.XlDataLabelsType.xlDataLabelsShowPercent, Excel.XlDataLabelsType.xlDataLabelsShowLabel, true, false, false, true, false, true);
            */

            // this is to fix bug in excel that shows number 1 instead of the actual correct label for cases where there is only one col in spreadsheet
            Excel.DataLabel dl = null; ;
            try
            {
                if (colCount == 1)
                {
                    Excel.Series ss = chart.SeriesCollection(1);
                    dl = ss.DataLabels(1);
                    dl.Text = worksheet.Cells[1, 1].Value.ToString();
                }
            }
            catch (Exception e)
            {
               
            }
            
            chart.ChartArea.Copy();

            // Paste into word doc

            chartLocation.Select();
            Selection sel = wordApp.Selection;
            try
            {
                sel.PasteSpecial();
            }
            catch (Exception) { }
            application.DisplayAlerts = false;
           // workbook.Close();
           // application.Quit();

        }

        public void CloseExcelApps()
        {
            Logger.logBegin("CloseExcelApps");
            GC.Collect();
            Logger.Info("CloseExcelApps", "WaitForPendingFinalizers Begin");
            GC.WaitForPendingFinalizers();
            Logger.Info("CloseExcelApps", "WaitForPendingFinalizers End");

            foreach (var wb in excelWorkbookList)
            {
                wb.Close();
                Marshal.ReleaseComObject(wb);
            }

            excelWorkbookList.Clear();

            foreach (var ex in excelAppList)
            {
                ex.Quit();
                Marshal.ReleaseComObject(ex);
            }
            excelAppList.Clear();
            Logger.logEnd("CloseExcelApps");
        }

        ///

        public void CreatePieChart(MSWord.Range chartLocation,
            int passedCount, int failedCount, int partialCount, int unprocessedCount)
        {

            // http://www.360doc.com/content/11/1031/16/665991_160573635.shtml

            string[,] data = new string[4, 5];
            data[0, 1] = "PASSED";
            data[0, 2] = "FAILED";
            data[0, 3] = "PARTIAL";
            data[0, 4] = "UNPROCESSED";
            data[1, 0] = "EAST";
            data[1, 1] = "" + passedCount;
            data[1, 2] = "" + failedCount;
            data[1, 3] = "" + partialCount;
            data[1, 4] = "" + unprocessedCount;


            object oMissing = System.Reflection.Missing.Value;
            MSWord.InlineShape oShape;
            object oClassType = "MSGraph.Chart";

            // AF Word.Range wrdRng = WordDoc.Bookmarks.get_Item(ref chartLocation).Range;

            MSWord.Range wrdRng = chartLocation;
            oShape = wrdRng.InlineShapes.AddOLEObject(ref oClassType, ref oMissing,

            ref oMissing, ref oMissing, ref oMissing,
            ref oMissing, ref oMissing, ref oMissing);
            //Demonstrate use of late bound oChart and oChartApp objects to
            //manipulate the chart object with MSGraph.
            object oChart;
            object oChartApp;
            oChart = oShape.OLEFormat.Object;
            oChartApp = oChart.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, oChart, null);

            Graph.Application grApp = (Graph.Application)oChartApp;
            grApp.Visible = false;

            //Change the chart type to Line.
            object[] Parameters = new Object[1];
            Parameters[0] = 4; //xlLine = 4
            oChart.GetType().InvokeMember("ChartType", BindingFlags.SetProperty,
            null, oChart, Parameters);
            Graph.Chart objChart = (Graph.Chart)(oShape.OLEFormat.Object);
            
            

            objChart.ChartType = Graph.XlChartType.xlPie;
            

            //objChart.ChartType = Graph.XlChartType.xlPieExploded;

            //objChart.ChartType = Graph.XlChartType.xl3DPieExploded;

            objChart.HasTitle = true;
            objChart.ChartTitle.Caption = "TEST PERFORMANCE CHART";
            objChart.ChartArea.Interior.Color = (int)MSWord.XlRgbColor.xlWhite;
            objChart.PlotArea.Interior.Color = (int)MSWord.XlRgbColor.xlWhite;
            objChart.PlotArea.Border.Color = (int)MSWord.XlRgbColor.xlLightGray;

            // Pie colors
            var series = objChart.SeriesCollection(1);
            var point = series.Points(1);
            point.Interior.Color = (int)MSWord.XlRgbColor.xlGreen;
            point = series.Points(2);
            point.Interior.Color = (int)MSWord.XlRgbColor.xlRed;
            point = series.Points(3);
            point.Interior.Color = (int)MSWord.XlRgbColor.xlPeachPuff;
            point = series.Points(4);
            point.Interior.Color = (int)MSWord.XlRgbColor.xlLightGrey;

            // Data
            DataSheet dataSheet;
            dataSheet = objChart.Application.DataSheet;
            int rownum = data.GetLength(0);
            int columnnum = data.GetLength(1);
            for (int i = 1; i <= rownum; i++)
                for (int j = 1; j <= columnnum; j++)
                {
                    dataSheet.Cells[i, j] = data[i - 1, j - 1];
                }
            objChart.Application.Update();
            oChartApp.GetType().InvokeMember("Update", BindingFlags.InvokeMethod, null, oChartApp, null);
            oChartApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, oChartApp, null);

            oShape.Width = 400;
            oShape.Height = 300;
        }

        private void SetOrientation()
        {
            wordApp.Selection.InsertNewPage(); wordApp.Selection.InsertNewPage();
            wordApp.Selection.InsertBreak(WdBreakType.wdSectionBreakNextPage); // here the trick lies    
            wordApp.Selection.PageSetup.Orientation = WdOrientation.wdOrientLandscape;
            /*
            wordApp.Selection.InsertNewPage(); wordApp.Selection.InsertNewPage();
            wordApp.Selection.InsertBreak(WdBreakType.wdSectionBreakNextPage);
            wordApp.Selection.InsertNewPage();
            wordApp.Selection.PageSetup.Orientation = WdOrientation.wdOrientPortrait;
             */
        }

        public MSWord.Table InsertTable(System.Data.DataTable dt, MSWord.Range tableLocation)
        {

            // Add the table to the document at the range.
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;


            wordApp.ActiveDocument.Tables.Add(tableLocation, rowCount + 1, colCount);

            // Format the table and apply a style.
            MSWord.Table tbl = aDoc.Tables[aDoc.Tables.Count];


            //tbl.Range.Font.Size = aDoc.Tables[1].Range.Font.Size;

            tbl.Range.Font.Size = 8;


            // using distributed width
            //tbl.Columns.DistributeWidth();


            //object styleName = "Medium Shading 1 - Accent 3";

            // Copy table style
            object styleName = aDoc.Tables[1].get_Style();

            tbl.set_Style(ref styleName);

            // Add table headers
            for (int i = 0; i < colCount; i++)
            {
                tbl.Cell(1, i + 1).Range.Text = dt.Columns[i].ColumnName;
                tbl.Cell(1, i + 1).Range.Font.Color = WdColor.wdColorWhite;
            }

            // Populate Data

            for (int rowNum = 0; rowNum < rowCount; rowNum++)
            {
                for (int colNum = 0; colNum < colCount; colNum++)
                {
                    tbl.Cell(rowNum + 2, colNum + 1).Range.Text = dt.Rows[rowNum].ItemArray[colNum].ToString();

                    if (dt.Rows[rowNum].ItemArray[colNum].ToString().EndsWith(".jpg") || dt.Rows[rowNum].ItemArray[colNum].ToString().EndsWith(".png"))
                    {
                        pictureRangeList.Add(tbl.Cell(rowNum + 2, colNum + 1).Range);
                    }

                }
            }

            tbl.Borders.InsideLineStyle = WdLineStyle.wdLineStyleSingle;
            tbl.Borders.OutsideLineStyle = WdLineStyle.wdLineStyleSingle;

            tbl.Rows.Alignment = WdRowAlignment.wdAlignRowCenter;
            tbl.Range.ParagraphFormat.SpaceAfter = 4;
            tbl.Range.ParagraphFormat.SpaceBefore = 4;

            //tbl.Range.InsertBreak(Microsoft.Office.Interop.Word.WdBreakType.wdPageBreak);


            MSWord.Range rng = aDoc.Range(tbl.Range.End, tbl.Range.End + 1);
            rng.Select();
            // rng.InsertBreak(Microsoft.Office.Interop.Word.WdBreakType.wdPageBreak);
            /// tbl.Select();
            Selection sel = wordApp.Selection;
            sel.TypeParagraph();
            sel.TypeParagraph();
            sel.InsertBreak(Microsoft.Office.Interop.Word.WdBreakType.wdPageBreak);

            return tbl;
        }

        public void ChangeOrientation(String textToFind, WdOrientation orientation)
        {
            MSWord.Range location = GetLocation("[" + textToFind + "]");
            location.Text = "";

            ChangeOrientation(location, orientation);
            /*
            location.InsertBreak(WdBreakType.wdSectionBreakNextPage);
            aDoc.Sections[aDoc.Sections.Count].PageSetup.Orientation = WdOrientation.wdOrientLandscape;
            */
        }

        public void ChangeOrientation(MSWord.Range location, WdOrientation orientation)
        {
            location.InsertBreak(WdBreakType.wdSectionBreakNextPage);
            aDoc.Sections[aDoc.Sections.Count].PageSetup.Orientation = WdOrientation.wdOrientLandscape;
        }

        public void ChangeOrientationAtEnd(WdOrientation orientation)
        {
            object oEndOfDoc = "\\endofdoc";
            MSWord.Range wrdRng = aDoc.Bookmarks.get_Item(ref oEndOfDoc).Range;
            wrdRng.Select();
            Selection sel = wordApp.Selection;

            sel.InsertBreak(WdBreakType.wdSectionBreakNextPage);
            sel.PageSetup.Orientation = orientation;
        }

        public MSWord.Table InsertTable(System.Data.DataTable dt, string textToFind)
        {

            MSWord.Range tableLocation = GetLocation("[" + textToFind + "]");
            MSWord.Table tbl = InsertTable(dt, tableLocation);

            return tbl;
        }

        public void CopySelFormat(string headingString)
        {
            MSWord.Range headeingLocation = GetLocation(headingString);
            headeingLocation.Select();
            Selection sel = wordApp.Selection;
            sel.CopyFormat();
        }

        public string AddHeading(string heading, int indentCount, bool doCR)
        {
            string headingValue = "";

            object oEndOfDoc = "\\endofdoc";
            string style = "Heading 1";
            string CR = "\r";
            object objStyle = style;
            MSWord.Range wrdRng = aDoc.Bookmarks.get_Item(ref oEndOfDoc).Range;

            if (doCR)
                wrdRng.Text = heading + CR;
            else
                wrdRng.Text = heading;

            CopySelFormat("Testing Summary");

            wrdRng.Select();
            Selection sel = wordApp.Selection;
            sel.PasteFormat();

            for (int i = 0; i < indentCount; i++)
                wrdRng.ListFormat.ListIndent();
               

            String text = wrdRng.ListFormat.ListString;
            text = wrdRng.ListFormat.ToString();
            text = wrdRng.Text;
            headingValue = wrdRng.ListFormat.ListString + " " + wrdRng.Text;

            wrdRng = aDoc.Bookmarks.get_Item(ref oEndOfDoc).Range;

            string cleanText = new string(heading.Where(c => !char.IsControl(c)).ToArray());
            wrdRng.InsertAfter("[" + cleanText + "]" + "\r");

            return headingValue;
        }

        public void InsertCrossReference(MSWord.Range rng, string heading)
        {
            object ReferenceType = "Heading";
            object ReferenceItem = 2;
            object InsertAsHyperlink = true;
            object IncludePosition = false;
            object SeparateNumbers = false;
            object SeparatorString = " ";

            ReferenceType = WdReferenceType.wdRefTypeHeading;

            object vHeadings = aDoc.GetCrossReferenceItems(WdReferenceType.wdRefTypeHeading);

            Array currentCrossReferenceItems = (Array)vHeadings;

            int len = currentCrossReferenceItems.Length;


            for (int i = currentCrossReferenceItems.GetLowerBound(0); i <= currentCrossReferenceItems.GetUpperBound(0); i++)
            {
                string value = (string)currentCrossReferenceItems.GetValue(i);
                if (value.EndsWith("/"))
                    value = value.TrimEnd('/');
                if (value.Trim().Equals(heading.Trim()))
                {
                    ReferenceItem = i;
                    break;
                }
            }

            /*
            rng.InsertCrossReference(ref ReferenceType,
                Word.WdReferenceKind.wdContentText, ref ReferenceItem,
                ref InsertAsHyperlink, ref IncludePosition,
                ref SeparateNumbers, ref SeparatorString);
            */

            rng.MoveEnd(Microsoft.Office.Interop.Word.WdUnits.wdCharacter, -1);

            rng.Select();
            Selection sel = wordApp.Selection;

            sel.InsertCrossReference(ref ReferenceType,
                MSWord.WdReferenceKind.wdNumberNoContext, ref ReferenceItem,
                ref InsertAsHyperlink, ref IncludePosition,
                ref SeparateNumbers, ref SeparatorString);

            rng.Select();
            sel = wordApp.Selection;
            sel.Font.Size = 14;
            sel.Font.Color = MSWord.WdColor.wdColorDarkRed;
        }

        public string CreateUserReadablePictureHeading(string heading)
        {
            string outHeading = "";
            string[] separatingChars = { "__", "." };

            string[] parts = heading.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);

            outHeading = "Picture for TC: " + parts[1] + " DS: " + parts[2] + " Step: " + parts[3];

            return outHeading;
        }

        public void CreateTOC(string textToFind)
        {
            object oTrue = true;
            MSWord.Range tocLocation = GetLocation("[" + textToFind + "]");
            MSWord.Range tocRange = tocLocation;
            tocRange.Text = "";

            object start = tocRange.End - 1;
            object oUpperHeadingLevel = "1";
            object oLowerHeadingLevel = "6";
            tocRange.Font.Size = 12;
            tocRange.Font.Name = "Times New Roman";
            tocRange = aDoc.Range(start, start);
            MSWord.TableOfContents toc = aDoc.TablesOfContents.Add(tocRange,
                                                                ref oTrue,
                                                                ref oUpperHeadingLevel,
                                                                ref oLowerHeadingLevel,
                                                                ref missing, ref missing,
                                                                ref oTrue, ref oTrue,
                                                                ref missing, ref oTrue, ref oTrue, ref oTrue);
        }


        //////////////////
    }
}
