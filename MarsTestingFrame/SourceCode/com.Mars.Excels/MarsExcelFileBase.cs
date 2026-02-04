using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;



namespace MarsTestFrame.SourceCode.com.Mars.Excels
{
    public abstract class MarsExcelFileBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsExcelFileBase));
        protected string mstrXlsFileName; // test suite name, for xls mode, .xls is attached
        protected string mstrExtraXlsPath = null;
        #region JetDB for excel
        protected bool isContainHeader = true;
        private const string CONNECTION_STRING = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<FILENAME>;Extended Properties=\"Excel 8.0;HDR=Yes;MaxScanRows=1;IMEX=1;\";";
        private const string CONNECTION_STRING_NOHEADER = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<FILENAME>;Extended Properties=\"Excel 8.0;HDR=No;MaxScanRows=1;\";";
        private const string CONNECTION_STRING_NOHEADER_WRITE = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<FILENAME>;Extended Properties=\"Excel 8.0;HDR=No\";";
        private const string CONNECTION_STRING_WRITE = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<FILENAME>;Extended Properties=\"Excel 8.0;HDR=YES\";";
        protected OleDbConnection mobjCurrentConnection = null;
        protected List<string> mlstCurrentTables = new List<string>();

        #endregion

        #region Properties
        public string XlsFileNameWithPath
        {
            get { return this.mstrXlsFileName; }
            set { this.mstrXlsFileName = value; }
        }
        public string ExtraFilePath
        {
            get { return this.mstrExtraXlsPath; }
            set { this.mstrExtraXlsPath = value; }
        }
        #endregion //Properties end

        public virtual ERROR_CODE loadTestCase()
        {
            Logger.logBegin("loadXlsFile");
            BeforeLoadTestCase();
            ERROR_CODE eError = mAlystTestCase();
            Logger.Info("loadXlsFile", string.Format("mAlystExcleFile return ERROR_CODE [{0:X}], INFO:[{1}]", eError, ERROR_INFO.GET_ERROR_STR(eError)));
            Logger.logEnd("loadXlsFile");
            if (this.mobjCurrentConnection!=null)
             this.mobjCurrentConnection.Close();
            return eError;
        }

        protected void closeDbConnection()
        {
            if (this.mobjCurrentConnection != null)
            {
                try
                {
                    this.mobjCurrentConnection.Close();
                    //this.mobjCurrentConnection.Dispose();
                }
                finally
                {
                    this.mobjCurrentConnection = null;
                }
            }
        }
        protected void closeDBAdapter(OleDbDataAdapter objAdptr)
        {
            Logger.logBegin("closeDBAdapter");
            try
            {
                objAdptr.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error("closeDBAdapter", e.Message);
            }
            finally
            {
                Logger.logEnd("closeDBAdapter");
            }
        }

        protected abstract ERROR_CODE mAlystTestCase();
        protected virtual ConfigObjectBase mLoadDataRow2ConfigObj(DataRow objOneRow, int iRowId = -1, long lAppId = -1)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            Logger.logEnd("mLoadDataRow2ConfigObj");
            return null;
        }
        protected virtual void BeforeLoadTestCase()
        {
            return;
        }

        protected ERROR_CODE AddNewColumnToSpecialTable(string strColumnInfo, string strTableName, int iStartFrom, int iNewColCnt = 1)
        {
            Logger.logBegin("AddNewColumnToSpecialTable");
            const string cnst_altertable = "alter table {1} add  {0} varchar(32)";
            string strQuery = string.Format(cnst_altertable, strColumnInfo, strTableName);
            int iAddColCnt = (iNewColCnt <= 0 ? 1 : iNewColCnt);

            for (int i = 1; i < iAddColCnt; i++)
            {
                strQuery = string.Format("{0}, \r\n F{1} varchar(32) ", strQuery, iStartFrom + i);
            }
            //strQuery = string.Format("{0})", strQuery);

            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;

            try
            {
                if (this.mobjCurrentConnection.State != ConnectionState.Open)
                    this.mobjCurrentConnection.Open();
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._JET_DB_CONNECTOIN_OPEN_PARA_1;
                Logger.Error("AddNewColumnToSpecialTable", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message), e);
                return eCde;
            }
            using (OleDbCommand objCmd = new OleDbCommand(strQuery))
            {
                try
                {
                    objCmd.Connection = this.mobjCurrentConnection;
                    objCmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    eCde = ERROR_CODE._JET_DB_CMD_EXPAND_COLUMN_PARA_1;
                    Logger.Error("AddNewColumnToSpecialTable", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message), e);
                    this.mobjCurrentConnection.Close();
                    return eCde;
                }
            }
            this.mobjCurrentConnection.Close();
            Logger.logEnd("AddNewColumnToSpecialTable");
            return eCde;
        }

        protected int GetColumnCountOfTable(string strTableName, ref ERROR_CODE ecde)
        {
            Logger.logBegin("GetColumnCountOfTable");
            int iResult = -1;
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            try
            {
                if (this.mobjCurrentConnection.State != ConnectionState.Open)
                    this.mobjCurrentConnection.Open();
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._JET_DB_CONNECTOIN_OPEN_PARA_1;
                Logger.Error("GetColumnCountOfTable", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message), e);
                this.mobjCurrentConnection.Close();
                return iResult = -1;
            }

            string strQuery = string.Format("select * from [{0}$] where 1=2", strTableName);
