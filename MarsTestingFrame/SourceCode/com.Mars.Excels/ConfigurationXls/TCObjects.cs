using Route2NSEx.src.Marquis.systemUtil;
using System;

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;


using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using System.Data;
using MarsTestFrame.com.Mars.TestConfigObjects.Adatpers;
using MarsTestFrame.SourceCode.systemUtil;
using System.IO;
using MarsTestFrame.SourceCode.com.Mars.TCDataSource;
using System.Text.RegularExpressions;
using MarsTestFrame.systemUtil;
namespace MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls
{
    public class TCObjects : MarsExcelFileBase /**Test case */
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TCObjects));

        private const string cnst_data_file_tail = "_DATA";

        #region Properties
        public string CurrentRunName { get; set; } // Sheet Name        
#if v_16AndUp
        public string CurrentDatasetName { get; set; }
#endif
        public TC_STATUS CurrentStatus { get; set; }
        public List<ConfigObjectBase> CurrentStepsList
        {
            get { return mlstSteps; }
        }
        
        #endregion

        #region member
        protected List<ConfigObjectBase> mlstSteps = new List<ConfigObjectBase>();
        protected Hashtable mlstDataFile = new Hashtable(); /** Data files List. Key: TestSuite Name, value: TCDataFile **/
        protected string mstrCurrentDataFileName = null;    /** default is null, should be changed by kewords           **/
        public string Id4Project { get ;set ;}
        public string Action4Project { get ;set ;} 
        #endregion

        protected override void BeforeLoadTestCase()
        {
            Logger.logBegin("BeforeLoadXlsFile");
            this.mstrExtraXlsPath = AppConfigReader.GetXlsRootPath() + "\\TC\\";
            Logger.logEnd("BeforeLoadXlsFile");
        }

        protected override ERROR_CODE mAlystTestCase()
        {
            Logger.logBegin("mAlystExcleFile");
            ERROR_CODE eCode = ERROR_CODE._NO_ERROR;
            eCode = this.GetDataTableFromExcelFile();
            if (eCode != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("mAlystExcleFile", ERROR_INFO.GET_ERROR_STR(eCode));
                return eCode;
            }
            /*** check table/sheet name exits or not ***/
            int iTablePos = this.CheckTableExists(this.CurrentRunName);
            if (iTablePos < 0)
            {
                eCode = ERROR_CODE._TEST_STEP_NO_SUCH_TABLE_OR_SHEET;
                Logger.Error("mAlystExcleFile", string.Format("{0}, SheetName:[{1}], TC/Test Case Name:[{2}]", ERROR_INFO.GET_ERROR_STR(eCode), this.mstrXlsFileName, this.CurrentRunName));
                return eCode;
            }

            /*** get all Data ***/
            mlstSteps.Clear();
            eCode = this.GetSpecialTableDataToList(this.mlstCurrentTables[iTablePos], mlstSteps);

            return eCode;
        }

        protected override ConfigObjectBase mLoadDataRow2ConfigObj(DataRow objOneRow, int iRowId=-1,long lAppId=-1)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            if (objOneRow == null)
            {
                Logger.Error("mLoadDataRow2ConfigObj", MarsTestFrame.Properties.Resources.DATA_ROW_IS_NULL);
                return null;
            }
            if (objOneRow == null) return null;
            TestSuiteAdapter objTestSuiteAdp = TestSuiteAdapterFactory.GetAdapterInstance(MARS_ADAPTER._DAPTR_XLSJET_2_TESTSTEP);
            ConfigObjectBase objResult = objTestSuiteAdp.LoadTestSuiteInfo(objOneRow,iRowId, lAppId);

            Logger.logEnd("mLoadDataRow2ConfigObj");
            return objResult;
        }

        public TCObjects()
            : base()
        {
            CurrentStatus = TC_STATUS._INITIALIZED;
        }


        public List<ConfigObjectBase> getTestSteps()
        {
            Logger.logBegin("getTestSteps");

            Logger.logEnd("getTestSteps");
            return null;
        }

        internal void InitDefaultDataFileName()
        {
            Logger.logBegin("InitDefaultDataFileName");
            int iLastIdx = this.mstrXlsFileName.LastIndexOf(".xls", StringComparison.CurrentCultureIgnoreCase);
            if (iLastIdx < 0) this.mstrCurrentDataFileName = this.mstrXlsFileName;
            else this.mstrCurrentDataFileName = this.mstrXlsFileName.Substring(0, iLastIdx);
            Logger.logEnd("InitDefaultDataFileName");
            return;
        }

        public ERROR_CODE loadData(string strValue = null)
        {
            Logger.logBegin("loadData");
            /*** 获得数据文件名称 ***/
            string strDataFileName = null;
            string strFileName = "";
            if (string.IsNullOrEmpty(strValue))
            {
                /** default **/
                int iLastIdx = this.mstrXlsFileName.LastIndexOf(".xls", StringComparison.CurrentCultureIgnoreCase);
                if (iLastIdx < 0) strFileName = this.mstrXlsFileName;
                else strFileName = this.mstrXlsFileName.Substring(0, iLastIdx);

                strDataFileName = string.Format("{0}\\Data\\{1}{2}", AppConfigReader.GetXlsRootPath(), strFileName, cnst_data_file_tail);
            }
            else
            {
                strDataFileName = string.Format("{0}\\Data\\{1}{2}", AppConfigReader.GetXlsRootPath(), strValue, cnst_data_file_tail);
                strFileName = strValue;
            }
            string strDataFileWithPath = string.Format("{0}{1}.xls", "", strDataFileName);
            if (!File.Exists(strDataFileWithPath))
            {
                Logger.Error("loadData", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_NO_SUCH_DATAFILE), strDataFileWithPath));
                return ERROR_CODE._TCDATA_NO_SUCH_DATAFILE;
            }

            TCDataFile objDataFile = new TCDataFile();
            objDataFile.XlsFileNameWithPath = string.Format("{0}{1}", strFileName, cnst_data_file_tail);
            objDataFile.ExtraFilePath = string.Format("{0}\\data\\", AppConfigReader.GetXlsRootPath());
            ERROR_CODE eCde = objDataFile.loadTestCase();
            //objDataFile.CloseDataFile();
            if (!mlstDataFile.ContainsKey(strFileName))
                mlstDataFile.Add(strFileName, objDataFile);
            else
                mlstDataFile[strFileName] = objDataFile;
            Logger.logEnd("loadData");
            return ERROR_CODE._NO_ERROR;
        }

        internal virtual string GetDataStringFromDataFile(string strObjectName, int iLoopId, ref ERROR_CODE eCde,int iStepId =-1)
        {
            Logger.logBegin("GetDataStringFromDataFile");
            if (!this.mlstDataFile.ContainsKey(this.mstrCurrentDataFileName))
            {
                eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                Logger.Error("GetDataStringFromDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrCurrentDataFileName));
                return null;
            }

            TCDataFile objDataFile = (TCDataFile)this.mlstDataFile[this.mstrCurrentDataFileName];
            string strResult = "";
            eCde = objDataFile.GetOneCellValueFromData(iLoopId, strObjectName, ref strResult);
            Logger.logEnd("GetDataStringFromDataFile");
            return strResult;
        }

        internal bool IsDataSetSet2Skipped(int iLoopID)
        {
            Logger.logBegin("IsDataSetSet2Skipped");
            Logger.Info("IsDataSetSet2Skipped", string.Format("Trying to get value from data file with LoopID:[{0}] for _SYSTEM_RUNMARK_", iLoopID));
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR ;
            try
            {
                string strRunMark = GetDataStringFromDataFile(SystemConstant.CNST_XLS_DATAFIELD_SYSTEM_RUNMARK, iLoopID, ref eCde);

                if (eCde == ERROR_CODE._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2)
                {
                    Logger.Info("IsDataSetSet2Skipped", string.Format("No such column [{0}] find for skip mark, default value, false, returns", SystemConstant.CNST_XLS_DATAFIELD_SYSTEM_RUNMARK));
                    return false;
                }

                if (eCde == ERROR_CODE._NO_ERROR)
                {
                    if (string.Compare(SystemConstant.CNST_XLS_DATAFIELD_SYSTEM_RUNMARK_SKIP, strRunMark, true) == 0)
                    {
                        Logger.Info("IsDataSetSet2Skipped", string.Format("Get data from data file :[{0}],return true", strRunMark));
                        return true;
                    }
                }
                /** other errors, just return false **/
                Logger.Info("IsDataSetSet2Skipped","Returns :false");
                return false;
            }
            finally
            {
                Logger.logEnd("IsDataSetSet2Skipped");
                
            }
            
        }


        internal ERROR_CODE SaveDataToSpecialCell(string strObjectName, int iLoopId, string strData2Store)
        {
            Logger.logBegin("SaveDataToSpecialCell");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (!this.mlstDataFile.ContainsKey(this.mstrCurrentDataFileName))
            {
                eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                Logger.Error("SaveDataToSpecialCell", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrCurrentDataFileName));
                return eCde;
            }

            TCDataFile objDataFile = (TCDataFile)this.mlstDataFile[this.mstrCurrentDataFileName] ;
            eCde = objDataFile.UpdateDataForDataFile(strObjectName, iLoopId, strData2Store);

            Logger.logEnd("SaveDataToSpecialCell");
            return eCde;
        }

        protected ERROR_CODE UpdateDataForDataFile(string strObjectName, int iLoopId, string strData2Update)
        {
            Logger.logBegin("UpdateDataForDataFile");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (!this.mlstDataFile.ContainsKey(this.mstrCurrentDataFileName))
            {
                eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                Logger.Error("GetDataStringFromDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrCurrentDataFileName));
                return eCde;
            }

            TCDataFile objDataFile = (TCDataFile)this.mlstDataFile[this.mstrCurrentDataFileName];
            eCde = objDataFile.UpdateDataForDataFile(strObjectName, iLoopId + 2, strData2Update);
            if (eCde == ERROR_CODE._NO_ERROR)
            {
                /** update cache **/
                eCde = objDataFile.UpdateDataCache(strObjectName, iLoopId + 1, strData2Update);
            }
            Logger.logEnd("UpdateDataForDataFile");
            return eCde;
        }

        public ERROR_CODE SwitchCurrentDataFile(string strTestSuiteName)
        {
            Logger.logBegin("SwitchCurrentDataFile");
            ERROR_CODE eCde = loadData(strTestSuiteName);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("SwitchCurrentDataFile", string.Format("Can't Load Data file:[{0}]", strTestSuiteName));
                return eCde;
            }
            this.mstrCurrentDataFileName = strTestSuiteName;
            Logger.logEnd("SwitchCurrentDataFile");
            return ERROR_CODE._NO_ERROR;
        }

        internal virtual int GetTestLoopCount()
        {
            //XlsFileNameWithPath
            Logger.logBegin("GetTestLoopCount");
            /** get default data file name */
            try
            {

                string strKey = XlsFileNameWithPath;
                
                int iPos = XlsFileNameWithPath.LastIndexOf(cnst_data_file_tail);
                if (iPos >= 0) strKey = XlsFileNameWithPath.Substring(0, iPos);
                iPos = XlsFileNameWithPath.LastIndexOf(".");
                if (iPos >= 0) strKey = XlsFileNameWithPath.Substring(0, iPos);

                if (mlstDataFile.ContainsKey(strKey))
                {
                    TCDataFile objDataFile = (TCDataFile)mlstDataFile[strKey] ;
                    int iLoop = objDataFile.GetColomnCount();
                    return iLoop;
                }
                else
                    return -1;
            }
            finally
            {
                Logger.logEnd("GetTestLoopCount");
            }


        }

        internal virtual ERROR_CODE SaveDataToSpecialCellComparisonMode(string strObjNameIndex, string strValueWithSetting, string strValue, int iLoop, string strConvertedValue = null)
        {
            /** 
             * Steps:
             * 1, GetReport data sheet name based on iLoop
             * 2, If no such sheet Exists, then create a new one
             * 3, Check the report Sheet is right or not(based on iLoop), otherwise, create a new Sheet
             * 4, Write Data to Special Cell
             * **/
            Logger.logBegin("SaveDataToSpecialCellComparisonMode");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR ;
                /* 1, GetReport data sheet name based on iLoop */
                if (!this.mlstDataFile.ContainsKey(this.mstrCurrentDataFileName))
                {
                    eCde = ERROR_CODE._SERVICE_ERROR_SERVER_NO_DATAFILE_CREATED_PARA_1;
                    Logger.Error("GetDataStringFromDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrCurrentDataFileName));
                    return eCde;
                }
                
                TCDataFile objDataFile = (TCDataFile)this.mlstDataFile[this.mstrCurrentDataFileName];  
                string strSheetName = string.Format("{0}_{1}", SystemConstant.CNST_DATA_RPT_SHEETNAME_PREFIX, iLoop);
                int iTargetColIndex = mGetColIndexFromComparisonSetting(strValueWithSetting, ref eCde) ;
                if (eCde!=ERROR_CODE._NO_ERROR) return eCde ;
                /**
                 * iTargetColIndex + 2
                 * Because Fields number starts from 1, and iTargetColIndex starts from 0
                 * **/
                string[] arrValue = strValue.Split('\n');
                Logger.Info("SaveDataToSpecialCellComparisonMode",string.Format("Total [{0}] rows data to insert", arrValue.Length));
                for (int i = 0; i < arrValue.Length;i++ )
                {
                    if (i==0)
                    {
                        eCde = objDataFile.SaveComparisonData(strObjNameIndex, iLoop, arrValue[0], iTargetColIndex + 2, strConvertedValue);
                    }
                    else
                    {
                        eCde = objDataFile.SaveComparisonData(string.Format("{0}_{1}", strObjNameIndex, i), iLoop, arrValue[i], iTargetColIndex + 2, strConvertedValue);
                    }
                }
                
                return eCde;
            }
            finally
            {
                Logger.logEnd("SaveDataToSpecialCellComparisonMode");
            }
        }

        private int mGetColIndexFromComparisonSetting(string strValueWithSetting, ref ERROR_CODE eCde)
        {
            Logger.logBegin("mGetColIndexFromComparisonSetting");
            try
            {
                int iPosStrt = strValueWithSetting.IndexOf(SystemConstant.CNST_ENHANCE_STORAGEMODE_COMPARISON_PREFIX);
                int iPosEnd = strValueWithSetting.IndexOf(SystemConstant.CNST_ENHANCE_STORAGEMODE_COMPARISON_CONVERT);
                if (iPosStrt>iPosEnd)
                {
                    eCde = ERROR_CODE._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_PARA_1;
                    Logger.Error("mGetColIndexFromComparisonSetting", string.Format(ERROR_INFO.GET_ERROR_STR(eCde),strValueWithSetting));
                    return -1;
                }
                Regex objReg = new Regex(@"\d{1}");
                Match objMatch = objReg.Match(strValueWithSetting);
                if (objMatch.Success)
                {
                    try
                    {
                        return int.Parse(objMatch.Value);
                    }
                    catch (Exception)
                    {
                        /** actually, it is impossible to be here **/
                        /** but in case Reg part is changed by miskate, a segment of stub code is left here **/
                        eCde = ERROR_CODE._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_NUMBER_PARA_1;
                        Logger.Error("mGetColIndexFromComparisonSetting", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strValueWithSetting));
                        return -1;
                    }                    
                }
                else
                {
                    eCde = ERROR_CODE._TEST_KEYWORD_SETTING_CAPTUREVALUE_COMPARISON_PARA_1;
                    Logger.Error("mGetColIndexFromComparisonSetting", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strValueWithSetting));
                    return -1;
                }                
            }
            finally
            {
                Logger.logEnd("mGetColIndexFromComparisonSetting");
            }
        }

        internal string GetObjectNameFromComparisonMode(string strComparisonModeValue, ref ERROR_CODE eCde)
        {
            Logger.logBegin("GetObjectNameFromComparisonMode");
            Logger.Info("GetObjectNameFromComparisonMode", TigerMarsUtil.GetParameter("strComparisonModeValue", strComparisonModeValue??"NULL"));
            
            eCde = ERROR_CODE._NO_ERROR;            
            int iPos =strComparisonModeValue==null?-1:strComparisonModeValue.LastIndexOf(";");
            if (iPos<0)
            {
                eCde = ERROR_CODE._TEST_STEP_COMPARISON_MODE_VALUE_SETTING_NO_OBJECT_PARA_1;
                Logger.Error("GetObjectNameFromComparisonMode", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strComparisonModeValue ?? "null"));
                return null;
            }
            Logger.logEnd("GetObjectNameFromComparisonMode");
            return strComparisonModeValue.Substring(iPos+1);            
            
        }

    }
}
