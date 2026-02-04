using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using Mars.Model;
using System.Data.Objects;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.Business
{
    public class B_REL_APP_TESTSUITE : REL_APP_TESTSUITEDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_REL_APP_TESTSUITE));
        public long getRelTestSuiteAppId(string strDBIdx, MarsEntities dbCntx=null)
        {
            MarsEntities marsEntities = dbCntx??BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("REL_APP_TESTSUITE_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());
        }

        public bool SetTSAndApps(string strDBIdx, MarsEntities objDBContext, long lTsID, List<long> appIds, ref string strError)
        {
            try
            {
                objDBContext.Set<REL_APP_TESTSUITE>();
                foreach(long appid in appIds)
                {
                    if (appid <= 0) continue;
                    REL_APP_TESTSUITE objEntity = new REL_APP_TESTSUITE();
                    objEntity.RELATIONSHIP_ID = getRelTestSuiteAppId(strDBIdx,objDBContext);
                    objEntity.TEST_SUITE_ID = lTsID;
                    objEntity.APPLICATION_ID = appid;
                    objDBContext.REL_APP_TESTSUITE.Add(objEntity);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SetTSAndApps", strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
        }
    }
}
