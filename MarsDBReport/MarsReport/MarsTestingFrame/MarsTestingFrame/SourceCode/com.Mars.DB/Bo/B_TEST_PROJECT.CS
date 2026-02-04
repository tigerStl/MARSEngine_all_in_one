extern alias clientWCF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Mars.Dto;
using MarsExcelDataProvider;
#if _Datafrom_Database
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#endif
namespace MarsTestFrame.SourceCode.com.Mars.DB.Bo
{
    internal class B_TEST_PROJECT
    {
#if _Datafrom_Database
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_PROJECT));

        public const string CNST_PROJECT_DESCRIPTION = "PROJECT_DESCRIPTION";
        public const string CNST_PROJECT_ID = "PROJECT_ID";

        internal List<T_TEST_PROJECTDTO> GetObjectsFromDataSet(DataSet dataSet)
        {
            if (dataSet == null) return null;
            /// load data to Objects
            /// 
            int iCnt = 0;
            try
            {
                if (dataSet.Tables.Count <= 0)
                {
                    Logger.Warnning("GetObjectsFromDataSet", string.Format("Tables.count ==0 , no data exists in Data Set"));
                    return null;
                }
                List<T_TEST_PROJECTDTO> lstResult = new List<T_TEST_PROJECTDTO>();
                foreach (DataRow oneRow in dataSet.Tables[0].Rows)
                {
                    T_TEST_PROJECTDTO objProj = new T_TEST_PROJECTDTO();
                    LoadDataRowToObject(oneRow, objProj);
                    lstResult.Add(objProj);
                }
                iCnt = lstResult.Count;
                return lstResult;
            }
            catch (Exception e)
            {
                Logger.Error("GetObjectsFromDataSet", string.Format("Exception when Load dataset into Object:[{0}]",e.Message),e);
                return null;
            }
            finally
            {
                Logger.Info("GetObjectsFromDataSet", string.Format("Loaded [{0}] objects", iCnt));
                Logger.logEnd("GetObjectsFromDataSet");
            }
        }

        private void LoadDataRowToObject(DataRow oneRow, T_TEST_PROJECTDTO objProj)
        {
            if (objProj == null) return;
            objProj.PROJECT_DESCRIPTION = (string)oneRow[CNST_PROJECT_DESCRIPTION];
            objProj.PROJECT_ID = (Int64)oneRow[CNST_PROJECT_ID];
            objProj.PROJECT_NAME = (string)oneRow["PROJECT_NAME"];
            //objProj.REL_APP_PROJ_RELATIONSHIP_ID = (Int64)oneRow["REL_APP_PROJ_RELATIONSHIP_ID"];
            objProj.STATUS = (short)oneRow["STATUS"];
            
        }
#endif
    }
}
