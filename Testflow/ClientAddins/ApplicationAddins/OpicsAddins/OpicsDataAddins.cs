using Route2NSEx.src.Marquis.systemUtil;
using System;

namespace TestFlowClient.ClientAddins.ApplicationAddins.OpicsAddins
{
    class OpicsDataAddins_ListViewForICAP : MarsClientAddins_Base
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(OpicsDataAddins_ListViewForICAP));

        protected readonly static string[] CNST_ARR_COLUMNS = { "Br", "Server", "Prior", "ProgName", "TableName", "Supplemental", "Description" };
        protected readonly static int[] CNST_ARR_COLUMNS_STRAT = { 0, 4, 24, 28, 37, 49, 62 };
        protected readonly static string[] CNST_ARR_PROCESS_COLUMNS = { "Time", "Br", "Source ID", "OPICS Table", "ExtRefNo", "Seq", "Status" };
        protected readonly static int[] CNST_ARR_PROCESS_COLUMNS_STRAT = { 0, 9, 13, 30, 41, 57, 63 };
        /// <summary>
        /// for Opics, data from Listview, is formatted string.  like 01  SECUR               1   FIDE     BRED                     CAPITAL MARKET                                                                                                                                                                                    
        /// </summary>
        /// <param name="objKeyword"></param>
        /// <param name="objName"></param>
        /// <param name="strRC">formats could be Allrows;ColoumnName or RowLimits:number1:number2;Column</param>
        /// <param name="objDataSrc"></param>
        /// <param name="isOK"></param>
        /// <param name="objError"></param>
        /// <returns></returns>
        public override string GetDataAddins(string objKeyword, string objName, string strRC, string objDataSrc, ref bool isOK, ref string objError)
        {

            Logger.Info("GetDataAddins", string.Format("Keyword:[{0}],object:[{1}], RC:[{2}], Data:[{3}]", objKeyword, objName, strRC, objDataSrc));

            if ((string.IsNullOrEmpty(strRC)) || (string.IsNullOrEmpty(objDataSrc)))
            {
                isOK = true;
                return objDataSrc;
            }
            string[] arrRC = strRC.Split(new string[] { ";" }, StringSplitOptions.None);
            if (arrRC.Length != 2)
            {
                objError = string.Format("unsupported RC format,it could be:[ALLROWS|(ROWLIMIT:NUMBER1:NUMBER2);COLUMNNAME],NOT [{0}]", strRC);
                isOK = false;
                return objDataSrc;
            }
            string[] arrDataToAlyst = null;
            if (strRC.ToUpper().StartsWith("ALLROWS"))
            {
                arrDataToAlyst = objDataSrc.Split(new string[] { "\n" }, StringSplitOptions.None);
            }
            else
            {
                arrDataToAlyst = new string[] { objDataSrc };
            }
            string strResult = "", strTmp;
            int iObjMode = objName == null ? 0 : ("ICAP_INTERFACE_TABLE".CompareTo(objName.ToUpper()) == 0 ? 0 : 1);
            int iColumnIdx = GetColumnIdx(arrRC[1], iObjMode);
            Logger.Info("GetDataAddins", string.Format("Try get data:[{0}], iColumnIdx:[{1}]", arrRC[1], iColumnIdx));
            if (iColumnIdx < 0)
            {
                objError = string.Format("Can't find column [{0}] from [{1}]", arrRC[1], CNST_ARR_COLUMNS);
                isOK = false;
                return objDataSrc;
            }
            int iRow = 0;



            foreach (string strOneRowToChange in arrDataToAlyst)
            {
                //string[] arrColumns = strOneRowToChange.Split(new string[] { " " },StringSplitOptions.RemoveEmptyEntries);
                if (iObjMode == 0)
                {
                    if (iColumnIdx >= CNST_ARR_COLUMNS_STRAT.Length - 1)
                        strTmp = strOneRowToChange.Substring(CNST_ARR_COLUMNS_STRAT[iColumnIdx]);
                    else
                        strTmp = strOneRowToChange.Substring(CNST_ARR_COLUMNS_STRAT[iColumnIdx], CNST_ARR_COLUMNS_STRAT[iColumnIdx + 1] - CNST_ARR_COLUMNS_STRAT[iColumnIdx]);
                }
                else
                {
                    if (iColumnIdx >= CNST_ARR_PROCESS_COLUMNS.Length - 1)
                        strTmp = strOneRowToChange.Substring(CNST_ARR_PROCESS_COLUMNS_STRAT[iColumnIdx]);
                    else
                        strTmp = strOneRowToChange.Substring(CNST_ARR_PROCESS_COLUMNS_STRAT[iColumnIdx], CNST_ARR_PROCESS_COLUMNS_STRAT[iColumnIdx + 1] - CNST_ARR_PROCESS_COLUMNS_STRAT[iColumnIdx]);
                }

                if (iRow == 0)
                {
                    strResult = strTmp;
                    iRow++;
                }
                else
                {
                    strResult += ("\n" + strTmp);
                }

            }
            isOK = true;
            return strResult;
        }

        private int GetColumnIdx(string columnIdx, int iMode)
        {
            if (iMode == 0)
            {
                for (int i = 0; i < CNST_ARR_COLUMNS.Length; i++)
                {
                    if (string.Compare(CNST_ARR_COLUMNS[i], columnIdx, true) != 0) continue;
                    return i;
                }
            }
            else
            {
                for (int i = 0; i < CNST_ARR_PROCESS_COLUMNS.Length; i++)
                {
                    if (string.Compare(CNST_ARR_PROCESS_COLUMNS[i], columnIdx, true) != 0) continue;
                    return i;
                }
            }
            return -1;
        }
    }
}