#if _tigerNewColumnTest
            for (int i=4;i<6;i++){
#endif
            using (OleDbDataAdapter objAdptr = new OleDbDataAdapter(strQuery, this.mobjCurrentConnection))
            {
                DataTable dt = new DataTable();
                try
                {
                    objAdptr.Fill(dt);
                    iResult = dt.Columns.Count;
#if _tigerNewColumnTest
                        DataColumn objColumn = dt.Columns.Add();
                        objColumn.ColumnName = string.Format("F{0}", i);
                        objColumn.DefaultValue = "   ";
#endif
                }
                catch (Exception e)
                {
                    eCde = ERROR_CODE._JET_DB_GET_COLUMN_EXCEPTION_PARA_1;
                    Logger.Error("GetColumnCountOfTable", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), e.Message));
                    return -1;
                }
            }
            this.mobjCurrentConnection.Close();
#if _tigerNewColumnTest
                this.mobjCurrentConnection.Close();
                this.mobjCurrentConnection.Open();
            }
            
#endif
            Logger.logEnd("GetColumnCountOfTable");
            return iResult;
        }

        protected virtual ERROR_CODE GetSpecialTableDataToList(string strTableName, List<ConfigObjectBase> lstDes)
        {
            Logger.logBegin("GetSpecialTableDataToList");

            const string strCmdText = "select * from [{0}]";
            OleDbDataAdapter objAdptr = null;
            string strRunSql = string.Format(strCmdText, strTableName);
            if (this.mobjCurrentConnection.State != ConnectionState.Open)
                this.mobjCurrentConnection.Open();
            using (objAdptr = new OleDbDataAdapter(strRunSql, this.mobjCurrentConnection))
            {
                try
                {
                    DataTable dt = new DataTable();
                    objAdptr.Fill(dt);
                    int iRowId = 0;
                    foreach (DataRow objRow in dt.Rows)
                    {
                        ConfigObjectBase objConfigInfo = mLoadDataRow2ConfigObj(objRow, iRowId++);
                        if (objConfigInfo == null) continue;
                        lstDes.Add(objConfigInfo);
                    }
                    return ERROR_CODE._NO_ERROR;
                }
                catch (Exception e)
                {
                    ERROR_CODE eCde = ERROR_CODE._EXCEL_FILE_CANT_LOADSHEET_PARA_2;
                    Logger.Error("GetSpecialTableDataToList", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strTableName, e.Message));
                    return eCde;
                }
                finally
                {
                    //closeDBAdapter(objAdptr);
                    //objAdptr = null;
                    this.mobjCurrentConnection.Close();
                    Logger.logEnd("GetSpecialTableDataToList");
                }
            }            
        }

        protected ERROR_CODE GetWritableConnectionWithoutHead()
        {
            this.closeDbConnection();
            mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_NOHEADER_WRITE.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
            return ERROR_CODE._NO_ERROR;
        }

        protected ERROR_CODE GetWriteModeConnnection()
        {
            Logger.logBegin("GetWriteModeConnnection");
            this.closeDbConnection();
            try
            {
                if (isContainHeader)
                    mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_WRITE.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
                else
                    mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_NOHEADER_WRITE.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
                return ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("GetWriteModeConnnection", string.Format("Can't create the write mode connection. FileName:[{0}],Error:[{1}]", mstrXlsFileName, e.Message),e);
                return ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2 ;
            }
        }
        protected ERROR_CODE RecoveryReadConnection()
        {
            Logger.logBegin("GetWriteModeConnnection");
            this.closeDbConnection();
            try
            {
                if (isContainHeader)
                    mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
                else
                    mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_NOHEADER.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
                return ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("GetWriteModeConnnection", string.Format("Can't create the write mode connection. FileName:[{0}],Error:[{1}]", mstrXlsFileName, e.Message), e);
                return ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2;
            }
        }

        protected ERROR_CODE GetNormalModeConnection()
        {
            this.closeDbConnection();
            if (isContainHeader)
                mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
            else
                mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_NOHEADER.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
            return ERROR_CODE._NO_ERROR;
        }

        protected virtual ERROR_CODE GetDataTableFromExcelFile(bool isDatafromDB=false)
        {
            Logger.logBegin("GetDataTableFromExcelFile");
            /*
            this.closeDbConnection();
            if (isContainHeader)
                mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
            else
                mobjCurrentConnection = new OleDbConnection(CONNECTION_STRING_NOHEADER.Replace("<FILENAME>", (this.mstrExtraXlsPath ?? "") + mstrXlsFileName));
            */
#if _Datafrom_Database
            if (isDatafromDB)
            {
                return ERROR_CODE._NO_ERROR;
            }
#endif
            GetNormalModeConnection();             
            mlstCurrentTables.Clear();
            try
            {
                mobjCurrentConnection.Open();
                DataTable dtSchema = mobjCurrentConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                for (int i = 0; i < dtSchema.Rows.Count; i++)
                {
#if _tigerHintLog
                    Logger.Info("-----", dtSchema.Rows[i][SystemConstant.CNST_XLS_JETDB_TABLENAME].ToString());
#endif
                    this.mlstCurrentTables.Add(dtSchema.Rows[i][SystemConstant.CNST_XLS_JETDB_TABLENAME].ToString());
                }
                dtSchema.Dispose();
                return ERROR_CODE._NO_ERROR;
            }
            catch (Exception e)
            {
                Logger.Error("GetDataTableFromExcelFile", e.Message, e);
                return ERROR_CODE._BATCH_ERROR_JET_READ_EXCEPTION;
            }
            finally
            {
                this.mobjCurrentConnection.Close();
                Logger.logEnd("GetDataTableFromExcelFile");
            }
        }


