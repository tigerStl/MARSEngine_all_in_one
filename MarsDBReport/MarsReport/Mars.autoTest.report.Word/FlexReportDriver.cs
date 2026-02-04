using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Data;
using ClosedXML.Excel;

namespace Mars.autoTest.report.Word
{
    public class FlexReportDriver
    {
       
        public string reportGroup { get; private set; }
        public string reportSet { get; private set; }

        public string reportFolder { get; private set; }
        public string outputType { get; private set; }
        public string outputPath { get; private set; }
        public string reportDataTemplate { get; private set; }
        public MarsConfig.MarsConfig mc { get; private set; }
       

        private FlexReportDataRepository repository = new FlexReportDataRepository();

        private FlexReport report = new FlexReport();

        public FlexReportDriver(string reportGroup, string reportSet, string reportFolder, string outputType, string outputPath, string reportDataTemplate, string ReportMode, MarsConfig.MarsConfig mc)
        {
            this.reportGroup = reportGroup;
            this.reportSet = reportSet;
            this.reportFolder = reportFolder;
            this.outputType = outputType;
            this.outputPath = outputPath;
            this.reportDataTemplate = reportDataTemplate;

            this.mc = mc;
            report.mc = mc;
            report.ReportGroup = reportGroup;
            report.ReportSet = reportSet;
            report.ReportMode = ReportMode;

        }

        public void GenerateReport()
        {
            ConfigureReport();
            RunReport();
        }

        private void RunReport()
        {
            Console.WriteLine("RunReport");
            string status = "";
            ExcelReportGen gen = new ExcelReportGen(report);
            gen.OpenDocument(mc.GetReportConfig().reportTemplatePath + @"\MarsFlexReportTemplate.xlsx");
            status = gen.GenerateFlexDocument();

            string fullOutputPath = outputPath + @"\MarsFlexReport" + "_" + reportFolder + "_" + status + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".xlsx";
            // gen.SaveDocument(outputPath + @"\MarsFlexReport" + "_" + reportFolder +  "_" + status+ "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".xlsx");
            gen.SaveDocument(fullOutputPath);

            Console.WriteLine("Report is saved to " + fullOutputPath);
            //Console.Write("Press <Enter> to exit... ");
            //while (Console.ReadKey().Key != ConsoleKey.Enter) { }
        }

        private void ConfigureReport()
        {
            Console.WriteLine("ConfigureReport");
            CreateReferenceConfiguration();
            
            /*
            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine(i + " -->> Sleping for 5 sec");
                Thread.Sleep(5000);
            }
            */

            ConfigureReportTabs();
        }

        private void ConfigureReportTabs()
        {
            Console.WriteLine("ConfigureReportTabs");

            //List<string> tabNames 
            var tabNames = report.GetReportConfigList().GroupBy(conf => conf.folder).Select(grp => grp.First().folder).ToList();

            foreach(string tabName in tabNames)
            {
                var entries = from entr in report.GetReportConfigList()
                              where entr.folder.Equals(tabName)
                              select entr;
                report.AddTab(tabName, entries.OrderBy(en => en.seq).ToList(), repository);
            }
        }

        private void CreateReferenceConfiguration()
        {
            // Load Report Configuration
            List<string>  sbList = LoadReportConfiguration();

            // Configure which storyboards are needed
            LoadDataRequirements(sbList);
        }

        private List<string>  LoadReportConfiguration()
        {
            // for now load configuration from Excel spreadsheet
            
            // Load Report Map
            var reportMap = LoadReportMap(); ;

            // Folder Info
            LoadFolderInfo();
            

            // Load Group Info
            LoadGroupInfo();

            // Load Set Info

            LoadSetInfo();

            // Load Excel Styles

            // LoadExcelStyles();

            // Load keyword translation
            LoadKeywordTranslation();

            return reportMap;

        }

