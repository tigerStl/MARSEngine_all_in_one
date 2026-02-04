using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.lang;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.verb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MARS.AIL.NLP.Inter.utilities.notifiy;

namespace MARS.AIL.NLP.Inter.dllEntry
{
    public class MARSNLPSettings
    {
        public const string cnst_NLP_API_SERVER_DOMAIN = "NLP_API_SERVER_DOMAIN";
        private static readonly IConfiguration _configuration=LoadConfiguration();
        internal static bool IsInited = false ;
        //public string NLP_API_SERVER_DOMAIN { get; set; }
        public static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the current directory
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        public static void init()
        {
            var nlpDomain = _configuration[$"systemSettings:{cnst_NLP_API_SERVER_DOMAIN}"];
            RESTClientToNLPServer.NLP_API_SERVER_DOMAIN = nlpDomain ?? "";
        }

        public static void setInitStatus(bool isInited)
        {
            IsInited = isInited; 
        }
    }

    public class MARSDLLEntryMain
    {
        private static NLog.Logger log = NLog.LogManager.GetLogger("MARSAI");

        private NLP_TextToAnalyst currentTextToAnalyst { get; set; }

        public MARSDLLEntryMain()
        {
            init();
        }

        private void init()
        {
            /// 從配置文件中獲得
            /// 
            if (!MARSNLPSettings.IsInited)
            {
                MARSNLPSettings.init();
            }
        }

        public List<Nlp_TestSteps>? AlystTextToSteps(NLP_TextToAnalyst textToAnalyst, ref string strError, ref bool isOk,
            NLP_AnalystTextCallback callBack = null)
        {
            log.Info($"AlystTextToSteps\t|{textToAnalyst.currentText}");
            string strCurrentText = "";
            try
            {
                List<Nlp_TestSteps>? rsltSteps = new List<Nlp_TestSteps>();
                this.currentTextToAnalyst = textToAnalyst;
                List<string>? lstSents = AnalystTextToSentence(ref strError, ref isOk, callBack);
                if (!isOk)
                {
                    if (callBack != null)
                    {
                        callBack(new NLP_TextAnalystStatus() { 
                            query_id = textToAnalyst.query_id, 
                            currentText = strError, 
                            isWithError = true
                        });
                    }
                    log.Info($"AlystTextToSteps\tError|{strError}");
                    return null;
                }
                AnalystASetence_ResponseToken keyTopicFromPrevious = null;
                foreach (var sent in lstSents??new List<string>())
                {
                    strCurrentText = sent ;
                    if (string.IsNullOrEmpty(sent)) continue;
                    var lstStpsForOneSent = AnalystSentencesToSteps(sent, keyTopicFromPrevious, ref strError, ref isOk, callBack);  
                    if (!isOk)
                    {
                        log.Error($"AnalystSentencesToSteps\t|Error |{strError}\r\nWhen analyst|{sent}|");
                        continue;
                    }
                    if (lstStpsForOneSent == null) {
                        log.Error($"AnalystSentencesToSteps\t|Error |{strError}\r\nWhen analyst|{sent}|");
                        continue;
                    }
                    if (lstStpsForOneSent.Count==0)
                    {
                        log.Info($"AnalystSentencesToSteps\t|no steps is generated from|\r\n{sent}");
                        continue;
                    }
                    if (callBack != null)
                    {
                        callBack(new NLP_TextAnalystStatus()
                        {
                            query_id = this.currentTextToAnalyst.query_id,
                            currentText = $"{sent} genereates steps.",
                            isWithError = false,
                            isLastNotification = false,
                            generatedStepsList = new List<Nlp_TestSteps>(lstStpsForOneSent)
                        });
                    }
                    rsltSteps.AddRange(lstStpsForOneSent);

                    /// 从上面一句获得核心句子（主语，宾语）
                    /// 这里包括两个类别的context topic。一个是段落的，一个是上一句的。对于段落的，只要取一次，除非存在转折等；目前，只考虑第一种模式，即只存在段落的
                    /// 
                    if (keyTopicFromPrevious==null)
                        keyTopicFromPrevious = currentSemanticsManagement.GetMainIdeaKeyTokenOfThisSemantics();
                    
                }
                isOk = true;
                return rsltSteps;
            }
            catch(Exception e)
            {
                isOk = false;
                log.Error(e, $"AlystTextToSteps\t{e.Message}");
                strError = $"Exceptions when analyst \"{strCurrentText}\"";
                if (callBack != null)
                {
                    callBack(new NLP_TextAnalystStatus()
                    {
                        query_id = textToAnalyst.query_id,
                        currentText = strCurrentText,
                        isLastNotification = false,
                        isWithError = true
                    });
                }
                return null;
            }
            finally
            {
                log.Info($"AlystTextToSteps");
            }
        }

