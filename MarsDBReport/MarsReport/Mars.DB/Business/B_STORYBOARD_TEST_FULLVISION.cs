using Mars.DataLayer;
using Mars.Dto;
#if !_forWebSvc
#if _forWebClient
using MarsEngineSvc.basicReturnDataStructure;
#endif
#endif
using Route2NSEx.src.Marquis.systemUtil;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    public class B_STORYBOARD_TEST_FULLVISION : V_STORYBOARD_TEST_FULLVISIONDTO
    {
        public bool bRightResultCmp = false;
        private static MLogger logger = MLogger.GetLogger(typeof(B_STORYBOARD_TEST_FULLVISION));
#if _forWebClient
        private const string CNST_WEBAPI_STORYBOARD = "MarsEngine/Storyboards";

#endif
#if _forWebClient
        public List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryboardByIdViaWebApi(string strDBIdx,long? iStory, ref bool isOk, ref string strError)
        {
            MarsRESTfulApiClient webclient = new MarsRESTfulApiClient(strDBIdx);
            string strURL = string.Format("{2}{0}?storyboardId={1}&currentDBIdx={3}", CNST_WEBAPI_STORYBOARD, iStory ?? -1, webclient.webURLPreFix,strDBIdx);
            RESTfulReturnStoryboardObjects storyboards = webclient.GetDataFromURL<RESTfulReturnStoryboardObjects>(strURL, ref isOk, ref strError);
            if ((!isOk) || (storyboards == null) || (storyboards.StoryboardDTOs == null))
            {
                return null;
            }
            return storyboards.StoryboardDTOs.ToList();
        }
#endif

        /*
        public List<B_STORYBOARD_TEST_FULLVISION> GetStoryboardRows(string projectName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_STORYBOARD_TEST_FULLVISION> storyBoardRowList = new List<B_STORYBOARD_TEST_FULLVISION>();

          
            var storyBoardRows = (from c in marsEntities.V_STORYBOARD_TEST_FULLVISION
                                 where c.PROJECT_NAME.Equals(projectName) || projectName == null
                                 orderby c.RUN_ORDER
                                 select c);

            foreach (V_STORYBOARD_TEST_FULLVISION storyBoardRow in storyBoardRows)
            {
                B_STORYBOARD_TEST_FULLVISION newStoryboardRow = new B_STORYBOARD_TEST_FULLVISION();
                newStoryboardRow.STORYBOARD_DETAIL_ID = storyBoardRow.STORYBOARD_DETAIL_ID;
                newStoryboardRow.PROJECT_ID = storyBoardRow.PROJECT_ID;
                newStoryboardRow.PROJECT_NAME = storyBoardRow.PROJECT_NAME;
                newStoryboardRow.PROJECT_DESCRIPTION = storyBoardRow.PROJECT_DESCRIPTION;
                newStoryboardRow.TEST_CASE_NAME = storyBoardRow.TEST_CASE_NAME;
                newStoryboardRow.TEST_CASE_ID = storyBoardRow.TEST_CASE_ID;
                newStoryboardRow.TEST_STEP_DESCRIPTION = storyBoardRow.TEST_STEP_DESCRIPTION;
                newStoryboardRow.TEST_SUITE_ID = storyBoardRow.TEST_SUITE_ID;
                newStoryboardRow.TEST_SUITE_NAME = storyBoardRow.TEST_SUITE_NAME;
                newStoryboardRow.TEST_SUITE_DESCRIPTION = storyBoardRow.TEST_SUITE_DESCRIPTION;
                newStoryboardRow.RUN_ORDER = storyBoardRow.RUN_ORDER;
                newStoryboardRow.DEPENDS_ON = storyBoardRow.DEPENDS_ON;
                newStoryboardRow.ALIAS_NAME = storyBoardRow.ALIAS_NAME;
                newStoryboardRow.DISPLAY_NAME = storyBoardRow.DISPLAY_NAME;
                newStoryboardRow.TEST_RUN_VALUE = storyBoardRow.TEST_RUN_VALUE;
                newStoryboardRow.LATEST_TEST_MARK_ID = storyBoardRow.LATEST_TEST_MARK_ID;
                newStoryboardRow.HIST_LATEST_TEST_MARK_ID = storyBoardRow.HIST_LATEST_TEST_MARK_ID;
                newStoryboardRow.HIST_ID = storyBoardRow.HIST_ID;
                newStoryboardRow.HIST_TEST_ID = storyBoardRow.HIST_TEST_ID;
                newStoryboardRow.TEST_CASE_BEGIN_TIME = storyBoardRow.TEST_CASE_BEGIN_TIME;
                newStoryboardRow.TEST_CASE_END_TIME = storyBoardRow.TEST_CASE_END_TIME;
                newStoryboardRow.HIST_TEST_RESULT_IN_TEXT = storyBoardRow.HIST_TEST_RESULT_IN_TEXT;
                newStoryboardRow.HIST_TEST_MODE = storyBoardRow.HIST_TEST_MODE;
                newStoryboardRow.HIST_RESULT = storyBoardRow.HIST_RESULT;
                newStoryboardRow.PARENT_ALIAS_NAME = storyBoardRow.PARENT_ALIAS_NAME;

                storyBoardRowList.Add(newStoryboardRow);
            }

            return storyBoardRowList;
        }
        */
    }
}
