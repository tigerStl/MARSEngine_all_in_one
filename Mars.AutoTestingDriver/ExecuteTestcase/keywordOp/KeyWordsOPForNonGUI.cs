extern alias clientWCF;
//extern alias inject2_64;
//extern alias inject4_64;

using System;
using System.Collections.Generic;
using System.Linq;
using Mars.message.Business;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;

using System.Diagnostics;
using Mars.AutoTestingDriver.SystemUtil;

//using mj4=inject4_64::ManagedInjector;
using mj4 = ManagedInjector;
//using mj2=inject2_64::ManagedInjector;

using System.Threading;
using System.Runtime.InteropServices;
using Mars.message.AutoTestingDriver.interProcess;
#if _EngineDriver
using MarsEnginer.windowsWrapper.SystemUtil;
#else
using Mars.windowsWrapper.SystemUtil;
#endif
using System.Reflection;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using com.Mars.Constants;
using Mars.AutoTestingDriver.ExecuteStoryboard;
using Mars.AutoTestingDriver.webSupport;
using Mars.AutoTestingDriver.referenceSources.configuration;
using Mars.message.DataLayer;
#if _forx86
using Mars.AutoTestingDriver.any.Properties;
#else
using Mars.AutoTestingDriver.Properties;
using System.Data.SqlClient;

