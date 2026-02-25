using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport
{
    public static class MarsUIInspector
    {
        public static string InspectElementPatterns(AutomationElement el, bool deepGridTable = true, int maxCellsToDump = 2000)
        {
            if (el == null) return "AutomationElement is null.";

            var sb = new StringBuilder();

            sb.AppendLine("=== AutomationElement Basic Info ===");
            AppendBasicInfo(sb, el);

            sb.AppendLine();
            sb.AppendLine("=== Supported Patterns ===");

            // 1) 列出当前元素支持的所有 Pattern（最实用的方式：遍历 AutomationPattern.LookupById）
            foreach (var p in GetAllAutomationPatterns())
            {
                object patObj = null;
                bool ok = false;
                try
                {
                    ok = el.TryGetCurrentPattern(p, out patObj) && patObj != null;
                }
                catch
                {
                    ok = false;
                }

                if (ok)
                {
                    sb.AppendLine($"- {p.ProgrammaticName} (Id={p.Id})");
                }
            }

            // 2) 深挖：Grid / Table
            if (deepGridTable)
            {
                sb.AppendLine();
                sb.AppendLine("=== Deep Dive: Grid/Table ===");
                DumpGridAndTable(sb, el, maxCellsToDump);
            }

            // 3) 补充：常见 Pattern 细节（你做录制/回放通常也要）
            sb.AppendLine();
            sb.AppendLine("=== Other Common Pattern Details ===");
            DumpCommonPatterns(sb, el);

            return sb.ToString();
        }

        // -------------------------
        // Basic Info
        // -------------------------
        private static void AppendBasicInfo(StringBuilder sb, AutomationElement el)
        {
            string SafeGet(Func<object> getter)
            {
                try { return Convert.ToString(getter(), CultureInfo.InvariantCulture) ?? ""; }
                catch { return ""; }
            }

            sb.AppendLine($"Name: {SafeGet(() => el.Current.Name)}");
            sb.AppendLine($"AutomationId: {SafeGet(() => el.Current.AutomationId)}");
            sb.AppendLine($"ClassName: {SafeGet(() => el.Current.ClassName)}");
            sb.AppendLine($"ControlType: {SafeGet(() => el.Current.ControlType?.ProgrammaticName)}");
            sb.AppendLine($"FrameworkId: {SafeGet(() => el.Current.FrameworkId)}");
            sb.AppendLine($"ProcessId: {SafeGet(() => el.Current.ProcessId)}");
            sb.AppendLine($"IsEnabled: {SafeGet(() => el.Current.IsEnabled)}");
            sb.AppendLine($"IsOffscreen: {SafeGet(() => el.Current.IsOffscreen)}");
            sb.AppendLine($"HasKeyboardFocus: {SafeGet(() => el.Current.HasKeyboardFocus)}");
            sb.AppendLine($"IsKeyboardFocusable: {SafeGet(() => el.Current.IsKeyboardFocusable)}");
            sb.AppendLine($"IsContentElement: {SafeGet(() => el.Current.IsContentElement)}");
            sb.AppendLine($"IsControlElement: {SafeGet(() => el.Current.IsControlElement)}");
            sb.AppendLine($"BoundingRectangle: {SafeGet(() => el.Current.BoundingRectangle)}");

            // RuntimeId 便于你做对象指纹，但注意：不是跨进程/跨会话稳定
            try
            {
                var rid = el.GetRuntimeId();
                sb.AppendLine($"RuntimeId: [{string.Join(",", rid)}]");
            }
            catch
            {
                sb.AppendLine("RuntimeId: <unavailable>");
            }
        }

        // -------------------------
        // Patterns enumeration helper
        // -------------------------
        private static IEnumerable<AutomationPattern> GetAllAutomationPatterns()
        {
            // 兼容：UIA 会不断加 pattern，这种方式能拿到系统里已注册的 pattern
            for (int id = 0; id < 10000; id++)
            {
                AutomationPattern p = null;
                try { p = AutomationPattern.LookupById(id); }
                catch { /* ignore */ }

                if (p != null) yield return p;
            }
        }

        // -------------------------
        // Grid/Table Deep Dive
        // -------------------------
        private static void DumpGridAndTable(StringBuilder sb, AutomationElement el, int maxCellsToDump)
        {
            // 1) GridPattern
            if (el.TryGetCurrentPattern(GridPattern.Pattern, out var gridObj) && gridObj is GridPattern grid)
            {
                int rows = Safe(() => grid.Current.RowCount, -1);
                int cols = Safe(() => grid.Current.ColumnCount, -1);

                sb.AppendLine("[GridPattern]");
                sb.AppendLine($"RowCount={rows}, ColumnCount={cols}");

                if (rows > 0 && cols > 0)
                {
                    // 示例：取(0,0) cell
                    var cell00 = Safe(() => grid.GetItem(0, 0), null);
                    if (cell00 != null)
                        sb.AppendLine($"Cell(0,0) Name='{Safe(() => cell00.Current.Name, "")}', ControlType={Safe(() => cell00.Current.ControlType?.ProgrammaticName, "")}");

                    // 遍历所有 cell（注意：大表格可能很重）
                    long total = (long)rows * cols;
                    long dumpCount = Math.Min(total, maxCellsToDump);

                    sb.AppendLine($"Dumping up to {dumpCount}/{total} cells:");

                    long dumped = 0;
                    for (int r = 0; r < rows && dumped < dumpCount; r++)
                    {
                        for (int c = 0; c < cols && dumped < dumpCount; c++)
                        {
                            AutomationElement cell = null;
                            try { cell = grid.GetItem(r, c); } catch { cell = null; }
                            if (cell == null)
                            {
                                sb.AppendLine($"  - Cell({r},{c}) <null>");
                                dumped++;
                                continue;
                            }

                            string val = TryGetValueQuick(cell);
                            string rect = Safe(() => cell.Current.BoundingRectangle.ToString(), "");
                            string ct = Safe(() => cell.Current.ControlType?.ProgrammaticName, "");

                            sb.AppendLine($"  - Cell({r},{c}) Name='{Safe(() => cell.Current.Name, "")}', Value='{val}', Type={ct}, Rect={rect}");
                            dumped++;
                        }
                    }
                }

                sb.AppendLine();
            }

            // 2) TablePattern
            if (el.TryGetCurrentPattern(TablePattern.Pattern, out var tableObj) && tableObj is TablePattern table)
            {
                sb.AppendLine("[TablePattern]");
                sb.AppendLine($"RowOrColumnMajor={Safe(() => table.Current.RowOrColumnMajor.ToString(), "Unknown")}");

                // Row Headers
                var rowHeaders = Safe(() => table.Current.GetRowHeaders(), Array.Empty<AutomationElement>());
                sb.AppendLine($"RowHeaders Count={rowHeaders?.Length ?? 0}");
                if (rowHeaders != null)
                {
                    foreach (var h in rowHeaders.Take(50))
                        sb.AppendLine($"  - RowHeader: '{Safe(() => h.Current.Name, "")}', Type={Safe(() => h.Current.ControlType?.ProgrammaticName, "")}");
                    if (rowHeaders.Length > 50) sb.AppendLine("  ... (truncated)");
                }

                // Column Headers
                var colHeaders = Safe(() => table.Current.GetColumnHeaders(), Array.Empty<AutomationElement>());
                sb.AppendLine($"ColumnHeaders Count={colHeaders?.Length ?? 0}");
                if (colHeaders != null)
                {
                    foreach (var h in colHeaders.Take(50))
                        sb.AppendLine($"  - ColumnHeader: '{Safe(() => h.Current.Name, "")}', Type={Safe(() => h.Current.ControlType?.ProgrammaticName, "")}");
                    if (colHeaders.Length > 50) sb.AppendLine("  ... (truncated)");
                }

                // 如果它也支持 GridPattern：可以从 cell 取 row/column header（最常用）
                if (el.TryGetCurrentPattern(GridPattern.Pattern, out var gridObj2) && gridObj2 is GridPattern grid2)
                {
                    var sampleCell = Safe(() => grid2.GetItem(0, 0), null);
                    if (sampleCell != null && sampleCell.TryGetCurrentPattern(TableItemPattern.Pattern, out var tiObj) && tiObj is TableItemPattern tip)
                    {
                        var rh = Safe(() => tip.Current.GetRowHeaderItems(), Array.Empty<AutomationElement>());
                        var ch = Safe(() => tip.Current.GetColumnHeaderItems(), Array.Empty<AutomationElement>());

                        sb.AppendLine("Sample Cell(0,0) Headers:");
                        sb.AppendLine($"  RowHeaderItems: {(rh == null ? 0 : rh.Length)}");
                        if (rh != null) foreach (var h in rh.Take(10)) sb.AppendLine($"    - '{Safe(() => h.Current.Name, "")}'");
                        if (rh != null && rh.Length > 10) sb.AppendLine("    ...");

                        sb.AppendLine($"  ColumnHeaderItems: {(ch == null ? 0 : ch.Length)}");
                        if (ch != null) foreach (var h in ch.Take(10)) sb.AppendLine($"    - '{Safe(() => h.Current.Name, "")}'");
                        if (ch != null && ch.Length > 10) sb.AppendLine("    ...");
                    }
                }

                sb.AppendLine();
            }

            if (!(Supports(el, GridPattern.Pattern) || Supports(el, TablePattern.Pattern)))
            {
                sb.AppendLine("No GridPattern/TablePattern on this element.");
            }
        }

        // -------------------------
        // Common patterns detail
        // -------------------------
        private static void DumpCommonPatterns(StringBuilder sb, AutomationElement el)
        {
            // ValuePattern
            if (el.TryGetCurrentPattern(ValuePattern.Pattern, out var vpObj) && vpObj is ValuePattern vp)
            {
                sb.AppendLine("[ValuePattern]");
                sb.AppendLine($"Value='{Safe(() => vp.Current.Value, "")}', IsReadOnly={Safe(() => vp.Current.IsReadOnly, false)}");
                sb.AppendLine();
            }

            // TextPattern
            if (el.TryGetCurrentPattern(TextPattern.Pattern, out var tpObj) && tpObj is TextPattern tp)
            {
                sb.AppendLine("[TextPattern]");
                var doc = Safe(() => tp.DocumentRange, null);
                if (doc != null)
                {
                    string text = Safe(() => doc.GetText(5000), "");
                    sb.AppendLine($"Text(<=5000): {text}");
                }
                sb.AppendLine();
            }

            // SelectionPattern
            if (el.TryGetCurrentPattern(SelectionPattern.Pattern, out var spObj) && spObj is SelectionPattern sp)
            {
                sb.AppendLine("[SelectionPattern]");
                sb.AppendLine($"CanSelectMultiple={Safe(() => sp.Current.CanSelectMultiple, false)}, IsSelectionRequired={Safe(() => sp.Current.IsSelectionRequired, false)}");
                var sel = Safe(() => sp.Current.GetSelection(), Array.Empty<AutomationElement>());
                sb.AppendLine($"Selected Count={sel?.Length ?? 0}");
                if (sel != null)
                {
                    foreach (var s in sel.Take(30))
                        sb.AppendLine($"  - '{Safe(() => s.Current.Name, "")}', Type={Safe(() => s.Current.ControlType?.ProgrammaticName, "")}");
                    if (sel.Length > 30) sb.AppendLine("  ...");
                }
                sb.AppendLine();
            }

            // ScrollPattern
            if (el.TryGetCurrentPattern(ScrollPattern.Pattern, out var scObj) && scObj is ScrollPattern sc)
            {
                sb.AppendLine("[ScrollPattern]");
                sb.AppendLine($"Horiz={Safe(() => sc.Current.HorizontallyScrollable, false)} ({Safe(() => sc.Current.HorizontalScrollPercent, 0.0)}%), View={Safe(() => sc.Current.HorizontalViewSize, 0.0)}");
                sb.AppendLine($"Vert ={Safe(() => sc.Current.VerticallyScrollable, false)} ({Safe(() => sc.Current.VerticalScrollPercent, 0.0)}%), View={Safe(() => sc.Current.VerticalViewSize, 0.0)}");
                sb.AppendLine();
            }

            // InvokePattern
            if (el.TryGetCurrentPattern(InvokePattern.Pattern, out var ivObj) && ivObj is InvokePattern iv)
            {
                sb.AppendLine("[InvokePattern] supported");
                sb.AppendLine();
            }

            // ExpandCollapsePattern
            if (el.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var ecObj) && ecObj is ExpandCollapsePattern ec)
            {
                sb.AppendLine("[ExpandCollapsePattern]");
                sb.AppendLine($"State={Safe(() => ec.Current.ExpandCollapseState.ToString(), "Unknown")}");
                sb.AppendLine();
            }
        }

        // -------------------------
        // Helpers
        // -------------------------
        private static bool Supports(AutomationElement el, AutomationPattern p)
        {
            try { return el.TryGetCurrentPattern(p, out var o) && o != null; }
            catch { return false; }
        }

        private static T Safe<T>(Func<T> getter, T fallback)
        {
            try { return getter(); } catch { return fallback; }
        }

        private static string TryGetValueQuick(AutomationElement el)
        {
            // Cell 常见取值优先顺序：ValuePattern -> TextPattern(短) -> Name
            try
            {
                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out var vpObj) && vpObj is ValuePattern vp)
                    return vp.Current.Value ?? "";
            }
            catch { /* ignore */ }

            try
            {
                if (el.TryGetCurrentPattern(TextPattern.Pattern, out var tpObj) && tpObj is TextPattern tp)
                    return tp.DocumentRange?.GetText(200) ?? "";
            }
            catch { /* ignore */ }

            try { return el.Current.Name ?? ""; } catch { return ""; }
        }


        public static Dictionary<string, string> DumpAllElements(AutomationElementCollection all)
        {
            var dumpNodes = new Dictionary<string, string>();
            int nodeIndex = 0;

            AddDumpNode(dumpNodes, ref nodeIndex, $"Total elements: {all?.Count ?? 0}");
            AddDumpNode(dumpNodes, ref nodeIndex, "============================================");

            if (all == null) return dumpNodes;

            for (int i = 0; i < all.Count; i++)
            {
                AddDumpNode(dumpNodes, ref nodeIndex, $"\n===== Element {i} =====");
                DumpElement(all[i], dumpNodes, ref nodeIndex);
            }

            return dumpNodes;
        }

        private static void DumpElement(AutomationElement el, Dictionary<string, string> dumpNodes, ref int nodeIndex)
        {
            if (el == null)
            {
                AddDumpNode(dumpNodes, ref nodeIndex, "Element is null");
                return;
            }

            DumpBasicProperties(el, dumpNodes, ref nodeIndex);
            DumpAllProperties(el, dumpNodes, ref nodeIndex);
            DumpSupportedPatterns(el, dumpNodes, ref nodeIndex);
        }

        // -----------------------------------------
        // 1️⃣ 常用属性
        // -----------------------------------------
        private static void DumpBasicProperties(AutomationElement el, Dictionary<string, string> dumpNodes, ref int nodeIndex)
        {
            try
            {
                var c = el.Current;

                AddDumpNode(dumpNodes, ref nodeIndex, "---- Basic Info ----");
                AddDumpNode(dumpNodes, ref nodeIndex, $"Name: {c.Name}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"AutomationId: {c.AutomationId}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"ClassName: {c.ClassName}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"ControlType: {c.ControlType?.ProgrammaticName}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"FrameworkId: {c.FrameworkId}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"ProcessId: {c.ProcessId}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"IsEnabled: {c.IsEnabled}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"IsOffscreen: {c.IsOffscreen}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"IsContentElement: {c.IsContentElement}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"IsControlElement: {c.IsControlElement}");
                AddDumpNode(dumpNodes, ref nodeIndex, $"BoundingRectangle: {c.BoundingRectangle}");
            }
            catch (Exception ex)
            {
                AddDumpNode(dumpNodes, ref nodeIndex, $"Basic property error: {ex.Message}");
            }
        }

        // -----------------------------------------
        // 2️⃣ 打印所有 AutomationProperty
        // -----------------------------------------
        private static void DumpAllProperties(AutomationElement el, Dictionary<string, string> dumpNodes, ref int nodeIndex)
        {
            AddDumpNode(dumpNodes, ref nodeIndex, "---- All Properties ----");

            for (int id = 0; id < 10000; id++)
            {
                AutomationProperty prop = null;

                try { prop = AutomationProperty.LookupById(id); }
                catch { }

                if (prop == null)
                    continue;

                try
                {
                    var value = el.GetCurrentPropertyValue(prop, true);

                    if (value == AutomationElement.NotSupported)
                        continue;

                    string display;

                    if (value is ControlType ct)
                        display = ct.ProgrammaticName;
                    else
                        display = value?.ToString() ?? "<null>";

                    AddDumpNode(dumpNodes, ref nodeIndex, $"{prop.ProgrammaticName}: {display}");
                }
                catch
                {
                    // ignore
                }
            }
        }

        // -----------------------------------------
        // 3️⃣ 打印所有支持的 Pattern
        // -----------------------------------------
        private static void DumpSupportedPatterns(AutomationElement el, Dictionary<string, string> dumpNodes, ref int nodeIndex)
        {
            AddDumpNode(dumpNodes, ref nodeIndex, "---- Supported Patterns ----");

            for (int id = 0; id < 10000; id++)
            {
                AutomationPattern pattern = null;

                try { pattern = AutomationPattern.LookupById(id); }
                catch { }

                if (pattern == null)
                    continue;

                try
                {
                    if (el.TryGetCurrentPattern(pattern, out var obj) && obj != null)
                    {
                        AddDumpNode(dumpNodes, ref nodeIndex, $"Pattern: {pattern.ProgrammaticName}");
                        DumpPatternDetails(pattern, obj, dumpNodes, ref nodeIndex);
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        // -----------------------------------------
        // 4️⃣ Pattern 内部细节
        // -----------------------------------------
        private static void DumpPatternDetails(AutomationPattern pattern, object patternObj, Dictionary<string, string> dumpNodes, ref int nodeIndex)
        {
            try
            {
                switch (pattern.ProgrammaticName)
                {
                    case "ValuePatternIdentifiers.Pattern":
                        var vp = (ValuePattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   Value: {vp.Current.Value}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   IsReadOnly: {vp.Current.IsReadOnly}");
                        break;

                    case "GridPatternIdentifiers.Pattern":
                        var gp = (GridPattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   RowCount: {gp.Current.RowCount}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   ColumnCount: {gp.Current.ColumnCount}");
                        break;

                    case "GridItemPatternIdentifiers.Pattern":
                        var gip = (GridItemPattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   Row: {gip.Current.Row}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   Column: {gip.Current.Column}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   RowSpan: {gip.Current.RowSpan}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   ColumnSpan: {gip.Current.ColumnSpan}");
                        break;

                    case "TablePatternIdentifiers.Pattern":
                        var tp = (TablePattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   RowOrColumnMajor: {tp.Current.RowOrColumnMajor}");
                        break;

                    case "SelectionPatternIdentifiers.Pattern":
                        var sp = (SelectionPattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   CanSelectMultiple: {sp.Current.CanSelectMultiple}");
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   IsSelectionRequired: {sp.Current.IsSelectionRequired}");
                        break;

                    case "ExpandCollapsePatternIdentifiers.Pattern":
                        var ec = (ExpandCollapsePattern)patternObj;
                        AddDumpNode(dumpNodes, ref nodeIndex, $"   State: {ec.Current.ExpandCollapseState}");
                        break;

                    default:
                        AddDumpNode(dumpNodes, ref nodeIndex, "   (pattern details not expanded)");
                        break;
                }
            }
            catch
            {
                // ignore pattern detail errors
            }
        }

        private static void AddDumpNode(Dictionary<string, string> dumpNodes, ref int nodeIndex, string value)
        {
            dumpNodes[$"Node_{nodeIndex:D6}"] = value ?? string.Empty;
            nodeIndex++;
        }
    }
}
