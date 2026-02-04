using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.message.Business
{
    public class B_PROJ_TS_TC_FULLVISION : V_PROJ_TS_TC_FULLVISIONDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_PROJ_TS_TC_FULLVISION));
        public static B_PROJ_TS_TC_FULLVISION ToBO(V_PROJ_TS_TC_FULLVISION entity)
        {
            if (entity == null) return null;

            var bo = new B_PROJ_TS_TC_FULLVISION();

            bo.TEST_SUITE_ID = entity.TEST_SUITE_ID;
            bo.TEST_SUITE_NAME = entity.TEST_SUITE_NAME;
            bo.TEST_SUITE_DESCRIPTION = entity.TEST_SUITE_DESCRIPTION;
            bo.TEST_CASE_ID = entity.TEST_CASE_ID;
            bo.TEST_CASE_NAME = entity.TEST_CASE_NAME;
            bo.TEST_STEP_CREATE_TIME = entity.TEST_STEP_CREATE_TIME;
            bo.TEST_STEP_CREATOR = entity.TEST_STEP_CREATOR;
            bo.TEST_STEP_DESCRIPTION = entity.TEST_STEP_DESCRIPTION;
            bo.USAGE_STATUS = entity.USAGE_STATUS;
            bo.CREATE_DATE = entity.CREATE_DATE;
            bo.CREATOR = entity.CREATOR;
            bo.PROJECT_DESCRIPTION = entity.PROJECT_DESCRIPTION;
            bo.PROJECT_ID = entity.PROJECT_ID;
            bo.PROJECT_NAME = entity.PROJECT_NAME;
            bo.STATUS = entity.STATUS;
            bo.DATA_ALIAS = entity.DATA_ALIAS;
            bo.DATA_SUMMARY_ID = entity.DATA_SUMMARY_ID;
            bo.DATA_AVAILABLE_MARK = entity.DATA_AVAILABLE_MARK;
            bo.DATA_VERSION = entity.DATA_VERSION;
            bo.DATA_STATUS = entity.DATA_STATUS;
            bo.DATA_SET_TYPE = entity.DATA_SET_TYPE;

            //entity.OnDTO(bo);

            return bo;
        }

        public static List<V_PROJ_TS_TC_FULLVISIONDTO> GetTSTCByNamePair(string strDBIdx,List<KeyValuePair<string, string>> lstTSTCPair, ref bool isOk, ref string strError)
        {

            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                List<string> lstTSName = lstTSTCPair.Select(p => p.Key).Distinct().ToList();
                var dx = (from q in dbCntx.V_PROJ_TS_TC_FULLVISION
                          where lstTSName.Contains(q.TEST_SUITE_NAME)
                          select q);
                Logger.Info("GetTSTCByNamePair", dx.ToString());
                var d = dx.ToList();
                var dEx = d.Where(l => lstTSTCPair.Any(k => (string.Compare(l.TEST_SUITE_NAME, k.Key, true) == 0) && (string.Compare(l.TEST_CASE_NAME, k.Value, true) == 0))).ToDTOs();
                isOk = true;
                return dEx;
            }
            catch (Exception e)
            {
                Logger.Error("GetTSTCByNamePair", strError = string.Format("Exception:{0}", e.Message), e);
                isOk = false;
                return null;

            }
        }
    }
}
