extern alias clientWCF;

using com.Mars.Constants;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using MarsTestFrame.SourceCode.com.Mars.Excels.DB;
using MarsTestFrame.SourceCode.systemUtil;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarsTestFrame.systemUtil;
using Mars.Dto;
using MarsTestFrame.SourceCode.com.Mars.DB;
using MarsTestFrame.SourceCode.com.Mars.DB.Dto;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.com.Mars.TestConfigObjects.Adatpers;
using System.Collections;
using Mars.Business;
using Mars.DataLayer;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;

namespace MarsTestFrame.SourceCode.com.Mars.Excels
{


    public class DashBoardFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DashBoardFactory));
#if _Datafrom_Database
        public static BatchXls GetDashBoardViaCfg()
        {
            return IsDashBoardFromDB() ? new DashBoardFromDB() : new BatchXls();
            
        }
        public static bool IsDashBoardFromDB()
        {
            string strTCDataSource = AppConfigReader.GetTCDataSource();
            if (string.IsNullOrEmpty(strTCDataSource))
            {
                Logger.Warnning("IsDashBoardFromDB", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._OBJECT_IS_NULL));
                return false;
            }
            if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_TCDATASOURCE_DB, strTCDataSource, true) == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get Projects information by Project id, and run type
        /// </summary>
        /// <param name="strProjID"> projet ID</param>
        /// <param name="iRunType"> value of combined 1,2,,4,8 by "or" opeartor  </param>
        /// <returns></returns>
        internal static List<ConfigObjectBase> GetProjectsAppViewsByProjID(string strProjID, int?[] arrRunType, long lAppId=-1)
        {
            //string strSubClause = DBBusinessMgr.GetWhereSubClauseForDashBoardProjectFullVisionView( arrRunType);
            List<V_STORYBOARD_TEST_FULLVISIONDTO> lstDashTestSuiteView= new DBBusinessMgr().GetProjectsAppViewsByProjID(strProjID, arrRunType);
            /// convert to TestSuiteObject
            /// 
            List<ConfigObjectBase> objBatches = new List<ConfigObjectBase>();
            TestSuiteDBAdapter objTSDBADP = (TestSuiteDBAdapter)TestSuiteAdapterFactory.GetAdapterInstance(MARS_ADAPTER._ADPTR_DB_2_TESTSUITE);
            if (objTSDBADP==null)
            {
                Logger.Error("GetProjectsAppViewsByProjID", "No TestSuiteDBAdapter instance returns.");
                return null;
            }
            foreach (V_STORYBOARD_TEST_FULLVISIONDTO objTestDash  in lstDashTestSuiteView)
            {
                //BatchConfigObjectFromDB objFromDb = new BatchConfigObjectFromDB();

                ConfigObjectBase objTS = objTSDBADP.LoadTestSuiteInfo(objTestDash,-1, lAppId);
                if (objTS == null)
                {
                    Logger.Warnning("GetProjectsAppViewsByProjID", string.Format("a null object results from TestSuiteDBAdapter.LoadTestSuiteInfo. [Dash Info:\r\n {0}]",objTestDash.ToString()));
                    continue;
                }
                objBatches.Add(objTS);
            }

            return objBatches;
        }

        //public static bool IsTestProjectsFromDB()
        //{
        //    string strProjDataSource = AppConfigReader.GetTestProjectSource();
        //    if (string.IsNullOrEmpty(strProjDataSource))
        //    {
        //        Logger.Warnning("IsTestProjectsFromDB", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._OBJECT_IS_NULL));
        //        return false;
        //    }
        //    if (string.Compare(SystemConstant.CNST_APPCONFIG_APPSETTING_PROJECTS_SOURCE_DB, strProjDataSource, true) == 0)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        public static List<MarsKeyValues<string, string>> RefreshProjectNamesWithIds()
        {
            Logger.logBegin("RefreshProjectNamesWithIds");
            try
            {
                List<T_TEST_PROJECTDTO> lstProj = new DBBusinessMgr().GetProjects();
                if (lstProj == null) return null;

                List<MarsKeyValues<string, string>> lstRslt = new List<MarsKeyValues<string, string>>();
                /// convert data to GUI Acceable Data
                foreach (T_TEST_PROJECTDTO objProj in lstProj)
                {
                    MarsKeyValues<string, string> objOneProject4GUI = new MarsKeyValues<string, string>(objProj.PROJECT_ID + "",objProj.PROJECT_NAME);
                    lstRslt.Add(objOneProject4GUI);
                }
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("RefreshProjectNamesWithIds", string.Format("Exceptions[{0}]",e.Message),e);
                
                return null;
            }
            finally { Logger.logEnd("RefreshProjectNamesWithIds"); }
                        
        }

        internal static ERROR_CODE GetTestStepsByTestSuiteIdAndTestCaseId(long? testSuiteKeyId, long? testCaseKeyId, Hashtable mlstDataFile)
        {
            Logger.logBegin("GetTestStepsByTestSuiteIdAndTestCaseId");
            try
            {
                //List
            }
            catch (Exception)
            {

                throw;
            }
            Logger.logEnd("GetTestStepsByTestSuiteIdAndTestCaseId");
            return ERROR_CODE._NO_ERROR;
        }

        public static List<MarsKeyValues<string, string>> RefreshProjectsWithApps(long projectId=int.MinValue,long storyboardId=int.MinValue)
        {
            try
            {
                IList<V_STB_PROJ_APP_FULLVISIONDTO> lstProj = null;
                if (storyboardId == int.MinValue)
                    lstProj = BoHelper.GetStoryBoardById("TEMPSTUB",null);
                else
                    lstProj = BoHelper.GetStoryBoardById("TEMPSTUB",storyboardId);
                //List<TEST_PROJECT_VIEW> lstProj = new DBBusinessMgr().GetProjectsAppViews();
                if (lstProj == null) return null;

                List<MarsKeyValues<string, string>> lstRslt = new List<MarsKeyValues<string, string>>();
                /// convert data to GUI Acceable Data
                /// MarsKeyValues
                /// 
                MarsKeyValues<string, string> objCurrentKeyValues = new MarsKeyValues<string, string>("","");
                V_STB_PROJ_APP_FULLVISIONDTO objCurrentProjView = new V_STB_PROJ_APP_FULLVISIONDTO();
                foreach (V_STB_PROJ_APP_FULLVISIONDTO objProj in lstProj)
                {
                    MarsKeyValues<string, string> objOneProject4GUI = new MarsKeyValues<string, string>(objProj.STORYBOARD_ID + "", objProj.STORYBOARD_NAME);
                    if (objCurrentProjView.STORYBOARD_ID != objProj.STORYBOARD_ID)
                    {
                        lstRslt.Add(objOneProject4GUI);
                        objCurrentProjView = objProj;
                        objCurrentKeyValues = objOneProject4GUI;
                        objCurrentKeyValues.Children = new List<MarsKeyValues<string, string>>();
                        objCurrentKeyValues.Children.Add(new MarsKeyValues<string, string>(objProj.APPLICATION_ID+"", objProj.APP_SHORT_NAME));
                        continue;
                    }
                    if (objCurrentProjView.APPLICATION_ID == objProj.APPLICATION_ID) continue;
                    objCurrentKeyValues.Children.Add(new MarsKeyValues<string, string>(objProj.APPLICATION_ID + "", objProj.APP_SHORT_NAME));
                }
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("RefreshProjectsWithApps", string.Format("Exceptions[{0}]", e.Message), e);

                return null;
            }
            finally {
                Logger.logEnd("RefreshProjectsWithApps");
            }
        }
        
#endif
    }

}
