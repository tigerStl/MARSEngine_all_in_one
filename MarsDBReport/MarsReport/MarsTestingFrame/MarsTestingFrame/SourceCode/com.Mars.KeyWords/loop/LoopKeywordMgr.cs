
extern alias clientWCF;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords.loop
{
    internal class LoopKeywordMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(LoopKeywordMgr));

        private int miCurrentLoopStatus = -1;//0--initialized, default, 1-- data is loaded ,2--is fetching data, -1 mean no loop for the current testcase
        private List<LoopInfo> mLoopInfoList=new List<LoopInfo>();
        private int miCurrentLoopDataIdx = -1;
        
        internal void Init()
        {
            miCurrentLoopStatus = -1;
            mLoopInfoList.Clear();
        }
        private string _sourceData = "";
        internal string SourceData
        {
            get {
                return _sourceData;
            }
            set {
                _sourceData = value;
                LoopDataInit();
                miCurrentLoopStatus = 1;
                miCurrentLoopDataIdx = 0;
            }
        }

        internal bool isInRunning()
        {
            return miCurrentLoopStatus > 0;
        }
        internal bool isLoopUsedInTestCase()
        {
            return miCurrentLoopStatus != -1;
        }

        private void LoopDataInit()
        {
            mLoopInfoList.Clear();

            if (string.IsNullOrEmpty(_sourceData)) return;
            string strTmp = _sourceData.Replace("\n","");
            string[] arrLoopData = strTmp.Split(new string[] { "\r" },StringSplitOptions.None);
            foreach(string strItm in arrLoopData)
            {
                /// as Mars are using regular expression, so data here need limitation
                /// 
                mLoopInfoList.Add(new LoopInfo(strItm));
            }
        }

        internal bool isLoopFinished()
        {
            /// 只有在一个testcase 结束时候才调用
            return miCurrentLoopDataIdx==-1? true :( miCurrentLoopDataIdx >= (this.mLoopInfoList == null ? 0 : this.mLoopInfoList.Count-1));
        }

        internal void MoveCurrentLoopNext()
        {
            Logger.Info("MoveCurrentLoopNext",string.Format("Current:[{0}],total:[{1}]",this.miCurrentLoopDataIdx++,this.mLoopInfoList==null?0:this.mLoopInfoList.Count));
            
        }

        internal string CurrentLoopData
        {
            get
            {
                /// As Mars uses regular Expression as default. to Located in grid, string needs be limitation
                string strResult = mLoopInfoList == null ? "" : (mLoopInfoList.Count == 0 ? "" : (
                    miCurrentLoopDataIdx < 0 ? mLoopInfoList[0].LoopItem : (
                    miCurrentLoopDataIdx >= mLoopInfoList.Count ? mLoopInfoList[mLoopInfoList.Count - 1].LoopItem : mLoopInfoList[miCurrentLoopDataIdx].LoopItem)));
                return string.Format("^{0}$", strResult);
            }
        }


    }

    internal class LoopInfo
    {
        private string loopItm="";

        internal LoopInfo(string strItm)
        {
            this.loopItm = strItm;
        }

        internal string LoopItem
        {
            get { return this.loopItm; }
        }
    }
}
