using Mars.message.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
#if !_noEntities
using Mars.message.MarsDataStructure.TestResult;
#endif
#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif
using Mars.message.DataLayer;
using System.Data.Common;
#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class B_PROJ_TEST_RESULT : T_PROJ_TEST_RESULTDTO
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(B_PROJ_TEST_RESULT));
#endif
        internal void cloneToEntityWithoutKey(T_PROJ_TEST_RESULT objRslt)
        {
            if (objRslt == null) return;
            objRslt.CREATE_TIME = this.CREATE_TIME;
            objRslt.LATEST_TEST_MARK_ID = this.LATEST_TEST_MARK_ID;
            objRslt.RELY_TEST_CASE_ID = this.RELY_TEST_CASE_ID;
            objRslt.STORYBOARD_DETAIL_ID = this.STORYBOARD_DETAIL_ID;
            objRslt.TESTER_ID = this.TESTER_ID;
            objRslt.TEST_BEGIN_TIME = this.TEST_BEGIN_TIME;
            objRslt.TEST_CASE_ID = this.TEST_CASE_ID;
            objRslt.TEST_END_TIME = this.TEST_END_TIME;
            objRslt.TEST_MODE = this.TEST_MODE;
            objRslt.TEST_RESULT = this.TEST_RESULT;
            objRslt.TEST_RESULT_IN_TEXT = this.TEST_RESULT_IN_TEXT;
        }
