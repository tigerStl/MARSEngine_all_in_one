

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using com.Mars.Constants;
using Mars.DataLayer;
using Mars.DataLayer.Generic;
using Mars.Model;
using Mars.Business;

namespace MarsTestFrame.SourceCode.com.Mars.DB
{
#if _Datafrom_Database

    /// <summary>
    /// Database accessing class in Bussiness Level
    /// </summary>
    public class DBBusinessMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DBBusinessMgr));
        public static string currentDBIdx = null;
        public List<V_OBJECT_APPSDTO> GetAppObjectsByAppShortName(string strDBIdx,string strAppShortName)
        {
            //MarsExcelDataProvider.DataProviderFactory objMarDBFac = new MarsExcelDataProvider.DataProviderFactory();
            //MarsExcelDataProvider.DataProvider objProvider = objMarDBFac.GetDataProvider();

            //B_OBJECT_APPS objBO = new B_OBJECT_APPS();
            Logger.Info("GetAppObjectsByAppShortName",string.Format("AppshortName:[{0}]",strAppShortName));

            B_OBJECT_APPS objApps = new B_OBJECT_APPS();
            return objApps.GetAppObjectsByAppShortName(strDBIdx, strAppShortName);
            //MarsDataAccessLayer<V_OBJECT_APPS> objDBAccess = new MarsDataAccessLayer<V_OBJECT_APPS>();
            //IList<V_OBJECT_APPS> lstObjDB = objDBAccess.GetList(p=>p.APP_SHORT_NAME == strAppShortName);
            //var lstO = from x in lstObjDB
            //           where x.APP_SHORT_NAME != null
            //           //&& x.OBJECT_TYPE != null
            //           && x.OBJECT_HAPPY_NAME != null
            //           select x;
            //IOrderedEnumerable<V_OBJECT_APPS> lstObjDBx = lstO.OrderBy(x => x.OBJECT_TYPE ?? "").ThenBy(objapp=>objapp.OBJECT_HAPPY_NAME);
            ////IOrderedEnumerable < V_OBJECT_APPS > lstObjDB = objDBAccess.GetList(p => p.APP_SHORT_NAME != null && p.APP_SHORT_NAME == strAppShortName, null).OrderBy(objApp => objApp.OBJECT_TYPE).ThenBy(objapp => objapp.OBJECT_HAPPY_NAME);

            //return V_OBJECT_APPSAssembler.ToDTOs(lstObjDBx);
            //return objBO.GetObjectsFromDataSet(objProvider.GetObjectDataSetByAppName(strAppShortName));
        }

        internal List<T_TEST_PROJECTDTO> GetProjects()
        {
            Logger.logBegin("GetProjects");
            //MarsExcelDataProvider.DataProviderFactory objMarDBFac = new MarsExcelDataProvider.DataProviderFactory();
            //MarsExcelDataProvider.DataProvider objProvider = objMarDBFac.GetDataProvider();
            //B_TEST_PROJECT objBO = new B_TEST_PROJECT();
            MarsDataAccessLayer<T_TEST_PROJECT> objDBAccess = new MarsDataAccessLayer<T_TEST_PROJECT>(currentDBIdx);
            IList<T_TEST_PROJECT> lstResultDB = objDBAccess.GetAll();
            return T_TEST_PROJECTAssembler.ToDTOs(lstResultDB); 
            //return objBO.GetObjectsFromDataSet(objProvider.GetProjectDataSetByProjectName(null));            
        }

        //internal List<TEST_PROJECT_VIEW> GetProjectsAppViews(string strAppNames = null,string strProjectName=null)
        //{
        //    Logger.logBegin("GetProjectsAppViews");
        //    MarsExcelDataProvider.DataProviderFactory objMarDBFac = new MarsExcelDataProvider.DataProviderFactory();
        //    MarsExcelDataProvider.DataProvider objProvider = objMarDBFac.GetDataProvider();
        //    B_TEST_PROJECT_VIEW objTestView = new B_TEST_PROJECT_VIEW();
        //    return objTestView.GetObjectsFromDataSet(objProvider.GetProjectAppsByAppNamesProjectNames(strAppNames,strProjectName)); 
        //}

        internal string GetWhereSubClauseForDashBoardProjectFullVisionView(int[] arr_iRunType)
        {
            Logger.logBegin("GetWhereSubClauseForDashBoardFullVisionView");
            string strResult = "",strSubResult = "";
            foreach(int iType in arr_iRunType)
            {
                try
                {
                    ENUM_TEST_SUITE_RUNTYPE enRunType = (ENUM_TEST_SUITE_RUNTYPE)iType;
                    strSubResult = "TEST_RUN_VALUE="+iType  ;
                    if (string.IsNullOrEmpty(strResult))
                    {
                        strResult = strSubResult;
                    }
                    else
                    {
                        strResult += (" or TEST_RUN_VALUE=" + iType); 
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("GetWhereSubClauseForDashBoardFullVisionView",string.Format("Exception:[{0}] ,when convert value [1] to ENUM_TEST_SUITE_RUNTYPE\r\n System ignores and continues to run...", e.Message, iType),e);
                }
            }
            Logger.logEnd("GetWhereSubClauseForDashBoardFullVisionView");
            return strResult;
        }

        internal List<V_STORYBOARD_TEST_FULLVISIONDTO> GetProjectsAppViewsByProjID(string strPrjID, int?[] arr_iRunTypeFilter)
        {
            
            Logger.logBegin("GetProjectsAppViews");
            List<V_STORYBOARD_TEST_FULLVISIONDTO> lstDashBoard = BoHelper.GetTestCasesByStoryBoardAndRunTypes(strPrjID, arr_iRunTypeFilter);
            return lstDashBoard;
            //string strSubClause = GetWhereSubClauseForDashBoardProjectFullVisionView(arr_iRunTypeFilter);
            //MarsExcelDataProvider.DataProviderFactory objMarDBFac = new MarsExcelDataProvider.DataProviderFactory();
            //MarsExcelDataProvider.DataProvider objProvider = objMarDBFac.GetDataProvider();

            //B_DASHBOARD_TEST_FULLVISION objTestView = new B_DASHBOARD_TEST_FULLVISION();
            //return objTestView.GetDashboardFullViewByIDAndRunType(objProvider.GetDashboardFullViewByIDAndRunType(strPrjID, arr_iRunTypeFilter));
        }
    }
#endif
}
