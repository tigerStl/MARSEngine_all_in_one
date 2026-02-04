using Mars.Inter.MQCenter.interProcess.HostedFramework;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.ThirdPartComponent.DevExpress
{
    public class DevExpressTreeListOpHelper
    {
        private const string LogCategory = nameof(DevExpressTreeListOpHelper) + "::" + nameof(SearchAndClick);

        /// <summary>
        /// Searches all nodes within a DevExpress TreeList control and optionally focuses the first node
        /// whose display text for a specific column satisfies the provided predicate.
        /// </summary>
        /// <param name="treeList">Instance of DevExpress.XtraTreeList.TreeList (passed as object).</param>
        /// <param name="columnIdentifier">Column identifier (field name, caption, or name).</param>
        /// <param name="matchCondition">Predicate used to validate the display text.</param>
        /// <param name="matchedDisplayText">The display text of the matched node if found.</param>
        /// <param name="matchedNode">The node instance that matched the condition.</param>
        /// <param name="setFocusWhenMatched">Indicates whether the matched node should be focused.</param>
        /// <returns>True when a node satisfies the predicate; otherwise, false.</returns>
        public static bool SearchAndClick(
            object treeList,
            string columnIdentifier,
            SearchAndClickData howToSearchAndClick,
            out string matchedDisplayText,
            out object matchedNode,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            ref (int x, int y, int w, int h) centralPos,
            bool setFocusWhenMatched = true)
        {
            matchedDisplayText = null;
            matchedNode = null;
            centralPos = default ;
            if (treeList == null)
            {
                MarsLoggerSimple.Error(LogCategory, "treeList instance is null");
                return false;
            }
            bool isOk = false;
            if (string.IsNullOrWhiteSpace(columnIdentifier))
            {
                MarsLoggerSimple.Error(LogCategory, "columnIdentifier is null or empty");
                return false;
            }
            if (howToSearchAndClick == null || howToSearchAndClick.DataToCompare == null || howToSearchAndClick.DataToCompare.Count == 0)
            {
                strError = "SearchAndClickData or DataToCompare is null or empty.";
                strAdv = "Please ensure that DataToCompare contains at least one level value.";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            try
            {
                var treeListType = treeList.GetType();
                System.Windows.Forms.Control c = treeList as System.Windows.Forms.Control;
                var columns = GetMemberValue(treeListType, treeList, "Columns");
                if (columns == null)
                {
                    MarsLoggerSimple.Error(LogCategory, "Failed to retrieve Columns from TreeList instance");
                    return false;
                }

                var targetColumn = ResolveColumn(columns, columnIdentifier, ref isOk, ref strError, ref strAdv, ref strStack);
                if ((targetColumn == null)||(!isOk))
                {
                    MarsLoggerSimple.Error(LogCategory, $"Column '{columnIdentifier}' not found");
                    return false;
                }

                var comparisonLevels = howToSearchAndClick.DataToCompare?
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();
                if (comparisonLevels == null || comparisonLevels.Count == 0)
                {
                    strError = "DataToCompare does not contain any valid value.";
                    strAdv = "Please ensure DataToCompare contains non-empty values.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
                string strErrorTmp="", strAdvTmp = "", strStackTmp = "";
                var nodeCellContexts = safeInvoke(c, () => BuildNodeCellContexts(treeList, targetColumn, ref strErrorTmp, ref strAdvTmp, ref strStackTmp));
                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                if (nodeCellContexts == null || nodeCellContexts.Count == 0)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "Unable to build TreeList node context from ViewInfo.";
                        strAdv = "Please ensure the TreeList contains visible rows.";
                        strStack = Environment.StackTrace;
                    }
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                var rootCandidates = nodeCellContexts
                    .Where(ctx => ctx.ParentNode == null || ctx.Level == 0)
                    .ToList();

                if (rootCandidates.Count == 0)
                {
                    strError = "No root rows were retrieved from the DevExpress TreeList.";
                    strAdv = "Please ensure the control is populated and expanded.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                var pathParts = new List<string>();
                NodeCellContext selectedContext = null;
                var currentCandidates = rootCandidates;

                for (int level = 0; level < comparisonLevels.Count; level++)
                {
                    string pattern = comparisonLevels[level];
                    var matchedCells = currentCandidates
                        .Where(ctx => !string.IsNullOrEmpty(ctx.Value) && MarsWindowsAPIsExtend.RegularTest(pattern, ctx.Value))
                        .ToList();

                    if (matchedCells.Count != 1)
                    {
                        strError = matchedCells.Count == 0
                            ? $"No cell matches pattern '{pattern}' at level {level + 1}."
                            : $"There are {matchedCells.Count} cells matching pattern '{pattern}' at level {level + 1}.";
                        strAdv = "Please adjust DataToCompare to uniquely identify a node.";
                        strStack = Environment.StackTrace;
                        MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                        return false;
                    }

                    selectedContext = matchedCells[0];
                    pathParts.Add(selectedContext.Value);

                    if (level < comparisonLevels.Count - 1)
                    {
                        currentCandidates = nodeCellContexts
                            .Where(ctx => ReferenceEquals(ctx.ParentNode, selectedContext.Node))
                            .ToList();

                        if (currentCandidates.Count == 0)
                        {
                            strError = $"Matched cell at level {level + 1} has no child rows available for further matching.";
                            strAdv = "Please ensure the target node is expanded and visible.";
                            strStack = Environment.StackTrace;
                            MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                            return false;
                        }
                    }
                }

                if (selectedContext == null)
                {
                    strError = "Failed to determine the final matched node.";
                    strAdv = "Please verify DataToCompare values.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                matchedNode = selectedContext.Node;
                matchedDisplayText = string.Join(">", pathParts);
                MarsLoggerSimple.Info(LogCategory, $"Matched node path '{matchedDisplayText}'");

                if (setFocusWhenMatched)
                {
                    //TryFocusNode(treeList, targetColumn, matchedNode);
                }

                Rectangle screenRect = Rectangle.Empty;
                bool isRectangleFetched = safeInvoke(c, () => TryGetCellScreenRectangle(treeList, selectedContext.Cell, out screenRect));
                MarsLoggerSimple.Info(LogCategory, $"Cell rectangle fetched: {isRectangleFetched}, Rectangle: {screenRect}");
                System.Threading.Thread.Sleep(200);
                if (!isRectangleFetched || screenRect.IsEmpty)
                {
                    strError = "Failed to get the rectangle of the matched cell.";
                    strAdv = "Please ensure that the DevExpress TreeList control is properly rendered and visible.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                int centerX = screenRect.Left + screenRect.Width / 2;
                int centerY = screenRect.Top + screenRect.Height / 2;
                centralPos.x = centerX;
                centralPos.y = centerY;
                MarsWindowsAPIsExtend.MoveMouse(centerX, centerY);
                uint iLastError = MarsWindowsAPIs.GetLastError(); // Clear last error;
                MarsLoggerSimple.Error(LogCategory, $"LastError|{iLastError}");
                System.Threading.Thread.Sleep(80);

                if (howToSearchAndClick.MouseAction.Equals("LEFT_CLICK", StringComparison.OrdinalIgnoreCase))
                {
                    //MarsWindowsAPIsExtend.LeftMouseClickBySendInput(centerX, centerY);
                    //MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);
                }
                else if (howToSearchAndClick.MouseAction.Equals("RIGHT_CLICK", StringComparison.OrdinalIgnoreCase))
                {
                    //MarsWindowsAPIsExtend.RightMouseClick(centerX, centerY);
                }
                else if (howToSearchAndClick.MouseAction.Equals("LEFT_DBL_CLICK", StringComparison.OrdinalIgnoreCase))
                {
                    //MarsWindowsAPIsExtend.LeftMouseDblClick(centerX, centerY);
                } else if (howToSearchAndClick.MouseAction.Equals("DRAW_RECT", StringComparison.OrdinalIgnoreCase))
                {
                    centralPos.x = screenRect.Left;
                    centralPos.y = screenRect.Top;
                    centralPos.w = screenRect.Width;
                    centralPos.h = screenRect.Height;
                    XorDrawing.DrawXorRectangleOnDeskTop(new MarsWindowsAPIs.RECT
                    {
                        Left = screenRect.Left,
                        Top = screenRect.Top,
                        Right = screenRect.Right,
                        Bottom = screenRect.Bottom
                    }, ref strError, 3);
                }
                else
                {
                    strError = $"Data settings are wrong, only LEFT_CLICK|RIGHT_CLICK|LEFT_DBL_CLICK are supported, but the settings are |{howToSearchAndClick.sourceSettings}";
                    strAdv = "Please correct the settings.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
                return true;
                
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error(LogCategory, ex.Message, ex);
                return false;
            }
        }

        private static List<NodeCellContext> BuildNodeCellContexts(
            object treeList,
            object targetColumn,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            var contexts = new List<NodeCellContext>();
            if (treeList == null || targetColumn == null)
            {
                return contexts;
            }

            var viewInfo = ReflectorForCSharp.GetMember(treeList, "ViewInfo");
            if (viewInfo == null)
            {
                strError = "Failed to retrieve ViewInfo from DevExpress TreeList.";
                strAdv = "Ensure the control is fully initialized.";
                strStack = Environment.StackTrace;
                return null;
            }

            var rowsInfo = ReflectorForCSharp.GetMember(viewInfo, "RowsInfo");
            if (rowsInfo == null)
            {
                strError = "Failed to retrieve RowsInfo from TreeList.ViewInfo.";
                strAdv = "Ensure the control uses a supported DevExpress version.";
                strStack = Environment.StackTrace;
                return null;
            }

            object rowsObject = ReflectorForCSharp.GetMember(rowsInfo, "Rows");
            if (rowsObject == null)
            {
                rowsObject = ReflectorForCSharp.GetMember(rowsInfo, "List");
            }

            if (!(rowsObject is IList rows))
            {
                strError = "TreeList.ViewInfo.RowsInfo.Rows is not available or not a list.";
                strAdv = "Ensure the TreeList contains visible rows.";
                strStack = Environment.StackTrace;
                return null;
            }

            var contextsByNode = new Dictionary<object, NodeCellContext>(ReferenceEqualityComparer.Default);
            string targetFieldName = GetStringMember(targetColumn.GetType(), targetColumn, "FieldName")
                                     ?? GetStringMember(targetColumn.GetType(), targetColumn, "Name");

            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                var node = ReflectorForCSharp.GetMember(row, "Node");
                if (node == null)
                {
                    continue;
                }

                if (contextsByNode.ContainsKey(node))
                {
                    continue;
                }

                var cellsObject = ReflectorForCSharp.GetMember(row, "Cells");
                if (!(cellsObject is IEnumerable cellEnumerable))
                {
                    continue;
                }

                object matchedCell = null;
                foreach (var cell in cellEnumerable)
                {
                    if (cell == null)
                    {
                        continue;
                    }

                    var cellColumn = ReflectorForCSharp.GetMember(cell, "Column");
                    if (cellColumn == null)
                    {
                        continue;
                    }

                    bool isSameColumn = ReferenceEquals(cellColumn, targetColumn);
                    if (!isSameColumn)
                    {
                        string cellFieldName = GetStringMember(cellColumn.GetType(), cellColumn, "FieldName")
                                               ?? GetStringMember(cellColumn.GetType(), cellColumn, "Name");

                        if (!string.IsNullOrEmpty(cellFieldName) && !string.IsNullOrEmpty(targetFieldName))
                        {
                            isSameColumn = string.Equals(cellFieldName, targetFieldName, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (!isSameColumn)
                    {
                        continue;
                    }

                    matchedCell = cell;
                    break;
                }

                if (matchedCell == null)
                {
                    continue;
                }

                string displayValue = GetCellValueString(matchedCell);
                if (string.IsNullOrEmpty(displayValue))
                {
                    displayValue = GetNodeDisplayText(node, targetColumn) ?? string.Empty;
                }

                var parentNode = ReflectorForCSharp.GetMember(node, "ParentNode")
                                  ?? ReflectorForCSharp.GetMember(node, "Parent");
                int level = ConvertToInt(ReflectorForCSharp.GetMember(node, "Level"));

                var context = new NodeCellContext
                {
                    Node = node,
                    ParentNode = parentNode,
                    Cell = matchedCell,
                    Value = displayValue ?? string.Empty,
                    Level = level,
                    Row = row
                };

                contextsByNode[node] = context;
                contexts.Add(context);
            }

            if (contexts.Count == 0)
            {
                strError = "No matching cell information could be retrieved from the TreeList.";
                strAdv = "Please ensure the TreeList column identifier is correct and rows are visible.";
                strStack = Environment.StackTrace;
                return null;
            }

            return contexts;
        }

        private static string GetCellValueString(object cell)
        {
            if (cell == null)
            {
                return null;
            }

            string[] propertyCandidates = { "CellValue", "Value", "DisplayText", "Text", "EditValue" };
            foreach (var property in propertyCandidates)
            {
                bool isNotExist = false;
                var value = ReflectorForCSharp.GetMember(cell, property, ref isNotExist);
                if (!isNotExist && value != null)
                {
                    return value.ToString();
                }
            }

            return null;
        }

        private static bool TryGetCellScreenRectangle(object treeList, object cell, out Rectangle screenRectangle)
        {
            screenRectangle = Rectangle.Empty;
            if (treeList == null || cell == null)
            {
                return false;
            }

            if (!TryGetRectangleFromCell(cell, out var cellRectangle))
            {
                return false;
            }

            var treeListType = treeList.GetType();
            try
            {
                var rectToScreen = treeListType.GetMethod(
                    "RectToScreen",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Rectangle) },
                    null);
                if (rectToScreen != null)
                {
                    var result = rectToScreen.Invoke(treeList, new object[] { cellRectangle });
                    if (result is Rectangle rect)
                    {
                        screenRectangle = rect;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore and fall back to Control-based conversion.
            }

            if (treeList is System.Windows.Forms.Control control)
            {
                screenRectangle = control.RectangleToScreen(cellRectangle);
                return true;
            }

            return false;
        }

        private static bool TryGetRectangleFromCell(object cell, out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            if (cell == null)
            {
                return false;
            }

            string[] rectangleMembers = { "CellValueRect", "Bounds", "Rect" };
            foreach (var member in rectangleMembers)
            {
                bool isNotExist = false;
                var candidate = ReflectorForCSharp.GetMember(cell, member, ref isNotExist);
                if (isNotExist || candidate == null)
                {
                    continue;
                }

                if (TryConvertToRectangle(candidate, out rectangle))
                {
                    return !rectangle.IsEmpty;
                }
            }

            return false;
        }

        private static bool TryConvertToRectangle(object value, out Rectangle rectangle)
        {
            rectangle = Rectangle.Empty;
            if (value == null)
            {
                return false;
            }

            if (value is Rectangle rect)
            {
                rectangle = rect;
                return true;
            }

            if (value is RectangleF rectF)
            {
                rectangle = Rectangle.Round(rectF);
                return true;
            }

            var type = value.GetType();
            var xMember = type.GetProperty("X") ?? (MemberInfo)type.GetField("X");
            var yMember = type.GetProperty("Y") ?? (MemberInfo)type.GetField("Y");
            var widthMember = type.GetProperty("Width") ?? (MemberInfo)type.GetField("Width");
            var heightMember = type.GetProperty("Height") ?? (MemberInfo)type.GetField("Height");

            if (xMember == null || yMember == null || widthMember == null || heightMember == null)
            {
                return false;
            }

            try
            {
                int x = Convert.ToInt32(GetMemberValue(xMember, value));
                int y = Convert.ToInt32(GetMemberValue(yMember, value));
                int width = Convert.ToInt32(GetMemberValue(widthMember, value));
                int height = Convert.ToInt32(GetMemberValue(heightMember, value));
                rectangle = new Rectangle(x, y, width, height);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object GetMemberValue(MemberInfo memberInfo, object instance)
        {
            if (memberInfo == null || instance == null)
            {
                return null;
            }

            return memberInfo switch
            {
                PropertyInfo propertyInfo => propertyInfo.GetValue(instance),
                FieldInfo fieldInfo => fieldInfo.GetValue(instance),
                _ => null
            };
        }

        private static object GetMemberValue(Type ownerType, object instance, string memberName)
        {
            if (ownerType == null || instance == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            try
            {
                var property = ownerType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    return property.GetValue(instance);
                }

                var field = ownerType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(instance);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error(LogCategory, $"GetMemberValue failed for {memberName}: {ex.Message}", ex);
            }

            return null;
        }

        private static object ResolveColumn(object columns, string columnIdentifier, 
            ref bool isOk, 
            ref string strError, ref string strAdv, ref string strStack)
        {
            if (columns == null)
            {
                isOk = false;
                strError = "TreeList Columns is null.";
                strAdv = "Please ensure that DevExpress TreeList is selected";
                strStack = Environment.StackTrace;
                return null;
            }

            var columnsType = columns.GetType();
            List<object> matchedColumns = new List<object>();
            // Try indexer with string parameter first (Columns["FieldName"]).
            var indexerByString = columnsType.GetMethod("get_Item", new[] { typeof(string) });
            //var innerList = columnsType.GetMember("InnerList", BindingFlags.Public);
            var innerList = ReflectorForCSharp.GetMember(columns, "InnerList");
            bool isNotExists = false;
            string strAllText = "";
            if (innerList is System.Collections.IList lstColumns)
            {
                foreach (var c in lstColumns)
                {
                    if (c == null) continue;

                    var o = ReflectorForCSharp.GetMember(c, "Caption", ref isNotExists);
                    if (o != null)
                    {
                        string strCaption = o.ToString();
                        strAllText = $"{strAllText};{strCaption}";
                        if (MarsWindowsAPIsExtend.RegularTest(columnIdentifier, strCaption))
                        {
                            matchedColumns.Add(c);
                        }
                    }
                }
                if (matchedColumns.Count != 1)
                {
                    strError = $"DevExpress TreeList Columns.InnerList has {matchedColumns.Count} columns matched the identifier '{columnIdentifier}' from '{strAllText}'.";
                    strAdv = "Please ensure that the column identifier is unique.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("ResolveColumn", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    isOk = false;
                    return null;
                }
                else 
                {
                    isOk = true;
                    return matchedColumns[0];
                }                
            }
            else {
                strError = "DevExpress TreeList Columns.InnerList is null. or Not a list";
                strAdv = "Please ensure that DevExpress TreeList is selected";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("ResolveColumn", $"{strError}\r\n{strAdv}\r\n{strStack}");
                isOk = false;
                return null;
            }            
        }

        private static string GetStringMember(Type type, object instance, string memberName)
        {
            if (type == null || instance == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            try
            {
                var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    var value = property.GetValue(instance);
                    return value?.ToString();
                }

                var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var value = field.GetValue(instance);
                    return value?.ToString();
                }
            }
            catch
            {
                // Ignore failures and fallback to null.
            }

            return null;
        }

        private static bool IsIdentifierMatched(string identifier, string candidate)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            return string.Equals(identifier, candidate, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<object> EnumerateNodes(object nodesCollection)
        {
            if (nodesCollection == null)
            {
                yield break;
            }

            var visited = new HashSet<object>(ReferenceEqualityComparer.Default);
            var stack = new Stack<object>();
            stack.Push(nodesCollection);

            while (stack.Count > 0)
            {
                var currentCollection = stack.Pop();
                if (currentCollection == null)
                {
                    continue;
                }

                foreach (var node in EnumerateCollectionItems(currentCollection))
                {
                    if (node == null)
                    {
                        continue;
                    }

                    if (!visited.Add(node))
                    {
                        continue;
                    }

                    yield return node;

                    var childNodes = GetMemberValue(node.GetType(), node, "Nodes");
                    if (childNodes != null)
                    {
                        stack.Push(childNodes);
                    }
                }
            }
        }

        private static IEnumerable<object> EnumerateCollectionItems(object collection)
        {
            if (collection is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    yield return item;
                }
                yield break;
            }

            var collectionType = collection.GetType();
            var countMember = collectionType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? (MemberInfo)collectionType.GetField("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var getItemMethod = collectionType.GetMethod("get_Item", new[] { typeof(int) });

            if (countMember == null || getItemMethod == null)
            {
                yield break;
            }

            var rawCount = countMember is PropertyInfo pi ? pi.GetValue(collection) : ((FieldInfo)countMember).GetValue(collection);
            var count = ConvertToInt(rawCount);

            for (var index = 0; index < count; index++)
            {
                object item = null;
                try
                {
                    item = getItemMethod.Invoke(collection, new object[] { index });
                }
                catch
                {
                    // Ignore and continue.
                }

                if (item != null)
                {
                    yield return item;
                }
            }
        }

        private static string GetNodeDisplayText(object node, object column)
        {
            if (node == null || column == null)
            {
                return null;
            }

            var nodeType = node.GetType();

            try
            {
                var getDisplayText = nodeType.GetMethod("GetDisplayText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { column.GetType() }, null);
                if (getDisplayText != null)
                {
                    var value = getDisplayText.Invoke(node, new[] { column });
                    return value?.ToString();
                }
            }
            catch
            {
                // Ignore and fallback to alternative strategies.
            }

            try
            {
                var fieldName = GetStringMember(column.GetType(), column, "FieldName") ??
                                GetStringMember(column.GetType(), column, "Name");

                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    var getValue = nodeType.GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
                    if (getValue != null)
                    {
                        var value = getValue.Invoke(node, new object[] { fieldName });
                        return value?.ToString();
                    }
                }
            }
            catch
            {
                // Ignore.
            }

            return null;
        }

        private static void TryFocusNode(object treeList, object column, object node)
        {
            if (treeList == null || node == null)
            {
                return;
            }

            var treeListType = treeList.GetType();
            var nodeType = node.GetType();

            try
            {
                var makeNodeVisible = treeListType.GetMethod("MakeNodeVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { nodeType }, null);
                makeNodeVisible?.Invoke(treeList, new[] { node });
            }
            catch
            {
                // Ignore.
            }

            try
            {
                var setFocusedNode = treeListType.GetMethod("SetFocusedNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { nodeType }, null);
                setFocusedNode?.Invoke(treeList, new[] { node });
            }
            catch
            {
                // Ignore.
            }

            if (column != null)
            {
                var columnType = column.GetType();
                try
                {
                    var setFocusedColumn = treeListType.GetMethod("SetFocusedColumn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { columnType }, null);
                    if (setFocusedColumn != null)
                    {
                        setFocusedColumn.Invoke(treeList, new[] { column });
                    }
                    else
                    {
                        var focusedColumnProperty = treeListType.GetProperty("FocusedColumn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        focusedColumnProperty?.SetValue(treeList, column);
                    }
                }
                catch
                {
                    // Ignore.
                }
            }

            try
            {
                var showEditor = treeListType.GetMethod("ShowEditor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                showEditor?.Invoke(treeList, null);
            }
            catch
            {
                // Ignore.
            }
        }

        private static int ConvertToInt(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        public static bool TryGetNodeRectangle(
            object treeList,
            object node,
            out Rectangle rectangle,
            object column = null,
            bool exactBounds = true)
        {
            rectangle = Rectangle.Empty;

            if (treeList == null)
            {
                MarsLoggerSimple.Error(LogCategory, "treeList instance is null when getting node rectangle");
                return false;
            }

            if (node == null)
            {
                MarsLoggerSimple.Error(LogCategory, "node instance is null when getting node rectangle");
                return false;
            }

            try
            {
                var treeListType = treeList.GetType();
                var nodeType = node.GetType();
                var methods = treeListType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => string.Equals(m.Name, "GetNodeDisplayRect", StringComparison.Ordinal))
                    .ToArray();

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        continue;
                    }

                    if (!parameters[0].ParameterType.IsAssignableFrom(nodeType))
                    {
                        continue;
                    }

                    object[] args;
                    if (parameters.Length == 1)
                    {
                        args = new[] { node };
                    }
                    else if (parameters.Length == 2)
                    {
                        var secondParamType = parameters[1].ParameterType;

                        if (secondParamType == typeof(bool))
                        {
                            args = new object[] { node, exactBounds };
                        }
                        else if (column != null && secondParamType.IsInstanceOfType(column))
                        {
                            args = new[] { node, column };
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (parameters.Length == 3)
                    {
                        // Handle overloads such as (node, column, bool) if present.
                        var secondParamType = parameters[1].ParameterType;
                        var thirdParamType = parameters[2].ParameterType;

                        if (column != null && secondParamType.IsInstanceOfType(column) && thirdParamType == typeof(bool))
                        {
                            args = new[] { node, column, (object)exactBounds };
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var result = method.Invoke(treeList, args);
                    if (result is Rectangle rc && !rc.IsEmpty)
                    {
                        rectangle = rc;
                        return true;
                    }
                }

                // Fallback to GetRowBounds if available.
                var altMethod = treeListType.GetMethod("GetRowBounds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (altMethod != null)
                {
                    var parameters = altMethod.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(nodeType))
                    {
                        var result = altMethod.Invoke(treeList, new[] { node });
                        if (result is Rectangle rc && !rc.IsEmpty)
                        {
                            rectangle = rc;
                            return true;
                        }
                    }
                }

                MarsLoggerSimple.Info(LogCategory, "No suitable reflection path found for node rectangle");
                return false;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error(LogCategory, ex.Message, ex);
                return false;
            }
        }

        private sealed class NodeCellContext
        {
            public object Node { get; set; }
            public object ParentNode { get; set; }
            public object Cell { get; set; }
            public object Row { get; set; }
            public string Value { get; set; }
            public int Level { get; set; }
        }

        /// <summary>
        /// Provides reference equality comparison for tracking visited nodes.
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Default = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        public static void safeInvoke(System.Windows.Forms.Control control, Action action)
        {
            if (control == null || action == null) return;
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }

        public static T safeInvoke<T>(System.Windows.Forms.Control control, Func<T> func)
        {
            if (func == null) return default;
            if (control == null) return func();
            if (control.InvokeRequired)
            {
                return (T)control.Invoke(func);
            }

            return func();
        }
    }
}
