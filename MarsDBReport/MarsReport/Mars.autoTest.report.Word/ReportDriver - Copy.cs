using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;

using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;

namespace Mars.autoTest.report.Word
{
    public class ReportDriver
    {
        #region generate pdf report
        private static MLogger Logger = MLogger.GetLogger(typeof(ReportDriver));

        public Mars.MarsConfig.MarsConfig mc { get; set; }

        public bool IS_FLEX_REPORT = false;


        /// <summary>
        /// Entry point for Summary report 
        /// </summary>
        /// 
        //public void GenPdfMgmtRpt()
        public void GenPdfMgmtRpt(string strDBIdx)
        {
            KillWordProc("MARS_SUMMARY_TEMPLATE");
            try
            {
                string mgmtReportConfigPath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"] + "\\MarsSummaryReportConfig.txt";
                if (File.Exists(mgmtReportConfigPath) == false)
                {
                    ShowMessage("WARNING: Report config file " + mgmtReportConfigPath + " is not found");
                    return;
                }

                WordSummaryReportConfig config = new WordSummaryReportConfig();
                WordReportFilter rf = new WordReportFilter(mgmtReportConfigPath);

                // proj result used to get true negin and end time
                B_T_PROJ_TEST_RESULT ptr = new B_T_PROJ_TEST_RESULT();
                var projTestResultList = ptr.GetProjTestResult(strDBIdx);

                // Get project data
                List<B_TEST_PROJECT> ProjectList = new List<B_TEST_PROJECT>();
                B_TEST_PROJECT objProject = new B_TEST_PROJECT();
                ProjectList = objProject.GetProject(strDBIdx);

                // Process projects while applying filtering
                foreach (var project in ProjectList)
                {
                    //if (ProjectIsRequiredForReport(project.PROJECT_NAME))
                    if (rf.ProjectIsRequired(project.PROJECT_NAME))
                    {
                        config.AddRowToProjectSummaryData(project.PROJECT_NAME, project.PROJECT_DESCRIPTION);
                        config.MarsProjectCount++;
                        var projectConfig = config.ConfigureProjectData(project.PROJECT_NAME, project.PROJECT_DESCRIPTION, project.PROJECT_ID);

                        //List<B_STORYBOARD_SUMMARY> storyboardList = BoHelper.GetAllStoryboardRows(project.PROJECT_ID);
                        List<B_STORYBOARD_SUMMARY> storyboardList = BoHelper.GetAllStoryboardRows(strDBIdx,project.PROJECT_ID);
                        foreach (var sb in storyboardList)
                        {
                            //if (StoryboardIsRequiredForReport(sb.STORYBOARD_NAME))
                            if (rf.StoryboardIsRequired(project.PROJECT_NAME, sb.STORYBOARD_NAME))
                            {
                                projectConfig.AddRowToProjectStoryboardData(sb.STORYBOARD_NAME, sb.DESCRIPTION);
                                config.MarsStoryboardCount++;
                                // get counts and stats from db
                                int iUnprocecced = 0;
                                V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(sb.STORYBOARD_ID, ref iUnprocecced, strDBIdx);

                                if (objStoryBrdSumInfo == null)
                                    continue;

                                // stats            
                                int marsTSCount = (int)(objStoryBrdSumInfo.TSCNT ?? 0);
                                int marsTCCount = (int)(objStoryBrdSumInfo.TCCNT ?? 0);
                                int marsTestStepCount = (int)(objStoryBrdSumInfo.STEP_CNT ?? 0);

                                // success stats
                                int marsBSucc = (int)(objStoryBrdSumInfo.BASE_RIGHT_CNT ?? 0);
                                int marsCSucc = (int)(objStoryBrdSumInfo.CMP_RIGHT_CNT ?? 0);
                                int marsBFail = (int)(objStoryBrdSumInfo.BASE_FAIL_CNT ?? 0);
                                int marsCFail = (int)(objStoryBrdSumInfo.CMP_FAIL_CNT ?? 0);
                                int marsBUnpr = iUnprocecced;
                                int marsCUnpr = iUnprocecced;
                                int marsBPartial = (int)(objStoryBrdSumInfo.BASE_PARTIAL_CNT ?? 0);
                                int marsCPartial = (int)(objStoryBrdSumInfo.CMP_PARTIAL_CNT ?? 0);

                                // TODO replace numbers with real counts
                                var sbConfig = projectConfig.ConfigureStoryboard(sb.STORYBOARD_NAME, sb.DESCRIPTION, sb.STORYBOARD_ID, marsCSucc, marsCFail, marsCUnpr, marsCPartial);

                                // storyboard detail data 
                                List<V_STORYBOARD_TEST_FULLVISIONDTO> currentStoryBoardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(sb.STORYBOARD_ID, strDBIdx);
                                /// sorted
                                currentStoryBoardInfo = new List<V_STORYBOARD_TEST_FULLVISIONDTO>(currentStoryBoardInfo.OrderBy(p => p.RUN_ORDER));
                                foreach (var row in currentStoryBoardInfo)
                                {
                                    var baselineProjTestResult = GetProjTestResult(projTestResultList, 1, row.STORYBOARD_DETAIL_ID);
                                    var compareProjTestResult = GetProjTestResult(projTestResultList, 0, row.STORYBOARD_DETAIL_ID);

                                    string bStart = "";
                                    string bDuration = "";
                                    string cStart = "";
                                    string cDuration = "";

                                    if (baselineProjTestResult != null)
                                    {
                                        bStart = baselineProjTestResult.TEST_BEGIN_TIME.ToString();
                                        bDuration = (baselineProjTestResult.TEST_END_TIME - baselineProjTestResult.TEST_BEGIN_TIME).ToString();
                                    }

                                    if (compareProjTestResult != null)
                                    {
                                        cStart = compareProjTestResult.TEST_BEGIN_TIME.ToString();
                                        cDuration = (compareProjTestResult.TEST_END_TIME - compareProjTestResult.TEST_BEGIN_TIME).ToString();
                                    }

                                    sbConfig.AddRowToStoryBoardData("" + row.RUN_ORDER,
                                                                    row.TEST_CASE_NAME,
                                                                    row.TEST_SUITE_NAME,
                                                                    row.DATA_SET_ALIAS_NAME,
                                                                    row.DATASET_DESCRIPTION,
                                                                    row.HIST_TEST_RESULT_IN_TEXT,
                                                                    bStart,
                                                                    bDuration,
                                                                    cStart,
                                                                    cDuration);
                                }

                                // storyboard results

                                StoryboardStats sbs = ComputeStoryboardStats((long)sb.STORYBOARD_ID, strDBIdx);
                               
                                sbConfig.AddRowToStoryBoardReportData("Passed", sbs.MarsBSucc, sbs.MarsCSucc);
                                sbConfig.AddRowToStoryBoardReportData("Failed", sbs.MarsBFail, sbs.MarsCFail);
                                sbConfig.AddRowToStoryBoardReportData("Partial", 0, sbs.MarsCPartial);
                                sbConfig.AddRowToStoryBoardReportData("Unprocessed", sbs.MarsBUnpr, sbs.MarsCUnpr);

                                // storyboard stats
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Suites", marsTSCount);
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Cases", marsTCCount);
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Steps", marsTestStepCount);

                                // update top summary results

                                config.MarsTestCaseCount += marsTCCount;
                                config.MarsTestStepCount += marsTestStepCount;

                                config.MarsCSucc += sbs.MarsCSucc;
                                config.MarsCFail += sbs.MarsCFail;
                                config.MarsCPartial += sbs.MarsCPartial;
                                config.MarsCUnpr += sbs.MarsCUnpr;
                                config.MarsBSucc += sbs.MarsBSucc;
                                config.MarsBFail += sbs.MarsBFail;
                                config.MarsBPartial += sbs.MarsBPartial;
                                config.MarsBUnpr += sbs.MarsBUnpr;
                            }
                        }
                    }


                    // Fill file config data
                    string strPath = ConfigurationManager.AppSettings["REPORT_PATH"];
                    string reportTemplatePath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];

                    config.TemplateFilePath = reportTemplatePath + "\\" + "MARS_SUMMARY_TEMPLATE.docx";
                    if (File.Exists(config.TemplateFilePath) == false)
                    {
                        ShowMessage("WARNING: Report template file " + config.TemplateFilePath + " is not found");
                        return;
                    }

                    config.OutputFilePath = strPath + "\\" + "MarsTestSummaryReport_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".docx";

                    // configure report date
                    string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                    config.ReportGenDate = currentDateTime;
                }


                // generate report
                WordSummaryReportGen gen = new WordSummaryReportGen(config);
                gen.OpenDocument();
                gen.GenerateDocument();
                gen.SaveDocument();
                ShowMessage("Report is created in " + config.OutputFilePath);
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex);
            }

