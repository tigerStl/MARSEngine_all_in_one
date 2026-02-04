extern alias clientWCF;
using com.Mars.Constants;

using MarsTestFrame.SourceCode.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using Mars.Dto;
using Mars.Business;

#if _Datafrom_Database
using MarsTestFrame.SourceCode.com.Mars.DB.Dto;
#endif

namespace MarsTestFrame.com.Mars.TestConfigObjects.Adatpers
{
    public class TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteAdapter));
        public virtual ConfigObjectBase LoadTestSuiteInfo(DataRow objRow, int iRowId = -1,long lAppId=-1)
        {
            return null;
        }
        
    }

#if _Datafrom_Database
    public class TestStepsDBAdapter: TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepsDBAdapter));
    }

    public class TestSuiteDBAdapter:TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteXlsAdapter));

       
        
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow, int iRowId = -1, long lAppId = -1)
        {
            Logger.Warnning("LoadTestSuiteInfo", "This method for Database is deceptive. Don't use it for Datagetting. use LoadtestSuiteInfo(V_DASHBOARD_TEST_FULLVISION objOneItem, int iRowID=-1) instead");
            return null;
        }

        public ConfigObjectBase LoadTestSuiteInfo(V_STORYBOARD_TEST_FULLVISIONDTO objOneItem, int iRowID=-1,long lAppId=-1)
        {
            Logger.Info("LoadTestSuiteInfo", string.Format("with iRowID [{0}]", iRowID));
            if (objOneItem == null)
            {
                Logger.Warnning("LoadTestSuiteInfo", "Object is null. ObjOneItem == null");
                return null;
            }
            BatchConfigObjectFromDB objBatch = new BatchConfigObjectFromDB();
            objBatch.Action = ConvertTypeValue2String(objOneItem.TEST_RUN_VALUE);
            objBatch.TestSuiteKeyID = objOneItem.TEST_SUITE_ID;
            objBatch.TestSuiteID = objOneItem.TEST_SUITE_ID == null ? null : objOneItem.TEST_SUITE_ID + "";
            objBatch.PreParentId = objOneItem.DEPENDS_ON == null ? null : objOneItem.DEPENDS_ON + "";
            
            objBatch.TCFilePath = objOneItem.TEST_SUITE_NAME;
            objBatch.TestCaseKeyId = objOneItem.TEST_CASE_ID;
            objBatch.TCSheetName = objOneItem.TEST_CASE_NAME;
            objBatch.RunResult = ConvertResultValue2String(objOneItem.HIST_RESULT);
#if v_useNameId
            objBatch.CurrentTestAppId = lAppId;
#endif
#if v_16AndUp
            objBatch.DataSetName = objOneItem.DATA_SET_ALIAS_NAME;
#endif
            objBatch.AssignedStoryObject = objOneItem;

            return objBatch;

            /** objBatch.Action = objRow[SystemConstant.CNST_XLS_HEADER_RUN].ToString();
                objBatch.TCFilePath = objRow[SystemConstant.CNST_XLS_HEADER_TEST_WORKBOOK].ToString();
                objBatch.TCSheetName = objRow[SystemConstant.CNST_XLS_HEADER_TEST_SHEET].ToString();
                
                objBatch.TestSuiteID = objRow[SystemConstant.CNST_XLS_HEADER_RELY].ToString();
                objBatch.RunResult = objRow[SystemConstant.CNST_XLS_HEADER_RESULT].ToString();*/

        }


        private string ConvertResultValue2String(short? sValue)
        {
            if (sValue == null) return null;
            if (sValue == 1) return BatchXls.cnst_SUCCESS;
            return BatchXls.cnst_FAILURE;
        }

        private string ConvertTypeValue2String(short? iRuntypeValue)
        {
            Logger.Info("ConvertTypeValue2String",string.Format("Parameters:iRuntypeValue:[{0}]", iRuntypeValue==null?"null":iRuntypeValue+"" ));
            switch (iRuntypeValue)
            {
                case null: return SystemConstant.CNST_RUNTYPE_SKIP;
                case (short)ENUM_TEST_SUITE_RUNTYPE._RUN: return SystemConstant.CNST_RUNTYPE_RUN;
                case (short)ENUM_TEST_SUITE_RUNTYPE._DONE:return SystemConstant.CNST_RUNTYPE_DONE;
                case (short)ENUM_TEST_SUITE_RUNTYPE._EXECUTE:return SystemConstant.CNST_RUNTYPE_EXE;                
                //case (short)ENUM_TEST_SUITE_RUNTYPE._FAILUE:return SystemConstant.CNST_RUNTYPE_RUN;
                default:return SystemConstant.CNST_RUNTYPE_SKIP;
            }
        }

    }
