extern alias clientWCF;

using Mars.message.DataLayer;
using Mars.message.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.AutoTestingDriver.ExecuteStoryboard
{
    class ExecutableTestStoryBoard
    {
        private static clientWCF::Route2NSEx.src.Marquis.systemUtil.MLogger Logger = clientWCF::Route2NSEx.src.Marquis.systemUtil.MLogger.GetLogger(typeof(ExecutableTestStoryBoard));

        internal static List<ExecutableTestStoryBoard> GetStoryboarddetailListByStoryboardId(long lStoryboardId, bool isBase, 
            ref string strError,
            string currentDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("GetStoryboarddetailListByStoryboardId", string.Format("Storyboard id:[{0}]", lStoryboardId));
            try
            {
#if !_forWebClient
                List<V_STORYBOARD_TEST_FULLVISIONDTO> storyboardsToRun = Mars.Business.B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(lStoryboardId, isBase,
                    currentDBIdx)
                    .OrderBy(p => p.RUN_ORDER).ToList();
#else                
                List<V_STORYBOARD_TEST_FULLVISIONDTO> storyboardsToRun = (new Mars.message.Business.B_V_STORYBOARD_TEST_FULLVISION()).GetStoryBoards(lStoryboardId, isBase, currentDBIdx)
                .OrderBy(p => p.RUN_ORDER).ToList();
#endif
                return ConvertFromDatabaseObject(storyboardsToRun);
            }catch(Exception e)
            {
                Logger.Error("strCurrentDB", strError = e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetStoryboarddetailListByStoryboardId");
            }

        }

        private V_STORYBOARD_TEST_FULLVISIONDTO assignedTestObjectFromDB;
        public V_STORYBOARD_TEST_FULLVISIONDTO AssignedTestObjectFromDB
        {
            get
            {
                return assignedTestObjectFromDB;
            }
        }

        private static ExecutableTestStoryBoard ConvertFromDatabaseObject(V_STORYBOARD_TEST_FULLVISIONDTO objDBStoryboard)
        {
            return new ExecutableTestStoryBoard() { assignedTestObjectFromDB = objDBStoryboard };
        }

        private static List<ExecutableTestStoryBoard> ConvertFromDatabaseObject(List<V_STORYBOARD_TEST_FULLVISIONDTO> objDBStoryboard)
        {
            if (objDBStoryboard == null) return null;
            List<ExecutableTestStoryBoard> lstResult = new List<ExecutableTestStoryBoard>();
            objDBStoryboard.ForEach(p =>
            {
                if (p != null)
                    lstResult.Add(ConvertFromDatabaseObject(p));
            });
            return lstResult;
        }

        private string shortAppShortName;
        public string ShortAppShortName
        {
            get
            {
                return shortAppShortName;
            }
            set
            {
                if (value == null) return;
                if (string.Compare(shortAppShortName, value, true) == 0) return;
            }
        }

        private string action;
        public string Action
        {
            get
            {
                return action;
            }
            set
            {
                if (value == null) action = "SKIP";
                action = value;
            }
        }

    }

}