            KillWordProc("MARS_SUMMARY_TEMPLATE");
        }


        public static WordReportConfig CreateWordReportConfig(string projectName, string storyboardName, string outputType, string outputPath, MarsConfig.MarsConfig mc, List<FlexReportDataSetConfig> list = null)
        {
            ReportDriver driver = new ReportDriver();
            driver.mc = mc;
            driver.IS_FLEX_REPORT = true;

            //return driver.GenerateReportByProjectAndStoryboardName(projectName, storyboardName, outputType, outputPath);
            return driver.GenerateReportByProjectAndStoryboardName(projectName, storyboardName, outputType, outputPath, 
                MarsConfig.MarsConfig.currentDBInfo.DBIdx,list); 
        }

        public WordReportConfig GenerateReportByProjectAndStoryboardName(string projectName, string storyboardName, string outputType, string outputPath,string strDBIdx,List<FlexReportDataSetConfig> list = null)
        {
            int iUnprocecced = 0;
            //int storyboardId = 104850;
            long projectId = BoHelper.GetProjectIdByName(projectName,strDBIdx);
            if (projectId == 0)
            {
                Console.WriteLine("\n** Error: Project [" + projectName + "] not found\n");
                return null;
            }

            long storyboardId = BoHelper.GetStoryboardByName(storyboardName, projectId,strDBIdx);

            if (storyboardId == 0)
            {
                Console.WriteLine("\n** Error: Storyboard [" + storyboardName + "] not found\n");
                return null;
            }

            V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(storyboardId, ref iUnprocecced, strDBIdx);
            this.mc = mc;
            return GenWordReportForStoryBoardId(projectName, storyboardName, objStoryBrdSumInfo, strDBIdx,
                 outputType.ToUpper(), outputPath, list);
        }

        private bool StoryboardIsRequiredForReport(string storyboardName)
        {
            if (storyboardName.Equals("WAIT Testing") == false)
                return true;
            else
                return false;
        }

        private bool ProjectIsRequiredForReport(string projectName)
        {
            if (projectName.Equals("FHLBC Repo") || projectName.Equals("FHLBC Treasury Products"))
                return true;
            else
                return false;
        }
       
       
        // Entry point for Detail Report
        public void GenWordReportForCurrentTestStoryBoard(long CurrentStoryBoardID, string storyboardName, string strDBIdx)
        {
            KillWordProc("MARS_TEMPLATE");
            string strError = "";
            int iUnprocecced = 0;
           
            //
            V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(CurrentStoryBoardID , ref iUnprocecced, strDBIdx);

            //config.StoryBoardConfig.StoryBoardDescr = objStoryBrdSumInfo.
            if (objStoryBrdSumInfo == null)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Can't get Storyboard summary information:Name:[{0}] ID:[{1}]", CurrentStoryBoardID, storyboardName));
                ShowMessage(strError, "Error");
                return;
            }

            GenWordReportForStoryBoardId("", storyboardName, objStoryBrdSumInfo, strDBIdx);

        }

        private void KillWordProc(string templateName)
        {
            Logger.logBegin("KillWordProc");
            try
            {

                System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("WINWORD");
                foreach (System.Diagnostics.Process CurrentProcess in processes)
                {
                    if (CurrentProcess.MainWindowTitle.Contains(templateName))
                    {
                        CurrentProcess.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("KillWordProc", string.Format("{0}", ex.Message), ex);
            }
            Logger.logEnd("KillWordProc");
        }

        public WordReportConfig GenWordReportForStoryBoardId(string projectName, string storyboardName, V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo, 
            string strDBIdx,
            string outputType = "WORD", string outputFolder = null,List<FlexReportDataSetConfig> list = null)
        {
            WordReportConfig config = new WordReportConfig();
            Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 1);
            // Word Doc Generation
            Logger.logBegin("GenWordReportForStoryBoardId");
            try
            {
                
                // Configure file locations
                //string templateWordDoc = "";
                // string outputWordDoc = "";

                config.TemplateFilePath = "";
                config.OutputFilePath = "";

                // Misc info

                string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                config.ReportGenDate = currentDateTime;


                // Configure Storyboard report data

                //String storyboardName = "MyStoryboard";
                String storyboardDescr = "MyStoryboard Description";

                DataTable sbdt = new DataTable();
                sbdt.Columns.Add("#");
                //sbdt.Columns.Add("Step_Name");
                
                sbdt.Columns.Add("TS_Name");
                sbdt.Columns.Add("TC_Name");
                sbdt.Columns.Add("Data_Set");
                sbdt.Columns.Add("Data_Set_Descr");

                sbdt.Columns.Add("BL Start");
                sbdt.Columns.Add("BL Duration");
                sbdt.Columns.Add("CP Start");
                sbdt.Columns.Add("CP Duration");
                sbdt.Columns.Add("BL Result");
                sbdt.Columns.Add("CP Result");

                config.ConfigureStoryBoard(storyboardName, storyboardDescr, sbdt);
                config.ProjectDescription = "My Project Description";

                string strError = "";

                config.StoryBoardConfig.StoryBoardName = objStoryBrdSumInfo.STORYBOARD_NAME;

                // Configure Report template data

                //string strPath = ConfigurationManager.AppSettings["REPORT_PATH"];
                string strPath = mc.GetReportConfig().reportPath;
                if (outputFolder != null)
                {
                    strPath = outputFolder;

                }
                // string reportTemplatePath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];
                string reportTemplatePath = mc.GetReportConfig().reportTemplatePath;


                config.TemplateFilePath = reportTemplatePath + "\\" + "MARS_TEMPLATE.docx";

                Logger.Info("GenWordReportForStoryBoardId", "config.TemplateFilePath :" + config.TemplateFilePath);

                if (File.Exists(config.TemplateFilePath) == false)
                {
                    ShowMessage("WARNING: Report template file " + config.TemplateFilePath + " is not found");
                    return null;
                }

                Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 2);
                // compose OutputFilePath
                string fileType = "";
              
                if (outputType.Equals("WORD"))
                    fileType = ".docx";
                else
                    if (outputType.Equals("EXCEL"))
                        fileType = ".xlsx";

                if (outputFolder != null)
                    strPath = outputFolder;

                config.OutputFilePath = strPath + "\\" + "MarsTestReport_" + config.StoryBoardConfig.StoryBoardName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + fileType;

                Logger.Info("GenWordReportForStoryBoardId", "config.OutputFilePath :" + config.OutputFilePath);

                // Fill stats config data

                if (Directory.Exists(strPath) == false)
                {
                    ShowMessage("WARNING: Report folder " + strPath + " is not found");
                    return null;
                }

                // Configure data for "Testing Summary"
                config.MarsTSCount = (int)(objStoryBrdSumInfo.TSCNT ?? 0);
                config.MarsTCCount = (int)(objStoryBrdSumInfo.TCCNT ?? 0);
                config.MarsTestStepCount = (int)(objStoryBrdSumInfo.STEP_CNT ?? 0);

                // Configure data for "Result Summary" and the Pie Chart
                StoryboardStats sbs = ComputeStoryboardStats((long)objStoryBrdSumInfo.STORYBOARD_ID, strDBIdx);

                // unit test 
                //B_V_STORYBOARD_TEST_FULLVISION stryboardInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                //bool isOk = false;
                //var dtlTable = stryboardInfo.FetchCapturedDataAsDataTable(objStoryBrdSumInfo.STORYBOARD_ID, strDBIdx, ref isOk, ref strError);


                config.MarsBSucc = sbs.MarsBSucc;
                config.MarsCSucc = sbs.MarsCSucc;
                config.MarsBFail = sbs.MarsBFail;
                config.MarsCFail = sbs.MarsCFail;
                config.MarsBPartial = sbs.MarsBPartial;
                config.MarsCPartial = sbs.MarsCPartial;
                config.MarsBUnpr = config.MarsTCCount - (sbs.MarsBSucc + sbs.MarsBFail);
                config.MarsCUnpr = config.MarsTCCount - (sbs.MarsCSucc + sbs.MarsCFail + sbs.MarsCPartial);

                //Configure data for the Storyboard         
                List<V_STORYBOARD_TEST_FULLVISIONDTO> currentStoryBoardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(objStoryBrdSumInfo.STORYBOARD_ID, strDBIdx);
                currentStoryBoardInfo = new List<V_STORYBOARD_TEST_FULLVISIONDTO>(currentStoryBoardInfo.OrderBy(p => p.RUN_ORDER));

                /// Added by tiger, 04-01-2019, no April fool
                /// Reason:
                ///   report can't get the righht status of storyboard
                ///   Therefore, calling how Storyboard of GUI getting status is the cheapst way
                #region  added by tiger ///
                List<B_STORYBOARD_TEST_FULLVISION> lstStatusFromBO = BoHelper.GetStoryboardRows(strDBIdx, objStoryBrdSumInfo.STORYBOARD_ID);
                #endregion

                B_T_PROJ_TEST_RESULT ptr = new B_T_PROJ_TEST_RESULT();

                var projTestResultList = ptr.GetProjTestResult(strDBIdx);

                config.ProjectDescription = (from o in currentStoryBoardInfo select o).FirstOrDefault().PROJECT_DESCRIPTION;
                config.ProjectName = (from o in currentStoryBoardInfo select o).FirstOrDefault().PROJECT_NAME;

                var sbId = (from o in currentStoryBoardInfo select o).FirstOrDefault().STORYBOARD_ID;

                string sbDescr = B_STORYBOARD_SUMMARY.GetStoryBoardInfoById(strDBIdx,sbId).DESCRIPTION;
                config.StoryBoardConfig.StoryBoardDescr = sbDescr;
                Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 3);
                foreach (var row in currentStoryBoardInfo)
                {
                    var baselineProjTestResult = GetProjTestResult(projTestResultList, 1, row.STORYBOARD_DETAIL_ID);
                    var compareProjTestResult = GetProjTestResult(projTestResultList, 0, row.STORYBOARD_DETAIL_ID);

                    DataRow newDTRow = sbdt.NewRow();
                    sbdt.Rows.Add(newDTRow);
                    newDTRow["#"] = row.RUN_ORDER;

                    newDTRow["TC_Name"] = row.TEST_CASE_NAME;
                    newDTRow["TS_Name"] = row.TEST_SUITE_NAME;
                    newDTRow["Data_Set"] = row.DATA_SET_ALIAS_NAME;
                    newDTRow["Data_Set_Descr"] = row.DATASET_DESCRIPTION;

                    string status = row.HIST_TEST_RESULT_IN_TEXT;
                    if ((!string.IsNullOrEmpty(status)) && (status.StartsWith("SUCCESS")))
                        status = "PASS";

                    if (status == null || status.StartsWith("Begin"))
                        status = "UNPR";
                    else if (status.StartsWith("FAILED"))
                        status = "FAILED";

                    newDTRow["BL Result"] = GenTestCaseStatus(baselineProjTestResult, row, "BL", sbs);

                    #region  added by tiger ///
                    var statusOFSB = lstStatusFromBO.FirstOrDefault(p => p.STORYBOARD_DETAIL_ID == row.STORYBOARD_DETAIL_ID);

                    if (statusOFSB != null)
                    {
                        Logger.Info("GenWordReportForStoryBoardId", string.Format("Find status result:{0}, run_order:[{1}]", statusOFSB.HIST_RESULT, statusOFSB.RUN_ORDER));
                        if (statusOFSB.HIST_RESULT == 3)
                        {
                            newDTRow["CP Result"] = "PARTIAL";
                        }
                        else if (statusOFSB.HIST_RESULT == 1)
                        {
                            newDTRow["CP Result"] = "PASS";
                        }
                        else
                            newDTRow["CP Result"] = "FAIL";
                    }
                    else
                        #endregion



                        newDTRow["CP Result"] = GenTestCaseStatus(compareProjTestResult, row, "CP", sbs);

                    newDTRow["BL Start"] = baselineProjTestResult == null ? null : baselineProjTestResult.TEST_BEGIN_TIME;
                    newDTRow["BL Duration"] = baselineProjTestResult == null ? null : (baselineProjTestResult.TEST_END_TIME - baselineProjTestResult.TEST_BEGIN_TIME);

                    newDTRow["CP Start"] = compareProjTestResult == null ? null : compareProjTestResult.TEST_BEGIN_TIME;
                    newDTRow["CP Duration"] = compareProjTestResult == null ? null : (compareProjTestResult.TEST_END_TIME - compareProjTestResult.TEST_BEGIN_TIME);
                }

                // Recalculate stats
                config.MarsCFail = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "FAIL" select row).Count();
                config.MarsCPartial = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "PARTIAL" select row).Count();
                config.MarsCSucc = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "PASS" select row).Count();
                config.MarsCUnpr = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "UNPR" select row).Count();

                config.MarsBFail = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "FAIL" select row).Count();
                config.MarsBPartial = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "PARTIAL" select row).Count();
                config.MarsBSucc = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "PASS" select row).Count();
                config.MarsBUnpr = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "UNPR" select row).Count();
                Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 4);
                List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptBaseline =
                GetTestStepReportViaStoryBoardId((long)objStoryBrdSumInfo.STORYBOARD_ID, 1, strDBIdx, true)??new List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>>();                

                List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptCompare =
                    GetTestStepReportViaStoryBoardId((long)objStoryBrdSumInfo.STORYBOARD_ID, 0, strDBIdx, true)??new List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>>();

                if (lstStepsRptCompare == null || lstStepsRptCompare.Count == 0)
                {
                    if (System.AppDomain.CurrentDomain.FriendlyName.ToLower().Equals("mars.exe"))
                    {
                        ShowMessage("WARNING: Compare result data is not available. \n Report can not be generated without result data.");
                    }
                    else
                        Console.WriteLine("WARNING: Compare result data is not available. \n Report can not be generated without result data.");
                    return null;
                }

                Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 5);

                bool isRight = false;
                Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicTestCaseInfo 
                    = B_TEST_CASE.GetTestCaseViaStoryBoardId(strDBIdx,(long)objStoryBrdSumInfo.STORYBOARD_ID, ref strError, ref isRight);

                /// Added by tiger, 2021-0407
                /// get all data tables by storyboardId
                /// 
                B_V_STORYBOARD_TEST_FULLVISION objStryboardInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                bool isOk = false;
                Dictionary<string, Dictionary<string, DataTable[]>> hashDataTables = objStryboardInfo.FetchCapturedDataAsDataTable(objStoryBrdSumInfo.STORYBOARD_ID, strDBIdx, ref isOk, ref strError); 
                if (!isOk)
                {
                    Logger.Error("GenWordReportForStoryBoardId", strError);
                    throw new Exception($"Can't Fetch Captured data from {strDBIdx} ");
                }
                /// added by tiger end, 2021-0407

                Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 6);
                //string strError = "";
                for (int i = 0; i < lstStepsRptCompare.Count; i++)
                {
                    bool baselineExists = true;
                    KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>);
                    KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>);

                    try
                    {

                        // if (default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>).Equals(lstStepsRptCompare[i]))
                        // {

                        // }
                        //  KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = lstStepsRptCompare[i];
                        //  KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = lstStepsRptBaseline[i];

                        stryBordDtlCompare = lstStepsRptCompare[i];
                        if (lstStepsRptBaseline.Count > i)
                            stryBordDtlBaseline = lstStepsRptBaseline[i];
                        else
                            baselineExists = false;


                    }
                    catch (Exception e)
                    {

                        Logger.Error("GenWordReportForCurrentTestStoryBoard", string.Format("{0}", e.Message), e);
                    }
                    
                    TestStepsReportGridData objStepRptData = new TestStepsReportGridData();
                    objStepRptData.GridData = stryBordDtlCompare;

                    /// write head 
                    /// 
                    string strTestcaseSectionInfo = string.Format("3.{0}. Test Case", i + 1);
                    int iTestCaseId = objStepRptData.GetTestCaseId(ref strError);
                    if (iTestCaseId < 0)
                    {
                        Logger.Error("GenStoryBoardTestCaseDetail", strError);
                        /// write error info to report
                        /// 
                        strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        {1}", strTestcaseSectionInfo, strError);

                        continue;
                    }
                    Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 7);

                    var queryTC = from tc in dicTestCaseInfo.Keys
                                  where tc.TEST_CASE_ID == iTestCaseId
                                  select tc;
                    T_TEST_CASE_SUMMARYDTO objTCSum = queryTC.FirstOrDefault();
                    if (objTCSum == null)
                    {
                        strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        Can't find Test case summary info from database.", strTestcaseSectionInfo);
                        continue;
                    }
                    /// get test case info               
                    strTestcaseSectionInfo = string.Format("{0} Name:{1}\r\n3.{3}.1. Test Case Description:\r\n {2}\r\n3.{3}.2. Test Case Summary", strTestcaseSectionInfo, objTCSum.TEST_CASE_NAME, objTCSum.TEST_STEP_DESCRIPTION ?? "(N/A)", i + 1);

                    string testCaseName = objTCSum.TEST_CASE_NAME;
                    var sbDetailId = stryBordDtlCompare.Value.FirstOrDefault().Value.FirstOrDefault().STORYBOARD_DETAIL_ID;
                    string dataSetName = (from m in currentStoryBoardInfo where m.STORYBOARD_DETAIL_ID == sbDetailId select m.DATA_SET_ALIAS_NAME).FirstOrDefault();

                    Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 8);
                    // Here we are filtering 
                    bool isRequired = false;
                    foreach (var item in list)
                    {
                        if (item.dataSet.Equals(dataSetName))
                        {
                            isRequired = true;
                        }
                    }

                    if (isRequired == false)
                        continue;

                    string dataSetDescr = (from m in currentStoryBoardInfo where m.STORYBOARD_DETAIL_ID == sbDetailId select m.DATASET_DESCRIPTION).FirstOrDefault();
                    //string dataSetName = objTCSum.;

                    DataTable tcdt = new DataTable();

                    //tcdt.Columns.Add("REF");
                    tcdt.Columns.Add("#", typeof(int));
                    tcdt.Columns.Add("Keyword");
                    tcdt.Columns.Add("Object Name");
                    tcdt.Columns.Add("Parameters");
                    tcdt.Columns.Add("Input");
                    tcdt.Columns.Add("Outp Baseline");
                    tcdt.Columns.Add("Outp Compare");
                    tcdt.Columns.Add("Status");
                    tcdt.Columns.Add("Img");
                    //config.ConfigureTestCase(testCaseName, dataSetName, dataSetDescr, tcdt);

                    var stpCmp = stryBordDtlCompare.Value.FirstOrDefault();
                    if ((stpCmp.Equals(default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>))) || (stpCmp.Value == null)) continue;

                    foreach (var testStepCompare in stryBordDtlCompare.Value.FirstOrDefault().Value)
                    {
                        if (testStepCompare == null) continue;

                        DataRow newDTRow = tcdt.NewRow();
                        tcdt.Rows.Add(newDTRow);

                        newDTRow["#"] = testStepCompare.RUN_ORDER;
                        newDTRow["Keyword"] = testStepCompare.KEY_WORD_NAME;
                        newDTRow["Object Name"] = testStepCompare.OBJECT_HAPPY_NAME;
                        newDTRow["Parameters"] = testStepCompare.COLUMN_ROW_SETTING;
                        newDTRow["Input"] = testStepCompare.INPUT_VALUE_SETTING;

                        // Fill bValue with NA if data is not available
                        string bValue = "NA";
                        if (baselineExists)
                        {
                            // This should not be done if baseline is missing
                            var testStepBaseline =
                                (from o in stryBordDtlBaseline.Value.FirstOrDefault().Value
                                 where o.RUN_ORDER == testStepCompare.RUN_ORDER
                                 select o).FirstOrDefault();
                            if (testStepBaseline != null && testStepBaseline.RETURN_VALUES != null)
                            {
                                bValue = CleanupValues(testStepBaseline.RETURN_VALUES);
                                
                            }
                            else
                                bValue = "";

                            if (testStepBaseline != null && testStepBaseline.INFO_PIC != null)
                            {
                                GeneratePictureFile(testStepBaseline.INFO_PIC, testCaseName, dataSetName, testStepBaseline.RUN_ORDER, "B", testStepBaseline.TEST_REPORT_STEP_ID);
                            }
                        }

                        string cValue = CleanupValues(testStepCompare.RETURN_VALUES);
                        newDTRow["Outp Baseline"] = bValue;
                        newDTRow["Outp Compare"] = cValue;

                        string status = testStepCompare.RUNNING_RESULT_INFO;
                        if (status != null && status.StartsWith("SUCCESS"))
                            status = "PASS";

                        // Exclued comaring trade id's
                        if (testStepCompare.OBJECT_HAPPY_NAME != null && testStepCompare.OBJECT_HAPPY_NAME.EndsWith("TRADE_ID") == false && bValue.Equals(cValue) == false)
                        {
                            status = "PARTIAL";
                        }

                        newDTRow["Status"] = status;

                        if (testStepCompare.INFO_PIC != null)
                        {
                            string fileName = GeneratePictureFile(testStepCompare.INFO_PIC, testCaseName, dataSetName, testStepCompare.RUN_ORDER, "C", testStepCompare.TEST_REPORT_STEP_ID);
                            newDTRow["Img"] = fileName;
                        }
                    }
                    Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 9);
                    // Sort TestCase by step #
                    DataView dv = tcdt.DefaultView;
                    dv.Sort = "#";
                    tcdt = dv.ToTable();

                    /////////
                    // TESTING!  HAS to be removed later
                    //string path = @"C:\software\MarsAutomation\ReportTemplates\TestTables.xlsx";
                    Dictionary<string, DataTable> reportTables = new Dictionary<string, DataTable>();
                    //if (File.Exists(path) == true)
                    //{
                    //     reportTables = ExcelReportGen.GetReportTables(path);
                    //}

                    /// datatable from DB
                    /// 

                    Dictionary<string, DataTable> newReportTables = new Dictionary<string, DataTable>();

                    if (hashDataTables.ContainsKey(sbDetailId + ""))
                    {
                        var targetTables = hashDataTables[sbDetailId + ""];
                        if (targetTables != null)
                        {
                            reportTables = B_Storybard_test_DataSummaryForReport.convertToReportFormat(targetTables);
                        }

                        Console.WriteLine("Tables found in targetTables");
                        foreach (string tblName in reportTables.Keys)
                        {
                            string realTableName = tblName;
                            // Remove word TABLE_ from keys
                            string prefix = "TABLE_";

                            if (tblName.StartsWith(prefix))
                            {
                                string newKey = tblName.Replace(prefix, "");
                                var table = reportTables[tblName];
                                
                                newReportTables.Add(newKey, table);
                                realTableName = newKey;

                                Console.WriteLine("Table " + realTableName);
                            }
                            else
                                newReportTables.Add(tblName, reportTables[tblName]);
                            Console.WriteLine("Table " + tblName + " renamed to " + realTableName);
                        }
                    }

                    // Dump data tables



                    Console.WriteLine("Contents of Dict reportTables");
                    foreach (var key in reportTables.Keys)
                    {
                        Console.WriteLine("Contents of Table = " + key);
                        Console.WriteLine(DumpDataTable(reportTables[key]));
                        Console.WriteLine("==================================");
                    }

                    Console.WriteLine("==================================");
                    Console.WriteLine("==================================");

                    Console.WriteLine("Contents of Dict newReportTables");
                    foreach (var key in newReportTables.Keys)
                    {
                        Console.WriteLine("Contents of Table = " + key);
                        Console.WriteLine(DumpDataTable(newReportTables[key]));
                        Console.WriteLine("==================================");
                    }

                    //config.ConfigureTestCase(projectName, storyboardName, testCaseName, dataSetName, dataSetDescr, tcdt, reportTables);
                    config.ConfigureTestCase(projectName, storyboardName, testCaseName, dataSetName, dataSetDescr, tcdt, newReportTables);
                    Console.WriteLine("GenWordReportForStoryBoardId Locator --> " + 10);
                }

                
                if (IS_FLEX_REPORT == false)
                {
                
                    // generate report
                    if (outputType.Equals("WORD"))
                    {
                        WordReportGen gen = new WordReportGen(config);
                        gen.OpenDocument();
                        gen.GenerateDocument();
                        gen.SaveDocument();
                    }

                    else
                    if (outputType.Equals("EXCEL"))
                    {
                        config.TemplateFilePath = mc.GetReportConfig().reportTemplatePath + @"\MarsReportTemplate.xlsx";
                        config.ReportTableWord = mc.GetReportConfig().reportTableWord;
                        ExcelReportGen gen = new ExcelReportGen(config);
                        gen.OpenDocument();
                        gen.GenerateStandardDocument();
                        gen.SaveDocument();
                    }

                    string temp = System.AppDomain.CurrentDomain.FriendlyName.ToLower();
                    if (System.AppDomain.CurrentDomain.FriendlyName.ToLower().Equals("mars.exe"))
                        ShowMessage("Report is created in " + config.OutputFilePath);
                    else
                        Console.WriteLine("\nReport is created in " + config.OutputFilePath);
                }
            }
            catch (Exception e)
            {
                Logger.Error("GenWordReportForCurrentTestStoryBoard", string.Format("{0}-\r\n{1}", e.Message, e.StackTrace), e);
                Console.WriteLine(string.Format("{0}-\r\n{1}", e.Message, e.StackTrace), e);
            }
            Logger.logEnd("GenWordReportForStoryBoardId");

            return config;
        }

        public static string DumpDataTable(DataTable table)
        {
            string data = string.Empty;
            StringBuilder sb = new StringBuilder();

            if (null != table && null != table.Rows)
            {
                foreach (DataRow dataRow in table.Rows)
                {
                    foreach (var item in dataRow.ItemArray)
                    {
                        sb.Append(item);
                        sb.Append(',');
                    }
                    sb.AppendLine();
                }

                data = sb.ToString();
            }
            return data;
        }

        private object GenTestCaseStatus(T_PROJ_TEST_RESULTDTO projTestResult, V_STORYBOARD_TEST_FULLVISIONDTO combined, string modev, StoryboardStats sbs)
        {
            string status = "UNPR";
            if (projTestResult != null)
            {
                status = projTestResult.TEST_RESULT_IN_TEXT;
            }

            if (modev.Equals("CP") &&
                projTestResult != null &&
                combined != null)
            {
                if (sbs.sbStatusDict.Keys.Contains(combined.STORYBOARD_DETAIL_ID) &&
                    sbs.sbStatusDict[combined.STORYBOARD_DETAIL_ID] == (int)TestCaseStatus.PARTIAL)
                    status = "PARTIAL";
            }

            if (status != null && status.StartsWith("SUCCESS"))
                status = "PASS";

            if (status != null &&
                (status.StartsWith("FAIL") ||
                 status.StartsWith("Begin") ||
                 status.StartsWith("Exception")))

                status = "FAIL";

            return status;
        }

        public enum TestCaseStatus
        {
            UNPR = 1,
            FAIL = 2,
            PARTIAL = 3,
            PASS = 4
        }

        StoryboardStats ComputeStoryboardStats(long sbId,string strDBIdx)
        {
            StoryboardStats sbs = new StoryboardStats();
            sbs.MarsTSCount = 0;
            sbs.MarsTCCount = 0;

            sbs.MarsTestStepCount = 0;

            sbs.MarsBSucc = 0;
            sbs.MarsCSucc = 0;
            sbs.MarsBFail = 0;
            sbs.MarsCFail = 0;
            sbs.MarsBUnpr = 0;
            sbs.MarsCUnpr = 0;
            sbs.MarsBPartial = 0;
            sbs.MarsCPartial = 0;

            //#region tiger Added
            List<B_STORYBOARD_TEST_FULLVISION> bStoryboardRows = BoHelper.GetStoryboardRows(strDBIdx,sbId);
            bStoryboardRows = bStoryboardRows.OrderBy(p => p.RUN_ORDER).ToList();
            //#endregion

            #region tiger Comment
            //List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptBaseline =
            //  GetTestStepReportViaStoryBoardId(sbId, 1, true);

            //List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptCompare =
            //   GetTestStepReportViaStoryBoardId(sbId, 0, true);
            #endregion
            #region tiger added
            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptBaseline =
              GetTestStepReportViaStoryBoardId(sbId, 1, strDBIdx,false);

            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptCompare =
               GetTestStepReportViaStoryBoardId(sbId, 0, strDBIdx, false);
            #endregion
            // Go through all storyboard lines
            for (int i = 0; i < lstStepsRptCompare.Count; i++)
            {
                TestCaseStatus bStatus = TestCaseStatus.PASS;
                TestCaseStatus cStatus = TestCaseStatus.PASS;

                KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = lstStepsRptCompare[i];

                Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> stryBordDtlCompareDict = lstStepsRptCompare[i].Value;

                long sbCompareDetId = (from o in stryBordDtlCompareDict
                                       select o.Value.FirstOrDefault().STORYBOARD_DETAIL_ID).FirstOrDefault().Value;

                // not sure if this is correct
                var stryBordDtlBaselineDict =
                    (from o in lstStepsRptBaseline
                     where o.Value.FirstOrDefault().Value.FirstOrDefault().STORYBOARD_DETAIL_ID == sbCompareDetId
                     select o);


                // There is a potential carash here !!! If baseline was not run completely, FIX it!!!
                //   KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = lstStepsRptBaseline[i];

                if (stryBordDtlCompare.Value == null)
                {
                    cStatus = TestCaseStatus.UNPR;
                }

                if (stryBordDtlBaselineDict == null)
                {
                    bStatus = TestCaseStatus.UNPR;
                }

                #region tiger added
                bool   isExtendObj             = false;
                string extendSrcObjResult      = null ;
                // stored extend steps with partial
                List<Int64> stepsExtendPartial = new List<Int64>();
                stepsExtendPartial.Clear();
                #endregion
                // go through evey testStep to find if there are any differences
                foreach (var testStepCompare in stryBordDtlCompare.Value.FirstOrDefault().Value)
                {
                    sbs.MarsTestStepCount++;
                    isExtendObj     = false;

                    #region tiger added
                    if (testStepCompare != null) {
                        if (testStepCompare.RUNNING_RESULT_INFO == null)
                        {
                            var source  = stryBordDtlCompare.Value.FirstOrDefault().Value.Where(p => (p.RUN_ORDER == testStepCompare.RUN_ORDER) && (p.RUNNING_RESULT_INFO != null)).FirstOrDefault();
                            if (source != null)
                            {
                                if (source.TEST_REPORT_STEP_ID != testStepCompare.TEST_REPORT_STEP_ID)
                                {
                                    extendSrcObjResult = source.RUNNING_RESULT_INFO ;
                                    isExtendObj        = true                       ;
                                }
                            }
                        }
                    }
                    #endregion

                    V_TEST_DATA_REPORT_SUMMARYDTO testStepBaseline = null;

                    if (stryBordDtlBaselineDict.FirstOrDefault().Value != null)
                    {

                        try
                        {
                            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> baseListDict = stryBordDtlBaselineDict.FirstOrDefault().Value;
                            IEnumerable<V_TEST_DATA_REPORT_SUMMARYDTO> arrDtos = baseListDict.SelectMany(p => p.Value);
                            if (arrDtos == null)
                            {
                                testStepBaseline = null;
                            }
                            else
                            {
                                #region tiger comment
                                //V_TEST_DATA_REPORT_SUMMARYDTO ob = arrDtos.Where(p => p.RUN_ORDER == testStepCompare.RUN_ORDER).FirstOrDefault();
                                #endregion
                                #region tiger add
                                V_TEST_DATA_REPORT_SUMMARYDTO ob = null;
                                if (testStepCompare.INPUT_VALUE_SETTING==null)
                                    ob = arrDtos.Where(p => (p.RUN_ORDER == testStepCompare.RUN_ORDER)&&(p.INPUT_VALUE_SETTING==null)).FirstOrDefault();
                                else
                                    ob = arrDtos.Where(p => (p.RUN_ORDER == testStepCompare.RUN_ORDER) && (string.Compare(p.INPUT_VALUE_SETTING,testStepCompare.INPUT_VALUE_SETTING, true)==0)).FirstOrDefault();
                                #endregion

                                if (ob == null)
                                {
                                    testStepBaseline = null;
                                }
                                else
                                {
                                    testStepBaseline = ob;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Logger.Error("ComputeStoryboardStats", string.Format("Exception:[{0}]", e.Message), e);
                            testStepBaseline = null;
                        };
                    }

                    if (testStepCompare != null
                        && testStepCompare.RUNNING_RESULT_INFO != null
                        && testStepCompare.RUNNING_RESULT_INFO.Contains("Exception"))
                    {
                        Logger.Info("ComputeStoryboardStats", "Exception detected");
                    }


                    if (testStepCompare == null || testStepCompare.RUNNING_RESULT_INFO == null)
                    {
                        #region added tiger
                        //only non-extended object should update status count
                        if (!isExtendObj)
                        #endregion
                        cStatus = UpdateStatusValue(cStatus, TestCaseStatus.UNPR);
                    }

                    else if (testStepCompare.RUNNING_RESULT_INFO.Equals("FAIL") || testStepCompare.RUNNING_RESULT_INFO.Contains("Exception"))
                    {
                        #region added tiger
                        //only non-extended object should update status count
                        if (!isExtendObj)
                            #endregion
                            cStatus = UpdateStatusValue(cStatus, TestCaseStatus.FAIL);
                    }

                    if (testStepBaseline == null || testStepBaseline.RUNNING_RESULT_INFO == null)
                    {
                        #region added tiger
                        //only non-extended object should update status count
                        if (!isExtendObj)
                            #endregion
                            bStatus = TestCaseStatus.UNPR;
                    }

                    else if (testStepBaseline.RUNNING_RESULT_INFO.Equals("FAIL") || testStepBaseline.RUNNING_RESULT_INFO.Contains("Exception"))
                    {
                        #region added tiger
                        //only non-extended object should update status count
                        if (!isExtendObj)
                            #endregion
                            bStatus = UpdateStatusValue(bStatus, TestCaseStatus.FAIL);
                    }

                    string bValue = "";

                    if (testStepBaseline != null)
                        bValue = CleanupValues(testStepBaseline.RETURN_VALUES);

                    string cValue = CleanupValues(testStepCompare.RETURN_VALUES);


                    string status = testStepCompare.RUNNING_RESULT_INFO;
                    #region added tiger
                    //only non-extended object should update status count
                    if (isExtendObj)
                        status = extendSrcObjResult;
                    #endregion
                    if ((status != null) && (status.StartsWith("SUCCESS")))
                    {
                        status = "PASS";
                    }

                    // Excluded comaring trade id's
                    if (testStepCompare.OBJECT_HAPPY_NAME != null && testStepCompare.OBJECT_HAPPY_NAME.EndsWith("TRADE_ID") == false && bValue.Equals(cValue) == false)
                    {
                        #region tiger added
                        if (isExtendObj)
                        {
                            if (stepsExtendPartial.IndexOf(testStepCompare.STEPS_ID) < 0)
                            {
                                status = "PARTIAL";
                                cStatus = UpdateStatusValue(cStatus, TestCaseStatus.PARTIAL);
                                stepsExtendPartial.Add(testStepCompare.STEPS_ID);
                            }
                        }
                        else
                        #endregion
                        {
                            status = "PARTIAL";
                            cStatus = UpdateStatusValue(cStatus, TestCaseStatus.PARTIAL);
                            #region tiger added
                            stepsExtendPartial.Add(testStepCompare.STEPS_ID);
                            #endregion
                        }
                    }
                }

                #region tiger added
                if (!isExtendObj)
                #endregion
                {
                    switch (bStatus)
                    {
                        case TestCaseStatus.PASS:
                            sbs.MarsBSucc++;
                            break;
                        case TestCaseStatus.FAIL:
                            sbs.MarsBFail++;
                            break;
                        case TestCaseStatus.UNPR:
                            sbs.MarsBUnpr++;
                            break;
                        #region tiger comment
                        //case TestCaseStatus.PARTIAL:
                        //    sbs.MarsBPartial++;
                        //    break;
                        #endregion
                        default:
                            break;

                    }

                    switch (cStatus)
                    {
                        case TestCaseStatus.PASS:
                            sbs.MarsCSucc++;
                            break;
                        case TestCaseStatus.FAIL:
                            sbs.MarsCFail++;
                            break;
                        case TestCaseStatus.UNPR:
                            sbs.MarsCUnpr++;
                            break;
                        #region tiger comment
                        //case TestCaseStatus.PARTIAL:
                        //    sbs.MarsCPartial++;
                        //    break;
                        #endregion
                        default:
                            break;

                    }
                }
                #region tiger added
                for (int k=0;k< stepsExtendPartial.Count; k++)
                {
                    sbs.MarsCPartial++;
                }
                #endregion
                sbs.sbStatusDict.Add(sbCompareDetId, (int)cStatus);
            }

            

            return sbs;
        }

        // while iterating through all testcase steps we should be careful not to overwrite "worth" status with a "better" one
        private TestCaseStatus UpdateStatusValue(TestCaseStatus oldStatus, TestCaseStatus newStatus)
        {
            TestCaseStatus updatedStatus = oldStatus;

            if (newStatus < oldStatus)
                updatedStatus = newStatus;

            return updatedStatus;
        }

        private T_PROJ_TEST_RESULTDTO GetProjTestResult(List<T_PROJ_TEST_RESULTDTO> projTestResultList, int testMode, long sbDetId)
        {
            T_PROJ_TEST_RESULTDTO result = null;

            var maxValue = (from o in projTestResultList
                            where o.STORYBOARD_DETAIL_ID == sbDetId && o.TEST_MODE == testMode
                            select o).Max(p => p.LATEST_TEST_MARK_ID);

            result = (from o in projTestResultList
                      where o.STORYBOARD_DETAIL_ID == sbDetId && o.TEST_MODE == testMode && o.LATEST_TEST_MARK_ID == maxValue
                      select o).FirstOrDefault();

            return result;
        }

        private string CleanupValues(string inpValues)
        {
            string outpValues = "";
            bool multiPart = false;

            if (inpValues != null)
            {
                char[] delimiters = new char[] { '\r', '\n' };
                string[] parts = inpValues.Split(delimiters,
                             StringSplitOptions.RemoveEmptyEntries);

                int partsCount = parts.Count();
                if (partsCount > 1)
                {
                    multiPart = true;
                }

                int lineNum = 1;

                foreach (string word in parts)
                {
                    string line = "";

                    Double n;

                    bool isNumeric = Double.TryParse(word, out n);

                    if (word.Contains("+308") || (isNumeric && Math.Abs(n) > 100000000000000))
                    {
                        line = "0";
                    }
                    else if (word.Equals("1/1/0001"))
                    {
                        line = " ";
                    }
                    else
                        line = word;


                    if (multiPart)
                        line = "" + lineNum++ + ":   " + line;

                    outpValues += line + "\r";
                }
            }
            outpValues = outpValues.TrimEnd('\r');
            return outpValues;
        }

        public string GeneratePictureFile(System.Byte[] pictureBytes, string testCaseName, string dataSetName, decimal? testStepNumber, string mode, long testreport_id)
        {
            // string strTmpFileName = Guid.NewGuid().ToString() + ".png";
            //string strTmpFileName = $"__{testCaseName}__{dataSetName}__{testStepNumber}__{testreport_id}.png"; 
            string strTmpFileName = "__" + testCaseName + "__" + dataSetName + "__" + testStepNumber + ".png";
            // strTmpFileName = System.IO.Path.Combine(MarsReportGen.TempPicturePath, strTmpFileName);

            string reportImagePath = @"c:\temp";

            try
            {
                //reportImagePath = ConfigurationManager.AppSettings["REPORT_IMAGE_PATH"];
                reportImagePath = mc.GetReportConfig().reportImagePath;
                if (!System.IO.Directory.Exists(reportImagePath))
                {
                    System.IO.Directory.CreateDirectory(reportImagePath);
                }
            }
            catch (Exception e) {
                throw new Exception($"Can't get and create directory [{reportImagePath}]");
            }

            string realFileName = System.IO.Path.Combine(reportImagePath, mode + strTmpFileName);
           
            strTmpFileName = System.IO.Path.Combine(reportImagePath, strTmpFileName);

            

            if (File.Exists(realFileName))
            {
                File.Delete(realFileName);
            }

            bool NEW_WAY = true;

            // Save Image new way begin
            if (NEW_WAY == true)
            {
                ImageHelper.SaveImage(pictureBytes, realFileName);
            }

            // Save Image new way end
            else
            {
                FileStream objImgStream = File.Open(realFileName, FileMode.CreateNew);
                objImgStream.Seek(0, SeekOrigin.Begin);
                objImgStream.Write(pictureBytes, 0, pictureBytes.Length);
                objImgStream.Close();
            }
            return strTmpFileName;
        }

        
        private List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> GetTestStepReportViaStoryBoardId(long testStoryBoardId, 
            int testMode, 
            string strDBIdx,
            bool isNormalizationReq = false)
        {
            Logger.Info("GetTestStepReportViaStoryBoardId", string.Format("try to get teststeps report info via storyboardid:[{0}]", testStoryBoardId));
            B_V_TEST_DATA_REPORT_SUMMARY objTestDtRpt = new B_V_TEST_DATA_REPORT_SUMMARY();
            string strError = "";
            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstTestStepRpt = 
                objTestDtRpt.getTestStpReportDataByTestStoryBoardId(strDBIdx,testStoryBoardId, ref strError, testMode, isNormalizationReq);
            return lstTestStepRpt;
        }

        private V_TESTSTORYBOARD_SUMMARYDTO GetStoryBoardSummaryInfoByStoryBoardId(long lStoryBoardId, ref int iUnprocecced,string strDBIdx)
        {
            Logger.Info("GetStoryBoardSummaryInfoByStoryBoardId", string.Format("Try to get Storyboard Summary info by ID:[{0}]", lStoryBoardId));
            B_V_TESTSTORYBOARD_SUMMARY objSum = new B_V_TESTSTORYBOARD_SUMMARY();
            V_TESTSTORYBOARD_SUMMARYDTO objResult = objSum.getSummaryInfoByStoryBoardId(strDBIdx,lStoryBoardId);
            if (objResult == null) return null;
            // get partial information and change result 
            List<B_STORYBOARD_TEST_FULLVISION> lstStryBrdInfo = BoHelper.GetStoryboardRows(strDBIdx,lStoryBoardId);
            int iPartialCnt = lstStryBrdInfo == null ? (int)(objResult.CMP_PARTIAL_CNT ?? 0) : lstStryBrdInfo.Count(p => p.HIST_RESULT == 3);
            int iRightCnt = lstStryBrdInfo == null ? (int)(objResult.CMP_RIGHT_CNT ?? 0) : lstStryBrdInfo.Count(p => p.HIST_RESULT == 1);
            iUnprocecced = lstStryBrdInfo.Count - (int)(objResult.TCCNT ?? 0);
            int iFailCnt = lstStryBrdInfo.Count - iPartialCnt - iRightCnt - iUnprocecced;
            iFailCnt = iFailCnt < 0 ? 0 : iFailCnt;
            objResult.CMP_PARTIAL_CNT = iPartialCnt;
            objResult.CMP_FAIL_CNT = iFailCnt;
            objResult.CMP_RIGHT_CNT = iRightCnt;

            return objResult;
        }

        private void ShowMessage(string strMsg, string strTitle = "Message")
        {
            MessageBox.Show(strMsg, strTitle);
          
        }

        #endregion

    }
}
