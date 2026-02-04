using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using System.Data;
using Route2NSEx.src.Marquis.systemUtil;
using System.Drawing;
using DataTableCompare;
using System.Configuration;

namespace Mars.autoTest.report.Word
{
 
    class ExcelReportGen
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ExcelReportGen));
        private WordReportConfig config;
        XLWorkbook wb;
        private static int sheetNum = 1;

        private static Dictionary<string, IXLCell> scenarioMap = new Dictionary<string, IXLCell>();
        private static Dictionary<string, string> testStepTextMap = new Dictionary<string, string>();

        int currentRow = 1;

        //XLColor.LightBlue

        XLColor[] tabColors = { XLColor.Blue, XLColor.Red, XLColor.Green, XLColor.DarkViolet, XLColor.Brown};
        int currentTabColorIdx = 0;
        private FlexReport report;

        string ReportTableWord = "CASHFLOW";

        public IXLCell sbLocation { get; private set; }
        public string ResultFromTableCompare { get; private set; }

        public ExcelReportGen(WordReportConfig config)
        {
            this.config = config;
        }

        public ExcelReportGen(FlexReport report)
        {
            this.report = report;
        }

        internal void OpenDocument()
        {
            //string reportTemplate = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];
            //wb = new XLWorkbook(reportTemplate + @"\MarsReportTemplate.xlsx");
            wb = new XLWorkbook(config.TemplateFilePath);
        }

        internal void OpenDocument(string path)
        {
            //string reportTemplate = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];
            //wb = new XLWorkbook(reportTemplate + @"\MarsReportTemplate.xlsx");
            wb = new XLWorkbook(path);
        }

        internal void SaveDocument(string path)
        {
            wb.SaveAs(path);
        }

        internal void GenerateStandardDocument()
        {
            FormatStoryboardData();
            GenerateTitleTab();
            GenerateTestCaseTabs();
            EnhanceAssetTab();
            GenerateConsolidatedReport();
        }

        internal string GenerateFlexDocument()
        {
            GenerateFlexTestCaseTabs();
            GenerateFlexTestStepsTabs();
            string status =  GenerateFlexTitleTab();
            GenerateUserLogTab();
            return status;
        }

        private void GenerateFlexTestStepsTabs()
        {
            Console.WriteLine("GenerateFlexTestStepsTabs");
            foreach (var tab in report.ReportTabs)
            {

                foreach (var dataConfig in tab.list)
                {

                    if (dataConfig != null && dataConfig.tc != null && dataConfig.tc.TestCaseData != null)
                    {
                        DataTable dt = dataConfig.tc.TestCaseData;

                        dt.TableName = dataConfig.dataSet;

                        DataTable testStepTable = TestStepTranslate(dt);
                    
                        string text = FormatTableAsText(testStepTable);
                        dataConfig.TestSteps = text;
                        if (testStepTextMap.Keys.Contains(dataConfig.dataSet) == false)
                           testStepTextMap.Add(dataConfig.dataSet, text);
                        //var sheet = wb.Worksheets.Add(testStepTable);
                        //sheet.Columns().AdjustToContents();
                    }
                }
            }
        }

        private string FormatTableAsText(DataTable testStepTable)
        {
            string text = "";
            foreach (DataRow row in testStepTable.Rows)
            {
                string data = row["Data"].ToString();
                if (data != null && data.Trim().Length != 0)
                    data = "(" + data + ")";
                text += row["#"] + ".   \t" + row["Keyword"] + "   \t" + row["Object"] + "   \t" + data + "\n";
            }

            return text;
        }

        private DataTable TestStepTranslate(DataTable dt)
        {
            DataTable testStepTable = new DataTable();
            testStepTable.TableName = dt.TableName;

            testStepTable.Columns.Add("#");
            testStepTable.Columns.Add("Keyword");
            testStepTable.Columns.Add("Object");
            testStepTable.Columns.Add("Data");

            int rowCount = 1;
            foreach (DataRow row in dt.Rows)
            {
                string keyword = row["Keyword"].ToString();
                if (report.KeywordTranslationDict.ContainsKey(keyword))
                {
                    DataRow newRow = testStepTable.NewRow();
                    newRow["#"] = "" + rowCount++;
                    newRow["Keyword"] = report.KeywordTranslationDict[keyword];
                    newRow["Object"] = row["Object Name"];
                    newRow["Data"] = row["Input"];

                    testStepTable.Rows.Add(newRow);
                    testStepTable.AcceptChanges();
                }

            }



            return testStepTable;

        }

        private string GenerateFlexTitleTab()
        {
            string status = "PASS";
            GenerateFlexTOCHeader();
            status = GenerateFlexTOC();
            //GenerateFlexTOCChart();

            return status;
        }

        private void GenerateFlexTOCHeader()
        {
            var tocWs = wb.Worksheet(1);

            //string reportName = GetDtFieldValue(report.FolderInfo, "Folder", report.ReportSet);
            //string reportDescr = GetDtFieldValue(report.FolderInfo, "Description", report.ReportSet);
            string reportName = report.FolderInfo.Rows[0]["Folder"].ToString();
            string reportDescr = report.FolderInfo.Rows[0]["Description"].ToString();
            string status = "PASS";
            foreach (var cf in report.GetReportConfigList())
            {
                if (cf.status.Equals("PARTIAL"))
                {
                    status = "PARTIAL";
                    break;
                }

                if (cf.status.Equals("FAIL"))
                {
                    status = "FAIL";
                    break;
                }

            }

            var dcell = ReplceTagWithValue("Report Name", reportName);
            dcell.Style.Alignment.WrapText = true;
            dcell  = ReplceTagWithValue("Description", reportDescr);
            dcell.Style.Alignment.WrapText = true;

            ReportUserLogInfo.AddMessage("ScenarioStatus", status);

            var cell = ReplceTagWithValue("Status", status);
            if (cell != null)
            {
                switch (status)
                {
                    case "PASS":
                        cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case "FAIL":
                        cell.Style.Fill.BackgroundColor = XLColor.AmericanRose;
                        break;

                    case "PARTIAL":
                        cell.Style.Fill.BackgroundColor = XLColor.GoldenYellow;
                        break;

                    default:
                        break;
                }
            }
        }

        private void GenerateFlexTOCChart()
        {
            //throw new NotImplementedException();
        }

        private string GenerateFlexTOC()
        {
            var tocWs = wb.Worksheet("TOC");
            DataTable tbl = CreateTableOfFlexTabs();

            string overallStatus = "PASS";

            tocWs.ShowGridLines = false;

            // Calculate stats
            
            int passCnt = 0;
            int partialCnt = 0;
            int failCnt = 0;

            int totalCnt = tbl.Rows.Count;

            for (int i = 0; i < tbl.Rows.Count; i ++)
            {
                if (tbl.Rows[i]["Status"].ToString().Equals("PASS"))
                {
                    passCnt++;
                }
                else if (tbl.Rows[i]["Status"].ToString().Equals("PARTIAL"))
                {
                    partialCnt++;
                }
                else if (tbl.Rows[i]["Status"].ToString().Equals("FAIL"))
                {
                    failCnt++;
                }
            }

            /*
            tocWs.Cell("J6").Value = partialCnt;
            tocWs.Cell("J7").Value = failCnt;
            tocWs.Cell("J8").Value = passCnt;
            */

            foreach (DataRow dataRow in tbl.Rows)
            {
                string testID = dataRow.Field<string>("Test ID");
                if (testStepTextMap.Keys.Contains(testID))
                {
                    string testSteps = testStepTextMap[testID].ToString();
                    Console.WriteLine("TOC: testSteps:\n" + testSteps);
                    Console.WriteLine("TOC: testSteps END");
                    if (testSteps.Length > 32760)
                        testSteps = testSteps.Substring(0, 32760);
                    dataRow["Test Steps"] = testSteps;
                }
            }

            tbl.AcceptChanges();

            IXLCell currentCell;

            var location = ReplceTagWithTable("TOC", tbl);



            int row = location.Address.RowNumber + 1;
            int cnt = 1;
            foreach (DataRow dataRow in tbl.Rows)
            {
                string status = dataRow.Field<string>("Status");
                string description = dataRow.Field<string>("Description");
                string testID = dataRow.Field<string>("Test ID");

                var curStatusCell = tocWs.Cell(location.Address.RowNumber + cnt, 7);
                var curDescrCell = tocWs.Cell(location.Address.RowNumber + cnt, 4);
                var curTestStepsCell = tocWs.Cell(location.Address.RowNumber + cnt, 5);

                if (testStepTextMap.Keys.Contains(testID))
                {
                    //string link = "'" + testID + "'!" + "A1";
                    //curTestStepsCell.Hyperlink = new XLHyperlink(link);
                    //curTestStepsCell.SetValue(testStepTextMap[testID].ToString());

                    // curTestStepsCell.SetDataValidation().InputMessage = testStepTextMap[testID].ToString();
                    // var validateList = "\"A,B,C,D\"";


                    //curTestStepsCell.SetDataValidation().List(validateList, true);


                    //curTestStepsCell.SetDataValidation().ErrorMessage = "ERR_TEST";
                    //      var options = new List<string> { "Option1", "Option2", "Option3" };
                    //  var validOptions = $"\"{String.Join(",", options)}\"";
                    //  curTestStepsCell.DataValidation.List(validOptions, true);

                    string testSteps = testStepTextMap[testID].ToString();
                    
                    if (testSteps.Length > 32760)
                        testSteps = testSteps.Substring(0, 32760);

                    curTestStepsCell.Comment.AddText(testSteps);
                    curTestStepsCell.Comment.Style.Size.SetAutomaticSize();
                    


                }

                if (scenarioMap.ContainsKey(description))
                {
                    try
                    {
                        var taretCell = scenarioMap[description];
                        string tabName = wb.Worksheet(2).Name;
                        string link = "'" + tabName + "'!" + taretCell.Address;
                        curDescrCell.Hyperlink = new XLHyperlink(link);
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "  " + e.StackTrace);
                        Console.WriteLine("Failed generating TOC entry for DataSet " + testID) ;
                        continue;
                    }
                    
                    //curDescrCell.Hyperlink = new XLHyperlink("'" + tabName + "'!A1");
                }
                else
                {
                    Console.WriteLine("GenerateFlexTOC: WARNING: key " + description + " is not found in scenarioMap");
                }

                switch (status)
                {
                    case "PASS":
                        curStatusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case "FAIL":
                        curStatusCell.Style.Fill.BackgroundColor = XLColor.AmericanRose;
                        break;

                    case "PARTIAL":
                        curStatusCell.Style.Fill.BackgroundColor = XLColor.GoldenYellow;
                        break;

                    default:
                        break;
                }


                cnt++;
            }



            currentCell = tocWs.Cell(row, location.Address.ColumnNumber);

            tocWs.Cell(location.Address.RowNumber, location.Address.ColumnNumber + 1).Style.Fill.BackgroundColor = location.Style.Fill.BackgroundColor;
            tocWs.Cell(location.Address.RowNumber, location.Address.ColumnNumber + 2).Style.Fill.BackgroundColor = location.Style.Fill.BackgroundColor;

            tocWs.Column("B").Width = 15;
            tocWs.Column("C").Width = 20;
            tocWs.Column("D").Width = 50;
            tocWs.Column("E").Width = 50;
            tocWs.Column("F").Width = 20;

            foreach (var ws in wb.Worksheets)
            {
                if (ws.Name.Equals("TOC"))
                {
                    continue;
                }

                /*
                else
                {
                    currentCell = tocWs.Cell(row, location.Address.ColumnNumber);
                    currentCell.Hyperlink = new XLHyperlink("'" + ws.Name + "'!A1");
                    row++;
                }
                */

            }

            if (partialCnt > 0)
            {
                overallStatus = "PARTIAL";
            }
            if (failCnt > 0)
            {
                overallStatus = "FAIL";
            }

            return overallStatus;
        }

        private DataTable CreateTableOfFlexTabs()
        {
            // Scenario code, scenario description

            // SEQ | datasetID | Dataset description | expected results | Status

            DataTable dt = new DataTable();

            dt.Columns.Add("Sequence");
            dt.Columns.Add("Test ID");
            dt.Columns.Add("Description");
            dt.Columns.Add("Test Steps");
            dt.Columns.Add("Expected Results");
            dt.Columns.Add("Status");
            dt.Columns.Add("Diary");

            foreach (var ds in report.GetReportConfigList())
            {
                if (ds.tc == null)
                {
                    Console.WriteLine("Warning: CreateTableOfFlexTabs: tc is null for DataSet " + ds.dataSet);
                    continue;
                }
                DataRow row = dt.Rows.Add();
                row["Sequence"] = ds.seq;
                row["Test ID"] = ds.dataSet;
                row["Description"] = ds.tc.DataSetDescr;
                row["Test Steps"] = ds.stepDesc;
                row["Expected Results"] = ds.expResults;
                row["Status"] = ds.status;
                row["Diary"] = ds.diary;
            }


            /*
            dt.Columns.Add("Name");
            dt.Columns.Add("Description");
            dt.Columns.Add("Status");

            foreach (var ws in wb.Worksheets)
            {
                if (ws.Name.Equals("TOC"))
                {
                    continue;
                }
                else
                {
                    DataRow row = dt.Rows.Add();
                    row["Name"] = ws.Name;
                    row["Description"] = GetDtFieldValue(report.FolderInfo, "Description", ws.Name);
                    row["Status"] = ws.Cell(4,3).Value;
                }
            }
            */

            return dt;
        }

        private void GenerateFlexTestCaseTabs()
        {
            Console.WriteLine("GenerateFlexTestCaseTabs");

            foreach(var tab in report.ReportTabs)
            {
                // Create sheet
                var ws = CreateFlexTab(tab.tabName);
                currentRow = 1;
                string tabStatus = "PASS";

                ReportUserLogInfo.currentFolder = tab.tabName;

                // Generate header -- At this point we do not need it
                // IXLRange tabHeaderRange = GenerateFlexSheetHeader(ws, tab.tabName, XLColor.FromArgb(0x4BACC6));

                foreach (var dataConfig in tab.list)
                {
                    ReportUserLogInfo.currentProject = dataConfig.project;
                    ReportUserLogInfo.currentCaseName = dataConfig.testCase;
                    ReportUserLogInfo.currentDataSetName = dataConfig.dataSet;
                    ReportUserLogInfo.currentSB = dataConfig.storyboard;

                    // Find test case status
                    string tcStatus = GetTCStatus(dataConfig.tc);
                    if (tcStatus.Equals("PARTIAL"))
                        tabStatus = "PARTIAL";
                    if (tcStatus.Equals("FAIL"))
                        tabStatus = "FAIL";

                    // Enrich config with status;
                    dataConfig.status = tcStatus;

                    // Generate TestCase header
                    if (dataConfig == null || dataConfig.tc == null)
                    {
                        Console.WriteLine("GenerateFlexTestCaseTabs: Warning: Result data not found for DataSet" + dataConfig.dataSet);
                        ReportUserLogInfo.AddMessage("ResultDataNotFound", "Result data not found for DataSet") ;
                        continue;
                    }

                    ReportUserLogInfo.AddMessage("TestCaseStatus", tcStatus);

                    GenerateTestCaseHeader(ws, dataConfig, tcStatus, XLColor.FromArgb(0x4BACC6));
                    // Generate test case info
                    GenerateTestCaseInfo(ws,  dataConfig.tc);

                    // AF added to make sure Column with is correct
                    Console.WriteLine("AdjustToContents()");
                    ws.Columns().AdjustToContents();


                    // Generate images
                    //GenerateFlexImagePart(ws, dataConfig.tc);
                    // Generate tables
                    //GenerateFlexDataComparePart(ws, dataConfig.tc);
                }

                //GenerateUserLogTab(ws); 
                //tabHeaderRange.Cell(4,2).Value = tabStatus;
                //SetStatusCellColor(tabHeaderRange.Cell(4, 2), tabStatus);
            }
        }

        private void GenerateUserLogTab()
        {
            var sheet = wb.AddWorksheet(ReportUserLogInfo.LogDataTable);
            sheet.Rows().AdjustToContents();
            sheet.Columns().AdjustToContents();
        }

        private string GetTCStatus(WordReportConfigTestCase tc)
        {
            string status = "PASS";

            if (tc != null && tc.TestCaseData != null)
            {
                foreach (DataRow row in tc.TestCaseData.Rows)
                {

                    if (row["Status"] != null && row["Status"].ToString().Equals("FAIL"))
                    {
                        status = "FAIL";
                        break;
                    }

                    if (row["Status"] != null && row["Status"].ToString().Equals("PARTIAL"))
                    {
                        status = "PARTIAL";
                        break;
                    }
                }
            }

            else
            {
                status = "Report ERROR";
                Console.WriteLine("Error in GetTCStatus tc or tc.TestCaseData is null");
            }
            

            return status;
        }



        private IXLRange GenerateTestCaseHeader(IXLWorksheet ws, FlexReportDataSetConfig tc, string status, XLColor xLColor)
        {
            int rangeRowNumber = 1;
            var dt = CreateFlexTCHeaderTable(tc.project, tc.storyboard, tc.testCase, tc.dataSet, status, tc.tc.DataSetDescr, tc.tc.TestCaseDescr);
            IXLCell firstCell = ws.Cell(currentRow, 2);

            IXLCell lastCell = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber + dt.Columns.Count - 1);
            IXLCell lastCellFirstCol = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber);


            var rngTable = ws.Range(firstCell, lastCell);
            var rngFirstColTable = ws.Range(firstCell, lastCellFirstCol);

            rngTable.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rngTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            rngTable.Style.Alignment.WrapText = true;

            rngFirstColTable.Style.Fill.BackgroundColor = xLColor;
            rngFirstColTable.Style.Font.Bold = true;
            rngFirstColTable.Style.Font.FontColor = XLColor.White;

            foreach (DataRow row in dt.Rows)
            {
                rngTable.Cell(rangeRowNumber, 1).Value = row.Field<string>(0);
                rngTable.Cell(rangeRowNumber, 2).Value = row.Field<string>(1);

                if (row.Field<string>(0).Equals("Status"))
                {
                    SetStatusCellColor(rngTable.Cell(rangeRowNumber, 2), row.Field<string>(1));
                }

                if (row.Field<string>(0).Equals("Data Set Descr"))
                {
                    try
                    {
                        scenarioMap.Add(row.Field<string>(1), rngTable.Cell(rangeRowNumber, 2));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("scenarioMap");
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }

                rangeRowNumber++;
            }

            rngTable.Style.Alignment.WrapText = true;

            currentRow += dt.Rows.Count + 2;

            return rngTable;
        }

        private void SetStatusCellColor(IXLCell cell, string status)
        {
            if (cell != null)
                switch (status)
                {
                    case "PASS":
                        cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case "FAIL":
                        cell.Style.Fill.BackgroundColor = XLColor.AmericanRose;
                        break;

                    case "PARTIAL":
                        cell.Style.Fill.BackgroundColor = XLColor.GoldenYellow;
                        break;

                    default:
                        break;
                }
        }

        private DataTable CreateFlexTCHeaderTable(string project, string storyboard, string testcase, string dataset, string status, string dataSetDescr, string testCaseDescr)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("1");
            dt.Columns.Add("2");

            DataRow row = dt.Rows.Add();
            row["1"] = "Project";
            row["2"] = project;

            row = dt.Rows.Add();
            row["1"] = "Storyboard";
            row["2"] = storyboard;

            row = dt.Rows.Add();
            row["1"] = "Test Case";
            row["2"] = testcase;

            row = dt.Rows.Add();
            row["1"] = "Test Case Descr";
            row["2"] = testCaseDescr;

            row = dt.Rows.Add();
            row["1"] = "Data Set";
            row["2"] = dataset;

            row = dt.Rows.Add();
            row["1"] = "Data Set Descr";
            row["2"] = dataSetDescr;

            row = dt.Rows.Add();
            row["1"] = "Status";
            row["2"] = status;


            return dt;
        }

        private IXLRange  GenerateFlexSheetHeader(IXLWorksheet ws, string tabName, XLColor xLColor)
        {
            int rangeRowNumber = 1;

            IXLCell firstCell = ws.Cell(currentRow, 2);

            string description = GetDtFieldValue(report.FolderInfo, "Description", tabName);
            //string expResult = GetDtFieldValue(report.FolderInfo, "Expected Result", tabName);
            string expResult = "";

            var dt = CreateFlexScenarioTable(tabName, description, expResult);

            IXLCell lastCell = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber + dt.Columns.Count - 1);
            IXLCell lastCellFirstCol = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber);


            var rngTable = ws.Range(firstCell, lastCell);
            var rngFirstColTable = ws.Range(firstCell, lastCellFirstCol);

            rngTable.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rngTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            rngFirstColTable.Style.Fill.BackgroundColor = xLColor;
            rngFirstColTable.Style.Font.Bold = true;
            rngFirstColTable.Style.Font.FontColor = XLColor.White;
            rngFirstColTable.Style.Alignment.WrapText = true;

            foreach (DataRow row in dt.Rows)
            {
                rngTable.Cell(rangeRowNumber, 1).Value = row.Field<string>(0);
                rngTable.Cell(rangeRowNumber, 2).Value = row.Field<string>(1);

                if (row.Field<string>(0).Equals("Status"))
                {
                    SetStatusCellColor(rngTable.Cell(rangeRowNumber, 2), row.Field<string>(1));
                }

                rangeRowNumber++;
            }

            currentRow +=6;

            return rngTable;
        }

        private string GetDtFieldValue(DataTable dt, string columnName, string key)
        {
            string retValue = "NA";

            for (int i = 0; i <= dt.Rows.Count - 1; i++)
            {
                if (dt.Rows[i][0].ToString().Equals(key))
                {
                    retValue = dt.Rows[i][columnName].ToString();
                    break;
                }
            }

            return retValue;
        }

        private DataTable CreateFlexScenarioTable(string scenario, string descr, string expResult)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("1");
            dt.Columns.Add("2");

            DataRow row = dt.Rows.Add();
            row["1"] = "Scenario";
            row["2"] = scenario;

            row = dt.Rows.Add();
            row["1"] = "Description";
            row["2"] = descr;

            row = dt.Rows.Add();
            row["1"] = "Expected Result";
            row["2"] = expResult;

            row = dt.Rows.Add();
            row["1"] = "Status";
            row["2"] = "";


            return dt;
        }

        private DataTable CreateFlexCpmmentTable(string baselineMessage, string compareMessage, int numOfRows)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("1");
            dt.Columns.Add("2");

            if (numOfRows == 1)
            {
                DataRow row = dt.Rows.Add();
                row["1"] = "Info";
                row["2"] = baselineMessage;
                
            }
            else
            {
                DataRow row = dt.Rows.Add();
                row["1"] = "Baseline";
                row["2"] = baselineMessage;
                row = dt.Rows.Add();
                row["1"] = "Compare";
                row["2"] = compareMessage;
            }
            
            return dt;
        }



        private IXLWorksheet CreateFlexTab(string tabName)
        {
            Console.WriteLine("tabName:" + tabName );
            string sheetName = tabName;

            var ws = wb.Worksheets.Add(sheetName);
            ws.TabColor = XLColor.FromArgb(0x31869B);
            ws.Cell(1, 8).Value = "Back to TOC";
            ws.Cell(1, 8).Hyperlink = new XLHyperlink("'" + "TOC" + "'!A1");

            ws.ShowGridLines = false;

            return ws;
        }

        private void GenerateTestCaseInfo(IXLWorksheet ws, WordReportConfigTestCase tc)
        {
            // FillTCInfo(ws,  tc);
            FillTCInfoComplete(ws, tc);
            currentRow += 2;
        }

        private void EnhanceAssetTab()
        {
            // Add Hyperlinks
            IXLCell startingCell = sbLocation;

            List<String> worksheetList = (from w in wb.Worksheets select w.Name).ToList();

            IXLWorksheet firstSheet = wb.Worksheet(1);

            DataTable table = config.StoryBoardConfig.StoryBoardData;
            var dataSetColumn = table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == "Data Set");
            var compareResultColumn = table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == "Compare Result");
            var baselineResultColumn = table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == "Baseline Result");


            if (startingCell != null)
            {
                int ro = startingCell.Address.RowNumber;
                foreach (DataRow tableRow in config.StoryBoardConfig.StoryBoardData.Rows)
                {
                    string dataSetName = tableRow.Field<string>(table.Columns.IndexOf(dataSetColumn));
                    if (dataSetName.Length > 22)
                        dataSetName = dataSetName.Substring(0, 22);
                    
                    string sheetName = GetFullSheetName(worksheetList, dataSetName + "_INF");
                    firstSheet.Cell(++ro, 2).Value = sheetName;
                    firstSheet.Cell(ro, 2).Hyperlink = new XLHyperlink("'" + sheetName + "'!A1");
                    firstSheet.Cell(ro, 2).Style.Font.FontColor = XLColor.DarkViolet;
                    firstSheet.Cell(ro, 2).Style.Font.FontName = "Calibri";


                    // Add Colors to result column
                    string baselineResultValue = tableRow.Field<string>(table.Columns.IndexOf(baselineResultColumn));
                    string compareResultValue = tableRow.Field<string>(table.Columns.IndexOf(compareResultColumn));

                    ApplyResultColors(firstSheet, ro, baselineResultColumn, baselineResultValue, 2);
                    ApplyResultColors(firstSheet, ro, compareResultColumn, compareResultValue, 2);

                }
            }
          
        }

        private void ApplyResultColors(IXLWorksheet sheet, int ro, DataColumn resultColumn, string resultValue, int offset)
        {
            if (resultValue != null && resultValue.Contains("FAIL"))
                resultValue = "FAIL";

            switch (resultValue)
            {
                case "PASS":
                    sheet.Cell(ro, resultColumn.Ordinal + offset).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    break;

                case "FAIL":
                    sheet.Cell(ro, resultColumn.Ordinal + offset).Style.Fill.BackgroundColor = XLColor.AmericanRose;
                    break;

                case "PARTIAL":
                    sheet.Cell(ro, resultColumn.Ordinal + offset).Style.Fill.BackgroundColor = XLColor.GoldenYellow;
                    break;

                default:
                    break;
            }
        }

        private string GetFullSheetName(List<string> worksheetList, string partialSheetName)
        {
            string fullSheetName = "";

            foreach (string str in worksheetList)
            {
                if (str.Contains(partialSheetName))
                {
                    fullSheetName = str;
                    break;
                }
            }

            return fullSheetName;
        }

        private void GenerateConsolidatedReport()
        {
            // clone storyboard table
            DataTable dtNewDataForConsolReport = config.StoryBoardConfig.StoryBoardData.Copy();
            DataTable dtForConsolReport;
            // add Project name and storyboard name to table


            DataColumn Col = dtNewDataForConsolReport.Columns.Add("Project", System.Type.GetType("System.String"));
            Col.SetOrdinal(0);
            Col = dtNewDataForConsolReport.Columns.Add("Storyboard", System.Type.GetType("System.String"));
            Col.SetOrdinal(1);

           
            foreach (DataRow row in dtNewDataForConsolReport.Rows)
            {
                row["Project"] = config.ProjectName;
                row["Storyboard"] = config.StoryBoardConfig.StoryBoardName;
            }


            // attempt to find an existing report to append data to
            string filePath = @"c:\temp\MarsConsolidatedReport.xlsx";
            if (File.Exists(filePath))
            {
                dtForConsolReport = ImportExceltoDatatable(filePath, "Sheet1");
                File.Delete(filePath);
                foreach (DataRow dr in dtNewDataForConsolReport.Rows)
                {
                    dtForConsolReport.Rows.Add(dr.ItemArray);
                }
            }
            else
            {
                dtForConsolReport = dtNewDataForConsolReport;
            }

            // add data
            // save data
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(dtForConsolReport, "ConsReport");
            wb.SaveAs(filePath);
        }

        int GetNextTabColorIdx()
        {
            if (currentTabColorIdx == tabColors.Count())
            {
                currentTabColorIdx = 0;
            }

            return currentTabColorIdx++;
        }

        private void FormatStoryboardData()
        {
            DataTable StoryBoardData = config.StoryBoardConfig.StoryBoardData;

            // remove Hour from duration
            foreach (DataRow row in StoryBoardData.Rows)
            {
                if (row["BL Duration"].ToString().Length > 5)
                    row["BL Duration"] = row["BL Duration"].ToString().Substring(3);
                if (row["CP Duration"].ToString().Length > 5)
                    row["CP Duration"] = row["CP Duration"].ToString().Substring(3);
            }

            foreach (DataColumn col in StoryBoardData.Columns)
            {
                switch (col.ColumnName)
                {
                    case "TC_Name":
                        {
                            col.ColumnName = "Test Case";
                            break;
                        }

                    case "TS_Name":
                        {
                            col.ColumnName = "Test Suite";
                            break;
                        }

                    case "Data_Set":
                        {
                            col.ColumnName = "Data Set";
                            break;
                        }

                    case "Data_Set_Descr":
                        {
                            col.ColumnName = "Data Set Descr";
                            break;
                        }

                    case "BL Start":
                        {
                            col.ColumnName = "Baseline Start";
                            break;
                        }

                    case "BL Duration":
                        {
                            col.ColumnName = "Baseline Duration";
                            break;
                        }

                    case "CP Start":
                        {
                            col.ColumnName = "Compare Start";
                            break;
                        }

                    case "CP Duration":
                        {
                            col.ColumnName = "Compare Duration";
                            break;
                        }

                    case "BL Result":
                        {
                            col.ColumnName = "Baseline Result";
                            break;
                        }

                    case "CP Result":
                        {
                            col.ColumnName = "Compare Result";
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }

        }

        private void GenerateTestCaseTabs()
        {
            foreach (var tc in config.TestCaseConfigList)
            {
                int tabColorEdx = GetNextTabColorIdx();

                GenerateTCInfoTab(tc, tabColorEdx);
                GenerateImageTab(tc, tabColorEdx);
                GenerateDataCompareTab(tc, tabColorEdx);
            }
        }

        private void GenerateDataCompareTab(WordReportConfigTestCase tc, int tabColorIdx)
        {
            string dataSetName = tc.DataSetName;
            string query = "Input Like '" + ReportTableWord + "%'";
            var foundRows = tc.TestCaseData.Select(query);
            //var foundRows = tc.TestCaseData.Select("Input Like 'CASHFLOW%'");

            int tcLength = tc.TestCaseData.Rows.Count;
            int tcCurrentRowNum = 0;

            if (dataSetName != null && foundRows.Count() > 0)
            {
                int currRowNum = 2;
                string currentTabName = tc.DataSetName + "_DATA";
                string sheetName = FormatSheetName(tc.DataSetName, "_DAT_", sheetNum++);
                var ws = wb.Worksheets.Add(sheetName).SetTabColor(tabColors[tabColorIdx]);
 
                while (tcCurrentRowNum < tcLength)
                {
                    if (tc.TestCaseData.Rows[tcCurrentRowNum]["Input"].ToString().StartsWith(ReportTableWord) == false)
                    {
                        tcCurrentRowNum++;
                        continue;
                    }

                    DataTable dt_b = CreateDataTable(tc, "BASELINE", tcCurrentRowNum);
                    //var cell = ws.Cell(1, 1);
                    //cell.InsertTable(dt_b);
                    //SaveToFile(dt_b, @"c:\temp\temp_BASELINE.xlsx");

                    DataTable dt_c = CreateDataTable(tc, "COMPARE", tcCurrentRowNum);
                    //cell = ws.Cell(1, 10);
                    //cell.InsertTable(dt_c);
                    //SaveToFile(dt_c, @"c:\temp\temp_COMPARE.xlsx");
                    InvokeDataCompare(dt_b, dt_c);

                    var resultWs = wb.Worksheet("Result");
                    var firstTableCell = resultWs.FirstCellUsed();
                    var lastTableCell = resultWs.LastCellUsed();
                    var rngData = resultWs.Range(firstTableCell.Address, lastTableCell.Address);


                    ws.Cell(currRowNum, 1).Value = rngData;
                    var fCell = ws.Cell(currRowNum, 1);
                    var lCell = ws.LastCellUsed();
                    var rng = ws.Range(fCell.Address, lCell.Address);
                    //rng.SetAutoFilter();

                    var table = rng.CreateTable();
                    table.ShowRowStripes = false;

                    /*
                    while (ws.Cell(currRowNum, currColNum).Value.ToString().Equals("SEPARATOR") == false)
                    {
                        ws.Cell(currRowNum, currColNum).Style.Fill.BackgroundColor = XLColor.LightBlue;
                        currColNum++;
                    }
                    */

                    ws.Columns().AdjustToContents();

                    wb.Worksheet("Result").Delete();
                    currRowNum = ws.LastRowUsed().RowNumber() + 4;

                    tcCurrentRowNum = tcCurrentRowNum + dt_b.Columns.Count;
                }
            }
        }

        private void GenerateFlexDataComparePart(IXLWorksheet ws,  WordReportConfigTestCase tc, string mode = "BOTH")
        {
            Console.WriteLine("GenerateFlexDataComparePart mode = " + mode);

            string dataSetName = tc.DataSetName;
            string query = "Input Like '" + ReportTableWord + "%'";
            var foundRows = tc.TestCaseData.Select(query);
            //var foundRows = tc.TestCaseData.Select("Input Like 'CASHFLOW%'");

            int tcLength = tc.TestCaseData.Rows.Count;
            int tcCurrentRowNum = 0;

            if (dataSetName != null && foundRows.Count() > 0)
            {
                //int currRowNum = 2;

                string currentTabName = tc.DataSetName + "_DATA";
               
                while (tcCurrentRowNum < tcLength)
                {
                    if (tc.TestCaseData.Rows[tcCurrentRowNum]["Keyword"].ToString().Equals("CaptureAndCompare") == false ||
                        IsCashflowObject(tc.TestCaseData.Rows[tcCurrentRowNum]["Object Name"].ToString()) == false ||
                        tc.TestCaseData.Rows[tcCurrentRowNum]["Input"].ToString().StartsWith(ReportTableWord) == false)
                    {
                        Console.WriteLine("Skipping Input in row " + tcCurrentRowNum + " ---> " + tc.TestCaseData.Rows[tcCurrentRowNum]["Input"].ToString());
                        tcCurrentRowNum++;
                        continue;
                    }

                    DataTable dt_b = CreateDataTable(tc, "BASELINE", tcCurrentRowNum);
                    Console.WriteLine("Cashflows BASELINE");
                    Console.WriteLine(DataTableUtil.DumpDataTable(dt_b));
                    
                    //var cell = ws.Cell(1, 1);
                    //cell.InsertTable(dt_b);
                    //SaveToFile(dt_b, @"c:\temp\temp_BASELINE.xlsx");

                    DataTable dt_c = CreateDataTable(tc, "COMPARE", tcCurrentRowNum);
                    Console.WriteLine("Cashflows COMPARE");
                    Console.WriteLine(DataTableUtil.DumpDataTable(dt_c));
                    //cell = ws.Cell(1, 10);
                    //cell.InsertTable(dt_c);
                    //SaveToFile(dt_c, @"c:\temp\temp_COMPARE.xlsx");
                    Console.WriteLine("mode:" + mode);
                    Console.WriteLine("dt_b.Columns[1].ColumnName:" + dt_b.Columns[1].ColumnName);

                    if (dt_b.Columns[1].ColumnName.StartsWith(mode) || mode.Equals("BOTH") || mode.Equals("ALL") || mode.Equals("CASHFLOWS_ALL"))
                    {
                        InvokeDataCompare(dt_b, dt_c, "CASHFLOWS");

                        var resultWs = wb.Worksheet("Result");
                        var firstTableCell = resultWs.FirstCellUsed();
                        var lastTableCell = resultWs.LastCellUsed();
                        var rngData = resultWs.Range(firstTableCell.Address, lastTableCell.Address);


                        ws.Cell(currentRow, 1).Value = rngData;
                        var fCell = ws.Cell(currentRow, 1);
                        var lCell = ws.LastCellUsed();
                        var rng = ws.Range(fCell.Address, lCell.Address);
                        //rng.SetAutoFilter();

                        var table = rng.CreateTable();
                        table.ShowRowStripes = false;

                        ws.Columns().AdjustToContents();

                        wb.Worksheet("Result").Delete();
                        currentRow = ws.LastRowUsed().RowNumber() + 4;

                        tcCurrentRowNum = tcCurrentRowNum + dt_b.Columns.Count;
                        if (mode.Equals("BOTH") == false)
                            break;
                    }
                    else
                    {
                        tcCurrentRowNum = tcCurrentRowNum + dt_b.Columns.Count;
                    }
                }
            }
        }

        private void GenerateFlexGenericDataComparePart(IXLWorksheet ws, WordReportConfigTestCase tc, string tableName = "BOTH")
        {
            Console.WriteLine("GenerateFlexGenericDataComparePart Table = " + tableName);

            string dataSetName = tc.DataSetName;
            

            DataTable dt_b = null;
            DataTable dt_c = null;

            Console.WriteLine("Getting table " + tableName + "_B" + " From ExtractedTablesDict");
            if (tc.ExtractedTablesDict.Keys.Contains(tableName + "_B"))
            {
                dt_b = tc.ExtractedTablesDict[tableName + "_B"].Copy();
                Console.WriteLine("Number of rows: " + dt_b.Rows.Count);
            }
            else
            {
                Console.WriteLine("Table " + tableName + "_B" + "NOT FOUND!");
                ReportUserLogInfo.AddMessage("B", "TableForCompareNotFound", "Table " + tableName + "_B" + " NOT FOUND!");
            }
                

            Console.WriteLine("Getting table " + tableName + "_C" + " From ExtractedTablesDict");
            if (tc.ExtractedTablesDict.Keys.Contains(tableName + "_C"))
            {
                dt_c = tc.ExtractedTablesDict[tableName + "_C"].Copy();
                Console.WriteLine("Number of rows: " + dt_c.Rows.Count);
            }

            else
            {
                Console.WriteLine("Table " + tableName + "_C" + "NOT FOUND!");
                ReportUserLogInfo.AddMessage("C", "TableForCompareNotFound", "Table " + tableName + "_C" + " NOT FOUND!");
            }
                


            // Handle situation where only one table is available.
            // Create the other table as a clone of the existing table
            // This way dataCompare can be run and produce a result where one side is empty

            if (dt_b != null && dt_c == null)
            {
                dt_c = dt_b.Clone();
            }

            if (dt_c != null && dt_b == null)
            {
                dt_b = dt_c.Clone();
            }



            if (dataSetName != null && dt_b != null && dt_c != null)
            {
                // add row number column
                DataColumn Col = null;
                if (dt_b.Columns.Contains("Row") == false)
                {
                    Col = dt_b.Columns.Add("Row");
                    Col.SetOrdinal(0);// to put the column in position 0;
                    for (int i = 0; i < dt_b.Rows.Count; i++)
                        dt_b.Rows[i][0] = i;
                }


                if (dt_c.Columns.Contains("Row") == false)
                {
                    Col = dt_c.Columns.Add("Row");
                    Col.SetOrdinal(0);// to put the column in position 0;
                    for (int i = 0; i < dt_c.Rows.Count; i++)
                        dt_c.Rows[i][0] = i;
                }

                // Compare Data!   
                InvokeDataCompare(dt_b, dt_c, tableName);

                var resultWs = wb.Worksheet("Result");
                var firstTableCell = resultWs.FirstCellUsed();
                var lastTableCell = resultWs.LastCellUsed();
                var rngData = resultWs.Range(firstTableCell.Address, lastTableCell.Address);


                ws.Cell(currentRow, 1).Value = rngData;
                var fCell = ws.Cell(currentRow, 1);
                var lCell = ws.LastCellUsed();
                var rng = ws.Range(fCell.Address, lCell.Address);
                //rng.SetAutoFilter();

                var table = rng.CreateTable();
                table.ShowRowStripes = false;

                ws.Columns().AdjustToContents();

                wb.Worksheet("Result").Delete();
                currentRow = ws.LastRowUsed().RowNumber() + 4;
            }
        }


        private void InvokeDataCompare(DataTable dt_b, DataTable dt_c, string tableName = null)
        {
            Console.WriteLine("InvokeDataCompare: trim baseline table");
            TrimTable(dt_b);
            Console.WriteLine("InvokeDataCompare: trim compare table");
            TrimTable(dt_c);

            string[] allFieldNames = (from dc in dt_b.Columns.Cast<DataColumn>()
                                     select dc.ColumnName).ToArray();

            string[] keyFieldNames;
            string[] cashflowFieldNames;

            if (tableName != null && tableName.StartsWith("CASHFLOWS"))
            {
                string fields = "Start,End,Date,Ccy,Type";
                cashflowFieldNames = fields.Split(',');
                List<string> keyFieldNamesList = new List<string>();
                foreach (string key in cashflowFieldNames)
                {
                    string realFieldName = GetRealFieldName(allFieldNames, key);

                    if (realFieldName != null)
                    {
                        keyFieldNamesList.Add(realFieldName);
                    }
                }
                keyFieldNames = keyFieldNamesList.ToArray();
            }
            else if (tableName != null && tableName.StartsWith("POSTINGS_TABLE"))
            {
                string fields = "Posting Date,Value Date,GL Date,Account ID,Debit Credit,Event Type,Posting Type";
                keyFieldNames = fields.Split(',');
            }
            else if (tableName != null && tableName.StartsWith("APPLICATIONS_AND_PAGES_TABLE"))
            {
                string fields = "Name";
                keyFieldNames = fields.Split(',');
            }

            else
            {
                keyFieldNames = new string[1];
                keyFieldNames[0] = dt_b.Columns[0].ColumnName;
            }

            
            string[] compareFieldNames = new string[allFieldNames.Length - 1];
            for (int i = 0; i < compareFieldNames.Length; i++)
                compareFieldNames[i] = allFieldNames[i + 1];

            DTCompare dtc = new DTCompare(dt_b, dt_c, allFieldNames, keyFieldNames, allFieldNames, compareFieldNames, null, null, wb);
            bool status;
            string errorMessage;
            bool compareStatus = dtc.Compare(out status, out errorMessage);

            if (compareStatus == false)
                ResultFromTableCompare = "PARTIAL";
        }

        private string GetRealFieldName(string[] allFieldNames, string key)
        {
            string realKey = null;
            foreach (string name in allFieldNames)
            {
                if (name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    realKey = name;
                    break;
                }
            }
                
            return realKey;
        }

        private void TrimTable(DataTable table)
        {
            DataColumn[] stringColumns = table.Columns.Cast<DataColumn>()
            .Where(c => c.DataType == typeof(string))
            .ToArray();

            foreach (DataRow row in table.Rows)
                foreach (DataColumn col in stringColumns)
                    if (row.Field<string>(col) != null)
                        row.SetField<string>(col, row.Field<string>(col).Trim());
        }

        private void SaveToFile(DataTable dt, string fileName)
        {
            var wb = new XLWorkbook();
            wb.Worksheets.Add(dt);

            if (File.Exists(fileName))
                File.Delete(fileName);
            wb.SaveAs(fileName);
        }

        private DataTable CreateDataTable(WordReportConfigTestCase tc, string mode, int rowNumber)
        {
            Console.Out.WriteLine("TC:" +  tc.TestCaseName + " DS: " + tc.DataSetName + " Mode: " + mode + " Row: " + rowNumber);

            DataTable dt = new DataTable();
            string dataColumnName;

            if (mode.Equals("BASELINE"))
                dataColumnName = "Outp Baseline";
            else
                dataColumnName = "Outp Compare";

            int tcCurrentRowNum = rowNumber;

            List<DataRow> foundRowsList = new List<DataRow>();
            
            while (tc.TestCaseData.Rows[tcCurrentRowNum]["Keyword"].ToString().Equals("CaptureAndCompare") 
                && IsCashflowObject(tc.TestCaseData.Rows[tcCurrentRowNum]["Object Name"].ToString())
                && tc.TestCaseData.Rows[tcCurrentRowNum]["Input"].ToString().StartsWith(ReportTableWord) != false
                )
            {
                foundRowsList.Add(tc.TestCaseData.Rows[tcCurrentRowNum]);
                tcCurrentRowNum++;
            }



            var foundRows = foundRowsList.ToArray();

            if (foundRows.Length == 0)
            {
                Console.Out.WriteLine("CreateDataTable: foundRows.Length = : " + foundRows.Length );
                return dt;
            }
            else
                Console.Out.WriteLine("CreateDataTable: foundRows.Length = : " + foundRows.Length);

            DataTable foundRowsDt =  foundRows.CopyToDataTable();

            string cashflowsDTdump = DataTableUtil.DumpDataTable(foundRowsDt);

            Console.Out.WriteLine("CreateDataTable: cashflowsDTdump : " + cashflowsDTdump);

            // Create table colums

            //Tiger recommended this change
            // int count = foundRows[0][dataColumnName].ToString().Split('\r').Count();
            int count = foundRows[0][dataColumnName].ToString().Split(new string[] {"\r" }, StringSplitOptions.None).Count();

            Console.WriteLine("Item count in foundRows[0] :" + count);

            dt.Columns.Add("Row");
            foreach (var row in foundRows)
            {
                string fieldName = row["Input"].ToString();
                dt.Columns.Add(fieldName);
            }

            // Create table rows
            for (int rowCount = 0; rowCount < count; rowCount++)
            {
                dt.Rows.Add(dt.NewRow());
                dt.Rows[rowCount]["Row"] = rowCount + 1;
            }

            // Populate Table rows
            foreach (var row in foundRows)
            {
                string fieldName = row["Input"].ToString();
                //Console.Out.WriteLine("fieldName  " + fieldName);
                if (fieldName.Equals("CASHFLOW_FLOWS"))
                    continue;
                #region to fix empty row
                var data = row[dataColumnName].ToString().Split('\r');
                #endregion
                foreach (var dataFieldReal in data)
                {
                    string dataField = dataFieldReal;
                    if (dataField.Contains(":") == false)
                    {
                        dataField = "1 :" + dataField;
                        Console.WriteLine("Changed dataField to " + dataField);
                    }
                    var pair = dataField.Split(':');
                    if (pair.Length != 2)
                    {
                        //Console.Out.WriteLine("Unexpected data");
                        continue;
                    }
                    //Console.Out.WriteLine("pair: " + pair[0] + " -- " + pair[1]);
                    int rowNum = int.Parse(pair[0].Trim()) - 1;
                    string rowData = pair[1].Trim();

                    // this is to ptotect prom creash
                    if (rowNum >= count)
                    {
                        Console.WriteLine("Error: rowNum >= count   rowNum:" + rowNum + " count:" + count + " fieldNam:" + fieldName);
                    }
                    else
                        dt.Rows[rowNum][fieldName] = rowData;
                }
            }
            dt.TableName = "Data";

            // Remove first part of each column name i.e CASHFLOWS_PRICE -> PRICE
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName.IndexOf('_') != -1)
                    col.ColumnName = col.ColumnName.Substring(col.ColumnName.IndexOf('_') + 1);
            }
            return dt;
        }

        private void GenerateFlexImagePart(IXLWorksheet ws, WordReportConfigTestCase tc, string imageTag)
        {
            var foundRows = tc.TestCaseData.AsEnumerable().Where(r => r.Field<string>("Img") != null);
            if (foundRows.Count() > 0)
            {
                FillIMGData(ws, foundRows, imageTag);
            }
        }

        private void GenerateImageTab(WordReportConfigTestCase tc, int tabColorIdx)
        {
            var foundRows = tc.TestCaseData.AsEnumerable().Where(r => r.Field<string>("Img") != null );
            if (foundRows.Count() > 0)
            {
                string sheetName = FormatSheetName(tc.DataSetName, "_IMG_", sheetNum++);
                var ws = wb.Worksheets.Add(sheetName).SetTabColor(tabColors[tabColorIdx]);

                FillIMGData(ws, foundRows, "");
                /*
                foreach (DataRow row in foundRows)
                {
                    //Console.Out.WriteLine(row["Img"]);
                    string pictureFileName = (string)row["Img"];
                    FileInfo fi = new FileInfo(pictureFileName);
                    string b_fileName = "B" + fi.Name;
                    string c_fileName = "C" + fi.Name;
                    string folderName = fi.DirectoryName;

                    var headerCell = ws.Cell(currentRow, firstCol);
                    headerCell.Value = FormatImageHeader(b_fileName);
                    headerCell = ws.Cell(currentRow, secondCol);
                    headerCell.Value = FormatImageHeader(c_fileName);

                    currentRow += 2;

                    var cell_b = ws.Cell(currentRow, firstCol);
                    var cell_c = ws.Cell(currentRow, secondCol);

                    string b_imageLocation = folderName + "\\" + b_fileName;
                    string c_imageLocation = folderName + "\\" + c_fileName;


                    ClosedXML.Excel.Drawings.IXLPicture image = null;

                    if (File.Exists(b_imageLocation))
                        //image = ws.AddPicture(b_imageLocation).MoveTo(cell_b).Scale(0.8);
                        image = ws.AddPicture(b_imageLocation).MoveTo(cell_b);
                    if (File.Exists(c_imageLocation))
                        image = ws.AddPicture(c_imageLocation).MoveTo(cell_c).Scale(0.8);
                        image = ws.AddPicture(c_imageLocation).MoveTo(cell_c);

                    Logger.Info("Add Picture file// " + folderName + "\\" + c_fileName);

                    currentRow += 60;
                }
                */
            }
        }

        static Dictionary<string, DataTable> testTables = null;
        internal static Dictionary<string, DataTable> GetReportTables(string filePath)
        {
            if (testTables == null)
                testTables = ImportExceltoDictOfDataTables(filePath);
            return testTables;
        }

        private void FillIMGData(IXLWorksheet ws, EnumerableRowCollection<DataRow> foundRows, string imageTag)
        {
            Console.WriteLine("FillIMGData: imageTag: " + imageTag);

            try
            {

                int firstCol = 1;
                int secondCol = 14;

                foreach (DataRow row in foundRows)
                {
                    //Console.Out.WriteLine(row["Img"]);
                    string input = "";

                    // Do not try to process images that are not tagged
                    if (row["Input"].GetType().ToString().StartsWith("System.DBNull"))
                    {
                        Console.WriteLine("Warning: row for IMAGE " + row["Img"] + " is not tagged. Replacing with Object Name " + row["Object Name"]);
                        row["Input"] = row["Object Name"];
                       
                    }

                    try
                    {
                        input = (string)row["Input"];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "" + e.StackTrace);
                        continue;
                    }

                    if (input.Equals(imageTag))
                    {
                        string pictureFileName = (string)row["Img"];
                        FileInfo fi = new FileInfo(pictureFileName);
                        string b_fileName = "B" + fi.Name;
                        string c_fileName = "C" + fi.Name;
                        string folderName = fi.DirectoryName;


                        var cell_b = ws.Cell(currentRow, firstCol);
                        var cell_c = ws.Cell(currentRow, secondCol);

                        string b_imageLocation = folderName + "\\" + b_fileName;
                        string c_imageLocation = folderName + "\\" + c_fileName;


                        ClosedXML.Excel.Drawings.IXLPicture image = null;

                        int imageRowCount = 0;
                        int baselineImageRowCount = 0;
                        int compareImageRowCount = 0;
                        if (File.Exists(b_imageLocation))
                        {
                            image = ws.AddPicture(b_imageLocation).MoveTo(cell_b);
                            baselineImageRowCount = (int)(image.Height / 19.5) + 2;
                            Console.WriteLine("Added image from " + folderName + "\\" + b_fileName);
                        }
                        else
                        {
                            Console.WriteLine("Image NOT FOUND " + folderName + "\\" + b_fileName);
                            ReportUserLogInfo.AddMessage("B", "ImageNotFounf", "Image NOT FOUND " + folderName + "\\" + b_fileName);
                        }
                           

                        if (File.Exists(c_imageLocation))
                        {
                            image = ws.AddPicture(c_imageLocation).MoveTo(cell_c);
                            compareImageRowCount = (int)(image.Height / 19.5) + 2;
                            Console.WriteLine("Added image from " + folderName + "\\" + c_fileName);
                        }
                        else
                        {
                            Console.WriteLine("Image  NOT FOUND " + folderName + "\\" + c_fileName);
                            ReportUserLogInfo.AddMessage("C", "ImageNotFound", "Image  NOT FOUND " + folderName + "\\" + c_fileName);
                        }

                        // Use the larger rowCount
                        if (baselineImageRowCount > compareImageRowCount)
                            imageRowCount = baselineImageRowCount;
                        else
                            imageRowCount = compareImageRowCount;

                        Console.WriteLine("Image " + imageTag + " incerted at row " + currentRow);
                        currentRow += imageRowCount;
                        Console.WriteLine("rowCount advanced to row " + currentRow);
                       
                        break;
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }

        private string FormatSheetName(string dataSetName, string typeTag, int sheetNumber)
        {
            string result = "";
            string name = dataSetName;
            if (name.Length > 22)
                name = name.Substring(0, 22);

            result = name + typeTag + sheetNumber;

            return result;
        }

        private string FormatImageHeader(string fileName)
        {
            string result = null; ;
            var strings = fileName.Split(new string[] { "__" }, StringSplitOptions.None);

            result = "TC: " + strings[1] + "     DS: " + strings[2] + "     Step: " + strings[3];
            var ss = result.Split('.');
            result = ss[0];
            return result;
        }

        private void GenerateTCInfoTab(WordReportConfigTestCase tc, int tabColorIdx)
        {
            Console.WriteLine("TC:" + tc.TestCaseName + "   DS:" + tc.DataSetName);
            string sheetName = FormatSheetName(tc.DataSetName, "_INF_", sheetNum++);
            var ws = wb.Worksheets.Add(sheetName).SetTabColor(tabColors[tabColorIdx]);
            
            FillTCInfo(ws,  tc);
            /*
            // Create a copy of original table with IMG col removed and use it
            DataTable dtMod = tc.TestCaseData.Copy();
            dtMod.Columns.Remove("Img");
            //cell.InsertTable(tc.TestCaseData);
            cell.InsertTable(dtMod);
            //ws.Columns().AdjustToContents();
            
            ws.Column("A").Width = 5; // #
            ws.Column("B").Width = 20; // Keyword
            ws.Column("C").Width = 40; // Object
            ws.Column("D").Width = 20; // Parameters
            ws.Column("E").Width = 20; // Input
            ws.Column("F").Width = 20; // Output Baseline
            ws.Column("G").Width = 20; // Output Compare
            ws.Column("H").Width = 10; // Status

            // Wrap text in Output columns to accomodate for tabular data
            ws.Column("F").Style.Alignment.WrapText = true;
            ws.Column("G").Style.Alignment.WrapText = true;

            EnhanceInfoTab(ws, dtMod);
            */
        }

        private void FillTCInfo(IXLWorksheet ws, WordReportConfigTestCase tc)
        {
            IXLCell cell = ws.Cell(currentRow, 1);
            // Create a copy of original table with IMG col removed and use it
            DataTable dtMod = tc.TestCaseData.Copy();
            dtMod.Columns.Remove("Img");

            // Remove data from cashflows output columns

            for (int i = 0; i < dtMod.Rows.Count - 1; i++)
            {
                if (dtMod.Rows[i]["Object Name"].ToString().Contains("CASHFLOWS"))
                {
                    dtMod.Rows[i]["Outp Baseline"] = " ";
                    dtMod.Rows[i]["Outp Compare"] = " ";
                }
            }
            
            cell.InsertTable(dtMod);
            
            ws.Column("A").Width = 5; // #
            ws.Column("B").Width = 20; // Keyword
            ws.Column("C").Width = 40; // Object
            ws.Column("D").Width = 20; // Parameters
            ws.Column("E").Width = 20; // Input
            ws.Column("F").Width = 20; // Output Baseline
            ws.Column("G").Width = 20; // Output Compare
            ws.Column("H").Width = 10; // Status

            // Wrap text in Output columns to accomodate for tabular data
            ws.Column("F").Style.Alignment.WrapText = true;
            ws.Column("G").Style.Alignment.WrapText = true;
            
            EnhanceInfoTab(ws, dtMod);
            currentRow += dtMod.Rows.Count;
        }

        private void FillTCInfoComplete(IXLWorksheet ws, WordReportConfigTestCase tc)
        {
            Console.WriteLine("FillTCInfoComplete: START DS:" + tc.DataSetName);
            IXLCell cell = ws.Cell(currentRow, 1);
            // Create a copy of original table with IMG col removed and use it
            DataTable dtMod = tc.TestCaseData.Copy();
           
            Console.WriteLine("Contents of Table = dtMod for DS " + tc.DataSetName);
            Console.WriteLine(DataTableUtil.DumpDataTable(dtMod));

            dtMod.Columns.Remove("Img");

            // Find out total number of images captured
            var foundRows = tc.TestCaseData.AsEnumerable().Where(r => r.Field<string>("Keyword").Equals("SnapShot"));
            int snapshotCount = 0;
            if (foundRows != null)
            {
                snapshotCount = foundRows.ToList().Count();
                if (snapshotCount == 0)
                    Console.WriteLine("Warning: No images found in Dataset " + tc.DataSetName);
            }
               
            ReportUserLogInfo.AddMessage("SnapShotCount", "" + snapshotCount);

            // Find if test case was compleated -- if "CLOSE_BUTTON" is found 
            var closeBtnFoundRows = tc.TestCaseData
                .AsEnumerable()
                .Where(r => (r.Field<string>("Object Name")!=null) 
                    && (r.Field<string>("Object Name").Equals("CLOSE_BUTTON")));
            int closeBtnCount = 0;
            if (closeBtnFoundRows != null)
            {
                closeBtnCount = closeBtnFoundRows.ToList().Count();
                if (closeBtnCount == 0)
                    Console.WriteLine("Warning: No CLOSE_BUTTON Click found in Dataset " + tc.DataSetName);
            }
            ReportUserLogInfo.AddMessage("CloseBtnCount", "" + closeBtnCount);

            // Add missing comments 
            dtMod = AddMissingComments(dtMod);



            // Remove data from cashflows output columns
            Console.WriteLine("FillTCInfoComplete: Remove data from Cashflows");
            for (int i = 0; i < dtMod.Rows.Count - 1; i++)
            {
                if (dtMod.Rows[i]["Object Name"].ToString().Contains("CASHFLOWS"))
                {
                    dtMod.Rows[i]["Outp Baseline"] = " ";
                    dtMod.Rows[i]["Outp Compare"] = " ";
                }
            }

            // Get tradeid
            Console.WriteLine("FillTCInfoComplete: Input = 'TRADE_ID ");
            string tradeid = "";
            string cashFlowsTag = null;
            string genericTableTag = null;
            string imageTag = null;
            string documentTag = null;
            DataRow[] result = dtMod.Select("Input = 'TRADE_ID'");
            if (result.Length > 0)
            {
                tradeid = result[0].Field<string>("Outp Baseline");
                string[] tradeids = tradeid.Split(new string[] { "\n", "\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (tradeids.Length > 0)
                {
                    string[] parts = tradeids[0].Split(':');
                    if (parts.Length == 2)
                    {
                        tradeid = parts[1].Trim();
                    }
                    else if (parts.Length == 1)
                    {
                        tradeid = parts[0];
                    }
                }
                Console.WriteLine();
            }

            // Split table into blocks based on Comment steps
            Console.WriteLine("FillTCInfoComplete: Split table into blocks based on Comment steps");
            int numOfRowsInMessage = 1;
            DataTable dataTablePartial = dtMod.Clone();
            foreach (DataRow dataRow in dtMod.Rows)
            {
                if (dataRow.Field<string>("Keyword").Equals("Comment"))
                {
                    try
                    {

                        if (dataTablePartial.Rows.Count != 0)
                        {
                            cell.InsertTable(dataTablePartial);
                            EnhanceInfoTab(ws, dataTablePartial);
                            currentRow += dataTablePartial.Rows.Count + 3;
                        }

                        cell = ws.Cell(currentRow, 1);
                        dataTablePartial = dtMod.Clone();

                        // Print banner
                        string commentString = dataRow.Field<string>("Input");
                        ReportUserLogInfo.AddMessage("Comment", commentString);
                        Console.WriteLine("Processing Comment string in row #" + dataRow.Field<Int32>("#") + " : " + commentString);

                        if (commentString == null)
                        {
                            Console.WriteLine("Warning: Comment string is null ");
                            continue;
                        }

                        var commentParts = commentString.Split('|');

                        String fullMessageTextBaseline = "";
                        String fullMessageTextCompare = "";
                        string documentMessageBaseline = "";
                        string documentMessageCompare = "";

                        numOfRowsInMessage = 1;
                        for (int i = 0; i < commentParts.Length; i++)
                        {
                            string str = commentParts[i];
                            if (str.Equals("TEXT"))
                            {
                                fullMessageTextBaseline += commentParts[i + 1];
                                fullMessageTextCompare += commentParts[i + 1];
                            }
                            else if (str.StartsWith("TAG"))
                            {
                                numOfRowsInMessage = 2;
                                var parts = str.Split(':');
                                if (parts.Length != 2)
                                {
                                    Console.WriteLine("TAG contents are missing. Skipping TAG ");
                                    ReportUserLogInfo.AddMessage("CommentTagNotFound", "TAG contents are missing. Skipping TAG");
                                    continue;
                                }

                                string tag = parts[1];

                                fullMessageTextBaseline += GetValueForTag(dtMod, tag, "Outp Baseline");
                                fullMessageTextCompare += GetValueForTag(dtMod, tag, "Outp Compare");
                            }

                            else if (str.StartsWith("DOCUMENT"))
                            {
                                numOfRowsInMessage = 1;
                                var parts = str.Split(':');
                                if (parts.Length != 2)
                                {
                                    Console.WriteLine("DOCUMENT contents are missing. Skipping DOCUMENT ");
                                    ReportUserLogInfo.AddMessage("CommentDocNotFound", "DOCUMENT contents are missing. Skipping DOCUMENT");
                                    continue;
                                }

                                documentTag = parts[1];

                                //documentMessageBaseline += GetTextValueForTag(dtMod, "DOCUMENT_" + documentTag, "Outp Baseline");
                                //documentMessageCompare  += GetTextValueForTag(dtMod, "DOCUMENT_" + documentTag, "Outp Compare");

                                documentMessageBaseline += GetTextValueForTag(dtMod,  documentTag, "Outp Baseline");
                                documentMessageCompare += GetTextValueForTag(dtMod,   documentTag, "Outp Compare");
                            }

                            else if (str.StartsWith("CASHFLOWS"))
                            {
                                var parts = str.Split(':');

                                if (parts.Length != 2)
                                {
                                    Console.WriteLine("CASHFLOWS contents are missing. Scipping CASHFLOWS ");
                                    ReportUserLogInfo.AddMessage("CommentCashflowsNotFound", "CASHFLOWS contents are missing. Scipping CASHFLOWS");
                                    continue;
                                }

                                cashFlowsTag = parts[1];
                            }

                            else if (str.StartsWith("TABLE"))
                            {
                                var parts = str.Split(':');

                                if (parts.Length != 2)
                                {
                                    Console.WriteLine("TABLE contents are missing. Scipping TABLE ");
                                    ReportUserLogInfo.AddMessage("CommentTableNotFound", "TABLE contents are missing. Scipping TABLE");
                                    continue;
                                }

                                genericTableTag = parts[1];
                            }

                            else if (str.StartsWith("IMAGE"))
                            {
                                var parts = str.Split(':');

                                if (parts.Length != 2)
                                {
                                    Console.WriteLine("IMAGE contents are missing. Scipping IMAGE ");
                                    ReportUserLogInfo.AddMessage("CommentImageNotFound", "IMAGE contents are missing. Scipping IMAGE");
                                    continue;
                                }

                                imageTag = parts[1];
                            }
                            
                        }
                        fullMessageTextBaseline = fullMessageTextBaseline.Replace("\"", "");
                        fullMessageTextCompare = fullMessageTextCompare.Replace("\"", "");

                        Console.WriteLine("B:" + fullMessageTextBaseline + " C:" + fullMessageTextCompare);

                        GenerateFlexCommentHeader(ws, XLColor.FromArgb(0x4BACC6), fullMessageTextBaseline, fullMessageTextCompare, numOfRowsInMessage);

                        currentRow += numOfRowsInMessage + 2;
                        cell = ws.Cell(currentRow, 1);

                        // Bring in IMAGE, CASHFLOWS or GENERIC TABLES if needed
                        if (cashFlowsTag != null)
                        {
                            GenerateFlexDataComparePart(ws, tc, cashFlowsTag);
                            cell = ws.Cell(currentRow, 1);
                            Console.WriteLine("GenerateFlexDataComparePart FINISHED for mode=" + cashFlowsTag);
                            cashFlowsTag = null;
                        }

                        else if (genericTableTag != null)
                        {
                            GenerateFlexGenericDataComparePart(ws, tc, genericTableTag);
                            cell = ws.Cell(currentRow, 1);
                            Console.WriteLine("GenerateFlexDataComparePart FINISHED for mode=" + genericTableTag);
                            genericTableTag = null;
                        }

                        else if (documentTag != null)
                        {
                            string lDocumentTag = documentTag;
                            documentTag = null;
                            GenerateFlexDocumentDataComparePart(ws, tc, lDocumentTag, documentMessageBaseline, documentMessageCompare);
                            cell = ws.Cell(currentRow, 1);
                            Console.WriteLine("GenerateFlexDataComparePart FINISHED for mode=" + documentTag);
                           
                        }


                        else if (imageTag != null)
                        {
                            Console.WriteLine("Row " + dataRow.Field<Int32>("#").ToString());
                            GenerateFlexImagePart(ws, tc, imageTag);
                            cell = ws.Cell(currentRow, 1);
                            imageTag = null;
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error while parsing Comment used for report generation");
                        Console.WriteLine(e.Message + e.StackTrace);
                        ReportUserLogInfo.AddMessage("CommentParsingError", "Error while parsing Comment used for report generation");
                    }
                 }
                else
                { 
                    if (dataRow["Keyword"].ToString().Equals("CaptureAndCompare") || dataRow["Keyword"].ToString().Equals("CaptureValue") || dataRow["Status"].ToString().Equals("FAIL"))
                        dataTablePartial.ImportRow(dataRow);
                }
            }

            // Show the last part in TC text

            if (dataTablePartial.Rows.Count > 0)
            {
                Console.WriteLine("FillTCInfoComplete: Format Sheet");
                cell = ws.Cell(currentRow, 1);
                cell.InsertTable(dataTablePartial);
                EnhanceInfoTab(ws, dataTablePartial);
                currentRow += dataTablePartial.Rows.Count + 3;
            
                ws.Column("A").Width = 5; // #
                ws.Column("B").Width = 20; // Keyword
                ws.Column("C").Width = 40; // Object
                ws.Column("D").Width = 20; // Parameters
                ws.Column("E").Width = 20; // Input
                ws.Column("F").Width = 20; // Output Baseline
                ws.Column("G").Width = 20; // Output Compare
                ws.Column("H").Width = 10; // Status

                // Wrap text in Output columns to accomodate for tabular data
                ws.Column("F").Style.Alignment.WrapText = true;
                ws.Column("G").Style.Alignment.WrapText = true;
            }
            // THIS HAS TO BE DONE CORRECTLY 

            // currentRow += dataTablePartial.Rows.Count;
            Console.WriteLine("FillTCInfoComplete: FillTCInfoComplete END");
        }

        private DataTable AddMissingComments(DataTable dtSrc)
        {
            List<string> commentImageList = new List<string>();
            List<string> commentTableList = new List<string>();
            List<string> commentDocumentList = new List<string>();
            List<string> commentCashflowsList = new List<string>();

            DataTable dtTarget = dtSrc.Clone();

            // Create a list of comments
            var foundRows = dtSrc.AsEnumerable().Where(r => r.Field<string>("Keyword").Equals("Comment"));
            foreach (DataRow row in foundRows)
            {
                string data = row.Field<string>("Input");
                if (data != null)
                {
                    string[] parts = data.Split('|');
                    if (parts.Length >= 3)
                    {
                        string[] dataParts = parts[2].Split(':');
                        if (dataParts.Length == 2)
                        {
                            string partType = dataParts[0];
                            if (partType.Equals("IMAGE"))
                            {
                                commentImageList.Add(dataParts[1]);
                            }
                            else if (partType.Equals("TABLE"))
                            {
                                commentTableList.Add(dataParts[1]);
                            }
                            else if (partType.Equals("DOCUMENT"))
                            {
                                commentDocumentList.Add(dataParts[1]);
                            }
                            else if (partType.Equals("CASHFLOWS"))
                            {
                                commentCashflowsList.Add(dataParts[1]);
                            }

                        }
                    }
                }
            }


            // Add a new Comment for each
            //  1. SnapShot that is not in the comment list
            //  2. Cashflow that is not in the comment list
            //  3. CaptureAndCompare    DOCUMENT_PREVIEW_TEXT  that is not in the comment list
            //  4. Table delivered by Tiger that is not in the comment list

            string cashflowMode = null;


            for (int i = 0; i < dtSrc.Rows.Count; i++)
            {
                DataRow srcRow = dtSrc.Rows[i];
                DataRow targetRow = dtTarget.NewRow();
                targetRow.ItemArray = srcRow.ItemArray;
                dtTarget.Rows.Add(targetRow);

                //  1. SnapShot that is not in the comment list
                if (srcRow.Field<string>("Keyword").Equals("SnapShot"))
                {

                    string imgName = srcRow.Field<string>("Input");
                    if (imgName == null)
                    {
                        imgName = srcRow.Field<string>("Object Name");
                        targetRow["Input"] = imgName;
                        dtTarget.AcceptChanges();
                    }
                        
                    if (commentImageList.Contains(imgName) == false)
                    {
                        DataRow targetCommentRow = dtTarget.NewRow();
                        targetCommentRow["Keyword"] = "Comment";
                        string comment = "TEXT|" + "\"" + "Image " + imgName + "\"" + "|IMAGE:" + imgName;
                        targetCommentRow["Input"] = comment;
                        targetCommentRow["#"] = targetRow["#"];  
                        dtTarget.Rows.Add(targetCommentRow);
                    }
                }

                //  2. Cashflow that is not in the comment list
                if (IsCashflowsRow(srcRow.Field<string>("Keyword"), srcRow.Field<string>("Object Name")))
                {
                    cashflowMode = ExtractCashflowMode(srcRow.Field<string>("Input"));
                }

                // At the end of cashflows check for existnce of corresponding comment and add it if needed
                if (cashflowMode != null &&
                    IsCashflowsRow(srcRow.Field<string>("Keyword"), srcRow.Field<string>("Object Name")) == false)
                {
                    // Implement using TABLE instead of cashflow
                    if (commentCashflowsList.Contains(cashflowMode) == false)
                    {
                        string tableName = "CASHFLOWS_TABLE_RECV";
                        if (cashflowMode.Equals("PAY"))
                            tableName = "CASHFLOWS_TABLE_PAY";


                        DataRow targetCommentRow = dtTarget.NewRow();
                        targetCommentRow["Keyword"] = "Comment";
                        string comment = "TEXT|" + "\"" + "Cashflows " + cashflowMode + "\"" + "|TABLE:" + tableName;
                        targetCommentRow["Input"] = comment;
                        targetCommentRow["#"] = targetRow["#"];
                        dtTarget.Rows.Add(targetCommentRow);
                        Console.WriteLine("Adding comment for Cashflows [" + comment + "]");
                    }
                    /*
                    if (commentCashflowsList.Contains(cashflowMode) == false)
                    {
                        DataRow targetCommentRow = dtTarget.NewRow();
                        targetCommentRow["Keyword"] = "Comment";
                        string comment = "TEXT|" + "\"" + "Cashflows " + cashflowMode + "\"" + "|CASHFLOWS:" + cashflowMode;
                        targetCommentRow["Input"] = comment;
                        targetCommentRow["#"] = targetRow["#"];
                        dtTarget.Rows.Add(targetCommentRow);
                        Console.WriteLine("Adding comment for Cashflows [" + comment + "]");
                    }
                    */
                    // this line has to be the last
                    cashflowMode = null;
                }


                //  3. CaptureAndCompare    DOCUMENT_PREVIEW_TEXT  that is not in the comment list
                    
                if (srcRow.Field<string>("Keyword").Equals("CaptureAndCompare") && 
                    srcRow.Field<string>("Object Name").Equals("DOCUMENT_PREVIEW_TEXT"))
                {
                    string docName = srcRow.Field<string>("Input");
                    if (commentDocumentList.Contains(docName) == false)
                    {
                        DataRow targetCommentRow = dtTarget.NewRow();
                        targetCommentRow["Keyword"] = "Comment";
                        string comment = "TEXT|" + "\"" + "Document " + docName + "\"" + "|DOCUMENT:" + docName;
                        targetCommentRow["Input"] = comment;
                        targetCommentRow["#"] = targetRow["#"];
                        dtTarget.Rows.Add(targetCommentRow);
                    }
                }
                    
                    //  4. Table delivered by Tiger that is not in the comment list


                }

            return dtTarget;
        }

        private bool IsCashflowsRow(string keyword, string objectName)
        {
            bool rc = false;
            if (keyword.Equals("CaptureAndCompare") && IsCashflowObject (objectName))
                rc = true;
            return rc;
        }

        private bool IsCashflowObject(string objectName)
        {
            bool rc = false;
            if (objectName != null && objectName.Equals("CASHFLOWS_TABLE") || objectName.Equals("FX_CASHFLOW"))
                rc = true;

            return rc;
        }

        private string ExtractCashflowMode(string inputString)
        {
            string modeString = "";
            var strings = inputString.Split('_');
            if (strings.Length > 1)
                modeString = strings[1];

            return modeString;

        }

        public DataTable ConvertTextToDataTable(string sourceMessage)
        {
            if (string.IsNullOrEmpty(sourceMessage)) return null;
            string[] arrData = sourceMessage.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            DataTable rslt = new DataTable();
            var col = rslt.Columns.Add("DATA");
            foreach (var itm in arrData)
            {
                rslt.Rows.Add(new string[] { itm });
            }
            return rslt;
        }

        private void GenerateFlexDocumentDataComparePart(IXLWorksheet ws, WordReportConfigTestCase tc, string genericTableTag, string fullMessageTextBaseline, string fullMessageTextCompare)
        {
            Console.WriteLine("GenerateFlexDocumentDataComparePart:  BEGIN");

            DataTable dt_b = ConvertTextToDataTable(fullMessageTextBaseline);
            DataTable dt_c = ConvertTextToDataTable(fullMessageTextCompare);

            // Handle the problem whe one of documents is missing
            if (dt_c == null && dt_b != null)
                dt_c = dt_b.Clone();

            if (dt_b == null  && dt_c != null)
                dt_b = dt_c.Clone();

            if (dt_b == null && dt_c == null)
            {
                Console.WriteLine("Error in GenerateFlexDocumentDataComparePart: both documents are not avalilable");
                return;
            }

                DataColumn Col = null;
            if (dt_b.Columns.Contains("Row") == false)
            {
                Col = dt_b.Columns.Add("Row");
                Col.SetOrdinal(0);// to put the column in position 0;
                for (int i = 0; i < dt_b.Rows.Count; i++)
                    dt_b.Rows[i][0] = i;
            }


            if (dt_c.Columns.Contains("Row") == false)
            {
                Col = dt_c.Columns.Add("Row");
                Col.SetOrdinal(0);// to put the column in position 0;
                for (int i = 0; i < dt_c.Rows.Count; i++)
                    dt_c.Rows[i][0] = i;
            }

            // Compare Data!   
            InvokeDataCompare(dt_b, dt_c);

            var resultWs = wb.Worksheet("Result");
            var firstTableCell = resultWs.FirstCellUsed();
            var lastTableCell = resultWs.LastCellUsed();
            var rngData = resultWs.Range(firstTableCell.Address, lastTableCell.Address);


            ws.Cell(currentRow, 1).Value = rngData;
            var fCell = ws.Cell(currentRow, 1);
            var lCell = ws.LastCellUsed();
            var rng = ws.Range(fCell.Address, lCell.Address);
            //rng.SetAutoFilter();

            var table = rng.CreateTable();
            table.ShowRowStripes = false;

            ws.Columns().AdjustToContents();

            wb.Worksheet("Result").Delete();
            currentRow = ws.LastRowUsed().RowNumber() + 4;

            Console.WriteLine("GenerateFlexDocumentDataComparePart:  END");
        }

        private IXLRange GenerateFlexCommentHeader(IXLWorksheet ws, XLColor xLColor, string fullMessageTextBaseline, string fullMessageTextCompare, int numOfRowsInMessage)
        {
            int rangeRowNumber = 1;

            IXLCell firstCell = ws.Cell(currentRow, 2);

            var dt = CreateFlexCpmmentTable(fullMessageTextBaseline, fullMessageTextCompare, numOfRowsInMessage);

            IXLCell lastCell = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber + dt.Columns.Count - 1);
            IXLCell lastCellFirstCol = ws.Cell(firstCell.Address.RowNumber + dt.Rows.Count - 1, firstCell.Address.ColumnNumber);


            var rngTable = ws.Range(firstCell, lastCell);
            rngTable.Style.Font.FontSize = 11;

            var rngFirstColTable = ws.Range(firstCell, lastCellFirstCol);

            rngTable.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rngTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            rngFirstColTable.Style.Fill.BackgroundColor = xLColor;
            rngFirstColTable.Style.Font.Bold = true;
            rngFirstColTable.Style.Font.FontColor = XLColor.White;
            rngFirstColTable.Style.Alignment.WrapText = true;

            foreach (DataRow row in dt.Rows)
            {
                rngTable.Cell(rangeRowNumber, 1).Value = row.Field<string>(0);
                rngTable.Cell(rangeRowNumber, 2).Value = row.Field<string>(1);
                rangeRowNumber++;
            }

            currentRow += 1;

            return rngTable;
        }

        private string GetTextValueForTag(DataTable dt, string fieldName, string coulumnName)
        {
            string value = "";

            try
            {
                DataRow[] result = dt.Select("Input = '" + fieldName + "'");

                if (result != null)
                {
                    value = result[0].Field<string>(coulumnName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetTextValueForTag: Value NOT FOUND for fieldName " + fieldName + " in column " + coulumnName);
                ReportUserLogInfo.AddMessage("TagValueNotFound", "Value NOT FOUND for fieldName " + fieldName + " in column " + coulumnName);
            }
            
            return value;
        }

        private string GetValueForTag(DataTable dt, string fieldName, string coulumnName)
        {
            string value = "";
            DataRow[] result = dt.Select("Input = '" + fieldName + "'");
            if (result.Length > 0)
            {
                string data = result[0].Field<string>(coulumnName);
                string[] tradeids = data.Split(new string[] { "\n", "\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (tradeids.Length > 0)
                {
                    string[] parts = tradeids[0].Split(':');
                    if (parts.Length == 2)
                    {
                        value = parts[1].Trim();
                    }
                    else if (parts.Length == 1)
                    {
                        value = parts[0];
                    }


                }
                Console.WriteLine();

            }
            return value;
        }

        private void EnhanceInfoTab(IXLWorksheet ws, DataTable table)
        {
            var statusColumn = table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == "Status");
            int ro = currentRow + 1;

            foreach (DataRow tableRow in table.Rows)
            {
                // Add Colors to status column
                string statusValue = tableRow.Field<string>(table.Columns.IndexOf(statusColumn));
                ApplyResultColors(ws, ro++, statusColumn, statusValue, 1);
            }
        }

        private void EnhanceFlexInfoTab(IXLWorksheet ws, DataTable table)
        {
            var statusColumn = table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == "Status");
            int ro = currentRow + 1;

            foreach (DataRow tableRow in table.Rows)
            {
                // Add Colors to status column
                string statusValue = tableRow.Field<string>(table.Columns.IndexOf(statusColumn));
                ApplyResultColors(ws, ro++, statusColumn, statusValue, 1);
            }
        }

        internal void GenerateTitleTab()
        {
            string date = DateTime.Now.ToString("yyyy/M/dd ");
            string time = DateTime.Now.ToString("hh:mm:ss");

            string currenTime = Convert.ToDateTime(date + time).ToString("yyyy/MM/dd HH:mm:ss");
            // Title info
            ReplceTagWithValue("ProjectName", config.ProjectName);
            ReplceTagWithValue("ProjectDescription", config.ProjectDescription);
            ReplceTagWithValue("StoryboardName", config.StoryBoardConfig.StoryBoardName);
            ReplceTagWithValue("StoryboardDescription", config.StoryBoardConfig.StoryBoardDescr);
            ReplceTagWithValue("ReportDate", currenTime);

            // Testing Summary
            ReplceTagWithValue("MARS_STORYBOARD", config.StoryBoardConfig.StoryBoardName);

            ReplceTagWithValue("MARS_TS_COUNT", "" + config.MarsTSCount);
            ReplceTagWithValue("MARS_TC_COUNT", "" + config.MarsTCCount);
            ReplceTagWithValue("MARS_TEST_STEP_COUNT", "" + config.MarsTestStepCount);
            ReplceTagWithValue("MARS_REP_GEN_DATE", currenTime);


            // Result Summary
            ReplceTagWithValue("MARS_B_SUCC", "" + config.MarsBSucc);
            ReplceTagWithValue("MARS_B_FAIL", "" + config.MarsBFail);
            ReplceTagWithValue("MARS_B_PART", "" + config.MarsBPartial);
            ReplceTagWithValue("MARS_B_UNPR", "" + config.MarsBUnpr);

            ReplceTagWithValue("MARS_C_SUCC", "" + config.MarsCSucc);
            ReplceTagWithValue("MARS_C_FAIL", "" + config.MarsCFail);
            ReplceTagWithValue("MARS_C_PART", "" + config.MarsCPartial);
            ReplceTagWithValue("MARS_C_UNPR", "" + config.MarsCUnpr);

            this.sbLocation =  ReplceTagWithTable("StoryboardTable", config.StoryBoardConfig.StoryBoardData);
        }

        

        internal IXLCell ReplceTagWithValue(string tag, string value)
        {
            IXLCell foundCell = wb.Worksheets.Worksheet(1).CellsUsed(cell => cell.GetString() == "[" + tag + "]").First();
            if (foundCell != null)
                foundCell.Value = value;
            return foundCell;
        }

        internal IXLCell ReplceTagWithTable(string tag, DataTable dt)
        {
            IXLCell foundCell = wb.Worksheets.Worksheet(1).CellsUsed(cell => cell.GetString() == "[" + tag + "]").First();
            if (foundCell != null)
            {
                var table = foundCell.InsertTable(dt);
                table.Style.Alignment.WrapText = true;
                int rowNum = 0;
                foreach (var row in table.Rows())
                {
                    if (rowNum > 0)
                        row.WorksheetRow().Height = 36;
                    rowNum++;
                }

                table.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top); // set vertical top Allignment
            }
                
            return foundCell;
        }

    
        internal void SaveDocument()
        {
          
            wb.SaveAs(config.OutputFilePath);
        }



        public static DataTable ImportExceltoDatatable(string filePath, string sheetName)
        {
            // Open the Excel file using ClosedXML.
            // Keep in mind the Excel file cannot be open when trying to read it
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(1);

                //Create a new DataTable.
                DataTable dt = new DataTable();

                //Loop through the Worksheet rows.
                bool firstRow = true;
                foreach (IXLRow row in workSheet.Rows())
                {
                    //Use the first row to add columns to DataTable.
                    if (firstRow)
                    {
                        foreach (IXLCell cell in row.Cells())
                        {
                            dt.Columns.Add(cell.Value.ToString());
                        }
                        firstRow = false;
                    }
                    else
                    {
                        //Add rows to DataTable.
                        dt.Rows.Add();
                        int i = 0;

                        foreach (IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                        {
                            dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                            i++;
                        }
                    }
                }

                return dt;
            }
        }

        public static Dictionary<string, DataTable>  ImportExceltoDictOfDataTables(string filePath)
        {
            Dictionary<string, DataTable> dict = new Dictionary<string, DataTable>();
            // Open the Excel file using ClosedXML.
            
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read  all sheets from Excel file.

                foreach (IXLWorksheet workSheet in workBook.Worksheets)
                {

                    //Create a new DataTable.
                    DataTable dt = new DataTable();

                    //Loop through the Worksheet rows.
                    bool firstRow = true;
                    foreach (IXLRow row in workSheet.Rows())
                    {
                        //Use the first row to add columns to DataTable.
                        if (firstRow)
                        {
                            foreach (IXLCell cell in row.Cells())
                            {
                                dt.Columns.Add(cell.Value.ToString());
                            }
                            firstRow = false;
                        }
                        else
                        {
                            //Add rows to DataTable.
                            dt.Rows.Add();
                            int i = 0;

                            foreach (IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                i++;
                            }
                        }
                    }

                    dict.Add(workSheet.Name, dt); ;
                }

                return dict;
            }
        }
    }
}
