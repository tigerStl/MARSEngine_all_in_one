using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.message.Business
{
    public class B_V_BASE_LINEDATA : V_BASE_LINEDATADTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_V_BASE_LINEDATA));
        public List<V_BASE_LINEDATADTO> GetBaseLindDataViaDataSummaryId(long lDataSummaryId, ref string strError, ref bool hasError)
        {
            Logger.Info("GetBaseLindDataViaDataSummaryId", string.Format("trying to get data with summaryId:[{0}]", lDataSummaryId));

            try
            {
                MarsEntities objDb = BoHelper.GetMarsEntitiesInstance(false);
                var query = from objBaseDataView in objDb.V_BASE_LINEDATA
                            where
                                objBaseDataView.DATA_SUMMARY_ID == lDataSummaryId
                            orderby objBaseDataView.OBJECT_HAPPY_NAME, objBaseDataView.LOOP_ID
                            select objBaseDataView;
                hasError = false;
                return V_BASE_LINEDATAAssembler.ToDTOs(query.ToList());
            }
            catch (Exception e)
            {
                Logger.Error("GetBaseLindDataViaDataSummaryId", strError = string.Format("Exception:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                hasError = true;
                return null;
            }
        }

        public List<V_BASE_LINEDATADTO> GetBaseLindDataViaStoryboardDetailId(long? currentStoryBoardDetailId, ref string strError, ref bool hasError)
        {
            Logger.Info("GetBaseLindDataViaStoryboardDetailId", string.Format("Try to get data via storyboard id:[{0}]", currentStoryBoardDetailId));
            try
            {
                MarsEntities objDb = BoHelper.GetMarsEntitiesInstance(false);
                var query = from objBaseData in objDb.V_BASE_LINEDATA
                            from objStoryBoard in objDb.T_STORYBOARD_DATASET_SETTING
                            where
                                 objStoryBoard.DATA_SUMMARY_ID == objBaseData.DATA_SUMMARY_ID
                             && objStoryBoard.STORYBOARD_DETAIL_ID == currentStoryBoardDetailId
                            orderby objBaseData.OBJECT_HAPPY_NAME, objBaseData.LOOP_ID
                            select objBaseData;
                hasError = false;
                return V_BASE_LINEDATAAssembler.ToDTOs(query.ToList());
            }
            catch (Exception e)
            {

                Logger.Error("GetBaseLindDataViaDataSummaryId", strError = string.Format("Exception:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                hasError = true;
                return null;
            }
        }
    }
}
