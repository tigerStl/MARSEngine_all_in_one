using System;
using System.Collections.Generic;
using System.Data;
using MarsTestFrame.SourceCode.com.Mars.DB.Dto;
using Route2NSEx.src.Marquis.systemUtil;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Bo
{
#if _Datafrom_Database
    internal class B_DASHBOARD_TEST_FULLVISION
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_DASHBOARD_TEST_FULLVISION));

        internal List<V_DASHBOARD_TEST_FULLVISION> GetDashboardFullViewByIDAndRunType(DataSet dataSet)
        {
            if (dataSet == null) return null;
            /// 添加adapter的处理
            List<V_DASHBOARD_TEST_FULLVISION> lstRslt = new List<V_DASHBOARD_TEST_FULLVISION>();
            try
            {
                foreach (DataRow oneRow in dataSet.Tables[0].Rows)
                {
                    V_DASHBOARD_TEST_FULLVISION  objOneItm = AssemblyOneRow(oneRow);
                    if (objOneItm == null) continue;
                    lstRslt.Add(objOneItm);
                }
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("GetDashboardFullViewByIDAndRunType", string.Format("Exceptions:[{0}] when assembly data from DataRows",e.Message), e );
                return null;
            }
        }

        internal V_DASHBOARD_TEST_FULLVISION AssemblyOneRow(DataRow oneRow)
        {
            if (oneRow == null) return null;

            V_DASHBOARD_TEST_FULLVISION objDashBoard = new V_DASHBOARD_TEST_FULLVISION();
            objDashBoard.DASHBOARD_ID = oneRow["DASHBOARD_ID"]==DBNull.Value? null :(long?)oneRow["DASHBOARD_ID"];
            objDashBoard.DASHBOARD_NAME = oneRow["DASHBOARD_NAME"]==DBNull.Value?null: (string)oneRow["DASHBOARD_NAME"];
            objDashBoard.DISPLAY_NAME = oneRow["DISPLAY_NAME"]==DBNull.Value? null : (string)oneRow["DISPLAY_NAME"];
            objDashBoard.HIST_TEST_MODE =(oneRow["HIST_TEST_MODE"]==DBNull.Value? null :(short?)oneRow["HIST_TEST_MODE"]);
            objDashBoard.HIST_TEST_RESULT_IN_TEXT = oneRow["HIST_TEST_RESULT_IN_TEXT"]==DBNull.Value? null : (string)oneRow["HIST_TEST_RESULT_IN_TEXT"];
            objDashBoard.HIS_ID = oneRow["HIS_ID"]==DBNull.Value? null :(long?)oneRow["HIS_ID"];
            objDashBoard.HIS_LATEST_TEST_MARK_ID = oneRow["HIS_LATEST_TEST_MARK_ID"]==DBNull.Value? null :(long?)oneRow["HIS_LATEST_TEST_MARK_ID"];
            objDashBoard.HIS_RESULT = oneRow["HIS_RESULT"] == DBNull.Value ? null : (short?)oneRow["HIS_RESULT"]; ;
            objDashBoard.HIS_TEST_ID = oneRow["HIS_LATEST_TEST_MARK_ID"]==DBNull.Value? null : (long?)oneRow["HIS_LATEST_TEST_MARK_ID"];
            objDashBoard.LATEST_TEST_MARK_ID = oneRow["LATEST_TEST_MARK_ID"] == DBNull.Value ? null: (long?)oneRow["LATEST_TEST_MARK_ID"];
            objDashBoard.PROJECT_DESCRIPTION = oneRow["PROJECT_DESCRIPTION"]==DBNull.Value? null : (string)oneRow["PROJECT_DESCRIPTION"];
            objDashBoard.PROJECT_ID = oneRow["PROJECT_ID"]==DBNull.Value?null: (long?)oneRow["PROJECT_ID"];
            objDashBoard.PROJECT_NAME = oneRow["PROJECT_NAME"] == DBNull.Value ? null : (string)oneRow["PROJECT_NAME"];
            objDashBoard.RUN_ORDER = oneRow["RUN_ORDER"] == DBNull.Value ? null : (long?)oneRow["RUN_ORDER"];
            objDashBoard.TEST_CASE_BEGIN_TIME = oneRow["TEST_CASE_BEGIN_TIME"] == DBNull.Value ? null : (DateTime ?)oneRow["TEST_CASE_BEGIN_TIME"];
            objDashBoard.TEST_CASE_END_TIME = oneRow["TEST_CASE_END_TIME"] == DBNull.Value ? null : (DateTime?)oneRow["TEST_CASE_END_TIME"];
            objDashBoard.TEST_CASE_ID = oneRow["TEST_CASE_ID"] == DBNull.Value ? null: (long?)oneRow["TEST_CASE_ID"];
            objDashBoard.TEST_CASE_NAME = oneRow["TEST_CASE_NAME"]==DBNull.Value? null :(string)oneRow["TEST_CASE_NAME"];
            objDashBoard.TEST_RUN_VALUE = oneRow["TEST_RUN_VALUE"] == DBNull.Value ? null : (short?)oneRow["TEST_RUN_VALUE"];
            objDashBoard.TEST_STEP_DESCRIPTION = oneRow["TEST_STEP_DESCRIPTION"]==DBNull.Value? null : (string)oneRow["TEST_STEP_DESCRIPTION"];
            objDashBoard.TEST_SUITE_NAME = oneRow["TEST_SUITE_NAME"] == DBNull.Value ? null : (string)oneRow["TEST_SUITE_NAME"];
            objDashBoard.TEST_SUITE_DESCRIPTION = oneRow["TEST_SUITE_DESCRIPTION"]==DBNull.Value? null :(string)oneRow["TEST_SUITE_DESCRIPTION"];
            objDashBoard.TEST_SUITE_ID = oneRow["TEST_SUITE_ID"] == DBNull.Value ? null : (long?)oneRow["TEST_SUITE_ID"];
            objDashBoard.ALIAS_NAME = oneRow["ALIAS_NAME"] == DBNull.Value ? null : (string)oneRow["ALIAS_NAME"];
            objDashBoard.RELY_ON = oneRow["RELY_ON"] == DBNull.Value ? null : (long?)oneRow["RELY_ON"];
            return objDashBoard;
        }
    }
#endif
}