        private void LoadKeywordTranslation()
        {
            report.KeywordTranslation = ImportExceltoDatatable(mc.GetReportConfig().reportTemplatePath + "\\" + "KeywordTranslation.xlsx", "Translation");
            Dictionary<string, string> translationDict = CreateTranslationDict(report.KeywordTranslation);
            report.KeywordTranslationDict = translationDict;
        }

        private Dictionary<string, string> CreateTranslationDict(DataTable keywordTranslationDt)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (DataRow row in keywordTranslationDt.Rows)
            {
                dict.Add(row["Keyword"].ToString(), row["Translation"].ToString());
            }
            return dict;
        }

        private void LoadExcelStyles()
        {
            report.StyleInfo = ImportExceltoDatatableWithStyle(mc.GetReportConfig().reportTemplatePath + "\\" + reportDataTemplate, "Style");
        }

        private void LoadSetInfo()
        {
            report.SetInfo = ImportExceltoDatatable(mc.GetReportConfig().reportTemplatePath + "\\" + reportDataTemplate, "Set");
            report.SetInfo.Columns[0].ColumnName = "Set";
        }

        private void LoadGroupInfo()
        {
            report.GroupInfo = ImportExceltoDatatable(mc.GetReportConfig().reportTemplatePath + "\\" + reportDataTemplate, "Group");
            report.GroupInfo.Columns[0].ColumnName = "Group";
        }

        private void LoadFolderInfo()
        {
            var reportFolders = reportFolder.Split('|').ToList();

            DataTable configTable = ImportExceltoDatatable(mc.GetReportConfig().reportTemplatePath + "\\" + reportDataTemplate, "Folder");
            configTable.Columns[0].ColumnName = "Folder";

            for (int i = configTable.Rows.Count - 1; i >= 0; i--)
            {
                string folder = configTable.Rows[i]["Folder"].ToString();
                if (reportFolders.Contains(folder) == false)
                {
                    configTable.Rows[i].Delete();
                }
            }

            configTable.AcceptChanges();

            report.FolderInfo = configTable;


        }

        private List<string> LoadReportMap()
        {
            var reportGroups = reportGroup.Split('|').ToList();
            var reportSets = reportSet.Split('|').ToList();
            var reportFolders = reportFolder.Split('|').ToList();
            // for now load configuration from Excel spreadsheet

            DataTable configTable = ImportExceltoDatatable(mc.GetReportConfig().reportTemplatePath + "\\" + reportDataTemplate, "ReportMap");
            // Delete all rows that are not in required FOLDER

            
            for (int i = configTable.Rows.Count - 1; i >= 0; i--)
            {
                string folder = configTable.Rows[i]["FOLDER"].ToString();
                string sb = configTable.Rows[i]["SB"].ToString();
                // WARNING! This code is here to filter out storyboards that begin with TEST so that test SB's do net get included in MarsReport
                if (reportFolders.Contains(folder) == false || sb.StartsWith("TEST"))
                {
                    configTable.Rows[i].Delete();
                }
            }
                        
            configTable.AcceptChanges();

            for (int i = 0; i < configTable.Rows.Count; i++)
            {
                // check for empty line
                if (configTable.Rows[i]["Project"] == null || configTable.Rows[i]["Project"].ToString().Trim().Length == 0)
                    break;
                string project = configTable.Rows[i]["Project"].ToString();
                string storyboard = configTable.Rows[i]["SB"].ToString();
                
                // WARNING! This code is here to filter out storyboards that begin with TEST so that test SB's do net get included in MarsReport
                if (storyboard != null && storyboard.StartsWith("TEST"))
                    continue;

                int storyboardRow = Convert.ToInt32(configTable.Rows[i]["SB_Row"]);
                string testCase = configTable.Rows[i]["TC"].ToString();
                string dataSet = configTable.Rows[i]["DS"].ToString();
                string group = configTable.Rows[i]["GROUP"].ToString();
                string set = configTable.Rows[i]["SET"].ToString();
                string folder = configTable.Rows[i]["FOLDER"].ToString();
                //int seq = Convert.ToInt32(configTable.Rows[i]["SEQ"]);
                int seq;
                bool isNumeric = int.TryParse(configTable.Rows[i]["SEQ"].ToString(), out seq);

                if (isNumeric == false)
                {
                    Console.WriteLine("Error parsing SEQ: [" + configTable.Rows[i]["SEQ"].ToString() + "]");
                    System.Environment.Exit(-1);
                }
                string expResults = "";
                if (configTable.Columns.Contains("EXPECTED RESULTS"))
                {
                    expResults = configTable.Rows[i]["EXPECTED RESULTS"].ToString();
                }

                string diary = "";
                if (configTable.Columns.Contains("DIARY"))
                {
                    diary = configTable.Rows[i]["DIARY"].ToString();
                }

                string stepDesc = "";

                if (configTable.Columns.Contains("STEPDESC"))
                {
                    stepDesc = configTable.Rows[i]["STEPDESC"].ToString();
                }


                // add config only for group and set specified
                //if ((group.Equals(reportGroup) || String.IsNullOrEmpty(reportGroup)) && 
                //    (set.Equals(reportSet) || String.IsNullOrEmpty(reportSet)))
                //    report.AddFlexReportDataSetConfig(project, storyboard, storyboardRow, testCase, dataSet, group, set, folder, seq);

                if ((String.IsNullOrEmpty(reportGroup) || reportGroups.Contains(group)) &&
                    (String.IsNullOrEmpty(reportSet) || reportSets.Contains(set)) &&
                    (String.IsNullOrEmpty(reportFolder) || reportFolders.Contains(folder))
                   )
                    report.AddFlexReportDataSetConfig(project, storyboard, storyboardRow, testCase, dataSet, group, set, folder, seq, expResults, diary, stepDesc);

            }
            var result = report.GetReportConfigList().GroupBy(test => test.storyboard)
                               .Select(grp => grp.First().project + "___" + grp.First().storyboard)
                   .ToList();

            report.OrderReportEntries();

            return result;
        }



