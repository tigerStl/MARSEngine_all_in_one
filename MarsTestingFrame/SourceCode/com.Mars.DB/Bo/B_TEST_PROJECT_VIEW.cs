using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MarsTestFrame.SourceCode.com.Mars.DB.Dto;
using Route2NSEx.src.Marquis.systemUtil;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Bo
{

    internal class B_TEST_PROJECT_VIEW
    {
#if _Datafrom_Database
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_PROJECT_VIEW));
        internal List<TEST_PROJECT_VIEW> GetObjectsFromDataSet(DataSet dataSet)
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
                List<TEST_PROJECT_VIEW> lstResult = new List<TEST_PROJECT_VIEW>();
                TEST_PROJECT_VIEW objCurrentProjApp = new TEST_PROJECT_VIEW();
                foreach (DataRow oneRow in dataSet.Tables[0].Rows)
                {
                    TEST_PROJECT_VIEW objProj = new TEST_PROJECT_VIEW();
                    LoadDataRowToObject(oneRow, objProj);
                    lstResult.Add(objProj);                 
                    
                }
                iCnt = lstResult.Count;
                return lstResult;
            }
            catch (Exception e)
            {
                Logger.Error("GetObjectsFromDataSet", string.Format("Exception when Load dataset into Object:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.Info("GetObjectsFromDataSet", string.Format("Loaded [{0}] objects", iCnt));
                Logger.logEnd("GetObjectsFromDataSet");
            }
        }

        private void LoadDataRowToObject(DataRow oneRow, TEST_PROJECT_VIEW objProj)
        {
            if (oneRow == null) return;
            objProj.APPLICATION_ID = (long)oneRow["APPLICATION_ID"];
            objProj.APP_SHORT_NAME = (string)oneRow["APP_SHORT_NAME"];
            objProj.PROJECT_ID = (long)oneRow["PROJECT_ID"];
            objProj.PROJECT_NAME = (string)oneRow["PROJECT_NAME"];
            objProj.TEST_SUITE_ID = (long)oneRow["TEST_SUITE_ID"];
            objProj.TEST_SUITE_NAME = (string)oneRow["TEST_SUITE_NAME"];
           
        }
#endif

    }
}
