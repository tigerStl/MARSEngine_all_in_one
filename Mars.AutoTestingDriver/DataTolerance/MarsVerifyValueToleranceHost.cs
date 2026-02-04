using Mars.plugins.standards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.AutoTestingDriver.DataTolerance
{
    internal class TestStepDataPrepare
    {
        enum e_prefixType
        {
            e_Unsupported = -1,
            e_noPrefix = 0x0,
            e_TolPrefix,
            e_NormlPrefix, //normalization prefix

        }

        internal const string cnst_mars_tol_prefix = "TOL:";
        internal const string cnst_mars_norml_prefix = "NORML:";

        private e_prefixType currentPreFixType = e_prefixType.e_noPrefix;

        private string dataSrc;
        public string DataSrc
        {
            get
            {
                return dataSrc;
            }
            set
            {
                dataSrc = DataExtractPrefix = value;
                if (string.IsNullOrEmpty(dataSrc)) return;

                //判断数据前缀类型
                if (dataSrc.ToUpper().StartsWith(cnst_mars_tol_prefix))
                {
                    #region TOLERANCE 
                    int iFirstComma = dataSrc.IndexOf(";");
                    if (iFirstComma < 0)
                    {
                        currentPreFixType = e_prefixType.e_noPrefix;
                        return;
                    }
                    DataExtractPrefix = dataSrc.Substring(iFirstComma + 1);
                    try
                    {
                        string strTmp = dataSrc.Substring(cnst_mars_tol_prefix.Length, iFirstComma - cnst_mars_tol_prefix.Length);
                        PrefixAddtionalInfo = strTmp;
                        currentPreFixType = e_prefixType.e_TolPrefix;
                    }
                    catch (Exception)
                    {
                        currentPreFixType = e_prefixType.e_noPrefix;
                    }
                    #endregion
                }

                if (dataSrc.ToUpper().StartsWith(cnst_mars_norml_prefix))
                {
                    #region NORMALIZATION data
                    int iFirstComma = dataSrc.IndexOf(";");
                    if (iFirstComma < 0)
                    {
                        currentPreFixType = e_prefixType.e_noPrefix;
                        return;
                    }
                    DataExtractPrefix = dataSrc.Substring(iFirstComma + 1);
                    try
                    {
                        string strTmp = dataSrc.Substring(cnst_mars_norml_prefix.Length, iFirstComma - cnst_mars_norml_prefix.Length);
                        PrefixAddtionalInfo = strTmp;
                        currentPreFixType = e_prefixType.e_NormlPrefix;
                    }
                    catch (Exception)
                    {
                        currentPreFixType = e_prefixType.e_noPrefix;
                    }
                    #endregion
                }
            }
        }

        public string DataExtractPrefix
        {
            get;
            private set;
        }

        public string PrefixAddtionalInfo
        {
            get;
            private set;
        }
    }


    class MarsVerifyValueToleranceHost
    {
        private static Dictionary<string, MarsVerifyFunc> MarsDefaultVerifyFunc = new Dictionary<string, MarsVerifyFunc>()
        {
            { "NormalizationFloat",  MarsNormalizationAFloat},

        };


        /// <summary>
        /// change a double string to double?. if string is close to maxvalue, then return null
        /// </summary>
        /// <param name="valueToBeVerify">data from target system</param>
        /// <param name="strDataSrc">data set from test case</param>
        /// <param name="resultAfterNormalization">result as double?</param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool MarsNormalizationAFloat(string valueToBeVerify, string strDataSrc, ref object resultAfterNormalization, ref string strError)
        {
            if (string.IsNullOrEmpty(strDataSrc))
            {
                strError = "Data from System Is empty Or Null";
                return false;
            }
            double dV = double.MinValue;
            if (!double.TryParse(valueToBeVerify, out dV))
            {
                resultAfterNormalization = null;
                return string.Compare(valueToBeVerify, strDataSrc) == 0;
            }

            dV = Math.Round(dV, 2);

            double dS = double.MinValue;
            if (!double.TryParse(strDataSrc, out dS))
            {
                if ((string.Compare(strDataSrc, "null", true) == 0)
                    || (string.IsNullOrEmpty(strDataSrc)))
                {
                    if (dV >= double.MaxValue - 1)
                    {
                        resultAfterNormalization = null;
                        return true;
                    }
                }
                resultAfterNormalization = null;
                return string.Compare(valueToBeVerify, strDataSrc) == 0;
            }

            resultAfterNormalization = dV;
            if (dS.Equals(dV))
            {
                return true;
            }
            strError = string.Format("values don't match:[{0}]-From Appliation:[source:{2}-Rounded to {1}]",
                strDataSrc, dV, valueToBeVerify);
            ;
            return false;
        }


        internal static bool IsTorleranceFuncRequired(string strPara)
        {
            return string.IsNullOrEmpty(strPara) ? false : MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + TestStepDataPrepare.cnst_mars_tol_prefix, strPara);
        }

        /// <summary>
        /// format of strParaWithFunc, TOL:FunctionName;DataToVerify
        /// </summary>
        /// <param name="strParaWithFunc"></param>
        /// <param name="strDataToVerify"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal static bool VerifyValueWithTorleranceFunc(string strParaWithFunc, string strDataToVerify, ref string strError)
        {
            strError = string.Format("Not a right format, requires TOL:FUNCTIONANEM;DATA_TO_BE_CHECK, but it is:[{0}]", strParaWithFunc);
            if (!IsTorleranceFuncRequired(strParaWithFunc))
            {
                return false;
            }

            int iPos = strParaWithFunc.IndexOf(";");
            if (iPos < 0)
            {
                return false;
            }
            string strTolPrefixAndFunc = strParaWithFunc.Substring(0, iPos);
            string strValueSrc = strParaWithFunc.Substring(iPos);
            string[] arrF = strTolPrefixAndFunc.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrF.Length != 2)
                return false;
            string strFuncName = arrF[1];
            object objResult = null;
            if (MarsDefaultVerifyFunc.Keys.Contains(strFuncName))
            {
                bool isOk = MarsDefaultVerifyFunc[strFuncName](strDataToVerify, strValueSrc, ref objResult, ref strError);
                if (!isOk)
                {
                    return false;
                }
                strError = "";
                return true;
            }

            return true;
        }


    }
}
