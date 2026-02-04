extern alias clientWCF;
using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.com.Mars.Excels;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.TCDataSource
{
    public class TCDataFile : MarsExcelFileBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TCDataFile));
        protected const string CNST_DATA_SHEETNAME = "Sheet1";


        #region member variables.
        protected string mstrCurrentDataTableName = "";
        protected List<ConfigObjectBase> mlstDataObject = new List<ConfigObjectBase>();
        #endregion

        protected virtual void msetCurrentDataTableName()
        {
            this.mstrCurrentDataTableName = CNST_DATA_SHEETNAME;
        }

        public TCDataFile()
            : base()
        {
            isContainHeader = false;
            msetCurrentDataTableName();
        }

        protected override ERROR_CODE mAlystTestCase()
        {
            Logger.logBegin("mAlystExcleFile");
            ERROR_CODE eCode = this.GetDataTableFromExcelFile();
            if (eCode != ERROR_CODE._NO_ERROR)
            {
                Logger.Info("mAlystExcelFile", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_LOAD_TABLENAMES), eCode));
                return eCode;
            }

            int iItm = this.CheckTableExists(this.mstrCurrentDataTableName);
            if (iItm < 0)
            {
                eCode = ERROR_CODE._TCDATA_NO_DATA_SHEET;
                Logger.Error("mAlystExcleFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCode), this.mstrCurrentDataTableName));
                return eCode;
            }

            /** load data **/
            try
            {
                eCode = this.GetSpecialTableDataToList(this.mlstCurrentTables[iItm], mlstDataObject);
            }
            catch (MarsExceptions em)
            {
                return (ERROR_CODE)em.ErrorId;
            }
            catch (Exception e)
            {
                Logger.Error("mAlystExcleFile", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_EXCEPTION_LOADDATA), e.Message), e);
                throw new MarsExceptions((int)ERROR_CODE._TCDATA_EXCEPTION_LOADDATA, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_EXCEPTION_LOADDATA), e.Message));
            }
            if (eCode != ERROR_CODE._NO_ERROR)
            {
                return eCode;
            }
            Logger.logEnd("mAlystExcleFile");
            return ERROR_CODE._NO_ERROR;
        }

        private bool mCheckObjectExists(string strObjectName)
        {
            Logger.logBegin("mCheckObjectExists");
            try
            {
                foreach (ConfigObjectBase objItem in this.mlstDataObject)
                {
                    if (objItem == null) continue;
                    TestDataObject objDtaObj = (TestDataObject)objItem;
                    if (string.Compare(objDtaObj.DataObjectName, strObjectName, true) == 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                Logger.logEnd("mCheckObjectExists");
            }
        }

        internal ERROR_CODE UpdateDataCache(string strObjectName, int iLoopId, string strData2Update, string strSheetName = CNST_DATA_SHEETNAME)
        {
            Logger.logBegin("UpdateDataCache");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (iLoopId < 0)
            {
                eCde = ERROR_CODE._TCDATA_DATA_UPDATECACHE_LOOPID_LESS_0_PARA_2;
                Logger.Error("UpdateDataCache", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId, strObjectName));
                return eCde;
            }
            foreach (ConfigObjectBase objItem in this.mlstDataObject)
            {
                if (objItem == null) continue;
                TestDataObject objData = (TestDataObject)objItem;
                if (string.Compare(objData.DataObjectName, strObjectName, true) == 0)
                {
                    if (iLoopId >= objData.TestData.Count)
                    {
                        eCde = ERROR_CODE._TCDATA_DATA_UPDATECACHE_LOOPID_GREATER_PARA_3;
                        Logger.Error("UpdateDataCache", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId, strObjectName, objData.TestData.Count));
                        return eCde;
                    }
                    objData.TestData[iLoopId] = strData2Update;
                    return ERROR_CODE._NO_ERROR;
                }
            }
            eCde = ERROR_CODE._TCDATA_DATA_UPDATECACHE_NO_OBJECTINDEX_PARA_1;
            Logger.Error("UpdateDataCache", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strObjectName));
            Logger.logEnd("UpdateDataCache");
            return eCde;
        }


        protected override ConfigObjectBase mLoadDataRow2ConfigObj(System.Data.DataRow objOneRow, int iRowId = -1, long lAppId = -1)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            if (objOneRow == null) return null;
            if (objOneRow.Table.Columns.Count <= 0)
            {
                throw new MarsExceptions((int)ERROR_CODE._TCDATA_NO_DATA_FIND, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_NO_DATA_FIND), this.mstrCurrentDataTableName));
            }
            TestDataObject objOneData = new TestDataObject();
            try
            {
                objOneData.DataObjectName = objOneRow[0].ToString();
                for (int i = 1; i < objOneRow.Table.Columns.Count; i++)
                {
                    objOneData.TestData.Add(objOneRow[i].ToString());
                }
                return objOneData;
            }
            catch (Exception e)
            {
                throw new MarsExceptions((int)ERROR_CODE._TCDATA_EXCEPTION_READFROM_ONEROW, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_EXCEPTION_READFROM_ONEROW), e.Message));
            }
            finally
            {
                Logger.logEnd("mLoadDataRow2ConfigObj");
            }
        }
