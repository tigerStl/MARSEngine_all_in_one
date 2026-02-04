using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.AutoData
{
    public enum MARSNLP_Industry
    {
        _unKnow=0x1, 
        _Automation, 
    }

    public class MARSNLPConstant
    {
        public const int cnst_command_len = 4;

        public const string cnst_sentence_pattern_vo    = "vo";
        public const string cnst_sentence_pattern_vopo  = "vopo";
        public const string cnst_sentence_pattern_sbo   = "sbo";  // 
        public const string cnst_sentence_pattern_sbon  = "sbon"; // 主系宾，标点
        public const string cnst_sentence_pattern_sban  = "sban"; // 主系表，标点结构

        public const string cnst_sentence_dep_dobj      = "dobj";
        public const string cnst_sentence_dep_pobj      = "pobj";
        public const string cnst_sentence_dep_root      = "ROOT";
        public const string cnst_sentence_dep_nsubj     = "nsubj"; // 名义主语
        public const string cnst_sentence_dep_attr      = "attr";
        public const string cnst_sentence_dep_punct     = "punct";
        public const string cnst_sentence_dep_advcl     = "advcl"; //状语从句修饰语
        public const string cnst_sentence_dep_advmod    = "advmod";//状语
        public const string cnst_sentence_dep_cc        = "cc";// 连词，并列词

        public const string cnst_sentence_pos_verb      = "verb";
        public const string cnst_sentence_pos_pron      = "pron";
        public const string cnst_sentence_pos_propn     = "PROPN"; /// 专有名词
        public const string cnst_sentence_pos_noun      = "noun";
        public const string cnst_sentence_pos_punct     = "punct";

        /// lemma
        /// 
        public const string cnst_sentence_lemma_be = "be";
        

        /// tag
        /// 
        public const string cnst_sentence_tag_lrb       = "-LRB-";//左括号
        public const string cnst_sentence_tag_rrb       = "-RRB-";//右括号
        public const string cnst_sentence_tag_vbz       = "VBZ"  ;//变形动词，第三人称现在
        public const string cnst_sentence_tag_vbg       = "VBG"  ;//动词，动名词或者现在分词
        public const string cnst_sentence_tag_vbn       = "VNB"  ;//过去分词
        public const string cnst_sentence_tag_vbp       = "VBP"  ;//非第三人称单数现在时
        public const string cnst_sentence_tag_comma     = ","    ;//逗号
        public const string cnst_sentence_tag_jj        = "JJ"   ;//JJ: This tag represents a general adjective. Adjectives are words that describe or modify nouns,JJ 如果在be动词后，表示表语，可以做宾语
        public const string cnst_sentence_tag_nnp       = "NNP"  ;//名词单数
        public const string cnst_sentence_tag_nnps      = "NNPS" ;//名词复数
        public const string cnst_sentence_tag_nns       = "NNS"  ;//名词复数
        /***
         * pattern_idx
         * */
        public const string cnst_pattern_idx_v          = "V";
        public const string cnst_pattern_idx_o          = "O";
        public const string cnst_pattern_idx_s          = "S";
        public const string cnst_pattern_idx_n          = "N";//标点
        public const string cnst_pattern_idx_z          = "Z";//状语从句，动名词等作为状语
        public const string cnst_pattern_idx_l          = "L";//连词 
        public const string cnst_pattern_idx_b          = "B";//BE动词
        public const string cnst_pattern_idx_j          = "J";//tag is "JJ"-->
        public const string cnst_pattern_idx_t          = "T";// 同位语
        public const string cnst_pattern_idx_a          = "A";//属性，对应cnst_sentence_dep_attr

        /***
         * MARS_OBJECT/MARS_DATA/
         * */
        public const string cnst_data_key_default_suffix    = "__default_suffix__";
        public const string cnst_data_value_default_suffix  = "__default_suffix__";
    }

    public class MARSNLP_REST_API_message
    {
        public const string cnst_response_FAILED = "FAILED";
        public const string cnst_response_SUCCESS = "SUCCESS";
        public const string cnst_response_OK = "OK";

    }
    /// <summary>
    /// 这些词的相关处理应该可以用另外一种“原语”表达。而实现层，解析原语，从而进行处理
    /// 原语类似于oracle之类的sql语法，从而只要一个引擎，即可以解析所有，
    /// 或者其他的动词可以在某些情况下
    /// </summary>
    public class MARSNLP_Verb_dictionary_AUTOMATION
    {
        public const string cnst_verb_fill      = "fill";
        public const string cnst_verb_open      = "open";
        public const string cnst_verb_launch    = "launch";
        public const string cnst_verb_select    = "select";
        public const string cnst_verb_lemme_be  = "be";
        public const string cnst_verb_create    = "create";
    }

    public class MARS_NLP_steps_Keywords
    {
        public const string cnst_keyword_capturevalue   = "CaptureValue";
        public const string cnst_keyword_clickmenuicon  = "ClickMenuIcon";
        public const string cnst_keyword_fillEdit       = "FillEdit";
        public const string cnst_keyword_pegwindow      = "PegWindow";
        public const string cnst_keyword_selectTab      = "SelectTab";
        public const string cnst_keyword_selectListItem = "SelectListItem";
        public const string cnst_keyword_selectDropDown = "SelectDropDown";
        public const string cnst_keyword_setBox         = "SetBox";
        public const string cnst_keyword_clickButton    = "ClickButton";
        public const string cnst_keyword_clickAt        = "ClickAt";

        public const string cnst_swftype_button         = "swfbutton";
        public const string cnst_swftype_combobox       = "swfcombobox";
        public const string cnst_swftype_checkbox       = "swfcheckbox";
        public const string cnst_swftype_edit           = "swfedit";
        public const string cnst_swftype_list           = "swflist";
        public const string cnst_swftype_object         = "swfobject";        
        public const string cnst_swftype_tab            = "swftab";
        public const string cnst_swftype_toolbar        = "swftoolbar";
        public const string cnst_swftype_pegwindow      = "pegwindow";

        public const string cnst_data_not_set           = "NOT_SET";

        public const string cnst_dictionary_type_mars_obj   = "_MARS_OBJ";
        public const string cnst_dictionary_type_mars_data  = "_MARS_DATA";
    }
}
