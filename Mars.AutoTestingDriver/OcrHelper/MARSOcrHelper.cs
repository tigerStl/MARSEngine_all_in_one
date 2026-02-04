using Route2NSEx.src.Marquis.systemUtil;
using System;
using Tesseract;

namespace Mars.AutoTestingDriver.OcrHelper
{
    public class MARSOcrHelper
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MARSOcrHelper));

        private static string _tessDataPath;
        private static void InitDataPath()
        {
            string strPth = typeof(MARSOcrHelper).Assembly.Location;
            strPth = System.IO.Path.GetDirectoryName(strPth);
            strPth = System.IO.Path.Combine(strPth, "config/ocrData");
            _tessDataPath = strPth;
        }
        public MARSOcrHelper()
        {
            InitDataPath();
        }

        public string ConvertBmpToText(string bmpFilePath, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            logger.logBegin("ConvertBmpToText", bmpFilePath);
            try
            {
                using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(bmpFilePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            isOk = true;
                            return page.GetText();
                        }
                    }
                }
                
            }
            catch (Exception e)
            {
                strAdv = $"Please Make sure that File|{bmpFilePath}| exists and can be OCR";
                strError = $"OCR occurs error |{e.Message}|{strAdv}";
                strStack = e.StackTrace;
                isOk = false;
                logger.Error("ConvertBmpToText", strError);
                return string.Empty;
            }
        }
    }
}
