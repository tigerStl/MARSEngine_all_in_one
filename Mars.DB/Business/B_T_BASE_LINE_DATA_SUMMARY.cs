using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System.Collections.Generic;
using System.Linq;

namespace Mars.message.Business
{
    public class B_T_BASELINE_DATA_SUMMARY : T_BASELINE_DATA_SUMMARYDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_T_BASELINE_DATA_SUMMARY));

        public Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> getBaseLineDataAndDetails(
            string strDBIdx,
            long iSummaryId)
        {
            Logger.Info("getBaseLineDataAndDetails", string.Format("Data set Summary Id:[{0}]", iSummaryId));
            Model.MarsEntities marsEntities = DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from baseSummary in marsEntities.T_BASELINE_DATA_SUMMARY
                        from baseDetail in marsEntities.T_BASELINE_DATA_DETAIL
                        where baseSummary.DATA_SUMMARY_ID == iSummaryId
                        && baseDetail.DATA_BASE_OBJ_ID == baseSummary.DATA_BASE_OBJ_ID
                        select new
                        {
                            sum = baseSummary,
                            dtl = baseDetail
                        };
            /// convert to DTOs
            Dictionary<T_BASELINE_DATA_SUMMARY, Dictionary<short?, List<T_BASELINE_DATA_DETAIL>>> objResult
                = query.GroupBy(x => x.sum, x => x.dtl)
                .ToDictionary(x => x.Key, x => x.ToList().GroupBy(p => (short?)p.LOOP_ID, p => p).ToDictionary(z => z.Key, z => z.ToList()));
            Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> objResultDto = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();
            foreach (T_BASELINE_DATA_SUMMARY k in objResult.Keys)
            {
                Dictionary<short?, List<T_BASELINE_DATA_DETAIL>> dicSubInfo = objResult[k];
                Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> dicSubInfoDto = new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>();
                foreach (short? s in dicSubInfo.Keys)
                {
                    if (s == null) continue;
                    List<T_BASELINE_DATA_DETAILDTO> lstSub = T_BASELINE_DATA_DETAILAssembler.ToDTOs(dicSubInfo[s]);
                    dicSubInfoDto.Add(s, lstSub);
                }
                objResultDto.Add(T_BASELINE_DATA_SUMMARYAssembler.ToDTO(k), dicSubInfoDto);
            }
            return objResultDto;
        }
    }
}
