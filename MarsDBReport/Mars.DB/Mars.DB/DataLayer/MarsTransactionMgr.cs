

using Mars.DataLayer.Generic;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;

namespace Mars.DataLayer
{
    public class MarsTransactionMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTransactionMgr));
        private static MarsEntities transcationEntityMgr = null;

        private MarsEntities currentDBContext = null;
        public MarsEntities CurrentDBContext
        {
            get { return currentDBContext == null ? currentDBContext = BoHelper.GetMarsEntitiesInstance(true, currentDBIdx) : currentDBContext; }
            set { currentDBContext = value; }
        }
        public MarsTransactionMgr()
        {

        }
        private string currentDBIdx = "MarsEntities";
        public MarsTransactionMgr(string strDBIdx, bool isInitDBCntx)
        {
            currentDBIdx = strDBIdx;
            currentDBContext = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
        }
        public static bool BeginTransaction(string strDBIdx,ref string strError)
        {
            try
            {
                transcationEntityMgr = null;
                transcationEntityMgr = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("BeginTransaction", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }

        }

        public static bool SaveChangesToTranscation(ref string strError)
        {
            try
            {
                transcationEntityMgr.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SaveChangesToTranscation", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public static bool AddModification(IMarsTigerTranscation objDataToChange, ref string strError)
        {
            if (objDataToChange == null)
            {
                return true;
            }

            try
            {
                Type t = objDataToChange.GetBOEntityType();
                bool isOk = objDataToChange.ModifyObject(transcationEntityMgr, ref strError);
                if (!isOk)
                {
                    Logger.Error("AddModification", strError = string.Format("Error from ModifyObject:[{0}]", strError));
                    return false;
                }
                // transcationEntityMgr
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AddModification", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public static bool RemoveList(List<IMarsTigerTranscation> lstEntities, ref string strError)
        {
            Logger.Info("RemoveList", string.Format("Count:[{0}]", lstEntities == null ? 0 : lstEntities.Count));
            foreach (IMarsTigerTranscation objEntity in lstEntities)
            {
                if (objEntity == null) continue;
                EN_TRANSCATION_DEALSTAUTS iStatus = objEntity.RemoveByTranscation(transcationEntityMgr, ref strError);
                if (iStatus == EN_TRANSCATION_DEALSTAUTS.EN_ERROR)
                {
                    Logger.Error("RemoveList", strError = string.Format("Can't delete object with error:[{0}]", strError));
                    return false;
                }
                if (iStatus == EN_TRANSCATION_DEALSTAUTS.EN_IGNORE)
                {
                    Logger.Warnning("RemoveList", strError = string.Format("Can't delete object with ignorable info:[{0}]", strError));
                }
            }
            return true;
        }

        public static bool AddList(string strDBIdx, List<IMarsTigerTranscation> lstEntities, ref string strError)
        {
            Logger.Info("RemoveList", string.Format("Count:[{0}]", lstEntities == null ? 0 : lstEntities.Count));
            foreach (IMarsTigerTranscation objEntity in lstEntities)
            {
                if (objEntity == null) continue;
                EN_TRANSCATION_DEALSTAUTS iStatus = objEntity.AddByTranscation(strDBIdx, transcationEntityMgr, ref strError);
                if (iStatus == EN_TRANSCATION_DEALSTAUTS.EN_ERROR)
                {
                    Logger.Error("RemoveList", strError = string.Format("Can't delete object with error:[{0}]", strError));
                    return false;
                }
                if (iStatus == EN_TRANSCATION_DEALSTAUTS.EN_IGNORE)
                {
                    Logger.Warnning("RemoveList", strError = string.Format("Can't delete object with ignorable info:[{0}]", strError));
                }
            }
            return true;
        }
    }
}
