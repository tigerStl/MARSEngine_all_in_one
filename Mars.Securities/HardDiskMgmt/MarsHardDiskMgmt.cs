using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Securities.HardDiskMgmt
{
	public class HardDrive
	{
		private string model = null;
		private string type = null;
		private string serialNo = null;

		public string Model
		{
			get { return model; }
			set { model = value; }
		}

		public string Type
		{
			get { return type; }
			set { type = value; }
		}

		public string SerialNo
		{
			get { return serialNo; }
			set { serialNo = value; }
		}
	}
	public class MarsHardDiskMgmt
    {
		private static ArrayList harsDiskInfo = null;

		public static string GetHostDiskSerialNo()
        {
			var pth = typeof(MarsHardDiskMgmt).Assembly.Location;
			var d = System.IO.Path.GetPathRoot(pth);
            try
            {
				foreach (var itm in HarsDiskInfo)
				{
					if (itm==null) continue ;
					if (!(itm is HardDrive)) continue;
					HardDrive hd = (HardDrive)itm;
					
				}
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static ArrayList HarsDiskInfo {
			get
            {
				int i = 0;
				if (harsDiskInfo == null)
                {
					harsDiskInfo = new ArrayList();
					ManagementClass mc = new ManagementClass("Win32_DiskDrive");
					ManagementObjectSearcher searcher = new
						ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
					var lst = searcher.Get();
					Console.WriteLine($"{lst.Count}");
					foreach (ManagementObject wmi_HD in searcher.Get()) { 
						HardDrive hd = new HardDrive();
						hd.Model = wmi_HD["Model"].ToString();
						hd.Type = wmi_HD["InterfaceType"].ToString();
						hd.SerialNo = wmi_HD["SerialNumber"].ToString();
						HarsDiskInfo.Add(hd);
						
					}

					//searcher = new
					//	ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");

					//i = 0;
					//foreach (ManagementObject wmi_HD in searcher.Get())
					//{

					//	// get the hard drive from collection
					//	// using index
					//	HardDrive hd = new HardDrive();
					//	//HardDrive hd = (HardDrive)hdCollection[i];

					//	// get the hardware serial no.
					//	if (wmi_HD["SerialNumber"] == null)
					//		hd.SerialNo = "None";
					//	else
					//		hd.SerialNo = wmi_HD["SerialNumber"].ToString();

					//	++i;
					//}
				}
				return harsDiskInfo;
            }
		}
		
	}
}
