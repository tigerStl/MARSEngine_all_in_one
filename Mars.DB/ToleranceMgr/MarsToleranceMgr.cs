using Route2NSEx.src.Marquis.systemUtil;
using System;

namespace Mars.message.Utility.ToleranceMgr
{

    public sealed class MarsToleranceMgr
    {

        //public static bool DealToleranceFunc(string strFuncIdx, string strPara, string strData1, string strData2, ref string strError)
        //{
        //    if (string.IsNullOrEmpty(strFuncIdx))
        //    {
        //        return string.Compare(strData1 ?? "", strData2 ?? "", true) == 0;
        //    }
        //    if (string.Compare(""))
        //}
    }

    public class MarsBasicToleranceFunc
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(MarsBasicToleranceFunc));

        public string FuncName;
        public string[] Parameter;

        public static MarsBasicToleranceFunc FromFuncStringWithPreFix(string strFuncInfo)
        {
            if (string.IsNullOrEmpty(strFuncInfo)) return null;
            if (!strFuncInfo.ToUpper().StartsWith("TOL:")) return null;
            string strFuncWithoutPrefix = strFuncInfo.Substring("TOL:".Length);
            return FromFuncString(strFuncWithoutPrefix);
        }

        public static MarsBasicToleranceFunc FromFuncString(string strFuncInfo)
        {
            string[] arrStr = strFuncInfo.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            MarsBasicToleranceFunc objRslt = new MarsBasicToleranceFunc();
            objRslt.FuncName = arrStr[0];
            objRslt.Parameter = new string[arrStr.Length - 1];
            Array.Copy(arrStr, 1, objRslt.Parameter, 0, objRslt.Parameter.Length);

            return objRslt;

        }

        public string CompareDataAsString(string baseLineData, string noneBaseLineData, ref bool isOk, ref string strError)
        {
            if (string.IsNullOrEmpty(FuncName))
            {
                bool isEqual = string.Compare(baseLineData == null ? "" : baseLineData.Trim(),
                    noneBaseLineData == null ? "" : noneBaseLineData.Trim(), true) == 0;
                if (isEqual)
                {
                    return "FALSE";
                }
                return "TRUE";
            }

            if ("TOL_COMPARE".CompareTo(FuncName.ToUpper()) == 0)
            {
                if ((Parameter == null) || (Parameter.Length != 1))
                {
                    isOk = false;
                    strError = "PARA IS WRONG";
                    return "FALSE" + ", " + strError;
                }


                double d1, d2;
                string strD1 = string.IsNullOrEmpty(baseLineData) ? "" : baseLineData.Replace(",", "");
                string strD2 = string.IsNullOrEmpty(noneBaseLineData) ? "" : noneBaseLineData.Replace(",", "");




                double dC;
                if ((double.TryParse(Parameter[0], out dC))
                    && (double.TryParse(strD1, out d1))
                    && (double.TryParse(strD2, out d2)))
                {

                    isOk = Math.Abs(d1 - d2) <= Math.Abs(dC);
                    if ((string.Compare("2.7705037286852", strD1, true) == 0) || ((string.Compare("2.7705053286172", strD1, true) == 0)))
                    {
                        Logger.Info("--------------", String.Format("{0}-{1}-dc:{2}=isOk:{3}", strD1, strD2, dC, isOk));
                    }
                    //Logger.Info("CompareDataAsString", string.Format("result:[{8}] bs-[{0}-{1}-{2}],non-[{3}-{4}-{5}] compare to:{6}-str [{7}]", baseLineData, strD1, d1,
                    //    noneBaseLineData, strD2, d2, dC, Parameter[0], isOk));
                    strError = null;
                    return isOk ? "TRUE" : "FALSE";
                }
                else
                {
                    if (string.Compare(strD1, strD2) == 0)
                    {
                        isOk = true;
                        return "TRUE";
                    }
                    isOk = false;
                    strError = string.Format("at least one of those is not a number:[delta-{0}], BaseData-[{1}], compareData-[{2}]", dC,
                        baseLineData,
                        noneBaseLineData);
                    return "FALSE," + strError;
                }
            }

            isOk = false;
            return "FALSE," + (strError = string.Format("unsupported tolerance Func:[{0}]", FuncName));
        }
    }
}
