using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.AutoTestingDriver.SystemUtil.DataStructure;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.Inter.MQCenter.Properties;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Utility.visualObjects.objectSpyer;
using MarsOpHelper.MarsOpHelper.data;
using MarsUFTAddins.IMars.tiger;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows.Markup.Localizer;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    public enum en_fillTable_paraType:short
    {
        dynamicRow=0x1,
        otherMode=0x02,
        allRows=0x03,
        rowNumber=0x04,
    }

    public class MARSColumnsInfo
    {
        public string columnKey { get; set; }
        public string columnCaption { get; set; }
        public int idxOfKey { get; set; }
    }


    

    class MarsTableOperation : ThirdPartControlOpBase
    {

        private static string _currentHelperPath = "";
        private static string CurrentHelperPath { get {
                if (string.IsNullOrEmpty(_currentHelperPath)) {
                    string pth = System.IO.Path.GetDirectoryName(typeof(MarsTableOperation).Assembly.Location);
                    _currentHelperPath = System.IO.Path.Combine(pth, "", "MarsOpHelper.exe");
                }
                return _currentHelperPath;
            }
            set => _currentHelperPath = value; 
        }

        private static int ExecuteKeyboardEventOutSide(string strData)
        {
            var StartInfo = new ProcessStartInfo();
            StartInfo.FileName = CurrentHelperPath;
            StartInfo.Arguments = $"-Type Keyboard data {strData} ";
            Process p = new Process();
            p.StartInfo = StartInfo;
            p.Start();
            p.WaitForExit(5000);
            return p.ExitCode;
        }

        private static int ExecuteClickOutSide(int x, int y, Mars_mouseSubType clickType)
        {
            var StartInfo = new ProcessStartInfo();
            StartInfo.FileName = CurrentHelperPath;
            //StartInfo.CreateNoWindow = true;
            
            //StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            string subType = "LeftClick";
            switch (clickType)
            {
                case Mars_mouseSubType.en_LeftClick:
                    subType = "LeftClick";
                    break;
                case Mars_mouseSubType.en_LeftDblClick:
                    subType = "LeftDblClick";
                    break;
                case Mars_mouseSubType.en_move:
                    subType = "Move";
                    break;
                case Mars_mouseSubType.en_rightClick:
                    subType = "RightClick";
                    break;
            }
            
            StartInfo.Arguments = $"-Type Mouse -X {x} -Y {y} -ClickType "+ subType;
            simpleLog.MarsLoggerSimple.Info("\t", $"Para is :{StartInfo.Arguments}");

            Process p = new Process();
            p.StartInfo = StartInfo;
            p.Start();
            p.WaitForExit(5000);
            simpleLog.MarsLoggerSimple.Info("\t", $"remote click reutrns [{p.ExitCode}]");
            return p.ExitCode;
        }

        private const string cnst_header_filter = "HeaderFilter";
        private const string cnst_header_column_swap = "HeaderColumnSwap";
        private const string cnst_header_caption = "HeaderCaption";

        internal const string cnst_Infragistics_Cell_DropDown = "DropDown";
        internal const string cnst_Infragistics_Cell_DropDownList = "DropDownList";
        internal const string cnst_Infragistics_Cell_DropDownValidate = "DropDownValidate";

        private static Hashtable cnst_arrHeader_typeMapping = new Hashtable()
        {
            {cnst_header_filter, "Infragistics.Win.UltraWinGrid.FilterDropDownButtonUIElement" },
            {cnst_header_column_swap, "Infragistics.Win.UltraWinGrid.SwapButtonUIElement" },
            {cnst_header_caption, "Infragistics.Win.TextUIElement" }
        };

        public const string CNST_GRIDCELL_COLNAME = "opicsCellColIndex";
        public const string CNST_GRIDCELL_ROWINDEX = "cellRowIdx";

        internal MarsTableOperation()
        {
            currentColumnStyle = null;
        }

        internal object GetDisplayLayoutFromGrid(object grid, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetDisplayLayoutFromGrid", grid == null ? "grid is null" : $"grid type [{grid.GetType()}]");
            try
            {
                if (grid == null)
                {
                    strError = "Grid is null";
                    isOk = false;
                    return null;
                }
                bool isNoMemberExists = false;
                object oDisplayLayout = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(grid, "DisplayLayout", ref isNoMemberExists); //ReflectorForCSharp.GetMember(grid, "DisplayLayout", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    isOk = false;
                    strError = $"Can't find DisplayLayout from [{grid.GetType()}]";
                    MarsLoggerSimple.Error("GetDisplayLayoutFromGrid", strError);
                    return null;
                }

                isOk = true;
                return oDisplayLayout;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetDisplayLayoutFromGrid");
            }
        }

        internal object GetBandFromDisplayLayout(object displayLayout, ref string strError, ref bool isOk)
        {
            bool isNoMemberExists = false;
            object oBands = ReflectorForCSharp.GetMember(displayLayout, "Bands", ref isNoMemberExists);
            if ((isNoMemberExists) || (oBands == null))
            {

                strError = $"Object [{displayLayout.GetType()}] does not contain columns for DisplayLayout property";
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError);
                isOk = false;
                return null;
            }
            object oLstFromBands = ReflectorForCSharp.GetMember(oBands, "List", ref isNoMemberExists);
            if ((isNoMemberExists) || (oLstFromBands == null))
            {

                strError = $"Object [{oLstFromBands.GetType()}] does not contain columns for DisplayLayout property";
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError);
                isOk = false;
                return null;
            }
            if (!(oLstFromBands is ArrayList))
            {
                strError = $"Object [{oLstFromBands.GetType()}] do not contain ArrayList";
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError);
                isOk = false;
                return null;
            }
            //the first item contains columns
            object BandsContainsColumn = (oLstFromBands as ArrayList)[0];
            isOk = true;
            return BandsContainsColumn;
        }

        internal object GetHeaderFromBand(object band, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetHeaderFromBand", band == null ? "band is null" : $"band type [{band.GetType()}]");
            try
            {
                if (band == null)
                {
                    strError = "band is null";
                    isOk = false;
                    return null;
                }
                bool isNoMemberExists = false;
                object oHeader = ReflectorForCSharp.GetMember(band, "Header", ref isNoMemberExists);
                if ((isNoMemberExists) || (oHeader == null))
                {
                    isOk = false;
                    strError = $"Can't find Header from [{band.GetType()}]";
                    MarsLoggerSimple.Error("GetHeaderFromBand", strError);
                    return null;
                }
                isOk = true;
                return oHeader;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetHeaderFromBand");
            }
        }

        internal int GetHeaderHeight(object header, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetHeaderHeight", header == null ? "band is null" : $"band type [{header.GetType()}]");
            try
            {
                if (header == null)
                {
                    strError = "band is null";
                    isOk = false;
                    return -2;
                }
                bool isNoMemberExists = false;
                object oHeader = ReflectorForCSharp.GetMember(header, "Height", ref isNoMemberExists);
                if ((isNoMemberExists) || (oHeader == null))
                {
                    isOk = false;
                    strError = $"Can't find Height from [{header.GetType()}]";
                    MarsLoggerSimple.Error("GetHeaderFromBand", strError);
                    return -3;
                }
                int iHeight;
                if (int.TryParse(oHeader.ToString(), out iHeight))
                {
                    isOk = true;
                    return iHeight;
                }
                strError = $"header [{header.GetType()}]'s height is not int, it is [{oHeader.GetType()}-[{oHeader.ToString()}]]";

                return -4;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetHeaderFromBand");
            }
        }

        internal int GetRowHeight(object oneRow, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetRowHeight", oneRow == null ? "band is null" : $"band type [{oneRow.GetType()}]");
            try
            {
                if (oneRow == null)
                {
                    strError = "band is null";
                    isOk = false;
                    return -2;
                }
                bool isNoMemberExists = false;
                object oHeader = ReflectorForCSharp.GetMember(oneRow, "Height", ref isNoMemberExists);
                if ((isNoMemberExists) || (oHeader == null))
                {
                    isOk = false;
                    strError = $"Can't find Height from [{oneRow.GetType()}]";
                    MarsLoggerSimple.Error("GetHeaderFromBand", strError);
                    return -3;
                }
                int iHeight;
                if (int.TryParse(oHeader.ToString(), out iHeight))
                {
                    isOk = true;
                    return iHeight;
                }
                strError = $"header [{oneRow.GetType()}]'s height is not int, it is [{oHeader.GetType()}-[{oHeader.ToString()}]]";

                return -4;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetRowHeight");
            }
        }

        internal ArrayList GetColumnsFromInfraGridFromBand(object oBands, string strPegName, string strObjName, ref string strError, ref bool isOk, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetColumnsFromInfraGridFromBand");
            bool isNoMemberExists = false;
            try
            {
                object oLstFromBands = ReflectorForCSharp.GetMember(oBands, "List", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no List for Grid.DisplayLayout.Bands, wrong Ultragrid version?");
                    strError = "Object does not contain columns for DisplayLayout property";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return null;
                }
                if (!(oLstFromBands is ArrayList))
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",
                        strError = string.Format("List from Grid.DisplayLayout.Bands should be ArrayList, it is [{0}]. wrong Ultragrid version?", oLstFromBands.GetType().ToString()));
                    strError = "Object columns do not contain ArrayList";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return null;
                }
                //the first item contains columns
                object BandsContainsColumn = (oLstFromBands as ArrayList)[0];
                object ColumnsInBand0 = ReflectorForCSharp.GetMember(BandsContainsColumn, "Columns", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0], wrong Ultragrid version?");
                    strError = "Object does not contain columns for DisplayLayout property";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return null;
                }
                object olstColumns = ReflectorForCSharp.GetMember(ColumnsInBand0, "List", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0].Columns, wrong Ultragrid version?");
                    strError = "Object does not contain List for Columns property";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return null;
                }
                if ((!(olstColumns is ArrayList)) || (olstColumns == null))
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",
                        strError = string.Format("List from Grid.DisplayLayout.Bands[0].Columns should be ArrayList, it is [{0}]. wrong Ultragrid version?", olstColumns.GetType().ToString()));
                    strError = "Member \"List\"'s type is not ArrayList ";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return null;
                }
                ArrayList lstColumns = olstColumns as ArrayList;
                isOk = true;
                return lstColumns;
            } catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetColumnsFromInfraGridFromBand");
            }
        }

        internal ArrayList GetColumnsFromInfraGrid(object oSourceControl, string strPegName, string strObjName, ref string strError, ref bool isOk, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetColumnsFromInfraGrid");
            //string strCapExpFixed = strColName.Replace(" ", @"\s"); //空格无法在正则表达式中正确处理
            bool isNoMemberExists = false;
            object oDisplayLayout = ReflectorForCSharp.GetMember(oSourceControl, "DisplayLayout", ref isNoMemberExists);
            if (isNoMemberExists)
            {
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = string.Format("no DisplayLayout for Grid, wrong Ultragrid version?\r\ntype:[{0}]\r\n{1}", oSourceControl.GetType(), oSourceControl.GetType().Assembly.GetName()));
                strError = "Object does not contain DisplayLayout property";
                isOk = false;
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure object is a UltraGrid";
                return null;
            }
            object oBands = ReflectorForCSharp.GetMember(oDisplayLayout, "Bands", ref isNoMemberExists);
            if (isNoMemberExists)
            {
                simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Bands for Grid.DisplayLayout, wrong Ultragrid version?");
                strError = $"Object [{strPegName}.{strObjName}] does not contain columns for DisplayLayout property";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }

            return GetColumnsFromInfraGridFromBand(oBands, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);

        }

        internal MarsColumnForTableInfo GetOneColumnInfo(object oneColumn, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            if (oneColumn == null)
            {
                isOk = false;
                strError = "Column Info is null";
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return null;
            }

            MarsColumnForTableInfo rslt = new MarsColumnForTableInfo();
            bool isNotExist = false;

            object oStyle = ReflectorForCSharp.GetMember(oneColumn, "Style", ref isNotExist);
            if (isNotExist)
            {
                isOk = false;
                strError = $"no Style find in [{oneColumn.GetType()}]";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "contact Marquis";
                return null;
            }

            object oHeader = ReflectorForCSharp.GetMember(oneColumn, "Header");
            if (oHeader == null)
            {
                strError = $"No 'Header' from [{oneColumn.GetType()}]";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "contact Marquis";
                return null;
            }
            bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
            rslt.isHidden = isHidden;

            string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
            string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oneColumn, "Key");
            //simpleLog.MarsLoggerSimple.Info("\t", string.Format("Header returns:[{0}], total columns:[{1}] - cur:[{2}] - compare:[{3}-{4}]", oHeader == null ? "NULL" : string.Format("{0}-{1}", caption, strKeyTmp), lstColumns.Count, i, strColName, strCapExpFixed));
            int idxTmp = ReflectorForCSharp.GetMemberByType<int>(oneColumn, "Index");

            rslt.columnKey = strKeyTmp;
            rslt.columnName = caption;
            isOk = true;
            return rslt;
        }

        internal List<MARSColumnsInfo> GetColumnsAsMarsInfo(ArrayList lstColumns, ref string strError,
            ref string strAdv,
            ref string strStack,
            ref bool isOk,
            bool isCheckingHidden = true)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetColumnsAsMarsInfo",$"[{iMark}]");
            bool isNotExist = false;
            List<MARSColumnsInfo> lstRslt = new List<MARSColumnsInfo>();
            try
            {
                for (int i = 0; i < lstColumns.Count; i++)
                {
                    object oColumnItm = lstColumns[i];
                    if (oColumnItm == null) continue;

                    object oStyle = ReflectorForCSharp.GetMember(oColumnItm, "Style", ref isNotExist);
                    if (isNotExist)
                    {
                        isOk = false;
                        simpleLog.MarsLoggerSimple.Error("GetColumnsAsMarsInfo", $"[{iMark}]");
                        strError = $"no Style find in [{oColumnItm.GetType()}]";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = Resources.mars_contact_to_marquis;
                        return null;
                    }

                    object oHeader = ReflectorForCSharp.GetMember(oColumnItm, "Header");
                    if (oHeader == null) continue;
                    bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
                    if ((isHidden) && (isCheckingHidden)) continue;

                    string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
                    string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oColumnItm, "Key");                   
                    int idxTmp = ReflectorForCSharp.GetMemberByType<int>(oColumnItm, "Index");

                    MARSColumnsInfo tmpCol = new MARSColumnsInfo();
                    tmpCol.idxOfKey = idxTmp;
                    tmpCol.columnCaption = caption;
                    tmpCol.columnKey = strKeyTmp;

                    lstRslt.Add(tmpCol);
                }
                isOk = true;
                return lstRslt;
            }
            catch (Exception ex)
            {
                strError = Resources.mars_mq_infragistics_table_cant_get_column_info;
                simpleLog.MarsLoggerSimple.Error("GetColumnsAsMarsInfo", $"[{iMark}]{Resources.mars_mq_infragistics_table_cant_get_column_info},\r\n{ex.Message}", 
                    ex.StackTrace);
                strStack = ex.StackTrace;
                isOk = false;
                strAdv = Resources.mars_contact_to_marquis;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetColumnsAsMarsInfo", $"[{iMark}], isOk[{isOk}]");
            }
        }
        /// <summary>
        /// if CurrentColIdx !=-1 then, return the index infor, otherwise, search by colName
        /// </summary>
        /// <param name="lstColumns"></param>
        /// <param name="strColName"></param>
        /// <param name="strCaption"></param>
        /// <param name="strKey"></param>
        /// <param name="idx"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <param name="isOk"></param>
        /// <param name="isCheckingHidden"></param>
        /// <param name="currentColIdx"></param>
        /// <returns></returns>
        internal object GetColumnByIdx(ArrayList lstColumns,
            string strColName,
            ref string strCaption,
            ref string strKey,
            ref int idx,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            ref bool isOk,
            bool isCheckingHidden = true,
            int currentColIdx=-1)
        {
            string strCapExpFixed = strColName.Replace(" ", @"\s"); //空格无法在正则表达式中正确处理
            string strAllKeys = "";
            bool isNotExist = false;
            object tmpStyle = null;

            if (currentColIdx >= 0)
            {
                if (currentColIdx>= lstColumns.Count)
                {
                    strError = $"col index|{currentColIdx}| is greater than columnCount|{lstColumns.Count}|";
                    return false;
                }
            }
            int istart=currentColIdx<0?0:currentColIdx-1,
                iEnd =currentColIdx<0?lstColumns.Count:currentColIdx;
            //for (int i = 0; i < lstColumns.Count; i++)
            for (int i = istart; i < iEnd; i++)
            {
                object oColumnItm = lstColumns[i];
                if (oColumnItm == null) continue;

                object oStyle = ReflectorForCSharp.GetMember(oColumnItm, "Style", ref isNotExist);
                if (isNotExist)
                {
                    isOk = false;
                    strError = $"no Style find in [{oColumnItm.GetType()}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "contact Marquis";
                    return null;
                }
                simpleLog.MarsLoggerSimple.DEBUG("GetColumnByIdx", $"cell Style|{oStyle.ToString()}");


                object oHeader = ReflectorForCSharp.GetMember(oColumnItm, "Header");
                if (oHeader == null) continue;
                bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
                if ((isHidden) && (isCheckingHidden)) continue;

                string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
                string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oColumnItm, "Key");
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("Header returns:[{0}], total columns:[{1}] - cur:[{2}] - compare:[{3}-{4}]", oHeader == null ? "NULL" : string.Format("{0}-{1}", caption, strKeyTmp), lstColumns.Count, i, strColName, strCapExpFixed));
                int idxTmp = ReflectorForCSharp.GetMemberByType<int>(oColumnItm, "Index");

                strAllKeys = string.Format("{0};[{1}]-[{2}]", strAllKeys, caption, strKeyTmp);
                simpleLog.MarsLoggerSimple.Info("\t", $"all keys:{strAllKeys}");
                if (currentColIdx >= 0)
                {
                    strKey = strKeyTmp;
                    idx = idxTmp;
                    strCaption = caption;
                    tmpStyle = oStyle;
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("index model, column idx:[{0}] i-[{1}], columnStyle is:[{2}]-[styleType:{3}]",
                        idx, i, tmpStyle, tmpStyle?.GetType()));
                    this.currentColumnStyle = tmpStyle;
                    isOk = true;
                    return oColumnItm;
                } else if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, caption))
                    || (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, strKeyTmp))
                    || (string.Compare(strColName, caption, true) == 0)
                    || (string.Compare(strColName, strKeyTmp, true) == 0)
                    )
                {
                    strKey      = strKeyTmp;
                    idx         = idxTmp;
                    strCaption  = caption;
                    tmpStyle    = oStyle;
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("column idx:[{0}] i-[{1}], columnStyle is:[{2}]-[styleType:{3}]",
                        idx, i, tmpStyle, tmpStyle?.GetType()));
                    this.currentColumnStyle = tmpStyle;
                    isOk = true;
                    return oColumnItm;
                }
            }
            isOk = false;
            strError = string.Format("Can't find key:[{0}] from [{1}]", strColName, strAllKeys);
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Check column Header captions' setting";
            return null;
        }

        public object targetColumnForScrollCol = null;

        internal bool GetColumnKeyForInfragisticsGrid(object oSourceControl, SearchAndClickPara_Multiple colunmsInfo, string strPegName, string strObjName, ref string strKey, ref int idx, ref string strError,
            ref string strAdv,
            ref string strStack,
            bool isCheckingHidden = true)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetColumnKeyForInfragisticsGrid", string.Format("columnNames:[{0}]", colunmsInfo == null ? "NULL" : colunmsInfo.ToString()));
            try
            {
                bool isOk = false;
                ArrayList lstColumns = GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                if ((!isOk) || (lstColumns == null))
                {
                    return false;
                }
                string[] arrColNames = colunmsInfo.columns;
                string strCaption = "";
                if (arrColNames.Length == 0)
                {
                    strError = "No column is set.";
                    strAdv = "Contact Marquis";
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("\tGetColumnKeyForInfragisticsGrid", strError);
                    return false;
                }
                int tmpIdx = 0;
                idx = -1;
                for (int i = 0; i < arrColNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(arrColNames[i]))
                    {
                        strError = $"Some column name is blank.";
                        strStack = Environment.StackTrace;
                        strAdv = "Check and fix the test step's setting";
                        simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError);
                        return false;
                    }
                    object targetColumn = GetColumnByIdx(lstColumns, arrColNames[i], ref strCaption, ref strKey, ref tmpIdx, ref strError, ref strAdv, ref strStack,
                        ref isOk, isCheckingHidden);
                    if (!isOk)
                    {
                        return false;
                    }
                    if (idx == -1)
                        idx = tmpIdx;
                    colunmsInfo.putColumnInfoWithIdx(tmpIdx, targetColumn, strCaption, strKey);
                }
                return true;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetColumnKeyForInfragisticsGrid");
            }
        }

        internal object currentColumnStyle = null;
        internal bool GetColumnKeyForInfragisticsGrid(object oSourceControl, string strColName,
            string strPegName, string strObjName,
            ref string strKey,
            ref int idx, ref string strError,
            ref string strAdv,
            ref string strStack,
            int currentIdx = -1)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetColumnKeyForInfragisticsGrid", $"columnName|[{strColName}]|currentIdx|{currentIdx}|");
            try
            {
                string strCapExpFixed = strColName.Replace(" ", @"\s"); //空格无法在正则表达式中正确处理
                #region replaced by method GetColumnsFromInfraGrid
                //bool isNoMemberExists = false;
                //object oDisplayLayout = ReflectorForCSharp.GetMember(oSourceControl, "DisplayLayout", ref isNoMemberExists);
                //if (isNoMemberExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = string.Format("no DisplayLayout for Grid, wrong Ultragrid version?\r\ntype:[{0}]\r\n{1}", oSourceControl.GetType(),oSourceControl.GetType().Assembly.GetName()));
                //    return false;
                //}
                //object oBands = ReflectorForCSharp.GetMember(oDisplayLayout, "Bands", ref isNoMemberExists);
                //if (isNoMemberExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Bands for Grid.DisplayLayout, wrong Ultragrid version?");
                //    return false;
                //}
                //object oLstFromBands = ReflectorForCSharp.GetMember(oBands, "List", ref isNoMemberExists);
                //if (isNoMemberExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no List for Grid.DisplayLayout.Bands, wrong Ultragrid version?");
                //    return false;
                //}
                //if (!(oLstFromBands is ArrayList))
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",
                //        strError = string.Format("List from Grid.DisplayLayout.Bands should be ArrayList, it is [{0}]. wrong Ultragrid version?", oLstFromBands.GetType().ToString()));
                //    return false;
                //}
                ////the first item contains columns
                //object BandsContainsColumn = (oLstFromBands as ArrayList)[0];
                //object ColumnsInBand0 = ReflectorForCSharp.GetMember(BandsContainsColumn, "Columns", ref isNoMemberExists);
                //if (isNoMemberExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0], wrong Ultragrid version?");
                //    return false;
                //}
                //object olstColumns = ReflectorForCSharp.GetMember(ColumnsInBand0, "List", ref isNoMemberExists);
                //if (isNoMemberExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0].Columns, wrong Ultragrid version?");
                //    return false;
                //}
                //if ((!(olstColumns is ArrayList)) || (olstColumns == null))
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",
                //        strError = string.Format("List from Grid.DisplayLayout.Bands[0].Columns should be ArrayList, it is [{0}]. wrong Ultragrid version?", olstColumns.GetType().ToString()));
                //    return false;
                //}
                //ArrayList lstColumns = olstColumns as ArrayList;
                #endregion
                bool isOk = false;


                ArrayList lstColumns = null;
                if (oSourceControl is System.Windows.Forms.Control)
                {
                    System.Windows.Forms.Control c = oSourceControl as System.Windows.Forms.Control;
                    if (c.InvokeRequired)
                    {
                        string strErrorTmp = "", strAdvTmp = "", strStackTmp = "";
                        c.Invoke(new Action(() => {
                            lstColumns = GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strErrorTmp, ref isOk, ref strAdvTmp, ref strStackTmp);
                            simpleLog.MarsLoggerSimple.Info("GetColumnKeyForInfragisticsGrid", $"Invoke|isOK|{isOk}|strErrorTmp|{strErrorTmp}|strStackTmp|{strStackTmp}"); 
                        }));
                        strError = strErrorTmp;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                    }
                    else
                    {
                        lstColumns = GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                    }
                }
                else
                    lstColumns = GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                if ((!isOk) || (lstColumns == null))
                {
                    return false;
                }

                string strAllKeys = "", strCaption = "";
                int idxTmp = -1;
                #region replaced by method GetHeaderByIdx
                /*
                for (int i = 0; i < lstColumns.Count; i++)
                {
                    object oColumnItm = lstColumns[i];
                    if (oColumnItm == null) continue;
                    object oHeader = ReflectorForCSharp.GetMember(oColumnItm, "Header");
                    if (oHeader == null) continue;
                    bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
                    if (isHidden) continue;
                    string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
                    string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oColumnItm, "Key");
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Header returns:[{0}], total columns:[{1}] - cur:[{2}] - compare:[{3}-{4}]", oHeader == null ? "NULL" : string.Format("{0}-{1}", caption, strKeyTmp), lstColumns.Count, i, strColName, strCapExpFixed));
                    idxTmp = ReflectorForCSharp.GetMemberByType<int>(oColumnItm, "Index");

                    strAllKeys = string.Format("{0};[{1}]-[{2}]", strAllKeys, caption, strKeyTmp);

                    if ((Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, caption))
                        || (Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, strKeyTmp))
                        || (string.Compare(strColName, caption, true) == 0)
                        || (string.Compare(strColName, strKeyTmp, true) == 0)
                        )
                    {
                        strKey = strKeyTmp;
                        idx = idxTmp;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("column idx:[{0}] i-[{1}]" ,idx, i));
                        return true;
                    }
                }
                strError = string.Format("Can't find column [{1}] from all keys [{0}]", strAllKeys, strColName);
                */
                #endregion replaced by GetHeaderByIdx
                object targetColumn = GetColumnByIdx(lstColumns, strColName, ref strCaption, ref strKey, ref idx, ref strError, ref strAdv, ref strStack,
                    ref isOk, currentColIdx:currentIdx);
                if (isOk)
                    targetColumnForScrollCol = targetColumn;
                return isOk;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetColumnKeyForInfragisticsGrid");
            }
        }

        internal List<MARSColumnsInfo> GetAllColumnsInfo(object oSourceControl,
            string strPegName, string strObjName,ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetAllColumnsInfo", $"[{iMark}] peg:[{strPegName}]-obj:[{strObjName}]");
            try
            {
                ArrayList lstColumns = GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                if ((!isOk) || (lstColumns == null))
                {
                    return null;
                }
                return GetColumnsAsMarsInfo(lstColumns, ref strError, ref strAdv, ref strStack, ref isOk);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetAllColumnsInfo", ex.Message, ex.StackTrace);
                isOk = false;
                strError = string.Format(Resources.mars_mq_capture_batch_unknow_excaption, strPegName, strObjName);
                strAdv = Resources.mars_contact_to_marquis;
                strStack = ex.StackTrace;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllColumnsInfo",$"[{iMark}]--isOK [{isOk}]");
            }
        }

        internal object[] GetRowsFromGridControl(object tbl, ref bool isOk, ref int iCount, ref string strError, ref string strAdv, ref string strStack)
        {
            bool isNotExists = false;
            object rows = ReflectorForCSharp.GetMember(tbl, "Rows", ref isNotExists);
            if ((isNotExists) || (rows == null))
            {
                simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = "Can't get member Rows from object by reflector, or rows return null.");
                isOk = false;
                strError = "Object does not contain Rows";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure object is a UltraGrid";
                return null;
            }

            MarsReflectCompare<int> afunc = (int v1, int v2, ref bool xx, ref string ss) =>
            {
                xx = true;
                return v1 - v2;
            };

            ReflectorForCSharp of = new ReflectorForCSharp();
            if (!of.WaitUntilMembersGreater<int>(rows, "Count", 0, ref strError, afunc))
            {
                simpleLog.MarsLoggerSimple.Error("GetRowsFromGridControl", strError);
                strError = "Object does not contains count";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure object is a UltraGrid";
                isOk = false;
                return null;
            }


            object oall = ReflectorForCSharp.GetMember(rows, "All", ref isNotExists);
            if ((isNotExists) || (oall == null))
            {
                simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = "Object does not contain columns");// "Can't get member All from object by reflector, or All return null.");
                isOk = false;
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure object is a UltraGrid";
                return null;
            }
            //int iCount = ReflectorForCSharp.GetMemberByType<int>(rows, "Count");
            //if ((iCount == default(int)) || (iCount < 0))
            //{
            //    isOk = false;
            //    simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = "Object contains 0 rows");//"0 row returns");
            //    return null;
            //}
            object[] arrAll = ReflectorForCSharp.GetMemberByType<object[]>(rows, "All");
            if (arrAll == null)
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = "Object property [All] value is NULL");
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            isOk = true;
            iCount = arrAll.Length;
            return arrAll;
        }
        internal object GetRowByCommand(object tbl, string strCmd, object[] arrAll, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
           
            int iCount = arrAll==null?-1: arrAll.Length;

            //object[] arrAll = GetRowsFromGridControl(tbl, ref isOk, ref iCount, ref strError, ref strAdv, ref strStack);
            simpleLog.MarsLoggerSimple.Info("\t", $"GetRowsFromGridControl.count = [{iCount}]");
            if (string.Compare("dynamicrow", strCmd, true) == 0)
            {
                isOk = true;
                return arrAll[iCount - 1];
            }

            int iRowIdxFromCmd;
            if (int.TryParse(strCmd, out iRowIdxFromCmd))
            {
                if (iRowIdxFromCmd >= iCount)
                {
                    simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = "Requested row number is larger than total number of rows"); //string.Format("[{0}] is greater than total row count:[{1}]", iRowIdxFromCmd, iCount));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Check the row number requested";
                    isOk = false;
                    return null;
                }
                isOk = true;
                return arrAll[iRowIdxFromCmd];
            }
            simpleLog.MarsLoggerSimple.Error("GetRowByCommand", strError = $"Current Keyword does not support parameter [{strCmd}]");//string.Format("unsupported command -[{0}]", strCmd));
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Check the keyword/parameter, see user manual";
            isOk = false;
            return null;
        }

        internal object GetTargetRowFromRows(object oRows, int iRowNumber, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {

            if (oRows == null)
            {
                strError = "Object property [Rows] value is NULL";//"Rows is null";              
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }

            object[] arrAll = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
            if ((arrAll == null))
            {
                simpleLog.MarsLoggerSimple.Error("GetTargetRowFromRows", strError = "Object property [All] value is NULL");// "Can't get member All from object by reflector, or All return null.");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            if (arrAll.Length <= iRowNumber)
            {
                strError = "Object property [All] value is NULL in Rows";// "No all in Rows, different version of Infragistics
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            isOk = true;
            return arrAll[iRowNumber];
        }

        internal object GetCellFromOneRow(object oRow, int idxCell, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {

            bool isNotExists = false;
            object oCells = ReflectorForCSharp.GetMember(oRow, "Cells", ref isNotExists);
            if (isNotExists)
            {
                strError = "No cells found in a row";// "Can't find Cells from One Row, Wrong version?";
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            object oCell = reflector.CallMethod(oCells, "get_Item", new Type[] { typeof(int) }, ref isNotExists, new object[] { idxCell });
            if (isNotExists)
            {
                strError = "Can't find target cell";//string.Format("No such cell index exists [{0}]", idxCell);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure grid cell identifiacation is correct";
                isOk = false;
                return null;
            }

            isOk = true;
            return oCell;
        }

        internal bool IsComboBoxCell()
        {
            
            if (this.currentColumnStyle == null) return false;
            simpleLog.MarsLoggerSimple.DEBUG("IsComboBoxCell", $"columnStyle|{this.currentColumnStyle}");
            if (this.currentColumnStyle.ToString().Equals(cnst_Infragistics_Cell_DropDown, StringComparison.OrdinalIgnoreCase)
                || this.currentColumnStyle.ToString().Equals(cnst_Infragistics_Cell_DropDownValidate, StringComparison.OrdinalIgnoreCase)
                || this.currentColumnStyle.ToString().Equals(cnst_Infragistics_Cell_DropDownList, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
        //[STAThread]
        async void clickAtByTask(System.Windows.Forms.Control c, int x, int y)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                c.Invoke(new Action(() =>
                {
                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                }));
                //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(x, y);
            });
        }


        internal bool FillCell(System.Windows.Forms.Control gridCntrol, object row, string strKey, int idx, 
            string strData, string strAttachInfo,
            string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            int delOrBackCount=50,
            bool isInProcess = true,
            string theThirdPara=null)
        {
            simpleLog.MarsLoggerSimple.logBegin("FillCell", $"key|{strKey}|idx|{idx}|data|{strData}|theThirdPara|{theThirdPara}");
            if ((row == null))
            {
                strError = "Passed NULL to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                simpleLog.MarsLoggerSimple.Error("FillCell", strError);
                return false;
            }
            ///需要判断是不是Infragistics？
            /// 
            bool isNotExists = false;
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            object rowUI = ReflectorForCSharp.GetMember(row, "UIElement", ref isNotExists);
            simpleLog.MarsLoggerSimple.Info("FillCell", $"UIElement|if not null|{rowUI?.ToString()}");
            //if ((isNotExists)||(rowUI==null))
            //{
            //    simpleLog.MarsLoggerSimple.Error("FillCell", strError = "No UIElement is find, wrong version of Infragistics?");
            //    return false;
            //}
            object oCells = ReflectorForCSharp.GetMember(row, "Cells", ref isNotExists);
            if ((isNotExists) || (oCells == null))
            {
                simpleLog.MarsLoggerSimple.Error("FillCell", strError = "Object property [Cells] is NULL");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            
            // try to get this here
            object oCell = null;
            MarsReflectCompare<bool> afunc = (bool v1, bool v2, ref bool xx, ref string ss) =>
            {
                xx = true;
                return (v1 ? 1 : 0) - (v2 ? 1 : 0);
            };
            try
            {
                oCell = reflector.CallMethod(oCells, "get_Item", new Type[] { typeof(int) }, ref isNotExists, new object[] { idx });
                //object oCellUIElment = reflector.CallMethod(oCell, "GetUIElement",new Type[] {null},ref isNotExists, new object[] { });
                object oCellUIElment = reflector.CallMethod(oCell, "GetUIElement", new Type[] { }, ref isNotExists, new object[] { });
                if ((oCellUIElment == null) || (isNotExists))
                {
                    simpleLog.MarsLoggerSimple.Error("FillCell", strError = "Object UIElement property is null");//"No object return after call GetUIElment");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }

                System.Drawing.Rectangle oRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "Rect");
                if (oRect.Equals(default(System.Drawing.Rectangle)))
                {
                    simpleLog.MarsLoggerSimple.Error("FillCell", strError = "Object property Rectangle is null"); //"No Rect object return,Wrong Infragistics Version?");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Drawing.Point ptNew = gridCntrol.PointToScreen(new System.Drawing.Point(oRect.Location.X + oRect.Width / 2, oRect.Location.Y + oRect.Height / 2));
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("src rect:[{0}], new Left top:[{1}]", oRect.ToString(), ptNew));                

                bool isExit = false ;
                bool isForceUseSendKeys = false;
                int extraWait = 1;
                if (!string.IsNullOrEmpty(theThirdPara))
                {
                    isForceUseSendKeys = (string.Compare("3", theThirdPara,true)==0)||(theThirdPara.IndexOf("DirectWrite", StringComparison.OrdinalIgnoreCase)>=0);
                    var arrWait = theThirdPara.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (arrWait.Length > 1)
                    {
                        if (int.TryParse(arrWait[1], out extraWait))
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", $"extra wait time is [{extraWait}]");
                        }
                    }
                }
                if ((this.IsComboBoxCell())&&(!isForceUseSendKeys))
                {
                    /// 算法，
                    /// 1, 点击右侧的下拉列表
                    /// 2，获得新的popup窗口
                    /// 3，设置新的index
                    /// 
                    System.Threading.Thread.Sleep(100);
                    System.Drawing.Point dropdownBtnPt = new System.Drawing.Point(ptNew.X + oRect.Width / 2 - 6,
                        ptNew.Y );
                    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => {
                        simpleLog.MarsLoggerSimple.Info("\t", "IsComboBoxCell click mouse");
                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(dropdownBtnPt.X, dropdownBtnPt.Y);
                    //}));
                    System.Threading.Thread.Sleep(100);

                    System.Drawing.Point dropdownListViewPt = new System.Drawing.Point(ptNew.X,
                        ptNew.Y + oRect.Height / 2 + 20);
                    
                    Cursor.Position = new Point(dropdownListViewPt.X, dropdownListViewPt.Y);
                    Thread.Sleep(150);

                    simpleLog.MarsLoggerSimple.Info("\t", $"ptNew [{ptNew}] - supposed position:[{dropdownListViewPt}]");
                    //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(dropdownListViewPt.X, dropdownListViewPt.Y);
                    //System.Threading.Thread.Sleep(1000);

                    string strErrorTmp = "", strStackTmp = "", strAdvTmp = "";
                    Control dropDownListView = null;
                    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    //{
                        dropDownListView = Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.FromScreenPoint(dropdownListViewPt,
                        ref strErrorTmp, ref strStackTmp);
                    //}));
                    
                    if (dropDownListView == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError=strErrorTmp, MarsErrorStacks.StackTraceDump());
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    
                    simpleLog.MarsLoggerSimple.Info("\t", $"contrl type is [{dropDownListView.GetType()}] after check cell");

                    
                    // 不一定是dropdown
                    if (!MarsComboboxOperation.cnst_DropdownType.Equals(dropDownListView.GetType().FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        strError = "Can't find dropdown window after click the Grid Cell. The Cell is not a dropdown style?";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    simpleLog.MarsLoggerSimple.Info("\t", $"before change index [{strData}]");
                    Mars_ValueListDropDownUnsafe_Op valueListUnSafeOp = new Mars_ValueListDropDownUnsafe_Op(dropDownListView, strData);
                    bool isOk = valueListUnSafeOp.IndexOf(gridCntrol, ref strError, ref strStack, ref strAdv);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError, strStack);
                        return false;
                    }
                    Thread.Sleep(100);
                    //System.Windows.Forms.SendKeys.SendWait("{enter}");
                    return true;
                }
                else
                {
                    //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIs.ReleaseCapture();
                    //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIs.SetCapture(gridCntrol.Handle);

                    simpleLog.MarsLoggerSimple.Info("\t",$"ptNew:{ptNew.X}-{ptNew.Y}" );

                    //ThreadHelper.JoinableTaskFactory.Run(async delegate {
                    //    // Switch to main thread
                    //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    //    // Do your work on the main thread here.
                    //    simpleLog.MarsLoggerSimple.Info("\t", $"click at cccc ptNew:{ptNew.X}-{ptNew.Y}");
                    //    Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                    //    Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                    //});
                    //ThreadHelper.JoinableTaskFactory.Run(async delegate {
                    //    await TaskScheduler.Default;
                    //    // You're now on a separate thread.
                    //    simpleLog.MarsLoggerSimple.Info("\t", $"click at ptNew JoinableTaskFactory:{ptNew.X}-{ptNew.Y}");
                    //    Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                    //    Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                    //    //await OrSomethingAsynchronous();
                    //});
                    //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                    try
                    {
                        //clickAtByTask(gridCntrol,ptNew.X, ptNew.Y);
                    }
                    finally
                    {
                        //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIs.ReleaseCapture();
                    }
                    //var frm = HighlightWindow.getInstance();
                    //frm.Hide();

                    //frm.Left = ptNew.X - 1;
                    //frm.Top = ptNew.Y - 1;
                    //frm.Width = oRect.Width + 1;
                    //frm.Height = oRect.Height + 1;
                    //frm.Show();
                    //frm.Update();
                    //System.Threading.Thread.Sleep(5000);
                    //HighlightWindow.HideAndDestroy();

                    System.Threading.Thread.Sleep(50);
                    if (isInProcess)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "inprocess click left");

                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                        Thread.Sleep(extraWait*1000);                       
                        
                        if (!isForceUseSendKeys)
                        {
                            var IsActiveCell = ReflectorForCSharp.GetMember(oCell, "Activated", ref isNotExists);
                            simpleLog.MarsLoggerSimple.Info("\t", $"Activated is [{IsActiveCell}]-[{IsActiveCell.GetType()}]");
                            simpleLog.MarsLoggerSimple.Info("\t", $"isInProcess|{isInProcess}|!isForceUseSendKeys|{!isForceUseSendKeys}");
                            try
                            {
                                strError = "";
                                strStack = "";
                                if (!(Boolean)IsActiveCell)
                                {
                                    bool isOk = reflector.SetMemberValue(true, oCell, "Activated", ref strError, ref strStack);
                                    simpleLog.MarsLoggerSimple.Info("\t", $"set Activated [{isOk}] - [{strError}] - [{strStack}]");
                                }
                            }
                            catch (Exception e)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", e.Message, e.StackTrace);
                            }

                            #region diffrent version
                            //var isInEditMode = ReflectorForCSharp.GetMember(oCell, "IsInEditMode", ref isNotExists);
                            //if ((!isNotExists)||(isInEditMode==null))
                            //{
                            //    simpleLog.MarsLoggerSimple.Info("\t", "isInEditMode is not exists or null");
                            //    System.Threading.Thread.Sleep(50);
                            //}
                            //else
                            //{
                            //    simpleLog.MarsLoggerSimple.Info("\t", $"type is :[{isInEditMode.GetType()}]");
                            //    try
                            //    {
                            //        if (!(bool)isInEditMode)
                            //        {
                            //            if (!reflector.SetMemberValue(true, oCell, "IsInEditMode", ref strError, ref strStack))
                            //            {
                            //                simpleLog.MarsLoggerSimple.Info("\t",$"not set IsInEditMode sucess");
                            //            }
                            //            else
                            //            {
                            //                simpleLog.MarsLoggerSimple.Info("\t", $"set IsInEditMode sucessfully");
                            //            }
                            //        }
                            //    }
                            //    catch (Exception e)
                            //    {
                            //        simpleLog.MarsLoggerSimple.Error("\t", e.Message, e.StackTrace);
                            //    }
                            //}
                            #endregion
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                        }
                        Thread.Sleep(50);
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "ExecuteClickOutSide click left");
                        ExecuteClickOutSide(ptNew.X, ptNew.Y, Mars_mouseSubType.en_LeftDblClick);
                    }
                    //ExecuteKeyboardEventOutSide("{BACKSPACE 20}");
                    //ExecuteKeyboardEventOutSide("{DEL 20}");
                    //isExit = true;
                    //reflector.WaitUntilMembersEquals<bool>(oCell, "Activated", true, ref strError, afunc);
                    //for (int i = 0; i < delOrBackCount; i++)
                    //{
                    //    System.Windows.Forms.SendKeys.SendWait("{BACKSPACE}");

                    //    System.Threading.Thread.Sleep(5);
                    //}
                    System.Threading.Thread.Sleep(50);
                    if (!isForceUseSendKeys)
                        System.Windows.Forms.SendKeys.SendWait("{HOME}+{END}{BACKSPACE}{DEL}");
                    //for (int i = 0; i < delOrBackCount; i++)
                    //{
                    //    System.Windows.Forms.SendKeys.SendWait("{Del}");
                    //    System.Threading.Thread.Sleep(5);
                    //}
                    System.Threading.Thread.Sleep(150);
                    simpleLog.MarsLoggerSimple.Info("\t", $"going to send keys|{strData}|");    
                    System.Windows.Forms.SendKeys.SendWait(strData);
                    //ExecuteKeyboardEventOutSide(strData);

                    string[] arrErrorColor = null;
                    /// is required to check error 
                    /// 
                    if (ClientDealWithGUIKeyword.currentStepCheckError != null)
                    {
                        arrErrorColor = ClientDealWithGUIKeyword.currentStepCheckError.errorColor==null?null: ClientDealWithGUIKeyword.currentStepCheckError.errorColor.ToArray();
                    }
                    else
                    {

                        if (string.IsNullOrEmpty(strAttachInfo))
                        {
                            return true;
                        }

                        //if (isExit) return true;

                        string[] array = strAttachInfo.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                        if (array.Length < 3 || string.Compare(array[0], "MarsAutoCheckError", true) != 0)
                        {
                            MarsLoggerSimple.Error("filltable", strError = $"unsupported extension command:[{strAttachInfo}]\r\n\tRight now, only MarsAutoCheckError is supported");
                            return true;
                        }
                        //if (string.Compare(array[1], "MarsAutoCheckError", true) == 0)
                        //{
                        //    MarsLoggerSimple.Error("filltable", strError = $"un........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................supported extension command:[{strAttachInfo}]\r\n\tRight now, only MarsAutoCheckError is supported");
                        //    return true;
                        //}
                        string text = array[array.Length - 1];
                        arrErrorColor = text.Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    Color bkColor = Color.White;
                    bool isOkTmp = true;
                    string strErrorTmp = "",
                        strAdvTmp = "",
                        strStackTmp = "";
                    gridCntrol.Invoke((Action)delegate
                    {
                        try
                        {
                            object oEditor = ReflectorForCSharp.GetMember(oCell, "Editor");
                            if (oEditor == null)
                            {
                                object oAppearance = ReflectorForCSharp.GetMember(oCell, "Appearance");
                                if (oAppearance == null)
                                {
                                    isOkTmp = false;
                                    strErrorTmp = "Object property [Appearance] value is NULL in Editor"; //"Can't find property Appearance from Cell";
                                strStackTmp = MarsErrorStacks.StackTraceDump();
                                    strAdvTmp = "Contact Marquis";
                                }
                                else
                                {
                                    object oBackColor = ReflectorForCSharp.GetMember(oAppearance, "BackColor");
                                    if (oBackColor == null || !(oBackColor is Color))
                                    {
                                        isOkTmp = false;
                                        strErrorTmp = oBackColor == null ? "Object property [BackColor] value is NULL in TextBox" : "Object property [BackColor]'s type is not Color in TextBox";//"TextBox.BackColor is null or not type of Color";
                                        strStackTmp = MarsErrorStacks.StackTraceDump();
                                        strAdvTmp = "Contact Marquis";
                                    }
                                    else
                                    {
                                        bkColor = (Color)oBackColor;
                                    }
                                }
                            }
                            else
                            {
                                object oTextBox = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(oCell, "Editor"), "TextBox");
                                if (oTextBox == null)
                                {
                                    object oAppearance = ReflectorForCSharp.GetMember(oCell, "Appearance");
                                    if (oAppearance == null)
                                    {
                                        isOkTmp = false;
                                        strErrorTmp = "Object property [Appearance] value is NULL in Cell";//"Can't find property Appearance from Cell";                                   
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                        strAdvTmp = "Contact Marquis";
                                    }
                                    else
                                    {
                                        object oBackColor = ReflectorForCSharp.GetMember(oAppearance, "BackColor");
                                        if (oBackColor == null || !(oBackColor is Color))
                                        {
                                            isOkTmp = false;
                                            strErrorTmp = "BackColor is null or not type of Color";
                                            StackFrame stck = (new StackFrame());
                                            strStackTmp = MarsErrorStacks.StackTraceDump();
                                            strAdvTmp = "Contact Marquis";
                                        }
                                        else
                                        {
                                            bkColor = (Color)oBackColor;
                                        }
                                    }
                                }
                                else
                                {
                                    object oBackColor = ReflectorForCSharp.GetMember(oTextBox, "BackColor");
                                    if (oBackColor == null || !(oBackColor is Color))
                                    {
                                        isOkTmp = false;
                                        strErrorTmp = oBackColor == null ? "Object property [BackColor] value is NULL in TextBox" : "Object property [BackColor]'s type is not Color in TextBox";//"TextBox.BackColor is null or not type of Color";
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                        strAdvTmp = "Contact Marquis";
                                    }
                                    else
                                    {
                                        bkColor = (Color)oBackColor;
                                    }
                                }
                            }
                        }
                        catch (Exception ex3)
                        {
                            isOkTmp = false;
                            strErrorTmp = $"Error while filling for a grid [{strPegName}].[{strObjName}]";
                            strStackTmp = $"{ex3.Message}\r\n{ex3.StackTrace}";
                            strAdvTmp = "Unidentified error. If this continues, contact Marquis";
                        }
                    });
                    if (!isOkTmp)
                    {
                        isOkTmp = false;
                        strError = strErrorTmp;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return false;
                    }
                    Color[] arrErrorColorInfo = new Color[arrErrorColor.Length];
                    try
                    {
                        for (int k = 0; k < arrErrorColor.Length; k++)
                        {
                            arrErrorColorInfo[k] = Color.FromName(arrErrorColor[k]);
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("filltable", strError = $"Excetion:[{ex.Message}]", ex);
                        strError = $"Error while filling for a grid [{strPegName}].[{strObjName}]";
                        strStack = $"{ex.Message}\r\n{ex.StackTrace}";
                        strAdv = "Unidentified error. If this continues, contact Marquis";
                        return false;
                    }
                    Color color = arrErrorColorInfo.Where((Color p) => p.ToArgb() == bkColor.ToArgb()).FirstOrDefault();
                    if (!color.Equals(default(Color)))
                    {
                        MarsLoggerSimple.Error("FillTable", strError = $"backgroud color is [{color.Name}], which matches error backcolor setting.");
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure the dataset is right";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("FillCell", strError = string.Format("Exception [{0}] when call \"get_item\" form cells, statckTrace:\r\n[{1}]", e.Message, e.StackTrace));
                strError = $"Error while filling a cell for a Grid [{strPegName}][{strObjName}]";
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return false;
            }

        }

        protected bool IsResponding(object obj, IntPtr targetHandle, int iWaitSeconds = -1)
        {
            HandleRef handleRef = new HandleRef(obj, targetHandle);

            int timeout = iWaitSeconds < 0 ? 5000 : iWaitSeconds * 1000; //three minutes for default
            IntPtr lpdwResult;

            IntPtr lResult = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                //handleRef,  
                targetHandle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                (uint)timeout,
                out lpdwResult);
            uint iLastError = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetLastError();
            simpleLog.MarsLoggerSimple.Info("IsResponding", string.Format("SendMessageTimeout returns:[{0}], result:[{1}], lasterror Code:[{2}]", lResult, lpdwResult, iLastError));
            return lResult != IntPtr.Zero;
        }
         
        public List<MarsColumnForTableInfo> GetTableColumnNames(object oGrid,
            string strPegName, string strObjName,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetTableColumnNames", $"pegname:[{strPegName}]-[{strObjName}]");
            List<MarsColumnForTableInfo> rslt = new List<MarsColumnForTableInfo>();
            try
            {
                ArrayList lstColumns = GetColumnsFromInfraGrid(oGrid, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                for (int i = 0; i < lstColumns.Count; i++)
                {
                    if (lstColumns[i] == null) continue; 

                    MarsColumnForTableInfo colMarsInfo = GetOneColumnInfo(lstColumns[i],ref isOk,ref strError, ref strAdv, ref strStack);
                    if ((!isOk) || (colMarsInfo == null))
                    {
                        isOk = false;
                        return null;
                    }
                    colMarsInfo.ord = i;
                    rslt.Add(colMarsInfo);
                }
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                strAdv = "contact Marquis";
                simpleLog.MarsLoggerSimple.Error("GetTableColumnNames", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetTableColumnNames", $"isOk = [{isOk}]");
            }
            
        }

        internal bool FillTableInGroupMode(object oGrid, string strData, string strCaptions, string strobjType, string strAttachInfo, 
            string strPegName, string strObjName, ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            ///判断是否有group的信息
            ///格式:groupname1;groupname2;....groupnameN;ColumnName:FirstCellIndex-SecondeColumn:type or TypeNumber
            ///注意，最后一个是 clumnName+冒号+值 实例: Description:Currency-Value1:1    
            simpleLog.MarsLoggerSimple.logBegin("FillTableInGroupMode", $"data to fill:{strData}, parameter:[{strCaptions}]");
            string[] arrCellInfo = strCaptions.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> lstCaptions = new List<string>(arrCellInfo);
            lstCaptions.RemoveAt(lstCaptions.Count - 1);
            string strLastCaptionIdx = arrCellInfo[arrCellInfo.Length - 1];
            string[] arrLastCellIdxWithHeader = new string[2],
                         arrColumnPart = new string[2];
            bool isLastItemFormatOk = CheckCellLocationFormat(strLastCaptionIdx, ref strError, ref strAdv, ref strStack, ref arrLastCellIdxWithHeader, ref arrColumnPart);
            if (!isLastItemFormatOk) return false;

            //wait until 
            MarsReflectCompare<int> afunc = (int v1, int v2, ref bool xx, ref string ss) =>
            {
                xx = true;
                return v1 - v2;
            };

            ReflectorForCSharp of = new ReflectorForCSharp();
            object olstRows = ReflectorForCSharp.GetMember(oGrid, "Rows");
            if (!of.WaitUntilMembersGreater<int>(olstRows, "Count", 0, ref strError, afunc))
            {
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            int iCount = ReflectorForCSharp.GetMemberByType<int>(olstRows, "Count");
            int iLevel = 0;
            bool isOk = true;

            object oCurrrentRow = FindAndExpandGroupedRow(olstRows, iLevel, lstCaptions, oGrid, ref strError, ref strAdv, ref strStack, ref isOk);
            if (!isOk)
            {
                return false;
            }
            if (oCurrrentRow == null)
            {
                simpleLog.MarsLoggerSimple.Error("MarsTigerActiveRowByFirstCellCaption_UltraGrid", strError = string.Format("No grouped Row find by caption path:[{0}]", lstCaptions));
                strError = "Object property [Grouped] is NULL";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            //定位到目标行，获得字段信息
            string strColKeyIdx = null,
                   strTmpCellText = "",
                   strColLocationKey = "";
            //strErroTmp      = "",
            //strStackTmp     = "",
            //strAdvTmp       = "";
            int iColKeyIdx = -1,
                    iColTargetIdx = -1;
            bool isRowColFind = GetColumnKeyForInfragisticsGrid(oGrid, arrLastCellIdxWithHeader[0], strPegName, strPegName, ref strColKeyIdx, ref iColKeyIdx, ref strError, ref strAdv, ref strStack);
            if (!isRowColFind)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Can't find Row Location ColumnKey or Col Location Column Key :[{0}]", strError));
                return false;
            }
            bool isColLocationFind = GetColumnKeyForInfragisticsGrid(oGrid, arrColumnPart[0], strPegName, strPegName, ref strColLocationKey, ref iColTargetIdx, ref strError, ref strAdv, ref strStack);
            if (!isColLocationFind)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Can't find Row Location ColumnKey or Col Location Column Key :[{0}]", strError));
                return false;
            }
            //simpleLog.MarsLoggerSimple.Info("\t", string.Format("IdxSource:[{0}] IdxValue:[{1}]，TargetSource:[{2}] TargetValue:[{3}]",
            //        arrLastCellIdxWithHeader[0], strColKeyIdx, arrColumnPart[0], strColLocationKey));
            string strWriteCmmd = arrColumnPart[1];
            //if (!(isRowColFind && isColLocationFind))
            //{

            //    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Can't find Row Location ColumnKey or Col Location Column Key :[{0}] /[{1}]", strError, strErroTmp));
            //    strError = $"Can't find column name [{}]";
            //    StackFrame stck = (new StackFrame());
            //    strStack = MarsErrorStacks.StackTraceDump();
            //    strAdv = "dd";
            //    return false;
            //}

            bool isNotExists = false;
            object oActiveRowScrollRegion = ReflectorForCSharp.GetMember(oGrid, "ActiveRowScrollRegion", ref isNotExists);
            if (isNotExists)
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("FillTableInGroupMode", strError = "Object property [ActiveRowScrollRegion]'s value is NULL in Grid");// "No ActiveRowScrollRegion in Grid, wrong infragistics version?");
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            System.Windows.Forms.Control cGrid = oGrid as System.Windows.Forms.Control;
            if (cGrid == null)
            {
                simpleLog.MarsLoggerSimple.Error("FillTableInGroupMode", strError = string.Format("grid is not control, it is :[{0}]", oGrid.GetType()));
                strError = $"Object [{strObjName}] is not a Control";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return isOk = false;
            }

            //获得所有的row
            object oAllRowFromGroup = ReflectorForCSharp.GetMember(oCurrrentRow, "Rows");
            object[] oAllInRows = ReflectorForCSharp.GetMemberByType<object[]>(oAllRowFromGroup, "All");
            int iSubRowCnt = ReflectorForCSharp.GetMemberByType<int>(oAllRowFromGroup, "Count");
           
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            foreach (object itmRow in oAllInRows)
            {
                if (itmRow == null) continue;
                object oCell = GetCellFromOneRow(itmRow, iColKeyIdx, ref isOk, ref strError, ref strAdv, ref strStack);
                if (!(isOk && (oCell != null)))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError);
                    return isOk = false;
                }
                strTmpCellText = reflector.GetMember<string>(oCell, "Text", ref isNotExists);
                if (isNotExists)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "Object property [Text]'s value is NULL in Cell");//"No Text property exists in Cell");                    
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Maruqis";
                    return isOk = false;
                }

                if (!((windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(arrLastCellIdxWithHeader[1], strTmpCellText)
                    || (string.Compare(arrLastCellIdxWithHeader[1], strTmpCellText) == 0))))
                {
                    ///for test
                    //simpleLog.MarsLoggerSimple.Info("\t", string.Format("[{0}]-[{1}]", arrLastCellIdxWithHeader[1], strTmpCellText));
                    continue;
                }

                /// 确定 该行不在可视行里面
                /// 

                if (cGrid.InvokeRequired)
                {
#if dotNet2
#else
                    cGrid.Invoke(new Action(() =>
                    {
                        //这里有个exception 如果不用不带参数的
                        reflector.CallMethodJustByName(oActiveRowScrollRegion, "ScrollRowIntoView", new object[] { itmRow });
                    }));
#endif
                }
                else
                {
                    reflector.CallMethodJustByName(oActiveRowScrollRegion, "ScrollRowIntoView", new object[] { itmRow });
                }
                System.Threading.Thread.Sleep(50);
                IsResponding(cGrid, cGrid.Handle);
                //判断该行是否激活，可以省略

                //获得UIElement
                object oUIElement = reflector.CallMethod(itmRow, "GetUIElement", new Type[] { }, ref isNotExists, null);
                if (isNotExists)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "GetUIElement method doesn't exists.");
                    strError = "Object property [UIElement] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                #region debug
                /*
                //获得目标Cell
                oCell = GetCellFromOneRow(itmRow, iColTargetIdx, ref isOk, ref strError);
                if ((oCell == null) || (!isOk))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError );
                    return isOk = false;
                }
                //获得Cell的UIElement
                object oUICell = reflector.CallMethod(oCell, "GetUIElement", new Type[] { }, ref isNotExists, null);
                if ((oUICell == null)||(isNotExists))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError);
                    return isOk = false;
                }*/
                #endregion
                //到此为止，获得指定的Cell
                isOk = FillCell(cGrid, itmRow, arrColumnPart[0], iColTargetIdx, strData, strAttachInfo, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                simpleLog.MarsLoggerSimple.Info("\t", $"FillCell return {isOk} {strError}");
                return isOk;
                ////获得rect,测试rect 和bounds
                //Rectangle oRect = reflector.GetMember<Rectangle>(oUICell, "Rect", ref isNotExists);
                //if (isNotExists)
                //{
                //    simpleLog.MarsLoggerSimple.Error("\t", strError=string.Format("No Rect property exists in type:[{0}]", oUICell.GetType()));
                //    return isOk = false;
                //}
                //Point clientPt = cGrid.PointToClient(oRect.Location);
                //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(clientPt.X + oRect.Width / 2, clientPt.Y + oRect.Height / 2);
                //if (arrColumnPart.Length < 2)
                //{

                //}
            }
            simpleLog.MarsLoggerSimple.Error("MarsTigerActiveRowByFirstCellCaption_UltraGrid", strError = string.Format("Can't locate the cell [{0}] by [{1}]", strTmpCellText, arrLastCellIdxWithHeader[1]));
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = $"Make sure the cell [{strTmpCellText}] is available in Screen";
            return isOk = false;
            //GetColumnKeyForInfragisticsGrid(oGrid,)
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lstRow"></param>
        /// <param name="iLevel"></param>
        /// <param name="lstCaptionIndexs">多层的grouped head的标题。允许多层</param>
        /// <param name="parentGrid"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        internal object FindAndExpandGroupedRow(object lstRow, int iLevel, List<string> lstCaptionIndexs, object parentGrid, ref string strError,
            ref string strAdv, ref string strStack,
            ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.Info("FindAndExpandGroupedRow", string.Format("iLevel:[{0}] lstCaptionIndexs:[{1}]", iLevel, String.Join(",", lstCaptionIndexs.ToArray())));
            if (iLevel >= lstCaptionIndexs.Count) return null;

            ReflectorForCSharp rf = new ReflectorForCSharp();
            bool isNotExists = false;
            object[] oAllInRows = ReflectorForCSharp.GetMemberByType<object[]>(lstRow, "All");
            //if (isNotExists)
            //{
            //    strError = string.Format("No member 'All' exists in Rows, wrong infragistics version?");
            //    simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow",strError);
            //    return null;
            //}

            foreach (object itmRow in oAllInRows)
            {
                if (itmRow == null) continue;

                string strBaseTypes = ReflectorForCSharp.GetObjectBaseType(itmRow.GetType());
                if (!strBaseTypes.Contains("UltraGridGroupByRow")) continue; //if (!(itmRow is UltraGridGroupByRow)) continue;
                //     UltraGridGroupByRow rowGroup = (UltraGridGroupByRow)itmRow;
                string strDesc = ReflectorForCSharp.GetMemberByType<string>(itmRow, "Description");
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(lstCaptionIndexs[iLevel], strDesc)) continue; //if (!TigerMarsUtil.RegularTest(lstCaptionIndexs[iLevel], rowGroup.Description)) continue;
                if (!rf.SetMemberValue(true, itmRow, "Expanded", ref strError, ref strStack)) //rowGroup.Expanded = true;
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow", strError = "Object property [Expanded] is NULL");// string.Format("Can't set value for 'Expanded' for a row with Error message [{0}]",strError));;
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }

                /// make the top row 
                /// 
