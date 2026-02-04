using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.restClient.communiteData
{
    public enum Token_extensionOrAppositive
    {
        _no_extOrAppositive = 0x0, 
        _object_Appositive, /// 宾语同位语
        _object_ext, /// 宾语补充说明
    
    }

    internal class CommuniteData
    {
    }

    public class AnalystASetence_Request
    {
        public string sentence { get; set; }
    }

    public class AnalystASetence_Response
    {
        public string imgPath { get; set; }
        public string pattern { get; set; }
        public string sourceText { get; set; }
        public string text { get; set; }
        public List<AnalystASetence_ResponseToken>? tokens { get; set; }
        public string getTrackString()
        {
            string tokenInfo = "";
            tokens.ForEach(t => tokenInfo = string.IsNullOrEmpty(tokenInfo)?t.getTrackString():$"{tokenInfo}\r\n{t.getTrackString()}");
            return $"imgPath|{imgPath}\r\npattern|{pattern}|sourceText|{sourceText}|text|{text}\r\n{tokenInfo}";
        }
    }

    public class AnalystText_Request
    {
        public string text { get; set; }
        public string query_id { get; set; } = new Guid().ToString();
    }

    public class AnalystText_Response
    {
        public string query_id { get; set; }
        public List<string> sentences { get; set; } = new List<string>();
        public bool result { get; set; }
        public string messsage { get; set; }
    }

    public class MarsNlpCompoundWord
    {
        public string? compound_word { get; set; }
        public string? pattern_idx { get; set; }
    }
    public class AnalystASetence_ResponseToken
    {
        public string dep { get; set; }
        public bool is_alpha { get; set; }
        public bool is_stop { get; set; }
        public string? lemma { get; set; }
        public string? pos { get; set; }
        public string? shape { get; set; }
        public string? tag { get; set; }
        public string? text { get; set; }
        public string? head { get; set; }
        public int? idx { get; set; }
        public int sourceFrom { get; set; } = 0;//数据来源，0，表示从python服务来, 1，表示动态创建

        public Token_extensionOrAppositive? extOrAppositive { get; set; } = Token_extensionOrAppositive._no_extOrAppositive;

        public MarsNlpCompoundWord? compound_words { get; set; } = null;

        public string getTrackString()
        {
            return $"text|{text}|dep|{dep}|is_alpha|{is_alpha}|is_stop|{is_stop}|lemma|{lemma}|pos|{pos}|shape|{shape}|tag|{tag}|"
                +(compound_words==null?"":$"{compound_words.pattern_idx}|{compound_words.compound_word}");
        }
        public string getLemmaIfNOCompoundInfo()
        {
            if (compound_words == null) return lemma;
            if (string.IsNullOrEmpty(compound_words.compound_word)) return lemma;
            return compound_words.compound_word;
        }

        public override string ToString()
        {
            return getTrackString();
        }

    }

}
