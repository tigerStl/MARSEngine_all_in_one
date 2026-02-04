using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mars.DataLayer;
using System.Xml;
using MARS.CompareGUI;
using MarsTestFrame.SourceCode.systemUtil;
using Mars.MarsConfig;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using DomUtil;
using Mars.TestFramework.DataCompare;
using System.Data;
using Mars.TestFramework.DataCompare.DataCompareBatch;
using Mars.BasicData;
using MarsDataCompare.commandlineMode;
//using log4net.Repository.Hierarchy;
using Route2NSEx.src.Marquis.systemUtil;

namespace MarsDataCompare
{
   
    static class Program
    {
        static MarsConfig mc;
        static string MarsEnvironment = "DEV";

        static MLogger logger = null;// MLogger.GetLogger("Programe");
        /// <summary>
        /// The main entry point for the application.
        /// 2024 11 05 tiger added:
        ///     to be interatged by web application. Web application will start it like -Mode FromWebComp -CompareId "xxxxx" -DB GEN_MARS_5 -OutPutDir xxxxxx
        ///     outputDir is relative directory
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                MLogger.LOGGER_NAME = "DATACOMPARE";
                logger = MLogger.GetLogger(MLogger.LOGGER_NAME);
                EventLogWrite("Program START");

                bool isOk = false;
                string strError = "";
           
                if (args != null && args.Length != 0 && args[0].Trim().Length != 0)
                {
                    ///
                    MarsAppParameter para = MarsAppParameter.AnalystCommandLines(args, ref isOk, ref strError);
                    if ((isOk)&&(para!=null))
                    {
                        /// new mode                       
                        MarsConfig.isUsingCommand = true; // to tell system that the current mode is command line                        
                        string targetFileName = MarsCompareByCommandManagement.DoCompareBySettings(para, ref isOk, ref strError);
                        Console.Write($"Have finished compare|{para.appCompareId}|, return|{isOk}|{strError}");
                        if (isOk)
                        {
                            Environment.ExitCode = 0;
                        }
                        else Environment.ExitCode = 1;
                        return;
                    }
                    else
                    {
                        logger.Error("Main", strError);
                    }

            EventLogWrite("Program START");

            if (args != null && args.Length != 0 && args[0].Trim().Length != 0)
                MarsEnvironment = args[0].Trim();
                }
           
            Console.WriteLine("Program START");

            DataTable MappingTable;

                // For remote debugging

                /*
                Console.WriteLine("Waiting for debugger to attach");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine("Debugger attached");
                */
                EventLogWrite("MarsEnvironment: " + MarsEnvironment);
                try
                {
                    mc = MarsConfig.Configure(MarsEnvironment);
                }
                catch (Exception e)
                {
                    EventLogWrite("Exception in MarsConfig.Configure" + e);
                    EventLogWrite("Exception in MarsConfig.Configure" + e.StackTrace);
                    EventLogWrite("Exception in MarsConfig.Configure" + e.InnerException);
                }
              
                ExecuteCompare.mc = mc;
                string dataConnectionMode = mc.AppSettings.ContainsKey("DataConnectionMode")? mc.AppSettings["DataConnectionMode"]:"";
                // Make dataConnectionMode be alway WEB

                dataConnectionMode = "WEB";
                Console.WriteLine("dataConnectionMode:" + dataConnectionMode);
                EventLogWrite("dataConnectionMode:" + dataConnectionMode);
                DomHelper.mode = dataConnectionMode;

               string MappingTableFileName = null;
               if (mc.AppSettings.Keys.Contains("MappingTableFileName"))
                   MappingTableFileName = mc.AppSettings["MappingTableFileName"];
               if (MappingTableFileName != null)
               {
                   MappingTable = DataCompareBatchConfig.ImportExceltoDatatable(MappingTableFileName, "Sheet1");
                   ExecuteCompare.MappingTable = MappingTable;
                   DataCompareForm.MappingTable = MappingTable;
               }
               

                // Change config file to Mars.exe.config
                // AppConfig.Change("Mars.exe.config");


                // XmlDocument xmlDoc = null;
                //if (InitSchemaChangingAndDBConnection2() == false)

                EventLogWrite("Before InitSchemaChangingAndDBConnectionUsingMarsConfig");
                Console.WriteLine("Before InitSchemaChangingAndDBConnectionUsingMarsConfig");
            if (dataConnectionMode.Equals("DB"))
            {
                            
                if (InitSchemaChangingAndDBConnectionUsingMarsConfig() == false)
                {
                    EventLogWrite("InitSchemaChangingAndDBConnection returned false" );
                    Console.WriteLine("InitSchemaChangingAndDBConnection returned false");
                }
                else
                {
                    EventLogWrite("InitSchemaChangingAndDBConnection returned true");
                    Console.WriteLine("InitSchemaChangingAndDBConnection returned true");
                }
                   
            }

            EventLogWrite("After InitSchemaChangingAndDBConnectionUsingMarsConfig");
            Console.WriteLine("After InitSchemaChangingAndDBConnectionUsingMarsConfig");

                // Test if reading is possible
                XmlDocument xmlDoc;
                try
                {
                    xmlDoc = DomHelper.ReadXmlDoc();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in DomHelper" + e);
                    Console.WriteLine("Exception in DomHelper" + e.StackTrace);
                    Console.WriteLine("Exception in DomHelper" + e.InnerException);

                    EventLogWrite("Exception in DomHelper" + e);
                    EventLogWrite("Exception in DomHelper" + e.StackTrace);
                    EventLogWrite("Exception in DomHelper" + e.InnerException);
                }
                
                Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DataCompareForm());
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Main" + e);
                Console.WriteLine("Exception in Main" + e.StackTrace);
                Console.WriteLine("Exception in Main" + e.InnerException);
                logger.Error("Main", e.Message, e);
                EventLogWrite("Exception in Main" + e);
                EventLogWrite("Exception in Main" + e.StackTrace);
                EventLogWrite("Exception in Main" + e.InnerException);

            }
        }

        public static void EventLogWrite(string msg)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(msg, EventLogEntryType.Information, 101, 1);
            }
        }

        private static bool InitSchemaChangingAndDBConnectionUsingMarsConfig()
        {
           
            try
            {
                DatabaseConnectionDetails det = mc.GetDatabaseConnectionDetails();
                MarsEntitiesExtends.NewSchemaName = det.Schema;
                string strConnString = det.EntityConnString;

                if (string.IsNullOrEmpty(strConnString)) return false;

                EventLogWrite("strConnString: " + strConnString);

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
    }
}
