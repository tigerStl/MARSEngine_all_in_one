using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using XmlCompareLib;
using Mars.Dto;
using Mars.DataLayer;
using Mars.Business;
//using MarsTestFrame.SourceCode.systemUtil;
using System.Configuration;
using System.Diagnostics;
using Mars.autoTest.report.Word;
using Mars.MarsConfig;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using MarsTestFrame.SourceCode.systemUtil;

namespace MarsReport
{
    class Program
    {
        static MarsConfig mc = null;

        static void Main(string[] args)
        {
            Console.WriteLine("\n\nExcecuting command: " + Environment.CommandLine);
            Console.WriteLine("Started at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

           

            try
            {
                GenerateReport(args);
            }

            catch (Exception e)
            {
                string strError = $"Exception:\r\n[{e.Message}]";
                Console.Out.WriteLine(string.Format("Exception:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                // MessageBox.Show(strError, "Message", MessageBoxButtons.OK,MessageBoxIcon.Error);
                if (e.InnerException != null)
                    Console.Out.WriteLine(string.Format("InnerException:[{0}]\r\nStackTrace:[{1}]", e.InnerException.Message, e.StackTrace), e);
                return;
            }
            Console.WriteLine("Ended at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        static void GenerateReport(string[] args)
        {
            bool rc = false;
            var runTimeParameters = Environment.GetCommandLineArgs();
            
            CommdLineOptions options = new CommdLineOptions();
            options.init(runTimeParameters, 1);


            string projectName = options.GetOptionStringValue("-P");
            string storyboardName = options.GetOptionStringValue("-S");
            string OutputPath = options.GetOptionStringValue("-O");
            string OutputType = options.GetOptionStringValue("-T");
            string MarsEnvironment = options.GetOptionStringValue("-E");

            string ReportSet = options.GetOptionStringValue("-SET");
            string ReportGroup = options.GetOptionStringValue("-GROUP");
            string ReportFolder = options.GetOptionStringValue("-F");
            string ReportType = options.GetOptionStringValue("-R");
            string ReportDataTemplate = options.GetOptionStringValue("-DATA_TEMPLATE");
            string ReportMode = options.GetOptionStringValue("-M");

            bool helpMode = options.GetOptionBooleanValue("-h");
            bool debugMode = options.GetOptionBooleanValue("-d");

            mc = MarsConfig.Configure(MarsEnvironment);

            if (helpMode == false)
            {
                Console.WriteLine("Project             : " + projectName);
                Console.WriteLine("Storyboard          : " + storyboardName);
                Console.WriteLine("Output Path         : " + OutputPath);
                Console.WriteLine("Output Type         : " + OutputType);

                Console.WriteLine("Report Set          : " + ReportSet);
                Console.WriteLine("Report Group        : " + ReportGroup);
                Console.WriteLine("Report Folder       : " + ReportFolder);
                Console.WriteLine("Report Type         : " + ReportType);
                Console.WriteLine("RepDataTemplate     : " + ReportDataTemplate);
                Console.WriteLine("ReportMode          : " + ReportMode); // BASE COMP BOTH
            }


            if (ReportType.Equals("FLEX"))
            {
                /*
                if ((ReportGroup.Length == 0 &&  ReportSet.Length == 0) ||
                    ReportDataTemplate.Length == 0)
                    helpMode = true;
                */
                if (ReportFolder.Length == 0)
                {
                    Console.WriteLine("WARNING: Folder parameter (-F) is a required value!");
                    helpMode = true;
                }
            }

            else
            {
                if (projectName == null || storyboardName == null || OutputPath == null || OutputType == null ||
                   projectName.Trim().Length == 0 ||
                   storyboardName.Length == 0 ||
                   OutputPath.Length == 0 ||
                   OutputType.Length == 0)
                   helpMode = true;
            }

           

            if (helpMode)
            {
                DisplayHelp();
                return;
            }

            // AppConfig.Change("Mars.exe.config");
                
            if (InitSchemaChangingAndDBConnectionUsingMarsConfig() == false)
            {
                Console.WriteLine("** Error connecting to MARS database");
                return;
            }
            else
                Console.WriteLine("Connected to MARS database");

            if (debugMode)
            { 
                string[] allProjectNames = BoHelper.GetAllProjectNames(MarsConfig.currentDBInfo.DBIdx);
                foreach (string proj in allProjectNames)
                {
                    long thisProjId = BoHelper.GetProjectIdByName(proj, MarsConfig.currentDBInfo.DBIdx);
                    Console.WriteLine("[" + proj + "] -- " + thisProjId);
                }
                Console.WriteLine();
                Console.WriteLine("DEBUG: Program is terminating because it is running in debug mode ('-d')");
                System.Environment.Exit(-33);
            }

            if (ReportType.Equals("FLEX"))
            {
                //TestGlobalVar();
                FlexReportDriver flexReportDriver = new FlexReportDriver(ReportGroup, ReportSet, ReportFolder, OutputType.ToUpper(), OutputPath, ReportDataTemplate, ReportMode, mc);
               
                flexReportDriver.GenerateReport();
            }
            else
            {
                /*
                int iUnprocecced = 0;
                //int storyboardId = 104850;
                long projectId = BoHelper.GetProjectIdByName(projectName);
                if (projectId == 0)
                {
                    Console.WriteLine("\n** Error: Project [" + projectName + "] not found\n");
                    return;
                }

                long storyboardId = BoHelper.GetStoryboardByName(storyboardName, projectId);

                if (storyboardId == 0)
                {
                    Console.WriteLine("\n** Error: Storyboard [" + storyboardName + "] not found\n");
                    return;
                }

                V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(storyboardId, ref iUnprocecced);
                
                driver.GenWordReportForStoryBoardId(objStoryBrdSumInfo, OutputType.ToUpper(), OutputPath);
                */
                // TestGlobalVar();
                ReportDriver driver = new ReportDriver();
                driver.mc = mc;
                driver.GenerateReportByProjectAndStoryboardName(projectName, storyboardName, OutputType.ToUpper(), OutputPath, MarsConfig.currentDBInfo.DBIdx);
            }
        }

        private static void TestGlobalVar()
        {
            
            bool status = false;

            GetGlobalDataFromDB("TODAY", ref  status);

        }

        private static string GetGlobalDataFromDB(string strData, ref bool isOk)
        {
            string strError = "";
            string strResult = "";
            isOk = BoHelper.GetGlobalVariableInfo(strData, ref strError, ref strResult);
            if (!isOk)
            {
                
            }
            return strResult;
        }

        private static void DisplayHelp()
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("+------------------------------------------------------------------------------+");
            Console.Out.WriteLine("|                          MARS TEST EXECUTION REPORT                          |");
            Console.Out.WriteLine("| Flag     ARGUMENTS    Purpose                                                |");
            Console.Out.WriteLine("|==============================================================================|");
            Console.Out.WriteLine("|                                                                              |");
            Console.Out.WriteLine("| -H                   Display This Screen                                     |");
            Console.Out.WriteLine("| -P       Project     MARS Project                                            |");
            Console.Out.WriteLine("| -S       Storyboard  MARS Storyboard                                         |");
            Console.Out.WriteLine("| -GROUP   Report Group                                                        |");
            Console.Out.WriteLine("| -SET     Report Set                                                          |");
            Console.Out.WriteLine("| -F       Report Folder                                                       |");
            Console.Out.WriteLine("| -R       Report Type: STANDARD or FLEX                                       |");
            Console.Out.WriteLine("| -M       Report Mode: BASE, COMP or BOTH.  Default:BOTH                      |");
            Console.Out.WriteLine("| -DATA_TEMPLATE       Report Data Template                                    |");
            Console.Out.WriteLine("| -T       Output Type Report output type: WORD or EXCEL                       |");
            Console.Out.WriteLine("| -E       Environment Mars Environment: DEV, UAT, etc                         |");
            Console.Out.WriteLine("| -O       Path        Output path                                             |");
            Console.Out.WriteLine("| Note: If Report Type is STANDARD, Project and Storyboard must be specified   |");
            Console.Out.WriteLine("|       If Report Type is FLEX, ReportSet, Report Group must    be specified   |");
            Console.Out.WriteLine("|==============================================================================|");
            Console.Out.WriteLine("| Example:                                                                     |");
            Console.Out.WriteLine("| MarsReport -E DEV -P \"Project 1\" -S \"StoryBoard 1\" -T excel -O c:\\temp      |");
            Console.Out.WriteLine("+------------------------------------------------------------------------------+");
            Console.Out.WriteLine("  Build time: " + GetLinkerTime());
            Console.Out.WriteLine("");

        }

        public static DateTime GetLinkerTime(TimeZoneInfo target = null)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        static  V_TESTSTORYBOARD_SUMMARYDTO GetStoryBoardSummaryInfoByStoryBoardId(long lStoryBoardId, ref int iUnprocecced, string strDBIdx)
        {
            //Logger.Info("GetStoryBoardSummaryInfoByStoryBoardId", string.Format("Try to get Storyboard Summary info by ID:[{0}]", lStoryBoardId));
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


        private static bool InitSchemaChangingAndDBConnection2()
        {

            var currentExeCfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            try
            {
                MarsEntitiesExtends.NewSchemaName = AppConfigReader.GetDefaultSchemaForOracle();

                string strPassword = currentExeCfg.AppSettings.Settings["MARS_DB_PWD"].Value;
                strPassword = Mars.Securities.MarsEncodePwd.DecodeString(strPassword);
                string strConnString = currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();

                if (string.IsNullOrEmpty(strConnString)) return false;

                strConnString = string.Format(strConnString, strPassword);
                MarsEntitiesExtends.connectionBuilder = new System.Data.EntityClient.EntityConnectionStringBuilder(strConnString);
                return true;
            }
            catch (Exception e)
            {
                //Logger.Error("InitSchemaChangingAndDBConnection", string.Format("exception:[{0}]", e.Message));
                Console.WriteLine("InitSchemaChangingAndDBConnection: " + e);
                return false;
            }
        }

        private static bool InitDBCachedConnections(DatabaseConnectionDetails det,string dbCnnStr)
        {
            Console.WriteLine($"db connection init:{det.DBIdx} - {dbCnnStr}");

            if (det == null) return false;
            if (string.IsNullOrEmpty(det.DBIdx))
                throw new Exception("Please add DBIdx for DB conenction in MarsConfig");

            if (MarsEntitiesExtends.CachedConnectionStrings == null)
                MarsEntitiesExtends.CachedConnectionStrings = new Dictionary<string, MarsDBCnnectionInfo>();
            //MarsEntitiesExtends.CachedConnectionStrings.Clear();
            
            MarsDBCnnectionInfo currentDBCnnInfo = null;
            if (!MarsEntitiesExtends.CachedConnectionStrings.ContainsKey(det.DBIdx))
                MarsEntitiesExtends.CachedConnectionStrings.Add(det.DBIdx, new MarsDBCnnectionInfo());
            currentDBCnnInfo = MarsEntitiesExtends.CachedConnectionStrings[det.DBIdx];
            currentDBCnnInfo.decodedPwd = det.Password;
            currentDBCnnInfo.dbType     = det.Type;
            currentDBCnnInfo.encodedPwd = det.Password;
            currentDBCnnInfo.hostName   = det.Host;
            currentDBCnnInfo.newSchema  = det.Schema;
            currentDBCnnInfo.port       = det.Port;
            currentDBCnnInfo.userName   = det.Login;
            currentDBCnnInfo.connectionStringFromCfg = dbCnnStr;
            
            currentDBCnnInfo.createEntityConnectionStringBuilder();
            return true;
        }

        private static bool InitSchemaChangingAndDBConnectionUsingMarsConfig()
        {

            try
            {

                DatabaseConnectionDetails det = mc.GetDatabaseConnectionDetails();                

                MarsEntitiesExtends.NewSchemaName = det.Schema;
                string strConnString = det.EntityConnString;
                if (!InitDBCachedConnections(det,strConnString))
                {
                    throw new Exception("Can't Create DB connections");
                }

                if (string.IsNullOrEmpty(strConnString)) return false;

                MarsEntitiesExtends.connectionBuilder = new System.Data.EntityClient.EntityConnectionStringBuilder(strConnString);

                /*
                MarsEntitiesExtends.NewSchemaName = AppConfigReader.GetDefaultSchemaForOracle();
                
                string strPassword = currentExeCfg.AppSettings.Settings["MARS_DB_PWD"].Value;
                strPassword = Mars.Securities.MarsEncodePwd.DecodeString(strPassword);
                string strConnString = currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();

                if (string.IsNullOrEmpty(strConnString)) return false;

                strConnString = string.Format(strConnString, strPassword);
                MarsEntitiesExtends.connectionBuilder = new System.Data.EntityClient.EntityConnectionStringBuilder(strConnString);
                */

                return true;
            }
            catch (Exception e)
            {
                //Logger.Error("InitSchemaChangingAndDBConnection", string.Format("exception:[{0}]", e.Message));
                Console.WriteLine("InitSchemaChangingAndDBConnection: " + e);
                return false;
            }
        }

    }
}