#if _Datafrom_Database
        protected int CheckTableExists(string strTABLENAME,bool isDataSourceFromDB=false)
#else
        protected int CheckTableExists(string strTABLENAME)
#endif
        {
            Logger.logBegin("CheckTableExists");
#if _Datafrom_Database
            if (isDataSourceFromDB)
            {
                /// When Data source is from Database, not required to get all test projects id from database
                /// 
                mlstCurrentTables.Clear();
                mlstCurrentTables.Add(strTABLENAME);
                return (int)ERROR_CODE._NO_ERROR;
            }
#endif   
            for (int i = 0; i < mlstCurrentTables.Count; i++)
            {
                string strTbl = mlstCurrentTables.ElementAt(i);
                if (strTbl == null) continue;
                strTbl = strTbl.Replace("$", "");
                if (string.Compare(strTbl, strTABLENAME, true) == 0)
                {
                    Logger.Info("CheckTableExists", string.Format("Find Item No[{0}], Value:[{1}]", i, mlstCurrentTables.ElementAt(i)));
                    Logger.logEnd("CheckTableExists");
                    return i;
                }
            }
            Logger.logEnd("CheckTableExists");
            return -1;
        }

        /// <summary>
        /// This method checks if the user entered sheetName exists in the Schema Table
        /// </summary>
        /// <param name="sheetName">Sheet name to be verified</param>
        /// <param name="dtSchema">schema table </param>
        private static bool CheckIfSheetNameExists(string sheetName, DataTable dtSchema)
        {
            foreach (DataRow dataRow in dtSchema.Rows)
            {
                if (sheetName == dataRow["TABLE_NAME"].ToString())
                {
                    return true;
                }
            }
            return false;
        }

        public void CloseDataFile()
        {
            Logger.logBegin("CloseDataFile");
            this.closeDbConnection();
            Logger.logEnd("CloseDataFile");
        }

        protected void FlushDataToHardDisk()
        {
            Logger.logBegin("FlushDataToHardDisk");
            this.mobjCurrentConnection.Close();
            this.mobjCurrentConnection.Open();
            Logger.logEnd("FlushDataToHardDisk");
        }

        protected ERROR_CODE ExpandColumnsOnSheet(string strOrgSheetName, int iColCnt, int iStrtFrom, string strTmpSheetName = "Sheet3")
        {
            Logger.logBegin("ExpandColumnsOnSheet");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            string strColumns = "";

            for (int i = 0; i < iColCnt; i++)
            {
                if (i == 0) strColumns = "F1 ";
                else
                    strColumns = string.Format("{0}, {2} F{1} ", strColumns, i + 1, i >= iStrtFrom ? "'' AS " : "");
            }

            /** Delete Tempbook1 **/
            try
            {
                System.IO.File.Delete(@"c:\temp\tempBook1.xls");
            }
            catch (Exception e)
            {
                Logger.Error("deletingTmpFile", "can't delete it with Exception:" + e.Message);
            }

            string strCreateSql = string.Format(@"select {2} into [Excel 8.0;No;Database=C:\temp\tempBook1.xls;].[{1}]] from [{0}$]", strOrgSheetName, strTmpSheetName ?? "Sheet3", strColumns);
            eCde = RunNoneQuerySql(strCreateSql);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
            /** Drop original Table **/
            string strDrop = string.Format("drop table [{0}$]", strOrgSheetName);
            eCde = RunNoneQuerySql(strDrop);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
            /** recovery from TmpSheet **/
            strCreateSql = string.Format("select {2} into {1} from [{0}$]", strTmpSheetName ?? "Sheet3", strOrgSheetName, strColumns);
            eCde = RunNoneQuerySql(strCreateSql);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
            /** Drop tmpsheet **/
            strDrop = string.Format("DELETE FROM {0}", strTmpSheetName ?? "Sheet3");
            eCde = RunNoneQuerySql(strCreateSql);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;

            /** flush data to hard disk **/
            this.FlushDataToHardDisk();

            /** Delete Tempbook1 **/
            try
            {
                System.IO.File.Delete(@"c:\temp\tempBook1.xls");
            }
            catch (Exception e)
            {
                Logger.Error("deletingTmpFile", "can't delete it with Exception:" + e.Message);
            }
            
            Logger.logEnd("ExpandColumnsOnSheet");
            return eCde;
        }

        protected ERROR_CODE RunNoneQuerySql(string strSql2Run)
        {
#if !_Datafrom_Database
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            Logger.logBegin("RunNoneQuerySql");
            Logger.Info("RunNoneSql", string.Format("{0}", TigerMarsUtil.GetParameter("sql", strSql2Run)));
            this.mobjCurrentConnection.Close();
            try
            {
                if (this.mobjCurrentConnection.State != ConnectionState.Open)
                {
                    this.mobjCurrentConnection.Open();
                }
                using (OleDbCommand objCmd = new OleDbCommand())
                {
                    Logger.Info("RunNoneQuerySql", "Get OleDBCommand");
                    objCmd.Connection = this.mobjCurrentConnection;
                    objCmd.CommandText = strSql2Run;
                    objCmd.ExecuteNonQuery();
                    Logger.Info("RunNoneQuerySql", "after run ExecuteNonQuery");
                }
                Logger.logEnd("RunNoneQuerySql");
                //this.mobjCurrentConnection.Close();
                return eCde;
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_NON_QUERY_PARA_2;
                Logger.Error("RunNoneQuerySql", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strSql2Run, e.Message), e);
                return eCde;
            }
            finally
            {
                this.mobjCurrentConnection.Close();
                Logger.logEnd("RunNoneQuerySql");                
            }