#endif
    public class TestSuiteXlsAdapter:TestSuiteAdapter
    {
        
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteXlsAdapter));
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow, int iRunId = -1, long lAppId = -1)
        {
            Logger.logBegin("LoadTestSuiteInfo");
            if (objRow == null)
            {
                Logger.Error("LoadTestSuiteInfo", "objRow == null");
                return null;
            }
            BatchConfigObject objBatch = new BatchConfigObject();
            /*** for excel load ***/
            try
            {
                objBatch.Action = objRow[SystemConstant.CNST_XLS_HEADER_RUN].ToString();
                objBatch.TCFilePath = objRow[SystemConstant.CNST_XLS_HEADER_TEST_WORKBOOK].ToString();
                objBatch.TCSheetName = objRow[SystemConstant.CNST_XLS_HEADER_TEST_SHEET].ToString();
                
                objBatch.TestSuiteID = objRow[SystemConstant.CNST_XLS_HEADER_RELY].ToString();
                objBatch.RunResult = objRow[SystemConstant.CNST_XLS_HEADER_RESULT].ToString();
                if (!string.IsNullOrEmpty(objBatch.RunResult))
                {
                    objBatch.RunResult = objBatch.RunResult.Replace("[", "");
                    objBatch.RunResult = objBatch.RunResult.Replace("]", "");
                    Logger.Info("LoadTestSuiteInfo", string.Format("get Result data:[{0}]", objBatch.RunResult));
                }
                if (objRow.Table.Columns.Contains(SystemConstant.CNST_XLS_HEADER_PARENT))
                {
                    objBatch.PreParentId = objRow[SystemConstant.CNST_XLS_HEADER_PARENT].ToString();
                }
                else
                {
                    Logger.Warnning("LoadTestSuiteInfo", string.Format("no such column exists:[{0}], default empty value is used", SystemConstant.CNST_XLS_HEADER_PARENT)); 
                    objBatch.PreParentId = "";
                }
                return objBatch;
            }
            catch(Exception e)
            {
                Logger.Error("LoadTestSuiteInfo",string.Format("error message:[{0}]",e.Message));
                return null;
            }
            finally
            {
                Logger.logEnd("LoadTestSuiteInfo");
            }
            
        }
    }

    public class TestStepsXlsAdapter : TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepsXlsAdapter));
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow,int iRunId=-1, long lAppId = -1)
        {
            Logger.logBegin("LoadTestSuiteInfo");
            try
            {
                if (objRow == null)
                {
                    Logger.Error("LoadTestSuiteInfo", MarsTestFrame.Properties.Resources.DATA_ROW_IS_NULL);
                    return null;
                }
                TestStep objStep = new TestStep();
                objStep.Keyword = objRow[SystemConstant.CNST_XLS_HEADER_KEYWORD].ToString();
                objStep.ObjectName = objRow[SystemConstant.CNST_XLS_HEADER_OBJECT].ToString();
                objStep.Row_Column = objRow[SystemConstant.CNST_XLS_HEADER_RC].ToString();
                objStep.Value = objRow[SystemConstant.CNST_XLS_HEADER_VALUE].ToString();
                objStep.Comment = objRow[SystemConstant.CNST_XLS_HEADER_COMMENT].ToString();
                objStep.RunID = iRunId;
                Logger.Info("LoadTestSuiteInfo", objStep.ToString());
                if ((objStep.Keyword == null) || (objStep.Keyword == "")) return null;
                
                return objStep;
            }
            finally
            {
                Logger.logEnd("LoadTestSuiteInfo");
            }
        }
        
    }

    public class TestObjectXlsAdapter : TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectXlsAdapter));
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow, int iRowId = -1, long lAppId = -1)
        {
            Logger.logBegin("LoadTestSuiteInfo");
            try
            {
                if (objRow == null) return null;
                if (objRow[SystemConstant.CNST_XLS_HEADER_DIC_OBJECTHAPPYNAME].ToString() == "") return null;
                if (objRow[SystemConstant.CNST_XLS_HEADER_DIC_OBJECTIDENTIFIER].ToString() == "") return null;
                TestObject objTO = null;
                if (TestObject.IsPegwindowObject(objRow[SystemConstant.CNST_XLS_HEADER_DIC_EXPAND].ToString()))
                    objTO = new TestPegwindowObject();
                else
                    objTO = new TestObject(); 
                objTO.ObjectName = objRow[SystemConstant.CNST_XLS_HEADER_DIC_OBJECTHAPPYNAME].ToString();
                objTO.QuickAccessString = objRow[SystemConstant.CNST_XLS_HEADER_DIC_OBJECTIDENTIFIER].ToString() ;
                objTO.Description = objRow[SystemConstant.CNST_XLS_HEADER_DIC_COMMENT].ToString() ;
                objTO.ObjectType = objRow[SystemConstant.CNST_XLS_HEADER_DIC_EXPAND].ToString() ;
                objTO.BuildPegQuickAcessString();

                Logger.Info("--QuickAccess.ReadFromDictionary--", string.Format("ObjectName:[{0}]\r\n\tQuickAccessString[{0}]", objTO.ObjectName,objTO.QuickAccessString));
                return objTO;
            }
            catch(Exception e)
            {
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_UNKNOW_GETDATA_FROM_DICFILE,string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_UNKNOW_GETDATA_FROM_DICFILE),e.Message));
            }
            finally
            {
                Logger.logEnd("LoadTestSuiteInfo");
            }
        }
    }

    public class TestDataXlsAdatper:TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectXlsAdapter));
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow, int iRowId = -1, long lAppId = -1)
        {
            return base.LoadTestSuiteInfo(objRow);
        }
    }

    
    public class TestSuiteAdapterFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteAdapterFactory)) ;
        public static TestSuiteAdapter GetAdapterInstance(MARS_ADAPTER eAdpterId)
        {
            Logger.logBegin("GetAdapterInstance");
            try
            {
                switch (eAdpterId)
                {
                    case MARS_ADAPTER._ADPTR_XLSJET_2_TESTSUITE:
                        return new TestSuiteXlsAdapter();
                    case MARS_ADAPTER._DAPTR_XLSJET_2_TESTSTEP:
                        return new TestStepsXlsAdapter();
#if _Datafrom_Database
                    case MARS_ADAPTER._ADPTR_DB_2_TESTSUITE:
                        return new TestSuiteDBAdapter();
                    case MARS_ADAPTER._ADPTR_DB_2_TESTSTEPS:
                        return new TestStepsDBAdapter();
#endif
                    case MARS_ADAPTER._ADPTR_OBJECTS_LOAD_FROM_XLS:
                        return new TestObjectXlsAdapter();
                    case MARS_ADAPTER._ADPTR_TCDATASOURCE_XLS:
                        return new TestDataXlsAdatper();
                    default: return null;
                }
            }
            finally
            {
                Logger.logEnd("GetAdapterInstance");
            }
        }
    }
}
