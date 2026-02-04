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
    public class B_REL_APP_PROJ : REL_APP_PROJDTO, IMarsTigerTranscation
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(B_REL_APP_PROJ));
        public long getRelAppProjId(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long relationId = (long)marsEntities.GETNEXT_VAL("REL_APP_PROJ_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public static B_REL_APP_PROJ CreateNewObject(long aPPLICATION_ID, long pROJECT_ID)
        {
            Logger.Info("CreateNewObject", string.Format("application_id:[{0}] project_Id:[{1}]", aPPLICATION_ID, pROJECT_ID));
            B_REL_APP_PROJ objResult = new B_REL_APP_PROJ();

            objResult.APPLICATION_ID = aPPLICATION_ID;
            objResult.PROJECT_ID = pROJECT_ID;
            objResult.RELATIONSHIP_ID = -1;

            return objResult;
        }

        public Type GetBOEntityType()
        {
            return typeof(REL_APP_PROJ);
        }

        public bool ModifyObject(MarsEntities objEntityMgr, ref string strError)
        {
            return true;
        }

        public EN_TRANSCATION_DEALSTAUTS RemoveByTranscation(MarsEntities transcationEntityMgr, ref string strError)
        {
            if (transcationEntityMgr == null)
            {
                strError = "Transcation Mgr object is null.";
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }
            try
            {
                var lst = transcationEntityMgr.REL_APP_PROJ.Where(p => p.APPLICATION_ID == this.APPLICATION_ID && p.PROJECT_ID == this.PROJECT_ID);
                foreach (var itm in lst)
                    transcationEntityMgr.REL_APP_PROJ.Remove(itm);
                return EN_TRANSCATION_DEALSTAUTS.EN_OK;
            }
            catch (Exception e)
            {
                Logger.Error("RemoveByTranscation", strError = string.Format("Exception:[{0}]", e.Message), e);
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }

        }

        public static List<B_REL_APP_PROJ> GetRecInfoByProjId(string strDBIdx, long lProjId, ref string strErrorOrHint, ref bool isOk)
        {
            Logger.logBegin("GetRecInfoByProjId", string.Format("Project Id [{0}]", lProjId));
            try
            {
                MarsEntities dbcntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var a = (from x in dbcntx.REL_APP_PROJ
                         from app in dbcntx.T_REGISTERED_APPS
                         where x.PROJECT_ID == lProjId
                         && app.APPLICATION_ID == x.APPLICATION_ID
                         select new
                         {
                             proj = x,
                             appl = app
                         }).OrderBy(p => p.appl.APP_SHORT_NAME);

                List<B_REL_APP_PROJ> lstRslt = new List<B_REL_APP_PROJ>();
                a.ToList().ForEach(
                    itm =>
                    {
                        B_REL_APP_PROJ objB = null;
                        if ((itm != null) && (itm.proj != null))
                        {
                            objB = CopyFromDTO(itm.proj.ToDTO());
                            if (itm.appl != null)
                                objB.ApplicationName = itm.appl.APP_SHORT_NAME;
                            lstRslt.Add(objB);
                        }
                    }
                    );
                isOk = true;
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("GetRecInfoByProjId", strErrorOrHint = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("GetRecInfoByProjId");
            }
        }

        public EN_TRANSCATION_DEALSTAUTS AddByTranscation(string strDBIdx, MarsEntities transcationEntityMgr, ref string strError)
        {
            if (transcationEntityMgr == null)
            {
                strError = "Transcation Mgr object is null.";
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }
            try
            {
                bool isOk = transcationEntityMgr.REL_APP_PROJ.Any(p => p.PROJECT_ID == this.PROJECT_ID && p.APPLICATION_ID == this.APPLICATION_ID);
                if (isOk)
                {
                    strError = string.Format("A recorder is Exists, application id:[{0}] project id:[{1}]", this.APPLICATION_ID, this.PROJECT_ID);
                    return EN_TRANSCATION_DEALSTAUTS.EN_IGNORE;
                }
                if (this.RELATIONSHIP_ID < 0)
                    this.RELATIONSHIP_ID = getRelAppProjId(strDBIdx);

                transcationEntityMgr.REL_APP_PROJ.Add(this.ToEntity());
                return EN_TRANSCATION_DEALSTAUTS.EN_OK;
            }
            catch (Exception e)
            {
                Logger.Error("AddByTranscation", strError = string.Format("Exception:[{0}]", e.Message), e);
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }

        }

        internal static B_REL_APP_PROJ CopyFromDTO(REL_APP_PROJDTO objSrc)
        {
            B_REL_APP_PROJ objResult = new B_REL_APP_PROJ();
            if (objSrc == null) return null;
            objResult.APPLICATION_ID = objSrc.APPLICATION_ID;
            objResult.PROJECT_ID = objSrc.PROJECT_ID;
            objResult.RELATIONSHIP_ID = objSrc.RELATIONSHIP_ID;
            return objResult;
        }

        //Data used for xml Exp and Imp
        public string ApplicationName;

        public bool CreateRelations(string strDBIdx, long lProjId, IEnumerable<long> appIds, MarsEntities currentDBContext, ref string strError)
        {
            Logger.logBegin("CreateRelations", string.Format("project id:[{0}] Appids:[{1}]", lProjId, appIds));
            try
            {
                if (currentDBContext == null)
                {
                    Logger.Error("CreateRelations", strError = "No Database context");
                    return false;
                }
                foreach (long lAppId in appIds)
                {
                    if (lAppId == -1) continue;
                    long relId = getRelAppProjId(strDBIdx, currentDBContext);
                    if (relId == -1)
                    {
                        Logger.Error("CreateRelations", strError = "Cant get Relation ship id.");
                        return false;
                    }
                    currentDBContext.Set<REL_APP_PROJ>();
                    currentDBContext.REL_APP_PROJ.Add(new REL_APP_PROJ()
                    {
                        RELATIONSHIP_ID = relId,
                        PROJECT_ID = lProjId,
                        APPLICATION_ID = lAppId
                    });
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateRelations", strError = string.Format("Exception [{0}] ", e.Message), e);
                return false;
            }
        }
    }
}