#else
            return ERROR_CODE._NO_ERROR;
#endif
        }

        internal ERROR_CODE mCreateNewSheetWithName(string strSheetName, string[] arrstrColumns)
        {
            Logger.logBegin("mCreateNewSheetWithName");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                const string cnst_create_new_sheet = "CREATE TABLE [{0}] ({1})";
                string strFields = "";
                for (int i = 0; i < arrstrColumns.Length; i++)
                {
                    if (i == 0)
                        strFields = arrstrColumns[0] + " varchar(240) ";
                    else
                        strFields = string.Format("{1},{0} varchar(240)", arrstrColumns[i], strFields);
                }
                string strSqlCreate = string.Format(cnst_create_new_sheet, strSheetName, strFields);
                return eCde = this.RunNoneQuerySql(strSqlCreate);
            }
            catch (Exception e)
            {
                string strError = string.Format("Can't create a new Table [{0}] with exception:[{1}]", strSheetName, e.Message);
                Logger.Error("mCreateNewSheetWithName", strError,e);
                return ERROR_CODE._SERVICE_ERROR_CLIENT_UNKNOW;
            }
            finally
            {
                Logger.logEnd("mCreateNewSheetWithName");
            }
        }

        internal bool IsRowExistsOnExcel(string strObjNameIndex, string strFieldName, ref ERROR_CODE eCde, string strSheetName = "Sheet1")
        {
            Logger.logBegin("IsRowExistsOnExcel");
            const string cnst_sql_select = "select * from [{0}$] where {1}='{2}'";
            string strSelect = string.Format(cnst_sql_select, strSheetName == null ? "Sheet1" : strSheetName, strFieldName, strObjNameIndex);
            eCde = ERROR_CODE._NO_ERROR;
            if (this.mobjCurrentConnection.State!= ConnectionState.Open)
                this.mobjCurrentConnection.Open();
            try
            {
                using (OleDbCommand objCmd = new OleDbCommand(strSelect, this.mobjCurrentConnection))
                {
                    OleDbDataReader objReader = objCmd.ExecuteReader();
                    while (objReader.Read())
                    {
                        Logger.Info("IsRowExistsOnExcel", "Exists");
                        return true;
                    }
                    objReader.Dispose();
                    objReader = null;
                }
                Logger.Info("IsRowExistsOnExcel", "Nothing found");
                return false;
            }
            catch (Exception e)
            {
                eCde = ERROR_CODE._JET_DB_EXCEPTION_WHEN_RUN_QUERY_PARA_2;
                Logger.Error("IsRowExistsOnExcel", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strSelect, e.Message), e);
                return false;
            }
            finally
            {
                this.mobjCurrentConnection.Close();
                Logger.logEnd("IsRowExistsOnExcel");
            }

        }
        
        protected bool CreateANewRow(string[] arrValues, string[] arrFields, ref ERROR_CODE eCde, string strSheetName = "Sheet1")
        {
            Logger.logBegin("CreateANewRow");
            eCde = ERROR_CODE._NO_ERROR;
            try
            {
                if (arrValues.Length != arrFields.Length)
                {
                    eCde = ERROR_CODE._JET_DB_FIELDNAMES_DNTMATCH_VALUES_PARA_2;
                    Logger.Error("CreateANewRow", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), arrFields.Length, arrValues.Length));
                    return false;
                }
                string strFiedls = "", strValues = "";
                for (int i = 0; i < arrValues.Length; i++)
                {
                    if (i == 0)
                    {
                        strFiedls = arrFields[0];
                        strValues = string.Format("'{0}'", arrValues[0]);
                    }
                    else
                    {
                        strFiedls = string.Format("{0}, {1}", strFiedls, arrFields[i]);
                        strValues = string.Format("{0}, '{1}'", strValues, arrValues[i]);
                    }
                }
                const string cnst_insert = "insert into [{0}$] ({1}) values ({2})";
                string strInsert = string.Format(cnst_insert, strSheetName ?? "Sheet1", strFiedls, strValues);
                eCde = this.RunNoneQuerySql(strInsert);
                return eCde == ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("CreateANewRow");
            }
        }

        protected bool UpdateValue(string[] arrFields, string[] arrValues, string[] arrWhereFields, string[] arrWhereValues, ref ERROR_CODE eCde, string strSheetName)
        {
            Logger.logBegin("UpdateValue");
            eCde = ERROR_CODE._NO_ERROR;
            try
            {
                if ((arrValues.Length != arrFields.Length) || (arrWhereFields.Length != arrWhereValues.Length))
                {
                    eCde = ERROR_CODE._JET_DB_FIELDNAMES_DNTMATCH_VALUES_PARA_2;
                    Logger.Error("UpdateValue", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), arrFields.Length, arrValues.Length));
                    return false;
                }
                string strSets = "", strWhere = "";
                for (int i = 0; i < arrFields.Length; i++)
                {
                    if (i == 0)
                    {
                        strSets = string.Format("{0}='{1}'", arrFields[0], arrValues[0]);
                    }
                    else
                    {
                        strSets = string.Format("{2},{0}='{1}'", arrFields[i], arrValues[i], strSets);
                    }
                }
                for (int i = 0; i < arrWhereFields.Length; i++)
                {
                    if (i == 0)
                    {
                        strWhere = string.Format("{0}='{1}'", arrWhereFields[0], arrWhereValues[0]);
                    }
                    else
                    {
                        strWhere = string.Format("{2} and {0}='{1}'", arrWhereFields[i], arrWhereValues[i], strWhere);
                    }
                }
                string strUpdate = string.Format("update [{0}$] set {1} {2}", strSheetName ?? "Sheet1", strSets, string.IsNullOrEmpty(strWhere) ? "" : (" where " + strWhere));
                eCde = this.RunNoneQuerySql(strUpdate);
                return eCde == ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("UpdateValue");
            }


        }
    }
}