#if !_Datafrom_Database
        public virtual ERROR_CODE GetOneCellValueFromData(int iLoopId, string strCellIdx, ref string strResult, string strSheetName = CNST_DATA_SHEETNAME)
#else
        public virtual ERROR_CODE GetOneCellValueFromData(int iLoopId, string strCellIdx, ref string strResult, int iStepId=-1, string strSheetName = CNST_DATA_SHEETNAME)
#endif
        {
            Logger.logBegin("GetOneCellValueFromData");

            const string cnst_SqlQuery = "select * from [{0}$] where F1='{1}'";
            string strSqlQuery = string.Format(cnst_SqlQuery, string.IsNullOrEmpty(strSheetName) ? CNST_DATA_SHEETNAME : strSheetName, strCellIdx);
            //strSqlQuery = string.Format("select * from [{0}$] where RESULT_OBJECT='{1}'", string.IsNullOrEmpty(strSheetName) ? CNST_DATA_SHEETNAME : strSheetName, strCellIdx);
            OleDbDataAdapter objAdptr = null;
            DataTable dt = new DataTable();
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (this.mobjCurrentConnection.State != ConnectionState.Open)
                this.mobjCurrentConnection.Open();
            try
            {
                using (objAdptr = new OleDbDataAdapter(strSqlQuery, this.mobjCurrentConnection))
                {
                    objAdptr.Fill(dt);
                    foreach (DataRow objRow in dt.Rows)
                    {
                        TestDataObject objData = (TestDataObject)mLoadDataRow2ConfigObj(objRow);
                        strResult = objData.GetSpecialColumnData(iLoopId, ref eCde);
                        if (eCde == ERROR_CODE._NO_ERROR)
                            Logger.Info("GetOneCellValueFromData", string.Format("Find result from DataFile:{0}-{1}-[{2}]", TigerMarsUtil.GetParameter("strCellIdx", strCellIdx), TigerMarsUtil.GetParameter("LoopId", iLoopId.ToString()), TigerMarsUtil.GetParameter("Value", strResult)));
                        return eCde;
                    }
                }
                eCde = ERROR_CODE._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2;
                Logger.Error("GetOneCellValueFromData", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strCellIdx, iLoopId));
                return eCde;
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._TCDATA_DATA_CELL_EXCEPTION_PARA_3;
                Logger.Error("GetOneCellValueFromData", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strCellIdx, iLoopId.ToString(), e.Message), e);
                return eCde;
            }
            finally
            {
                objAdptr.Dispose();
                this.mobjCurrentConnection.Close();
                Logger.logEnd("GetOneCellValueFromData");
            }
        }

        internal ERROR_CODE UpdateDataForDataFile(string strObjectName, int iLoopId, string strData2Update, string strSheetName = CNST_DATA_SHEETNAME)
        {
            Logger.logBegin("UpdateDataForDataFile");

            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
#if _tigerDelFirstRow
            !!!!! for Ole DB driver for Excel, no delete from is supported. !!!!!
            this.mobjCurrentConnection.Close();
            this.mobjCurrentConnection.Open();
            string strDel = "insert into [Sheet1$] values ('a','b','c')";
            eCde = base.RunNoneQuerySql(strDel);
            this.mobjCurrentConnection.Close();
            this.mobjCurrentConnection.Open();
            strDel = "delete from [Sheet1$] where ObjectName='SUMMIT_LOGIN_ENVIRONMENT'";
            eCde = base.RunNoneQuerySql(strDel);
            !!!!! And All data file should include Header !!!!!
            !!!!! To support that, header names could be ObjectName, T1,T2..... !!!!!
#endif

            /** get the column count */
            int iColCnt = GetTableColCnt();
            base.FlushDataToHardDisk();
            if (iColCnt < (iLoopId + 1))
            {
                /** expand Table ---- !!!! Not supported by driver !!!! **/
                eCde = ERROR_CODE._JET_DB_NO_EXPAND_COLUMN_SUPPORT_PARA_1;
                Logger.Error("UpdateDataForDataFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), iLoopId));
                return eCde;
            
            }

            /** check whether the object name exists on the data file **/
            bool isObjectExists = mCheckObjectExists(strObjectName);
            if (!isObjectExists)
            {
                eCde = AddNewRowToFile(strObjectName);
                if (eCde != ERROR_CODE._NO_ERROR) return eCde;
                /** update cache **/
                InsertObjectNameToCache(strObjectName);
            }
            //this.GetWriteModeConnnection();
            const string cnst_SqlUpdate = "update [{0}$] set F{1}='{3}' where F1='{2}'";
            string strSqlUpdate = string.Format(cnst_SqlUpdate, string.IsNullOrEmpty(strSheetName) ? CNST_DATA_SHEETNAME : strSheetName, iLoopId + 1, strObjectName, strData2Update);
            
            eCde = this.RunNoneQuerySql(strSqlUpdate);

            Logger.logEnd("UpdateDataForDataFile");
            return eCde;

        }

        private void InsertObjectNameToCache(string strObjectName)
        {
            Logger.logBegin("InsertObjectNameToCache");
            try
            {
                TestDataObject objNewData = new TestDataObject();
                objNewData.DataObjectName = strObjectName;
                this.mlstDataObject.Add(objNewData);
            }
            finally
            {
                Logger.logEnd("InsertObjectNameToCache");
            }
        }

        private ERROR_CODE AddNewRowToFile(string strObjectName, string strTableName = CNST_DATA_SHEETNAME)
        {
            Logger.logBegin("AddNewRowToFile");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            const string cnst_insert_TmpObject = "insert into [{0}$] ({1}) values ('{2}')";
            try
            {
                string strInsert = string.Format(cnst_insert_TmpObject, strTableName ?? CNST_DATA_SHEETNAME, SystemConstant.CNST_DATA_FIELD_OBJECT, strObjectName);
                if (!this.isContainHeader)
                {
                    strInsert = string.Format(cnst_insert_TmpObject, strTableName ?? CNST_DATA_SHEETNAME, "F1", strObjectName);
                }
                eCde = base.RunNoneQuerySql(strInsert);
                base.FlushDataToHardDisk();
                return eCde;
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._TCDATA_DATA_INSERT_OBJECT_EXCEPTION_PARA_3;
                Logger.Error("AddNewRowToFile", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrXlsFileName, strObjectName, e.Message), e);
                return eCde;
            }
            finally
            {
                Logger.logEnd("AddNewRowToFile");
            }
        }

        private int GetTableColCnt(string strSheetName = CNST_DATA_SHEETNAME)
        {
            Logger.logBegin("GetTableColCnt");
            string strTableName = strSheetName ?? CNST_DATA_SHEETNAME;
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            int iColumnCnt = this.GetColumnCountOfTable(strTableName, ref eCde);
            if (eCde != ERROR_CODE._NO_ERROR) return -1;

            Logger.logEnd("GetTableColCnt");
            return iColumnCnt;
        }

        internal int GetColomnCount()
        {
            Logger.logBegin("GetColomnCount");
            int iMaxColumnCnt = Int16.MinValue;
            foreach (ConfigObjectBase objItem in this.mlstDataObject)
            {
                if (objItem == null) continue;
                TestDataObject objData = (TestDataObject)objItem;
                iMaxColumnCnt = Math.Max(iMaxColumnCnt, objData.GetColumnCount());
            }
            Logger.logEnd("GetColomnCount");
            return iMaxColumnCnt;
        }

        internal ERROR_CODE SaveComparisonData(string strObjNameIndex, int iLoop, string strValue, int iTargetColIndex, string strConvertedValue = null)
        {
            Logger.logBegin("SaveComparisonData");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                string strSheetName = string.Format("{0}_{1}", SystemConstant.CNST_DATA_RPT_SHEETNAME_PREFIX, iLoop);
                bool isSheetExistsInExcelFile = mSheetExits(strSheetName, ref eCde);
                if (eCde != ERROR_CODE._NO_ERROR) return eCde;

                if (!isSheetExistsInExcelFile)
                {
                    /** create a new sheet with name **/
                    eCde = base.mCreateNewSheetWithName(strSheetName, new string[] { SystemConstant.CNST_DATA_RPT_DEFAULT_FIELDNAME_OBJECT ,
                        SystemConstant.CNST_DATA_RPT_DEFAULT_FIELDNAME_APPLICATION_1,
                        SystemConstant.CNST_DATA_RPT_DEFAULT_FIELDNAME_APPLICATION_2,
                        SystemConstant.CNST_DATA_RPT_DEFAULT_FIELDNAME_RESULT});
                    if (eCde != ERROR_CODE._NO_ERROR) return eCde;
                    this.FlushDataToHardDisk();
                }

                /** to check whether the object name exists **/
                bool isRowExists = base.IsRowExistsOnExcel(strObjNameIndex, "F1", ref eCde, strSheetName);
                if (eCde != ERROR_CODE._NO_ERROR) return eCde;
                bool isDone = false;
                if (!isRowExists)
                {
                    /** create a new row **/
                    isDone = base.CreateANewRow(new string[] { strObjNameIndex }, new string[] { "F1" }, ref eCde, strSheetName);
                    if (eCde != ERROR_CODE._NO_ERROR) return eCde;
                }
                /** update value **/

                isDone = base.UpdateValue(
                    new string[] { string.Format("F{0}", iTargetColIndex) },
                    new string[] { strValue },
                    new string[] { "F1" }, new string[] { strObjNameIndex },
                    ref eCde, strSheetName
                );

                if (!isDone)
                {
                    Logger.Error("SaveComparisonData", "Can't save data to Excel!");
                }
                this.FlushDataToHardDisk();

                /** update comparison result **/
                if (iTargetColIndex != 2)
                {
                    isDone = UpdateComparisonResultValue(strSheetName, strObjNameIndex, ref eCde);
                    this.FlushDataToHardDisk();
                }

                return eCde;
            }
            catch (Exception e)
            {
                //Exceptions occur when Update to Cell. Cell Index:[{0}], Column Index:[{1}] Exceptions:[{2}]
                Logger.Error("SaveComparisonData", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_DATA_UPDATE_EXCEPTION_PARA_3), strObjNameIndex, iTargetColIndex, e.Message),e);
                return ERROR_CODE._TCDATA_DATA_UPDATE_EXCEPTION_PARA_3;
            }
            finally
            {
                Logger.logEnd("SaveComparisonData");
            }
        }

        private bool UpdateComparisonResultValue(string strSheetName, string strObjNameIdx, ref ERROR_CODE eCde)
        {
            eCde = ERROR_CODE._NO_ERROR;
            string strSqlFormat = "update [{0}$] set F4='{1}' where F2=F3 and F1='{2}'";
            string strSql = string.Format(strSqlFormat, strSheetName, "TRUE", strObjNameIdx);
            eCde = this.RunNoneQuerySql(strSql);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("UpdateComparisonResultValue", string.Format("Can't update the result cell:\r\n\tsheetName-[{0}]\r\n\tObjectNameIdx:[{1}]\r\n\tErrorCode:[{2:X}]", strSheetName, strObjNameIdx, eCde));
                return false;
            }
            strSqlFormat = "update [{0}$] set F4='{1}' where F2<>F3 and F1='{2}'";
            strSql = string.Format(strSqlFormat, strSheetName, "FALSE", strObjNameIdx);
            eCde = this.RunNoneQuerySql(strSql);
            if (eCde != ERROR_CODE._NO_ERROR)
            {
                Logger.Error("UpdateComparisonResultValue", string.Format("Can't update the result cell:\r\n\tsheetName-[{0}]\r\n\tObjectNameIdx:[{1}]\r\n\tErrorCode:[{2:X}]", strSheetName, strObjNameIdx, eCde));
                return false;
            }
            return true;
        }

        private bool mSheetExits(string strSheetName, ref ERROR_CODE eCde)
        {
            Logger.logBegin("mSheetExits");
            try
            {
                eCde = ERROR_CODE._NO_ERROR;
                this.FlushDataToHardDisk();
                DataTable dtSchema = mobjCurrentConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                for (int i = 0; i < dtSchema.Rows.Count; i++)
                {
                    if (string.Compare(strSheetName, dtSchema.Rows[i][SystemConstant.CNST_XLS_JETDB_TABLENAME].ToString().Replace("$", ""), true) == 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("GetDataTableFromExcelFile", e.Message, e);
                eCde = ERROR_CODE._BATCH_ERROR_JET_READ_EXCEPTION;
                return false;
            }
            finally
            {
                this.mobjCurrentConnection.Close();
                Logger.logEnd("mSheetExits");
            }
        }


    }
}
