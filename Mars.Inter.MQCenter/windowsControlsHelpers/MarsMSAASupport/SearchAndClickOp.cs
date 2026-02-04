using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.Inter.MQCenter.MSAASupport;
using Accessibility;
using Mars.message.windowsWrapper.SystemUtil;
using System.Windows;
using System.Drawing;
//using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.AutoTestingDriver.ExecuteTestcase.MarsMSAASupport
{
    public class SearchAndClickOp
    {
        //private static MLogger MarsLoggerSimple = MLogger.GetLogger(typeof(SearchAndClickOp));

        /// <summary>
        /// 从handle处理Grid对象
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="strObjMarsType"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        public static bool ParseAndExecuteActionFromHandle(int hwnd, string strObjMarsType, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("ParseAndExecuteActionFromHandle", $"{iMark}|{strParaMeter}|{strData}|{strObjMarsType}");
            try
            {
                if (hwnd == 0)
                {
                    strError = "No validate handle for datagrid";
                    MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                var provider = new MarsAutoAccessibleSupportProvider();
                bool isOk = provider.CreateAccessibleObject(new IntPtr(hwnd), ref strError);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                return ParseAndExecuteAction(provider.AccessibleObject, strObjMarsType, strParaMeter, strData, ref strError, ref dealResult);
            }
            catch(Exception e)
            {
                strError = e.Message;
                MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{iMark}|{strError}|{Environment.StackTrace}", e);
                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ParseAndExecuteActionFromHandle", $"{iMark}|");
            }
        }

        /// <summary>
        /// Parses action parameter and executes the specified action
        /// </summary>
        /// <param name="targetObject">Target MSAA object</param>
        /// <param name="strParaMeter">Action parameter string</param>
        /// <param name="strData">Data parameter</param>
        /// <param name="strError">Error message reference</param>
        /// <param name="dealResult">Deal result reference</param>
        /// <returns>True if action executed successfully, false otherwise</returns>
        public static bool ParseAndExecuteAction(dynamic targetObject, string strObjMarsType, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("SearchAndClickOp.ParseAndExecuteAction", $"{iMark}|Parsing action: {strParaMeter}, ObjectType: {strObjMarsType}, Data: {strData}");

            try
            {
                // 1. 检查对象类型必须为swfTable或者winTable
                if (string.IsNullOrEmpty(strObjMarsType) || 
                    (!strObjMarsType.Equals("swfTable", StringComparison.OrdinalIgnoreCase) && 
                     !strObjMarsType.Equals("winTable", StringComparison.OrdinalIgnoreCase)))
                {
                    strError = $"Object type must be 'swfTable' or 'winTable', but got: {strObjMarsType}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                // 2. 解析参数格式：MarsAddins;Deal No.*;Action:LEFT_DBL_CLICK
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strError = "Parameter string cannot be empty";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                // 检查MarsAddins前缀
                if (!strParaMeter.StartsWith("MarsAddins;", StringComparison.OrdinalIgnoreCase))
                {
                    strError = $"Parameter must start with 'MarsAddins;', but got: {strParaMeter}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                // 解析参数：MarsAddins;Deal No.*;Action:LEFT_DBL_CLICK
                string[] parts = strParaMeter.Split(';');
                if (parts.Length < 3)
                {
                    strError = $"Invalid parameter format. Expected: MarsAddins;ColumnHeader;Action:ActionType, but got: {strParaMeter}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                string columnHeader = parts[1].Trim();
                string actionPart = parts[2].Trim();

                // 解析Action部分
                if (!actionPart.StartsWith("Action:", StringComparison.OrdinalIgnoreCase))
                {
                    strError = $"Action part must start with 'Action:', but got: {actionPart}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                string actionType = actionPart.Substring(7).Trim(); // Remove "Action:" prefix
                MarsLoggerSimple.Info("ParseAndExecuteAction", $"Parsed - ColumnHeader: {columnHeader}, ActionType: {actionType}, Data: {strData}");

                // 3. 获得IAccessible对象的role和roleName，逐级遍历子对象直到出现rolename为row的
                IAccessible accessible = targetObject as IAccessible;
                if (accessible == null)
                {
                    strError = "Target object is not an MARS Standard control manged interface object";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }
                List<IAccessible> lstChildObjs = new List<IAccessible>();
                bool isOk = FindDataGridChildren(accessible, lstChildObjs, ref strError);
                if (!isOk || lstChildObjs.Count == 0)
                {
                    strError = $"Failed to find DataGrid children: {strError}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }
                if (lstChildObjs.Count > 1)
                {
                    MarsLoggerSimple.Warning("ParseAndExecuteAction", strError = $"Multiple DataGrid objects found, Please enhance object identifiers");
                    return false;
                }


                dynamic tableObject = null;
                if (!FindTableWithRows(targetObject, ref tableObject, ref strError))
                {
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|Failed to find table with rows: {strError}");
                    return false;
                }

                MarsLoggerSimple.Info("ParseAndExecuteAction", "Successfully found table object with rows");

                /// 因为tableObject有row，需要从中获得所有的类型为row的对象。其中第一条是列头。从列头中获得列头名称idx
                /// 然后匹配strData，找到cell和rectangle。如果cell不在可视区域，则需要滚动到可视区域另外，cell可能高度为
                /// 0，等，需要获得row的rectangle，然后执行action
                /// 所以，3.1 获得tableObject的所有row对象
                /// 3.2 获得第一列
                /// 3.3 匹配列头和字段，找到索引
                /// 3.4 逐行扫描，找到匹配的cell（如果有多个，报错）
                /// 3.5 获得cell的rectangle
                /// 
                // 3.1 获得tableObject的所有row对象
                List<IAccessible> allRows = GetAllRowsFromTableObjects(tableObject, ref strError, ref isOk);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark} |Failed to find table with rows: {strError}");
                    return false;
                }
                if (allRows.Count <= 1)
                {
                    // 因为第一行是列头 
                    strError = "No rows found in the table object";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }
                /// 3.2/3.3 获得列头，找到匹配的列头
                /// 
                List<string> lstHeader = GetColumnHeaders(allRows[0], ref isOk, ref strError);
                if (!isOk)
                {
                    strError = $"can't get columns' header info, check log for more detail:{strError}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }
                int iColIdx = -1;
                List<int> matchedColumnIndx = new List<int>();
                for(int i = 0; i < lstHeader.Count; i++)
                {
                    if (string.IsNullOrEmpty(lstHeader[i])) continue;
                    if (MarsWindowsAPIsExtend.RegularTest(columnHeader, lstHeader[i]))
                    {
                        matchedColumnIndx.Add(i);
                    }
                }
                if (matchedColumnIndx.Count == 0)
                {
                    strError = $"Can't find matching column header for pattern '{columnHeader}'\r\n{string.Join(",",matchedColumnIndx)}";
                    isOk = false;
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }
                if (matchedColumnIndx.Count > 1)
                {
                    strError = $"Multiple matching column headers found for pattern '{columnHeader}', Please enhance column header pattern\r\n{string.Join(",", matchedColumnIndx)}";
                    isOk = false;
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                /// 3.4 逐行扫描，找到匹配的cell（如果有多个，报错）
                /// 
                List<Rectangle> targetRects = MatchCellValuesByCellId(allRows, matchedColumnIndx[0], strData, ref strError, ref isOk);
                if ((!isOk)||(targetRects==null)||(targetRects.Count<=0))
                {
                    strError = $"Failed to match cell values by column index {matchedColumnIndx[0]}: {strError}";
                    MarsLoggerSimple.Error("ParseAndExecuteAction",$"{iMark}|{strError}" );
                    return false;
                }
                Rectangle rect = targetRects[0];
                /// 判断是否合理的
                /// 
                bool isRectVisible = rect.Width > 0 && rect.Height > 0;
                if (!isRectVisible)
                {
                    strError = "Target cell's height or width is 0.";
                    MarsLoggerSimple.Error("ParseAndExecuteAction", $"{iMark}|{strError}");
                    return false;
                }

                // 4. 执行相应的操作
                bool result = ExecuteTableAction(rect, actionType, strParaMeter,  ref strError);

                if (result)
                {
                    // Set result
                    dealResult = new MARSDealResult
                    {
                        AckTime = DateTime.Now,
                        ResultMessage = "OK",
                        ActualInputData = strData
                    };
                    
                    MarsLoggerSimple.Info("ParseAndExecuteAction", "Action executed successfully");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                strError = $"Failed to parse and execute action: {ex.Message}";
                MarsLoggerSimple.Error("ParseAndExecuteAction", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ParseAndExecuteAction");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRows"></param>
        /// <param name="v"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        private static List<Rectangle> MatchCellValuesByCellId(List<IAccessible> allRows, int cellIdx, string strData, ref string strError, ref bool isOk)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MatchCellValuesByCellId", $"{iMark}|cellIdx|{cellIdx}|pattern|{strData}");
            List<Rectangle> matchedRects = new List<Rectangle>();
            try
            {
                if (allRows == null || allRows.Count <= 1)
                {
                    strError = "allRows is null or empty";
                    MarsLoggerSimple.Error("MatchCellValuesByCellId", $"{iMark}|{strError}");
                    isOk = false;
                    return matchedRects;
                }

                if (cellIdx < 0)
                {
                    strError = $"cellIdx {cellIdx} is invalid";
                    MarsLoggerSimple.Error("MatchCellValuesByCellId", $"{iMark}|{strError}");
                    isOk = false;
                    return matchedRects;
                }

                // Skip header row at index 0
                for (int r = 1; r < allRows.Count; r++)
                {
                    IAccessible row = allRows[r];
                    if (row == null) continue;

                    int childCount = 0;
                    try
                    {
                        childCount = row.accChildCount;
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} get accChildCount failed|{ex.Message}");
                        continue;
                    }

                    if (childCount <= 0 || cellIdx < 0)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} has no children or invalid cellIdx {cellIdx}|childCount|{childCount}");
                        continue;
                    }
                    // if row's children count less than target index, skip
                    if (childCount <= cellIdx)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} has insufficient children for cellIdx {cellIdx}|childCount|{childCount}");
                        continue;
                    }                        

                    object[] children = new object[childCount];
                    int obtained = MARSAccessibleProvider.AccessibleChildren(row, 0, childCount, children, out int nObtained);
                    if (obtained != 0 || nObtained <= 0)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} failed to get children|ret|{obtained}|nObtained|{nObtained}");
                        continue;
                    }

                    if (cellIdx >= nObtained)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} has insufficient obtained children for cellIdx {cellIdx}|nObtained|{nObtained}");
                        continue;
                    }

                    string cellValue = "", cellName="";
                    Rectangle rect = Rectangle.Empty;

                    try
                    {
                        object childObj = children[cellIdx];
                        if (childObj is int childIndex)
                        {
                            try
                            {
                                cellName = row.get_accName(childIndex);
                                cellValue = row.get_accValue(childIndex);
                                row.accLocation(out int left, out int top, out int width, out int height, childIndex);
                                rect = new Rectangle(left, top, width, height);
                                // Prefer getting the IAccessible for more reliable location and value                                 
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} childIndex {childIndex} handling failed|{ex.Message}");
                                continue;
                            }
                        }
                        else if (childObj is IAccessible cellAcc2)
                        {
                            try { 
                                cellValue = cellAcc2.get_accValue(0) ?? "";
                                cellName = cellAcc2.get_accName(0) ?? "";
                                int l, t, w, h;
                                cellAcc2.accLocation(out l, out t, out w, out h, 0);
                                rect = new Rectangle(l, t, w, h);
                                matchedRects.Add(rect);
                            }
                            catch (Exception e)
                            {
                                MarsLoggerSimple.Error("MatchCellValuesByCellId", $"{iMark}|row {r} cellAcc2 handling failed|{e.Message}", e);
                                rect = Rectangle.Empty; 
                            }
                        }
                        else
                        {
                            MarsLoggerSimple.Error("MatchCellValuesByCellId", $"{iMark}|row {r} childObj is neither int nor IAccessible");
                            // Fallback: attempt to get value via row interface using the index if we can infer it
                            try { cellValue = ""; } catch { cellValue = ""; }
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|row {r} process cell failed|{ex.Message}");
                        continue;
                    }

                    bool isMatch = false;
                    try
                    {
                        isMatch = MarsWindowsAPIsExtend.RegularTest(strData ?? string.Empty, cellValue ?? string.Empty);
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("MatchCellValuesByCellId", $"{iMark}|RegularTest error|{ex.Message}");
                        isMatch = false;
                    }

                    MarsLoggerSimple.Info("MatchCellValuesByCellId", $"{iMark}|row {r}|cellIdx {cellIdx}|cellValue|{cellValue}|match|{isMatch}|rect|{rect.X},{rect.Y},{rect.Width},{rect.Height}");

                    if (isMatch)
                    {
                        if (!rect.IsEmpty)
                            matchedRects.Add(rect);
                        else
                        {
                            // Try to use row location as fallback if cell rect is empty
                            try
                            {
                                int l, t, w, h;
                                row.accLocation(out l, out t, out w, out h, 0);
                                matchedRects.Add(new Rectangle(l, t, w, h));
                            }
                            catch(Exception e) {
                                MarsLoggerSimple.Error("MatchCellValuesByCellId", e.Message, e);
                            }
                        }
                    }
                }

                if (matchedRects.Count == 0)
                {
                    isOk = true; // no matches is not necessarily an error here; caller decides
                    return matchedRects;
                }

                if (matchedRects.Count > 1)
                {
                    isOk = false;
                    strError = $"Multiple matches found for pattern '{strData}' at column index {cellIdx}: {matchedRects.Count}";
                    return matchedRects;
                }

                isOk = true;
                return matchedRects;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = $"Exception in MatchCellValuesByCellId: {e.Message}";
                MarsLoggerSimple.Error("MatchCellValuesByCellId", $"{iMark}|{strError}", e);
                return matchedRects;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MatchCellValuesByCellId", $"{iMark}|count|{matchedRects.Count}|isOk|{isOk}|err|{strError}");
            }
        }

        private static List<string> GetColumnHeaders(IAccessible headerRow, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetColumnHeaders", $"{iMark}");
            try
            {
                if (headerRow == null)
                {
                    strError = "Header row is null";
                    MarsLoggerSimple.Error("GetColumnHeaders", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                string rowName = headerRow.get_accName(0);
                MarsLoggerSimple.Info("GetColumnHeaders", $"the row name is|{rowName}");
                int iCellCnt = headerRow.accChildCount;
                if (iCellCnt <= 0)
                {
                    strError = "Header row has no children";
                    MarsLoggerSimple.Error("GetColumnHeaders", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                object[] children = new object[iCellCnt];
                int obtained = MARSAccessibleProvider.AccessibleChildren(headerRow, 0, iCellCnt, children, out int nObtained);
                if (obtained != 0 || nObtained <= 0)
                {
                    strError = $"Failed to get child objects from header row. Result: {obtained}, Obtained: {nObtained}";
                    MarsLoggerSimple.Error("GetColumnHeaders", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                MarsLoggerSimple.Info("GetColumnHeaders", $"{iMark}|Found {nObtained} children in header row");
                List<string> lstHeaders = new List<string>();
                
                for (int i = 0; i < nObtained; i++)
                {
                    object childObj = children[i];
                    if (childObj == null)
                        continue;
                    IAccessible childAcc = null;
                    string strName = "", strValue = "";
                    if (childObj is int childIndex)
                    {
                        // Get child by index
                        object indexedChild = headerRow.accChild[childIndex];
                        if (indexedChild is IAccessible tmpCell)
                        {
                            /// 假定没有子对象，但是log子对象
                            /// 
                            strName = tmpCell.get_accName(0);
                            strValue = tmpCell.get_accValue(0);
                            int iCellChildCnt = tmpCell.accChildCount;
                            MarsLoggerSimple.Info("GetColumnHeaders", $"cell info|Name|{strName}|value|{strValue}|childCount|{iCellChildCnt}");
                            lstHeaders.Add(strValue);
                        }
                        else
                        {
                            MarsLoggerSimple.Warning("GetColumnHeaders", $"{iMark}|Child {i} indexed by {childIndex} is not IAccessible, taken name or value as column");
                            strName = headerRow.get_accName(childIndex);
                            strValue = headerRow.get_accValue(childIndex);
                            MarsLoggerSimple.Info("GetColumnHeaders", $"get columnsHeaders|{strName}|{strValue}");
                            lstHeaders.Add(strValue);
                        }
                    }
                    else
                    {
                        childAcc = childObj as IAccessible;
                        strName = childAcc.get_accName(0);
                        strValue = childAcc.get_accValue(0);
                        int iCellChildCnt = childAcc.accChildCount;
                        MarsLoggerSimple.Info("GetColumnHeaders", $"cell info-directly cell MARSMSAA|Name|{strName}|value|{strValue}|childCount|{iCellChildCnt}");
                        lstHeaders.Add(strValue);
                    }
                }
                isOk = true;
                return lstHeaders;
            }
            catch(Exception e)
            {
                strError = $"Exception in GetColumnHeaders: {e.Message}";
                isOk = false;
                MarsLoggerSimple.Error("GetColumnHeaders", $"{iMark}|Exception|{e.Message}", e);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("GetColumnHeaders", $"{iMark}|returns|{isOk}|{strError}");
            }
        }

        /// <summary>
        /// 从tableObject中获得所有的row对象，tableObject是IAccessible对象
        /// </summary>
        /// <param name="tableObject"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<IAccessible> GetAllRowsFromTableObjects(dynamic tableObject, ref string strError, ref bool isOk)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetAllRowsFromTableObjects", $"{iMark}");
            try
            {
                List<IAccessible> lstRows = new List<IAccessible>();
                IAccessible tblAcc = tableObject as IAccessible;
                if (tblAcc==null)
                {
                    strError = "Table object is not an IAccessible object";
                    MarsLoggerSimple.Error("GetAllRowsFromTableObjects", $"{iMark}|{strError}");
                    isOk = false;
                    return lstRows;
                }
                /// 获得tableObject的所有子对象
                /// 
                int childCount = tblAcc.accChildCount;
                if (childCount <= 0)
                {
                    strError = "Table object has no children";
                    MarsLoggerSimple.Error("GetAllRowsFromTableObjects", $"{iMark}|{strError}");
                    isOk = false;
                    return lstRows;
                }
                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(tblAcc, 0, childCount, children, out int nObtained);
                if (obtained != 0 || nObtained <= 0)
                {
                    strError = $"Failed to get child objects from table. Result: {obtained}, Obtained: {nObtained}";
                    MarsLoggerSimple.Error("GetAllRowsFromTableObjects", $"{iMark}|{strError}");
                    isOk = false;
                    return lstRows;
                }
                MarsLoggerSimple.Info("GetAllRowsFromTableObjects", $"{iMark}|Found {nObtained} children in table object");
                int roleId = -1;
                // 遍历子对象，找到role为row的对象
                for (int i = 0; i < nObtained; i++)
                {
                    object childObj = children[i];
                    if (childObj == null)
                        continue;
                    IAccessible childAcc = null;
                    if (childObj is int childIndex)
                    {
                        // Get child by index
                        object indexedChild = tblAcc.accChild[childIndex];
                        if (indexedChild != null)
                        {
                            childAcc = indexedChild as IAccessible;
                            if (childAcc == null)
                            {
                                MarsLoggerSimple.Warning("GetAllRowsFromTableObjects", $"{iMark}|Child {i} indexed by {childIndex} is not IAccessible");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        childAcc = childObj as IAccessible;
                    }
                    if (childAcc == null)
                    {
                        MarsLoggerSimple.Warning("GetAllRowsFromTableObjects", $"{iMark}|Child {i} is not IAccessible");
                        continue;
                    }
                    roleId = MARSAccessibleProvider.Get_Role(childAcc);
                    if (roleId == MARSAccessibleConstans.ROLE_SYSTEM_ROW)
                    {
                        lstRows.Add(childAcc);
                    }
                    else
                    {
                        MarsLoggerSimple.Info("GetAllRowsFromTableObjects", $"{iMark}|Child {i} is not a row, roleId: {roleId}");
                    }
                }
                isOk = true;
                return lstRows;
            }
            catch (Exception e)
            {
                strError = $"Exception in GetAllRowsFromTableObjects: {e.Message}";
                MarsLoggerSimple.Error("GetAllRowsFromTableObjects", $"{iMark}|{strError}", e);
                isOk = false;
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("GetAllRowsFromTableObjects", $"{iMark}|returns|{isOk}|{strError}|");
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect">目标的区域</param>
        /// <param name="strPara">for clickAt</param>
        /// <param name="actionType">事件设置，如left_click等</param>
        /// <param name="strData">暂时不用</param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool ExecuteTableAction(Rectangle rect,string actionType, string strPara, ref string strError)
        {
            MarsLoggerSimple.logBegin("ExecuteTableAction", $"Rect:{rect}, ActionType: {actionType}, para: {strPara}");

            try
            {
                // 根据actionType执行相应的操作
                switch (actionType.ToUpper())
                {
                    case "LEFT_CLICK":
                        return ExecuteLeftClick(rect,strPara, ref strError);
                    
                    case "RIGHT_CLICK":
                        return ExecuteRightClick(rect, strPara, ref strError);
                    
                    case "LEFT_DBL_CLICK":
                        return ExecuteLeftDoubleClick(rect,strPara, ref strError);
                    
                    //case "SCROLL":
                    //    return ExecuteScroll(rect, ref strError);
                    
                    default:
                        if (actionType.StartsWith("CLICK_AT:", StringComparison.OrdinalIgnoreCase))
                        {
                            return ExecuteClickAt(rect, actionType, ref strError);
                        }
                        else
                        {
                            strError = $"Unsupported action type: {actionType}";
                            MarsLoggerSimple.Error("ExecuteTableAction", strError);
                            return false;
                        }
                }
            }
            catch (Exception ex)
            {
                strError = $"Exception in ExecuteTableAction: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteTableAction", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ExecuteTableAction");
            }
        }

        /// <summary>
        /// Executes left click action
        /// </summary>
        private static bool ExecuteLeftClick(Rectangle rect, string strPara, ref string strError)
        {
            MarsLoggerSimple.Info("ExecuteLeftClick", $"Executing left click on rect |{rect}| with data '{strPara}'");
            
            try
            {
                int x = rect.X + rect.Width / 2;
                int y = rect.Y + rect.Height / 2;
                MarsWindowsAPIsExtend.MoveMouse(x, y);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                System.Threading.Thread.Sleep(100);
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to execute left click: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteLeftClick", strError);
                return false;
            }
        }

        /// <summary>
        /// Executes right click action
        /// </summary>
        private static bool ExecuteRightClick(Rectangle rect, string strPara, ref string strError)
        {
            MarsLoggerSimple.Info("ExecuteRightClick", $"Executing left click on rect|{rect}| with data '{strPara}'");
            
            try
            {
                int x = rect.X + rect.Width / 2;
                int y = rect.Y + rect.Height / 2;
                MarsWindowsAPIsExtend.MoveMouse(x, y);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.RightMouseClick(x, y);
                System.Threading.Thread.Sleep(50);
                return true;
                
            }
            catch (Exception ex)
            {
                strError = $"Failed to execute right click: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteRightClick", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ExecuteRightClick");
            }
        }

        /// <summary>
        /// Executes left double click action
        /// </summary>
        private static bool ExecuteLeftDoubleClick(Rectangle rect, string strPara, ref string strError)
        {
            MarsLoggerSimple.Info("ExecuteLeftDoubleClick", $"Executing left click on rect|{rect}| with data '{strPara}'");

            try
            {
                int x = rect.X + rect.Width / 2;
                int y = rect.Y + rect.Height / 2;
                MarsWindowsAPIsExtend.MoveMouse(x, y);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseDblClick(x, y);
                System.Threading.Thread.Sleep(50);
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to execute left double click: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteLeftDoubleClick", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ExecuteLeftDoubleClick");
            }
        }

        /// <summary>
        /// Executes click at specific coordinates action
        /// </summary>
        private static bool ExecuteClickAt(Rectangle rect, string actionType, ref string strError)
        {
            MarsLoggerSimple.Info("ExecuteClickAt", $"Executing left click on rect|{rect}| with data '{actionType}'");
            
            try
            {
                // 解析CLICK_AT:X:Y格式
                // 例如：CLICK_AT:10:20 或 CLICK_AT:-5:-10
                string[] coords = actionType.Substring(9).Split(':'); // Remove "CLICK_AT:" prefix
                if (coords.Length != 2)
                {
                    strError = $"Invalid CLICK_AT format. Expected CLICK_AT:X:Y, but got: {actionType}";
                    MarsLoggerSimple.Error("ExecuteClickAt", strError);
                    return false;
                }

                if (!int.TryParse(coords[0], out int x) || !int.TryParse(coords[1], out int y))
                {
                    strError = $"Invalid coordinates in CLICK_AT. X: {coords[0]}, Y: {coords[1]}";
                    MarsLoggerSimple.Error("ExecuteClickAt", strError);
                    return false;
                }

                MarsLoggerSimple.Info("ExecuteClickAt", $"Clicking at coordinates X: {x}, Y: {y}");
                
                // TODO: 实现具体的坐标点击逻辑
                // 如果x，y为负数，表示从右下角开始计算
                int clickX = (x >= 0) ? (rect.X + x) : (rect.Right + x);
                int clickY = (y >= 0) ? (rect.Y + y) : (rect.Bottom + y);
                MarsWindowsAPIsExtend.MoveMouse(clickX, clickY);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.MoveMouse(clickX, clickY);
                System.Threading.Thread.Sleep(100);
                MarsLoggerSimple.Info("ExecuteClickAt", "Click at coordinates executed successfully");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to execute click at coordinates: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteClickAt", strError);
                return false;
            }
        }

        /// <summary>
        /// Executes scroll action to make cell visible
        /// </summary>
        private static bool ExecuteScroll(dynamic targetObject, string columnHeader, string strData, ref string strError)
        {
            MarsLoggerSimple.Info("ExecuteScroll", $"Executing scroll to make cell visible on column '{columnHeader}' with data '{strData}'");
            
            try
            {
                // TODO: 实现具体的滚动逻辑
                // SCROLL表示将找到的单元格移动到视图可视区域
                
                MarsLoggerSimple.Info("ExecuteScroll", "Scroll executed successfully");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to execute scroll: {ex.Message}";
                MarsLoggerSimple.Error("ExecuteScroll", strError);
                return false;
            }
        }

        /// <summary>
        /// Finds table object with rows by traversing IAccessible children
        /// </summary>
        /// <param name="targetObject">Target accessible object</param>
        /// <param name="tableObject">Found table object</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if table with rows found, false otherwise</returns>
        private static bool FindTableWithRows(dynamic targetObject, ref dynamic tableObject, ref string strError)
        {
            MarsLoggerSimple.logBegin("FindTableWithRows", "Searching for table with rows");

            try
            {
                // Convert dynamic object to IAccessible
                IAccessible accessible = targetObject as IAccessible;
                if (accessible == null)
                {
                    strError = "Target object is not an IAccessible object";
                    MarsLoggerSimple.Error("FindTableWithRows", strError);
                    return false;
                }

                // Get role and role name of the current object
                object roleObj = accessible.get_accRole(0);
                int iRole = (roleObj is int) ? (int)roleObj : -1;

                string roleName = MARSAccessibleProvider.GetRoleName(iRole);
                MarsLoggerSimple.Info("FindTableWithRows", $"Current object role: {roleObj}, roleName: {roleName}");

                // Check if current object is ButtonDropDownGrid - stop traversal and return
                if (roleObj is int roleInt && roleInt == MARSAccessibleConstans.ROLE_SYSTEM_BUTTONDROPDOWNGRID)
                {
                    MarsLoggerSimple.Info("FindTableWithRows", "Found ButtonDropDownGrid, stopping traversal");
                    tableObject = targetObject;
                    return true;
                }

                // Check if current object is a row - get its parent
                if (roleObj is int rowRoleInt && rowRoleInt == MARSAccessibleConstans.ROLE_SYSTEM_ROW)
                {
                    MarsLoggerSimple.Info("FindTableWithRows", "Found row object, getting its parent");
                    
                    object parentObj = accessible.accParent;
                    if (parentObj != null)
                    {
                        MarsLoggerSimple.Info("FindTableWithRows", "Got row parent, checking if it's a table");
                        tableObject = parentObj;
                        return true;
                    }
                    else
                    {
                        MarsLoggerSimple.Warning("FindTableWithRows", $"Failed to get row parent");
                    }
                }

                // Check if current object is a table
                if (IsTableRole(roleObj))
                {
                    MarsLoggerSimple.Info("FindTableWithRows", "Current object is a table, checking for rows");
                    
                    // Check if this table has rows
                    if (HasRows(accessible))
                    {
                        tableObject = targetObject;
                        MarsLoggerSimple.Info("FindTableWithRows", "Found table with rows");
                        return true;
                    }
                }

                // Get child count
                int childCount = accessible.accChildCount;
                
                MarsLoggerSimple.Info("FindTableWithRows", $"Child count: {childCount}");

                // Traverse children using MARSAccessibleProvider.AccessibleChildren
                if (childCount > 0)
                {
                    object[] children = new object[childCount];
                    int obtained = MARSAccessibleProvider.AccessibleChildren(accessible, 0, childCount, children, out int nObtained);
                    
                    if (obtained == 0 && nObtained > 0)
                    {
                        MarsLoggerSimple.Info("FindTableWithRows", $"Found {nObtained} children using AccessibleChildren");
                        
                        for (int i = 0; i < nObtained; i++)
                        {
                            object childObj = children[i];
                            if (childObj == null)
                            {
                                MarsLoggerSimple.Warning("FindTableWithRows", $"Child {i} is null");
                                continue;
                            }

                            // Check if child is a number (index)
                            if (childObj is int childIndex)
                            {
                                MarsLoggerSimple.Info("FindTableWithRows", $"Child {i} is index: {childIndex}");
                                
                                // Get child by index from row object
                                object indexedChild = accessible.accChild[childIndex];
                                if (indexedChild != null)
                                {
                                    if (FindTableWithRows(indexedChild, ref tableObject, ref strError))
                                    {
                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                // Child is an IAccessible object
                                MarsLoggerSimple.Info("FindTableWithRows", $"Child {i} is IAccessible object");
                                
                                if (FindTableWithRows(childObj, ref tableObject, ref strError))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        MarsLoggerSimple.Warning("FindTableWithRows", $"Failed to get child objects using AccessibleChildren. Result: {obtained}, Obtained: {nObtained}");
                    }
                }

                strError = "No table with rows found in the object hierarchy";
                MarsLoggerSimple.Error("FindTableWithRows", strError);
                return false;
            }
            catch (Exception ex)
            {
                strError = $"Exception in FindTableWithRows: {ex.Message}";
                MarsLoggerSimple.Error("FindTableWithRows", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindTableWithRows");
            }
        }


        /// <summary>
        /// Checks if role object represents a table
        /// </summary>
        /// <param name="roleObj">Role object</param>
        /// <returns>True if role is table-related</returns>
        private static bool IsTableRole(object roleObj)
        {
            if (roleObj is int roleInt)
            {
                return roleInt == MARSAccessibleConstans.ROLE_SYSTEM_TABLE || roleInt == MARSAccessibleConstans.ROLE_SYSTEM_ROW || 
                       roleInt == MARSAccessibleConstans.ROLE_SYSTEM_CELL || roleInt == MARSAccessibleConstans.ROLE_SYSTEM_COLUMNHEADER || 
                       roleInt == MARSAccessibleConstans.ROLE_SYSTEM_ROWHEADER;
            }
            return false;
        }

        /// <summary>
        /// Checks if accessible object has rows
        /// </summary>
        /// <param name="accessible">IAccessible object</param>
        /// <returns>True if object has rows</returns>
        private static bool HasRows(IAccessible accessible)
        {
            try
            {
                int childCount = accessible.accChildCount;
                if (childCount <= 0) return false;

                // Use MARSAccessibleProvider.AccessibleChildren to get child objects
                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(accessible, 0, childCount, children, out int nObtained);
                
                if (obtained != 0 || nObtained <= 0) return false;

                // Check if any child is a row
                for (int i = 0; i < nObtained; i++)
                {
                    object childObj = children[i];
                    if (childObj == null) continue;

                    if (childObj is int childIndex)
                    {
                        // Get child by index
                        object indexedChild = accessible.accChild[childIndex];
                        if (indexedChild != null)
                        {
                            IAccessible childAccessible = indexedChild as IAccessible;
                            if (childAccessible != null)
                            {
                                object roleObj = childAccessible.get_accRole(0);
                                if (roleObj is int roleInt && roleInt == MARSAccessibleConstans.ROLE_SYSTEM_ROW)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Check if child is a row
                        IAccessible childAccessible = childObj as IAccessible;
                        if (childAccessible != null)
                        {
                            object roleObj = childAccessible.get_accRole(0);
                            if (roleObj is int roleInt && roleInt == MARSAccessibleConstans.ROLE_SYSTEM_ROW)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从一个IAccessible对象向下查找所有的子对象到一个list中，直到是DataGrid的对象
        /// </summary>
        /// <param name="parentObject">父级IAccessible对象</param>
        /// <param name="childObjects">用于存储子对象的列表</param>
        /// <param name="maxDepth">最大搜索深度，防止无限递归</param>
        /// <param name="currentDepth">当前搜索深度</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool FindDataGridChildren(IAccessible parentObject, List<IAccessible> childObjects, 
            ref string strError, int maxDepth = 10, int currentDepth = 0)
        {
            try
            {
                MarsLoggerSimple.logBegin("FindDataGridChildren", $"Searching at depth {currentDepth}");

                if (parentObject == null)
                {
                    MarsLoggerSimple.Warning("FindDataGridChildren", strError="Parent object is null");
                    return false;
                }

                if (currentDepth >= maxDepth)
                {
                    MarsLoggerSimple.Warning("FindDataGridChildren", strError= $"Maximum depth {maxDepth} reached, stopping search");
                    return false;
                }   

                // 检查当前对象是否是DataGrid
                if (IsDataGridObject(parentObject))
                {
                    MarsLoggerSimple.Info("FindDataGridChildren", strError= $"Found DataGrid object at depth {currentDepth}");
                    childObjects.Add(parentObject);
                    return true;
                }

                // 获取子对象数量
                int childCount = 0;
                try
                {
                    childCount = parentObject.accChildCount;
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Warning("FindDataGridChildren", strError= $"Failed to get child count at depth {currentDepth}: {ex.Message}");
                    return false;
                }

                if (childCount <= 0)
                {
                    MarsLoggerSimple.Info("FindDataGridChildren", strError= $"No children found at depth {currentDepth}");
                    return false;
                }

                // 使用MARSAccessibleProvider.AccessibleChildren获取子对象
                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(parentObject, 0, childCount, children, out int nObtained);
                
                if (obtained != 0 || nObtained <= 0)
                {
                    MarsLoggerSimple.Warning("FindDataGridChildren", strError=$"Failed to get child objects at depth {currentDepth}. Result: {obtained}, Obtained: {nObtained}");
                    return false;
                }

                MarsLoggerSimple.Info("FindDataGridChildren", $"Found {nObtained} children at depth {currentDepth}");

                // 递归搜索每个子对象
                bool foundDataGrid = false;
                for (int i = 0; i < nObtained; i++)
                {
                    try
                    {
                        object childObject = children[i];
                        if (childObject == null)
                            continue;

                        if (childObject is IAccessible childAcc)
                        {
                            // 递归搜索子对象
                            if (FindDataGridChildren(childAcc, childObjects,ref strError, maxDepth, currentDepth + 1))
                            {
                                foundDataGrid = true;
                            }
                        }
                        else
                        {
                            MarsLoggerSimple.Info("FindDataGridChildren", strError = $"Child object {i} at depth {currentDepth} is not IAccessible: {childObject?.GetType()?.Name ?? "null"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("FindDataGridChildren", strError = $"Error processing child object {i} at depth {currentDepth}: {ex.Message}");
                    }
                }

                MarsLoggerSimple.logEnd("FindDataGridChildren", strError = $"Search completed at depth {currentDepth}, found DataGrid: {foundDataGrid}");
                return foundDataGrid;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindDataGridChildren", strError = $"Exception at depth {currentDepth}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 判断一个IAccessible对象是否是DataGrid对象
        /// </summary>
        /// <param name="accessibleObject">要检查的IAccessible对象</param>
        /// <returns>True if the object is a DataGrid, false otherwise</returns>
        private static bool IsDataGridObject(IAccessible accessibleObject)
        {
            try
            {
                if (accessibleObject == null)
                    return false;

                // 获取对象的角色
                object roleObj = accessibleObject.get_accRole(0);
                if (roleObj == null)
                    return false;

                int role = 0;
                if (roleObj is int roleInt)
                {
                    role = roleInt;
                }
                else if (roleObj is string roleStr && int.TryParse(roleStr, out int parsedRole))
                {
                    role = parsedRole;
                }
                else
                {
                    return false;
                }

                // 检查是否是表格相关的角色
                // ROLE_SYSTEM_TABLE = 0x2F (47)
                // ROLE_SYSTEM_LIST = 0x21 (33) - 有时候DataGrid会显示为List-
                bool isTableRole = (role == MARSAccessibleConstans.ROLE_SYSTEM_DROPLIST )// 0x2F)
                                || (role == MARSAccessibleConstans.ROLE_SYSTEM_LIST)
                                || (role == MARSAccessibleConstans.ROLE_SYSTEM_TABLE);

                // 检查角色名称是否为ButtonDropDownGrid
                bool isButtonDropDownGrid = false;
                try
                {
                    string roleName = MARSAccessibleProvider.GetRoleName(role);
                    isButtonDropDownGrid = string.Equals(roleName, "ButtonDropDownGrid", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Warning("IsDataGridObject", $"Failed to get role name: {ex.Message}");
                }

                if (isTableRole || isButtonDropDownGrid)
                {
                    // 获取对象名称和值进行进一步验证
                    string objectName = "";
                    string objectValue = "";
                    try
                    {
                        objectName = accessibleObject.get_accName(0) ?? "";
                        objectValue = accessibleObject.get_accValue(0) ?? "";
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("IsDataGridObject", $"Failed to get object properties: {ex.Message}");
                    }

                    MarsLoggerSimple.Info("IsDataGridObject", $"Found potential DataGrid - Role: {role}, Name: '{objectName}', Value: '{objectValue}'");
                    
                    // 可以根据具体需求添加更多的DataGrid识别条件
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("IsDataGridObject", $"Error checking DataGrid object: {ex.Message}");
                return false;
            }
        }
    }
}