        void LoadDataRequirements(List<string> sbList)
        {
            foreach (string sb in sbList)
            {
                var strings = sb.Split(new string[] { "___" }, System.StringSplitOptions.None);
                AddUnit(strings[0], strings[1]);
            }

            Console.WriteLine("-->> CreateReferenceConfiguration FINISHED");
        }

        private void AddUnit(string projectName, string storyboardName)
        {
            Console.WriteLine("AddUnit for Project [" + projectName + "] Storyboard [" + storyboardName + "]");
            WordReportConfig config = ReportDriver.CreateWordReportConfig(projectName, storyboardName, outputType, outputPath, mc, report.GetReportConfigList());
            repository.AddUnit(projectName,  storyboardName, config);
        }

        public static DataTable ImportExceltoDatatable(string filePath, string sheetName)
        {
            // Open the Excel file using ClosedXML.
            // Keep in mind the Excel file cannot be open when trying to read it
            Console.WriteLine("Open template file [" + filePath + "]") ;
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(sheetName);

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
                        try
                        {

                            foreach (IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                i++;
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }

                return dt;
            }
        }

        public static DataTable ImportExceltoDatatableWithStyle(string filePath, string sheetName)
        {
            // Open the Excel file using ClosedXML.
            // Keep in mind the Excel file cannot be open when trying to read it
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(sheetName);

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

                        dt.Columns.Add("CellStyle");
                        firstRow = false;
                    }
                    else
                    {
                        //Add rows to DataTable.
                        dt.Rows.Add();
                        int i = 0;
                        try
                        {

                            foreach (IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                //
                                if (cell.Address.ColumnLetter.Equals("B"))
                                {
                                 //   ClosedXML.Excel.IXLStyle style = cell.Style.cl;

                                 //   var type = cell.Style.GetType(); 
                                }

                                //

                                i++;
                            }
                        }
                        catch (Exception e) { }
                    }
                }

                return dt;
            }
        }
    }
}