#if !_noEntities
        public List<StoryboardHistSummaryInfo> getResultHistInfoByDetailId(string strDBIdx,long storyboardDetailId, ref bool isOk, ref string strError, MarsEntities objDBCntx = null)
        {
            Logger.logBegin("getResultHistInfoByDetailId",string.Format("Storyboard Detail Id:[{0}]", storyboardDetailId));
            List<StoryboardHistSummaryInfo> lstResult = new List<StoryboardHistSummaryInfo>();
            try
            {
                MarsEntities dbCntx = objDBCntx ?? BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
                var q = from his in dbCntx.T_PROJ_TEST_RESULT
                        where his.STORYBOARD_DETAIL_ID == storyboardDetailId
                        select his;
                var qGrp = q.ToList().GroupBy(p => new { p.LATEST_TEST_MARK_ID, p.TEST_MODE }).Select(rslt => new StoryboardHistSummaryInfo
                {
                    Hist_VersionNumber = rslt.Key.LATEST_TEST_MARK_ID ?? -1,
                    Hist_CreateDates = rslt.Select(z => z.TEST_END_TIME).ToList(),
                    Test_ModeMark = rslt.Key.TEST_MODE ?? -1,
                    AliasName = rslt.Select(z => z.RESULT_ALIAS_NAME).FirstOrDefault(),
                    ResultDescription = rslt.Select(z => z.RESULT_DESC).FirstOrDefault(),
                    StoryboardDetailId = storyboardDetailId,
                    Hist_Id = rslt.Select(z => z.HIST_ID).FirstOrDefault(),
                    AssignedDatabaseRecord = rslt.FirstOrDefault() ==null? null :T_PROJ_TEST_RESULTAssembler.ToDTO(rslt.FirstOrDefault())
                });
                lstResult = qGrp == null ? null : qGrp.ToList();
                isOk = true;
                return lstResult;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("getResultHistInfoByDetailId", strError = string.Format("Exception:[{0}]", e.Message), e);
                return lstResult;
            }
            finally {
                Logger.logEnd("getResultHistInfoByDetailId");
            }
        }

        public bool SaveDetailInof(string strDBIdx, List<StoryboardHistSummaryInfo> lstHistData, ref string strError, MarsEntities objDBCntx = null)
        {
            Logger.logBegin("SaveDetailInof",string.Format("Totally [{0}] records to be updated", lstHistData==null?-1:lstHistData.Count));
            try
            {
                MarsEntities dbCntx = objDBCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;
                if (lstHistData==null)
                {
                    Logger.Error("SaveDetailInof", "Data = null, no Data will be saved.");
                    return false;
                }
                List<T_PROJ_TEST_RESULTDTO> lstRecord2BeUpdated = new List<T_PROJ_TEST_RESULTDTO>();                
                lstHistData.ForEach(itm=> {
                    bool isNew = false;
                    if (itm.AssignedDatabaseRecord != null)
                    {
                        T_PROJ_TEST_RESULTDTO objDto = (T_PROJ_TEST_RESULTDTO)itm.AssignedDatabaseRecord;
                        T_PROJ_TEST_RESULTDTO oDto = objDto.ToEntity().ToDTO();
                        if (oDto.LATEST_TEST_MARK_ID != itm.Hist_VersionNumber)
                            isNew = true;
                        oDto.LATEST_TEST_MARK_ID = itm.Hist_VersionNumber;
                        if (oDto.RESULT_ALIAS_NAME != itm.AliasName)
                            isNew = true;
                        oDto.RESULT_ALIAS_NAME = itm.AliasName;
                        if (oDto.TEST_MODE != itm.Test_ModeMark)
                            isNew = true;
                        oDto.TEST_MODE = (short)itm.Test_ModeMark;
                        if (oDto.RESULT_DESC != itm.ResultDescription)
                            isNew = true;
                        oDto.RESULT_DESC = itm.ResultDescription;
                        if (isNew)
                            lstRecord2BeUpdated.Add(oDto);
                        
                    }
                });

                lstRecord2BeUpdated.ForEach(itm => {
                    T_PROJ_TEST_RESULT objEntity = dbCntx.T_PROJ_TEST_RESULT.Where(p => p.HIST_ID == itm.HIST_ID).FirstOrDefault();
                    if (objEntity != null)
                    {
                        
                        dbCntx.Set<T_PROJ_TEST_RESULT>();
                        dbCntx.T_PROJ_TEST_RESULT.Attach(objEntity);
                        objEntity.LATEST_TEST_MARK_ID = itm.LATEST_TEST_MARK_ID;
                        objEntity.RESULT_ALIAS_NAME = itm.RESULT_ALIAS_NAME;
                        objEntity.TEST_MODE = itm.TEST_MODE;
                        objEntity.RESULT_DESC = itm.RESULT_DESC;
                    }
                    
                });
                
                int iCnt = dbCntx.SaveChanges();
                Logger.Info("SaveDetailInof", string.Format("Updated [{0}] records", iCnt));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SaveDetailInof",strError=string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("SaveDetailInof");
            }
        }

        internal static bool UpdateTestResultByConnection(DbConnection dbCnn, B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError)
        {
            Logger.logBegin("UpdateTestResultByConnection", storyBoardTestResult == null ? "" : storyBoardTestResult.HIST_ID + "");
            string strSql = @"UPDATE T_PROJ_TEST_RESULT SET TEST_RESULT_IN_TEXT=:RESULTINTXT , 
                              TEST_RESULT=:TEST_RESULT, TEST_END_TIME=SYSDATE
                             WHERE HIST_ID=:HIST_ID";
            try
            {
                using (DbCommand dbCmd = dbCnn.CreateCommand())
                {
                    dbCmd.CommandText = strSql;
                    DbParameter paraResultInTxt = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    paraResultInTxt.ParameterName = "RESULTINTXT";
                    if (string.IsNullOrEmpty(storyBoardTestResult.TEST_RESULT_IN_TEXT))
                    {
                        paraResultInTxt.Value = storyBoardTestResult.TEST_RESULT != 1 ? "FAILED" : "SUCCESS";
                    }
                    else
                    {
                        paraResultInTxt.Value = storyBoardTestResult.TEST_RESULT_IN_TEXT ;
                    }
                    dbCmd.Parameters.Add(paraResultInTxt);
                    DbParameter paraTestResult = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    paraTestResult.ParameterName = "TEST_RESULT";
                    paraTestResult.Value = storyBoardTestResult.TEST_RESULT;
                    dbCmd.Parameters.Add(paraTestResult);

                    DbParameter paraHIST_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    paraHIST_ID.ParameterName = "HIST_ID";
                    paraHIST_ID.Value = storyBoardTestResult.HIST_ID;
                    dbCmd.Parameters.Add(paraHIST_ID);

                    int iRslt = dbCmd.ExecuteNonQuery();
                    Logger.Info("UpdateTestResultByConnection",string.Format("updated records:[{0}]", iRslt));
                    return true;
                }
            }catch(Exception e)
            {
                Logger.Error("UpdateTestResultByConnection", strError = string.Format("Exception :[{0}]", e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("UpdateTestResultByConnection");
            }
        }
#endif
    }
}
