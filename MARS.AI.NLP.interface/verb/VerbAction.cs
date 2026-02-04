using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    public delegate string Func_TypeForKeyword(string strSwfType);
    internal class VerbAction
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(VerbAction));

        protected static NLog.Logger actionRecord_log = LogManager.GetLogger("ActionRecord_log");
        protected MARSNLP_Industry currentIndustryField;
        protected string currentVerb;
        public VerbAction(string strVerb)
        {
            currentVerb = strVerb;
            currentIndustryField = MARSNLP_Industry._Automation;
            setKeywordForSwfType();
        }

        public static string getDefaultKeywordBasedOnType(string strObjType)
        {
            string keyword = "";
            switch ((strObjType ?? "").ToLower())
            {
                case MARS_NLP_steps_Keywords.cnst_swftype_edit:
                    keyword = MARS_NLP_steps_Keywords.cnst_keyword_fillEdit;
                    break;
                case MARS_NLP_steps_Keywords.cnst_swftype_checkbox:
                    keyword = MARS_NLP_steps_Keywords.cnst_keyword_setBox;
                    break;
                case MARS_NLP_steps_Keywords.cnst_swftype_combobox:
                    keyword = MARS_NLP_steps_Keywords.cnst_keyword_selectDropDown;
                    break;
                case MARS_NLP_steps_Keywords.cnst_swftype_pegwindow:
                    keyword = MARS_NLP_steps_Keywords.cnst_keyword_pegwindow;
                    break;
                default:
                    keyword = MARS_NLP_steps_Keywords.cnst_keyword_clickAt;
                    break;
            }
            return keyword;
        }

        protected virtual List<Nlp_TestSteps>? genSteps(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info("genSteps\tbegin virtual, should invoke implementations");
            log.Info("genSteps\tend");
            return null;
        }

        internal List<Nlp_TestSteps>? GenerateTestSteps(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"GenerateTestSteps\tbegin");
            
            return genSteps(rslt, ref strError, ref isOk);
        }

        protected Func_TypeForKeyword? _getKeywordFromType = null;
        protected virtual void setKeywordForSwfType()
        {

        }
        /// <summary>
        /// 依据类别指定keyword，大部分情况下，可通过类别直接指定一个keyword。也有些对象可以用于多个模式，故而需要其他场景
        /// 信息指定
        /// </summary>
        /// <param name="strswfType"></param>
        /// <param name="typeExtInfo">附加信息，应该是一个函数，暂时采用附加字符串模式</param>
        /// <returns></returns>
        protected virtual string? getKeywordBasedOnType(string strswfType, string? typeExtInfo=null, bool isCapture=false)
        {
            if (string.IsNullOrEmpty(strswfType)) return null;
            switch (strswfType.ToLower())
            {
                case MARS_NLP_steps_Keywords.cnst_swftype_edit:
                    return isCapture ? MARS_NLP_steps_Keywords.cnst_keyword_capturevalue : MARS_NLP_steps_Keywords.cnst_keyword_fillEdit;
                case MARS_NLP_steps_Keywords.cnst_swftype_toolbar: //可能是launchapplication
                    return MARS_NLP_steps_Keywords.cnst_keyword_clickmenuicon;
                case MARS_NLP_steps_Keywords.cnst_swftype_button:
                    return MARS_NLP_steps_Keywords.cnst_keyword_clickButton;
                case MARS_NLP_steps_Keywords.cnst_swftype_checkbox:
                    return isCapture ? MARS_NLP_steps_Keywords.cnst_keyword_capturevalue: MARS_NLP_steps_Keywords.cnst_keyword_setBox;
                default: return null;
            }
        }
    }
}
