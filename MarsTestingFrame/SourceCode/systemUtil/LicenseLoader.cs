using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Globalization;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace MarsTestFrame.SourceCode.systemUtil
{
    public interface ILicenseMgr
    {
        bool isAvailable();
        string GetLicenseInfo();

    }



    [Serializable()]
    public class TestFrameLicense : ILicenseMgr
    {
        private static MLogger logger = MLogger.GetLogger(typeof(TestFrameLicense));

        public int mode { get; set; }
        private const string cnst_defaultFilename = "MarsTestFrame.lic";
        private int LicenseYearInfo = -0x4A; /**YEAR * 37 **/
        private int LicenseMonthInfo = -0x26;/**Month * 19 **/
        private int LicenseDateInfo = -0x86;/** day * 67 **/

        private string hardDiskNumber = "";

        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(
            string PathName,
            StringBuilder VolumeNameBuffer,
            UInt32 VolumeNameSize,
            ref UInt32 VolumeSerialNumber,
            ref UInt32 MaximumComponentLength,
            ref UInt32 FileSystemFlags,
            StringBuilder FileSystemNameBuffer,
            UInt32 FileSystemNameSize);
        public double GetDistance()
        {
            DateTime objInfo = new DateTime(LicenseYearInfo / 0x25, LicenseMonthInfo / 0x13, LicenseDateInfo / 0x43);
            DateTime dt = DateTime.Now;
            return (objInfo - dt).TotalDays;
        }
        public bool isAvailable()
        {
            try
            {
                DateTime dt = DateTime.Now;
                logger.Info("isAvailable", string.Format("date now:[{0}] -[{1}]-[{2}]-[{3}]", dt,
                    LicenseYearInfo / 0x25, LicenseMonthInfo / 0x13, LicenseDateInfo / 0x43));
                DateTime objInfo = new DateTime(LicenseYearInfo / 0x25, LicenseMonthInfo / 0x13, LicenseDateInfo / 0x43);

                /** get hard disk **/
                bool isDisk = true;
                if (!string.IsNullOrEmpty(this.hardDiskNumber))
                {
                    string strCurrentDisk = GetHardDiskInfo();
                    if (string.Compare(strCurrentDisk, hardDiskNumber.Replace("-", ""), true) == 0)
                        isDisk = true;
                    else
                        isDisk = false;
                }

                return (objInfo >= dt) && isDisk;
            }
            catch (Exception e)
            {
                logger.Error("isAvailable", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }

        }

        private static string GetHardDiskInfo()
        {

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            System.IO.DriveInfo dInfo = new System.IO.DriveInfo("C");  //You must put the drive letter here.
            uint serial_number = 0;
            uint max_component_length = 0;
            StringBuilder sb_volume_name = new StringBuilder(256);
            UInt32 file_system_flags = new UInt32();
            StringBuilder sb_file_system_name = new StringBuilder(256);
            if (GetVolumeInformation("c:\\", sb_volume_name,
                (UInt32)sb_volume_name.Capacity, ref serial_number, ref max_component_length,
                ref file_system_flags, sb_file_system_name, (UInt32)sb_file_system_name.Capacity) == 0)
            {
                logger.Error("GetHardDiskInfo", "Can't get Volumn information");
                return "";
            }
            else
            {
                return string.Format("{0:X}", serial_number);
            }
        }

        public string GetLicenseInfo()
        {
            return null;
        }

        public static ILicenseMgr LoadLicense()
        {
            string strFilePath = GetFilePath();
            BinaryFormatter objBF = new BinaryFormatter();
            objBF.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            ILicenseMgr objIResult = null;
            try
            {
                string strLicReversted = "";

                FileStream objFS = File.Open(strFilePath, FileMode.Open);
                using (StreamReader objStremReader = new StreamReader(objFS))
                {
                    strLicReversted = objStremReader.ReadToEnd();
                }

                if ((strLicReversted.Length % 2) != 0) throw new Exception("Wrong License File!!");
                int iCnt = strLicReversted.Length / 2;
                byte[] arrFBRevsersted = new byte[iCnt];

                for (int i = 0; i < iCnt; i++)
                {
                    byte.TryParse(strLicReversted.Substring(i * 2, 2), NumberStyles.HexNumber, null, out arrFBRevsersted[iCnt - i - 1]);
                }

                MemoryStream objMemory = new MemoryStream(arrFBRevsersted);
                object objResult = objBF.Deserialize(objMemory);

                //object objResult = objBF.Deserialize(objFS);
                if (objResult is TestFrameLicense)
                {
                    objIResult = (TestFrameLicense)objResult;
                    logger.Info("######", string.Format("[{0}-{1}-{2}]", ((TestFrameLicense)objResult).LicenseYearInfo, ((TestFrameLicense)objResult).LicenseMonthInfo, ((TestFrameLicense)objResult).LicenseDateInfo));
                    return objIResult;
                }
                logger.Error("######", "no file find....");
                return new TestFrameLicense();
            }
            catch (Exception e)
            {
                logger.Error("######", string.Format("Exception :[{0}]", e.Message), e);
                return new TestFrameLicense();
            }
            finally
            {

            }


        }

        public static void Save(string strlastDate, string strHardDiskNum = null)
        {
            TestFrameLicense objResutl = new TestFrameLicense();
            string strYear = strlastDate.Substring(0, 4);
            string strMonth = strlastDate.Substring(4, 2);
            string strDte = strlastDate.Substring(6, 2);
            objResutl.LicenseYearInfo = int.Parse(strYear) * 37;
            objResutl.LicenseMonthInfo = int.Parse(strMonth) * 0x13;
            objResutl.LicenseDateInfo = int.Parse(strDte) * 67;
            objResutl.hardDiskNumber = strHardDiskNum;

            MemoryStream ms = new MemoryStream();
            BinaryFormatter objBF = new BinaryFormatter();
            objBF.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            objBF.Serialize(ms, objResutl);
            //GetHardDiskInfo();
            StringBuilder sb = new StringBuilder();
            ms.Seek(0, SeekOrigin.Begin);
            byte[] arrData = ms.GetBuffer();
            byte[] arrDes = new byte[ms.Length];
            System.Buffer.BlockCopy(arrData, 0, arrDes, 0, arrDes.Length);
            string s = "";
            for (int i = 0; i < arrDes.Length; i++)
            {
                if (i == 0)
                {
                    s = string.Format("{0:X2}", arrDes[i]);
                }
                else
                {
                    s = string.Format("{1:X2}{0}", s, arrDes[i]);
                }
            }
            string strResult = s;
            //string strResult = Encoding.ASCII.GetString(arrData);
            //byte[] arrR = Encoding.ASCII.GetBytes(strResult) ;
            string strPath = typeof(TestFrameLicense).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);

            //FileStream objFS = new FileStream("C:\\automationTest\\Automation Workbooks\\dlls\\" + cnst_defaultFilename, FileMode.Create);
            FileStream objFS = new FileStream(System.IO.Path.Combine(strPath, cnst_defaultFilename), FileMode.Create);
            objFS.Seek(0, SeekOrigin.Begin);

            byte[] bytes = Encoding.ASCII.GetBytes(strResult);
            //System.Buffer.BlockCopy(strResult.ToCharArray(), 0, bytes, 0, bytes.Length);

            objFS.Write(bytes, 0, bytes.Length);
            objFS.Flush();
            objFS.Close();
            objFS = null;
            /*
            FileStream objFS = new FileStream("C:\\automationTest\\Automation Workbooks\\" + cnst_defaultFilename,FileMode.CreateNew);
            BinaryFormatter objBF = new BinaryFormatter();
            objBF.Serialize(objFS, objResutl);
            objBF = null;
            objFS.Flush();
             
            objFS = null;
             * * */
        }

        private static string GetFilePath()
        {
            string stmp = Assembly.GetExecutingAssembly().Location;

            stmp = string.Format("{0}\\{1}", stmp.Substring(0, stmp.LastIndexOf('\\')), cnst_defaultFilename);
            return stmp;
        }
    }

    internal class LicenseLoader
    {

    }
}
