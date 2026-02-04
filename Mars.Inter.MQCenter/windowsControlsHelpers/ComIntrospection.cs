using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public static class ComIntrospection
    {
        // ---- COM 基础接口声明 ----
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("00020400-0000-0000-C000-000000000046")]
        private interface IDispatch
        {
            [PreserveSig] int GetTypeInfoCount(out int pctinfo);
            [PreserveSig] int GetTypeInfo(int iTInfo, int lcid, out ITypeInfo ppTInfo);
            [PreserveSig] int GetIDsOfNames(ref Guid riid, IntPtr rgszNames, uint cNames, int lcid, IntPtr rgDispId);
            [PreserveSig]
            int Invoke(int dispIdMember, ref Guid riid, int lcid, ushort wFlags,
                                     IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("00020401-0000-0000-C000-000000000046")]
        private interface ITypeInfo
        {
            [PreserveSig] int GetTypeAttr(out IntPtr pTypeAttr);
            void GetTypeComp(out object /*ITypeComp*/ ppTComp);
            [PreserveSig] int GetFuncDesc(int index, out IntPtr pFuncDesc);
            [PreserveSig] int GetVarDesc(int index, out IntPtr pVarDesc);
            [PreserveSig] int GetNames(int memid, [Out] string[] rgBstrNames, int cMaxNames, out int pcNames);
            [PreserveSig] int GetRefTypeOfImplType(int index, out int href);
            [PreserveSig] int GetImplTypeFlags(int index, out int pImplTypeFlags);
            [PreserveSig] int GetIDsOfNames([MarshalAs(UnmanagedType.LPWStr)] string rgszNames, int cNames, out int pMemId);
            [PreserveSig] int Invoke();
            [PreserveSig]
            int GetDocumentation(int index,
                [MarshalAs(UnmanagedType.BStr)] out string strName,
                [MarshalAs(UnmanagedType.BStr)] out string strDocString,
                out int dwHelpContext,
                [MarshalAs(UnmanagedType.BStr)] out string strHelpFile);
            [PreserveSig] int GetDllEntry();
            [PreserveSig] int GetRefTypeInfo(int hRef, out ITypeInfo ppTI);
            [PreserveSig] int AddressOfMember();
            [PreserveSig] int CreateInstance();
            [PreserveSig] int GetMops();
            [PreserveSig] int GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);
            [PreserveSig] void ReleaseTypeAttr(IntPtr pTypeAttr);
            [PreserveSig] void ReleaseFuncDesc(IntPtr pFuncDesc);
            [PreserveSig] void ReleaseVarDesc(IntPtr pVarDesc);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("00020402-0000-0000-C000-000000000046")]
        private interface ITypeLib
        {
            [PreserveSig] int GetTypeInfoCount();
            [PreserveSig] int GetTypeInfo(int index, out ITypeInfo ppTI);
            [PreserveSig] int GetTypeInfoType(int index, out int pTKind);
            [PreserveSig] int GetTypeInfoOfGuid(ref Guid guid, out ITypeInfo ppTI);
            [PreserveSig] int GetLibAttr(out IntPtr pTLibAttr);
            [PreserveSig] int GetTypeComp(out object ppTComp);
            [PreserveSig]
            int GetDocumentation(int index,
                [MarshalAs(UnmanagedType.BStr)] out string strName,
                [MarshalAs(UnmanagedType.BStr)] out string strDocString,
                out int dwHelpContext,
                [MarshalAs(UnmanagedType.BStr)] out string strHelpFile);
            [PreserveSig] int IsName();
            [PreserveSig] int FindName();
            [PreserveSig] void ReleaseTLibAttr(IntPtr pTLibAttr);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("B196B283-BAB4-101A-B69C-00AA00341D07")]
        private interface IProvideClassInfo
        {
            void GetClassInfo(out ITypeInfo ppTI);
        }

        // ---- 公共方法 ----

        /// <summary>
        /// 获取 COM 对象“主要接口名”（优先 IDispatch 的类型名；其次 coclass 名；再退到支持的接口名列表）。
        /// </summary>
        public static string GetPrimaryInterfaceName(object com)
        {
            if (com == null) return string.Empty;

            // 1) IDispatch -> ITypeInfo 名（通常是接口名，如 IAccessible / HTMLDocument）
            string name;
            if (TryGetIDispatchTypeName(com, out name))
                return name;

            // 2) IProvideClassInfo -> coclass 名（类名）
            if (TryGetCoclassName(com, out name))
                return name;

            // 3) 遍历可 QI 的托管接口（返回最可能的接口集合）
            var list = GetSupportedManagedInterfaces(com);
            if (list.Count == 1) return list[0];
            if (list.Count > 1) return string.Join(", ", list);

            // 兜底：RCW 类型名 / __ComObject
            return com.GetType().FullName ?? "System.__ComObject";
        }

        // ---- 实现细节 ----

        private static bool TryGetIDispatchTypeName(object com, out string name)
        {
            name = null;
            IntPtr pDisp = IntPtr.Zero;
            try
            {
                // 若不支持 IDispatch，这里会抛异常
                pDisp = Marshal.GetIDispatchForObject(com);
                var disp = (IDispatch)Marshal.GetTypedObjectForIUnknown(pDisp, typeof(IDispatch));
                int hr = disp.GetTypeInfo(0, 0, out ITypeInfo ti);
                if (hr >= 0 && ti != null)
                {
                    // index = -1 取类型自身的名称
                    if (ti.GetDocumentation(-1, out string n, out _, out _, out _) >= 0 && !string.IsNullOrEmpty(n))
                    {
                        name = n;
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                if (pDisp != IntPtr.Zero) Marshal.Release(pDisp);
            }
            return false;
        }

        private static bool TryGetCoclassName(object com, out string name)
        {
            name = null;
            IntPtr pUnk = IntPtr.Zero;
            IntPtr pPci = IntPtr.Zero;
            try
            {
                pUnk = Marshal.GetIUnknownForObject(com);
                var iidPCI = new Guid("B196B283-BAB4-101A-B69C-00AA00341D07"); // IProvideClassInfo
                if (Marshal.QueryInterface(pUnk, ref iidPCI, out pPci) >= 0 && pPci != IntPtr.Zero)
                {
                    var pci = (IProvideClassInfo)Marshal.GetTypedObjectForIUnknown(pPci, typeof(IProvideClassInfo));
                    pci.GetClassInfo(out ITypeInfo ti);
                    if (ti != null && ti.GetDocumentation(-1, out string n, out _, out _, out _) >= 0)
                    {
                        name = n; // coclass 名，例如 "HTMLDocument"
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                if (pPci != IntPtr.Zero) Marshal.Release(pPci);
                if (pUnk != IntPtr.Zero) Marshal.Release(pUnk);
            }
            return false;
        }

        /// <summary>
        /// 返回该 RCW 实际支持的托管接口名（通过 QueryInterface 验证）。
        /// </summary>
        public static List<string> GetSupportedManagedInterfaces(object com)
        {
            var names = new List<string>();
            if (com == null) return names;

            IntPtr pUnk = IntPtr.Zero;
            try
            {
                pUnk = Marshal.GetIUnknownForObject(com);

                // 从 RCW 反射出可能的接口（带 Guid 的 COM 接口）
                var ifaces = com.GetType().GetInterfaces()
                    .Where(t =>
                        t.IsInterface &&
                        t.GetCustomAttributes(typeof(GuidAttribute), false).FirstOrDefault() is GuidAttribute)
                    .Distinct();

                foreach (var t in ifaces)
                {
                    var gid = ((GuidAttribute)t.GetCustomAttributes(typeof(GuidAttribute), false)[0]).Value;
                    var iid = new Guid(gid);
                    IntPtr pIntf;
                    int hr = Marshal.QueryInterface(pUnk, ref iid, out pIntf);
                    if (hr >= 0 && pIntf != IntPtr.Zero)
                    {
                        names.Add(t.FullName ?? t.Name);
                        Marshal.Release(pIntf);
                    }
                }
            }
            catch { }
            finally
            {
                if (pUnk != IntPtr.Zero) Marshal.Release(pUnk);
            }
            return names;
        }
    }
}