        /// <summary>
        /// 分析段落到不同的句子，通过调用python服务的接口
        /// </summary>
        /// <param name="text"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public List<string>? AnalystTextToSentence(ref string strError, ref bool isOk, 
            NLP_AnalystTextCallback callBack=null)
        {
            log.Info($"AnalystTextToSentence\tbegin|{this.currentTextToAnalyst.currentText}");
            try
            {
                string text = this.currentTextToAnalyst.currentText;
                RESTClientToNLPServer clnt = new RESTClientToNLPServer();
                var rspns = clnt.analystText(this.currentTextToAnalyst, ref strError, ref isOk, callBack);
                if (!isOk)
                {
                    log.Error($"AnalystTextToSentence\tError when call analystText|query_id|{rspns.query_id}|{strError}");
                    return null;
                }
                if (!rspns.result)
                {
                    strError = rspns.messsage;
                    isOk = false;
                    log.Error($"AnalystTextToSentence\t|analystText return false|Error|{rspns.messsage}|query_id|{rspns.query_id}");
                    return null;
                }
                isOk = true;
                return rspns.sentences;
            }
            catch( Exception ex ) 
            {
                log.Error(ex, $"analystText\tException|{ex.Message}");
                isOk = false;
                strError = $"can't Analyst Text with Error |{ex.Message}";
                return null;
            }
            finally
            {
                log.Info($"AnalystTextToSentence\tEnd");
            }
        }

