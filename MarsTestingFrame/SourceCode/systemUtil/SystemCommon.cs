using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.performance.systemInfo
{
    public class SystemCommon
    {
        public static string GetFriendSystemName()
        {
            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            return name != null ? name.ToString() : "Unknown";
        }

        public static string GetTotalPhysicalMemory()
        {
            ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_ComputerSystem");
            var totalNumber = (from x in new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_ComputerSystem").Get().Cast<ManagementObject>()
                               select x.GetPropertyValue("TotalPhysicalMemory")).FirstOrDefault();
            return totalNumber==null?"Unknown":totalNumber.ToString();
        }
    }

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    //private class MEMORYSTATUSEX
    //{
    //    public uint dwLength;
    //    public uint dwMemoryLoad;
    //    public ulong ullTotalPhys;
    //    public ulong ullAvailPhys;
    //    public ulong ullTotalPageFile;
    //    public ulong ullAvailPageFile;
    //    public ulong ullTotalVirtual;
    //    public ulong ullAvailVirtual;
    //    public ulong ullAvailExtendedVirtual;
    //    public MEMORYSTATUSEX()
    //    {
    //        this.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
    //    }
    //}
}
