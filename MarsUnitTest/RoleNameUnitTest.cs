using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Accessibility;

namespace MarsUnitTest
{
    [TestClass]
    public class RoleNameUnitTest
    {
        // oleacc.dll P/Invoke
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(
            IntPtr hwnd,
            uint dwId,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] ref IAccessible ppvObject);

        [DllImport("oleacc.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetRoleText(int lRole, StringBuilder lpszRole, uint cchRoleMax);

        private const uint OBJID_WINDOW = 0x00000000;
        private static Guid IID_IAccessible =
            new Guid("618736e0-3c3d-11cf-810c-00aa00389b71");

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [TestMethod]
        public void DumpRoleNameAndIdToFile()
        {
            // 例子：找到一个“记事本”窗口（需要先打开 Notepad）
            IntPtr hwnd = FindWindow("Notepad", null);
            Assert.AreNotEqual(IntPtr.Zero, hwnd, "找不到 Notepad 窗口，请先运行记事本");

            IAccessible acc = null;
            int hr = AccessibleObjectFromWindow(hwnd, OBJID_WINDOW, ref IID_IAccessible, ref acc);
            Assert.IsTrue(hr >= 0 && acc != null, "AccessibleObjectFromWindow 失败");

            // 获取角色
            object roleObj = acc.get_accRole(0);
            int roleId = (roleObj is int id) ? id : 0;
            string roleName = GetRoleName(roleId);

            // 写到文件
            string path = @"C:\temp\rolename.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, $"RoleId={roleId}, RoleName={roleName}");

            Console.WriteLine($"已保存: {path}");
        }

        private static string GetRoleName(int roleId)
        {
            var sb = new StringBuilder(256);
            if (GetRoleText(roleId, sb, (uint)sb.Capacity) > 0)
                return sb.ToString();
            return $"Unknown Role ({roleId})";
        }

        [TestMethod]
        public void TestGetRoleName()
        {
            string path = @"C:\temp\rolename.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                for (int roleId = 0; roleId <= 0x100; roleId++) // 遍历 0–256 范围
                {
                    string name = GetRoleName(roleId);
                    if (!string.IsNullOrEmpty(name))
                    {
                        string line = $"RoleId=0x{roleId:X2}, RoleName={name}";
                        Console.WriteLine(line);
                        writer.WriteLine(line);
                    }
                }
            }

            Console.WriteLine($"已保存所有 RoleId → RoleName 到 {path}");
        }
    }
}