        /// <summary>
        /// 将句子分析后，组成测试步骤
        /// </summary>
        /// <param name="strCurrenSentence">需要分析的句子</param>
        /// <param name="keyTopicFromPrevious">从上面句子中获得的主题token，首句可能为空，如果本段没有，则为空</param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public List<Nlp_TestSteps>? AnalystSentencesToSteps(string? strCurrenSentence,
            AnalystASetence_ResponseToken keyTopicFromPrevious,
            ref string strError, ref bool isOk,
            NLP_AnalystTextCallback callBack = null)
        {
            log.Info($"AnalystSentencesToSteps\tbegin|{strCurrenSentence}|");
            try
            {
                //create a new 语义对象
                this.currentSemanticsManagement = new Semantics_blocksManagement();

                if (strCurrenSentence == null)
                {
                    strError = "";
                }
                if (callBack != null)
                {
                    callBack(new NLP_TextAnalystStatus()
                    {
                        query_id = this.currentTextToAnalyst.query_id,
                        isLastNotification = false, 
                        currentText = $"\tPreparing Analyst sentence|{strCurrenSentence}"
                    });
                }
                RESTClientToNLPServer clnt = new RESTClientToNLPServer();
                var rslt = clnt.analystSetence(strCurrenSentence??"", ref strError, ref isOk);
                if (isOk)
                {
                    Console.WriteLine($"\t\tnlp returns|{rslt.pattern}");
                    /// generate test step  
                    /// 
                    if (string.IsNullOrEmpty(rslt.pattern))
                    {                         
                        strError=$"nlp server can't find pattern for |{strCurrenSentence}|, please correct spell errors and express it in different way";
                        isOk = false;
                        log.Error($"AnalystSentencesToSteps\t|{strError}");
                        
                        return null;
                    }
                    string cmd4 = rslt.pattern.Length >= MARSNLPConstant.cnst_command_len ? rslt.pattern.Substring(0, MARSNLPConstant.cnst_command_len) : rslt.pattern;
                    List<Nlp_TestSteps> lstResult = new List<Nlp_TestSteps>();
                    switch (cmd4.ToLower())
                    {
                        case MARSNLPConstant.cnst_sentence_pattern_vo:
                            List<Nlp_TestSteps>? vo_steps = buildVO(rslt, ref strError, ref isOk);
                            if ((!isOk)||(vo_steps==null))
                            {
                                strError = $"{MARSNLPConstant.cnst_sentence_pattern_vo}|buildVO return error|{strError}";
                                isOk = false;
                                log.Error($"AnalystSentencesToSteps\t|{strError}");
                                return null;
                            }
                            else
                            {
                                lstResult.AddRange(vo_steps);
                                //foreach (var stp in vo_steps)
                                //{
                                //    var tmpStp = stp.getTestStep();
                                //    if (string.IsNullOrEmpty(tmpStp))
                                //    {
                                //        continue;
                                //    }
                                //    lstResult.Add(tmpStp);                                    
                                //}
                                isOk = true;
                                return lstResult;
                            };                            
                        case MARSNLPConstant.cnst_sentence_pattern_vopo:
                            List<Nlp_TestSteps>? vopo_step = buildVOPO(rslt, ref strError, ref isOk);
                            if ((!isOk)||(vopo_step==null))
                            {
                                log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_vopo}|buildVOPO return error|{strError}");
                                isOk = false;
                                return null;
                            }
                            else
                            {
                                lstResult.AddRange(vopo_step);
                                //foreach (var stp in vopo_step)
                                //{
                                //    var tmpStp = stp.getTestStep();
                                //    if (string.IsNullOrEmpty(tmpStp))
                                //    {
                                //        continue;
                                //    }
                                //    lstResult.Add(tmpStp);
                                //    log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_vopo}|generated Step is:\t|{tmpStp}|");
                                //}
                            }
                            isOk = true;
                            return lstResult;
                        case MARSNLPConstant.cnst_sentence_pattern_sbo:
                            List<Nlp_TestSteps>? sbo_step = buildSBO(rslt, ref strError, ref isOk);
                            if (!isOk)
                            {
                                log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_sbo}|buildSBO return error|{strError}");
                                isOk = false;
                                return null;
                            }
                            else
                            {
                                lstResult.AddRange(sbo_step);
                                //foreach (var stp in sbo_step)
                                //{
                                //    log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_sbo}|generated Step is:\t|{stp.getTestStep()}|");
                                //    var tmpStp = stp.getTestStep();
                                //    if (string.IsNullOrEmpty(tmpStp))
                                //    {
                                //        continue;
                                //    }
                                //    lstResult.Add(tmpStp);
                                //}
                                isOk = true;
                                return lstResult;
                            }
                        case MARSNLPConstant.cnst_sentence_pattern_sbon:
                        default:
                            if ((cmd4.ToLower().StartsWith(MARSNLPConstant.cnst_sentence_pattern_vo)) || // 动宾结构
                                (cmd4.ToLower().StartsWith(MARSNLPConstant.cnst_sentence_pattern_sbon))||
                                (cmd4.ToLower().StartsWith(MARSNLPConstant.cnst_sentence_pattern_sban))) // 主系表结构
                            {
                                List<Nlp_TestSteps>? vostarts_steps = buildVOStarts(rslt, keyTopicFromPrevious, ref strError, ref isOk, callBack);
                                if (!isOk)
                                {
                                    log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_vo}|buildVO return error|{strError}");
                                    return null;
                                }
                                else
                                {
                                    lstResult.AddRange(vostarts_steps);
                                    //foreach (var stp in vostarts_steps)
                                    //{
                                    //    if (stp == null) continue;
                                    //    var tmpStp = stp.getTestStep();
                                    //    if (string.IsNullOrEmpty(tmpStp))
                                    //    {
                                    //        continue;
                                    //    }
                                    //    log.Info($"AnalystSentencesToSteps\t|{MARSNLPConstant.cnst_sentence_pattern_vo}|generated Step is:\t|{tmpStp}|");
                                    //    lstResult.Add(tmpStp);
                                    //}
                                }
                                isOk = true;
                                return lstResult;
                            }
                            strError = $"unsupported pattern|{rslt.pattern}| please wait for next version";
                            log.Error($"AnalystSentencesToSteps\t|{strError}");
                            isOk = false;
                            return null;
                    }
                }
                else
                {
                    log.Error($"AnalystSentencesToSteps\t|{strError}");
                    isOk = false;
                    return null;
                }
            }
            finally
            {
                log.Info($"AnalystSentencesToSteps\tEnd");
            }
        }
        public void NLP_AnalystTextCallbackImpl(NLP_TextAnalystStatus data)
        {
            Console.WriteLine($"\t|{data.query_id}|{data.isLastNotification}|{data.currentText}");
            if ((data.generatedStepsList != null) && (data.generatedStepsList.Count > 0))
            {
                foreach (var stp in data.generatedStepsList)
                    Console.WriteLine($"\t\t|{stp.getTestStep()}");
            }
        }

        public void startSvc()
        {
            log.Info($"startSvc\tbegin");
            MARSNLPSettings.init();
            bool isStop = false, isOk = false;
            string strError = "";
            
            while (!isStop)
            {
                Console.WriteLine("Please input an instruction to create test step, or type ':stop' to quit'");
                string? strCurrentInstruction = Console.ReadLine();
                NLP_TextToAnalyst nLP_TextToAnalyst = new NLP_TextToAnalyst()
                {
                    query_id = Guid.NewGuid().ToString(),
                    currentText = strCurrentInstruction
                };

                if (string.IsNullOrEmpty(strCurrentInstruction))
                {
                    Console.WriteLine("Please input an instruction to create test step, or type ':stop' to quit'");
                    continue;
                }
                if (":stop".Equals(strCurrentInstruction, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Stop----------------");
                    break;
                }
                var lstStps = AlystTextToSteps(nLP_TextToAnalyst, ref strError, ref isOk, NLP_AnalystTextCallbackImpl);
                if (isOk)
                {
                    foreach (var stp in lstStps)
                    {
                        if (stp == null) continue;
                        log.Info($"Generated Steps is|{stp.getTestStep()}");
                        Console.WriteLine($"Generated Steps is|{stp.getTestStep()}");
                    }
                }                
            }
        }
        public Semantics_blocksManagement currentSemanticsManagement { get; set; }
        public List<Nlp_TestSteps>? buildVOStarts(AnalystASetence_Response rslt,
            AnalystASetence_ResponseToken keyTopicFromPrevious,
            ref string strError, ref bool isOk,
            NLP_AnalystTextCallback callBack = null)
        {
            log.Info($"buildVOStarts\tbegin");
            List<Nlp_TestSteps> rslt_stp = new List<Nlp_TestSteps>();
            string cmd = "";
            try
            {
                currentSemanticsManagement = new Semantics_blocksManagement(keyTopicFromPrevious);
                currentSemanticsManagement.buildSemanticsByTokens(rslt.tokens, null, ref isOk, ref strError, callBack, this.currentTextToAnalyst.query_id);
                /// 需要进行信息重组
                /// 
                currentSemanticsManagement.restructureSemnaticBlockList(ref strError, ref isOk);
                List<Nlp_TestSteps>? stps = currentSemanticsManagement.GenerateSteps(ref strError, ref isOk);
                stps = stps.Distinct().ToList();
                return stps;
            }
            catch (Exception e)
            {
                log.Error($"buildVOStarts\tException|{strError = e.Message}\r\n{e.StackTrace}");
                isOk = false;
                return null;
            }
            finally
            {
                log.Info($"buildVOStarts\tend");
            }
        }

        /// 说明是动宾结构
        List<Nlp_TestSteps>? buildVO(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"buildVO\tbegin");
            try
            {
                if ((rslt == null) || (rslt.tokens == null))
                {
                    log.Error(strError = $"buildVO\tNo result");
                    isOk = false;
                    return null;
                };
                /// 判断是否是合法的verb
                VerbAction? verbaction = VerbTestStepFactory.GetVerbActionByVerb(rslt.tokens[0].text, ref isOk, ref strError, MARSNLP_Industry._Automation);
                if (!isOk) return null;
                if (verbaction == null)
                {
                    isOk = false;
                    strError = $"can't create dictionary for {rslt.tokens[0].text}";
                    return null;
                }
                return verbaction.GenerateTestSteps(rslt, ref strError, ref isOk);
            }
            finally
            {
                log.Info($"buildVO\tend|isOK|{isOk}|strError|{strError}|");
            }
        }

        /// build vopo
        List<Nlp_TestSteps>? buildVOPO(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"buildVOPO\tbegin");
            try
            {
                if ((rslt == null) || (rslt.tokens == null))
                {
                    log.Error(strError = $"buildVOPO\tNo result");
                    isOk = false;
                    return null;
                };
                /// 判断是否是合法的verb
                VerbAction? verbaction = VerbTestStepFactory.GetVerbActionByVerb(rslt.tokens[0].text, ref isOk, ref strError, MARSNLP_Industry._Automation);
                if (!isOk) return null;
                if (verbaction == null)
                {
                    isOk = false;
                    strError = $"can't create dictionary for {rslt.tokens[0].text}";
                    return null;
                }
                return verbaction.GenerateTestSteps(rslt, ref strError, ref isOk);
            }
            finally
            {
                log.Info($"buildVO\tend|isOK|{isOk}|strError|{strError}|");
            }
        }

        List<Nlp_TestSteps>? buildSBO(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"buildSBO\tbegin");
            try
            {
                if ((rslt == null) || (rslt.tokens == null))
                {
                    log.Error(strError = $"buildSBO\tNo result");
                    isOk = false;
                    return null;
                }
                /// 判断是否是合法的BE动词
                /// 
                var verbBe = rslt.tokens.FirstOrDefault(p =>
                MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_lemme_be.Equals(p.lemma, StringComparison.OrdinalIgnoreCase));
                if (verbBe == null)
                {
                    strError = $"No is/are found from |{rslt.text}|";
                    log.Error($"buildSBO\t|{strError}");
                    isOk = false;
                    return null;
                }
                VerbAction? verbAction = VerbTestStepFactory.GetVerbActionByVerb(verbBe.lemma, ref isOk, ref strError);
                if (!isOk) return null;
                if (verbAction == null)
                {
                    isOk = false;
                    strError = $"can't create dictionary for {rslt.tokens[0].text}";
                    return null;
                }
                return verbAction.GenerateTestSteps(rslt, ref strError, ref isOk);

            }
            finally
            {
                log.Info($"buildSBO\tend|isOK|{isOk}|strError|{strError}");
            }
        }
    }
}
