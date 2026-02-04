using Mars.DataLayer;
using Mars.DataLayer.Generic;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Objects;


using System.Linq;

namespace Mars.Business
{
    public class B_REL_TEST_SUIT_PROJECT : REL_TEST_SUIT_PROJECTDTO, IMarsTigerTranscation
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_REL_TEST_SUIT_PROJECT));

        private static List<REL_TEST_SUIT_PROJECTDTO> g_CachedSuitProj = null;

        public static List<REL_TEST_SUIT_PROJECTDTO> GetCached(string strDBIdx, MarsEntities dbCntx = null)
        {
            if (g_CachedSuitProj == null)
            {
                MarsEntities dbOp = dbCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntx;
                try
                {
                    g_CachedSuitProj = (from q in dbOp.REL_TEST_SUIT_PROJECT
                                        select q)
                            .OrderBy(p => p.PROJECT_ID)
                            .ThenBy(p => p.T_TEST_SUITE)
                            .ToDTOs();
                }
                catch (Exception e)
                {
                    Logger.Error("GetCached", e.Message, e);
                    return g_CachedSuitProj = null;
                }
            }
            return g_CachedSuitProj;
        }

        public EN_TRANSCATION_DEALSTAUTS AddByTranscation(string strDBIdx, MarsEntities transcationEntityMgr, ref string strError)
        {
            Logger.Info("AddByTranscation", "Begin");
            if (transcationEntityMgr == null)
            {
                Logger.Error("AddByTranscation", "transcationEntityMgr==null");
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }
            if (this.RELATIONSHIP_ID <= 0)
            {
                this.RELATIONSHIP_ID = getRelTestSuiteProject(strDBIdx,transcationEntityMgr);
            }
            try
            {
                //var obj = transcationEntityMgr.REL_TEST_SUIT_PROJECT.FirstOrDefault(p=>p.PROJECT_ID==this.PROJECT_ID && p.TEST_SUITE_ID==this.TEST_SUITE_ID);
                //if (obj==null)
                transcationEntityMgr.REL_TEST_SUIT_PROJECT.Add(this.ToEntity());
                return EN_TRANSCATION_DEALSTAUTS.EN_OK;
            }
            catch (Exception e)
            {
                Logger.Error("AddByTranscation", strError = string.Format("Can't add new objec to REL_TEST_SUIT_PROJECT with Exception:[{0}]", e.Message), e);
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }


        }

        public Type GetBOEntityType()
        {
            return typeof(REL_TEST_SUIT_PROJECT);
        }

        public long getRelTestSuiteProject(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("REL_TEST_SUIT_PROJECT_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public bool ModifyObject(MarsEntities objEntityMgr, ref string strError)
        {
            Logger.Warnning("ModifyObject", "Not implement!!!!");
            return false;
        }

        public EN_TRANSCATION_DEALSTAUTS RemoveByTranscation(MarsEntities transcationEntityMgr, ref string strError)
        {
            Logger.Info("RemoveByTranscation", "Begin");
            if (transcationEntityMgr == null)
            {
                Logger.Error("RemoveByTranscation", "transcationEntityMgr==null");
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }
            try
            {
                var query = transcationEntityMgr.REL_TEST_SUIT_PROJECT.Where(p => p.PROJECT_ID == this.PROJECT_ID && p.TEST_SUITE_ID == this.TEST_SUITE_ID);
                foreach (var itm in query)
                {
                    transcationEntityMgr.REL_TEST_SUIT_PROJECT.Remove(itm);
                }
                return EN_TRANSCATION_DEALSTAUTS.EN_OK;
            }
            catch (Exception e)
            {
                Logger.Error("RemoveByTranscation", strError = string.Format("Exceptions when call Where/Remove :[{0}]", e.Message), e);
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }

        }

        public long getRelTestSuiteAppId(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("REL_TEST_SUIT_PROJECT_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());
        }

        public bool InsertProjAndTSIds(string strDBIdx, MarsEntities objDBCntx, long lProjId, List<long> lstTsIds, ref string strError)
        {
            Logger.logBegin("InsertProjAndTSIds");
            try
            {
                if (lstTsIds == null) return true;
                if (objDBCntx == null) return true;
                objDBCntx.Set<REL_TEST_SUIT_PROJECT>();
                foreach (long lTS in lstTsIds)
                {
                    if (lTS <= 0) continue;
                    REL_TEST_SUIT_PROJECT objEntity = new REL_TEST_SUIT_PROJECT();
                    objEntity.RELATIONSHIP_ID = getRelTestSuiteAppId(strDBIdx,objDBCntx);
                    objEntity.PROJECT_ID = lProjId;
                    objEntity.TEST_SUITE_ID = lTS;
                    objDBCntx.REL_TEST_SUIT_PROJECT.Add(objEntity);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertProjAndTSIds", strError = string.Format("InsertProjAndTSIds-Exception:{0}", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("InsertProjAndTSIds");
            }
        }
    }
}
