using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.notifiy;
using MARS.AIL.NLP.Inter.verb;
using NLog;
using NLog.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MARS.AIL.NLP.Inter.lang
{

    /// <summary>
    /// 对语义对象的管理
    /// </summary>
    public class Semantics_blocksManagement
    {
        private static Logger log = NLog.LogManager.GetLogger(typeof(Semantics_blocksManagement).Name);
        public List<Semantics_block> sentenceSemanticsList = new List<Semantics_block>();
        public Semantics_block? parementSemantic { get; set; } = null;
        /// <summary>
        /// 该变量可能是
        /// </summary>
        private AnalystASetence_ResponseToken? contextTopicToken = null;
        public Semantics_blocksManagement(AnalystASetence_ResponseToken? keyTopicFromContext = null)
        {
            this.contextTopicToken = keyTopicFromContext;
        }

        public bool setLianciOrLogicToken(AnalystASetence_ResponseToken? lianCiToken, ref string strError, int iIdx = 0)
        {
            if ((sentenceSemanticsList == null) || (sentenceSemanticsList.Count <= 0)||(sentenceSemanticsList.Count<iIdx))
            {
                strError = Resource.ERROR_NO_SEMANTIC_BLOCK;
                log.Error($"setLianciOrLogicToken\t|{strError}");
                return false;
            }

            sentenceSemanticsList[iIdx].currentLianCi = lianCiToken;
            return true;
        }
        /// <summary>
        /// 获得该语义段中的核心内容。主要是主语或者宾语。如果句子中存在多个主语或者宾语，在一个段落中，如果该段落，或者句子缺少主语，或者宾语，那么该信息将传递给后面
        /// </summary>
        /// <returns></returns>
        public AnalystASetence_ResponseToken GetMainIdeaKeyTokenOfThisSemantics()
        {
            log.Info($"GetMainIdeaKeyTokenOfThisSemantics\tbegin");
            if ((sentenceSemanticsList == null) || (sentenceSemanticsList.Count <= 0))
            {
                log.Info($"sentenceSemanticsList\tsetences semantic list is null or contains no item");
                return null;
            }
            /// 不同的动词会产生不同的语句的重点。比如
            for (int i=0;i<sentenceSemanticsList.Count;i++) {
                var semantics = sentenceSemanticsList[i];
                if ((semantics.subjectTokens!=null)
                    && (semantics.subjectTokens.Count > 0))
                {
                    log.Info($"GetMainIdeaKeyTokenOfThisSemantics\t|has subject item|{semantics.subjectTokens.Count}|");
                    return semantics.subjectTokens[0];
                }
                /// 处理宾语
                if ((semantics.objectTokens != null)&&(semantics.objectTokens.Count>0)) {
                    log.Info($"GetMainIdeaKeyTokenOfThisSemantics\t|has object item|{semantics.objectTokens.Count}|");
                    return semantics.objectTokens[0];
                }
            }
            /// 该语义中没有
            /// 
            log.Error($"GetMainIdeaKeyTokenOfThisSemantics\t");
            return null;
        }
        /// <summary>
        /// 获得动作的执行者。如果是被动语态，就是宾语，否则就是主语
        /// </summary>
        /// <returns></returns>
        public AnalystASetence_ResponseToken GetMainActionOperatorOfThisSemantics()
        {
            return null;
        }

        /// <summary>
        /// 找到最后一个宾语或者主语，作为That的替换
        /// 从语法上来讲，代词的或者类似的，替代词最其最近的宾语，该宾语可以作为从句的主语，所以，需要找到最后一个宾语，或者名词
        /// 算法：
        /// 1，找到最后一个token
        /// </summary>
        /// <param name="sourceTokens"></param>
        /// <param name="idx"></param>
        /// <param name="targetSemantic"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public AnalystASetence_ResponseToken? FindLastPreviousObject(List<AnalystASetence_ResponseToken>? sourceTokens, int idx, ref Semantics_block targetSemantic,
            ref string strError, ref bool isOk)
        {
            log.Info($"FindLastPreviousObject\tbegin|{idx}");
            if (sourceTokens==null)
            {
                isOk = false;
                strError = Resource.ERROR_NLP_FIND_THAT_RELATED_OBJ_NULL_PARA;
                log.Error($"FindLastPreviousObject\tError|{strError}");
                return null;
            }
            var lst = sourceTokens
                .Where(x => x.idx<idx)
                .Where(x => x.dep.Equals(MARSNLPConstant.cnst_sentence_dep_dobj, StringComparison.OrdinalIgnoreCase)
                    || (x.pos.Equals(MARSNLPConstant.cnst_sentence_pos_noun, StringComparison.OrdinalIgnoreCase) && ((x.compound_words != null) && (!string.IsNullOrEmpty(x.compound_words.compound_word)))))
                .OrderBy(x=>x.idx)
                .ToList();
            if (lst.Count <= 0)
            {
                strError = Resource.ERROR_NLP_CANT_FIND_THAT_RELATED_OBJ;
                log.Error($"FindLastPreviousObject\t|{strError}");
                isOk = false;
                return null;
            }
            /// 返回最后一个
            /// 
            isOk = true;
            AnalystASetence_ResponseToken? grammar_token = lst.LastOrDefault();
            //Semantics_block? targetSemantic = null;
            ///在现有的 语义链中找到该语法信息，最后一个
            ///

            for (int i=sentenceSemanticsList.Count-1; i>=0; i--)
            {
                var itm = sentenceSemanticsList[i];
                if (itm == null) continue;
                var findRslt = itm.FindSpecialToken(grammar_token, ref strError, ref isOk);
                if ((isOk) && (findRslt != null))
                {
                    grammar_token = findRslt;
                    targetSemantic = itm;
                    break;
                }
            }
            if (targetSemantic == null)
            {
                isOk = false;
                strError = Resource.ERROR_NLP_CANT_FIND_THAT_RELATED_OBJ;
                log.Error($"FindLastPreviousObject\t|{strError}");
                return null;
            }
            return grammar_token;
        }
        /// <summary>
        /// 处理状语,不同的动词对象有不同的状语，因此主语+动词+状语可以形成一个语义
        /// </summary>
        /// <param name="strSub"></param>
        /// <param name="strVerb">should be only one item</param>
        /// <param name="advInfoDic"></param>
        /// <returns></returns>
        private List<Diction_Phrase_Query> phraseAdverbialTokensForDictionaryLookup(string strSub, string?[] strVerbs, 
            Dictionary<AnalystASetence_ResponseToken, List<AnalystASetence_ResponseToken>> advInfoDic,
            ref int queryId)
        {
            log.Info($"phraseAdverbialTokensForDictionaryLookup\tbegin|{strSub}|{strVerbs}");
            List<Diction_Phrase_Query> lstDicQuery = new List<Diction_Phrase_Query>();
            if ((strVerbs == null) || (strVerbs.Length <= 0)) return null;
            string? strVerb = strVerbs.FirstOrDefault(p=>!string.IsNullOrEmpty(p));
            
            try
            {
                string itmPrefix = (string.IsNullOrEmpty(strSub) ? "" : strSub + " ") + (strVerb ?? "");
                itmPrefix = string.IsNullOrEmpty(itmPrefix) ? "" : $"{itmPrefix} ";
                foreach ( var kvp in advInfoDic.Keys )
                {
                    if (kvp == null) continue;
                    foreach (var advItm in advInfoDic[kvp])
                    {
                        if (advItm == null) continue;
                        queryId++;
                        lstDicQuery.Add(new Diction_Phrase_Query()
                        {
                            phrase = $"{itmPrefix}{advItm.getLemmaIfNOCompoundInfo()}",
                            query_id = $"{queryId}"
                        });
                    }
                }
            }
            finally
            {
                log.Info($"phraseAdverbialTokensForDictionaryLookup\tend");
            }
            return lstDicQuery;
        }

        /// <summary>
        /// 有时候会依据主谓 或动宾模式产生多个待查询对象，而在人的思维活动中，需要依据后文确定是哪个。后文可能是一个模式。比如
        /// pay fix （3.5%）,fix 可能有多个意思，但是依据3.5，也就是preview数据，知道，这个是fixed rate， 因此，在token字典
        /// 中增加 "preview":[{
        ///    "shape":"\d+(%)",
        ///    "ref":"fixed rate"
        ///  }]
        ///  该模式暂时没有实现
        /// </summary>
        /// <param name="semanticItem"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        internal List<Nlp_TestSteps>? generateStepsForSemanticBlock(Semantics_block semanticItem,ref string strError, ref bool isOk)
        {
            log.Info($"generateStepsForSemanticBlock\tbegin");
            List<Nlp_TestSteps>? rsltStpList = new List<Nlp_TestSteps>();
            try
            {
                if ((semanticItem.predictTokens == null) || (semanticItem.predictTokens.Count == 0))
                {
                    log.Error($"generateStepsForSemanticBlock\tError|{semanticItem.ToString()}");
                    isOk = false;
                    strError = Resource.ERROR_NLP_NO_MORPHEME_IN_SEMANTIC_PREDICT;//没有谓语语素。目前需要谓语，以后需要调整
                    log.Error($"generateStepsForSemanticBlock\tError|{strError}");
                    return null;
                }

                RESTClientToNLPServer dictionaryQuery = new RESTClientToNLPServer();
                /// 动词应该只有一个，或者其附加的to 等介词啥的
                /// 
                string?[] arrVerbs = semanticItem.predictTokens.Select(p => p.lemma).ToArray();
                string allVerbs = string.Join(" ", arrVerbs);
                
                /// 从后台查字典，例如：create
                /// 
                List<Diction_Phrase_Query> wordsToBeLookedUp = new List<Diction_Phrase_Query>();
                /// 辅助主语
                /// 在这里，有可能没有主语，如动宾结构。那么，就查询谓语和宾语的
                /// 也可能有主语，那么主语和宾语能够定位一个实际的对象。如 set pay index bias, 前面的宾语递延下来的主语，如 
                var subFirstToken = semanticItem.subjectTokens.FirstOrDefault(p => p!=null && p.compound_words != null && (!string.IsNullOrEmpty(p.compound_words.compound_word)))                    ;
                string subjuect_info = subFirstToken == null ? "" : subFirstToken.compound_words.compound_word;
                /// 状语信息，对于状语而言，是说明动词的的特性的，因此，在金融行业状语和动词组合会形成新的词汇，但是并非所有的状语都可以和组成
                /// 
                int iQueryId = 0;
                bool isBeVerb = false;
                if ((!string.IsNullOrEmpty(allVerbs)) && ("be".Equals(allVerbs.Trim(), StringComparison.OrdinalIgnoreCase)))
                    isBeVerb = true;
                foreach (var o in semanticItem.objectTokens)
                {
                    if (o == null) continue;
                    log.Info($"\t|semanticItem.objectTokens|{o.text}");
                    /// 处理过程，因为mars是vo结构，所以，需要找谓语和宾语，
                    List<string> allObjects = semanticItem.objectTokens.Select(p => p.compound_words == null ? p.lemma
                        : (string.IsNullOrEmpty(p.compound_words.compound_word) ? p.lemma : p.compound_words.compound_word))
                        .ToList();

                    /// 添加纯主语模式
                    /// 
                    iQueryId++;
                    wordsToBeLookedUp.Add(new Diction_Phrase_Query()
                    {
                        phrase = subjuect_info,
                        query_id = $"{iQueryId}"
                    });
                    /// 
                    /// 添加主谓模式
                    wordsToBeLookedUp.Add(new Diction_Phrase_Query()
                    {
                        phrase = $"{subjuect_info} {allVerbs}",
                        query_id = $"{iQueryId}"
                    });

                    /// 获得default data
                    /// 
                    var dafaultObject = allObjects.FirstOrDefault();
                    for (int i = 0; i < allObjects.Count; i++)
                    {
                        iQueryId++;
                        wordsToBeLookedUp.Add(new Diction_Phrase_Query
                        {
                            phrase = $"{allVerbs} {allObjects[i]}",
                            query_id = $"{iQueryId}"
                        });  // 因爲不確定是詞還是詞組，因此需要都進行查字典
                        /// 加上宾语本身
                        /// 
                        iQueryId++;
                        wordsToBeLookedUp.Add(new Diction_Phrase_Query
                        {
                            phrase = $"{allObjects[i]}",
                            query_id = $"{iQueryId}"
                        });
                        if (!string.IsNullOrEmpty(subjuect_info))
                        {
                            /// 添加主谓宾模式
                            iQueryId++;
                            wordsToBeLookedUp.Add(new Diction_Phrase_Query
                            {
                                phrase = $"{subjuect_info} {allVerbs} {allObjects[i]}",
                                query_id = $"{iQueryId}"
                            });
                        }
                    }
                    
                    /// 处理状语
                    if ((semanticItem.adverbialTokens != null)&&(semanticItem.adverbialTokens.Count>0))
                    {
                        /// 添加主谓状模式
                        List<Diction_Phrase_Query> adverbialInfo = phraseAdverbialTokensForDictionaryLookup(subjuect_info, arrVerbs,                            
                            semanticItem.adverbialTokens,
                            ref iQueryId);
                        if ((adverbialInfo != null) || (adverbialInfo.Count > 0))
                        {
                            wordsToBeLookedUp.AddRange(adverbialInfo);
                        }
                    }

                    wordsToBeLookedUp.RemoveAll(p => (p == null) || (string.IsNullOrEmpty(p.phrase)));
                    if (!isBeVerb)
                        wordsToBeLookedUp.Insert(0, new Diction_Phrase_Query
                        {
                            phrase = allVerbs,
                            query_id = "0"
                        });

                    /// 处理同位语, 目前同位语只处理单个的
                    /// 
                    if ((semanticItem.appositiveOrExtendTokens != null) && (semanticItem.appositiveOrExtendTokens.Count > 0))
                    {
                        string appositiveToLookup = "";
                        foreach (var apposItm in semanticItem.appositiveOrExtendTokens.Keys)
                        {
                            //if (apposItm == null) continue;
                            var v = semanticItem.appositiveOrExtendTokens[apposItm];
                            if (v == null) continue;
                            var vstr = string.Join("", v.Select(p=>p.lemma));
                            iQueryId++;
                            wordsToBeLookedUp.Add(new Diction_Phrase_Query { 
                                phrase = $"{subjuect_info} {allVerbs} {apposItm.lemma} {vstr}".ToLower(),
                                query_id = iQueryId+""
                            });
                        }
                        
                    }

                    /// 
                    /// 创建查询字典
                    /// 
                    var rspns = dictionaryQuery.lookupDictionariesWithQueryIds(wordsToBeLookedUp, ref strError, ref isOk);
                    if ((!isOk)||(rspns==null)||(rspns.obj==null))
                    {
                        log.Error($"GenerateSteps\t|from lookupDictionariesWithQueryIds|{strError}");
                        continue;
                    }
                    foreach (var itm in rspns.obj)
                    {
                        /// 语义层
                        /// 
                        log.Info($"GenerateSteps\t|{itm.phrase}|");
                        if ((itm == null)||(itm.status==null)) continue;
                        if (!itm.status.Equals(MARSNLP_REST_API_message.cnst_response_OK, StringComparison.OrdinalIgnoreCase))
                        {
                            log.Error($"GenerateSteps\tfrom lookupDictionariesWithQueryIds|{itm.phrase}|is wrong|{itm.message}");
                            continue;
                        }
                        bool isFirst = isObjectOrSubjectItSelf(itm.query_id, ref strError, ref isOk);
                        ///动词本身,如果是动词本身，需要对特殊动词进行处理，就像普通字典一样，有基本意义，就是对应基本的keyword，
                        ///目前，暂时没有处理，仅仅continue
                        if (isFirst)
                        {
                            log.Info($"Verb itself, just continue, IGNORE NOW");
                        }
                        else
                        {
                            for (int i = 0; i < itm.obj.Count; i++)
                            {
                                /// 语义分层
                                var stpObj = itm.obj[i];
                                if (stpObj == null) continue;                                
                                List<MARSOBJ>? stpMarsObj = stpObj.getMARSOBJOrFromRef();
                                var stpMarsData = stpObj.getMARSDataOrFromRef();
                                string strKeyword = MARS_NLP_steps_Keywords.cnst_keyword_clickAt;
                                Nlp_TestSteps stp = new Nlp_TestSteps();
                                var defaultData = stpObj.getDefaultData();
                                
                                foreach (var marsStepObj in stpMarsObj)
                                {
                                    if (marsStepObj == null) continue;
                                    if (string.IsNullOrEmpty(marsStepObj.ref_keyword))
                                    {
                                        /// 没有指定的，就判断对象类别
                                        /// 
                                        string strType = marsStepObj.swftype == null ? "" : marsStepObj.swftype.ToLower();
                                        stp.keyword = VerbAction.getDefaultKeywordBasedOnType(strType);
                                    }
                                    else
                                    {
                                        stp.keyword = marsStepObj.ref_keyword;
                                    }
                                    if (defaultData != null)
                                    {
                                        //stp.objectHappyName = defaultData.key;
                                        stp.objectHappyName = stpObj.getAliasOrFromRef();
                                        /// 注，对于pegwindow而言，有一种情况是使用runtime的
                                        if (stp.keyword.Equals(MARS_NLP_steps_Keywords.cnst_keyword_pegwindow, StringComparison.OrdinalIgnoreCase))
                                        {
                                            stp.data = null;
                                        }
                                        else
                                            stp.data = defaultData.GetDictionaryValue(itm.phrase);
                                    }
                                    else
                                    {
                                        stp.objectHappyName = stpObj.getAliasOrFromRef();
                                        if (string.IsNullOrEmpty(stp.objectHappyName))
                                        {
                                            stp.objectHappyName = defaultData == null ? null : defaultData.key;
                                        }
                                        
                                        if (stp.keyword.Equals(MARS_NLP_steps_Keywords.cnst_keyword_pegwindow, StringComparison.OrdinalIgnoreCase))
                                        {
                                            stp.data = null;
                                        }
                                        else
                                            stp.data = dafaultObject;
                                    }
                                    rsltStpList.Add(stp);
                                }
                            }
                        }
                    }

                }

                /// 处理children的step
                /// 
                foreach (var c in semanticItem.children)
                {
                    if (c == null) continue;
                    var subList = generateStepsForSemanticBlock(c, ref strError, ref isOk);
                    if ((isOk)&&(subList!=null))
                        rsltStpList.AddRange(subList);
                }

                isOk = true;
                return rsltStpList;
            }catch(Exception e)
            {
                strError = Resource.SYS_EXCEPTION_WHEN_GEN_STEPS_BASED_ON_SEMANTICS_BLOCK;
                isOk = false;
                log.Error(e, $"generateStepsForSemanticBlock\tException|{e.Message}");
                return rsltStpList;
            }
            finally
            {
                log.Info("generateStepsForSemanticBlock\tend");
            }
        }

        /// <summary>
        /// 判断当前的语义数据是宾语或者主语本身，还是叠加动词之后的，动宾词组
        /// </summary>
        /// <param name="sourceSemantics"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        private bool isObjectOrSubjectItSelf(string? query_id, ref string strError, ref bool isOk, int compareNum=0)
        {
            log.Info("isObjectOrSubjectItSelf\tBegin");
            
            if (!string.IsNullOrEmpty(query_id))
            {
                isOk = false;
                strError = Resource.ERROR_NLP_INTERNAL_QUERY_ID_NOT_EXISTS;
                return false;
            }
            int i_query_id = -1;
            if (!int.TryParse(query_id, out i_query_id))
            {
                isOk = false;
                strError = Resource.ERROR_NLP_INTERNAL_QUERY_ID_NOT_NUMBER; 
                return false;
            }
            isOk = true;
            return i_query_id == compareNum;
        }

        internal List<Nlp_TestSteps>? GenerateSteps(ref string strError, ref bool isOk)
        {
            log.Info($"GenerateSteps\tbegin");
            try
            {
                RESTClientToNLPServer dictionaryQuery = new RESTClientToNLPServer();
                List<Nlp_TestSteps>? rsltStpList = new List<Nlp_TestSteps>();
                foreach (var semanticItem in sentenceSemanticsList)
                {
                    if (semanticItem==null) continue;
                    var stpListForSemanticItem = generateStepsForSemanticBlock(semanticItem, ref strError, ref isOk);
                    if (!isOk)
                    {
                        log.Error($"GenerateSteps\tError|{strError}");
                    }
                    else
                    {
                        rsltStpList.AddRange(stpListForSemanticItem);
                    }
                }
                isOk = true;
                return rsltStpList;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                log.Error($"GenerateSteps\tException|{e.Message}|{e.StackTrace}");
                return null;
            }
            finally
            {
                log.Info($"GenerateSteps\tEnd");
            }
        }
        /// <summary>
        /// 将括号里面的token取出来，为简单起见，不做堆栈处理
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="idx"></param>
        /// <param name="skipToIdx"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        internal List<AnalystASetence_ResponseToken>? getSubSemanticsForLRB(List<AnalystASetence_ResponseToken>? tokens, int idx, ref int skipToIdx, ref string strError, ref bool isOk)
        {
            log.Info("getSubSemanticsForLRB");
            if (tokens == null)
            {
                strError = "no data passed";
                isOk = false;
                return null;
            }
            /// 有可能有迭代的，即在括号中包括 , 
            var t = tokens
                .Select((p, index)=>new { v=p, ind = index})
                .Skip(idx+1)
                .TakeWhile(p => (p.v!=null)&&(!(p.v.tag.Equals(MARSNLPConstant.cnst_sentence_tag_rrb, StringComparison.OrdinalIgnoreCase)
                || (p.v.tag.Equals(",")))))
                .ToList();
            if (t == null)
            {
                skipToIdx = tokens.Count;
            }
            else
            {
                if (t.Count==0) 
                {
                    isOk = false;
                    strError = Resource.NLP_NO_PAIRED_BRACKETS;
                    return null;
                }
                var l = t.Last();
                if (l.v.idx == null)
                {
                    isOk = false;
                    strError = Resource.ERROR_NLP_INTERNAL_IDX_NOT_SET;
                    return null;
                }
                skipToIdx = l.ind + 1; //t.Last().idx??0 + 1;
            }
            isOk = true;
            return t.Select(p=>p.v).ToList();
        }
        /// <summary>
        /// 判断传入的tokens信息是否构成一个句子，或者是一个简单的词（无动词）
        /// </summary>
        /// <param name="tokensInsideLRB"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        internal bool isSingleSementic(List<AnalystASetence_ResponseToken>? tokensInsideLRB, ref string strError, ref bool isOk)
        {
            log.Info($"isSingleSementic\tbegin");
            if (tokensInsideLRB == null)
            {
                isOk = false;
                strError = "No tokens passed";
                log.Error($"isSingleSementic\t{strError}");
                return false;
            }
            bool noInvalidItems = tokensInsideLRB.All(item =>
                item.lemma != MARSNLPConstant.cnst_sentence_lemma_be && item.pos != MARSNLPConstant.cnst_sentence_pos_verb);
            isOk = true; 
            return noInvalidItems;
        }

        private static readonly string[] cnst_marks = { ",", ";", ".", "(", ")", "'", "?", @"""" };
        private bool isTokenAPunct(AnalystASetence_ResponseToken token, string[]? punctToCheck = null)
        {
            
            if (token == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(token.dep)) return false;
            if (!token.dep.Equals(MARSNLPConstant.cnst_sentence_dep_punct, StringComparison.OrdinalIgnoreCase)) return false;
            if ((punctToCheck == null)||(punctToCheck.Length==0))
            {
                return cnst_marks.Any(p => p.Equals(token.lemma));
            }
            return punctToCheck.Any(p => p.Equals(token.lemma));
        }        

        /// <summary>
        /// 状语从句，状语
        /// 对于状语而言，需要获得前面的动词和主语。这里有两种模式，第一种，作为前面动词的语义对象的一部分，第二种，重新创建一个
        /// 语义对象。因为后期可能需要就相关内容做逻辑运算，那么在一个语义对象中可能会好些。
        /// 所以需要在语义对象中创建一个节点
        /// 另外，状语可能是词组，因此，需要将相关的状语 combine 起来
        /// 该方法就是将状语以及该状语相关的词联合起来，如果存在and/or之类连词，需要判断后面的动词是否和当前动词形态是否一致，如果形态不一致
        /// 比如，动名词做状语，但是and后面是动词一般形态，那么，这将不是状语
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i_parttern_idx"></param>
        /// <param name="skipToIdx"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal List<AnalystASetence_ResponseToken>? getRelatedAdvcl(List<AnalystASetence_ResponseToken>? tokens, int idx, ref int skipToIdx, ref string strError, ref bool isOk)
        {
            log.Info($"getRelatedAdvcl\tbegin|{idx}");
            bool isStop = false;
            List<AnalystASetence_ResponseToken> rslt = new List<AnalystASetence_ResponseToken>();
            skipToIdx = idx;
            while (!isStop)
            {
                if (skipToIdx>=tokens.Count) {
                    isStop = true;
                    break; 
                }
                try
                {
                    if (tokens[skipToIdx].dep.Equals(MARSNLPConstant.cnst_sentence_dep_advcl, StringComparison.OrdinalIgnoreCase)
                        || tokens[skipToIdx].dep.Equals(MARSNLPConstant.cnst_sentence_dep_advmod, StringComparison.OrdinalIgnoreCase))
                    {
                        rslt.Add(tokens[skipToIdx]);
                        continue;
                    }
                    /// 如果出现；.和：说明分析结束
                    if (tokens[skipToIdx].dep.Equals(MARSNLPConstant.cnst_sentence_dep_punct, StringComparison.OrdinalIgnoreCase) 
                        && (!string.IsNullOrEmpty(tokens[skipToIdx].lemma))
                        && (tokens[skipToIdx].lemma.Equals(";") || tokens[skipToIdx].lemma.Equals(":")|| tokens[skipToIdx].lemma.Equals(".")))
                    {
                        isStop = true;
                        break;
                    }
                    /// 连词处理中，存在些问题，需要预处理，如果预处理后面不是副词，而是动词，或者动词不做名词用，则表明，该连词是上一个动词的并列词，均不做处理
                    if (tokens[skipToIdx].dep.Equals(MARSNLPConstant.cnst_sentence_dep_cc, StringComparison.OrdinalIgnoreCase))
                    {
                        var preview_id = skipToIdx + 1; 
                        while (preview_id < tokens.Count)
                        {
                            var tmpToken = tokens[preview_id];
                            if (tmpToken.dep.Equals(MARSNLPConstant.cnst_sentence_dep_advcl, StringComparison.OrdinalIgnoreCase)
                                || tmpToken.dep.Equals(MARSNLPConstant.cnst_sentence_dep_advmod, StringComparison.OrdinalIgnoreCase))
                            {
                                /// 如果连词后面是副词修饰内容，那么这些，这些应该是，前面状语的一部分，比如 running safetly and quickly,safetly和
                                /// quickly同时修饰running
                                rslt.Add(tmpToken);
                                skipToIdx = preview_id;
                                preview_id++;
                            }
                            else if (isTokenAPunct(tmpToken, new string[] { ";", ":", "."}))
                            {
                                /// 如果标点存在于“；”， “：”， “。”
                                /// 说明，一个句子已经结束
                                preview_id++;
                                skipToIdx = preview_id;
                                isStop    = true; // 结束循环
                                break;
                            }
                            else if (isTokenAPunct(tmpToken, new string[] { "," }))
                            {
                                /// 如果存在，说明 格式 running quickly, safetyl and ....
                                /// 继续
                                preview_id++;
                            }else if (tmpToken.pos.Equals(MARSNLPConstant.cnst_sentence_pos_verb, StringComparison.OrdinalIgnoreCase)
                                && (tmpToken.tag.Equals(MARSNLPConstant.cnst_sentence_tag_vbz, StringComparison.OrdinalIgnoreCase)  // 现在时，第三人称
                                ||  tmpToken.tag.Equals(MARSNLPConstant.cnst_sentence_tag_vbp, StringComparison.OrdinalIgnoreCase))) // 非第三人称
                            {
                                /// 是动词，需要判断前面的动词是不是相同形态，如果是一般形态，那么，前面一定也是一般形态，说明不是状语
                                /// 
                                log.Info($"getRelatedAdvcl\t|POS check|{tmpToken.text}|is|{tmpToken.tag}, not advcl or related part,then quit");
                                isStop = true;
                                skipToIdx--;//因为后面有个++
                                break;
                            }
                            else
                            {
                                log.Info($"getRelatedAdvcl\t|{tmpToken.text}|is|{tmpToken.tag}, not advcl or related part,then quit");
                                isStop = true;
                                break;
                            }
                        }
                        
                    }
                    
                }
                catch (Exception e)
                {
                    isOk = false;
                    strError = e.Message;
                    log.Error(e, $"getRelatedAdvcl\tException|{e.Message}");
                    return null;
                }
                finally
                {
                    skipToIdx++;
                }
            }
            isOk = true;
            return rslt;
        }
        /// <summary>
        /// 在token链表中寻找and的子句
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i_parttern_idx"></param>
        /// <param name="skipToIdx"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal List<AnalystASetence_ResponseToken>? getSubTokensForConjunctionOrCoordinate(List<AnalystASetence_ResponseToken>? tokens, int idx, ref int skipToIdx, ref bool isOk, ref string strError)
        {
            log.Info($"getSubTokensForConjunctionOrCoordinate\tbegin|{idx}");
            try
            {
                bool isStop = false;
                List<AnalystASetence_ResponseToken> rslt = new List<AnalystASetence_ResponseToken>();
                if ((tokens == null)||(tokens.Count<=0))
                {
                    isOk = true;
                    return rslt;
                }
                rslt.Add(tokens[idx]); /// 第一个节点必须是and or之类
                skipToIdx = idx+1 ; 
                while (!isStop)
                {
                    try
                    {
                        var tmpToken = tokens[skipToIdx];
                        if (tmpToken == null) { continue; }
                        if (isTokenAPunct(tmpToken, new string[] { ";", ":", "." }))
                        {
                            /// 如果标点存在于“；”， “：”， “。”
                            /// 说明，一个句子已经结束
                            isStop = true; // 结束循环
                            break;
                        }else if (tmpToken.dep.Equals(MARSNLPConstant.cnst_sentence_dep_cc)) {
                            /// 另外一个连词，说明当前连词子句结束
                            /// 
                            if (skipToIdx > idx)
                            {
                                skipToIdx -= 2; //因为最后有个finally ++
                                isStop = true;
                                break;
                            }
                            else
                            {
                                rslt.Add(tmpToken);
                            }
                        }
                        else
                        {
                            rslt.Add(tmpToken);
                        }
                    }
                    catch(Exception x)
                    {
                        strError = Resource.SYS_EXCEPTION_INTERNAL_GET_CONJUCTION;
                        log.Error(x, $"getSubTokensForConjunctionOrCoordinate\tException|{strError}\r\n{x.Message}|{x.StackTrace}");
                        isOk = false;
                        return null;
                    }
                    finally
                    {
                        skipToIdx++;
                        if (!isStop)
                            isStop = skipToIdx> tokens.Count-1;
                    }
                }
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = Resource.SYS_EXCEPTION_INTERNAL_CANT_GET_CONJUCTION_SUBLIST;
                log.Error(e, $"getSubTokensForConjunctionOrCoordinate\tExeption|{e.Message}|{strError}");
                return null;
            }
            finally
            {
                log.Info($"getSubTokensForConjunctionOrCoordinate\tEnd");
            }
        }

        /// <summary>
        /// 信息重组
        /// 比如有些句子没有主语，需要将主语从parent哪里搬过来，所以需要判断主语，宾语是不是缺失
        /// </summary>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        internal void restructureSemnaticBlockList(ref string strError, ref bool isOk)
        {
            log.Info($"restructureSemnaticBlockList\tbegin");
            try
            {
                foreach(var semanticItm in this.sentenceSemanticsList)
                {
                    if (semanticItm == null) continue;
                    semanticItm.restructure(ref strError, ref isOk);
                }
            }
            finally
            {
                log.Info($"restructureSemnaticBlockList0\tend");
            }
        }

        /// <summary>
        /// 构建语义块
        /// </summary>
        /// <param name="tokensForConjunction"></param>
        /// <param name="parentSemanticsBlock"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal void buildSemanticsByTokens(List<AnalystASetence_ResponseToken>? sourceTokens, Semantics_block parentSemanticsBlock,
            ref bool isOk, 
            ref string strError,
            NLP_AnalystTextCallback callBack = null,
            string queryId=null)
        {
            log.Info($"buildSemanticsByTokens\tbegin|parent|{parentSemanticsBlock??null}");
            //Semantics_block semanticBlock = new Semantics_block();
            if (callBack != null)
            {
                callBack(new NLP_TextAnalystStatus()
                {
                    query_id = queryId, 
                    currentText = $"\t\tgoing to analysis sementic",
                    isLastNotification = false,
                    isWithError = false
                });
            }
            parementSemantic = parentSemanticsBlock;//先保留父节点
            if (sourceTokens == null)
            {
                isOk = false;
                strError = Resource.SYS_PARA_TOKENS_IS_NULL;
                log.Error($"buildSemanticsByTokens\tError|{strError}");
                return ;
            }
            int iIdx = 0, skipToIdx=-1;

            Semantics_block currentSemanticItem = new Semantics_block(this.parementSemantic);
            this.sentenceSemanticsList.Add(currentSemanticItem);
            bool isSwitchPreviousToken = false,
                isPreviousTokenBe = false; 
            AnalystASetence_ResponseToken? previousSemanticToken = null;//记录上一个有意义的语义token,逻辑意义上
            AnalystASetence_ResponseToken? generalPreviousSemeanticToken = null;// 通用意义上前一个token
            try
            {
                while (iIdx < sourceTokens.Count)
                {
                    try
                    {
                        var current_token = sourceTokens == null ? null : sourceTokens[iIdx];
                        if (current_token == null) continue;
                        if ((current_token.compound_words == null)
                        || (string.IsNullOrEmpty(current_token.compound_words.pattern_idx)))
                        {
                            log.Info($"buildSemanticsByTokens\tcompound_words.pattern_idx is empty or NULL|txt|{current_token.text}|dep|{current_token.dep}|pos|{current_token.pos}");
                            continue;
                        }
                        isOk = true;
                        /// 处理每个语义信息
                        switch (current_token.compound_words.pattern_idx)
                        {
                            case MARSNLPConstant.cnst_pattern_idx_v: // 谓语
                                currentSemanticItem.predictTokens.Add(current_token);
                                if (current_token.dep.Equals(MARSNLPConstant.cnst_sentence_dep_root, StringComparison.OrdinalIgnoreCase))
                                {
                                    currentSemanticItem.depFromToken = current_token.dep;
                                    //current_phrase.Push(currentTokens);
                                }
                                else
                                {
                                    // 处理head
                                    if (string.IsNullOrEmpty(current_token.head))
                                    {
                                        log.Error($"buildSemanticsByTokens\tNo Head for the token|{current_token.text}|dep|{current_token.dep}|head|{current_token.head}|");
                                        continue;
                                    }
                                    // 从堆栈中找到head的值
                                    //var headObject = nlp_helper.findLastHeadFromStack(current_phrase, current_token, ref isOk, ref strError);
                                }
                                isSwitchPreviousToken = true;
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_b:/// be动词，和谓语类似
                                currentSemanticItem.predictTokens.Add(current_token);
                                currentSemanticItem.depFromToken = current_token.dep;
                                isSwitchPreviousToken = false;
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_z:/// 状语从句，状语
                                                                    /// 对于状语而言，需要获得前面的动词和主语。这里有两种模式，第一种，作为前面动词的语义对象的一部分，第二种，重新创建一个
                                                                    /// 语义对象。因为后期可能需要就相关内容做逻辑运算，那么在一个语义对象中可能会好些。
                                                                    /// 所以需要在语义对象中创建一个节点
                                                                    /// 另外，状语可能是词组，因此，需要将相关的状语 combine 起来
                                                                    /// 
                                List<AnalystASetence_ResponseToken>? relatedAdvcl = this.getRelatedAdvcl(sourceTokens, iIdx, ref skipToIdx, ref strError, ref isOk);
                                if (!isOk)
                                {
                                    log.Error($"buildSemanticsByTokens\t|Error|{strError}");
                                }
                                else
                                {
                                    if (relatedAdvcl != null)
                                    {
                                        if (!currentSemanticItem.adverbialTokens.ContainsKey(current_token))
                                            currentSemanticItem.adverbialTokens.Add(current_token, relatedAdvcl);
                                        else currentSemanticItem.adverbialTokens[current_token].AddRange(relatedAdvcl);
                                    }
                                    iIdx = skipToIdx - 1;// 因为后面有个
                                }
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_a:/// 属性
                                ///如果前面是be动词，那么，就是表语，和 _idx_j一样
                                ///
                                if (generalPreviousSemeanticToken != null)
                                {
                                    if (generalPreviousSemeanticToken.lemma.Equals(MARSNLPConstant.cnst_sentence_lemma_be))
                                    {
                                        currentSemanticItem.objectTokens.Add(current_token);
                                        isSwitchPreviousToken = true;
                                        break;
                                    }
                                    else
                                    {
                                        log.Error($"buildSemanticsByTokens\tun-supported attr dep tag|{current_token.text}|{current_token.tag}|{current_token.pos}");
                                    }
                                }
                                else
                                {
                                    log.Error($"buildSemanticsByTokens\tun-supported attr dep tag without generalPreviousSemeanticToken|{current_token.text}|{current_token.tag}|{current_token.pos}");
                                }
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_o:/// 宾语
                                currentSemanticItem.objectTokens.Add(current_token);
                                isSwitchPreviousToken = true;
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_j:
                                if (current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_jj, StringComparison.OrdinalIgnoreCase))
                                {
                                    ///如果前面是be动词，则当做宾语
                                    ///
                                    if ((generalPreviousSemeanticToken != null) && (generalPreviousSemeanticToken.lemma.Equals(MARSNLPConstant.cnst_sentence_lemma_be)))
                                    {
                                        currentSemanticItem.objectTokens.Add(current_token);
                                    }
                                }
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_n: ///标点
                                                                     /// 对于标点而言，有左右括号，将逐步处理，首先获得括号中间的内容，那么中间的可能是前面的同位语
                                                                     /// 比如 pay fix(rate is 3.5%)或者pay fix（3.5）那么，fix应该是fix rate，如果问题在于，如何定位pay fix是
                                                                     /// fix rate, pay fix应该就是pay fix rate，正常的思路就是，pay fix->pay fix rate->fix rate->swap fix rate
                                                                     /// 这里是左括号的处理
                                if (current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_lrb, StringComparison.OrdinalIgnoreCase))
                                {
                                    /// 将处理的左括号的内容拿出来，然后构建语义树，作为当前语义树的同位语或者扩展信息（依据括号内容确定）
                                    /// 处理后，主循环跳过括号的内容                                    
                                    isSwitchPreviousToken = false;
                                    /// 处理括号里面的，将括号的东西获取后，需要构建一个语义信息对象。需要定位该语义对象和当前语义的关系
                                    /// 如果，只是简单的一个词，那么，就是同位语或者补足语。如果是宾语的同位语，那么其中一个是对象，而另外一个可能是数据
                                    List<AnalystASetence_ResponseToken>? tokensInsideLRB = this.getSubSemanticsForLRB(sourceTokens, iIdx, ref skipToIdx, ref strError, ref isOk);
                                    log.Info($"buildSemanticsByTokens\tGet data from getSubSemanticsForLRB" + (tokensInsideLRB == null ? -1 : tokensInsideLRB.Count));
                                    bool isSingleInfo = this.isSingleSementic(tokensInsideLRB, ref strError, ref isOk);
                                    if (!isOk)
                                    {
                                        /// ignore all those
                                        /// 
                                        iIdx = skipToIdx; // 因为在finally中有++
                                        continue;
                                    }
                                    iIdx = skipToIdx + 1; // 因为在finally中有++
                                    if (isSingleInfo)
                                    {
                                        /// 简单信息，那么作为前面一个词的同位语
                                        /// 
                                        if (previousSemanticToken != null)
                                        {
                                            previousSemanticToken.extOrAppositive = Token_extensionOrAppositive._object_Appositive; //说明有同位语
                                            currentSemanticItem.appositiveOrExtendTokens.Add(previousSemanticToken, tokensInsideLRB);
                                        }
                                    }
                                    else
                                    {
                                        /// 需要将内部的句子重新分析，可能是be动词
                                        /// 从句子的总体而言，是前面一个词的补充说明，如果前面是宾语，那么就是宾语的补充说明，如果是动词，就是状语
                                        /// 可以将内容，语义block合并到前一个对象中
                                        /// 
                                        Semantics_blocksManagement subSemanticBlocks = new Semantics_blocksManagement();
                                        var prntForLRB = (iIdx< sourceTokens.Count)? sourceTokens[iIdx - 1]:null;
                                        subSemanticBlocks.buildSemanticsByTokens(tokensInsideLRB, currentSemanticItem, ref isOk, ref strError);
                                        this.sentenceSemanticsList.AddRange(subSemanticBlocks.sentenceSemanticsList);
                                        log.Info($"buildSemanticsByTokens\t|{strError}");
                                    }
                                }
                                else if (current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_comma))
                                {
                                    /// 逗号，do nothing
                                    /// 
                                    isSwitchPreviousToken = false;
                                    break;
                                }
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_s: // 代词，that之类，从句，后面的是该节点的子节点
                                if (current_token.dep.Equals(MARSNLPConstant.cnst_sentence_dep_nsubj, StringComparison.OrdinalIgnoreCase))
                                {
                                    if ((current_token.pos.Equals(MARSNLPConstant.cnst_sentence_pos_pron, StringComparison.OrdinalIgnoreCase))
                                        )
                                    {
                                        ///代词模式，名义主语，后面可能是从句
                                        ///那么，前面的如果是宾语，那么，就是宾从，前面就是独立的，同样，该结构是前面的子结构，也是链中节点
                                        ///
                                        Semantics_block tmpParentSemantic = new Semantics_block(currentSemanticItem); // 临时的父节点
                                        Semantics_block nextBlock = new Semantics_block(tmpParentSemantic)
                                        {
                                            depFromToken = current_token.dep
                                        };
                                        ///需要找实际主语，或者实际宾语
                                        ///
                                        
                                        //this.sentenceSemanticsList.Add(currentSemanticItem);
                                        AnalystASetence_ResponseToken? previousObject = this.FindLastPreviousObject(sourceTokens, current_token.idx ?? -1,
                                            ref tmpParentSemantic,
                                            ref strError, ref isOk);
                                        if (!isOk)
                                        {
                                            log.Error($"buildSemanticsByTokens\t|{strError}");
                                            /// 如果无法找到这个对应的主语，将这个继续放到链里面，在生成阶段，忽略
                                            /// 如果是名词，那么将直接添加到主语
                                            /// 
                                            if (current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_nnp, StringComparison.OrdinalIgnoreCase)||
                                                current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_nnps, StringComparison.OrdinalIgnoreCase)||
                                                current_token.tag.Equals(MARSNLPConstant.cnst_sentence_tag_nns, StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (nextBlock.subjectTokens.Count <= 0) {
                                                    nextBlock.subjectTokens.Add(current_token);
                                                }                                                
                                            }
                                        }
                                        else
                                        {
                                            //nextBlock.parentSemantics.Add(tmpParentSemantic);
                                            nextBlock.subjectTokens.Add(previousObject);// 将前面的实际主语加到链表中
                                        }
                                        //nextBlock.parentSemantics.Add(currentSemanticItem);
                                        currentSemanticItem.children.Add(nextBlock);

                                        /// 转换当前模块
                                        /// 
                                        currentSemanticItem = nextBlock;
                                    }else if (current_token.pos.Equals(MARSNLPConstant.cnst_sentence_pos_noun, StringComparison.OrdinalIgnoreCase)  // 名词
                                            ||current_token.pos.Equals(MARSNLPConstant.cnst_sentence_pos_propn, StringComparison.OrdinalIgnoreCase))// 专有名词
                                    {
                                        /// 名词，又是主语，因此，该部分是主语，但是需要找到前面的辅助的主语，以确定该主语的实际含义。
                                        /// 
                                        currentSemanticItem.subjectTokens.Add(current_token);
                                        ///
                                        if (string.IsNullOrEmpty(current_token.compound_words.compound_word))
                                        {
                                            /// 从parent中找主谓拼组合词
                                            /// 如果是段落中的，需要從前面的topic中獲得
                                            ///
                                            isOk = currentSemanticItem.buildSubjectCombinWords(current_token,ref strError, this.contextTopicToken);

                                        }
                                        else
                                        {
                                            /// 将上下文的内容一起合并
                                            /// 其实，这里最好用多member模式更加符合思维模式。
                                            /// 
                                            if ((this.contextTopicToken.compound_words!=null) &&(!string.IsNullOrEmpty(this.contextTopicToken.compound_words.compound_word)))
                                                current_token.compound_words.compound_word = $"{this.contextTopicToken.compound_words.compound_word} {current_token.compound_words.compound_word}";
                                        }
                                    }
                                }
                                // 如果是代词，需要判断是否是从句
                                break;
                            case MARSNLPConstant.cnst_pattern_idx_l: // 连词
                                /// 如果是连词，需要将树的结构重构？变成 and--node1(
                                ///                                   |--node2
                                /// 或者构建在系统的树中？因为估计以后要做逻辑运算，and/or/Rather than/?
                                /// 注：暂时不做逻辑处理，只是处理文字方面的东西
                                /// 在处理文字方面的时候，需要取得层级，和那一层的动词对应，如果没有语义，则单独构建一个
                                /// 判断语义时候，寻找动词的相同形态作为语义的，如and后面是第三人称动词，那么，对应的就是谓语的重叠，如果后面是补语，
                                /// 就是补语的集合
                                List<AnalystASetence_ResponseToken>? tokensForConjunction = this.getSubTokensForConjunctionOrCoordinate(sourceTokens, iIdx,
                                    ref skipToIdx, ref isOk, ref strError);
                                /// 找到该连词的内容后，补足主语，谓语，构建语义对象
                                /// 
                                if ((!isOk)||((tokensForConjunction!=null)&&(tokensForConjunction.Count<=0)))
                                {
                                    ///需要跳过连词
                                    ///
                                    log.Error($"buildSemanticsByTokens\tError|{strError}|");
                                }
                                else
                                {
                                    /// 将conjunction的子链作为新的进行分析，然后合并到现在的语义树中
                                    /// 假定连词只有一个
                                    Semantics_blocksManagement subSemantics = new Semantics_blocksManagement();

                                    var currentLianCi = tokensForConjunction[0];
                                    tokensForConjunction.RemoveAt(0);
                                    /// 对于连词，需要找到对应的语法成分，比如，连词后面是动词，那么，连词在句子中的成分就是找前面相同成分的位置，从而找到parents
                                    /// 
                                    MarsSubSentenceType conjunctionSubSentenceType = subSemantics.previewSubSentenceTypeForConjunction(tokensForConjunction, ref strError,ref isOk);
                                    if (!isOk)
                                    {
                                        /// 如果对于and之类连词出现错误，那么，可能是子句为空，结构可能是 and, ........
                                        /// 这种模式暂时没有处理，因此，直接跳过 注意！！！！！
                                        /// 
                                        skipToIdx = iIdx + 1;
                                        break;
                                    }
                                    if (conjunctionSubSentenceType.predictToken!=null) {
                                        /// 假定只有该情况，以后有扩展再加强， 注意！！！！
                                        /// 从paremnet里面找和该动词相同结构的词，如果找到，那么parent对象的parent就是该对象的parent，从而替换该parent
                                        /// 
                                        Semantics_block suggestParent = currentSemanticItem.traceBackForConjunctionPredict(conjunctionSubSentenceType.predictToken, 
                                            ref isOk, ref strError);
                                        if (!isOk)
                                        {
                                            /// 如果没有找到对象，可能是语法错误，或者拼写错误
                                            /// 如此，不做处理，用户可以修正后重新
                                        }
                                        else
                                        {
                                            currentSemanticItem = suggestParent;
                                        }
                                    }

                                    subSemantics.buildSemanticsByTokens(tokensForConjunction, currentSemanticItem,
                                        ref isOk, ref strError);
                                    if (!isOk)
                                    {
                                        log.Error($"buildSemanticsByTokens\t|{strError}|");
                                        /// skip掉,
                                        /// 
                                        break;
                                    }

                                    isOk = subSemantics.setLianciOrLogicToken(currentLianCi, ref strError);
                                    if (!isOk)
                                    {
                                        log.Error($"buildSemanticsByTokens\t|{strError}");
                                        break;
                                    }

                                    /// 将子链添加到主链中
                                    /// 因为这是连词，并列关系，所以直接加到,语义节点中
                                    /// 
                                    sentenceSemanticsList.AddRange(subSemantics.sentenceSemanticsList);
                                    /// 因为后面有个++
                                    iIdx = (skipToIdx - 1);
                                }
                                break;
                        }
                        if (isSwitchPreviousToken)
                        {
                            previousSemanticToken = current_token;
                        }
                        generalPreviousSemeanticToken = current_token;
                    }
                    catch (Exception e)
                    {
                        log.Error($"buildVOStarts\tException|{strError = e.Message}\r\n{e.StackTrace}");
                        isOk = false;
                        return;
                    }
                    finally
                    {
                        iIdx++;
                    }
                }
            }catch(Exception x)
            {
                log.Error(x, $"buildVOStarts\tException|{x.Message}|{x.StackTrace}");
                isOk = false;
                return;
            }
            finally
            {
                log.Info($"buildVOStarts\tEnd");
            }
        }

        private MarsSubSentenceType previewSubSentenceTypeForConjunction(List<AnalystASetence_ResponseToken> tokensForSub, ref string strError, ref bool isOk)
        {
            log.Info($"previewSubSentenceTypeForConjunction\tBegin");
            if ((tokensForSub == null)||(tokensForSub.Count<=0))
            {
                isOk = false;
                strError = Resource.ERROR_NLP_INTERNAL_NO_SUBSENTENCE_PREVIEW;
                log.Error($"previewSubSentenceTypeForConjunction\t|{strError}");
                return null;
            }
            MarsSubSentenceType subSentencePatternInfo = new MarsSubSentenceType();
            foreach (var itm in tokensForSub)
            {
                if (itm == null) continue;
                if (itm.compound_words == null) continue;
                if (string.IsNullOrEmpty(itm.compound_words.pattern_idx)) continue;
                subSentencePatternInfo.subSentencePatternInShort += itm.compound_words.pattern_idx;
                
                if (itm.compound_words.pattern_idx.Equals(MARSNLPConstant.cnst_pattern_idx_v, StringComparison.OrdinalIgnoreCase))
                {
                    /// 谓语
                    /// 
                    subSentencePatternInfo.predictToken = itm;
                }
                else if (itm.compound_words.pattern_idx.Equals(MARSNLPConstant.cnst_pattern_idx_s, StringComparison.OrdinalIgnoreCase))
                {
                    /// 主语
                    /// 
                    subSentencePatternInfo.subjectToken = itm;
                }else if (itm.compound_words.pattern_idx.Equals(MARSNLPConstant.cnst_pattern_idx_o, StringComparison.OrdinalIgnoreCase))
                {
                    subSentencePatternInfo.objectToken = itm;
                }
            }
            isOk = true;
            return subSentencePatternInfo;
        }
    }


    /// <summary>
    /// 语义对象
    /// 
    /// </summary>
    public class Semantics_block
    {
        private static NLog.Logger log = NLog.LogManager.GetLogger(typeof(Semantics_block).Name);
        public string? depFromToken { get; set; } // 说明语法成分，如果为空，就是默认的主节点
        public List<AnalystASetence_ResponseToken> subjectTokens { get; set; } // 主语对应的tokens
            = new List<AnalystASetence_ResponseToken>();
        public List<AnalystASetence_ResponseToken> predictTokens { get; set; } // 谓语对应的tokens
            = new List<AnalystASetence_ResponseToken>();
        public List<AnalystASetence_ResponseToken> objectTokens { get; set; } // 宾语对应的tokens
            = new List<AnalystASetence_ResponseToken>();
        /// <summary>
        /// 当前的连词，默认为null。通过连词处理前后句的关系，也可以做逻辑计算，如and or之类
        /// </summary>
        public AnalystASetence_ResponseToken? currentLianCi { get; set; } = null;

        /// <summary>
        ///  使用双向列表可以从语法链中任意一个节点获得所有段落信息，通过遍历
        /// </summary>
        public List<Semantics_block> children { get; set; } //如果有从句之类, 
            = new List<Semantics_block>();
        /// <summary>
        /// 补充说明或者同位语信息，这里
        /// </summary>
        public Dictionary<AnalystASetence_ResponseToken, List<AnalystASetence_ResponseToken>?> appositiveOrExtendTokens { get; set; }
            = new Dictionary<AnalystASetence_ResponseToken, List<AnalystASetence_ResponseToken>?>();
        /// <summary>
        /// 状语信息。状语可能是词组，也可能是
        /// </summary>
        public Dictionary<AnalystASetence_ResponseToken, List<AnalystASetence_ResponseToken>> adverbialTokens { get; set; }
            = new Dictionary<AnalystASetence_ResponseToken, List<AnalystASetence_ResponseToken>>();
        public List<Semantics_block> parentSemantics { get; set; } // 父辈信息，通常是一个
            = new List<Semantics_block>();



        /// <summary>
        /// 需要同时处理主语和宾语，如果宾语有了，则返回宾语，否则处理主语。如果都为空，则回调上一级parent的内容
        /// </summary>
        /// <returns></returns>
        public string? getFirstParenetSemanticsCompbindTxt()
        {
            var block = this;// (this.parentSemantics != null) && (this.parentSemantics.Count > 0) ? this.parentSemantics[0]:null;
            if (block == null)
            {
                return null;
            }
            var firstO = ((block.objectTokens != null) && (block.objectTokens.Count > 0)) ? block.objectTokens[0] : null;
            var firstS = ((block.subjectTokens != null) && (block.subjectTokens.Count > 0)) ? block.subjectTokens[0] : null;
            string strFirstOCompound = firstO==null? null : firstO.compound_words==null?null:firstO.compound_words.compound_word;
            string strFirstSCompound = firstS==null ? null : firstS.compound_words == null ? null : firstS.compound_words.compound_word;
            if (string.IsNullOrEmpty(strFirstSCompound) && string.IsNullOrEmpty(strFirstOCompound))
            {
                /// 回调parent的数据
                ///
                if ((block.parentSemantics != null) && (block.parentSemantics.Count > 0))
                    return block.parentSemantics[0].getFirstParenetSemanticsCompbindTxt();
                else return null;
            }
            else if (!string.IsNullOrEmpty(strFirstOCompound))
            {
                return strFirstOCompound;
            }
            else return strFirstSCompound;            
            
        }

        public string? getFirstPridcitLemma()
        {
            if ((parentSemantics == null) || (parentSemantics.Count <= 0)) return null;
            var prnt = this.parentSemantics[0];
            if ((prnt.predictTokens == null) || (prnt.predictTokens.Count <= 0)) return null;
            
            return prnt.predictTokens[0].lemma;
        }


        public Semantics_block(Semantics_block prnt)
        {
            if (prnt != null)
            {
                parentSemantics.Add(prnt);
            }
        }

        /// <summary>
        /// 如果主语的combined words 为null,需要从 parent 的语义块中获得补充。即将token中的compound 内容用新内容替换
        /// 替换的原则如下：
        /// parent's combine text（如果没有，选择第一个主语）...+上一层的谓语+本身的第一个主语（如果不是被动语态）
        /// </summary>
        /// <param name="currentToken">需要调整的token</param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool buildSubjectCombinWords(AnalystASetence_ResponseToken currentToken, ref string strError, AnalystASetence_ResponseToken? topicContextToken=null)
        {
            log.Info($"buildSubjectCombinWords\tbegin");
            try
            {
                if (currentToken.compound_words == null)
                    currentToken.compound_words = new MarsNlpCompoundWord();

                string? targetSubject = currentToken.lemma;                                
                /// 添加谓语
                /// 
                string? verbInfo = this.getFirstPridcitLemma();
                targetSubject = string.IsNullOrEmpty(verbInfo) ? targetSubject : $"{verbInfo} {targetSubject}";

                if ((this.subjectTokens != null) && (this.subjectTokens.Count > 0))
                {
                    //找上一个，因为宾语后面加括号，括号里面的内容是解析前面的宾语的，因此，宾语就是本身以及上下文的组合
                    string? contextTxt = this.getFirstParenetSemanticsCompbindTxt();
                    if (contextTxt == null)
                    {
                        /// 将上下文的信息作为主要信息
                        /// 
                        contextTxt = topicContextToken==null? null
                            :(topicContextToken.compound_words==null?null: topicContextToken.compound_words.compound_word);
                    }
                    targetSubject = string.IsNullOrEmpty(contextTxt) ? targetSubject : $"{contextTxt} {targetSubject}";
                    
                }
                currentToken.compound_words.compound_word = targetSubject;                
                return true;
            }
            catch(Exception e)
            {
                log.Error(e, $"buildSubjectCombinWords\tException|{e.Message}|{e.StackTrace}");
                return false;
            }
            finally
            {
                log.Info($"buildSubjectCombinWords\tEnd");
            }
        }

        public override string ToString()
        {
            string strRslt = "";
            strRslt = ((subjectTokens==null) || (subjectTokens.Count==0))?"NO_Subject" : string.Join("|", subjectTokens.Select(p => p.text));
            strRslt =string.Format("{0}|{1}", strRslt,
                ((predictTokens == null) || (predictTokens.Count == 0)) ? "NO_Verbs" : string.Join("|", predictTokens.Select(p => p.text)));
            strRslt = string.Format("{0}|{1}", strRslt,
                ((objectTokens == null) || (objectTokens.Count == 0)) ? "NO_OBJECT" : string.Join("|", objectTokens.Select(p => p.text)));
            return strRslt;
        }

        public AnalystASetence_ResponseToken? FindSpecialToken(AnalystASetence_ResponseToken? grammarToken, ref string strError, ref bool isOk)
        {
            log.Info(string.Format("FindSpecialToken\tBegin|{0}", grammarToken==null?"NULL":grammarToken.idx+""));
            if (grammarToken==null)
            {
                strError = Resource.ERROR_NLP_FIND_SPECIAL_TOKEN_NULL_PARA;
                log.Error($"FindSpecialToken\tError|{strError}");
                isOk = false;
                return null;
            }
            var subject= subjectTokens==null?null:subjectTokens.FirstOrDefault(p => p.idx == grammarToken.idx);
            if (subject != null)
            {
                isOk = true;
                return subject;
            }
            var pred = predictTokens==null?null:predictTokens.FirstOrDefault(p => p.idx == grammarToken.idx);
            if (pred != null)
            {
                isOk = true;
                return pred;
            }
            var obj = objectTokens == null ? null : objectTokens.FirstOrDefault(p => p.idx == grammarToken.idx);
            if (obj != null)
            {
                isOk = true;
                return obj;
            }
            isOk = false;
            /// 该语义不存在于该语义组中
            return null;
        }
        /// <summary>
        /// 为连词寻找相同结构的谓语
        /// </summary>
        /// <param name="predictToken"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal Semantics_block traceBackForConjunctionPredict(AnalystASetence_ResponseToken predictToken, ref bool isOk, ref string strError, int iLevel=1)
        {
            log.Info($"traceBackForConjunctionPredict\tBegin|Levle|{iLevel}");
            if (predictToken==null)
            {
                isOk = false;
                strError = Resource.ERROR_NLP_INTERNAL_NO_PREDICTION_TOKEN;
                return null;
            }
            if (predictTokens!=null)
            {
                var verb = predictTokens.FirstOrDefault(p => p.tag.Equals(predictToken.tag, StringComparison.OrdinalIgnoreCase));
                if (verb != null)
                {
                    isOk = true;
                    return this;
                }
            }
            /// 从父辈中找
            /// 
            int iNewLevel = iLevel + 1;
            foreach(var itmPrnt in this.parentSemantics)
            {
                if (itmPrnt == null) continue;
                var verbFromPrnt = itmPrnt.traceBackForConjunctionPredict(predictToken, ref isOk, ref strError, iNewLevel);
                if ((verbFromPrnt != null)&&(isOk))
                {
                    return itmPrnt;
                }
            }
            strError = Resource.MESSAGE_NLP_NO_FIND_PREDICTION;
            log.Info($"traceBackForConjunctionPredict\tInfo|{iNewLevel}|no find prediction");
            isOk = false;
            return null;
        }
        /// <summary>
        /// 信息重组，如果缺少主语，将主语从parent节点中补足
        /// </summary>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        public void restructure(ref string strError, ref bool isOk)
        {
            log.Info("resturucture\tbegin");
            try
            {
                if ((this.parentSemantics != null) && (this.parentSemantics.Count > 0))
                {
                    var targetParent = this.parentSemantics.FirstOrDefault();
                    string suggestSubject = targetParent.getFirstParenetSemanticsCompbindTxt();
                    if (subjectTokens == null) subjectTokens = new List<AnalystASetence_ResponseToken>();
                    if (subjectTokens.Count == 0)
                    {
                        /// 缺乏主语，因此需要前面的宾语或者主语补充
                        /// 
                        subjectTokens.Add(new AnalystASetence_ResponseToken()
                        {
                            compound_words = new MarsNlpCompoundWord()
                            {
                                compound_word = suggestSubject
                            },
                            sourceFrom = 1
                        });
                    }
                }
                /// 处理children
                foreach(var itm in this.children)
                {
                    if (itm == null) { continue; }
                    itm.restructure(ref strError, ref isOk);
                    if (!isOk)
                    {
                        log.Error($"restructure\tError|{strError}");
                    }
                    isOk = true;
                }
            }
            finally
            {
                log.Info("restructure\tend");
            }
        }
    }

    internal class Morpheme_Tokens
    {
        public AnalystASetence_ResponseToken? main_Token{  get; set; }
        public List<Morpheme_Tokens>? child_Morphemes { get; set; }
        public int Level
        {
            get; set;
        } = 0;

        public Morpheme_Tokens(AnalystASetence_ResponseToken mainToken, int parentLevle = 0)
        {
            this.main_Token = mainToken;
            Level = parentLevle;
        }
    }

    public class Morpheme_TreeNode<T>
    {
        private readonly T _value;
        private readonly List<Morpheme_TreeNode<T>> _children = new List<Morpheme_TreeNode<T>>();

        public Morpheme_TreeNode(T value)
        {
            _value = value;
        }


        public Morpheme_TreeNode<T> this[int i]
        {
            get { return _children[i]; }
        }

        public Morpheme_TreeNode<T> Parent { get; private set; }

        public T Value { get { return _value; } }

        public ReadOnlyCollection<Morpheme_TreeNode<T>> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public Morpheme_TreeNode<T> AddChild(T value)
        {
            var node = new Morpheme_TreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        public Morpheme_TreeNode<T>[] AddChildren(params T[] values)
        {
            return values.Select(AddChild).ToArray();
        }

        public bool RemoveChild(Morpheme_TreeNode<T> node)
        {
            return _children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(Value);
            foreach (var child in _children)
                child.Traverse(action);
        }

        public IEnumerable<T> Flatten()
        {
            return new[] { Value }.Concat(_children.SelectMany(x => x.Flatten()));
        }
    }
}
