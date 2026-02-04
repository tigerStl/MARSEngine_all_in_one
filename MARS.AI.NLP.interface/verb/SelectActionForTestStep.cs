using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    internal class SelectActionForTestStep : VerbAction
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(FillActionForTestStep));

        public SelectActionForTestStep(string strVerb) : base(strVerb)
        {
        }

        protected override List<Nlp_TestSteps>? genSteps(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"genSteps begin|{rslt}");
            
            string strData = "",
                strHappyName = "";
            /// 对于select而言，无法直接赋予keyword，需要判断对象类别
            /// 
            var root = rslt.tokens.Where(p => MARSNLPConstant.cnst_sentence_dep_root.Equals(p.dep, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (root == null)
            {
                strError = "NOT A reconginizable sentence, please try again";
                log.Error($"genSteps\t|{strError}");
                isOk = false;
                return null;
            }
            List<Nlp_TestSteps?> lstRslt = new List<Nlp_TestSteps?>();
            if (MARSNLPConstant.cnst_sentence_pattern_vopo.Equals(rslt.pattern, StringComparison.OrdinalIgnoreCase) )
            {
                /// vopo, 第一个o应该是data
                /// 
                var dobj = rslt.tokens.Where(p=>p.head.Equals(root.text) 
                    && (MARSNLPConstant.cnst_sentence_dep_dobj.Equals(p.dep)))
                    .FirstOrDefault();
                if (dobj == null) {
                    strError = Resource.ERROR_NLP_CANNT_UNDERSTAND_DOBJ;
                    log.Error($"gensteps\t|{strError}");
                    isOk = false;
                    return null;
                }
                strData = dobj.text;
                var lastObj = rslt.tokens.Where(p => (!p.head.Equals(root.text))
                    && (MARSNLPConstant.cnst_sentence_dep_pobj.Equals(p.dep))
                    || (MARSNLPConstant.cnst_sentence_dep_dobj.Equals(p.dep)))
                    .LastOrDefault();
                if (lastObj == null)
                {
                    strError = Resource.ERROR_NLP_CANNT_UNDERSTAND_POBJ;
                    log.Error($"gensteps\t|{strError}");
                    isOk = false;
                    return null;
                }
                strHappyName = lastObj.text;
                /// happyName 可能是object，也可能需要从字典中区
                /// 
                RESTClientToNLPServer rstClnt = new RESTClientToNLPServer();
                try
                {
                    var objInfo = rstClnt.lookupDictionary(lastObj.text.ToLower(), ref strError, ref isOk);
                    if ((!isOk) || (objInfo == null)||(objInfo.Count<=0))
                    {
                        isOk = false;
                        strError = $"can't lookup from dictionary|{lastObj.text.ToLower()}";
                        actionRecord_log.Info($"{rslt.text}|\r\nError|{strError}");
                        log.Error($"genstps\t|{strError}");
                        return null;
                    }
                    if (objInfo.Count > 1)
                    {
                        /// 無法定位單一對象
                        /// 
                        strError = Resource.ERROR_NLP_CONFUSION_OBJECT;
                        log.Error($"gensteps\t|{strError}");
                        actionRecord_log.Info($"{rslt.text}|\r\nError|{strError}");
                        isOk = false;
                        return null;
                    }
                    var o = objInfo[0];                    
                    if ((o._MARS_OBJ == null)||(o._MARS_OBJ.Count<=0))
                    {
                        strError = Resource.ERROR_NLP_NO_MARS_OBJ;
                        log.Error($"genstps\t|{strError}");
                        return null;
                    }
                    /// 在系统字典中，可能会有多个对字典的解释，需要通过上下文判断
                    /// 目前显示所有的
                    /// 
                    foreach (var marsObj in o._MARS_OBJ)
                    {
                        Nlp_TestSteps stpRslt = new Nlp_TestSteps();
                        stpRslt.objectHappyName = strHappyName;
                        stpRslt.data = strData;
                        if (marsObj == null) continue;
                        switch ((marsObj.swftype ?? "").ToUpper())
                        {
                            case MARS_NLP_steps_Keywords.cnst_swftype_tab:
                                stpRslt.keyword = MARS_NLP_steps_Keywords.cnst_keyword_selectTab;
                                break;
                            case MARS_NLP_steps_Keywords.cnst_swftype_list:
                                stpRslt.keyword = MARS_NLP_steps_Keywords.cnst_keyword_selectListItem;
                                break;
                            case MARS_NLP_steps_Keywords.cnst_swftype_combobox:
                                stpRslt.keyword = MARS_NLP_steps_Keywords.cnst_keyword_selectDropDown;
                                break;
                            default:
                                strError = Resource.ERROR_NLP_UNKNOW_OBJECT_TYPE;
                                log.Error($"genstps\t|{strError}");
                                actionRecord_log.Info($"{rslt.text}|\r\nError|{strError}|\r\n{marsObj.swftype}");
                                isOk = false;
                                continue;
                        }
                        lstRslt.Add(stpRslt);
                    }
                    isOk = true;
                    return lstRslt;
                } catch (Exception e)
                {
                    strError = e.Message;
                    log.Error($"gensteps\t{strError}|\r\n{e.StackTrace}");
                    isOk = false;
                    return null;
                }
            }
            else
            {
                strError = Resource.NLP_UNKNOW_PATTERN;
                isOk = false;
                log.Error(strError);
                actionRecord_log.Info($"{rslt.text}|\r\nError|{strError}");
                return null;
            }            
        }
    }
}
