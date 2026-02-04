using Mars.message.DataLayer;
using Mars.message.DataLayer.Generic;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mars.message.Business
{

    public class BSwiftMessage
    {
        public const string cnst_swift_col_name = "_TMP_SWIFT_COL_";
        public DataTable ConvertSwiftToDataTable(string sourceMessage)
        {
            if (string.IsNullOrEmpty(sourceMessage)) return null;
            string[] arrData = sourceMessage.Split(new string[] { "\r\n", "\r", "\n" },StringSplitOptions.None);
            DataTable rslt = new DataTable();
            var col = rslt.Columns.Add(cnst_swift_col_name);
            foreach(var itm in arrData)
            {
                rslt.Rows.Add(new string[] { itm });
            }
            return rslt;
        }
    }

    public class B_Storybard_test_DataSummaryForReport
    {
        public string object_happy_name {
            get;
            set;
        }

        public string steps_id
        {
            get;set;
        }

        public string input_value_setting
        {
            get;set;
        }
        public string obj_type { get; set; }
        public string attachedNamePegBlock { get; set; } //用来表示属于哪一个peg

        public string return_values { get; set; }
        public int run_order { get; set; }
        public int Test_mode { get; set; }
        public int Key_extraId { get; set; }
        public string storyboard_detail_id { get; set; }
        public string dataSetName { get; set; }
        public long datasetId { get; set; }
        public string keyword_name { get; set; }
        public string column_row_setting { get; set; } /// COLUMN_ROW_SETTING

        public string getColumnNameFromParameter()
        {
            if (string.IsNullOrEmpty(column_row_setting)) return null;
            string[] arrPara = column_row_setting.Split(';');
            if (arrPara.Length >= 2) return arrPara[1];
            return null;
        }
        /**
         * 
         * 
         * select 
--rpt.application_id,
stry.hist_id ,
stp.object_id,
mv_obj.object_happy_name,
stp.steps_id,
kwrd.key_word_name,
rptSum.* ,
stry.*
--application_id
from v_test_data_report_summary rptSum,
v_storyboard_test_fullVision stry,
--,获得test的application
t_test_report rpt,
T_test_steps stp,
t_keyword kwrd,
mv_object_snapshot mv_obj
where 
  stry.storyboard_detail_id = rptSum.storyboard_detail_id
--and stry.storyboard_id =   243151
and rptSum.test_mode=stry.hist_test_mode
and rpt.hist_id = stry.hist_id
and stp.steps_id = rptSum.steps_id
and mv_obj.object_id=stp.object_id
--and (upper(kwrd.key_word_name)='CAPTUREANDCOMPARE' OR upper(kwrd.key_word_name)='CAPTUREVALUE')
and (kwrd.key_word_id = 1 or kwrd.key_word_id=77)
and stp.key_word_id = kwrd.key_word_id
and upper(TYPE_NAME)='SWFTABLE'
and stry.storyboard_name='D1 Swap and MM Trade Entry'

order by stry.storyboard_detail_id,stry.Hist_id,stry.run_order
         * */
    }

    public class B_V_STORYBOARD_TEST_FULLVISION : V_STORYBOARD_TEST_FULLVISIONDTO
    {
        public const string CNST_ACTION_EXECUTE = "EXECUTE";
        public const string CNST_ACTION_DONE = "DONE";
        public const string CNST_ACTION_SKIP = "SKIP";
        public const string CNST_ACTION_RUN = "RUN";

        private static MLogger logger = MLogger.GetLogger(typeof(B_V_STORYBOARD_TEST_FULLVISION));

#if !_forWebSvc
        private string getColNameFromCol_Row_Setting(string strColPara)
        {
            if (string.IsNullOrEmpty(strColPara))
            {
                return null;
            }
            string[] arrTmp = strColPara.Split(';');
            if (arrTmp.Length >= 2) return arrTmp[1];
            return strColPara;
        }

        /// <summary>
        /// 最后的DataTable是个数组，因为有baseline也有compare
        /// </summary>
        /// <param name="stoaryBoardId"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns>storyboard_detail_id-objectName-DataTable</returns>
        public Dictionary<string, Dictionary<string, DataTable>> FetchCapturedDataAsDataTable(long stoaryBoardId,string strDBIdx, 
            ref bool isOk, ref string strError)
        {
            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            if (objEntities == null)
            {
                strError = $"Can't get DB Connection for [{strDBIdx}]";
                isOk = false;
                return null;
            }

            /// there are some data setting problems from test cases.
            /// the best way is to build object name based on test case and its input data, object happy name
            /// 1, cache all test cases and its object happy names
            /// 2, fixes object names 


            string strSql = @"select 
                --rpt.application_id,
                mv_obj.TYPE_NAME,
                mv_obj.object_happy_name, 
                stp.object_id,
                stp.steps_id,
                stp.run_order,
                decode(stp.COLUMN_ROW_SETTING, null,''|| stp.run_order, stp.COLUMN_ROW_SETTING) COLUMN_ROW_SETTING,
                kwrd.key_word_name,
                stry.data_set_alias_name,
                stry.data_setting_id,
                decode(rptSum.input_value_setting, null, rptSum.object_happy_name, rptSum.input_value_setting) input_value_setting,
                rptSum.test_mode,
                rptSum.return_values,
                rptSum.storyboard_detail_id ,
                case kwrd.key_word_id                
                when 77 then 1
                when 1 then 1
                when 89 then 2
                else 0
                end  keyExtra   
                --rptSum.* ,
                --stry.*
                --application_id
                from v_test_data_report_summary rptSum,
                v_storyboard_test_fullVision stry,
                --,获得test的application
                t_test_report rpt,
                T_test_steps stp,
                t_keyword kwrd,
                mv_object_snapshot mv_obj
                where 
                  stry.storyboard_detail_id = rptSum.storyboard_detail_id
                and stry.storyboard_id =" + stoaryBoardId+
                @" and rptSum.test_mode=stry.hist_test_mode
                and rpt.hist_id = stry.hist_id
                and stp.steps_id = rptSum.steps_id
                and mv_obj.object_id=stp.object_id
                --and (upper(kwrd.key_word_name)='CAPTUREANDCOMPARE' OR upper(kwrd.key_word_name)='CAPTUREVALUE')
                and (kwrd.key_word_id = 1 or kwrd.key_word_id=77 or kwrd.key_word_id=15 or kwrd.key_word_id=89)
                and stp.key_word_id = kwrd.key_word_id
                and (upper(mv_obj.TYPE_NAME)='SWFTABLE' or upper(mv_obj.TYPE_NAME)='PEGWINDOW')
                --and stry.storyboard_name='D1 Swap and MM Trade Entry'
                order by rptSum.storyboard_detail_id, rptSum.test_mode ,stp.run_order ,rptSum.input_value_setting
                ";
            MarsEntities dbCnnEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            if (dbCnnEntities == null)
            {
                logger.Error("FetchCapturedDataAsDataTable", strError = $"can' get Entities connection for [{strDBIdx}]");
                isOk = false;
                return new Dictionary<string, Dictionary<string, DataTable>>();
            }
            try
            {
                DbConnection cnn = dbCnnEntities.Database.Connection;
                if (cnn.State != ConnectionState.Open)
                {
                    cnn.Open();
                }
                DbCommand dbCmmd = cnn.CreateCommand();
                dbCmmd.CommandText = strSql;
                var reader = dbCmmd.ExecuteReader();
                List<B_Storybard_test_DataSummaryForReport> lstData = new List<B_Storybard_test_DataSummaryForReport>();
                List<B_Storybard_test_DataSummaryForReport> lstByKeyData = new List<B_Storybard_test_DataSummaryForReport>();
                /// 存在相同对象名称，但是属于不同的对象，如swap 的cash_flow,pay 和recv
                /// 
                //bool isRequireReFill = false;
                int preRunOrder = -1, currentRunOrder = -1;
                //long preDetailId = -1, currentDetailId = -1;
                //string currentObjectName = "";
                string currentPegName = "tmp";
                List<B_Storybard_test_DataSummaryForReport> requiredRefill = new List<B_Storybard_test_DataSummaryForReport>();
                while (reader.Read())
                {
                    bool isPeg = false;
                    try
                    {
                        /// 算法：
                        /// 若 isRequireReFill为true，
                        /// 1，即需要判断什么时候需要进行refill
                        /// 条件为，最后一次的当前的run_order比pre的runorder是否大于4？closewindow,pegwindow for main, clickButton or select menu, pegwindow, capture
                        /// 

                        B_Storybard_test_DataSummaryForReport tmpRptData = new B_Storybard_test_DataSummaryForReport();
                        tmpRptData.column_row_setting = reader["COLUMN_ROW_SETTING"] as string;
                        tmpRptData.datasetId = (long)reader["DATA_SETTING_ID"];
                        tmpRptData.obj_type = reader["TYPE_NAME"] as string;
                        tmpRptData.dataSetName = reader["DATA_SET_ALIAS_NAME"] as string;
                        tmpRptData.input_value_setting = reader["INPUT_VALUE_SETTING"] as string;
                        tmpRptData.keyword_name = reader["KEY_WORD_NAME"] as string;
                        tmpRptData.object_happy_name = reader["OBJECT_HAPPY_NAME"] as string;
                        tmpRptData.return_values = reader["RETURN_VALUES"] as string;
                        tmpRptData.run_order = System.Decimal.ToInt32((System.Decimal)(reader["RUN_ORDER"]));
                        tmpRptData.steps_id = reader["STEPS_ID"] as string;
                        tmpRptData.storyboard_detail_id = (long)reader["STORYBOARD_DETAIL_ID"] + "";
                        tmpRptData.Test_mode = (short)reader["TEST_MODE"];
                        tmpRptData.Key_extraId = System.Decimal.ToInt32(((System.Decimal)reader["KEYEXTRA"]));

                        if (tmpRptData.obj_type.Equals("Pegwindow", StringComparison.OrdinalIgnoreCase))
                        {
                            isPeg = true;
                            currentPegName = $"{tmpRptData.object_happy_name}_{tmpRptData.run_order}";
                        }
                        else
                        {
                            isPeg = false;
                        }
                        tmpRptData.attachedNamePegBlock = currentPegName;
                        if (!isPeg)
                        {
                            
                            lstData.Add(tmpRptData);
                        }
                        preRunOrder = currentRunOrder;
                    }
                    catch (Exception e)
                    {

                    }
                }
                reader.Close();
                cnn.Close();

                var groupedLst = (from g in lstData
                        group g by new { g.storyboard_detail_id, g.attachedNamePegBlock, g.object_happy_name,g.Key_extraId } into gg
                        select gg).ToDictionary(p=>p.Key, 
                        p=>p.ToList());
                /// 重新修正对象名称
                /// 
                /// 重新修正对象名称
                /// 
                /// 添加compareBykey处理
                /// 

                foreach (var itm in groupedLst.Keys)
                {
                    if (itm == null) continue;
                    var lst = groupedLst[itm];
                    string strObjNameFixed = itm.object_happy_name;
                    if (itm.Key_extraId == 2) // key compare
                    {
                        /// 算法
                        /// 
                        strObjNameFixed = $"Mars_key:{strObjNameFixed}";
                        lst.ForEach(p => p.object_happy_name = p.Test_mode == 1 ? $"{strObjNameFixed}_B" : $"{strObjNameFixed}_C");
                    }
                    else
                    {                                                
                        if (lst.Any(p => p.input_value_setting.ToUpper().IndexOf("_PAY_") >= 0))
                        {
                            strObjNameFixed = strObjNameFixed + "_PAY";
                        }
                        else if ((lst.Any(p => p.input_value_setting.ToUpper().IndexOf("_RECEIVE_") >= 0))
                           || (lst.Any(p => p.input_value_setting.ToUpper().IndexOf("_RECV_") >= 0)))
                        {
                            strObjNameFixed = strObjNameFixed + "_RECV";
                        }
                        lst.ForEach(p => p.object_happy_name = p.Test_mode == 1 ? $"{strObjNameFixed}_B" : $"{strObjNameFixed}_C");
                    }
                }

                /// storyboard_detail_id - objectName - DataTable
                Dictionary<string, Dictionary<string, DataTable[]>> rslt = new Dictionary<string, Dictionary<string, DataTable[]>>();
                Dictionary<string, Dictionary<string, DataTable>> rsltTmp = new Dictionary<string, Dictionary<string, DataTable>>();
                /// 拼成dataTable
                foreach (var itmg in groupedLst.Keys) //detail id
                {
                    if (itmg == null) continue;
                    var tmpLstData = groupedLst[itmg];
                    if (tmpLstData == null) continue;

                    string strDetailId = $"{itmg.storyboard_detail_id}";
                    Dictionary<string, DataTable> obj_tabls = null;// new Dictionary<string, DataTable>();
                    if (!rsltTmp.ContainsKey(strDetailId))
                    {
                        rsltTmp.Add(strDetailId, obj_tabls = new Dictionary<string, DataTable>());
                    }
                    else
                    {
                        obj_tabls = rsltTmp[strDetailId];
                    }
                    ///
                    // 分成两个list， _B 和 _C
                    var lstBOrC = tmpLstData
                        .Where(p=>p!=null && p.Key_extraId!=2) // extraid!=2 means, not compare by key
                        .GroupBy(p => p.object_happy_name)
                        .ToDictionary(p => p.Key, p => p.OrderBy(x => x.input_value_setting).ToList());
                    /// 先处理 非 comparebykey的
                    List<DataTable> lstTmpBorCtbl = new List<DataTable>();
                    int iTmpColName = 0;
                    foreach (var kBorC in lstBOrC.Keys) // object name-->key
                    {                        
                        if (kBorC == null) continue;
                        
                        var borcTablesDataInList = lstBOrC[kBorC]
                            .Where(p=>p.Key_extraId==1)
                            .GroupBy(p => p.column_row_setting)
                            .ToDictionary(p => p.Key, p => p
                                                            .OrderBy(z=>z
                                                                        .input_value_setting)
                                                                        .ToList()
                            );
                        DataTable tmpDataTbl = new DataTable();
                        iTmpColName = 0;
                        foreach (var tmpCol in borcTablesDataInList.Keys)
                        {
                            DataColumn newCol = null;
                            if (string.IsNullOrEmpty(tmpCol))
                            {
                                newCol = tmpDataTbl.Columns.Add($"_UNKNOW_{iTmpColName++}_");
                            }
                            else
                            {
                                var aCol = getColNameFromCol_Row_Setting(tmpCol);
                                if (aCol != null) 
                                    newCol = tmpDataTbl.Columns.Add(aCol); 
                            }
                            if (newCol == null) continue;
                            // 将list添加到dataTable中
                            if (borcTablesDataInList[tmpCol] == null) continue;
                            for (int j=0;j< borcTablesDataInList[tmpCol].Count; j++)
                            {
                                if (j < tmpDataTbl.Rows.Count)
                                    tmpDataTbl.Rows[j][newCol] = borcTablesDataInList[tmpCol][j].return_values;
                                else
                                {
                                    var newRow = tmpDataTbl.Rows.Add();
                                    newRow[newCol] = borcTablesDataInList[tmpCol][j].return_values;
                                }
                            }
                        }
                        obj_tabls.Add(kBorC, tmpDataTbl);
                        lstTmpBorCtbl.Add(tmpDataTbl);
                    }

                    /// 处理 comparebykey
                    /// parameter-list return values
                    var lstBorCForCompareByKey = tmpLstData
                        .Where(p => p != null && p.Key_extraId == 2)
                        .GroupBy(p=>p.column_row_setting)
                        .ToDictionary(p=>p.Key, p=>p
                                                .OrderBy(x=>x.input_value_setting)
                                                .ToList());
                    string[] arrColFromStepPara = null;

                    
                    
                    foreach (var kCompareDic in lstBorCForCompareByKey.Keys)
                    {
                        if (string.IsNullOrEmpty(kCompareDic)) continue;
                        if (lstBorCForCompareByKey[kCompareDic] == null) continue;
                        if (lstBorCForCompareByKey[kCompareDic].Count <= 0) continue;

                        Dictionary<string, DataTable> tmpBCCmpKyDic = new Dictionary<string, DataTable>();
                        var lstCmpKeyData = lstBorCForCompareByKey[kCompareDic];
                        if (lstCmpKeyData == null) continue;
                        var lstTargetDataTableNames = lstCmpKeyData.Select(p => p.object_happy_name)
                            .Distinct()
                            .ToList();
                        if (lstTargetDataTableNames == null) continue;
                        foreach(var itmTmpTblName in lstTargetDataTableNames)
                        {
                            // 给该对象table取名
                            if (string.IsNullOrEmpty(itmTmpTblName)) continue;
                            string strExtractedObjName = ExtractObjNameFromFixedObjName(itmTmpTblName, ref isOk, ref strError);
                            if (!isOk) return null;
                            string strCmpTableName = $"_CMPKEY_{strExtractedObjName}";// _" + (firstData.Test_mode == 1 ? "_B" : "_C");                        

                            tmpBCCmpKyDic.Add(strCmpTableName, new DataTable());
                        }

                        var firstData = lstBorCForCompareByKey[kCompareDic][0];

                        //string strExtractedObjName = ExtractObjNameFromDataReturned(firstData.input_value_setting,firstData.object_happy_name, ref isOk, ref strError);
                        //if (!isOk) return null;
                        DataTable dtCompareKey = null;
                        /// create columns
                        //string strCmpTableName = $"_CMPKEY_{strExtractedObjName}";// _" + (firstData.Test_mode == 1 ? "_B" : "_C");                        
                        arrColFromStepPara = ExtractColumnFromCompareByKeyPara(kCompareDic, ref isOk, ref strError);
                        if ((!isOk) || (arrColFromStepPara == null))
                        {
                            return null;
                        }
                        foreach (var k in tmpBCCmpKyDic.Keys)
                        {
                            dtCompareKey = tmpBCCmpKyDic[k];
                            foreach (var colName in arrColFromStepPara)
                            {
                                dtCompareKey.Columns.Add(colName);
                            }
                        }
                        
                        //insert rows
                        foreach (var tmpRow in lstBorCForCompareByKey[kCompareDic])
                        {
                            if (tmpRow == null) continue;
                            string objName = ExtractObjNameFromFixedObjName(tmpRow.object_happy_name, ref isOk,ref strError);
                            if (!isOk) // it should never happen, but for safe
                                continue;
                            string strTableNameIdx = $"_CMPKEY_{objName}";
                            if (!tmpBCCmpKyDic.ContainsKey(strTableNameIdx)) // that should  never happen too
                                continue;
                            dtCompareKey = tmpBCCmpKyDic[strTableNameIdx];

                            string[] arrCells = ExctractCellsDataFromReturnValues(tmpRow.input_value_setting, tmpRow.return_values, ref isOk, ref strError);
                            if (!isOk) continue ;
                            for(int i=0; i < arrCells.Length; i++)
                            {
                                arrCells[i] = arrCells[i].Replace("]", "");
                            }
                            dtCompareKey.Rows.Add(arrCells);
                        }
                        foreach (var k in tmpBCCmpKyDic.Keys)
                        {
                            obj_tabls.Add(k, tmpBCCmpKyDic[k]);
                        }
                    }
                    
                    ///// 判断是否已经有该对象
                    ///// 
                    //foreach (var oneReturnedValue in groupedLst[itmg])
                    //{
                    //    if (oneReturnedValue == null) continue;
                    //    DataTable obj_dataTable = null;
                    //    if (!obj_tabls.ContainsKey(oneReturnedValue.object_happy_name))
                    //    {
                    //        obj_tabls.Add(oneReturnedValue.object_happy_name, obj_dataTable = new DataTable());
                    //    }
                    //    else
                    //    {
                    //        obj_dataTable = obj_tabls[oneReturnedValue.object_happy_name];
                    //    }
                    //    //if (obj_dataTable.Columns.Contains())
                    //}
                }
                return rsltTmp;
                #region old codes
                //lstData = lstData.OrderBy(p => p.storyboard_detail_id).ThenBy(p => p.Test_mode).ThenBy(p => p.object_happy_name).ThenBy(p => p.input_value_setting).ToList();


                ////组成结果
                ///// storyboard_detail_id-objectName-DataTable
                ///// 中间结构：storyboard_detail_id-objectName-column-return values
                //Dictionary<string, Dictionary<string, Dictionary<string, List<string>[]>>> rsltInter = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>[]>>>();
                //foreach(var itm in lstData)
                //{
                //    if (itm == null) continue;
                //    Dictionary<string, Dictionary<string, List<string>[]>> assignedTable = null;
                //    if (!rsltInter.ContainsKey(itm.storyboard_detail_id))
                //    {
                //        rsltInter.Add(itm.storyboard_detail_id, assignedTable = new Dictionary<string, Dictionary<string, List<string>[]>>());
                //    }
                //    assignedTable = rsltInter[itm.storyboard_detail_id];
                //    Dictionary<string, List<string>[]> dtTblInter = null;
                //    if (!assignedTable.ContainsKey(itm.object_happy_name))
                //    {
                //        assignedTable.Add(itm.object_happy_name, new Dictionary<string, List<string>[]>());
                //    }
                //    dtTblInter = assignedTable[itm.object_happy_name];

                //    ///判断是否已经存在该column
                //    ///
                //    string colFromPara = itm.getColumnNameFromParameter();
                //    if (string.IsNullOrEmpty(colFromPara))
                //    {
                //        logger.Warnning("\t", $"can't deal with [step id-{itm.steps_id}]--[{itm.keyword_name}('{itm.object_happy_name}','{itm.column_row_setting}','{itm.return_values}')]");
                //        continue;
                //    }
                //    List<string> lstReturnData = new List<string>();
                //    if (!dtTblInter.ContainsKey(colFromPara))
                //    {
                //        dtTblInter.Add(colFromPara, new List<string>[]{
                //            new List<string>(),
                //            new List<string>() });
                //    }
                //    if (itm.Test_mode==0)
                //        lstReturnData = dtTblInter[colFromPara][0];// test mode =0 
                //    else
                //        lstReturnData = dtTblInter[colFromPara][1];
                //    lstReturnData.Add(itm.return_values);
                //}
                ///// 将数据排序，然后构建Datatable
                ///// 
                
                //foreach (var itm in rsltInter.Keys) /// detail id, object name, data
                //{
                //    if (itm == null) continue;
                //    var vDataList = rsltInter[itm]; // -- object name, data
                //    if (vDataList == null) continue;
                //    Dictionary<string, DataTable[]> targetDatTbl = new Dictionary<string, DataTable[]>(); 
                //    rslt.Add(itm, targetDatTbl); // 添加 detail id, Dictionary(object name, 数据表)
                //    // keys in vDataList.dictionary是table的column
                //    foreach (var itmTable in vDataList.Keys)  // 对象名称，数据
                //    {
                //        // key 表示表的名称，比如cash_flow
                //        if (itmTable == null) continue;
                //        DataTable[] dtTbl = new DataTable[2] {new DataTable(), new DataTable()};
                        
                //        targetDatTbl.Add(itmTable, dtTbl); // itmTable是对象名称
                //        // vDAta
                //        // 
                //        insertColumnAndHeaderToTable(vDataList[itmTable], dtTbl);
                //    }
                //}
                //return rslt;
                #endregion
            }
            catch (Exception e)
            {
                logger.Error("FetchCapturedDataAsDataTable", strError =e.Message, e);
                isOk = false;
                return new Dictionary<string, Dictionary<string, DataTable>>();
            }
        }

        private string[] ExctractCellsDataFromReturnValues(string strKeyPart, string strTargetPart, ref bool isOk, ref string strError)
        {
            if (strKeyPart == null)
            {
                isOk = false;
                strError = "No Input_value_setting data is passed";
                return null;
            }
            const string cnst_idx_split = "_[";
            string[] arrRslt = strKeyPart.Split(new string[] { cnst_idx_split }, StringSplitOptions.None); 
            if (arrRslt.Length <= 1)
            {
                isOk = false;
                strError = "Can't split data by ['_[']";
                return null;
            }
            arrRslt[0] = arrRslt[0].Replace("_", "");
            //re move the first item
            arrRslt = arrRslt
                .Skip(1)
                .Concat(new string[] { strTargetPart })
                .ToArray();
            isOk = true;
            return arrRslt;
        }
        private string ExtractObjNameFromFixedObjName(string fixObjName, ref bool isOk, ref string strError)
        {
            if ((!fixObjName.ToUpper().StartsWith("MARS_KEY:")))
            {
                isOk = false;
                strError = "object name information is wrong";
                return null;
            }
            string tmpNameFromFixedName = fixObjName.Substring("MARS_KEY:".Length);
            isOk = true;
            return tmpNameFromFixedName;
        }

        private string ExtractObjNameFromDataReturned(string strDataReturnsIdx, string fixedObjName, ref bool isOk, ref string strError)
        {
            
            if ((string.IsNullOrEmpty(fixedObjName))||(!fixedObjName.ToUpper().StartsWith("MARS_KEY:")))
            {
                isOk = false;
                strError = "object name information is wrong";
                return null;
            }
            string tmpNameFromFixedName = fixedObjName.Substring("MARS_KEY:".Length);
            isOk = true;
            return tmpNameFromFixedName;
            //if (string.IsNullOrEmpty(strDataReturnsIdx))
            //{
            //    isOk = false;
            //    strError = "No Data is set at the Test step";
            //    return null;
            //}
            //const string cnst_nameIdx = "__[";
            //int iPos = strDataReturnsIdx.IndexOf(cnst_nameIdx);
            //if (iPos == 0)
            //{
            //    isOk = false;
            //    strError = "Not set Data for the test step.";
            //    return null;
            //}
            //if (iPos<0)
            //{
            //    isOk = true;
            //    return strDataReturnsIdx;
            //}

            //string strObjName = strDataReturnsIdx.Substring(0, iPos);            
            //isOk = true;
            //if (strObjName.Equals("_")) return tmpNameFromFixedName;
            //return strObjName;
        }

        private string[] ExtractColumnFromCompareByKeyPara(string strCompareByKeyPara,ref bool isOk, ref string strError)
        {
            const string cnst_idxKey = "KEY:[";
            // strCompareByKeyPara 示例：ALLROWS:KEY:[Posting Date;BookingDate;Value Date;GL Date;Account ID;Account Type;Currency;Debit Credit;Event Type];Amount
            isOk = false;
            if (string.IsNullOrEmpty(strCompareByKeyPara))
            {
                strError = "Parameter the step is blank or NULL";
                return null;
            }
            int idx = strCompareByKeyPara.ToUpper().IndexOf(cnst_idxKey);
            if (idx < 0)
            {
                strError = $"Parameter format is not right, it should be :ALLROWS;KEY:[.....;.....];TARGET_column, but is :[{strCompareByKeyPara}]";
                return null;
            }
            string strSubColumns = strCompareByKeyPara.Substring(idx + cnst_idxKey.Length);
            // 删除最后一个 ]
            strSubColumns = strSubColumns.Replace("]", "");
            string[] arrRslt = strSubColumns.Split(new string[] { ";" }, StringSplitOptions.None);
            isOk = true;
            return arrRslt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_value_setting"></param>
        /// <param name="strDefault"></param>
        /// <param name="requireReFill">是不是需要回改，通过run_order判断</param>
        /// <returns></returns>
        public string CorrectObjectName(string input_value_setting, string strDefault, ref bool requireReFill)
        {
            if (string.IsNullOrEmpty(input_value_setting)) return strDefault;
            int iPos = input_value_setting.LastIndexOf("_");
            if (iPos <= -1) return strDefault;
            if (iPos ==  0) return strDefault;
            string last = input_value_setting.Substring(iPos+1);
            int tmpIdx=-1;
            if (int.TryParse(last, out tmpIdx))
            {
                //最后是数字
                string tmpName = input_value_setting.Substring(0, iPos);
                //需要去掉另外一个 _ 后缀
                iPos = tmpName.LastIndexOf("_");
                if (iPos <= -1) return tmpName;
                if (iPos == 0) return tmpName;
                if (input_value_setting.StartsWith("CASHFLOW"))
                {
                    Regex rx = new Regex(@"_(PAY|RECEIVE){1}_\S+_[0-9]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    if (!rx.IsMatch(input_value_setting))
                    {
                        requireReFill = true;
                        return tmpName;
                    }
                }
                requireReFill = false;
                return tmpName.Substring(0, iPos);
            }
            else
            {
                // 最后不是数字
                int tmpIpos = input_value_setting.LastIndexOf("_");                
                if (tmpIpos<=-1)
                    return input_value_setting.Substring(0, iPos);

                string strTmpPre = input_value_setting.Substring(0, tmpIpos);
                tmpIpos = strTmpPre.LastIndexOf("_");
                if (tmpIpos <= -1) {
                    requireReFill = true;
                    return strTmpPre;
                }

                string checkPayOrReceive = strTmpPre.Substring(tmpIpos + 1);
                if ((checkPayOrReceive.Equals("PAY",StringComparison.OrdinalIgnoreCase))
                    ||(checkPayOrReceive.Equals("RECEIVE", StringComparison.OrdinalIgnoreCase)))
                {
                    requireReFill = false;
                    // 如果是receive或者pay的花，就保留 
                    return strTmpPre;
                }
                requireReFill = true ;
                return input_value_setting.Substring(0, iPos);
            }
        }

        private void insertColumnAndHeaderToTable(Dictionary<string, List<string>[]> captionAndDataList, DataTable[] dtTbl)
        {
            if (captionAndDataList == null) return;
            int iUnNameIdx = 0;
            int colIdx = 0;
            foreach (var caption in captionAndDataList.Keys)
            {
                string targetCaption = caption;
                if (string.IsNullOrEmpty(targetCaption))
                {
                    targetCaption = $"_UN_NAME_{iUnNameIdx++}";
                }
                List<string>[] lstData = captionAndDataList[caption];                
                colIdx = 0;
                /// 算法
                /// 1，循环List<string>[]
                
                foreach(var lstToInsert in lstData)
                {
                    DataColumn newCol = null; 
                    if (dtTbl[colIdx].Columns.Contains(targetCaption))
                    {
                        newCol = dtTbl[colIdx].Columns.Add($"{targetCaption}_{dtTbl[colIdx].Columns.Count}");
                    }
                    else
                        newCol = dtTbl[colIdx].Columns.Add(targetCaption);
                    if (lstToInsert == null) continue;
                    //排序
                    lstToInsert.Sort( );

                    for (int i = 0; i < lstToInsert.Count; i++)
                    {
                        if (i < dtTbl[colIdx].Rows.Count)
                        {
                            dtTbl[colIdx].Rows[i][newCol] = lstToInsert[i];
                        }
                        else
                        {
                            var newRow = dtTbl[colIdx].Rows.Add();
                            newRow[newCol] = lstToInsert[i];
                        }
                    }
                    colIdx += 1;
                }
                
            }
        }
#endif

        public void SetRunValueByString(string strCmd)
        {
            if (string.IsNullOrEmpty(strCmd))
            {
                this.TEST_RUN_VALUE = 1;//Execute for default ;
                return;
            }
            switch (strCmd.ToUpper())
            {
                case CNST_ACTION_DONE:
                    this.TEST_RUN_VALUE = 8;
                    break;
                case CNST_ACTION_SKIP:
                    this.TEST_RUN_VALUE = 4;
                    break;
                case CNST_ACTION_RUN:
                    this.TEST_RUN_VALUE = 2;
                    break;
                default:
                    this.TEST_RUN_VALUE = 1;
                    break;
            }
        }

#if !_forWebSvc
        public static List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryBoards(Int64? iStory, 
            string strDBIdx ) //= MarsEntitiesExtends.cnst_default_dbName)
#else
        public List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryBoards(Int64? iStory,string strDBIdx)
#endif
        {
            logger.logBegin("GetStoryBoards", string.Format("id:[{0}]", iStory ?? -1));
            try
            {
                List<V_STORYBOARD_TEST_FULLVISION> lstResult = null; //new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
                MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION> objMarsData = new MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION>(strDBIdx);
                if (iStory == null)
                    lstResult = (List<V_STORYBOARD_TEST_FULLVISION>)(objMarsData.GetAll().OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_DETAIL_ID).ToList<V_STORYBOARD_TEST_FULLVISION>());
                else
                    lstResult = (List<V_STORYBOARD_TEST_FULLVISION>)(objMarsData.GetList(d => d.STORYBOARD_ID == iStory, null).OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_DETAIL_ID).ThenByDescending(p => p.HIST_TEST_MODE).ToList<V_STORYBOARD_TEST_FULLVISION>());

                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstRsltDto = new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
                foreach (var itm in lstResult)
                {
                    int idx = lstRsltDto.FindIndex(p => p.RUN_ORDER == itm.RUN_ORDER);
                    if (idx == -1)
                    {
                        lstRsltDto.Add(itm.ToDTO());
                        continue;
                    }
                }
                logger.logEnd("GetStoryBoards");
                return lstRsltDto;
            }
            catch (Exception e)
            {
                logger.Error("GetStoryBoards", string.Format("Exceptions:[{0}]", e.Message), e);
                return null;
            }

        }

        public static List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryboardsAllMode(string strDBIdx, Int64? iStory)
        {
            logger.logBegin("GetStoryboardsAllMode", string.Format("id:[{0}]", iStory ?? -1));
            try
            {
                MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                var s = (from q in objEntities.V_STORYBOARD_TEST_FULLVISION
                         where q.STORYBOARD_ID == iStory
                         orderby new { q.RUN_ORDER, q.HIST_TEST_MODE }
                         select q).ToDTOs();

                return s;
            }
            catch (Exception e)
            {
                logger.Error("GetStoryboardsAllMode", e.Message, e);
                return new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
            }
            finally
            {
                logger.logEnd("GetStoryboardsAllMode");
            }

        }
#if !(_forWebSvc || _forWebClient)
        public static List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryBoards(Int64? iStory, bool isBase, 
            string currentDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public List<V_STORYBOARD_TEST_FULLVISIONDTO> GetStoryBoards(Int64? iStory, bool isBase,string currentDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            logger.logBegin("GetStoryBoards", string.Format("id:[{0}],dbIdx:[{1}]", iStory ?? -1, currentDBIdx));
            try
            {
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstRsltDto = new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
#if !_forWebClient  //svc and normal mode
                List<V_STORYBOARD_TEST_FULLVISION> lstResult = null; //new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
                MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION> objMarsData = new MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION>(currentDBIdx);
                if (iStory == null)
                    lstResult = (List<V_STORYBOARD_TEST_FULLVISION>)(objMarsData.GetAll().OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_DETAIL_ID).ToList<V_STORYBOARD_TEST_FULLVISION>());
                else
                    lstResult = (List<V_STORYBOARD_TEST_FULLVISION>)(objMarsData.GetList(d => d.STORYBOARD_ID == iStory, null).OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_DETAIL_ID).ThenByDescending(p => p.HIST_TEST_MODE).ToList<V_STORYBOARD_TEST_FULLVISION>());
#else
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstResult = null; //new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
                B_STORYBOARD_TEST_FULLVISION stryboardVsn = new B_STORYBOARD_TEST_FULLVISION();
                bool isOk = false;
                string strError = "";
                lstResult = stryboardVsn.GetStoryboardByIdViaWebApi(currentDBIdx,iStory, ref isOk, ref strError);
                if ((!isOk) || (lstResult == null))
                {
                    return lstRsltDto;
                }
#endif
                foreach (var itm in lstResult)
                {
                    int idx = lstRsltDto.FindIndex(p => p.RUN_ORDER == itm.RUN_ORDER);
#if !_forWebClient
                    if (idx == -1)
                    {
                        lstRsltDto.Add(itm.ToDTO());
                        continue;
                    }
                    var tmpItm = lstRsltDto[idx];
                    if (tmpItm == null)
                    {
                        lstRsltDto.Add(itm.ToDTO());
                        continue;
                    }

                    if (tmpItm.HIST_TEST_MODE == (isBase ? 1 : 2))
                    {
                        continue;
                    }
                    lstRsltDto[idx] = itm.ToDTO();
#else
                    if (idx == -1)
                    {
                        lstRsltDto.Add(itm);
                        continue;
                    }
                    var tmpItm = lstRsltDto[idx];
                    if (tmpItm == null)
                    {
                        lstRsltDto.Add(itm);
                        continue;
                    }

                    if (tmpItm.HIST_TEST_MODE == (isBase ? 1 : 2))
                    {
                        continue;
                    }
                    lstRsltDto[idx] = itm;
#endif
                }

                //lstRsltDto = V_STORYBOARD_TEST_FULLVISIONAssembler.ToDTOs(lstResult);            
                logger.logEnd("GetStoryBoards");
                return lstRsltDto;
            }
            catch (Exception e)
            {
                logger.Error("GetStoryBoards", string.Format("Exceptions:[{0}]", e.Message), e);
                return null;
            }

        }

        internal static List<V_STORYBOARD_TEST_FULLVISIONDTO> GetTestCasesByStoryBoardAndRunTypes(string strStoryBoardId,
            int?[] arr_iRunTypeFilter,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            logger.logBegin("GetStoryBoards");
            //List<V_STORYBOARD_TEST_FULLVISION> lstResult = null; //new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
            MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION> objMarsData = new MarsDataAccessLayer<V_STORYBOARD_TEST_FULLVISION>(strDBIdx);
            int iStoryBoardId;

            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            bool isAInt = int.TryParse(strStoryBoardId, out iStoryBoardId);
            //BuildOrExpression
            if (isAInt)
            {
                var query = (from v in objEntities.V_STORYBOARD_TEST_FULLVISION
                             where v.STORYBOARD_ID == iStoryBoardId
                             && (arr_iRunTypeFilter.Contains(v.TEST_RUN_VALUE))
                             orderby v.STORYBOARD_ID, v.RUN_ORDER
                             select v).OrderBy(p => p.RUN_ORDER);
                IList<V_STORYBOARD_TEST_FULLVISION> LstEntity = query.ToList<V_STORYBOARD_TEST_FULLVISION>();
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstRslt = new List<V_STORYBOARD_TEST_FULLVISIONDTO>();

                foreach (var itm in LstEntity)
                {
                    if (lstRslt.Any(p => p.RUN_ORDER == itm.RUN_ORDER)) continue;
                    lstRslt.Add(itm.ToDTO());
                }
                logger.Info("GetTestCasesByStoryBoardAndRunTypes", string.Format("Source count:[{0}] filtered Count:[{1}]", LstEntity.Count, lstRslt.Count));
                return lstRslt;// V_STORYBOARD_TEST_FULLVISIONAssembler.ToDTOs(LstEntity);
            }
            else
            {

            }
            return null;

        }

        public Dictionary<T_STORYBOARD_SUMMARYDTO, List<V_STORYBOARD_TEST_FULLVISIONDTO>> GetStoryboardInfoAndDetailByProjectId(string strDBIdx, 
            long lProjId, ref bool isOk, ref string strErrorOrHint)
        {
            logger.logBegin("GetStoryboardInfoAndDetailByProjectId", string.Format("Project Id:[{0}]", lProjId));
            try
            {
                MarsEntities objDBCntx = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var q = from dtl in objDBCntx.V_STORYBOARD_TEST_FULLVISION
                        from s in objDBCntx.T_STORYBOARD_SUMMARY
                        where dtl.PROJECT_ID == lProjId
                        && dtl.STORYBOARD_ID == s.STORYBOARD_ID
                        select new
                        {
                            sb = s,
                            sb_dtl = dtl
                        };
                Dictionary<T_STORYBOARD_SUMMARY, List<V_STORYBOARD_TEST_FULLVISION>> dictEntityData = q.GroupBy(p => p.sb, p => p.sb_dtl).ToDictionary(p => p.Key, z => z.ToList());
                // convert to DTO result
                Dictionary<T_STORYBOARD_SUMMARYDTO, List<V_STORYBOARD_TEST_FULLVISIONDTO>> dictResult = new Dictionary<T_STORYBOARD_SUMMARYDTO, List<V_STORYBOARD_TEST_FULLVISIONDTO>>();
                isOk = true;
                if (dictEntityData.Keys == null)
                    return dictResult;
                foreach (T_STORYBOARD_SUMMARY itm in dictEntityData.Keys)
                {
                    if (itm == null) continue;
                    T_STORYBOARD_SUMMARYDTO objDto = itm.ToDTO();
                    if (dictEntityData[itm] == null) continue;
                    List<V_STORYBOARD_TEST_FULLVISIONDTO> lstDtl = dictEntityData[itm].ToDTOs().ToList();
                    dictResult.Add(objDto, lstDtl);
                }
                return dictResult;
            }
            catch (Exception e)
            {
                isOk = false;
                logger.Error("GetStoryboardInfoAndDetailByProjectId", strErrorOrHint = string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                logger.logEnd("GetStoryboardInfoAndDetailByProjectId");
            }
        }

        public static B_V_STORYBOARD_TEST_FULLVISION CopyFromDto(V_STORYBOARD_TEST_FULLVISIONDTO objSrc)
        {
            if (objSrc == null) return null;
            B_V_STORYBOARD_TEST_FULLVISION objB = new B_V_STORYBOARD_TEST_FULLVISION();
            objB.STORYBOARD_ID = objSrc.STORYBOARD_ID;
            objB.STORYBOARD_DETAIL_ID = objSrc.STORYBOARD_DETAIL_ID;
            objB.STORYBOARD_NAME = objSrc.STORYBOARD_NAME;
            objB.PROJECT_ID = objSrc.PROJECT_ID;
            objB.PROJECT_NAME = objSrc.PROJECT_NAME;
            objB.PROJECT_DESCRIPTION = objSrc.PROJECT_DESCRIPTION;
            objB.TEST_CASE_NAME = objSrc.TEST_CASE_NAME;
            objB.TEST_CASE_ID = objSrc.TEST_CASE_ID;
            objB.TEST_STEP_DESCRIPTION = objSrc.TEST_STEP_DESCRIPTION;
            objB.TEST_SUITE_ID = objSrc.TEST_SUITE_ID;
            objB.TEST_SUITE_NAME = objSrc.TEST_SUITE_NAME;
            objB.TEST_SUITE_DESCRIPTION = objSrc.TEST_SUITE_DESCRIPTION;
            objB.RUN_ORDER = objSrc.RUN_ORDER;
            objB.DEPENDS_ON = objSrc.DEPENDS_ON;
            objB.ALIAS_NAME = objSrc.ALIAS_NAME;
            objB.PARENT_ALIAS_NAME = objSrc.PARENT_ALIAS_NAME;
            objB.DISPLAY_NAME = objSrc.DISPLAY_NAME;
            objB.TEST_RUN_VALUE = objSrc.TEST_RUN_VALUE;
            objB.LATEST_TEST_MARK_ID = objSrc.LATEST_TEST_MARK_ID;
            objB.HIST_LATEST_TEST_MARK_ID = objSrc.HIST_LATEST_TEST_MARK_ID;
            objB.HIST_ID = objSrc.HIST_ID;
            objB.HIST_TEST_ID = objSrc.HIST_TEST_ID;
            objB.TEST_CASE_BEGIN_TIME = objSrc.TEST_CASE_BEGIN_TIME;
            objB.TEST_CASE_END_TIME = objSrc.TEST_CASE_END_TIME;
            objB.HIST_TEST_RESULT_IN_TEXT = objSrc.HIST_TEST_RESULT_IN_TEXT;
            objB.HIST_TEST_MODE = objSrc.HIST_TEST_MODE;
            objB.HIST_RESULT = objSrc.HIST_RESULT;
            objB.DATA_SETTING_ID = objSrc.DATA_SETTING_ID;
            objB.DATA_SUMMARY_ID = objSrc.DATA_SUMMARY_ID;
            objB.DATA_SET_CREATETIME = objSrc.DATA_SET_CREATETIME;
            objB.DATA_SET_TESTERID = objSrc.DATA_SET_TESTERID;
            objB.DATA_SET_VERSION = objSrc.DATA_SET_VERSION;
            objB.DATA_SET_ALIAS_NAME = objSrc.DATA_SET_ALIAS_NAME;
            objB.DATA_SET_SHARE_MARK = objSrc.DATA_SET_SHARE_MARK;
            objB.DATA_SET_STATUS = objSrc.DATA_SET_STATUS;
            objB.DATA_SET_TYPE = objSrc.DATA_SET_TYPE;
            objB.DATASET_DESCRIPTION = objSrc.DATASET_DESCRIPTION;

            return objB;
        }
#if !_forWebSvc
        public static bool UpdateDepends(IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> lstStoryBoardToChange,
            string strAction, string strDefaultAction2,
            ref string strError,
            string strDBIdx)
#else
        public bool UpdateDepends(IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> lstStoryBoardToChange, 
            string strAction, string strDefaultAction2, 
            ref string strError,
            string strDBIdx)
#endif
        {
#if !_forWebClient
            DbConnection dbCnn = null;
            DbTransaction trans = null;
            logger.logBegin("UpdateDepends");
            try
            {
                if (lstStoryBoardToChange == null) return true;
                List<long> lstStoryboardDetailId = lstStoryBoardToChange.Select(p => p.STORYBOARD_DETAIL_ID).ToList();

                logger.Info("UpdateDepends", string.Format("records to be update:[{0}]", lstStoryboardDetailId.Count));
                MarsEntities DbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

                if ((dbCnn = DbCntx.Database.Connection).State != ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                trans = dbCnn.BeginTransaction();
                bool isOk = B_PROJ_TC_MGR.updateRunTypeByIdsViaConnection(lstStoryboardDetailId, strAction, dbCnn, ref strError);
                if (!isOk)
                    trans.Rollback();
                else
                    trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                logger.Error("UpdateDepends", strError = e.Message, e);
                if (trans != null)
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception)
                    {

                    }
                }
                return false;
            }
            finally
            {
                logger.logEnd("UpdateDepends");
            }
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = clnt.StoryboarTestFullVision_UpdateDepends(lstStoryBoardToChange, strAction, strDefaultAction2, ref strError);
            return isOk;
#endif
        }

    }
}