#region translate "ActiveRowScrollRegion.FirstRow" to reflector
                //parentGrid.ActiveRowScrollRegion.FirstRow = rowGroup;
                object oActiveRowScrollRegion = ReflectorForCSharp.GetMember(parentGrid, "ActiveRowScrollRegion", ref isNotExists);
                if (isNotExists)
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow", strError = "Object property [ActiveRowScrollRegion]'s value is NULL in Grid");// "No ActiveRowScrollRegion in Grid, wrong infragistics version?");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                if (!rf.SetMemberValue(itmRow, oActiveRowScrollRegion, "FirstRow", ref strError, ref strStack))
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow", strError = string.Format("Error when get FirstRow from Grid, [{0}]", strError));
                    strAdv = "Contact Marquis";
                    return null;
                }
                #endregion //translate to reflector

                iLevel++;
                if (iLevel >= lstCaptionIndexs.Count - 1)
                {
                    isOk = true;
                    return itmRow;  //if (iLevel >= lstCaptionIndexs.Count - 1) return rowGroup;
                }

                //return FindAndExpandGroupedRow(rowGroup.Rows, iLevel, lstCaptionIndexs, parentGrid);
                object oSubRows = ReflectorForCSharp.GetMember(itmRow, "Rows", ref isNotExists);
                if (isNotExists)
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow", strError = "Object property [Rows]'value is NULL in Grid");//"No sub 'Rows' member in ROW, wrong Infragistics version?");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return null;
                }
            }
            strError = "Can't find target row";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Contact Marquis";
            return null;
        }

        private bool CheckCellLocationFormat(string strLastCaptionIdx, ref string strError,
            ref string strAdv, ref string strStack,
            ref string[] strRowPart, ref string[] strColumnPart)
        {
            simpleLog.MarsLoggerSimple.Info("CheckCellLocationFormat", string.Format("Location format to test:[{0}]", strLastCaptionIdx));

            string[] arrCaptions = strLastCaptionIdx.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrCaptions.Length != 2)
            {
                strError = "Incorrect format for grid cell location";// string.Format("Location string format should be : 'ColumnNameToLocationRow:RowCaptionIdx-ColumnNameToLocationCol:DataTypeName or DataTypeNumber'. Data is:[{0}]", strLastCaptionIdx);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct parameter use";
                return false;
            }
            int i = 0;
            foreach (string strItm in arrCaptions)
            {
                string[] arrItm = strItm.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrItm.Length != 2)
                {
                    strError = "Incorrect format for grid cell location";// string.Format("Location string format should be : 'ColumnNameToLocationRow:RowCaptionIdx-ColumnNameToLocationCol:DataTypeName or DataTypeNumber'. Data is:[{0}]", strLastCaptionIdx);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct parameter use";
                    return false;
                }
                if (i == 0)
                {
                    strRowPart = arrItm;
                    i++;
                }
                else
                {
                    strColumnPart = arrItm;
                }
            }

            return true;
        }



        internal const string cnst_old_ver_cmd_for_FillTable = @"(^ADDROWTEMPLATE|^DYNAMICROW|^ALLROWS|^[0-9]*){1};(\w+\S+.*){1,};([0-3]|Editor|Combo|DirectWrite|Checkbox)";
        internal en_fillTable_paraType checkMode(string strParaMeter)
        {
            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_old_ver_cmd_for_FillTable, strParaMeter))
            {
                if (strParaMeter.StartsWith("ALLROWS", StringComparison.OrdinalIgnoreCase))
                    return en_fillTable_paraType.allRows;
                if (strParaMeter.StartsWith("DYNAMICROW", StringComparison.OrdinalIgnoreCase))
                    return en_fillTable_paraType.dynamicRow;
                // 假定是数字模式
                return en_fillTable_paraType.rowNumber;
            }
            return en_fillTable_paraType.otherMode;
        }

        internal bool SelectTab4DockingArea(Control c, string strParameter, string strData, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("SelectTab4DockingArea", string.Format("para:[{0}] data:[{1}]", strParameter, strData));
            //size,bound
            var bounds = ReflectorForCSharp.GetMember(c, "Bounds");
            Rectangle rctScrn1, rctScrn, rct = (Rectangle)bounds;
            rctScrn = c.Parent.RectangleToScreen(rct);
            rctScrn1 = c.RectangleToScreen(rct);
            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("source:[{0}] parent:[{1}]", rctScrn1.ToString(), rctScrn));
            var vPane = ReflectorForCSharp.GetMember(c, "Pane");
            if (vPane == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = "Object property [Pane] is NULL");// string.Format("vPane is null :[{0}]", vPane == null ? "NULL" : vPane.GetType().ToString()));）； ;
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("vPane type is:[{0}]", vPane.GetType().ToString()));
            var vTabInfo = ReflectorForCSharp.GetMember(vPane, "tabInfo");
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("vTabInfo type is:[{0}]", vTabInfo == null ? "NULL" : vTabInfo.GetType().ToString()));

            var vPaneUI = ReflectorForCSharp.GetMember(vPane, "UIElement");
            if (vPaneUI == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Object property [UIElement] is null"));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
            }
            var vPaneElement = ReflectorForCSharp.GetMember(vPane, "Element");
            if (vPaneElement != null)
            {
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("vPaneElement uielemet type is :[{0}]", vPaneElement.GetType().ToString()));
                var vChildElements = ReflectorForCSharp.GetMember(vPaneElement, "ChildElements");
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("vChildElements type is:[{0}]", vChildElements == null ? "NULL" : vChildElements.GetType().ToString()));
                if (vChildElements != null)
                {
                    ArrayList lstChildElements = (ArrayList)vChildElements;
                    for (int i = 0; i < lstChildElements.Count; i++)
                    {
                        var oElement = lstChildElements[i];
                        if (oElement == null) continue;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("vChildElements-[{0}] type is:[{1}]", i, oElement.GetType().ToString()));
                    }
                }
            }

            ReflectorForCSharp reflector = new ReflectorForCSharp();
            var vPanes = ReflectorForCSharp.GetMember(vPane, "Panes");
            if ((vPanes == null) || (!(vPanes is ICollection)))
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = vPanes == null ? "Object property [Panes] is NULL" : "Object member [Panes]'s type is not ICollection");// string.Format("Panes is null or not ICollection:[{0}]", vPanes == null ? "NULL" : vPanes.GetType().ToString()))) ;
                strStack = $"{strError}\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = "Contact Marquis";
                return false;
            }
            string strTotal = "";
            foreach (var itm in (ICollection)vPanes)
            {
                if (itm == null) continue;
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("type is:[{0}]", itm.GetType()));
                var txt = ReflectorForCSharp.GetMember(itm, "Text");
                if (txt == null) continue;
                strTotal = strTotal + ";" + txt.ToString();
                if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, txt.ToString()))
                {
                    var idx = ReflectorForCSharp.GetMember(itm, "Index");
                    if ((idx == null) || (!(idx is int)))
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError = idx == null ? "Object property [Index] is NULL" : "Object member [Index]'s type is not int");// string.Format("Index is null or not ICollection:[{0}]", idx == null ? "NULL" : idx.ToString()));
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    string strTmpError = "",
                        strAdvTmp = "",
                        strStackTmp = "";
                    bool isOk = false;
                    HandleRef handleRef = new HandleRef(c, c.Handle);
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        IntPtr lpdwResult;
                        IntPtr lResult = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //handleRef,
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult);

                        isOk = reflector.SetProperty(vPane, "SelectedTabIndex", idx, ref strTmpError);
                        if (!isOk)
                        {
                            StackFrame stck = (new StackFrame());
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            strAdvTmp = "";
                        }
                        lResult = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //handleRef,
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult);
                        var SelectedTabPane = ReflectorForCSharp.GetMember(vPane, "SelectedTabPane");
                        if (SelectedTabPane != null)
                        {
                            var controlFromSelectedTabPane = ReflectorForCSharp.GetMember(SelectedTabPane, "Control");
                            if ((controlFromSelectedTabPane == null) || (!(controlFromSelectedTabPane is Control)))
                            {
                                return;
                            }

                            Control cntrlControlFromSelectedTabPane = (Control)controlFromSelectedTabPane;

                            Rectangle rctContrlForSelected = cntrlControlFromSelectedTabPane.RectangleToScreen(cntrlControlFromSelectedTabPane.Bounds);
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rctContrlForSelected.Left + rctContrlForSelected.Width / 2,
                                rctContrlForSelected.Top + rctContrlForSelected.Height / 2);
                        }
                    }));
                    Thread.Sleep(500);
                    if (isOk)
                    {
                        return true;
                    }
                    else
                    {
                        strError = strTmpError;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return false;
                    }

                }
            }

            strError = $"Can't find [{strData}] in Tab";// string.Format("Can't find [{0}] from [{1}]",strData,  strTotal);
            strStack = $"no [{strData}] in [{strTotal}] \r\nMarsErrorStacks.StackTraceDump()";
            strAdv = $"Make sure Tabpage [{strData}] is available in Screen";
            return false;
            /*
            var cUIElement = ReflectorForCSharp.GetMember(c, "ControlUIElement");
            if (cUIElement == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", string.Format("can't find ControlUIElement from :[{0}]", c == null ? "NULL" : c.GetType().ToString()));
            }
            else
            {
                var childUI = ReflectorForCSharp.GetMember(cUIElement, "ChildElements");
                if (childUI != null)
                {
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("childUI = ", childUI.GetType()));
                }
            }
            //ReflectorForCSharp.SetProperty()

            //已经确认是 WindowDockingArea : DockControlBase, IDockingArea, IDockAreaContainer            
            var vDockAreaPane = ReflectorForCSharp.GetMember(c, "DockAreaPane");
            if (vDockAreaPane == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find DockAreaPane from :[{0}]", c==null?"NULL":c.GetType().ToString()));
                return false;
            }
            reflector.SetProperty(vDockAreaPane, "SelectedTabIndex", 2, ref strError);
            var vManager = ReflectorForCSharp.GetMember(vDockAreaPane, "Manager");
            if (vManager == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find Manager from :[{0}]", vDockAreaPane == null ? "NULL" : vDockAreaPane.GetType().ToString()));
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t",string.Format("vManager type:[{0}]", vManager.GetType()) );
            var vControlPanes = ReflectorForCSharp.GetMember(vManager, "ControlPanes"); //可能不同的版本这个地方不一样
            if (vControlPanes == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find ControlPanes from :[{0}]", vControlPanes == null ? "NULL" : vControlPanes.GetType().ToString()));
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("vControlPanes type:[{0}]", vControlPanes.GetType()));

            return false;*/
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="infraGrid"></param>
        /// <param name="strParaMeter">HeaderFilter或者headerSort或者HeaderColumnSwap，如果为空，就是HeaderFilter</param>
        /// <param name="strData"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal bool clickButtonOnTable(object infraGrid, string strParaMeter, string strData, string strPegName, string strObjName, ref string strDataReturn, ref string strError, ref string strAdv, ref string strStack)
        {
            /**
             * 点击table的column， 用于处理table的filter等问题
             * */
            simpleLog.MarsLoggerSimple.logBegin("clickButtonOnTable", string.Format("objtype:[{0}], para:[{1}], data:[{2}]", infraGrid == null ? "NULL" : infraGrid.GetType().ToString(),
                strParaMeter, strData));
            if (infraGrid == null)
            {
                strError = "Passing null object to a function";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            //data 是column， parameters是位置
            if ((string.IsNullOrEmpty(strParaMeter)) && (string.IsNullOrEmpty(strData)))
            {
                strError = $"Keyword [ClickButton] does not support parameter [{strParaMeter}]";// "column information is null or empty";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Check the keyword/parameter, see user manual";
                return false;
            }

            bool isOk = false;
            //string[] arrxy = strParaMeter.Split()
            try
            {
                ArrayList lstColumns = GetColumnsFromInfraGrid(infraGrid, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                if ((!isOk) || (lstColumns == null))
                {
                    return false;
                }
                string strCaption = "",
                    strKey = "";
                int idx = -1;

                object targetColumn = GetColumnByIdx(lstColumns, strData, ref strCaption, ref strKey, ref idx, ref strError, ref strAdv, ref strStack, ref isOk);
                if ((!isOk) || (targetColumn == null))
                {
                    simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("GetColumnByIdx return false or null, with error :[{0}]", strError));
                    //StackFrame stck = (new StackFrame());
                    //strStack = MarsErrorStacks.StackTraceDump();
                    //strAdv = "dd";
                    return false;
                }
                object targetHeader = ReflectorForCSharp.GetMember(targetColumn, "Header");
                if (targetHeader == null)
                {
                    simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("GetMember-Header return false or null, wrong version of Infragistics? type:[{0}]", targetColumn.GetType()));
                    strError = $"Object property [Header] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Windows.Forms.Control cGrd = infraGrid as System.Windows.Forms.Control;
                if (cGrd == null)
                {
                    string strTyps = ReflectorForCSharp.GetObjectBaseType(infraGrid.GetType());
                    simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("[{0}] is not control", strTyps));
                    strError = $"Object [{strObjName}] is not a Control";
                    strStack = $"[{strTyps}]\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Contact Marquis";
                    return false;
                }
                Rectangle rect = default(Rectangle);
                ReflectorForCSharp reflct = new ReflectorForCSharp();
                object oUIElement = null;
                string strErrorTmp = "",
                    strAdvTmp = "",
                    strStackTmp = "";
                bool isNotExist = false;
                bool isOkTmp = false;
                if (cGrd.InvokeRequired)
                {
                    cGrd.Invoke(new Action(() =>
                    {
                        //调用UIElement
                        //reflct.GetMethod(targetHeader, "GetUIElement");
                        oUIElement = reflct.CallMethodByTypes(targetHeader, "GetUIElement", new object[] { });

                        simpleLog.MarsLoggerSimple.Info("clickButtonOnTable", string.Format("header uiElement is :[{0}]", oUIElement == null ? "null" : oUIElement.GetType().ToString()));

                        if (oUIElement == null)
                        {
                            simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strErrorTmp = "clickButtonOnTable.GetUIElement returns null, find more details from log file");
                            strErrorTmp = "Object property [UIElement] is NULL";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            strAdvTmp = "Contact Marquis";
                            isOkTmp = false;
                            return;
                        }
                        ThirdPartControlOpBase.PrintUIElementsAndItsChildInfo(oUIElement);
                        string strActionAreType = string.IsNullOrEmpty(strParaMeter) ? cnst_header_filter : strParaMeter;
                        string strTypeName = (string)(cnst_arrHeader_typeMapping.ContainsKey(strActionAreType) ? cnst_arrHeader_typeMapping[strActionAreType] : cnst_arrHeader_typeMapping[cnst_header_caption]);
                        oUIElement = ThirdPartControlOpBase.GetChildUIElementByTypeName(oUIElement, strTypeName, ref isOk, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);

                        if (!isOk) return;

                        //获得rect
                        object orct = ReflectorForCSharp.GetMember(oUIElement, "Rect", ref isNotExist);
                        if (isNotExist)
                        {
                            isOkTmp = false;
                            simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strErrorTmp = string.Format("no Rect exist in type:[{0}]", ReflectorForCSharp.GetObjectBaseType(oUIElement.GetType())));
                            strErrorTmp = "Object property [Rect] is Null";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            strAdvTmp = "Contact Marquis";
                            return;
                        }
                        if (!(orct is Rectangle))
                        {
                            isOkTmp = false;
                            simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", string.Format("Rect from UIElement is not Rectangle, it is:[{0}]", ReflectorForCSharp.GetObjectBaseType(orct.GetType())));
                            strErrorTmp = "Member [Rect]'s type in UIElement is not Rectangle.";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            strAdvTmp = "Contact Marquis";
                            return;
                        }
                        isOkTmp = true;
                        rect = (Rectangle)orct;
                    }));
                    if (!isOkTmp)
                    {
                        strError = strErrorTmp;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return false;
                    }

                }
                else
                {
                    //调用UIElement
                    reflct.GetMethod(targetHeader, "GetUIElement");
                    oUIElement = reflct.CallMethodByTypes(targetHeader, "GetUIElement", new object[] { null });
                    if (oUIElement == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = "clickButtonOnTable.GetUIElement returns null, find more details from log file");
                        strError = "Object property [UIElement] is NULl";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        isOk = false;
                        return false;
                    }
                    ThirdPartControlOpBase.PrintUIElementsAndItsChildInfo(oUIElement);

                    //获得rect
                    object orct = ReflectorForCSharp.GetMember(oUIElement, "Rect", ref isNotExist);
                    if (isNotExist)
                    {
                        isOkTmp = false;
                        simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("no Rect exist in type:[{0}]", ReflectorForCSharp.GetObjectBaseType(oUIElement.GetType())));
                        strError = "Object property [Rect] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    if (!(orct is Rectangle))
                    {
                        isOkTmp = false;
                        simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("Rect from UIElement is not Rectangle, it is:[{0}]", ReflectorForCSharp.GetObjectBaseType(orct.GetType())));
                        strError = "Object member [Rect]'s type is not Rectangle";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    isOkTmp = true;
                    rect = (Rectangle)orct;
                }
                //点击指定位置
                Point pt = cGrd.PointToScreen(rect.Location);
                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X + rect.Width - 12, pt.Y + rect.Height / 2);
                strDataReturn = "SUCESS";
                return true;
                
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("clickButtonOnTable", strError = string.Format("clickButtonOnTable,Exception:[{0}]", e.Message), e);
                strError = $"Error while operating for a control  [{strPegName}].[{strObjName}]";
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return false;
            }
        }

        internal bool SelectListItemByHeaderAsFilter(string strData, string strParaMeterSrc, object infraGrid, string strPegName, string strObjName, ref string strError,
            ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("SelectListItemByHeaderAsFilter", string.Format("data:[{0}] para:[{1}]", strData, strParaMeterSrc));
            bool isOk = false;
            try
            {
                string strParaMeter = "";
                string[] arrTmpPara = strParaMeterSrc.Split(new string[] { ";" }, StringSplitOptions.None);
                if (arrTmpPara.Length <= 1) strParaMeter = strParaMeterSrc;
                else strParaMeter = arrTmpPara[1];

                ArrayList lstColumns = GetColumnsFromInfraGrid(infraGrid, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
                if ((!isOk) || (lstColumns == null))
                {
                    return false;
                }
                string strCaption = "",
                    strKey = "";
                int idx = -1;

                Control cGrid = infraGrid as Control;
                if (cGrid == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("object is not control, it is [{0}]", infraGrid == null ? "null" : infraGrid.GetType().ToString()));
                    strError = $"Object [{strObjName}] is not a Control";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return isOk = false;
                }

                object targetColumn = GetColumnByIdx(lstColumns, strParaMeter, ref strCaption, ref strKey, ref idx, ref strError,
                    ref strAdv, ref strStack,
                    ref isOk);
                if ((!isOk) || (targetColumn == null))
                {
                    //simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("GetColumnByIdx return false or null, with error :[{0}]", strError));
                    //strError = 
                    //strStack = MarsErrorStacks.StackTraceDump();
                    //strAdv = "dd";
                    return false;
                }
                object targetHeader = ReflectorForCSharp.GetMember(targetColumn, "Header");
                if (targetHeader == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("GetMember-Header return false or null, wrong version of Infragistics? type:[{0}]", targetColumn.GetType()));
                    strError = "Object property [Header] is NULL";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return false;
                }
                #region UltraGridBase objHeadGrd = objColumn.Header.Band.Layout.Grid;
                object oBand = ReflectorForCSharp.GetMember(targetHeader, "Band");
                if (oBand == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = "Object property [Band] is NULL");// string.Format("Can't find Band member from type:[{0}]", targetHeader.GetType()));
                    strStack = $"no Band in [{targetHeader.GetType()}]\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Contact Marquis";
                    return false;
                }
                object oLayout = ReflectorForCSharp.GetMember(oBand, "Layout");
                if (oBand == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = "Object property [Layout] is NULL");// string.Format("Can't find Layout member from type:[{0}]", oBand.GetType()));
                    strStack = $"{oBand.GetType()}\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Contact Marquis";
                    return false;
                }
                object objHeadGrd = ReflectorForCSharp.GetMember(oLayout, "Grid");
                if (objHeadGrd == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = "Object property [Grid] is NULL");// string.Format("Can't find Grid member from type:[{0}]", oLayout.GetType()));/ ;
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                #endregion

                object oFilterDropDown = ReflectorForCSharp.GetMember(objHeadGrd, "FilterDropDown");
                if (oFilterDropDown == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("Can't find FilterDropDown member from type:[{0}]", objHeadGrd.GetType()));
                    strError = "Object Property [FilterDropDown] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                object oDropDown = ReflectorForCSharp.GetMember(oFilterDropDown, "DropDown");
                if (oDropDown == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("Can't find DropDown member from type:[{0}]", oFilterDropDown.GetType()));
                    strError = "Object property [DropDown] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return false;
                }

                object DropDownUI = ReflectorForCSharp.GetMember(oDropDown, "UIElement");
                ThirdPartControlOpBase.PrintUIElementsAndItsChildInfo(DropDownUI);

                object oItemMetrics = ReflectorForCSharp.GetMember(oFilterDropDown, "ItemMetrics");
                int iItmHeight = 14;
                if (oItemMetrics == null)
                {
                    //使用默认的高度，14
                }
                else
                {
                    object oHeight = ReflectorForCSharp.GetMember(oItemMetrics, "ValueListItemsSizeCache");
                    Hashtable hsItmHeight = oHeight as Hashtable;
                    if (hsItmHeight == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("Can't find ValueListItemsSizeCache (Hashtable) member from type:[{0}]", oHeight == null ? "null" : oHeight.GetType().ToString()));
                        strError = "Object Property [ValueListItemsSizeCache] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        // use default
                    }
                    else
                    {
                        object oZ = hsItmHeight[0];
                        if (!(oZ is Size))
                        {
                            simpleLog.MarsLoggerSimple.Info("SelectListItemByHeaderAsFilter", (strError = "Item of ValueListItemsSizeCache is Not Size type, default height, 14, is applied"));
                            strError = $"Object property [ValueListItemsSizeCache] is NULL";
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                        }
                        else
                        {
                            iItmHeight = ((Size)oZ).Height;
                        }
                    }
                }
                object oDropdownUI = ReflectorForCSharp.GetMember(oDropDown, "ControlUIElement");
                if (oDropdownUI == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", (strError = "Can't find ControlUIElement from Header.Band.Layout.Grid.FilterDropDown.DropDown"));
                    strError = $"Object [{strPegName}].[{strObjName}]'s property [ControlUIElement] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                object oRect = ReflectorForCSharp.GetMember(oDropdownUI, "Rect");
                if (oRect == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", (strError = string.Format("Can't find Rect from Header.Band.Layout.Grid.FilterDropDown.DropDown.ControlUIElement.type:[{0}]", oDropdownUI == null ? "null" : oDropdownUI.GetType().ToString())));
                    strError = $"Object [{strPegName}].[{strObjName}]'s property [Rect] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                Rectangle rect = default(Rectangle);
                try
                {
                    rect = (Rectangle)oRect;
                }
                catch (Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", (strError = string.Format("Rect is not Rectangle, it is type:[{0}]", oRect == null ? "null" : oRect.GetType().ToString())));
                    strError = "Object property [Rect] is NULL";
                    strStack = ex.StackTrace;
                    strAdv = "Unidentified error. If this continues, contact Marquis";
                    return isOk = false;
                }
                object oFromObjUI = ReflectorForCSharp.GetMember(oDropdownUI, "Control");
                if (oFromObjUI == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", (strError = string.Format("Can't find ControlUIElement from Header.Band.Layout.Grid.FilterDropDown.DropDown. type:[{0}]", oDropdownUI == null ? "null" : oDropdownUI.GetType().ToString())));
                    strError = "Object property [ControlUIElement] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                Control cFromObjUI = oFromObjUI as Control;
                if (cFromObjUI == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("object(cFromObjUI) is not control, it is [{0}]", infraGrid == null ? "null" : infraGrid.GetType().ToString()));
                    strError = $"Object [{strObjName}] is not a Control";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                Rectangle rectTarget = cFromObjUI.RectangleToScreen(rect);
                Point pt = cFromObjUI.PointToScreen(rect.Location);
                int iPosY = pt.Y + 5;
                pt.X += 5;
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SetCursorPos(pt.X + rectTarget.Width / 2, iPosY);
                Thread.Sleep(200);

                object oValueListItems = ReflectorForCSharp.GetMember(oFilterDropDown, "ValueListItems");
                IList valueListItems = oValueListItems as IList;
                if ((oValueListItems == null) || (valueListItems == null))
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("No ValueListItems exists from oFilterDropDown or It is not IList, type is:[{0}]",
                        oValueListItems == null ? "null" : oValueListItems.GetType().ToString()));
                    strError = oValueListItems == null ? "Object property [ValueListItems] is NULL" : "Object member [ValueListItems]'s type is not IList";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return isOk = false;
                }
                string strTxt = "";
                bool isOutOfRange = false;
                ReflectorForCSharp objReflector = new ReflectorForCSharp();
                for (int i = 0; i < valueListItems.Count; i++)
                {
                    object itm = valueListItems[i];

                    object oDisplayText = ReflectorForCSharp.GetMember(itm, "DisplayText");
                    string DisplayText = oDisplayText == null ? "" : oDisplayText.ToString();
                    strTxt += (";" + DisplayText);
                    if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, DisplayText))
                        || (string.Compare(strData, DisplayText, true) == 0))
                    {
                        if (!isOutOfRange)
                        {
                            Thread.Sleep(200);
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X + rect.Width / 2, iPosY);
                            return isOk = true;
                        }
                        else
                        {
                            try
                            {
                                Thread.Sleep(20);
                                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SetCursorPos(pt.X + rect.Width / 2, iPosY);
                                Thread.Sleep(100);
                                objReflector.CallMethod(oDropDown, "SelectItemByMouse", new object[] { itm });
                                return isOk = true;
                            }
                            catch (Exception ex)
                            {
                                simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter",
                                    strError = string.Format("Exception when call SelectItemByMouse with error:[{0}]", ex.Message), ex);
                                strError = $"Error while operating for a control  [{strPegName}].[{strObjName}]";
                                strStack = ex.StackTrace;
                                strAdv = "Unidentified error. If this continues, contact Marquis";
                                return isOk = false;
                            }
                        }
                    }
                    if ((iPosY + iItmHeight - pt.Y) < rect.Height) iPosY += iItmHeight;
                    else isOutOfRange = true;
                }
                simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("Can't find [{0}] from [{1}]", strData, strTxt));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Make sure the Tab [{strData}] is Available in Screen";
                return isOk = false;
            }
            catch (Exception ez)
            {
                simpleLog.MarsLoggerSimple.Error("SelectListItemByHeaderAsFilter", strError = string.Format("SelectListItemByHeaderAsFilter Exception:[{0}]", ez.Message), ez);
                strStack = ez.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return isOk = false;
            }
        }

        internal bool LocatedRowByRowHeader(System.Windows.Forms.Control targetGrid, string strRowCaptionIdx, int headerCol, string strParameter, ref object targetRow, ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("\tLocatedRowByRowHeader", $"rowCaptionIdx:[{strRowCaptionIdx}], headerCol:[{headerCol}], parameter:[{strParameter}]");
            bool isOk = false;
            int iCount = -1;
            object[] arrRows = GetRowsFromGridControl(targetGrid, ref isOk, ref iCount, ref strError, ref strAdv, ref strStack);
            try
            {
                if ((!isOk) || (arrRows == null)) return false;

                ReflectorForCSharp reflector = new ReflectorForCSharp();
                bool isNotExists = true;
                string strtmpTxt4HeaderCaption = "";

                foreach (object itmRow in arrRows)
                {
                    if (itmRow == null) continue;

                    string strBaseTypes = ReflectorForCSharp.GetObjectBaseType(itmRow.GetType());
                    if (!strBaseTypes.Contains("UltraGridGroupByRow"))
                    {
                        object headerCell = GetCellFromOneRow(itmRow, headerCol, ref isOk, ref strError, ref strAdv, ref strStack);
                        if ((!isOk) || (headerCell == null))
                            continue; //here, expand should be applied for all tables
                        string strHeadCellText = reflector.GetMember<string>(headerCell, "Text", ref isNotExists);
                        strtmpTxt4HeaderCaption = string.Format("{0};[{1}]", strtmpTxt4HeaderCaption, strHeadCellText);
                        if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strRowCaptionIdx, strHeadCellText))
                        {
                            targetRow = itmRow;
                            return true;
                        }
                        continue;
                    }
                    else
                    {
                        // that is a grouped row, should expands and then check
                        if (!reflector.SetMemberValue(true, itmRow, "Expanded", ref strError, ref strStack)) //rowGroup.Expanded = true;
                        {
                            isOk = false;
                            simpleLog.MarsLoggerSimple.Error("LocatedRowByRowHeader", strError = string.Format("Can't set value for 'Expanded' for a row with Error message [{0}]", strError));
                            strError = "Object property [Expanded] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        Thread.Sleep(100);
                        object oSubRows = ReflectorForCSharp.GetMember(itmRow, "Rows", ref isNotExists);
                        if (isNotExists)
                        {
                            isOk = false;
                            simpleLog.MarsLoggerSimple.Error("FindAndExpandGroupedRow", strError = "Object property [Rows]'s value is NULL");//"No sub 'Rows' member in ROW, wrong Infragistics version?");                            
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        //获得所有的row                    
                        object[] oAllInRows = ReflectorForCSharp.GetMemberByType<object[]>(oSubRows, "All");
                        int iSubRowCnt = ReflectorForCSharp.GetMemberByType<int>(oSubRows, "Count");
                        foreach (object itmSubRow in oAllInRows)
                        {
                            if (itmSubRow == null) continue;
                            object headerCell = GetCellFromOneRow(itmSubRow, headerCol, ref isOk, ref strError, ref strAdv, ref strStack);
                            if ((!isOk) || (headerCell == null))
                                continue; //here, expand should be applied for all tables
                            string strHeadCellText = reflector.GetMember<string>(headerCell, "Text", ref isNotExists);
                            strtmpTxt4HeaderCaption = $"{strHeadCellText};{strtmpTxt4HeaderCaption}";
                            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strRowCaptionIdx, strHeadCellText))
                            {
                                isOk = true;
                                targetRow = itmSubRow;
                                return true;
                            }
                        }
                    }
                }
                strError = string.Format("Can't find [{0}] from [{1}]", strRowCaptionIdx, strtmpTxt4HeaderCaption);
                simpleLog.MarsLoggerSimple.Info("\tLocatedRowByRowHeader", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure column name exists in the grid";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("\tLocatedRowByRowHeader");
            }
        }
    }

    /// <summary>
    /// find column for Record and replay
    /// </summary>
    public class MarsUltraGridColumnFinder
    {
        /// <summary>
        /// 依据鼠标位置找到cell的相关信息
        /// </summary>
        /// <param name="ultraGrid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string FindColumnName(object ultraGrid, int x, int y)
        {
            // Validate input
            if (ultraGrid == null) throw new ArgumentNullException(nameof(ultraGrid));

            // Use reflection to get the Columns collection
            Type gridType = ultraGrid.GetType();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            // Locate the 'DisplayLayout' property
            PropertyInfo displayLayoutProperty = gridType.GetProperty("DisplayLayout", flags);
            if (displayLayoutProperty == null) throw new InvalidOperationException("DisplayLayout property not found.");

            object displayLayout = displayLayoutProperty.GetValue(ultraGrid);
            if (displayLayout == null) throw new InvalidOperationException("DisplayLayout is null.");

            // Locate the 'Bands' property in DisplayLayout
            PropertyInfo bandsProperty = displayLayout.GetType().GetProperty("Bands", flags);
            if (bandsProperty == null) throw new InvalidOperationException("Bands property not found.");

            object bands = bandsProperty.GetValue(displayLayout);
            if (bands == null) throw new InvalidOperationException("Bands is null.");
            // Assume single band for simplicity; access the first band
            
            foreach (var band in (System.Collections.IEnumerable)bands)
            {
                // Now access the 'Columns' property within the band
                PropertyInfo columnsProperty = band.GetType().GetProperty("Columns", flags);
                if (columnsProperty == null) throw new InvalidOperationException("Columns property not found.");

                object columns = columnsProperty.GetValue(band);
                if (columns == null) throw new InvalidOperationException("Columns is null.");

                // Iterate through columns to find the one matching the point
                foreach (var column in (System.Collections.IEnumerable)columns)
                {
                    // Reflect to access column bounds or location properties
                    PropertyInfo headerProperty = column.GetType().GetProperty("Header", flags);
                    if (headerProperty == null) continue;

                    object header = headerProperty.GetValue(column);
                    if (header == null) continue;

                    // Find location (or bounds) of the column
                    PropertyInfo locationProperty = header.GetType().GetProperty("Bounds", flags); // Or Location
                    if (locationProperty == null) continue;

                    var bounds = locationProperty.GetValue(header);
                    if (bounds != null && IsPointWithinBounds(bounds, x, y))
                    {
                        // Return the column name
                        PropertyInfo keyProperty = column.GetType().GetProperty("Key", flags);
                        return keyProperty?.GetValue(column)?.ToString();
                    }
                }

                // Break after processing the first band (if needed)
                break;
            }

            return null; // No match found
        }


        private static bool IsPointWithinBounds(object bounds, int x, int y)
        {
            // Assume bounds has X, Y, Width, and Height properties; adapt as necessary
            PropertyInfo xProperty = bounds.GetType().GetProperty("X");
            PropertyInfo yProperty = bounds.GetType().GetProperty("Y");
            PropertyInfo widthProperty = bounds.GetType().GetProperty("Width");
            PropertyInfo heightProperty = bounds.GetType().GetProperty("Height");

            int bx = (int)(xProperty?.GetValue(bounds) ?? 0);
            int by = (int)(yProperty?.GetValue(bounds) ?? 0);
            int bwidth = (int)(widthProperty?.GetValue(bounds) ?? 0);
            int bheight = (int)(heightProperty?.GetValue(bounds) ?? 0);

            return x >= bx && x <= bx + bwidth && y >= by && y <= by + bheight;
        }

        private static string GetCellColumnNameFromCell(object cell, ref bool isOk, ref string strError)
        {
            PropertyInfo columnProperty = cell.GetType()
                .GetProperty("Column", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo valueProperty = cell.GetType()
                .GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (columnProperty == null || valueProperty == null)
            {
                isOk = false;
                MarsLoggerSimple.Info("GetCellColumnNameFromCell", strError = "Could not retrieve column or value information.");
                return null;
            }

            object column = columnProperty.GetValue(cell);
            object value = valueProperty.GetValue(cell);

            // Get column name
            PropertyInfo columnNameProperty = column.GetType()
                .GetProperty("Key", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            string columnName = columnNameProperty?.GetValue(column)?.ToString();
            MarsLoggerSimple.Info("GetCellColumnNameFromCell", $"Column: {columnName}, Value: {value}");
            return columnName;
        }
        /// <summary>
        /// 获得当前table中的信息
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="point"></param>
        /// <param name="columnName"></param>
        /// <param name="textOfCell"></param>
        /// <param name="rowId">鼠标点击的cell或者activecell的row number</param>
        /// <param name="strError"></param>
        /// <param name="isToGetActiveCell">是否只获取active的cell</param>
        /// <returns></returns>
        public static bool GetCellInfoAtPoint(object grid, Point point, ref string columnName, ref string textOfCell,
            ref int rowId,
            ref string strError,
            bool isToGetActiveCell = false)
        {
            bool isOk = true;
            string cellText = "";
            try
            {
                // Reflect on the grid to find rows
                PropertyInfo rowsProperty = grid.GetType()
                    .GetProperty("Rows", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (rowsProperty == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", strError = "Could not find the 'Rows' property.");
                    return false;
                }
                
                if (isToGetActiveCell)
                {
                    // 获取 ActiveCell 属性
                    var activeCell = ReflectorForCSharp.GetPropertyValue<object>(grid, "ActiveCell", ref strError, ref isOk,  null);
                    if ((!isOk)||(activeCell == null))
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"ActiveCell,IsToGetActiveCell|{isToGetActiveCell}|Error|{strError}");
                        return false;
                    }
                    /// get row of active cell
                    /// 
                    var rowFromActiveCell = ReflectorForCSharp.GetPropertyValue<object>(activeCell, "Row", ref strError, ref isOk, null);
                    if ((!isOk) || (rowFromActiveCell == null))
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"Row,IsToGetActiveCell|{isToGetActiveCell}|Error|{strError}");
                        return false;
                    }
                    rowId = ReflectorForCSharp.GetPropertyValue<int>(rowFromActiveCell, "Index", ref strError, ref isOk, -1);
                    if ((!isOk) || (rowId == -1))
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"Index,IsToGetActiveCell|{isToGetActiveCell}|Error|{strError}");
                        return false;
                    }
                    textOfCell = ReflectorForCSharp.GetPropertyValue<string>(rowFromActiveCell, "Text", ref strError, ref isOk, string.Empty);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"Text,IsToGetActiveCell|{isToGetActiveCell}|Error|{strError}");
                        return false;
                    }
                    columnName = GetCellColumnNameFromCell(activeCell, ref isOk, ref strError);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"columnName,IsToGetActiveCell|{isToGetActiveCell}|Error|{strError}");
                        return false;
                    }
                    return true;
                }

                Point localPoint = ((System.Windows.Forms.Control)grid).PointToClient(point);
                var rowsCollection = rowsProperty.GetValue(grid);

                // If rows exist, iterate over them to find the relevant cell
                if (rowsCollection == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", strError = "No rows available in the grid.");
                    return false;
                }
                
                // Reflect to get methods or properties to identify elements at a position
                foreach (var row in (System.Collections.IEnumerable)rowsCollection)
                {
                    // Use reflection to access the cells of the row
                    PropertyInfo cellsProperty = row.GetType()
                        .GetProperty("Cells", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (cellsProperty == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", "Could not find the 'Cells' property for a row.");
                        continue;
                    }

                    var cellsCollection = cellsProperty.GetValue(row);
                    foreach (var cell in (System.Collections.IEnumerable)cellsCollection)
                    {
                        //// 判断单元格是否可见
                        //PropertyInfo isVisibleProperty = cell.GetType().GetProperty("IsVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        //if (isVisibleProperty == null || !(bool)isVisibleProperty.GetValue(cell)) continue;

                        // 获取单元格文本
                        PropertyInfo textProperty = cell.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        cellText = textProperty?.GetValue(cell)?.ToString() ?? string.Empty;

                        // 获取cell中的row信息
                        PropertyInfo rowProperty = cell.GetType().GetProperty("Row", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        var cell_row = rowProperty?.GetValue(cell);
                        PropertyInfo rowIndexProperty = cell_row?.GetType().GetProperty("Index", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        rowId = (int)(rowIndexProperty?.GetValue(cell_row)??-1);

                        MethodInfo getUIElementMethod = cell.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m=>m.Name== "GetUIElement" & m.GetParameters().Length==0);
                        if (getUIElementMethod == null) continue;

                        object uiObject = getUIElementMethod.Invoke(cell, new object[] { });

                        // Reflect to find position comparison logic
                        //MethodInfo isCellAtPointMethod = cell.GetType()
                        //    .GetMethod("IsAtPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        simpleLog.MarsLoggerSimple.Info("GetCellInfoAtPoint", $"find cell text|{cellText}");
                        if (getUIElementMethod != null)
                        {
                            // 调用 GetUIElement 方法，获取 UIElement
                            object uiElement = getUIElementMethod.Invoke(cell, null);

                            if (uiElement == null)
                            {

                                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", "无法获取 UIElement。");
                                continue;
                            }

                            // 获取 UIElement 的 ClipRect 属性
                            PropertyInfo clipRectProperty = uiElement.GetType()
                                .GetProperty("ClipRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            if (clipRectProperty == null)
                            {
                                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", "无法找到 'ClipRect' 属性。");
                                continue;
                            }

                            var clipRect = clipRectProperty.GetValue(uiElement);
                            Rectangle rect = default(Rectangle); 
                            if (clipRect is Rectangle)
                            {
                                rect = (Rectangle)clipRect;
                            }
                            else
                            {
                                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", "ClipRect is not Rectangle");
                                continue;
                            }
                            // 检查点是否在 ClipRect 中
                            if ((rect==null) || (!rect.Contains(localPoint)))
                            {
                                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", $"找到非目标单元格，ClipRect: {rect}|{localPoint}|{cellText}");                                
                                continue;
                            }

                            PropertyInfo cell_rect = uiElement.GetType()
                                .GetProperty("Rect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (cell_rect == null)
                            {
                                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", "无法找到 'cell_rect' 属性。");
                                continue;
                            }

                            var cell_rect_value = cell_rect.GetValue(uiElement);
                            // 检查点是否在 ClipRect 中
                            if (cell_rect_value != null)
                            {
                                simpleLog.MarsLoggerSimple.Info("GetCellInfoAtPoint",$"GET rect|{cell_rect_value}|{localPoint}");
                            }
                            if (cell_rect_value is Rectangle rect_cell && rect_cell.Contains(localPoint))
                            {
                                simpleLog.MarsLoggerSimple.Info("GetCellInfoAtPoint", $"找到单元格，ClipRect: {rect_cell}");
                                textOfCell = cellText;
                                /// 获得column name
                                /// 
                                
                                columnName = GetCellColumnNameFromCell(cell, ref isOk, ref strError);
                                if (!isOk)
                                {
                                    simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", strError=$"can't find column name due to |{strError}");
                                    return false;
                                }
                                return isOk;
                            }
                        }
                    }
                }
                isOk = false;
                strError = $"Please ensure that mouse clicks at validated cell.";
                return false;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetCellInfoAtPoint", strError = $"An error occurred: {ex.Message}");
                return false;
            }
        }
    }

    


    public class InfragisticsToolFinder
    {
        
    }
}
