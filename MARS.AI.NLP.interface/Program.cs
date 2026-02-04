// See https://aka.ms/new-console-template for more information
#if !_DLL
using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.dllEntry;
//using MARS.AIL.NLP.Inter;
//using MARS.AIL.NLP.Inter.AutoData;
//using MARS.AIL.NLP.Inter.AutoSteps;
//using MARS.AIL.NLP.Inter.dllEntry;
//using MARS.AIL.NLP.Inter.lang;
//using MARS.AIL.NLP.Inter.restClient.communiteData;
//using MARS.AIL.NLP.Inter.utilities.data_helper;
//using MARS.AIL.NLP.Inter.utilities.log;
//using MARS.AIL.NLP.Inter.verb;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using NLog.Config;
//using NLog.Extensions.Logging;
//using System.Collections.Generic;
//using System.Net;
//using static MARS.AI.NLP.Inter.restClient.Dicationary_Phrase_Query_ResponseItem;


//string current_NLP_domain = "http://localhost:18888";

//Console.WriteLine("Please input an instruction to create test step, or type ':stop' to quit'");

//var services = new ServiceCollection();
//services.AddLogging();
//var provider = services.BuildServiceProvider();
//var factory = provider.GetService<ILoggerFactory>();
//factory.AddNLog();
//factory.ConfigureNLog("nlog.config");
//var logger = provider.GetService<ILogger<Program>>();
//logger.LogCritical("hello nlog");

//var log = LogMgr.getLogByType(typeof(Program));


//RESTClientToNLPServer.NLP_API_SERVER_DOMAIN = current_NLP_domain;

//bool isStop = false, isOk=false;
//string strError = "";

(new MARSDLLEntryMain()).startSvc();

//List<Nlp_TestSteps>? buildSentenceMorphmem(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
//{
//    log.Info("buildSentenceMorphmem\tbegin");
//    int iLevel = 0, iIdx = 0;
//    Morpheme_TreeNode<AnalystASetence_ResponseToken>? sentence_tree = new Morpheme_TreeNode<AnalystASetence_ResponseToken>(new AnalystASetence_ResponseToken());
//    for (iIdx = 0;iIdx<rslt.tokens.Count;iIdx++)
//    {
//        if (rslt.tokens[iIdx].compound_words == null) continue;
//        if (string.IsNullOrEmpty(rslt.tokens[iIdx].compound_words.pattern_idx))
//        {
//            continue;
//        }
//        if (rslt.tokens[iIdx].compound_words.pattern_idx.Equals(MARSNLPConstant.cnst_pattern_idx_o, StringComparison.OrdinalIgnoreCase))
//        {
//            sentence_tree.AddChild(rslt.tokens[iIdx]);
//        }
//        else
//        {

//        }
//    }
//}



/// 复杂动宾结构，可能存在多个宾语从句




Console.WriteLine("bye");
#else 
  /// dll 模式
#endif