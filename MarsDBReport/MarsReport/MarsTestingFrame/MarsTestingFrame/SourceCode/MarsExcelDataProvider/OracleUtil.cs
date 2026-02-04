extern alias clientWCF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
#if Managed_Driver
using Oracle.ManagedDataAccess.Client;
#else
using Oracle.DataAccess.Client;
#endif
using System.Data;
using System.Data.Common;
#if _Datafrom_Database
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#endif

namespace MarsExcelDataProvider
{
    public class OracleUtil
    {
        public static string ConnString;
#if _Datafrom_Database
        private static MLogger Logger = MLogger.GetLogger(typeof(OracleUtil));
#endif
        public static void oracleTest()
        {
            using (OracleConnection sqlConnection = new OracleConnection(ConnString))
            {
                OracleCommand command = new OracleCommand("select * from TEST_CASE_VIEW ", sqlConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(command);
                OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable dt = ds.Tables[0];
                Console.WriteLine("\ndbSource ready for access");
            }
        }

        public static DataSet GenTCDataSet(string testProjectRequested, string testSuiteRequested, List<string> tcList, bool needObjectID)
        {
            DataSet ds = new DataSet();

            string objectIdSnippet = "";

            if (needObjectID)
                objectIdSnippet = " object_id, ";

            foreach (string tc in tcList)
            {
                string sqlQuery = " select key_word_name as keyword,  " +
                                  "        object_happy_name as object,  " +
                                  objectIdSnippet +
                                  "        column_row_setting as row_column " +
                                  " from TEST_CASE_VIEW  " +
                                  " where project_name = '" + testProjectRequested + "'" +
                                  " and test_suite_name = '" + testSuiteRequested + "'" +
                                  " and test_case_name = '" + tc + "'";

                DataTable dt = GetDataTable(sqlQuery);
                Console.WriteLine("tc = " + tc);
                dt.TableName = tc;

                // modify column names
                dt.Columns["KEYWORD"].ColumnName = "keyword";
                dt.Columns["OBJECT"].ColumnName = "object";
                dt.Columns["ROW_COLUMN"].ColumnName = "row_column";

                // add columns
                dt.Columns.Add("value");
                dt.Columns.Add("Comment");

                ds.Tables.Add(dt);
            }

            return ds;
        }

        public static  DataTable GetDataTable(string sqlQuery)
        {
            DataTable dt = null;
            using (OracleConnection sqlConnection = new OracleConnection(ConnString))
            {
                Console.WriteLine(sqlQuery);

                OracleCommand command = new OracleCommand(sqlQuery, sqlConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(command);
                OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
                ds.Tables.Remove(dt);
            }
            return dt;
        }

        public static  List<string> GetTCList(string projectRequested, string testSuiteRequested)
        {
            List<string> tcList;

            using (OracleConnection sqlConnection = new OracleConnection(ConnString))
            {
                string selectString = " select distinct test_case_name " +
                                      " from TEST_CASE_VIEW " +
                                      " where project_name = '" + projectRequested + "'" +
                                      " and test_suite_name = '" + testSuiteRequested + "'";

                Console.WriteLine(selectString);

                OracleCommand command = new OracleCommand(selectString, sqlConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(command);
                OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                DataTable dt = ds.Tables[0];
                tcList = dt.AsEnumerable().Select(x => x[0].ToString()).ToList();


                foreach (string e in tcList)
                    Console.WriteLine(e);
            }

            return tcList;
        }


        private static DataSet RunQuery(DbConnection objCnn, DbCommand objCmd, DbDataAdapter objDtaAdpt, DbCommandBuilder objCmdB  )
        {
            try
            {

                return null;
            }
            catch (Exception)
            {
                return null;
                
            }
        }
          
        public static int GetMaxLoopId(string testProjectRequested, string testSuiteRequested)
        {
            int loopId = 0;

            string sqlQuery = " select max(loop_id) as max_loop_id " +
                                  " from TEST_CASE_VIEW   " +
                                  " where project_name = '" + testProjectRequested + "'" +
                                  " and test_suite_name = '" + testSuiteRequested + "'" ;

            DataTable dt = OracleUtil.GetDataTable(sqlQuery);
            
            string ss = dt.Rows[0].ItemArray[0].ToString();
            Int32.TryParse(ss, out loopId);
            return loopId;
        }

        public static DataTable GetDataSheetTable(string testProjectRequested, string testSuiteRequested)
        {
            int maxLoopId = OracleUtil.GetMaxLoopId(testProjectRequested, testSuiteRequested);
            string loopIdSet = GetLoopIdSet(maxLoopId);

            string sqlQuery =
                        " select * from  " +
                        " ( " +
                        "     select object_happy_name as object, data_value as data, loop_id  " +
                        "     from TEST_CASE_VIEW   " +
                        "     where project_name = '" + testProjectRequested + "'" +
                        "     and test_suite_name = '" + testSuiteRequested + "'" +

                        " ) " +
                        " pivot  " +
                        " ( " +
                        "     max(data) " +
                        "     for loop_id in (" + loopIdSet + ")" +
                        " ) ";

            Console.WriteLine(sqlQuery);

            DataTable dt = OracleUtil.GetDataTable(sqlQuery);

            dt.TableName = "Sheet1";

            return dt;
        }

        public static DataTable GetObjectDataTable()
        {
            string sqlQuery = " select * from test_object_view  "; 
            DataTable dt = OracleUtil.GetDataTable(sqlQuery);

            dt.TableName = "Object";
            return dt;
        }

        public static string GetLoopIdSet(int maxLoopId)
        {
            string loopIdSet = "";

            for (int i = 1; i < maxLoopId; i++)
            {
                loopIdSet += i + " as Data" + i + ",";
            }
            loopIdSet += maxLoopId + " as Data" + maxLoopId;

            return loopIdSet;

        }

#if _Datafrom_Database
        /// <summary>
        /// Notice, all methods listing here needs Exception clause outside to catch 
        /// Exception information
        /// </summary>
        private DbConnection CurrentConnection = null;
        private DbConnection GetDbConnection()
        {
            if (CurrentConnection==null)
            {
                CurrentConnection = new OracleConnection(ConnString);
            }
            CurrentConnection.Open();
            return CurrentConnection;
        }

        private const string CNST_TABLENAME_V_DASHBOARD_TEST_FULLVISION = "V_DASHBOARD_TEST_FULLVISION";

        internal DataSet GenDashboardFullViewByParas(string[] arrFields, object[] arrValues)
        {
            Logger.Info("GenDashboardFullViewByParas", string.Format("Fields:[{0}] \r\n[{1}]", arrFields, arrValues));
            return GenRecordsByParasAndTableName(CNST_TABLENAME_V_DASHBOARD_TEST_FULLVISION, arrFields, arrValues, "ORDER BY DASHBOARD_ID, RUN_ORDER");
        }

        private DataSet GenRecordsByParasAndTableName(string strTableName, string[] arrFields, object[] arrValues, string strOrderBy)
        {
            string strSlctFrom = string.Format("Select * FROM {0}", strTableName);
            string strWhereClause = "", strSub;
            string strSql = "";
            List<object> lstPara = new List<object>();
            if (arrFields == null)
            {
                /// Select All records
                /// 
                strSql = strSlctFrom;
            }
            else
            {
                if (arrFields.Length != (arrValues==null?-1: arrValues.Length))
                {
                    Logger.Error("GenRecordsByParasAndTableName", string.Format("Fields length [{0}] desn't match Values [{1}] count", arrFields.Length, arrValues==null?"null":arrValues.Length+""));
                    return null;
                }
                int iL;
                string strSubPara;
                for(int i=0;i<arrFields.Length;i++)
                {
                    object ov = arrValues[i];
                    if (ov == null) continue;
                    if (ov.GetType().IsArray)
                    {
                        strSub = "";
                        iL = 0;
                        List<string> lstSub = new List<string>();
                        foreach (object v in (Array)ov)
                        {
                            if (v == null) continue;
                            if (string.IsNullOrEmpty(strSub))
                            {
                                strSubPara = string.Format("{0}{1}", arrFields[i], iL);
                                strSub = string.Format("{0}=:{1}", arrFields[i], strSubPara);
                            }
                            else
                            {
                                strSubPara = string.Format("{0}{1}", arrFields[i], iL);
                                strSub = string.Format("{0} or {1}=:{2}", strSub, arrFields[i], strSubPara);
                            }
                            iL++;
                            lstSub.Add(strSubPara);
                        }
                        lstPara.Add(lstSub);
                    }
                    else
                    {
                        strSub = string.Format("{0}=:{0}", arrFields[i]);
                        lstPara.Add(arrFields[i]);
                    }
                    if (string.IsNullOrEmpty(strWhereClause))
                        strWhereClause = string.Format(" ({0}) ", strSub);
                    else strWhereClause = string.Format("{0} and ({1})", strWhereClause, strSub);
                }
                strSql = string.Format("{0} \r\n {1} \r\n {2} \r\n{3}", strSlctFrom, string.IsNullOrEmpty(strWhereClause)?"":" WHERE ", strWhereClause, string.IsNullOrEmpty(strOrderBy)?"": strOrderBy);
            }
            return GetDataSetViaSqlParasEx(this.CurrentConnection, strSql, lstPara, arrValues);

        }

        

        protected string GetParameters(string[] arrv)
        {
            /// This function should be overrided if the database is not Oralce
            /// 
            string strCndtn = "";
            foreach (string strItm in arrv)
            {
                if (string.IsNullOrEmpty(strItm)) continue;
                if (string.IsNullOrEmpty(strCndtn))
                    strCndtn = ":" + strItm;
                else
                    strCndtn = string.Format("{0} and {1}=:{1}", strCndtn, strItm);
            }
            return strCndtn;
        }

        internal DataSet GenProjectAppsByAppNamesProjectNames(string strAppName=null, string strProjectName = null)
        {
            Logger.logBegin("GenProjectAppsByAppNamesProjectNames");

            string strSql = "Select * from TEST_PROJECT_VIEW ";
            string strOrder = "ORDER BY PROJECT_ID,PROJECT_NAME";
            if (this.CurrentConnection == null)
                GetDbConnection();
            string[] arrCF = null;
            string strCondition = GetParameters(arrCF =new string[] { strAppName==null? null : "APP_SHORT_NAME", strProjectName==null?null: "PROJECT_NAME" }); 
            try
            {
                if (string.IsNullOrEmpty(strCondition))
                {
                    strSql = string.Format("{0}\r\n {1}", strSql, strOrder);
                    return GetDataSetViaSqlParas(this.CurrentConnection, strSql, null, null);
                }
                else
                {
                    strSql = string.Format("{0}\r\n where {1} \r\n {2}", strSql, strCondition, strOrder);
                    return GetDataSetViaSqlParas(this.CurrentConnection, strSql, arrCF, new string[] { strAppName, strProjectName });
                }
            }
            finally
            {
                Logger.logEnd("GenProjectAppsByAppNamesProjectNames");
            }
        }

        internal DataSet GenProjects(string strProjectName)
        {
            string strSql = "Select * from T_TEST_PROJECT ";
            string strOrder = "ORDER BY PROJECT_NAME";
            if (this.CurrentConnection == null)
                GetDbConnection();
            if (string.IsNullOrEmpty(strProjectName))
            {
                strSql = string.Format("{0}\r\n{1}",strSql,strOrder);
                return GetDataSetViaSqlParas(this.CurrentConnection,strSql,null, null);
            }
            else
            {
                string[] arrPara = new string[] { "PROJECT_NAME" };
                string strCondition = GetParameters(arrPara);
                if (string.IsNullOrEmpty(strCondition))
                {
                    Logger.Warnning("GenProjects", string.Format("no condition clause returns for getting data with [{0}].Default condition clause will use", strProjectName));
                    strSql = string.Format("{0}\r\n{1}", strSql, strOrder);
                    return GetDataSetViaSqlParas(this.CurrentConnection, strSql, null, null);
                }
                else
                {
                    strSql = string.Format("{0}\r\n where {1} \r\n{2}", strSql, strCondition, strOrder);
                    return GetDataSetViaSqlParas(this.CurrentConnection, strSql, arrPara, new string[] { strProjectName});
                }
            }
        }

      

        internal DataSet GenObjects(string strAppShortName = null)
        {
            string strSql = "Select * from V_OBJECT_APPS ";
            string strOrder = "order by OBJECT_TYPE,OBJECT_HAPPY_NAME";
            if (this.CurrentConnection == null)
                GetDbConnection();
            if (strAppShortName == null)
            {
                strSql = string.Format("{0}\r\n{1}", strSql, strOrder);
                return GetDataSetViaSqlParas(this.CurrentConnection, strSql, null, null);
            }
            else
            {
                strSql = string.Format("{0} where APP_SHORT_NAME=:shortName \r\n{1}", strSql, strOrder);
                return GetDataSetViaSqlParas(this.CurrentConnection, strSql, new string[] { "shortName" }, new object[] { strAppShortName });
            }

            
            //using (DbCommand dbCmmd = new OracleCommand(strSql, (OracleConnection)CurrentConnection))
            //{
                
            //    if (isParaMode)
            //    {
            //        dbCmmd.Parameters.Add(new OracleParameter("shortName", strAppShortName));
            //    }
            //    OracleDataAdapter adapter = new OracleDataAdapter((OracleCommand)dbCmmd);
            //    OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
            //    DataSet ds = new DataSet();
            //    adapter.Fill(ds);
            //    return ds;
            //}
            
        }
        internal DataSet GetDataSetViaSqlParas(DbConnection dbCnn, string strSql, string[] arrParas, object[] arrValues) 
        {
            int iLenP = arrParas==null?0:arrParas.Length, iLenV = arrValues == null ? 0 : arrValues.Length;
            Logger.Info("GetDataSetViaSqlParas",string.Format("Sql to execute :[{0}]\r\n paramters:{1}",strSql, arrValues));
            if (iLenP!=iLenV)
            {
                throw new System.ArgumentException(string.Format("GetDataSetViaSqlParas, parameters  are wrong.\r\n[arrParas:{0}]-[arrValues:{1}]\r\nSql:{2}", arrParas, arrValues, strSql));
            }
            using (DbCommand dbCmmd = new OracleCommand(strSql, (OracleConnection)dbCnn))
            {
                for (int i=0;i<iLenP;i++)
                {
                    if (string.IsNullOrEmpty(arrParas[i])) continue;
                    dbCmmd.Parameters.Add(new OracleParameter(arrParas[i], arrValues[i]));
                }
                OracleDataAdapter adapter = new OracleDataAdapter((OracleCommand)dbCmmd);
                OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
        }
        internal DataSet GetDataSetViaSqlParasEx(DbConnection dbCnn, string strSql, List<object> lstPara, object[] arrValues)
        {
            Logger.Info("GetDataSetViaSqlParas", string.Format("With OR where clause, Sql:\r\n[{0}]", strSql));
            int iL = 0;
            if (this.CurrentConnection == null)
                GetDbConnection();
            using (DbCommand dbCmmd = new OracleCommand(strSql,(OracleConnection)dbCnn))
            {
                for (int i=0;i< arrValues.Length; i++)
                {
                    iL = 0;
                    if (arrValues[i] == null) continue;
                    if (arrValues[i].GetType().IsArray)
                    {
                        List<string> lstParaSub = null;
                        if (lstPara[i] is List<string>)
                        {
                            lstParaSub = (List<string>)lstPara[i];
                            if (lstParaSub == null) continue;
                            Array ov = (Array)arrValues[i];
                            foreach (string objParaSub in lstParaSub)
                            {
                                if (ov.GetValue(iL) == null) continue;
                                dbCmmd.Parameters.Add(new OracleParameter(objParaSub, ov.GetValue(iL)));
                                iL++;
                            }
                        }
                        else
                        {
                            Logger.Error("GetDataSetViaSqlParasEx", string.Format("The [{0}]th parameters should be a list.",i));
                            return null;
                        }
                    }
                    else
                    {
                        dbCmmd.Parameters.Add(new OracleParameter(lstPara[i].ToString(),arrValues[i]));
                    }
                }
                OracleDataAdapter adapter = new OracleDataAdapter((OracleCommand)dbCmmd);
                OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
        }
#endif
    }
}
