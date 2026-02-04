extern alias clientWCF;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
//using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase
{
    public enum MarsVar_Type
    {
        varT_normal = 0x0,
        varT_Sattus = 0x1,
        VarT_unKnown = 0x2, 
        VarT_userMemIteration = 0x3
    }
    public class MarsVarDataPrefix
    {
        public const string cnst_dataFromat = "FromVar:.*(;){0,1}";
        /// <summary>
        /// 将变量分成两部分，例如：FromVar:dddd;theRest一部分是FromVar:dddd;的，另外一个部分是剩余部分
        /// varIdx就是上面的dddd，command就是FromVar, dataNoPreFix就是剩余部分,theRest
        /// </summary>
        /// <param name="strData">source data from test case</param>
        /// <param name="dataNoPreFix">remove para format data, for example: FromVar:abc;dddd, then the dataNoPreFix is dddd </param>
        /// <param name="strVarIdx">as the example above, the var Idx is abc</param>
        /// <param name="strVarCmmd">FromVar</param>
        /// <returns></returns>
        public static bool IsVariableFormat(string strData,ref string dataNoPreFix,ref string strVarIdx,ref string strVarCmmd)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(cnst_dataFromat);
            var m = r.Match(strData);
            if (!m.Success) return false;
            dataNoPreFix = strData.Replace(m.Value, "");
            int iPos = m.Value.IndexOf(":");
            strVarCmmd = m.Value.Substring(0,iPos);
            strVarIdx = m.Value.Substring(iPos+1);
            return true;
        }

        public static bool GetVariable(string strVarIdx, string strVarTyp, ref List<MarsVarBasic> varVlaues)
        {
            if (string.IsNullOrEmpty(strVarIdx) || (string.IsNullOrEmpty(strVarTyp))) return false;
            ///依据strVarTyp获取数据信息
            ///
            if (!MarsVariableTable.marsVariableTable.ContainsKey(strVarIdx)) return false;
            varVlaues = MarsVariableTable.marsVariableTable[strVarIdx];
            return true;
        }
    }

    public class MarsVarBasic
    {
        protected static MLogger Logger = MLogger.GetLogger(typeof(MarsVarBasic));
        public string varName;
        public string varValue;
        public int varStatus;
        public string varScope;//变量范围说明
        public DateTime createTime;
        public long assignedKey;
        public MarsVarBasic(string strVarName, string combinedValues, long tempKey = -1)
        {
            createTime = new DateTime();
            this.varName = strVarName;
            assignedKey = tempKey;
            DealwithCombinedValues(combinedValues);

        }
        public virtual void DealwithCombinedValues(string combinedValues)
        {

        }

        public virtual int GetVarRowsCount()
        {
            return varValue==null?0:1 ;
        }

        public virtual bool Update(int idxOfVar,string newValueToUpdate)
        {
            return true;
        }

        public virtual string GetStringForDB()
        {
            return null;
        }

        public virtual bool synchronizeToDB(string strDBIdx, ref string strError)
        {
            strError = "Unimplement method synchronizeToDB";
            return false;
        }
    }

    public class MarsStatusVar: MarsVarBasic
    {
        public List<KeyValuePair<string, string>> varItems = new List<KeyValuePair<string, string>>();
        public MarsStatusVar(string strValrName, string combinedValues,long tempKey=-1) : base(strValrName, combinedValues, tempKey)
        {

        }

        public override bool Update(int idxOfVar, string newValueToUpdate)
        {
            if ((idxOfVar < 0) || (idxOfVar >= varItems.Count)) return false;
            varItems[idxOfVar] = new KeyValuePair<string,string> (varItems[idxOfVar].Key, newValueToUpdate);
            return true;
        }
        /// <summary>
        /// 通过数据更新。或者通过Api
        /// </summary>
        /// <param name="strDBIdx"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public override bool synchronizeToDB(string strDBIdx, ref string strError)
        {
            string strData2DB = GetStringForDB();
            Mars.message.Business.B_SYSTEM_LOOKUP sysLookup = new message.Business.B_SYSTEM_LOOKUP();
            bool isOk = sysLookup.updateStatusData(strDBIdx, (int)this.assignedKey, this.varName, strData2DB,ref strError);
            if (!isOk)
            {
                Logger.Error("synchronizeToDB", strError);
                return false;
            }
            return true;
        }

        public override string GetStringForDB()
        {
            var x = varItems.Select(p => p.Key + ":" + p.Value).ToList();
            return string.Join("\r\n", x);
        }

        public override int GetVarRowsCount()
        {
            return varItems == null ? 0 : varItems.Count(p=>string.Compare("0", p.Value, true)!=0);
        }

        public override void DealwithCombinedValues(string combinedValues)
        {
            if (string.IsNullOrEmpty(combinedValues)) return;
            var lstOfStatusVar = combinedValues.Split(new string[] { "\r\n","\r","\n" },StringSplitOptions.RemoveEmptyEntries);
            foreach (var itm in lstOfStatusVar)
            {
                if (itm == null) continue;
                if (string.IsNullOrEmpty(itm)) continue;
                string[] arrItms = itm.Split(':');
                if (arrItms.Length != 2) continue;
                varItems.Add(new KeyValuePair<string, string>(arrItms[0], arrItms[1]));
            }
            // 添加到变量表
            MarsVarBasic targetInTable = this;
            if (MarsVariableTable.marsVariableTable.ContainsKey(this.varName))
            {
                var lstOfVars = MarsVariableTable.marsVariableTable[this.varName];
                if (lstOfStatusVar == null) {
                    MarsVariableTable.marsVariableTable[this.varName] = new List<MarsVarBasic>();
                    lstOfVars = MarsVariableTable.marsVariableTable[this.varName];
                }
                lstOfVars.Add(this);
            }
            else
            {
                MarsVariableTable.marsVariableTable.Add(this.varName, new List<MarsVarBasic>() { this });
            }
        }
    }

    public class MarsVariableTable
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsVariableTable));
        public static Dictionary<string, List<MarsVarBasic>> marsVariableTable = new Dictionary<string, List<MarsVarBasic>>();

        public static MarsVarBasic getVariableDetailByIdx(string strVarIdx, int idx, ref bool isFind, ref string strError,ref int innerIdx)
        {
            Logger.logBegin("getVariableDetailByIdx", $"varIdx:[{strVarIdx}] index of postion:[{idx}]");
            if (string.IsNullOrEmpty(strVarIdx))
            {
                isFind = false;
                strError = "variable name is empty";
                return null;
            }
            if (!marsVariableTable.ContainsKey(strVarIdx))
            {
                strError = $"no such variable [{strVarIdx}] exists";
                isFind = false;
                return null;
            }
            var lstOfVars = marsVariableTable[strVarIdx];
            int iSrcIdx = 0; 

            for (int i = 0; i < lstOfVars.Count; i++)
            {
                var oneItm = lstOfVars[i];
                if (oneItm == null) continue;
                var oneStatus = oneItm as MarsStatusVar;
                if (oneStatus == null) continue;
                if (oneStatus.varItems == null) continue;                
                if ((iSrcIdx + oneStatus.varItems.Count-1)< idx)
                {
                    iSrcIdx += oneStatus.varItems.Count;
                    continue;
                }
                isFind = true;
                innerIdx = idx - iSrcIdx;
                return oneStatus;                
                
            }
            isFind = false;
            return null;
        }
    }
}
