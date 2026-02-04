#if _VEDIO_TIGER_
using Microsoft.Expression.Encoder;
using Microsoft.Expression.Encoder.Profiles;
#endif

namespace TestFrameMonitor.source.serializeration
{
#if _VEDIO_TIGER_
    /**     
        
    */

    public class MarsVedioRptMgr
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(MarsVedioRptMgr));
        public static void Test()
        {
            MarsXmlVedioIndex objVedioIndex = new MarsXmlVedioIndex();
            objVedioIndex.InitializationTest();
            objVedioIndex.TestProjectName = "T_Logo.xls";
            objVedioIndex.TestSuiteName = "F_LogOn";
            objVedioIndex.CurrentLoop = 1;
            objVedioIndex.ProjectRelyId = "T1";

            TestStep4Services objStep = new TestStep4Services();
            objStep.RunID = 100;
            objStep.Keyword = "FillEdit";
            objStep.ObjectName = "MainWindowEdit";
            objStep.Row_Column = "";
            objStep.Value = "qauser";
            
            Thread.Sleep(200);
            objVedioIndex.AddNewStepInfo(string.Format("{0}", objStep.RunID), objStep.Keyword, objStep.QuickAccess, objStep.ObjectName, objStep.Row_Column, objStep.Value);
            Thread.Sleep(1000);
            objVedioIndex.AttachDataToLatestStep("", true, "Done");

            objStep = new TestStep4Services();
            objStep.RunID = 90;
            objStep.Keyword = "FillEdit";
            objStep.ObjectName = "CCYEdit";
            objStep.Row_Column = "";
            objStep.Value = "USD";
            Thread.Sleep(400);
            objVedioIndex.AddNewStepInfo(string.Format("{0}", objStep.RunID), objStep.Keyword, objStep.QuickAccess, objStep.ObjectName, objStep.Row_Column, objStep.Value);
            Thread.Sleep(3000);
            objVedioIndex.AttachDataToLatestStep("", false, "Unsupportted Object method");

            MarsXmlVedioIndex.Export(objVedioIndex);


            MarsXmlVedioIndex objIndex = MarsXmlVedioIndex.Import("T1", "T_Logo.xls", "F_LogOn",1);
            if (objIndex==null)
            {
                Console.WriteLine("NO object returns");

            }
        }

        private static MarsXmlVedioIndex g_objXmlVedioIndex = null;
        public static void Initialization()
        {
            //if (g_objXmlVedioIndex ==null)
            //{
                g_objXmlVedioIndex = new MarsXmlVedioIndex();
            //}
            g_objXmlVedioIndex.InitializationTest();
        }
        public static void AddOneStepInfo(string strId, string strKey, string strObject, string strHappy, string strRC, string strData)
        {
            if (g_objXmlVedioIndex == null) return;
            g_objXmlVedioIndex.AddNewStepInfo(strId, strKey, strObject,strHappy, strRC, strData);
        }

        public static MarsTigerXmlReportItem GetLastStepInfo()
        {
            if (g_objXmlVedioIndex == null) return null;
            if (g_objXmlVedioIndex.TestSteps == null) return null;
            return g_objXmlVedioIndex.TestSteps.Count == 0 ? null : g_objXmlVedioIndex.TestSteps[g_objXmlVedioIndex.TestSteps.Count - 1];
        }

        public static MarsXmlVedioIndex GetVedioIndex()
        {
            return g_objXmlVedioIndex;
        }


        public static void AttachDataToLast(string strOutData, bool isResult, string strMsg)
        {
            if (g_objXmlVedioIndex == null) return;
            g_objXmlVedioIndex.AttachDataToLatestStep(strOutData, isResult, strMsg);
        }

        public static bool ExportsAll(ref string strError)
        {
            if (g_objXmlVedioIndex == null) return true ;
            try
            {
                MarsXmlVedioIndex.Export(g_objXmlVedioIndex);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ExportsAll",string.Format("Exceptions:[{}],when export data to xml files",e.Message),e);
                return false;
            }
        }

        internal static bool ConvertVedioWith(string strFileName, ref string strError)
        {
            Logger.Info("ConvertVedioWith", string.Format("Convert file to mp4:[{0}]",strFileName));
           
#if No_VideoConvert_Demo
            return true;
#else
            MediaItem objItm = new MediaItem(strFileName);
            try
            {
                MarsXmlVedioIndex objVideoInfo = g_objXmlVedioIndex;
                g_objXmlVedioIndex = null;
                foreach (MarsTigerXmlReportItem objCaption in objVideoInfo.TestSteps)
                {
                    ScriptCommand oneCaption = new ScriptCommand();

                    oneCaption.Type = "caption";
                    oneCaption.Time = objCaption.StartTime - objVideoInfo.GetStartTime();
                    oneCaption.Command = objCaption.GetCommandForCommandScript();
                    objItm.ScriptCommands.Add(oneCaption);
                }
                string strActualVedioFilePath = "";
                string strTargetFilePaht = Path.GetDirectoryName(strFileName);
                using (Job objJob = new Job())
                {
                    objItm.OutputFormat = new WindowsMediaOutputFormat();

                    ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile();
                    ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.Size = new System.Drawing.Size(1024, 768);
                    ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.AspectRatio = new System.Windows.Size(16, 9);
                    ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.Bitrate = new ConstantBitrate(1000);

                    ////((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.NumberOfEncoderThreads = 4;
                    objItm.OutputFileName = "{OriginalFilename}.{DefaultExtension}";//                    string.Format("{0}.wmv", strFileName);

                    objJob.MediaItems.Add(objItm);
                    objJob.OutputDirectory = strTargetFilePaht;
                    objJob.Encode();
                    strActualVedioFilePath = objJob.ActualOutputDirectory;
                }

                /// Move file to Up folder
                /// 
                MoveFilesToUpFolder(strActualVedioFilePath,".wmv", strTargetFilePaht);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ConvertVedioWith", string.Format("Exception:[{0}] when convert xesc file to Wmv. ", e.Message),e);
                return false;
            }
            finally { objItm = null; }
#endif
        }

        private static bool MoveFilesToUpFolder(string strPathToMove,string strSux,string strTargetFilePath)
        {
            string[] arrFiles = Directory.GetFiles(strPathToMove, "*"+strSux);
            try
            {
                foreach (string strOneFile in arrFiles)
                {
                    string strFileName = Path.GetFileNameWithoutExtension(strOneFile);
                    string strTargetFileName = string.Format("{0}\\{1}{2}", strTargetFilePath, strFileName, strSux);
                    if (File.Exists(strTargetFileName))
                    {
                        File.Delete(strTargetFileName);
                    }
                    File.Move(strOneFile,string.Format("{0}\\{1}{2}",strTargetFilePath, strFileName, strSux));
                }
                /// delete directory
                /// 
                Directory.Delete(strPathToMove);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("MoveFilesToUpFolder", string.Format("Exceptions:[{0}],when move files from [{1}], with [{2}]",e.Message,strPathToMove, strSux),e);
                return false;
                
            }
            
        }

        internal static void UpdateTestProject(string currentTestSuiteName)
        {
            if (g_objXmlVedioIndex == null)
                return;
            g_objXmlVedioIndex.TestProjectName = currentTestSuiteName;
        }

        internal static void UpdateTestSuite(string currentTestCaseName)
        {
            if (g_objXmlVedioIndex == null)
                return;
            g_objXmlVedioIndex.TestSuiteName = currentTestCaseName;
        }

        internal static void UpdateTestCurrentRelyId(string currentRelyId)
        {
            if (g_objXmlVedioIndex == null) return;
            g_objXmlVedioIndex.ProjectRelyId = currentRelyId;
        }

        internal static void UpdateCurrentLoopId(int testCurrentLoopId)
        {
            if (g_objXmlVedioIndex == null) return;
            g_objXmlVedioIndex.CurrentLoop = testCurrentLoopId;
        }
    }

    //public static class XmlHelper
    //{
    //    private static void XmlSerializeInternal(Stream stream, object o, Encoding encoding)
    //    {
    //        if (o == null)
    //            throw new ArgumentNullException("o");
    //        if (encoding == null)
    //            throw new ArgumentNullException("encoding");

    //        XmlSerializer serializer = new XmlSerializer(o.GetType());

    //        XmlWriterSettings settings = new XmlWriterSettings();
    //        settings.Indent = true;
    //        settings.NewLineChars = "\r\n";
    //        settings.Encoding = encoding;
    //        settings.IndentChars = "    ";

    //        using (XmlWriter writer = XmlWriter.Create(stream, settings))
    //        {
    //            serializer.Serialize(writer, o);
    //            writer.Close();
    //        }
    //    }

    //    /// <summary>
    //    /// 将一个对象序列化为XML字符串
    //    /// </summary>
    //    /// <param name="o">要序列化的对象</param>
    //    /// <param name="encoding">编码方式</param>
    //    /// <returns>序列化产生的XML字符串</returns>
    //    public static string XmlSerialize(object o, Encoding encoding)
    //    {
    //        using (MemoryStream stream = new MemoryStream())
    //        {
    //            XmlSerializeInternal(stream, o, encoding);

    //            stream.Position = 0;
    //            using (StreamReader reader = new StreamReader(stream, encoding))
    //            {
    //                return reader.ReadToEnd();
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 将一个对象按XML序列化的方式写入到一个文件
    //    /// </summary>
    //    /// <param name="o">要序列化的对象</param>
    //    /// <param name="path">保存文件路径</param>
    //    /// <param name="encoding">编码方式</param>
    //    public static void XmlSerializeToFile(object o, string path, Encoding encoding)
    //    {
    //        if (string.IsNullOrEmpty(path))
    //            return;

    //        using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
    //        {
    //            XmlSerializeInternal(file, o, encoding);
    //        }
    //    }

    //    /// <summary>
    //    /// 从XML字符串中反序列化对象
    //    /// </summary>
    //    /// <typeparam name="T">结果对象类型</typeparam>
    //    /// <param name="s">包含对象的XML字符串</param>
    //    /// <param name="encoding">编码方式</param>
    //    /// <returns>反序列化得到的对象</returns>
    //    public static T XmlDeserialize<T>(string s, Encoding encoding)
    //    {
    //        if (string.IsNullOrEmpty(s))
    //            throw new ArgumentNullException("s");
    //        if (encoding == null)
    //            throw new ArgumentNullException("encoding");

    //        XmlSerializer mySerializer = new XmlSerializer(typeof(T));
    //        using (MemoryStream ms = new MemoryStream(encoding.GetBytes(s)))
    //        {
    //            using (StreamReader sr = new StreamReader(ms, encoding))
    //            {
    //                return (T)mySerializer.Deserialize(sr);
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 读入一个文件，并按XML的方式反序列化对象。
    //    /// </summary>
    //    /// <typeparam name="T">结果对象类型</typeparam>
    //    /// <param name="path">文件路径</param>
    //    /// <param name="encoding">编码方式</param>
    //    /// <returns>反序列化得到的对象</returns>
    //    public static T XmlDeserializeFromFile<T>(string path, Encoding encoding)
    //    {
    //        if (string.IsNullOrEmpty(path))
    //            throw new ArgumentNullException("path");
    //        if (encoding == null)
    //            throw new ArgumentNullException("encoding");

    //        string xml = File.ReadAllText(path, encoding);
    //        return XmlDeserialize<T>(xml, encoding);
    //    }
    //}

#endif
}
