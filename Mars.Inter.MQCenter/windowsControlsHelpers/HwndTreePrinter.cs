using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public static class HwndTreePrinter
    {
        /// <summary>
        /// 将 HwndNode 树打印到指定文件（UTF-8），带树枝线。可选输出 Acc 信息。
        /// </summary>
        public static void PrintTreeToFile(MarsHwndAccBuilder.HwndNode root, string filePath, bool includeAccInfo = true)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 不带 BOM 的 UTF-8
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                // 文件头
                writer.WriteLine($"# HWND Tree dump  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                // 打印根
                WriteNode(writer, root, prefix: "", isLast: true, includeAccInfo: includeAccInfo);
            }
        }

        // 递归打印（树深通常很有限，安全）
        private static void WriteNode(StreamWriter w, MarsHwndAccBuilder.HwndNode n, string prefix, bool isLast, bool includeAccInfo)
        {
            string branch = prefix.Length == 0 ? "" : (isLast ? "└─" : "├─");
            w.WriteLine($"{prefix}{branch}HWND=0x{n.Hwnd.ToInt64():X}  Class='{n.ClassName}'  Parent=0x{n.ParentHwnd.ToInt64():X}");

            if (includeAccInfo && n.Acc != null)
            {
                string subPrefix = prefix + (prefix.Length == 0 ? "" : (isLast ? "  " : "│ "));
                string text = OneLine(n.Acc.Text, 120);
                string value = OneLine(n.Acc.Value, 120);
                string atxt = OneLine(n.Acc.AttachText, 120);

                w.WriteLine($"{subPrefix}  Acc: Role={n.Acc.RoleName}, Text='{text}', Value='{value}', AttachText='{atxt}', Host=0x{n.Acc.HostHwnd.ToInt64():X}");
            }

            // 子节点
            var kids = n.Children ?? new List<MarsHwndAccBuilder.HwndNode>();
            for (int i = 0; i < kids.Count; i++)
            {
                bool last = (i == kids.Count - 1);
                string nextPrefix = prefix + (prefix.Length == 0 ? "" : (isLast ? "  " : "│ "));
                WriteNode(w, kids[i], nextPrefix, last, includeAccInfo);
            }
        }

        // 单行清洗 + 截断，避免把长文本/多行撑坏格式
        private static string OneLine(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            if (s.Length > max) s = s.Substring(0, max) + "…";
            return s;
        }
    }
}