using System.Text.RegularExpressions;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.AutoTestingDriver.mars.javasupport;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.para;
using Mars.message.DataLayer.multipleDBSupport;
using System.IO;
using Mars.message.DatasourceMgr;
using System.Data;
using Mars.AutoTestingDriver.ExecuteTestcase.symbolTable;
using Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp;
using static com.Mars.Constants.Mars_applicationTyp;
using Mars.AutoTestingDriver.webSupport.TestDialog;
#endif

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp
{

    public class MarsTestEnv
    {
        public string EnvName;
        public object Data;
    }

    public class StatusVariablePara
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StatusVariablePara));
        public string source;
        public string to;
        public string varType;
        public string AliasOfVar;

        //To:Loop:VariableNameInMemory
        public const string cnst_formatOfSource = "(StatusVar|Loop){1}:.*";

        private StatusVariablePara(string strSource)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(cnst_formatOfSource);
            var m = r.Match(strSource);
            //if (!MarsWindowsAPIsExtend.RegularTest(cnst_formatOfSource, strSource))
            if (!m.Success)
                throw new Exception($"Wrong format of the parameter, it requires [{cnst_formatOfSource }], but it is [{strSource}]");
            int ipos = m.Value.IndexOf(":", 3);
            if (ipos < 0)
            {
                throw new Exception($"Wrong format of the parameter, it requires [{cnst_formatOfSource }], but it is [{strSource}]");
            }
            varType = m.Value.Substring(0, ipos);
            this.AliasOfVar = m.Value.Substring(ipos+1);            
        }

        public static StatusVariablePara GetVariableParaInst(string strSource)
        {
            Logger.logBegin("GetVariableParaInst");
            try
            {
                return new StatusVariablePara(strSource);
            }catch(Exception e)
            {
                Logger.Error("GetVariableParaInst", e.Message, e.StackTrace);
                return null;
            }
            finally
            {
                Logger.logEnd("GetVariableParaInst");
            }
        }
    }

    public sealed class MarsTestENVVarMgr
    {
        public static List<MarsTestEnv> MarsVarList = new List<MarsTestEnv>();
        public static bool CheckValue(string strNameIdx, object oValue)
        {
            var v = (from q in MarsVarList
                     where string.Compare(strNameIdx, q.EnvName, true) == 0
                     select q
                    ).FirstOrDefault();
            if (v == null)
                return false;
            if (v.Data == null)
            {
                if (oValue != null) return false;
                return true;
            }

            if (oValue == null)
                return false;
            Type t = v.GetType();
            if (!(oValue.GetType().IsSubclassOf(v.GetType())) || (oValue.GetType() == v.GetType()))
            {
                return false;
            }
            if (oValue is string)
            {
                //regular expression is enabled
                return (string.Compare((string)oValue, v.ToString(), true) == 0) || MarsWindowsAPIsExtend.RegularTest(strNameIdx, (string)oValue);
            }

            return oValue == v;
        }

        public static void AddEnvVarAndItsValue(string strNameIdx, object ov)
        {
            var x = MarsVarList.Where(p => string.Compare(p.EnvName, strNameIdx, true) == 0)
                .FirstOrDefault();
            if (x == null)
                MarsVarList.Add(new MarsTestEnv()
                {
                    EnvName = strNameIdx,
                    Data = ov
                });
            else
            {
                x.Data = ov;
            }
        }
    }

    public class InjectorHost
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(InjectorHost));
        internal static string InjectorAttached = "64-2.0";
        private static object CurrentInjector = null;
        public static string GetInjectorFile()
        {
            return string.Format("ManagedInjector{0}.dll", InjectorAttached);
        }

        public static object GetInjector(ref string strError)
        {
            if (CurrentInjector != null) return CurrentInjector;
            string strPath = typeof(InjectorHost).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            strPath = System.IO.Path.Combine(strPath, GetInjectorFile());
            if (!System.IO.File.Exists(strPath))
            {
                Logger.Error("GetInjector", strError = string.Format("No such file [{0}]", strPath));
                return null;
            }
            try
            {
                Assembly assemblyInjector = Assembly.LoadFile(strPath);
                return CurrentInjector = assemblyInjector.GetType("ManagedInjector.Injector");
            }
            catch (Exception e)
            {
                CurrentInjector = null;
                Logger.Error("GetInjector", strError = e.Message, e);
                return CurrentInjector = null;
            }

        }

        private static string GetInjectorCurrentHostFileName()
        {
            return string.Format("InjectorHost{0}.exe", InjectorAttached);
        }

        internal static bool RunInjectToDotNet2(string strProcessNameWithoutExtension, ref string strError)
        {
            string strPath = typeof(InjectorHost).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            strPath = System.IO.Path.Combine(strPath, GetInjectorCurrentHostFileName());
            if (!System.IO.File.Exists(strPath))
            {
                Logger.Error("GetInjector", strError = string.Format(Resources.mars_no_such_exefile, strPath));
                return false;
            }

            Process marsInjector = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = strPath,
                    Arguments = strProcessNameWithoutExtension
                }
            };
            marsInjector.Start();
            marsInjector.WaitForExit();
            if (marsInjector.ExitCode == -2)
            {
                strError = string.Format(Resources.mars_wrong_parameter_for_injectexe_code_2, strPath);//"no argument or application identifier is empty";
                return false;
            }
            if (marsInjector.ExitCode == -1)
            {
                strError = string.Format(Resources.mars_wrong_parameter_for_injectexe_code_1, strProcessNameWithoutExtension);
                return false;
            }
            if (marsInjector.ExitCode == -3)
            {
                strError = string.Format(Resources.mars_engine_cant_access_target_app_code_3, strProcessNameWithoutExtension);//"Exception when injector to target application";
                return false;
            }
            if (marsInjector.ExitCode == -4)
            {
                strError = string.Format(Resources.mars_engine_cant_access_target_app_code_4, strProcessNameWithoutExtension);//"Exception when injector to target application";strError = string.Format("Can't injector to that application:[{0}]", strProcessNameWithoutExtension);
                return false;
            }
            return true;
        }

        public static bool startMarsObjectTool32(int targetPid,ref string strError)
        {
            int iMark = new Random().Next();
            Logger.Info($"{iMark}|begin");
            string app32ToolPath = "";
            try
            {
                app32ToolPath = typeof(InjectorHost).Assembly.Location;
                app32ToolPath = System.IO.Path.GetDirectoryName(app32ToolPath);
                app32ToolPath = System.IO.Path.Combine(app32ToolPath, "MARSToolHostAgent32.exe");
                Console.WriteLine($"going to start {app32ToolPath} startInject {targetPid}");
                Logger.Info($"going to start {app32ToolPath} startInject {targetPid}");
                string cmd = $"\"{app32ToolPath} startInject {targetPid}\"";
                var p = Process.Start(app32ToolPath, $" startInject {targetPid}");
                //var p = Process.Start("cmd", cmd);
                string pName = p.ProcessName;
                p.WaitForExit();
                int pCde = p.ExitCode;
                Console.WriteLine($"MARSToolHostAgent32 returns|{pCde}|");
                Logger.Info($"MARSToolHostAgent32 returns|{pName}|{pCde}|");
                switch (p.ExitCode)
                {
                    case -2:
                        strError =  Resources.mars_obj_tool_wrong_para_code_1;//;"parameter is wrong";
                        return false;
                    case -1:
                        strError = Resources.mars_obj_tool_target_id_is_wrong_code_2;/*$"{targetPid} is not taken as id for MARSToolHostAgent32";*/
                        return false;
                    case -3:
                        strError = Resources.mars_cannot_access_target_process_3; // $"Can't open |{targetPid}|";
                        return false;
                    case -4:
                        strError = Resources.mars_cannot_find_process_mainhandle_code_4;//$"Can't find process |{targetPid}|'s main handle";
                        return false;
                    case 0:
                        strError = Resources.mars_unknow_command_code_0;// "unknow command";
                        return false;
                    default:
                        if (p.ExitCode > 0)
                            return true;
                        else
                            strError = Resources.mars_undefined_error;// "undefined error";
                        return false;
                }
                
                
            }
            catch (Exception e)
            {
                Logger.Error("startMarsObjectTool32",strError = $"{iMark}|Exception|{e.Message}|\r\n{e.StackTrace}");
                Console.WriteLine($"Can't start {app32ToolPath} with exception|{e.Message}|\r\n{e.StackTrace}");
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("Please press [enter] to quit");
                Console.ReadLine();
                return false;
            }
            finally
            {
                Logger.Info($"{iMark}|end");
            }
        }

    }

    public sealed class KeyWordsOPForNonGUI
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordsOPForNonGUI));

        public static string currentDBIdx = "";

        private static B_REGISTERED_APPS currentApplicationStartPath = null;
        public static B_REGISTERED_APPS CurrentApplicationStartPath
        {
            get { return currentApplicationStartPath; }
            set { currentApplicationStartPath = value; }
        }
        public static Dictionary<string, MarsKeywordOperation> Non_GUIKeyword = new Dictionary<string, MarsKeywordOperation>()
        {
            { SystemConstant.CNST_RESERVED_KEYWORD_ACTIVEPROCESS, MARSKEYWORD_ActiveProcess },
            { SystemConstant.CNST_RESERVED_KEYWORD_ASSERTVALUE, MARSKEYWORD_AssertValue},
            { "CHANGESIZE"      , MARSKEYWORD_ChangeSize        },
            { SystemConstant.CNST_RESERVED_KEYWORD_CLOSEPROCESS, MARSKEYWORD_CloseProcess},
            { "COMMENT"         , MARSKEYWORD_Comment           },
            {"COMPARETEXTFILES" , null                          },
            {"DBCOMPARE"        , MARSKEYWORD_DbCompare         },
            {"ELSE"             , MARSKEYWORD_ELSE              },
            {"ENDLOOP"          , MARSKEYWORD_EndLoop           },
            {SystemConstant.CNST_RESERVED_KEYWORD_ENDSUBLOOP, MARSKEYWORD_EndSubLoop },
            {"EXECUTECOMMAND"   , null                          },
            {"IF"               , MARSKEYWORD_IF                },
            {"IFEND"            , MARSKEYWORD_IFEND             },            
            { "KILLAPPLICATION" , MARSKEYWORD_KillApplication   },
            {"LOADVARIABLES"    , MARSKEYWORD_LoadVariables     }, 
            {"LOOP"             , MARSKEYWORD_Loop              },
            {SystemConstant.CNST_RESERVED_KEYWORD_OPENEXTERNALFILE, MARSKEYWORD_OpenExternalFile },
            {"QUERYDATAFROMDATASOURCE", MARSKEYWORD_QueryDataFromDataSource }, 
            {"REMOVEVARIABLE"   , MARSKEYWORD_RemoveVariable    },
            {"RESUMENEXT"       , MARSKEYWORD_ResumeNext        },
            { "STARTAPPLICATION", MARSKEYWORD_StartApplication  },
            {SystemConstant.CNST_RESERVED_KEYWORD_SUBLOOP   , MARSKEYWORD_SubLoop    },
            {"WAITFORSECONDS"   , MARSKEYWORD_WaitForSeconds    },
            {"WEBTESTDIALOG"    , MARSKEYWORD_WebTestDialog     },

        };


        private static bool MARSKEYWORD_ResumeNext(long lStepId, string strParaMeter, string strData, string strApiRunTimeConfig, B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo, ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            //do nothging just return true ;
            Logger.logBegin("ARSKEYWORD_ResumeNext", "Nothing is required but return true");
            Logger.logEnd("ARSKEYWORD_ResumeNext");
            return true;
        }


        /// <summary>
        /// for jump.
        /// 2018-12-21, tiger
        ///   support environment variable check
        ///   格式： if(null, value, ENV_VAR:abc)
        ///   或者： if(null, value, Modal_var:abc)
        /// </summary>
        /// <param name="stepid"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// 
        internal const string CNST_ENV_VAR = "ENV_VAR";
        internal const string CNST_IF_PARA_MARS_HAS_ERR = "MARS_HAS_EXCEPTION";

        private static bool MARSKEYWORD_IF(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_IF", string.Format("Parameter:[{0}]-Data [{1}]", strParaMeter, strData));
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.AskTime = DateTime.Now;


            if (MarsWindowsAPIsExtend.RegularTest(CNST_IF_PARA_MARS_HAS_ERR, strParaMeter))
            {
                strData = string.IsNullOrEmpty(strData) ? "T" : strData;
                if ((string.Compare(strData, "True", true) == 0) || (string.Compare(strData, "T", true) == 0))
                { //需要判断存在excpetion 或者error
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;

                    if (MarsGlobalStatusMgr.resumeNextStatus.hasExceptionsPrevious)
                    {
                        dealResult.ErrorMessage = strError = Resources.mars_if_keyword_check_excepttion_T_T;//$"if keyword wants to check [{CNST_IF_PARA_MARS_HAS_ERR}] with [true], and memory variable is [true]";
                        dealResult.ResultMessage = "OK";
                        return true;
                    }
                    else
                    {
                        dealResult.ErrorMessage = strError = Resources.mars_if_keyword_check_exception_T_F;// $"if keyword wants to check [{CNST_IF_PARA_MARS_HAS_ERR}] with [true], and memory variable is [false]"; ;
                        dealResult.ResultMessage = "FAILED";
                        return true;
                    }
                }
                else // 需要判断 没有 excpetion
                {
                    if (MarsGlobalStatusMgr.resumeNextStatus.hasExceptionsPrevious)
                    {
                        dealResult.ErrorMessage = strError = Resources.mars_if_keyword_check_exception_F_T;// $"if keyword wants to check [{CNST_IF_PARA_MARS_HAS_ERR}] with [false], and memory variable is [true]"; ;
                        dealResult.ResultMessage = "FAILED";
                        return false;
                    }
                    else
                    {
                        dealResult.ErrorMessage = strError = Resources.mars_if_keyword_check_exception_F_F;// $"if keyword wants to check [{CNST_IF_PARA_MARS_HAS_ERR}] with [fase], and memory variable is [false]"; ;
                        dealResult.ResultMessage = "OK";
                        return true;
                    }
                }
                //return true;

            }

            if (MarsWindowsAPIsExtend.RegularTest("^" + CNST_ENV_VAR + ":", strData))
            {
                string strEnvVar = strData.Substring(CNST_ENV_VAR.Length + 1);
                strEnvVar = string.IsNullOrEmpty(strEnvVar) ? "" : strEnvVar;
                bool ischecked = MarsTestENVVarMgr.CheckValue(strEnvVar, strParaMeter);
                if (!ischecked)
                {
                    dealResult.ErrorMessage = strError = string.Format(Resources.mars_if_keyword_para_no_such_env_var_paired_value,//"no such environment variable is matched:[{0}]-[{1}]"
                         strEnvVar, strData);
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                else
                {
                    dealResult.ErrorMessage = strError = string.Format(Resources.mars_if_keyword_para_env_var_paired_value,//"environment variable is matched:[{0}]-[{1}]",
                          strEnvVar, strData);
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = "OK";
                    return true;
                }
            }
            else
            {

                dealResult.ErrorMessage = strError = string.Format(Resources.mars_if_keyword_para_unsupport_env_var_typ, //"Unspported env var type or format:[{0}]", 
                    strParaMeter);
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = string.Compare(strParaMeter, strData) == 0 ? "OK" : "FAILED";
                return (string.Compare(strParaMeter, strData) == 0) || (MarsWindowsAPIsExtend.RegularTest(strParaMeter, strData));
            }
            //dealResult.ErrorMessage = strError = string.Format("Unspported env var type:[{0}]",strParaMeter);
            //dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
            //dealResult.AckTime = DateTime.Now;
            //dealResult.ResultMessage = "FAILED";
            //return false;
        }
        private static bool MARSKEYWORD_ELSE(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, 
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            //do nothing, 
            //just return true
            return true;
        }

        private static bool MARSKEYWORD_IFEND(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            //do nothing, 
            //just return true
            return true;
        }


        private delegate bool DoCompareData(string data, string RC, ref string strResultFileName, ref string strError);

        public static bool MARSKEYWORD_ChangeSize(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities",
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ChangeSize", string.Format("Parameter:[{0}]-Data [{1}]", strParaMeter, strData));
            Logger.logEnd("MARSKEYWORD_ChangeSize");
            return true;
        }
        public static bool MARSKEYWORD_DbCompare(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_DbCompare", string.Format("Parameter:[{0}]-Data [{1}]", strParaMeter, strData));

            if (string.IsNullOrEmpty(strData) || string.IsNullOrEmpty(strParaMeter))
            {
                strError = string.Format("Parameter and Data should not be empty. Parameter is for Comparison type");
                Logger.Error("\t", strError);
                return false;
            }

            string strResultFileName = "";
            string strDBCompareAssembliy = "Mars.TestFramework.DataCompare.dll";
            Assembly dbAssembly = null;
            //Assembly.lad
            if ((dbAssembly = AssemblyIsLoaded(strDBCompareAssembliy)) == null)
            {
                string strRoot = typeof(KeyWordsOPForNonGUI).Assembly.Location;
                strRoot = System.IO.Path.GetDirectoryName(strRoot);
                try
                {
                    dbAssembly = Assembly.LoadFile(System.IO.Path.Combine(strRoot, strDBCompareAssembliy));
                }
                catch (Exception e)
                {
                    Logger.Error("\t", (strError = Resources.mars_load_test_data_compare_error) +
                        $"with Error:[{e.Message}]",
                        e);
                    return false;
                }//AppDomain.CurrentDomain.Load()
            }
            Type dbCmpareTypeFromAssembly = dbAssembly.GetType("Mars.TestFramework.DataCompare.DataCompare");
            if (dbCmpareTypeFromAssembly == null)
            {
                Logger.Error("\t", strError = Resources.mars_load_test_dta_cmp_no_class);// "no such type exists [Mars.TestFramework.DataCompare.DataCompare]");
                ConsoleLog.IntimeLog(strError);
                return false;
            }
            //DoCompareData
            try
            {
                ConstructorInfo dbCompareConstructor = dbCmpareTypeFromAssembly.GetConstructor(new Type[] { });

                object dbCompareInst = dbCompareConstructor.Invoke(null); //Activator.CreateInstance(dbCmpareTypeFromAssembly, null);

                MethodInfo mDo = dbCmpareTypeFromAssembly.GetMethod("DoCompareData");
                DoCompareData doDelegate = (DoCompareData)Delegate.CreateDelegate(typeof(DoCompareData), dbCompareInst, mDo);
                bool isOk = doDelegate.Invoke(strData, strParaMeter, ref strResultFileName, ref strError);
                if (!isOk)
                {
                    ConsoleLog.IntimeLog("\t", strError);
                    return false;
                }
            }
            catch (Exception e)
            {
                strError = Resources.mars_dta_cmp_cannot_invoke;// "Can't call DBCompare";
                Logger.Error("\t", string.Format("Can't get instance for [{1}] and invoke method with Error:[{0}]",
                    e.Message, dbCmpareTypeFromAssembly.ToString()), e);
                return false;
            }

            return true;
        }

        public static Assembly AssemblyIsLoaded(string pathToAssembly)
        {
            foreach (Assembly currentAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try // This try/catch block is necessary because the .Location property is not supported in a dynamic assembly.
                {
                    if (currentAssembly.Location.ToUpper().EndsWith(pathToAssembly.ToUpper()))
                    //if (currentAssembly.Location.Equals(pathToAssembly,StringComparison.OrdinalIgnoreCase))
                    {
                        return currentAssembly;
                    }
                }
                catch (Exception ex) { }
            }

            return null;
        }


        private static bool MARSKEYWORD_WaitForSeconds(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, 
            ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_WaitForSeconds", string.Format("Parameter:[{0}]-Data [{1}]", strParaMeter, strData));
            int iSeconds;
            if (!int.TryParse(strData ?? "", out iSeconds))
            {
                // default sleep 3 seconds
                iSeconds = 3;
                ConsoleLog.IntimeLog("\tWarrning:WaitForSeconds, parameter is not number [{0}], default value 3 is applied", strParaMeter);
                Logger.Info("WaitForSeconds", "\tWarrning: parameter is not number [{0}], default value 3 is applied", strParaMeter);
            }
            if ((string.Compare("F", strParaMeter ?? "", true) == 0) || (currentApplicationStartPath == null))
            {
                Thread.Sleep(iSeconds * 1000);
                return true;
            }
            else
            {
                /// find process and wait for idle
                /// 
                Process[] arrP = Process.GetProcessesByName(currentApplicationStartPath.PROCESS_IDENTIFIER);
                if ((arrP == null) || (arrP.Length <= 0))
                {
                    /// no such application is runing
                    /// 
                    ConsoleLog.IntimeLog("\tWaitForSeconds\tNo such application is running [{0}]", currentApplicationStartPath.PROCESS_IDENTIFIER);
                    strError = string.Format("\tWaitForSeconds\tNo such application is running [{0}]", currentApplicationStartPath.PROCESS_IDENTIFIER);
                    Logger.Error("WaitForSeconds", strError);
                    return true;
                }
                HandleRef handleRef = new HandleRef(arrP[0], arrP[0].MainWindowHandle);
                int timeout = iSeconds < 0 ? 2000 * 60 : iSeconds * 1000; //three minutes for default
                IntPtr lpdwResult;

                IntPtr lResult = MarsWindowsAPIs.SendMessageTimeout(
                    //handleRef,
                    arrP[0].MainWindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    (uint)timeout,
                    out lpdwResult);
            }
            return true;
        }

        internal static bool RunKeywordByKeywordName(long stepId, string strKeyword,
            B_V_OBJECT_SNAPSHOT stepObject, string cOLUMN_ROW_SETTING, string dATA_VALUE, string strApiRuntimeConfig,
            Mars_applicationTyp.MARS_APPTYPE appTyp,
            ref string strError,
            ref MARSDealResult dealResult,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName,
            KeywordExecuteCallBack dataSetBackCallBack = null,
            bool isAttachUIAAHwnd = false)
        {
            string upperKeyword = strKeyword == null ? "" : strKeyword.ToUpper();
            if (!Non_GUIKeyword.ContainsKey(upperKeyword))
            {
                strError =  string.Format(Resources.mars_no_keyword, //"No such Keyword function for [{0}]", 
                    strKeyword);
                return false;
            }
            if (Non_GUIKeyword[upperKeyword] == null)
            {
                strError = string.Format(Resources.mars_unsupport_keyword, // "unsupported keyword:[{0}]", 
                    strKeyword);
                return false;
            }

            return Non_GUIKeyword[upperKeyword](stepId, cOLUMN_ROW_SETTING, dATA_VALUE, strApiRuntimeConfig, stepObject, "", 
                ref strError, 
                ref dealResult, 
                appTyp, 
                strDBIdx, 
                dataSetBackCallBack);
        }

        private static bool MARSKEYWORD_Comment(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, 
            ref string strError, 
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_Comment", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            Logger.logEnd("MARSKEYWORD_Comment");
            return true;
        }

        

        private static bool MARSKEYWORD_StartApplication(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, 
            string strAttachInfo, 
            ref string strError, 
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_StartApplication", string.Format("Parameter:[{0}] data:[{1}] Path:[{2}] Process:[{3}] application EXTRAREQUIREMENT:[{4}], application type:[{5}]",
                strParaMeter, strData,
                currentApplicationStartPath == null ? "n/a" : currentApplicationStartPath.STARTER_COMMAND,
                currentApplicationStartPath == null ? "n/a" : currentApplicationStartPath.PROCESS_IDENTIFIER,
                currentApplicationStartPath == null ? "n/a" : currentApplicationStartPath.EXTRAREQUIREMENT,
                appTyp
                ));

            IntPtr dialogHdl = IntPtr.Zero;
            bool isDialogMode = false;

            try
            {
                /// start application needs inject once it start
                /// 
                int iShortNameIdx = -1;

                MarsKeywordParameter startAppPara = new MarsKeywordParameter(strParaMeter);
                
                Console.WriteLine("\tbefore compare");
                if (string.Compare(MarsDriverConst.CURRENT_APPLICATION, strData, true) == 0)
                {
                    Console.WriteLine("after compare MarsDriverConst.CURRENT_APPLICATION");
                    Logger.Info("\t", "after compare MarsDriverConst.CURRENT_APPLICATION");
                    if (currentApplicationStartPath == null)
                    {
                        Logger.Error("StartApplication", strError = Resources.mars_keyword_start_application_no_app);// ("CURRENT_APPLICATION IS SET, but no value of the application."));
                        return false;
                    }
                    Console.WriteLine("after compare MarsDriverConst.row 489");
                    Logger.Info("\t", "after compare MarsDriverConst.row 489");
                    if (string.IsNullOrEmpty(currentApplicationStartPath.PROCESS_IDENTIFIER))
                    {
                        currentApplicationStartPath.PROCESS_IDENTIFIER = System.IO.Path.GetFileNameWithoutExtension(currentApplicationStartPath.STARTER_COMMAND);
                    }
                }
                else if ((!string.IsNullOrEmpty(strData)) &&
                    ((iShortNameIdx = strData.ToUpper().IndexOf(SystemConstant.CNST_RUNTIME_PARA_SHORTNAME)) >= 0))
                {
                    Logger.Info("\t", $"short name mode idxPos:{iShortNameIdx}");
                    string strTargetAppShortName = strData.Substring(iShortNameIdx + SystemConstant.CNST_RUNTIME_PARA_SHORTNAME.Length + 1);
                    Logger.Info("\t", $"short name mode application shortname:{strTargetAppShortName}");
                    bool isOk = false;
                    currentApplicationStartPath = B_REGISTERED_APPS.GetApplicationByShortName(
                        currentDBIdx,
                        strTargetAppShortName, ref isOk, ref strError);

                    if (currentApplicationStartPath == null)
                    {
                        Logger.Error("MARSKEYWORD_StartApplication", $"no such app :[{strTargetAppShortName}] with error:[{strError}]");
                        return false;
                    }

                    //修改当前的application_id，从而实现切换
                    stepObject.APPLICATION_ID = currentApplicationStartPath.APPLICATION_ID;

                    //get data from configure file
                    var tmpAppFromCfg = MarsDriverAppConfigMgr.CurrentApplications.GetSingle(strTargetAppShortName);
                    if ((tmpAppFromCfg == null) || (!isOk))
                    {
                        Logger.Error("MARSKEYWORD_StartApplication", strError = string.IsNullOrEmpty(strError) ? $"can't find app from config [{strTargetAppShortName}]" : strError);
                        strError = string.Format(Resources.mars_keyword_start_application_no_app_in_cfgfile, strTargetAppShortName);
                        return false;
                    }
                    if ((!string.IsNullOrEmpty(currentApplicationStartPath.PROCESS_IDENTIFIER))
                    && (currentApplicationStartPath.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_IE) >= 0))
                    {

                    }
                    else
                    {
                        currentApplicationStartPath.STARTER_COMMAND = tmpAppFromCfg.path;
                    }
                }
                else
                {
                    Logger.Info("\t", $"normal mode idxPos:{iShortNameIdx}");
                    ///normal mode change the current application info 
                    /// 
                    if (currentApplicationStartPath == null)
                        currentApplicationStartPath = new B_REGISTERED_APPS();
                    currentApplicationStartPath.STARTER_COMMAND = strData;
                    if (string.IsNullOrEmpty(currentApplicationStartPath.PROCESS_IDENTIFIER))
                        currentApplicationStartPath.PROCESS_IDENTIFIER = System.IO.Path.GetFileNameWithoutExtension(strData);
                }
                string strAdv = "",
                        strStack = "";
                currentApplicationStartPath.EXTRAREQUIREMENT = currentApplicationStartPath.EXTRAREQUIREMENT ?? "";
                if ((!string.IsNullOrEmpty(currentApplicationStartPath.PROCESS_IDENTIFIER))
                    && ((currentApplicationStartPath.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_IE) >= 0)
                    ||((currentApplicationStartPath.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_CHROME) >= 0))
                    ||((currentApplicationStartPath.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_EDGE) >= 0))
                    ||((currentApplicationStartPath.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_CHROME) >= 0))))
                {
                    //启动web模式
                    
                    return StartWebApplication(currentApplicationStartPath, strParaMeter, ref strError, ref strAdv, ref strStack);
                }
                if (appTyp == Mars_applicationTyp.MARS_APPTYPE.MARS_JAVA)
                {
                    return StartJAVAApplication(currentApplicationStartPath, strParaMeter, ref strError, ref strAdv, ref strStack);
                }
                

                Process[] arrPOld = null;
                try
                {
                    Logger.Info("MARSKEYWORD_StartApplication", $"before call GetProcessesByName [{currentApplicationStartPath.PROCESS_IDENTIFIER}]");
                    arrPOld = Process.GetProcessesByName(currentApplicationStartPath.PROCESS_IDENTIFIER);
                }
                catch (Exception e)
                {
                    strError = string.Format(Resources.mars_keyword_start_application_no_right_to_access_process, currentApplicationStartPath.PROCESS_IDENTIFIER);
                    Logger.Error("MARSKEYWORD_StartApplication", $"{strError}\r\n{e.Message}", e);
                    
                    return false;
                }
                
                /// start the application
                /// 
                Logger.Info("MARSKEYWORD_StartApplication", string.Format("going to start from path:[{0}]", currentApplicationStartPath.STARTER_COMMAND));
                dealResult.ActualInputData = currentApplicationStartPath.STARTER_COMMAND;
                ProcessStartInfo pstarter = new ProcessStartInfo(currentApplicationStartPath.STARTER_COMMAND);
                pstarter.WorkingDirectory = System.IO.Path.GetDirectoryName(currentApplicationStartPath.STARTER_COMMAND);
                Process p = Process.Start(pstarter);

                string strAppExTyp = currentApplicationStartPath.EXTRAREQUIREMENT;
                string strProcessName = currentApplicationStartPath.PROCESS_IDENTIFIER;
                try
                {
                    var outPut = startAppPara.GetSpecialParaExists(MarsKeywordParameter.cnst_para_idx_Output);
                    if (outPut != null)
                    {
                        MarsKeywordParaNestedItem nestedOutput = outPut as MarsKeywordParaNestedItem;
                        if (nestedOutput == null)
                        {
                            Logger.Error("MARSKEYWORD_StartApplication", strError = Resources.mars_keyword_start_application_checkOuput_setting);// "Please check Output settings of Startapplication keyword.");
                            return false;
                        }
                        foreach (var itmOutputItm in nestedOutput.nestedItems)
                        {
                            if (itmOutputItm.Equals(default(KeyValuePair<string, string>))) continue;
                            if (string.IsNullOrEmpty(itmOutputItm.Key)
                                ||(string.IsNullOrEmpty(itmOutputItm.Value))) continue;
                            if (string.Compare(MarsKeywordParameter.cnst_para_idx_PID, itmOutputItm.Key, true) == 0)
                            {
                                MarsMemoryVaiableTable.AddObject(itmOutputItm.Value, p.Id);
                            }
                        }
                        
                    }
                    MarsWindowsAPIsExtend.WaitForCurrentProcessResponse(30, p);
                    //p.WaitForInputIdle(30*1000);// max 30 seconds
                }
                catch (Exception e)
                {
                    Logger.Error("MARSKEYWORD_StartApplication", e.Message, e);
                    Console.WriteLine($"Exception when start [{currentApplicationStartPath.STARTER_COMMAND}] --[{e.Message}]");
                }
                long n = DateTime.Now.Ticks, pre = n;
                Console.Write("going to check and wait process's responding status.");

                MarsWindowsAPIsExtend.WaitForCurrentProcessResponse(10);
                
                Console.WriteLine($"\t\ttry to find process [{currentApplicationStartPath.PROCESS_IDENTIFIER}] for 20 times");
                var noWaitPara = startAppPara.GetSpecialParaExists(MarsKeywordParameter.cnst_para_idx_nowait);
                if (noWaitPara!=null)
                {
                    Logger.Info("MARSKEYWORD_StartApplication","use no wait parameter, just return true" );
                    
                    return true;
                }
                for (int i = 0; i < 20; i++)
                {
                    Console.Write($", [{i}]");
                    Process[] arrPNew = Process.GetProcessesByName(currentApplicationStartPath.PROCESS_IDENTIFIER);
                    //find new process 
                    if (arrPOld == null)
                    {
                        p = arrPNew == null ? null : (arrPNew.Length > 0 ? arrPNew[0] : null);
                    }
                    else
                    {
                        if ((arrPNew == null) || (arrPNew.Length <= 0))
                        {
                            if (i < 19)
                            {
                                System.Threading.Thread.Sleep(3000);
                                continue;
                            }
                            strError = string.Format(Resources.mars_keyword_start_application_cannt_find_process, 
                                currentApplicationStartPath.PROCESS_IDENTIFIER);
                            return false;
                        }
                        var pNew = arrPNew.Where(x => !arrPOld.Any(xo => xo.Id == x.Id)).FirstOrDefault();
                        if (pNew == null)
                        {
                            strError = string.Format(Resources.mars_keyword_start_application_cannt_find_new_process, 
                                currentApplicationStartPath.PROCESS_IDENTIFIER);
                            return false;
                        }
                        p = pNew;
                        break;
                    }
                }

                Console.WriteLine($"\n\t\tfind application process [{p}]");
                //可能是dialog模式
                
                if (ModalChecker.IsWaitingForUserInput(p, ref dialogHdl))
                {
                    isDialogMode = true;
                }
                else
                {
                    if (appTyp != MARS_APPTYPE.MARS_CORE_WPF)
                    {
                        //对于core wpf应用而言，这里不需要
                        //等待mainwindow is visible
                        if (!MarsWindowsAPIsExtend.IsProcessMainWindowLoaded(p, ref strError))
                        {
                            ConsoleLog.IntimeLog(strError);
                            Logger.Error("StartApplication", strError);
                            return false;
                        }
                    }
                    ConsoleLog.IntimeLog(strError);
                    ConsoleLog.IntimeLog_keywordSub("Wait for 2 seconds, to make sure that target application is available");
                   
                    /**
                     * 有可能是dialog在外，因此，MainwindowHandle为空或者0
                     * */
                    Thread.Sleep(1000);
                    IntPtr lpdwResult;
                    HandleRef handleRef = new HandleRef(p, p.MainWindowHandle);
                    if (!p.MainWindowHandle.Equals(IntPtr.Zero))
                    {
                        IntPtr lResult = MarsWindowsAPIs.SendMessageTimeout(
                                //handleRef,
                                p.MainWindowHandle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                3000,
                                out lpdwResult);
                    }
                }
                if ((!string.IsNullOrEmpty(strParaMeter)) && (string.Compare("ENGINE_DELAY", strParaMeter, true) == 0))
                {
                    ConsoleLog.IntimeLog("\tNo InjectMode ");
                    return true;
                }
                //going to inject
                try
                {
                    Console.WriteLine($"\tto check app type|{appTyp}");
                    //MarsWindowsAPIsExtend.ShowWindowInTaskbar(p.MainWindowHandle);
                    if (appTyp == MARS_APPTYPE.MARS_CORE_WPF)
                    {
                        Console.WriteLine($"\tCORE WPF MODE................");
                        Logger.Info("MARSKEYWORD_StartApplication", $"HOST APPLICATION TO |{strProcessName}");
                        return dotnetCore.MarsCoreAppInterfaceManagement.HostToTargetApplication("FROM MARSKEYWORD_StartApplication", strProcessName, ref strError, ref strAdv, ref strStack);
                    }
                    Console.WriteLine($"\tnon {appTyp},framework way");
                    //Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.dll"), "Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", "StartMonitorThread");
                    string strPathOfMars = System.IO.Path.GetDirectoryName(typeof(KeyWordsOPForNonGUI).Assembly.Location);
                    Logger.Info("MARSKEYWORD_StartApplication", "try to load injector " + System.IO.Path.Combine(strPathOfMars, "MarsInterMQCenter.dll"));
                    //Injector.Launch(p.MainWindowHandle, System.IO.Path.Combine(strPathOfMars, "MarsInterMQCenter.dll"), "Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", "StartMonitorThread");
                    if ((currentApplicationStartPath.EXTRAREQUIREMENT != null) && (string.Compare(currentApplicationStartPath.EXTRAREQUIREMENT,
                        B_REGISTERED_APPS.cnst_app_require_dotNet2, true) == 0))
                    {
                        if (!InjectorHost.RunInjectToDotNet2(currentApplicationStartPath.PROCESS_IDENTIFIER, ref strError))
                        {
                            ConsoleLog.IntimeLog("\tInjectorHost.RunInjectToDotNet2 Error:[{0}]", strError);
                            Logger.Error("MARSKEYWORD_StartApplication", strError = string.Format(Resources.mars_keyword_start_application_cannot_inter_op_target_process, currentApplicationStartPath.PROCESS_IDENTIFIER));
                            return false;
                        }
                    }
                    else
                    {
                        
                        if ((!string.IsNullOrEmpty(strAppExTyp)) &&
                            ((strAppExTyp.ToUpper().IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_COMMON_INJ) >= 0)
                            || (strAppExTyp.ToUpper().IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_REQUIRE_QT) >= 0))
                            )
                        {
                            bool isOk = StoryboardExecute.InjectorByLoadLibWay(strProcessName, ref strError);
                            ConsoleLog.IntimeLog("\tInjected [{1}] status:[{0}] by LoadLibway ", isOk ? "Ok" : "failed withe error:" + strError, strProcessName);
                            return isOk;
                        }
                        else
                        {
                            Logger.Info("MARSKEYWORD_StartApplication", $"before get the 64/32|{p.Id}|{p.ProcessName}");
                            bool is32Bit = MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.IsProcess32(p.Handle);
                            string strMQCenterDllName = is32Bit ? "MarsInterMQCenter.Any.dll" : "MarsInterMQCenter.dll";
                            Logger.Info("MARSKEYWORD_StartApplication", $"is32Bit|{is32Bit}|{strMQCenterDllName}");
                            if (is32Bit)
                            {
                                Logger.Info("MARSKEYWORD_StartApplication", $"begin to call startMarsObjectTool32|{p.ProcessName}|{p.Id}");
                                // just start the process and inject
                                var isOk = InjectorHost.startMarsObjectTool32(p.Id, ref strError);
                                if (!isOk)
                                {
                                    Logger.Error("MARSKEYWORD_StartApplication", $"startMarsObjectTool32 returns|{strError}");
                                    return false;
                                }
                                return true;
                            }

                            string tmpNameSpace = "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc";
                            if (strAppExTyp.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WINX86) >= 0)
                            {
                                if (strAppExTyp.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DLGSTART) >= 0)
                                {
                                    Logger.Info("MARSKEYWORD_StartApplication", $"win32 and dialog --DIALOG|{tmpNameSpace}|");
                                    mj4.Injector.Launch(isDialogMode ? dialogHdl:p.MainWindowHandle, 
                                        System.IO.Path.Combine(strPathOfMars, "MarsInterMQCenter.Any.dll"), 
                                        tmpNameSpace, //"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                        "StartMonitorThread", "Normal");
                                }
                                else
                                {
                                    Logger.Info("MARSKEYWORD_StartApplication", $"win32 and dialog -- win32|{tmpNameSpace}");
                                    mj4.Injector.Launch(isDialogMode ? dialogHdl : p.MainWindowHandle, System.IO.Path.Combine(strPathOfMars, "MarsInterMQCenter.Any.dll"),
                                        tmpNameSpace, //"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                        "StartMonitorThread", "Normal");
                                }
                            }
                            else
                            {

                                ConsoleLog.IntimeLog($"application is normal|{tmpNameSpace}");
                                
                                mj4.Injector.Launch( isDialogMode?dialogHdl:p.MainWindowHandle, 
                                    System.IO.Path.Combine(strPathOfMars,
                                    strMQCenterDllName //"MarsInterMQCenter.dll"
                                    ),
                                    tmpNameSpace, //"Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                    "StartMonitorThread", "Normal");
                            }
                        }
                    }

                    
                    Logger.Info("MARSKEYWORD_StartApplication", "Try to check Injection status");
                    if (!mj4.Injector.IsInjected(System.IO.Path.GetFileNameWithoutExtension(currentApplicationStartPath.PROCESS_IDENTIFIER)))
                    {
                        ConsoleLog.IntimeLog("\tStartApplication Error: can't Access application:[{0}] by Mars Agent", currentApplicationStartPath.PROCESS_IDENTIFIER);
                        Logger.Info("MARSKEYWORD_StartApplication", "\tStartApplication Error: can't Access application:[{0}] by Mars Agent", currentApplicationStartPath.PROCESS_IDENTIFIER);
                        strError = string.Format(Resources.mars_keyword_start_application_cannot_access_proc_by_agnt, //"\tStartApplication Error: can't Access application:[{0}] by Mars Agent", 
                            currentApplicationStartPath.PROCESS_IDENTIFIER);
                        return false;
                    }
                    else
                    {
                        ConsoleLog.IntimeLog("\tInjected");
                    }
                    return true;
                }
                catch (Exception e)
                {
                    
                    Logger.Error("MARSKEYWORD_StartApplication", string.Format("\tException:[{0}] \r\n\tStackTrace:{1}", e.Message, e.StackTrace), 
                        e);
                    strError = Resources.mars_keyword_start_application_noright_start_or_access_application;
                    ConsoleLog.IntimeLog(strError);
                    return false;
                }

            }catch(Exception e)
            {
                Logger.Error("MARSKEYWORD_StartApplication", e.Message, e);
                strError = Resources.mars_keyword_start_application_noright_start_or_access_application_1;
                ConsoleLog.IntimeLog(strError);
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_StartApplication");
            }
        }        
        /// <summary>
        /// start JAVA should be two ways,
        /// 1, exe, 
        /// 2, java ......
        /// start java means run a command and set
        /// </summary>
        /// <param name="appInfo"></param>
        /// <param name="strPara"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        private static bool StartJAVAApplication(B_REGISTERED_APPS appInfo, string strPara, ref string strError, ref string strAdv, ref string strStack)
        {
            Logger.logBegin("StartJAVAApplication", $"strPara:{strPara}");
            try
            {
                if (appInfo == null)
                {
                    strAdv = "Contact Marquis";
                    strError = Resources.mars_keyword_start_application_web_no_shortname_indb;// "No Application information from DB";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return false;
                }
                string ver = "";
                bool isJavaCritical = false;
                string strJavaProcessInfo = appInfo.EXTRAPOPUPMENU;
                if (!string.IsNullOrEmpty(appInfo.EXTRAREQUIREMENT))
                {
                    isJavaCritical = Mars.AutoTestingDriver.mars.javasupport.MarsJavaSupport.IsJavaVersionCritical(appInfo.EXTRAREQUIREMENT, ref ver);
                    int iVerRq, iVerC;
                    if (isJavaCritical)
                    {
                        if (!int.TryParse(ver, out iVerRq))
                        {
                            strError = Resources.mars_keyword_start_application_java_version_wrong;
                            strAdv   = strError;
                            strStack = MarsErrorStacks.StackTraceDump();
                            return false;
                        }
                        bool isInstalled = Mars.AutoTestingDriver.mars.javasupport.MarsJavaSupport.IsJavaSupport(ref ver);
                        if (!isInstalled)
                        {
                            strError = Resources.mars_keyword_start_application_no_java;
                            strAdv   = strError;
                            strStack = MarsErrorStacks.StackTraceDump();
                            return false;
                        }
                        if (!int.TryParse(ver, out iVerC))
                        {
                            strError = Resources.mars_keyword_start_application_use_regular_java;
                            strAdv   = strError;
                            strStack = MarsErrorStacks.StackTraceDump();
                            return false;
                        }
                        if (iVerC < iVerRq)
                        {
                            strError = string.Format(Resources.mars_keyword_start_application_java_not_match_version, iVerC, iVerRq) ;
                            strAdv   = strError;
                            strStack = MarsErrorStacks.StackTraceDump();
                            return false;
                        }                                              
                    }
                }
                /// then start java app
                /// 
                 Process targetProces = null;
                string strCmmdAttach = "";
                try
                {
                    targetProces = Process.Start(appInfo.STARTER_COMMAND);
                    targetProces.WaitForInputIdle(1000 * 60);//1 min
                    if (!string.IsNullOrEmpty(strJavaProcessInfo))
                    {
                        switch (strJavaProcessInfo)
                        {
                            case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_MENUPOP_BYPID:
                                strCmmdAttach = $" -TypePId {targetProces.Id}";
                                break;
                            case SystemConstant.CNST_APPCONFIG_APPREG_ATTR_MENUPOP_BYPNAME:
                                strCmmdAttach = $" -TypePName {appInfo.PROCESS_IDENTIFIER}";
                                break;
                            default: break;
                        }
                    }
                }catch(Exception ex)
                {
                    Logger.Error("StartJAVAApplication", strError = string.Format(Resources.mars_keyword_start_application_cant_create_process, appInfo.STARTER_COMMAND),
                        ex.StackTrace);
                    strAdv = strError;
                    strStack = ex.StackTrace;
                    return false;
                }

                var chckAndWait = ParaCheckMainWait.IsParaForCheckWait(strPara);
                /// then start java agent
                /// 
                bool isOk = false;
                string strStartCmd = MarsJavaSupport.GetJavaStartCommand(ref strError, ref isOk);
                if (isOk)
                {
                    if (chckAndWait != null)
                    {
                        Thread.Sleep(chckAndWait.waitTime*1000);
                    }

                    ProcessStartInfo javaPStart = new ProcessStartInfo();
                    javaPStart.FileName = "java.exe";
                    javaPStart.Arguments = $"-jar \"{strStartCmd}\" {strCmmdAttach}";
                    javaPStart.WorkingDirectory = System.IO.Path.GetDirectoryName(strStartCmd);
                    javaPStart.CreateNoWindow = false;
                    var javaP = Process.Start(javaPStart);
                    
                    // wait until the app is injected
                    KeyWordHelper KH = new KeyWordHelper();
                    KH.WaitUntilTimeOut(5, () =>
                    {
                        return javaP.HasExited;
                    },
                    (msg) =>
                    {
                        Logger.Info("StartJAVAApplication", msg);
                    });

                    if (javaP.HasExited && (javaP.ExitCode != 1))
                    {

                        Logger.Error("StartJAVAApplication", strError = "java has exits with error code. ");
                        isOk = false;
                        return false;
                    }
                    // create wsClient and send message
                    MarsJavaWebSocketClient wsClient = MarsJavaWebSocketClient.GetJavaWebSocketClient("ws://localhost:8062");
                    if (!wsClient.ReconnectToJavaJvmServer(ref strError))
                    {
                        Logger.Info("StartJAVAApplication", $"Can't connect to WS of java engine with error|{strError}|");
                        return false;
                    }
                }
                else
                {
                    Logger.Error("StartJAVAApplication", $"can't get java command with Error:{strError}");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                strError = Resources.mars_keyword_start_application_web_failed;
                Logger.Error("StartJAVAApplication", $"{Resources.mars_keyword_start_application_web_failed} with exception [{e.Message}]", e);
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;

                return false;
            }
            finally
            {
                Logger.logEnd("StartJAVAApplication");
            }
        }

        private static bool StartWebApplication(B_REGISTERED_APPS appInfo,string strPara, ref string strError, ref string strAdv, ref string strStack)
        {
            Logger.logBegin("StartWebApplication", $"strPara:{strPara}");
            try
            {            
                if (appInfo == null)
                {
                    strAdv = "Contact Marquis";
                    strError = Resources.mars_keyword_start_application_web_no_shortname_indb;// "No Application information from DB";
                    return false;
                }
                string strURL = $"{appInfo.STARTER_PATH}{appInfo.STARTER_COMMAND}";
                if ((appInfo.EXTRAREQUIREMENT.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WEB_IE) >= 0))
                {
                    return MARSWebDriver.GetInstance(strPara).StartWebApplication(strURL, strPara, ref strError, ref strAdv, ref strStack);
                }
                else
                {
                    strError = string.Format(Resources.mars_keyword_start_applicaton_web_not_set_4_browser, appInfo.APP_SHORT_NAME);
                    strAdv = "Contact Marquis";
                    strStack = "";
                    return false;
                }
            }
            catch (Exception e)
            {
                strError = Resources.mars_keyword_start_application_web_failed;
                Logger.Error("StartWebApplication",$"{Resources.mars_keyword_start_application_web_failed} with exception [{e.Message}]" , e);
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;
                
                return false;
            }
            finally
            {
                Logger.logEnd("StartWebApplication");
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_RemoveVariable(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            ///消除变量状态
            ///
            Logger.logBegin("MARSKEYWORD_RemoveVariable");
            Logger.logEnd("MARSKEYWORD_RemoveVariable");
            return true;
        }
        /// <summary>
        /// 仅用在web相关的应用中，将提供xpath的测试
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_WebTestDialog(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            ///消除变量状态
            ///
            Logger.logBegin("MARSKEYWORD_WebTestDialog");
            var testDialg = MARSXpathDialog.GetInstance();
            if (testDialg != null)
            {
                testDialg.ShowDialog();
            }
            Logger.logEnd("MARSKEYWORD_WebTestDialog");
            return true;
        }

        private static bool MARSKEYWORD_OpenExternalFile(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_OpenExternalFile", $"Parameter:[{strParaMeter}]-Data [{strData}]");

            if (dealResult == null)
                dealResult = new MARSDealResult();

            dealResult.AskTime = DateTime.Now;

            try
            {
                // 检查agent服务程序是否存在
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string agentDir = Path.Combine(appDir, "agent");
                string agentExePath = Path.Combine(agentDir, "MARSFileAgent.exe");

                if (!File.Exists(agentExePath))
                {
                    strError = $"FAILED: Agent service not found. Expected location: {agentExePath}. Please ensure the agent is installed properly.";
                    Logger.Error("MARSKEYWORD_OpenExternalFile", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    return false;
                }

                // 构造启动参数，传入strParameter和strData
                string arguments = $"-method OpenExternalFile -parameter \"{strParaMeter}\" -data \"{strData}\"";

                // 启动agent进程
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = agentExePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    WorkingDirectory = agentDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process agentProcess = Process.Start(startInfo);

                if (agentProcess == null)
                {
                    strError = "FAILED: Unable to start agent process.";
                    Logger.Error("MARSKEYWORD_OpenExternalFile", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    return false;
                }

                // 等待进程输出，获取进程ID
                string output = agentProcess.StandardOutput.ReadToEnd();
                string errorOutput = agentProcess.StandardError.ReadToEnd();

                agentProcess.WaitForExit(5000); // 等待最多5秒

                if (!string.IsNullOrEmpty(errorOutput))
                {
                    strError = $"FAILED: Agent error: {errorOutput}";
                    Logger.Error("MARSKEYWORD_OpenExternalFile", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    return false;
                }

                // 尝试从输出解析进程ID
                int processId = 0;
                if (!string.IsNullOrEmpty(output) && int.TryParse(output.Trim(), out processId))
                {
                    Logger.Info("MARSKEYWORD_OpenExternalFile", $"Successfully opened external file with process ID: {processId}");
                    dealResult.ResultMessage = $"OK,ProcessId={processId}";
                    dealResult.ErrorMessage = string.Empty;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    return true;
                }
                else
                {
                    strError = $"FAILED: Unable to parse process ID from agent output: {output}";
                    Logger.Error("MARSKEYWORD_OpenExternalFile", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strError = $"FAILED: Exception occurred: {ex.Message}";
                Logger.Error("MARSKEYWORD_OpenExternalFile", strError, ex);
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.AckTime = DateTime.Now;
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_OpenExternalFile");
            }
        }
        

        /// <summary>
        /// Query Data from Data source. 
        /// Get data from data source name, and then save to MemeoryVar, as table or 
        /// sample QueryDataFromDataSource,,
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_QueryDataFromDataSource(long runOrdId, string strParaMeter, 
            string strData, string strApiRunTimeConfig, B_V_OBJECT_SNAPSHOT stepObject, 
            string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_QueryDataFromDataSource", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            try
            {
                /// setps:
                /// 1, check data setting is right or not
                /// 2, get data settings
                /// 3, 
                /// 
                bool isOk = false ;
                DBQueryKeywordDataParaMgr dbQueryMgr = new DBQueryKeywordDataParaMgr();
                /// request
                DBQueryDataSettingRoot dataSettingForQuery = dbQueryMgr.isDataSettingFromStepIsRight(strData, ref isOk);
                if (!isOk)
                {
                    dealResult.ErrorMessage = Resources.mars_wrong_dataSetting_format_for_query_data;
                    dealResult.ResultMessage = "FAILED";
                    return false; 
                }
                if ((dataSettingForQuery.Export == null)||(string.IsNullOrEmpty(dataSettingForQuery.Export.varName)))
                {
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError = Resources.mars_datasource_data_no_export);
                    dealResult.ErrorMessage = strError;                     
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                // get data set via RESTful
                B_QUERY clientQuery = new B_QUERY();
                string strStack = "", strAdv = "";
                B_QUERY webDataSourceReturnedData = clientQuery.GetQuerySourceVarName(strParaMeter,strDBIdx,
                    ref isOk,ref strError, ref strStack, ref strAdv);
                if (!isOk)
                {
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                DatasourceManament dbsourceMgr = new DatasourceManament();
                MarsEngineDBSourceRoot dbsourceData = dbsourceMgr.PharseDbsource(webDataSourceReturnedData.QUERY_DESC,ref isOk, ref strError);
                if ((!isOk) || (dbsourceData == null))
                {
                    isOk = false;
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                if ((webDataSourceReturnedData.DB_CONN == null)
                    ||(string.IsNullOrEmpty(webDataSourceReturnedData.DB_CONN.CONNECTION_STRING)))
                {
                    isOk = false;
                    dealResult.ErrorMessage = (strError = Resources.mars_datasouce_no_db_connection_string);
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError);
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                DataTable dbtbl = dbsourceMgr.QueryDataBasedonDataSource(dbsourceData,
                    dataSettingForQuery,
                    webDataSourceReturnedData.DB_CONN.CONNECTION_STRING, 
                    ref isOk, ref strError );
                if ((!isOk) || (dbtbl == null))
                {
                    isOk = false;
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }

                /// save table to MARS symbolTable                
                isOk = MarsSystemSymbolTableMgr.putVarToSymbolTable(dataSettingForQuery.Export.varName,
                    dataSettingForQuery.Export.varType, dbtbl, ref strError);
                if (!isOk)
                {
                    Logger.Error("MARSKEYWORD_QueryDataFromDataSource", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                strError = Resources.mars_keyword_load_variable_backend_error;
                Logger.Error("MARSKEYWORD_QueryDataFromDataSource", e.Message, e.StackTrace);
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_QueryDataFromDataSource");
            }
        }

        /// <summary>
        /// Requirement is for fhlb's project. Finastra summit will open a window file. Fhlb requires to snapshot the word. So, 
        /// active the process and then snapshot the window. 
        /// The keyword implementation is in an agent. to Use the agent, the agent should be installed in the target machine. 
        /// The agent will be started by MARS automatically.
        /// once the agent method returns, the result json is:
        /// {
        ///     status: "OK"|"FAILED",
        ///     message: "Success"|errorMessage is status is "FAILED",
        ///     ExternalData:processId|stackTrace if status is "FAILED"
        /// }
        /// the keyword running in agent only.
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter">prefix, all parameter will pass to agent</param>
        /// <param name="strData">process the whole string will pass to agent</param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_ActiveProcess(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                    B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                    ref MARSDealResult dealResult,
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ActiveProcess", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            try
            {
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.AskTime = DateTime.Now;

                if (string.IsNullOrEmpty(strParaMeter)||string.IsNullOrEmpty(strData))
                {
                    strError = "Invalid input parameters.";
                    Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }

                try
                {
                    string strAgentName = Mars.AutoTestingDriver.Utils.ExternalAgentManager.DEFAULT_AGENT_NAME;
                    var cfg = Mars.AutoTestingDriver.Utils.ExternalAgentManager.GetAgentConfig(strAgentName);
                    if (cfg == null)
                    {
                        strError = $"Agent configuration for '{strAgentName}' not found.";
                        Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                        dealResult.ErrorMessage = strError;
                        dealResult.ResultMessage = "FAILED";
                        return false;
                    }

                    if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.IsAgentRunning(cfg))
                    {
                        if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.StartAgent(cfg, out string startErr))
                        {
                            strError = $"Failed to start agent '{strAgentName}': {startErr}";
                            Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                            dealResult.ErrorMessage = strError;
                            dealResult.ResultMessage = "FAILED";
                            return false;
                        }
                    }

                    // If configured, invoke agent action 'ActiveProcess' passing strData as payload
                    if (!string.IsNullOrEmpty(cfg.InvokeUrl) && cfg.UseHttp)
                    {
                        var task = Mars.AutoTestingDriver.Utils.ExternalAgentManager.InvokeAgentAsync(cfg, "ActiveProcess", strData);
                        task.Wait();
                        var resp = task.Result;
                        // try to parse response JSON and set dealResult accordingly
                        try
                        {
                            var j = Newtonsoft.Json.Linq.JObject.Parse(resp);
                            var status = (string)j["status"] ?? "FAILED";
                            dealResult.ResultMessage = status == "OK" ? "SUCCESS" : "FAILED";
                            dealResult.ErrorMessage = (string)j["message"] ?? string.Empty;
                            dealResult.ReturnedData = j["ExternalData"]?.ToString();
                            try
                            {
                                // store returned data for later retrieval
                                Mars.AutoTestingDriver.AISupport.AgentSupport.AgentMethodDataStorage.SetMethodData(cfg.AgentName ?? strAgentName, "ActiveProcess", dealResult.ReturnedData);
                            }
                            catch { }
                            return status == "OK";
                        }
                        catch
                        {
                            // not JSON or parse error — treat as success if non-empty
                            if (!string.IsNullOrEmpty(resp))
                            {
                                dealResult.ResultMessage = "SUCCESS";
                                dealResult.ErrorMessage = resp;
                                try
                                {
                                    Mars.AutoTestingDriver.AISupport.AgentSupport.AgentMethodDataStorage.SetMethodData(cfg.AgentName ?? strAgentName, "ActiveProcess", resp);
                                }
                                catch { }
                                return true;
                            }
                            dealResult.ResultMessage = "FAILED";
                            dealResult.ErrorMessage = "Agent invocation returned empty response.";
                            return false;
                        }
                    }
                    else
                    {
                        // No invoke url configured; agent process is ensured running — consider success
                        dealResult.ResultMessage = "SUCCESS";
                        dealResult.ErrorMessage = "";
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    Logger.Error("MARSKEYWORD_ActiveProcess", ex.Message, ex.StackTrace);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }

            }
            catch (Exception e)
            {
                strError = string.Format("Exception in MARSKEYWORD_AssetValue: {0}", e.Message);
                Logger.Error("MARSKEYWORD_AssetValue", e.Message, e.StackTrace);
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_AssetValue");
            }
        }
        /// <summary>
        /// close process is an  agent provided method, from default agent. the  invoke format should be closeProcess(nullable, parameter:nullable, data:PId, comment:nullable)
        /// PId is from from AgentMethodDataStorage, latest data from ActiveProcess.externalData
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter">not required</param>
        /// <param name="strData">nullable</param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_CloseProcess(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                   B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                   ref MARSDealResult dealResult,
                   Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                   string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CloseProcess", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            try
            {
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.AskTime = DateTime.Now;

                if (string.IsNullOrEmpty(strParaMeter) || string.IsNullOrEmpty(strData))
                {
                    strError = "Invalid input parameters.";
                    Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }

                try
                {
                    string strAgentName = Mars.AutoTestingDriver.Utils.ExternalAgentManager.DEFAULT_AGENT_NAME;
                    var cfg = Mars.AutoTestingDriver.Utils.ExternalAgentManager.GetAgentConfig(strAgentName);
                    if (cfg == null)
                    {
                        strError = $"Agent configuration for '{strAgentName}' not found.";
                        Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                        dealResult.ErrorMessage = strError;
                        dealResult.ResultMessage = "FAILED";
                        return false;
                    }

                    if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.IsAgentRunning(cfg))
                    {
                        if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.StartAgent(cfg, out string startErr))
                        {
                            strError = $"Failed to start agent '{strAgentName}': {startErr}";
                            Logger.Error("MARSKEYWORD_ActiveProcess", strError);
                            dealResult.ErrorMessage = strError;
                            dealResult.ResultMessage = "FAILED";
                            return false;
                        }
                    }

                    // close process is an agent provided method. The invoke format expected by agent is:
                    // closeProcess(nullable, parameter:nullable, data:PId, comment:nullable)
                    // where PId is obtained from AgentMethodDataStorage latest ActiveProcess ExternalData.
                    if (!string.IsNullOrEmpty(cfg.InvokeUrl) && cfg.UseHttp)
                    {
                        // retrieve last ActiveProcess returned data
                        string last = null;
                        try
                        {
                            last = Mars.AutoTestingDriver.AISupport.AgentSupport.AgentMethodDataStorage.GetMethodData(cfg.AgentName ?? strAgentName, "ActiveProcess");
                        }
                        catch { last = null; }

                        string pid = null;
                        if (!string.IsNullOrEmpty(last))
                        {
                            // ExternalData may be "pid" or "pid|stacktrace" — extract leading integer token
                            var parts = last.Split(new char[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var p in parts)
                            {
                                var t = p.Trim();
                                if (int.TryParse(t, out var _))
                                {
                                    pid = t;
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(pid))
                        {
                            strError = "Cannot determine PID from last ActiveProcess data.";
                            Logger.Error("MARSKEYWORD_CloseProcess", strError);
                            dealResult.ErrorMessage = strError;
                            dealResult.ResultMessage = "FAILED";
                            return false;
                        }

                        // build payload according to agent expectation
                        var payloadObj = new
                        {
                            parameter = strParaMeter ?? (string)null,
                            data = pid,
                            comment = strData ?? (string)null
                        };
                        var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payloadObj);

                        var task = Mars.AutoTestingDriver.Utils.ExternalAgentManager.InvokeAgentAsync(cfg, "closeProcess", payloadJson);
                        task.Wait();
                        var resp = task.Result;
                        try
                        {
                            var j = Newtonsoft.Json.Linq.JObject.Parse(resp);
                            var status = (string)j["status"] ?? "FAILED";
                            dealResult.ResultMessage = status == "OK" ? "SUCCESS" : "FAILED";
                            dealResult.ErrorMessage = (string)j["message"] ?? string.Empty;
                            dealResult.ReturnedData = j["ExternalData"]?.ToString();
                            return status == "OK";
                        }
                        catch
                        {
                            if (!string.IsNullOrEmpty(resp))
                            {
                                dealResult.ResultMessage = "SUCCESS";
                                dealResult.ErrorMessage = resp;
                                return true;
                            }
                            dealResult.ResultMessage = "FAILED";
                            dealResult.ErrorMessage = "Agent invocation returned empty response.";
                            return false;
                        }
                    }
                    else
                    {
                        // No HTTP invoke configured — cannot perform closeProcess
                        strError = "Agent invoke URL not configured for closeProcess.";
                        Logger.Error("MARSKEYWORD_CloseProcess", strError);
                        dealResult.ErrorMessage = strError;
                        dealResult.ResultMessage = "FAILED";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    Logger.Error("MARSKEYWORD_CloseProcess", ex.Message, ex.StackTrace);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }

            }
            catch (Exception e)
            {
                strError = string.Format("Exception in MARSKEYWORD_CloseProcess: {0}", e.Message);
                Logger.Error("MARSKEYWORD_CloseProcess", e.Message, e.StackTrace);
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_CloseProcess");
            }
        }
        

        /// <summary>
        /// 判断数据是否和指定的数据一致。
        /// strData 可以有多种格式：
        /// 主要是前后两段分开。变量部分在前面，如：Modaul_Var:variableName;Data to be compared
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData">strData 可以有多种格式：主要是前后两段分开。变量部分在前面，如：Modaul_Var:variableName;Data to be compared
        ///  如果是Mem_Var:variableName;Data to be compared，则变量的值在memory中获得。
        /// memory var，可以用captureValue实现。data参数为：ToMem:variableName
        /// </param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_AssertValue(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                    B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                    ref MARSDealResult dealResult,
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_AssertValue", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            try
            {
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.AskTime = DateTime.Now;

                // 检查 strData 是否为空
                if (string.IsNullOrEmpty(strData))
                {
                    strError = "Data parameter is empty. Expected format: MemVar:varIndex;DataToCompare";
                    Logger.Error("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 检查是否是 MemVar 模式
                const string cnst_MemVar_Prefix = "MemVar:";
                if (!strData.StartsWith(cnst_MemVar_Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    strError = string.Format("Unsupported format: [{0}]. Expected format: MemVar:varIndex;DataToCompare", strData);
                    Logger.Error("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 解析 MemVar:varIndex;DataToCompare 格式
                string dataAfterPrefix = strData.Substring(cnst_MemVar_Prefix.Length);
                int separatorPos = dataAfterPrefix.IndexOf(';');

                if (separatorPos < 0)
                {
                    strError = string.Format("Invalid format: [{0}]. Expected format: MemVar:varIndex;DataToCompare", strData);
                    Logger.Error("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 提取变量索引和要比较的数据
                string varIndex = dataAfterPrefix.Substring(0, separatorPos).Trim();
                string dataToCompare = dataAfterPrefix.Substring(separatorPos + 1);

                if (string.IsNullOrEmpty(varIndex))
                {
                    strError = "Variable index is empty in MemVar format";
                    Logger.Error("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 从 CaptureParaMgr.globalMemoryData 中获取变量值
                string actualValue = "";
                string getVarError = "";
                bool isGetVarOk = CaptureParaMgr.GetVariableByIdx(varIndex, ref actualValue, ref getVarError);

                if (!isGetVarOk)
                {
                    strError = string.Format("Failed to get memory variable [{0}]: {1}", varIndex, getVarError);
                    Logger.Error("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 比较实际值和期望值
                Logger.Info("MARSKEYWORD_AssertValue", string.Format("Comparing: actualValue=[{0}], expectedValue=[{1}]", actualValue, dataToCompare));
                /// 比较模式有两种，一，采用正则表达式，2，采用normal模式
                /// 
                bool isMatch = false;
                if (actualValue == null && dataToCompare == null)
                {
                    isMatch = true;
                }
                else if (actualValue == null || dataToCompare == null)
                {
                    isMatch = false;
                }
                else
                {
                    // 支持精确匹配和正则表达式匹配
                    isMatch = (string.Compare(actualValue, dataToCompare, false) == 0)
                           || MarsWindowsAPIsExtend.RegularTest(dataToCompare, actualValue);
                }

                dealResult.ActualInputData = actualValue;
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                dealResult.AckTime = DateTime.Now;

                if (isMatch)
                {
                    strError = string.Format("AssertValue succeeded: Variable [{0}] value [{1}] matches expected value [{2}]",
                        varIndex, actualValue, dataToCompare);
                    Logger.Info("MARSKEYWORD_AssertValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "SUCCESS";
                    return true;
                }
                else
                {
                    strError = string.Format("AssetValue failed: Variable [{0}] value [{1}] does not match expected value [{2}]",
                        varIndex, actualValue, dataToCompare);
                    Logger.Error("MARSKEYWORD_AssetValue", strError);
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = "FAILED";
                    return false;
                }
            }
            catch (Exception e)
            {
                strError = string.Format("Exception in MARSKEYWORD_AssetValue: {0}", e.Message);
                Logger.Error("MARSKEYWORD_AssetValue", e.Message, e.StackTrace);
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_AssetValue");
            }
        }


        /// <summary>
        /// load special variable to Memoery.这里支持所有的variable,但是由于时间关系，先支持status variable
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="strParaMeter">varialbe type 内存索引名称 </param>
        /// <param name="strData">variable name, like normal</param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <returns></returns>
        /// 示例： loadVariables, [], To:Loop:VariableNameInMemory, Modual or Global
        private static bool MARSKEYWORD_LoadVariables(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError, 
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_LoadVariables", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            try
            {
                StatusVariablePara loadVarInfo = StatusVariablePara.GetVariableParaInst(strData);
                if (loadVarInfo == null)
                {
                    strError = string.Format(Resources.mars_keywod_load_variable_status,//"Status variable format is wrong, it should be :{0}", 
                        StatusVariablePara.cnst_formatOfSource);
                    return false;
                }
                /// 从后台获得数据
                /// 
                B_SYSTEM_LOOKUP dataLookup = new B_SYSTEM_LOOKUP();
                var lstVars= dataLookup.GetSystemLookup(loadVarInfo.varType.ToUpper(), loadVarInfo.AliasOfVar,strDBIdx);
                var lstVarsDB = lstVars.Select(p => new { p.LOOKUP_ID, p.DISPLAY_NAME }).ToList();
                /// 添加到vartable中
                /// 假定目前只有status variable
                /// 
                foreach(var itm in lstVarsDB)
                {
                    if (itm == null) continue;
                    MarsStatusVar statusVar = new MarsStatusVar(loadVarInfo.AliasOfVar, itm.DISPLAY_NAME, itm.LOOKUP_ID);
                }

                return true;
            }catch(Exception e)
            {
                strError = Resources.mars_keyword_load_variable_backend_error;
                Logger.Error("MARSKEYWORD_LoadVariables", e.Message, e.StackTrace);
                return false ;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_LoadVariables");
            }
        }


        
        private static bool MARSKEYWORD_SubLoop(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SubLoop", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            /// loop中可以使用 fromVar:VariableName 在data中，采用格式如下
            /// FromVar:TEST_STAUTS
            /// 
            string strDataNoPreFix = "",
                strVarIdx = "",
                strVarTyp = "";
            if (MarsVarDataPrefix.IsVariableFormat(strData, ref strDataNoPreFix, ref strVarIdx, ref strVarTyp))
            {
                /// 从中获得数据
                /// 
                List<MarsVarBasic> dataFromVariableTable = null;
                if (!MarsVarDataPrefix.GetVariable(strVarIdx, strVarTyp, ref dataFromVariableTable))
                {
                    if (dealResult == null)
                        dealResult = new MARSDealResult();
                    dealResult.ResultMessage = "FAILED";
                    dealResult.ErrorMessage = strError = string.Format(Resources.mars_keyword_loop_can_not_get_loop_var, strData);
                    Logger.Error("MARSKEYWORD_SubLoop", strError);
                    return false;
                }
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.ResultMessage = MARSDealResult.CNST_SUCCESS;
                //dealResult.re
            }
            Logger.logEnd("MARSKEYWORD_SubLoop");
            return true;
        }

        private static bool MARSKEYWORD_Loop(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_Loop", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            /// loop中可以使用 fromVar:VariableName 在data中，采用格式如下
            /// FromVar:TEST_STAUTS
            /// 
            string strDataNoPreFix = "",
                strVarIdx="",
                strVarTyp="";
            if (MarsVarDataPrefix.IsVariableFormat(strData, ref strDataNoPreFix,ref strVarIdx, ref strVarTyp))
            {
                /// 从中获得数据
                /// 
                List<MarsVarBasic> dataFromVariableTable = null;
                if (!MarsVarDataPrefix.GetVariable(strVarIdx, strVarTyp, ref dataFromVariableTable))
                {
                    if (dealResult == null)
                        dealResult = new MARSDealResult();
                    dealResult.ResultMessage = "FAILED";
                    dealResult.ErrorMessage = strError= string.Format(Resources.mars_keyword_loop_can_not_get_loop_var, strData) ;
                    Logger.Error("MARSKEYWORD_Loop", strError);
                    return false;
                }
                if (dealResult == null)
                    dealResult = new MARSDealResult();
                dealResult.ResultMessage = MARSDealResult.CNST_SUCCESS;
                //dealResult.re
            }
            Logger.logEnd("MARSKEYWORD_Loop");
            return true;
        }

        private static bool MARSKEYWORD_EndLoop(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, 
            ref string strError, ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_EndLoop", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            Logger.logEnd("MARSKEYWORD_EndLoop");
            return true;
        }

        private static bool MARSKEYWORD_EndSubLoop(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_EndSubLoop", string.Format("para:[{0}] data-[{1}]", strParaMeter, strData));
            Logger.logEnd("MARSKEYWORD_EndSubLoop");
            return true;
        }
        


        private static bool MARSKEYWORD_KillApplication(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, 
            ref string strError, 
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("KillApplication", string.Format("ParaMeter:[{0}] strData:[{1}]", strParaMeter, strData));
            ConsoleLog.IntimeLog("\tKillAppliation [{0}] [Data-{1}]", strParaMeter, strData);

            if (((string.IsNullOrEmpty(strParaMeter) && (string.IsNullOrEmpty(strData)))))
            {
                ConsoleLog.IntimeLog(strError = Resources.mars_keyword_killapplication_no_parameter_data);// "\t......no Parameter and DataSetting, return false");;
                Logger.Error("KillApplication", strError);
                return false;
            }
            if ((!string.IsNullOrEmpty(strData)) && (string.IsNullOrEmpty(strParaMeter)))
            {
                strParaMeter = strData;
            }
            string strProcessNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(strParaMeter);
            Process curP = Process.GetCurrentProcess();
            Process[] arrPr = Process.GetProcessesByName(strProcessNameWithoutExtension);
            if ((arrPr == null) || (arrPr.Length == 0))
            {
                ConsoleLog.IntimeLog("\t...No such application [{0}] runs, return ture", strParaMeter);
                Logger.Info("KillApplication", "\t...No such application [{0}] runs, return ture", strParaMeter);
                return true;
            }
            try
            {
                foreach (var itm in arrPr)
                {
                    if (itm == null) continue;
                    if (itm.SessionId == curP.SessionId)
                        itm.Kill();
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format(Resources.mars_keyword_killapplication_cannot_kill,//"\t...Exception when kill application:[{0}]\r\n\t{1}\r\n\t{2}", 
                    strParaMeter, e.Message, e.StackTrace);
                ConsoleLog.IntimeLog(strError);
                Logger.Error("KillApplication", strError);
                return false;
            }


        }

    }

}
