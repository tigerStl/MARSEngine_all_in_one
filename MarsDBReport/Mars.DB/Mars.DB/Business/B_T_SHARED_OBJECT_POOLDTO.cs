
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    internal class B_T_SHARED_OBJECT_POOLDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_T_SHARED_OBJECT_POOLDTO));

        internal static IList<T_SHARED_OBJECT_POOLDTO> GetSharedObjectPoolInfoByDataSummaryId(string strDBIdx, long dATA_SUMMARY_ID)
        {
            Logger.Info("GetSharedObjectPoolInfoByDataSummaryId", string.Format("DataSummaryId:[{0}]", dATA_SUMMARY_ID));
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from objPool in objEntities.T_SHARED_OBJECT_POOL
                        where objPool.DATA_SUMMARY_ID == dATA_SUMMARY_ID
                        select objPool;

            return T_SHARED_OBJECT_POOLAssembler.ToDTOs(query);
        }
    }
}
