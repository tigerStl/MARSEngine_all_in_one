using Mars.autoTest.report.pdf.Interface;
//using Mars.performance.systemInfo;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.pdf
{
    internal delegate void AdjustPageEnvent(double dRequiredHeight, ref int iPageNumber, ref double dCurPosY, out PdfPage currPage, out XGraphics objGrph) ;
    internal delegate void ExtendRowEnvent(double dRequiredHeight, ref int iPageNumber, ref double dCurPosY, out PdfPage currPage, out XGraphics objGrph);
    public class MarsReportGen
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsReportGen));

        private const float cnst_topBttmMargin = 2.5f;
        private const float cnst_leftRightMargin = 2.0f;
        private const float cnst_a4height_cm = 29.5f;
        private const float cnst_a4width_cm = 21.0f;

        private const int cnst_spaceAferSectionHead = 10;

        #region properties
        private string currentTargetDir;
        private string clientName = "";
        public string ClientName { get { return ClientName; } set { if (value != clientName) clientName = value; } }
        private string fileName;
        private string fullPathFileName;
        private string targetClientLogoPath;
        public MARSRPT_Summary summaryInfo;
        private XGraphics currentGrph;
        private PdfPage currentPage=null;

        private string targetApplicationName;
        public string TargetApplicationName
        {
            get
            {
                return targetApplicationName;
            }
            set
            {
                if (value != targetApplicationName)
                    targetApplicationName = value;
            }
        }

        private static string tempPicturePath;
        public static string TempPicturePath
        {
            get { return tempPicturePath; }
            set { tempPicturePath = value; }
        }
        private string reportFirstPageHeader;
        public string ReportFirstPageHeader
        {
            get { return reportFirstPageHeader; }
            set { reportFirstPageHeader = value; }
        }
        private string reportPageEyebrow;
        public string ReportPageEyebrow
        {
            get { return reportPageEyebrow; }
            set { reportPageEyebrow = value; }
        }

        public string FileName
        { 
            get { return fileName; }
            set
            {
                if (value != fileName)
                {
                    fileName = value;
                    pdfTargetDocument = new PdfDocument();
                    fullPathFileName = Path.Combine(currentTargetDir, fileName);
                    //m_objScreenMgr.ParentDocumentPdf = pdfTargetDocument;
                }
            }
        }

        public string ClientLogoPath
        {
            get { return targetClientLogoPath; }
            set { if (value != targetClientLogoPath) targetClientLogoPath = value; }
        }

        private PdfDocument pdfTargetDocument = null;
        #endregion

        #region //screen shot page
        //private MarsTigerScreenShotMgr m_objScreenMgr = new MarsTigerScreenShotMgr();
        #endregion

        public bool BeginToGen(string strPath,ref string strError)
        {
            Logger.Info("BeginToGen",string.Format("Try save report to :[{0}]", strPath));
            if (Directory.Exists(strPath))
            {
                currentTargetDir = strPath;
                return true;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(strPath);
                }
                catch (Exception e)
                {
                    Logger.Error("BeginToGen", strError=string.Format("Can't create Target Directory:[{0}] with exception:[{1}], stackTrace:[{2}]", strPath, e.Message,e.StackTrace), e);
                    return false;
                }
            }
            /// try to create tempeoray file to test whether it is writable
            /// 
            string strTmpFileName = "MarsReportTestTmp.tmp";
            FileInfo fi = null;
            FileStream fs = null;
            try
            {
                strTmpFileName = Path.Combine(currentTargetDir, strTmpFileName);
                fi = new FileInfo(strTmpFileName);
                fi.Attributes = FileAttributes.Temporary;
                fs = fi.Create();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("BeginToGen", strError = string.Format("Can't create temp File:[{0}] with exception:[{1}], stackTrace:[{2}]", strTmpFileName, e.Message, e.StackTrace), e);
                return false;
            }finally
            {
                if (fs != null)
                {
                    fs.Close();                    
                }
                if (fi != null)
                    fi.Delete();
            }
            
        }
        /// <summary>
        /// To generate the first page
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool GenLogoPage(ref string strError)
        {
            Logger.logBegin("GenLogoPage");
            pdfTargetDocument.Info.Title = "Created with PDFsharp";
            PdfPage objFirst = pdfTargetDocument.AddPage();
            XGraphics objCurrentGrph = XGraphics.FromPdfPage(objFirst);

            objFirst.Size = PdfSharp.PageSize.A4;

            Logger.Info("GenLogPage", string.Format("size:[{0},{1}]", objFirst.Width, objFirst.Height));
            /// Write page title
            /// 
            CreateLogPageTitle(objFirst, objCurrentGrph);
            //pdfTargetDocument.Save(fullPathFileName = Path.Combine(currentTargetDir, fileName));
            CreateFirstPage(objFirst, objCurrentGrph);
            
            return true;
        }
        private void CreateLogPageTitle(PdfPage objCurrentPage, XGraphics objGrph)
        {
            Logger.logBegin("CreateLogPageTitle");
            /// steps:
            /// 1, write page lead
            /// 2, write page Logo
            /// 
            /// 1, write page lead
            /// 
            
            XFont font = new XFont("Arial", 10, XFontStyle.Bold);
            objGrph.DrawLine(basicPen_black, new System.Drawing.Point(xLeft,yTop),
                new System.Drawing.Point(xRight, yTop));
            string strTitle = ReportPageEyebrow;
            XSize xsz = objGrph.MeasureString(strTitle, textFont);
            /// draw a Marquis logo at the right side
            /// 
            /// not implement
            /// 

            objGrph.DrawString(strTitle, textFont, regularBrush,xLeft,yTop-xsz.Height );            
        }

        private void CreateFirstPage(PdfPage objCurrentPage, XGraphics objGrph)
        {
            Logger.logBegin("CreateFirstPage");
            /// steps:
            /// 1, write Mars automation testing Letter 
            /// 2, write project name
            /// 3, write version
            /// 4, write generate date
            /// 

            /// 1, write Mars automation testing Letter 
            int xStart = xLeft ;
            int yStart = yTop + 40;
            string strInfo = ReportFirstPageHeader;
            XFont objFont = textFont18Bold;
            //XGraphics objGrph = XGraphics.FromPdfPage(objCurrentPage);
            XSize objXSz = objGrph.MeasureString(strInfo,objFont);            
            DrawStringCenterOnRect(strInfo, objCurrentPage, new XRect(xStart, yStart, xRight - xLeft, yStart + objXSz.Height), objFont, objGrph);

            objFont = textFont12Regular;
            strInfo = string.Format("On {0}",this.targetApplicationName);
            xStart = xLeft;
            yStart = yStart + (int)objXSz.Height + 30;
            objXSz = objGrph.MeasureString(strInfo, objFont);
            DrawStringCenterOnRect(strInfo, objCurrentPage, new XRect(xStart, yStart, xRight - xLeft, objXSz.Height), objFont, objGrph);

            strInfo = string.Format("For {0}",this.clientName);
            xStart = xLeft;
            yStart = yStart + (int)objXSz.Height + 5;
            objXSz = objGrph.MeasureString(strInfo, objFont);
            DrawStringCenterOnRect(strInfo, objCurrentPage, new XRect(xStart, yStart, xRight - xLeft, objXSz.Height), objFont, objGrph);

            /// Create client logo
            /// 
            xStart = xLeft;
            yStart = yStart + (int)objXSz.Height + 20;
            string strError = "";
            bool isLogoOk=DrawImageCenterOnRect(this.targetClientLogoPath, objCurrentPage,ref objGrph, xStart, yStart, xRight - xLeft,ref strError);

            /// write target company 
            /// 
            objFont = textFont;
            strInfo ="Marquis Bussiness Tech Solution LLC\r\nwww.mbtsllc.com" ;
            xStart = xLeft;
            objXSz = objGrph.MeasureString(strInfo, objFont);
            yStart = yBottom - (int)objXSz.Height*2- 5;// *2 because two rows,the last row is 
            
            strInfo = string.Format("{0}\r\n{1}", strInfo, DateTime.Today.ToString("yyyy-MM-dd"));
            double dNeedHeight = 0.0;
            XRect rectTarget = new XRect(xStart, yStart, xRight - xLeft, objXSz.Height * 2);
            //int iPage = 1;
            //DoTestHeightBeforeWriteMultLines(strInfo, objFont, XBrushes.Black, ref rectTarget, ref iPage,XStringFormats.TopLeft, ref objGrph);
            WriteMultRowsInsideOfARect(strInfo, rectTarget, objCurrentPage,ref objGrph, XStringFormats.Center,objFont,out dNeedHeight);
            //WriteMultRowsInsideOfARect(DateTime.Today.ToString("yyyy-MM-dd"), new XRect(xStart, yStart+ dNeedHeight, xRight - xLeft, objXSz.Height ), objCurrentPage, ref objGrph, XStringFormats.Center, objFont, out dNeedHeight);
            DrawBottomCloseLine(objCurrentPage, ref objGrph,1);
        }

        private void DoTestHeightBeforeWriteMultLines(string strInfo, XFont objFont, XBrush objBrush, ref XRect rectTarget, ref int iPageNumber, XStringFormat frmt, ref XGraphics objGrph)
        {
            XTextFormatter xf = new XTextFormatter(objGrph);
            double dNeedHeight = 0d, dyNew = 0.0 ;
            xf.PrepareDrawStringByTiger(strInfo, objFont, objBrush, rectTarget, frmt, out dNeedHeight);
            dyNew = rectTarget.Y;
            AdjustPageAndGraph(dNeedHeight + rectTarget.Y, ref iPageNumber, ref dyNew, out this.currentPage, out objGrph);
            rectTarget = new XRect(rectTarget.X, dyNew, rectTarget.Width, dNeedHeight);
        }

        internal static bool DrawImageCenterOnRect(string imgPathFull, PdfPage objCurrentPage, ref XGraphics objGrph, int xStart, int yStart, int iWidth,ref string strError)
        {
            Logger.Info("DrawImageCenterOnRect",string.Format("try to draw a img [{0}] from LeftTop:[{1},{2}] width:[{3}]", imgPathFull, xStart,yStart,iWidth));
            try
            {
                XImage objImg = XImage.FromFile(imgPathFull);
                double dRate = objImg.Size.Width / iWidth;
                if (dRate<1)
                {
                    /// draw original size
                    /// 
                    int ix = xStart + (int)((iWidth - objImg.Size.Width) / 2.0);
                    objGrph.DrawImage(objImg, new System.Drawing.Rectangle(ix, yStart,(int)objImg.Size.Width,(int)objImg.Size.Height));
                    return true;
                }

                if (objImg.Size.Width>iWidth)
                {
                    Logger.Warnning("DrawImageCenterOnRect", "Can't draw orignal picture, change to 256 width");
                    dRate = objImg.Size.Width / 256.0;
                    double dH = objImg.Size.Height / dRate;
                    int ix = xStart + (int)((iWidth - 256) / 2.0);

                    objGrph.DrawImage(objImg, new System.Drawing.Rectangle(ix, yStart, 256, (int)dH));
                }
                return true;
            }
            catch (Exception e)
            {

                Logger.Error("DrawImageCenterOnRect", string.Format("Exception when call XImage.FromFile:[{0}], stackTrace:[{1}]",e.Message,e.StackTrace),e);
                return false;
            }
            
           
        }

        internal static void WriteMultRowsInsideOfARectTiger(string strText, XRect rectTarget, PdfPage objCurrentPage,
            ref XGraphics objGrph, XStringFormat frmt, XFont objFont,out double dwTotalHeight,
            XBrush objBrush = null)
        {
            Logger.Info("WriteMultRowsInsideOfARectTiger",string.Format("{0},rect:[{1}]", strText, rectTarget));
            XTextFormatter tf = new XTextFormatter(objGrph);
            tf.Alignment = frmt.Alignment == XStringAlignment.Center ? XParagraphAlignment.Center : (frmt.Alignment == XStringAlignment.Near ? XParagraphAlignment.Left : XParagraphAlignment.Right);
            tf.DrawStringTiger(strText, objFont, XBrushes.Black, rectTarget, XStringFormats.TopLeft,out dwTotalHeight);
        }

        internal static void WriteMultRowsInsideOfARect(string strText, XRect rectTarget,PdfPage objCurrentPage,
            ref XGraphics objGrph, XStringFormat frmt, XFont objFont, 
            out double dNeedHeight,XBrush objBrush = null)
        {
            Logger.Info("WriteMultRowsInsideOfARect",string.Format("{0}, rect:[{1}]",strText, rectTarget));     
            XTextFormatter tf = new XTextFormatter(objGrph);
            tf.Alignment = frmt.Alignment == XStringAlignment.Center ? XParagraphAlignment.Center : (frmt.Alignment == XStringAlignment.Near? XParagraphAlignment.Left : XParagraphAlignment.Right);
            tf.DrawString(strText, objFont, XBrushes.Black, rectTarget, frmt, out dNeedHeight);
        }

        internal static XPoint GetPageDefaultTopLeft()
        {
            return new XPoint(xLeft, yTop);
        }

        internal static XSize GetPageDefaultSize()
        {
            return new XSize(xRight - xLeft, yBottom - yTop);
        }

        private void DrawBottomCloseLine(PdfPage objCurrentPage, ref XGraphics objGrph, int iPage=-1)
        {
            Logger.Info("DrawBottomCloseLine",string.Format("from to:[{0},{1},{2},{3}]",xLeft,yBottom,xRight,yBottom));
            objGrph.DrawLine(basicPen_black, new System.Drawing.Point(xLeft, yBottom),
                new System.Drawing.Point(xRight, yBottom));
            if (iPage==-1)
            {
                return;
            }
            string strInfo = string.Format("Page {0}",iPage);
            XFont objFnt = textFontSmall;
            double dNeedHeight = 0.0;
            WriteMultRowsInsideOfARect(strInfo, new XRect(xLeft, yBottom, xRight - xLeft, 20), objCurrentPage, ref objGrph, XStringFormats.BottomCenter, objFnt, out dNeedHeight);
        }

        internal static void DrawStringOnRectWithPosition(string strInfo, PdfPage objCurrentPage, XRect rectTarget, XFont objFont, XGraphics objGrph, XStringFormat frmt, XBrush objBrush = null)
        {
            Logger.Info("DrawStringOnRectWithPosition", string.Format("Try to write a string:[{0}], in a rect:[{1}], format:[{2}]", strInfo, rectTarget, frmt));
            double dNeedHeight = 0.0;
            WriteMultRowsInsideOfARect(strInfo, rectTarget, objCurrentPage, ref objGrph, frmt, objFont, out dNeedHeight);
//            objGrph.DrawString(strInfo, objFont, objBrush == null ? XBrushes.Black : objBrush, rectTarget, frmt);
        }

        internal static void DrawStringCenterOnRect(string strInfo,PdfPage objCurrentPage, XRect rectTarget, XFont objFont, XGraphics objGrph, XBrush objBrush= null)
        {
            Logger.Info("DrawStringCenterOnRect",string.Format("rectTarget:[{0},{1},{2},{3}]",rectTarget.Left,rectTarget.Top, rectTarget.Width,rectTarget.Height));
            //XGraphics objGrph = XGraphics.FromPdfPage(objCurrentPage);
            DrawStringOnRectWithPosition(strInfo, objCurrentPage, rectTarget, objFont, objGrph, XStringFormats.TopCenter);
        }
        private void DrawStringLeftOnRect(string strInfo, PdfPage objCurrentPage, XRect rectTarget, XFont objFont, XGraphics objGrph, XBrush objBrush = null)
        {
            Logger.Info("DrawStringLeftOnRect",string.Format("rectTarget:[{0},{1},{2},{3}]", rectTarget.Left, rectTarget.Top, rectTarget.Width, rectTarget.Height));
            DrawStringOnRectWithPosition(strInfo, objCurrentPage, rectTarget, objFont, objGrph, XStringFormats.TopLeft);
        }
        #region Rect information
        private const string cnst_font_name = "Arial";
        internal const int cnst_section_dis = 15;
        internal const double cnst_subSetction_dis = 1.5;
        internal const double cnst_rowDis = 1;
        internal static XFont textFontSmall = new XFont(cnst_font_name, 8, XFontStyle.Regular);
        internal static XFont textFont = new XFont(cnst_font_name, 8, XFontStyle.Regular);        
        private XPen basicPen_black = new XPen(XColor.FromName("LightGray"), 1);
        private XBrush regularBrush = XBrushes.Black;
        internal static XFont textFont18Bold = new XFont(cnst_font_name, 14, XFontStyle.Bold);
        internal static XFont textFont12Regular = new XFont(cnst_font_name, 9, XFontStyle.Regular);
        internal static XFont textFont12RegularBold = new XFont(cnst_font_name, 10, XFontStyle.Bold);
        //private XFont textFont24Bold = new XFont(cnst_font_name, 24, XFontStyle.Bold);
        internal static XFont textLeadFont = new XFont(cnst_font_name, 14, XFontStyle.Bold);


        private static int xLeft
        {
            get { return (int)XUnit.FromCentimeter(cnst_leftRightMargin); }
        }
        private static int yTop
        {
            get { return (int)XUnit.FromCentimeter(cnst_topBttmMargin); }
        }
        private static int xRight
        {
            get { return (int)XUnit.FromCentimeter(cnst_a4width_cm - cnst_leftRightMargin); }
        }
        private static int yBottom
        {
            get { return (int)XUnit.FromCentimeter(cnst_a4height_cm - cnst_topBttmMargin); }
        }

        
        #endregion //Rect information

        public bool saveToFile(ref string strError)
        {
            Logger.logBegin("saveToFile");
            if (pdfTargetDocument==null)
            {
                strError = "Main document is null";
                return false;
            }
            try
            {
                if(File.Exists(fullPathFileName))
                {
                    File.Delete(fullPathFileName);
                }
                pdfTargetDocument.Save(fullPathFileName);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("saveToFile",string.Format("Exception:[{0}], stackTrace:[{1}]",e.Message,e.StackTrace),e);
                return false;
            }
        }

        
        public bool GenSummaryPage(long? l_base_fail_cnt, long? l_base_partial_cnt, long? l_base_right_cnt, long? l_cmp_fail_cnt,
            long? l_cmp_partial_cnt, long? l_cmp_right_cnt, long? l_tccnt, long? l_tscnt, long? l_teststep_cnt,string strStoryBoardName,long l_storyBoardId,int iUnprcTCCnt,
            MarsPieGraphEnhance storybrdPieData,
            ref string strError, ref int iReturnPage,ref double dCurrentYPos,ref int iStartPage)
        {
            Logger.logBegin("GenSummaryPage");
            PdfPage objSummary = pdfTargetDocument.AddPage();
            currentPage = objSummary;

            objSummary.Size = PdfSharp.PageSize.A4;
            currentGrph = XGraphics.FromPdfPage(objSummary);
            CreateLogPageTitle(objSummary, currentGrph);
            DrawBottomCloseLine(objSummary, ref currentGrph, iStartPage);

            /// Write Head Line of Summary
            /// 
            XFont objFont = textFont18Bold;
            string strInfo = "•  Testing Summary";
            int iyStop = yTop + 5;
            XSize objSz = currentGrph.MeasureString(strInfo, objFont);
            this.DrawStringLeftOnRect(strInfo, objSummary, new XRect(xLeft, iyStop, xRight - xLeft, objSz.Height), objFont, currentGrph);

            /// 
            string strSummary = string.Format(
@"Test Storyboard   : {0}
                              {1} Test Suites
                              {2} Test Cases {5}

Current report is generated on {3}
{4} test steps are covered. 
  ",
                        strStoryBoardName, l_tscnt ?? 0, l_tccnt ?? 0, DateTime.Now.ToString("MM/dd/yyyy HH:mm"), l_teststep_cnt??0,
                        iUnprcTCCnt<=0?"":string.Format("{0} unprocessed", iUnprcTCCnt));            
            objFont = textFont12Regular;
            iyStop += ((int)objSz.Height + cnst_spaceAferSectionHead);
            objSz = currentGrph.MeasureString(strSummary, objFont);
            XSize objSzDefault = currentGrph.MeasureString("aA", objFont);
            XSize objRequiredSize = currentGrph.MeasureString(strSummary, objFont);
            int iRequiredRow = (int)Math.Ceiling(objRequiredSize.Width / (xRight - xLeft));
            objSz.Height = objSz.Height * iRequiredRow;
            double dNeedHeight = 0.0;
            //int iPage = 1;
            XRect rectTarget = new XRect(xLeft, iyStop, xRight - xLeft, objRequiredSize.Height);
            DoTestHeightBeforeWriteMultLines(strInfo, objFont, XBrushes.Black, ref rectTarget, ref iStartPage, XStringFormats.TopLeft, ref currentGrph);
            WriteMultRowsInsideOfARect(strSummary, rectTarget, objSummary,ref currentGrph, XStringFormats.TopLeft,objFont,out dNeedHeight);
            iReturnPage = iStartPage;
            dCurrentYPos = dNeedHeight + iyStop;

            #region baseline info
            strSummary = string.Format("Baseline");
            objFont = textFont12RegularBold;
            WriteMultRowsInsideOfARectTiger(strSummary, new XRect(xLeft, dCurrentYPos, xRight - xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            dCurrentYPos += dNeedHeight;

            objFont = textFont12Regular;
            strSummary = string.Format(@"Success {0}
Failed     {1} 
", l_base_right_cnt ?? 0, l_base_fail_cnt ?? 0);
            WriteMultRowsInsideOfARectTiger(strSummary, new XRect(xLeft, dCurrentYPos, xRight - xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            dCurrentYPos += dNeedHeight;
            #endregion 

            #region comparison info
            strSummary = string.Format("Comparison");
            objFont = textFont12RegularBold;
            WriteMultRowsInsideOfARectTiger(strSummary, new XRect(xLeft, dCurrentYPos, xRight - xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            dCurrentYPos += dNeedHeight;

            objFont = textFont12Regular;
            strSummary = string.Format(@"Success {0}
Failed     {1}
Unprocessed {2}", l_cmp_right_cnt ?? 0,( l_cmp_fail_cnt ?? 0)+(l_cmp_partial_cnt??0),iUnprcTCCnt);
            WriteMultRowsInsideOfARectTiger(strSummary, new XRect(xLeft, dCurrentYPos, xRight - xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            dCurrentYPos += dNeedHeight;
            #endregion 

            /*Baseline 
Success {5}
Failed     {6} 

Comparison 
Success  {7}
Failed      {8}",                        
*/

            /// draw a pie
            /// 
            GenTestCaseGraphPieInfo(storybrdPieData, ref iReturnPage, ref dCurrentYPos, ref strError, false);
            
            Logger.logEnd("GenSummaryPage");
            return true;
        }

        /*
        public bool GenEnvironment(ref int iCurrentPage, ref double dCurrentPos_y, ref string strError)
        {
            Logger.Info("GenEnvironment",string.Format("CurrentPage:[{0}] currentPosY:[{1}]",iCurrentPage,dCurrentPos_y));
            XFont objFont = textFont18Bold;            
            if (currentPage == null)
            {
                currentPage = pdfTargetDocument.AddPage();
                currentPage.Size = PdfSharp.PageSize.A4;
                CreateLogPageTitle(currentPage, currentGrph);
                DrawBottomCloseLine(currentPage, ref currentGrph, iCurrentPage);
            }
            if (currentGrph==null)
                currentGrph = XGraphics.FromPdfPage(currentPage);
            
            string strInfo = "•  Testing Environment";
            int iyStop = cnst_section_dis + (int)dCurrentPos_y;
            XSize objSz = currentGrph.MeasureString(strInfo, objFont);
            this.DrawStringLeftOnRect(strInfo, currentPage, new XRect(xLeft, iyStop, xRight - xLeft, objSz.Height), objFont, currentGrph);

            strInfo = "  Testing Support environment";
            iyStop += (3 +(int)objSz.Height);
            objFont = textFont12RegularBold;
            objSz = currentGrph.MeasureString(strInfo,objFont);
            this.DrawStringLeftOnRect(strInfo, currentPage, new XRect(xLeft, iyStop, xRight - xLeft, objSz.Height), objFont, currentGrph);

            /// basic environment information
            /// 
            objFont = textFont12Regular;
            string strFriendlyName = SystemCommon.GetFriendSystemName();
            string strTotalPhysicalMem = SystemCommon.GetTotalPhysicalMemory();
            string strFreeDisk = string.Format("HardDisk User Free size/Total Size:[{0}MB/{1}MB]",SystemCommon.GetDiskFree(),SystemCommon.GetDiskTotalSize());
            strInfo = string.Format("    Tested on machine:[{2}], {4} version:[{0}], [{1}] bits, Service Pack:[{3}].\r\n    Total Memory:[{5}]\r\n    {6}", Environment.OSVersion.ToString(),
                Environment.Is64BitOperatingSystem ? 64 : 32, Environment.MachineName,string.IsNullOrWhiteSpace(Environment.OSVersion.ServicePack)?"NONE": Environment.OSVersion.ServicePack,
                strFriendlyName, strTotalPhysicalMem, strFreeDisk);
            iyStop += ((int)objSz.Height);
            double dNeedHeight = 0.0;
            WriteMultRowsInsideOfARect(strInfo, new XRect(xLeft, iyStop, xRight - xLeft, objSz.Height), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            dCurrentPos_y = iyStop + dNeedHeight;
            return true;
        }
        */

        public bool GensignaturePage(string strSoftWareProvider, ref int iCurrentPage, ref double dCurrentPos_y, ref string strError,string strClientFullName)
        {
            Logger.Info("GensignaturePage", string.Format("softeware provider:[{0}] iCurrentPage:[{1}] currentPos_y:[{2}]",
                strSoftWareProvider, iCurrentPage, dCurrentPos_y));
            /// write client info
            /// 
            string strHeader = "•  Signature";
            XFont objFont = textFont18Bold;
            if (currentPage == null)
            {
                currentPage = pdfTargetDocument.AddPage();
                currentPage.Size = PdfSharp.PageSize.A4;
                CreateLogPageTitle(currentPage, currentGrph);
                DrawBottomCloseLine(currentPage, ref currentGrph, iCurrentPage);
            }
            if (currentGrph == null)
                currentGrph = XGraphics.FromPdfPage(currentPage);
            double dyStart = dCurrentPos_y + cnst_section_dis;
            XSize objSz = currentGrph.MeasureString(strHeader,objFont);
            double dHeight = 0.0;
            XRect rectT = new XRect(xLeft, dyStart, xRight - xLeft, objSz.Height+1);
            this.DoTestHeightBeforeWriteMultLines(strHeader, objFont, XBrushes.Black, ref rectT, ref iCurrentPage, XStringFormats.TopLeft, ref currentGrph);
            WriteMultRowsInsideOfARectTiger(strHeader, rectT, currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart = (rectT.Y + dHeight+ cnst_rowDis);

            /// write client section
            /// 
            objFont = textFont12RegularBold;            
            strHeader = "Test Project sponsor: "+ strClientFullName;
            objSz = currentGrph.MeasureString(strHeader, objFont);
            rectT = new XRect(xLeft, dyStart, xRight - xLeft, objSz.Height + 1);
            this.DoTestHeightBeforeWriteMultLines(strHeader, objFont, XBrushes.Black, ref rectT, ref iCurrentPage, XStringFormats.TopLeft, ref currentGrph);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart = (rectT.Y + dHeight + cnst_rowDis);

            strHeader = "(Sign here)                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Italic);
            objSz = currentGrph.MeasureString(strHeader, objFont);
            rectT = new XRect(xLeft, dyStart, xRight - xLeft, objSz.Height + 1);
            this.DoTestHeightBeforeWriteMultLines(strHeader, objFont, XBrushes.Black, ref rectT, ref iCurrentPage, XStringFormats.TopLeft, ref currentGrph);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart = (rectT.Y + dHeight + cnst_rowDis);
            XPen objPen = new XPen(XColors.Black);
            objPen.Width = 0.8;
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 0.5), new XPoint(xLeft + 200, dyStart - 0.5));

            objFont = textFont12RegularBold;
            strHeader = "Date:";
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);

            strHeader = "                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Regular);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis + 4);
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 4.5), new XPoint(xLeft + 200, dyStart - 4.5));

            /// write software provider 
            /// 
            objFont = textFont12RegularBold;
            strHeader = "Software provider: "+ strSoftWareProvider;
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);


            strHeader = "(Sign Here)                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Italic);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);
            objFont = textFont12RegularBold;
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 0.5), new XPoint(xLeft + 200, dyStart - 0.5));

            strHeader = "Date:";
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);

            strHeader = "                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Regular);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis + 4);
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 4.5), new XPoint(xLeft + 200, dyStart - 4.5));

            /// write Tester provider 
            /// 
            objFont = textFont12RegularBold;
            strHeader = "Tester provider: Marquis Business Tech Solution LLC." ;
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);


            strHeader = "(Sign Here)                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Italic);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);
            objFont = textFont12RegularBold;
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 0.5), new XPoint(xLeft + 200, dyStart - 0.5));

            strHeader = "Date:";
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);

            strHeader = "                             ";
            objFont = new XFont(cnst_font_name, 11, XFontStyle.Regular);
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis + 4);
            this.currentGrph.DrawLine(objPen, new XPoint(xLeft, dyStart - 4.5), new XPoint(xLeft + 200, dyStart - 4.5));

            strHeader = "(End)";
            objFont = textFontSmall;
            WriteMultRowsInsideOfARectTiger(strHeader, new XRect(xLeft, dyStart, xRight - xLeft, 10), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dHeight);
            dyStart += (dHeight + cnst_rowDis);

            dCurrentPos_y = dyStart;
            return true;
        }

        public bool GenTestStoryBoardSummary(ReportGridDataInterface objStoryBoardData, string strStoryBoardDescription, ref int iCurrentPage, ref double dCurrentPos_y, ref string strError)
        {
            Logger.Info("GenTestStoryBoardSummary",string.Format("Try to gen storyboard from yPos:[{0}]", dCurrentPos_y));
            /// steps:
            /// 1, write section header
            /// 2, Write sub Header
            /// 3, Write Grid
            /// 
            string strInfo = "•  Test Storyboard Information";
            XFont objFont = textFont18Bold;
            if (currentPage == null)
            {
                currentPage = pdfTargetDocument.AddPage();
                currentPage.Size = PdfSharp.PageSize.A4;
                CreateLogPageTitle(currentPage, currentGrph);
                DrawBottomCloseLine(currentPage, ref currentGrph, iCurrentPage);
            }
            if (currentGrph == null)
                currentGrph = XGraphics.FromPdfPage(currentPage);
            int iYStart = (int)dCurrentPos_y+ cnst_section_dis+3;
            XSize objSz = default(XSize);
            #region 1, write section header
            objSz = currentGrph.MeasureString(strInfo, objFont);
            this.DrawStringLeftOnRect(strInfo, currentPage, new XRect(xLeft, iYStart, xRight - xLeft, objSz.Height), objFont, currentGrph);
            #endregion

            double dNeedHeight = 0.0;
            #region 2, Write sub Header
            objFont = textFont12Regular;
            iYStart += (int)Math.Ceiling(objSz.Height);
            strInfo = string.Format("1. Description \r\n    {0}\r\n2. Storyboard Test Cases Details", strStoryBoardDescription);
            objSz = currentGrph.MeasureString(strInfo, objFont);
            WriteMultRowsInsideOfARect(strInfo, new XRect(xLeft, iYStart, xRight - xLeft, objSz.Height), currentPage, ref currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
            #endregion

            iYStart += ((int)dNeedHeight+3);
            GenAStandardGrid(ref pdfTargetDocument,ref iYStart, ref currentPage, ref currentGrph,ref iCurrentPage, objStoryBoardData);
            dCurrentPos_y = iYStart;
            return true;
        }

        private void GenAStandardGrid(ref PdfDocument pdfDocument, ref int currentYPosFrom, ref PdfPage currentPage, ref XGraphics currentGrph,ref int iCurrentPageNumber, ReportGridDataInterface objStoryBoardData)
        {
            Logger.logBegin("GenAStandardGrid");
            /// create section
            /// 
            //Section objSection = currentPage.

            PDFTableDrawExtand objDrawTableInPdf = new PDFTableDrawExtand();
            objDrawTableInPdf.currentGrph = currentGrph;
            objDrawTableInPdf.currentDocument = pdfDocument;
            objDrawTableInPdf.currentPage = currentPage;
            objDrawTableInPdf.xLeft = xLeft;
            objDrawTableInPdf.yTop = (int)currentYPosFrom+1;
            objDrawTableInPdf.HeaderFont = textFont;
            objDrawTableInPdf.DataFont = textFont;
            objDrawTableInPdf.GridData = objStoryBoardData;
            objDrawTableInPdf.CurrentPageNumber = iCurrentPageNumber;
            objDrawTableInPdf.adjustEventHandler = this.AdjustPageAndGraph;

            string strError = "";
            objDrawTableInPdf.DrawTable(ref strError);

            currentYPosFrom = (int)(objDrawTableInPdf.ydCurrentPos) + 1;
            iCurrentPageNumber = objDrawTableInPdf.CurrentPageNumber;
        }

        public bool GenTestStoryTestCaseSection(string strSectionCaption, ref int iCurrentPage, ref double dCurrentPos_y, ref string strError)
        {
            Logger.Info("GenTestStoryTestCaseSection",string.Format("iCurrentPage:[{0}] currentPos_y:[{1}], section toWrite:[{2}]", iCurrentPage, dCurrentPos_y, strSectionCaption));

            XFont objFont = textFont12Regular;

            /// get height
            /// 
            XTextFormatter tf = new XTextFormatter(this.currentGrph);
            XRect rectTarget;
            double dwH;
            dCurrentPos_y += cnst_subSetction_dis;
            tf.PrepareDrawStringByTiger(strSectionCaption, objFont, XBrushes.Black, rectTarget = new XRect(xLeft, dCurrentPos_y, xRight-xLeft, 10), XStringFormats.TopLeft, out dwH);
            this.DrawStringLeftOnRect(strSectionCaption, this.currentPage, rectTarget = new XRect(xLeft, dCurrentPos_y, xRight - xLeft, dwH), objFont, this.currentGrph);
            dCurrentPos_y += (dwH + 1);
            return true;
        }

        public void GenTestCaseDetailInfo(string strTestcaseSectionInfo, ref int iCurrentPage, ref double dCurrentPos_y, ref string strError)
        {
            Logger.Info("GenTestCaseDetailInfo",string.Format("Write sub section of Test case info:[0], position y:[{1}], page number:[{2}]",strTestcaseSectionInfo, dCurrentPos_y, iCurrentPage));
            dCurrentPos_y += 1;
            double dNeedHeight = 0.0;
            XFont objFont = textFont12Regular;
            
            //int iPage = 1;
            XRect rectTarget = new XRect(xLeft, dCurrentPos_y, xRight - xLeft, 10);
            DoTestHeightBeforeWriteMultLines(strTestcaseSectionInfo, objFont, XBrushes.Black, ref rectTarget, ref iCurrentPage, XStringFormats.TopLeft, ref currentGrph);

            WriteMultRowsInsideOfARect(strTestcaseSectionInfo, rectTarget, this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont,out dNeedHeight);
            dCurrentPos_y = rectTarget.Y+dNeedHeight;
        }

        public void GenTestCaseGraphPieInfo(MarsPieGraphEnhance objRptPieData, ref int iCurrentPage, ref double dCurrentPosY, ref string strError,bool isTCData=true)
        {
            Logger.Info("GenTestCaseGraphPieInfo", string.Format("Try to draw pies from y_Pos {0}, page:[{1}]", dCurrentPosY, iCurrentPage));
            double dNeedHeight = 0.0;
            XFont objFont = textFont12Regular;
            if (objRptPieData==null)
            {
                Logger.Warnning("GenTestCaseGraphPieInfo", strError="Data is null, no Pie graph is to draw");
                WriteMultRowsInsideOfARect(string.Format("({0})", strError), new XRect(xLeft,dCurrentPosY, xRight-xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
                dCurrentPosY += (dNeedHeight+ cnst_subSetction_dis);
                return;
            }

            /// to draw series pies
            /// 
            List<KeyValuePair<string, double>> lstPieInfo = objRptPieData.GetPartsInfo();
            double dRadius = objRptPieData.GetRadius();

            if ((lstPieInfo==null)||(lstPieInfo.Count==0))
            {
                Logger.Warnning("GenTestCaseGraphPieInfo", strError = "Angles return null or count is 0, no Pie graph is to draw");
                WriteMultRowsInsideOfARect(string.Format("({0})", strError), new XRect(xLeft, dCurrentPosY, xRight - xLeft, 10), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dNeedHeight);
                dCurrentPosY += (dNeedHeight + cnst_subSetction_dis);
                return;
            }
            XBrush[] arrBrush = new XBrush[] { XBrushes.LimeGreen, XBrushes.Red, XBrushes.Gold,XBrushes.Orange};
            XPen objPen = new XPen(XColors.Gray);
            objPen.Width = 1;
            double yPos = dCurrentPosY + cnst_subSetction_dis;
            PdfPage currPage = null;
            XGraphics objGrph = null;
            if (!isTCData)
                AdjustPageAndGraph(yPos+ dRadius,ref iCurrentPage, ref dCurrentPosY,out currPage,out objGrph);
            yPos = dCurrentPosY;
            objFont = textFont;
            XSize objSz = this.currentGrph.MeasureString("Unprocess",objFont);
            double dSmplDisX = 3.0;
            XRect objRct = new XRect(-xLeft / 2.0 + xRight / 2.0 - dRadius / 2.0-dSmplDisX/2.0, yPos, dRadius, dRadius);
            double dStartAngle = 0.0,dEndangle=0.0;
            if (!isTCData)
                for (int i=0;i<lstPieInfo.Count;i++)
                {
                    dStartAngle = i == 0 ? 0.0 : dStartAngle + lstPieInfo[i-1].Value * 360;
                    dEndangle = dStartAngle + lstPieInfo[i].Value * 360-1;
                    if (lstPieInfo[i].Value == 0) continue;
                    Logger.Info("GenTestCaseGraphPieInfo",string.Format("Loop :{0}",i));
                    try
                    {
                        if (i<lstPieInfo.Count)
                            this.currentGrph.DrawPie(objPen, arrBrush[i % arrBrush.Length], objRct, dStartAngle, lstPieInfo[i].Value * 360);
                        else
                            this.currentGrph.DrawPie(objPen, arrBrush[i % arrBrush.Length], objRct, dStartAngle, 360);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("GenTestCaseGraphPieInfo",string.Format("exception:[{0}] stackTrace:[{1}]",e.Message,e.StackTrace));
                    }
                    //break; 
                }
            
            /// draw samples 
            /// 
            XSize objSmplSz = new XSize(5, objSz.Height-2);
            double dSmpYPos = yPos;
            double dSmpXPos = xLeft + (xRight - xLeft) / 2.0 + dSmplDisX;
            double dTxtXPos = dSmpXPos + objSmplSz.Width + 1.0;
            double dTxtYPos = dSmpYPos;
            double dTmpTxtHeight = 0.0;
            if (!isTCData)
            {
                for (int i = 0; i < lstPieInfo.Count; i++)
                {

                    /// draw a rectangle
                    /// 
                    this.currentGrph.DrawRectangle(arrBrush[i % arrBrush.Length], new XRect(dSmpXPos, dSmpYPos, objSmplSz.Width, objSmplSz.Height));
                    /// write a string
                    /// 
                    string strCurSmplTxt = string.Format("{0} {1}", lstPieInfo[i].Key, (lstPieInfo[i].Value).ToString("P"));
                    XSize objSmpTxtSize = this.currentGrph.MeasureString(strCurSmplTxt, objFont);
                    WriteMultRowsInsideOfARect(strCurSmplTxt, new XRect(dTxtXPos, dTxtYPos, objSmpTxtSize.Width + 1, objSmpTxtSize.Height), this.currentPage, ref this.currentGrph, XStringFormats.TopLeft, objFont, out dTmpTxtHeight);

                    /// move yPos down
                    dSmpYPos += (objSmpTxtSize.Height + 1);
                    dTxtYPos = dSmpYPos;

                    /// write a test step result based on loop
                    /// 
                    if (objRptPieData == null) continue;
                    //dSmpYPos += objRct.Height + cnst_section_dis;                
                    //break;
                }

                dCurrentPosY = (yPos + cnst_subSetction_dis * 2 + dRadius);
            }
            if (isTCData)
                GenTestCaseResult((ReportGridDataInterface)objRptPieData, ref iCurrentPage, ref dCurrentPosY, ref strError);
        }

        /// <summary>
        /// Draw a table about test case result
        /// </summary>
        /// <param name="objGridDataForDetail"> data to be draw</param>
        /// <param name="iCurrentPage"> pages </param>
        /// <param name="dYPosFrom"></param>
        /// <param name="strError"></param>
        private bool GenTestCaseResult(ReportGridDataInterface objGridDataForDetail, ref int iCurrentPage, ref double dYPosFrom, ref string strError)
        {
            Logger.Info("GenTestCaseResult",string.Format("Try to draw a table about data :currentPage:[{0}] posyFrom:[{1}] ", iCurrentPage, dYPosFrom));
            if (objGridDataForDetail==null)
            {
                Logger.Error("GenTestCaseResult",strError="Source data is null.");
                return false;
            }
            if (!(objGridDataForDetail is ReportGridDataMasterInterface))
            {
                Logger.Warnning("GenTestCaseResult", strError = "objGridDataForDetail is not ReportGridDataMasterInterface");
                return false;
            }
            ReportGridDataMasterInterface objContrlLoop = (ReportGridDataMasterInterface)objGridDataForDetail;
            int iLoopCnt = objContrlLoop.GetTopLevelLoopCount();
            for (int i=0;i<iLoopCnt;i++)
            {
                objContrlLoop.SetCurrentLoopId(i);
                bool isFetchedData = objGridDataForDetail.BeginFetchRows();
                if (!isFetchedData) continue;
                // draw header
                //List<KeyValuePair<string, int>> lstHeader=objGridDataForDetail.GetGridColumnInfo();
                PDFTableDrawExtand objDrawTableInPdf = new PDFTableDrawExtand();
                objDrawTableInPdf.currentGrph = currentGrph;
                objDrawTableInPdf.currentDocument = this.pdfTargetDocument;
                objDrawTableInPdf.currentPage = currentPage;
                objDrawTableInPdf.xLeft = xLeft;
                objDrawTableInPdf.yTop = (int)dYPosFrom + 1;
                objDrawTableInPdf.HeaderFont = textFont;
                objDrawTableInPdf.DataFont = textFont;
                objDrawTableInPdf.GridData = objGridDataForDetail;
                objDrawTableInPdf.CurrentPageNumber = iCurrentPage;
                objDrawTableInPdf.adjustEventHandler = this.AdjustPageAndGraph;
                //while (objGridDataForDetail.FetchOneRowData)
               
                objDrawTableInPdf.DrawTable(ref strError);
                dYPosFrom = (objDrawTableInPdf.ydCurrentPos) + 1;
                //break;
            }
            return true;
        }

        internal void AdjustPageAndGraph(double dRequiredHeight, ref int iPageNumber, ref double dCurPosY,out PdfPage currPage,out XGraphics objGrph)
        {
            currPage = this.currentPage;
            //if (currentGrph != null)
                objGrph = currentGrph;
            if (dRequiredHeight<= yBottom)
            {
                return;
            }

            ///Steps:
            /// 1, create a new page
            /// 2, create new title and tail
            /// 3, change iPageNubmer, dCurposY
            /// 
            this.currentPage=  this.pdfTargetDocument.AddPage();
            currentPage.Size = PdfSharp.PageSize.A4;
            this.currentGrph = XGraphics.FromPdfPage(this.currentPage);

            CreateLogPageTitle(currentPage, currentGrph);            
            DrawBottomCloseLine(currentPage, ref currentGrph, ++iPageNumber);

            dCurPosY = yTop + 5;
            currPage = this.currentPage;
            objGrph = currentGrph;
        }

        
    }

    internal class PDFTableDrawExtand
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(PDFTableDrawExtand));
        private XGraphics _currentGrph;
        public XGraphics currentGrph { get { return _currentGrph; } set { this._currentGrph = value; } }
        private PdfPage _currentPage = null;
        public PdfPage currentPage { get { return _currentPage; } set { _currentPage = value; } }
        public PdfDocument currentDocument { get; set; }
        public List<KeyValuePair<string, int>> columnDictionaryInfo { get; set; }
        public ReportGridDataInterface GridData;
        public int xLeft { get; set; }
        public int yTop { get; set; }
        public XFont HeaderFont { get; set; }
        public XFont DataFont { get; set; }

        public int yLastPos { get; set; }

        internal AdjustPageEnvent adjustEventHandler = null;
        private int _currentPageNumber;
        public int CurrentPageNumber { get { return _currentPageNumber; } set { _currentPageNumber = value; } }


        protected List<int> columnsWidth = new List<int>();

        public bool DrawTable(ref string strError)
        {
            Logger.logBegin("DrawTable");

            bool isRight = DrawHeader(ref strError);
            if (!isRight)
            {
                Logger.Error("DrawTable",string.Format("Error when Generate Table Header:[{0}]",strError));
                return false;
            }

            /// draw data rows
            /// 
            List<KeyValuePair<string, string>> dicRowData = null;
            int i = 0;
            while ((dicRowData=this.GridData.FetchOneRowData())!=null)
            {
                ///Draw one row
                /// 
                isRight = DrawDataRow(dicRowData, ref this.ydCurrentPos, ref strError, i%2==0?XBrushes.White:XBrushes.LightBlue);
                byte[] arrImg = GridData.GetExtendImgData();
                if ((arrImg!=null))
                {
                    if(arrImg.Length>0)
                    ///draw an empty row and draw a pic
                    /// 
                    DrawPictureRow(arrImg, ref this.ydCurrentPos, ref strError, i % 2 == 0 ? XBrushes.White : XBrushes.LightBlue);
                }
                i++;
                
            }
            this.GridData.EndFetchRow();
            return isRight;
        }

        private bool DrawPictureRow(byte[] arrImg, ref double ydCurrentPos, ref string strError, XBrush backGround=null)
        {
            Logger.Info("DrawPictureRow",string.Format("Pic Len:[{0}], ydCurrentPos:[{1}]", arrImg==null?0:arrImg.Length, ydCurrentPos));
            if (arrImg == null) return false;
            ///steps:
            /// 1, get img's height, which is png, save to an temp file
            /// 2, draw a empty 
            /// 
            string strTmpFileName = Guid.NewGuid().ToString() + ".png";
            strTmpFileName = Path.Combine(MarsReportGen.TempPicturePath, strTmpFileName);
            FileStream objImgStream = File.Open(strTmpFileName, FileMode.CreateNew);
            objImgStream.Seek(0, SeekOrigin.Begin);
            objImgStream.Write(arrImg, 0, arrImg.Length);
            objImgStream.Close();

            XImage objImg = XImage.FromFile(strTmpFileName);
            double dH = objImg.PointHeight,
                dW = objImg.PointWidth,
                dWScale =1,dHScale=1;
            if (dW> MarsReportGen.GetPageDefaultSize().Width)
            {
                dWScale = dW / MarsReportGen.GetPageDefaultSize().Width;
                //dH /= dScale;
            }
            if (dH> MarsReportGen.GetPageDefaultSize().Height)
            {
                dHScale = dH / MarsReportGen.GetPageDefaultSize().Height;
            }
            dHScale = Math.Max(dHScale, dWScale);
            dH /= dHScale;
            dW /= dHScale;
            
            double yTop = ydCurrentPos+dH;

            if (adjustEventHandler != null)
            {
                adjustEventHandler(yTop, ref this._currentPageNumber, ref yTop, out this._currentPage, out this._currentGrph);
            }
            /// draw a empty rectangle
            /// 
            int iWdth = columnsWidth.Sum();
            int yiTop = (int)yTop;
            List<XRect> lstTargetCellRect = new List<XRect>();
            DrawEmptyRow(dH, this.xLeft, ref yiTop, true, backGround, new List<int>() { iWdth }, ref lstTargetCellRect);

            /// draw a picture at the left top of the first cell
            /// 
            //if (lstTargetCellRect.Count<=0)
            //{

            //}
            XRect rectTarget = new XRect(lstTargetCellRect[0].Left + 2, lstTargetCellRect[0].Top + 2, lstTargetCellRect[0].Width - 4, lstTargetCellRect[0].Height - 4);
            this._currentGrph.DrawImage(objImg, rectTarget);
            ydCurrentPos = yiTop + dH+ 1;
            try
            {
                objImg.Dispose();
                objImg = null;
                File.Delete(strTmpFileName);
            }
            catch (Exception e)
            {
                Logger.Warnning("DrawPictureRow", string.Format("Exception:[{0}] when delete target file:[{1}]", e.Message, strTmpFileName));
            }
            return true;
        }

        private bool DrawDataRow(List<KeyValuePair<string, string>> dicRowData, ref double yTop,ref string strError, XBrush backGroud=null)
        {
            Logger.Info("DrawDataRow",string.Format("Data count to create:[{0}]", dicRowData==null?0:dicRowData.Count));
            ///steps:
            /// 1, get the height of row
            double dRightHeight = GetDataRowHeight(dicRowData);

            /// 2, draw outer border
            /// 
            List<XRect> lstTargetCellPos = new List<XRect>();
            int yiTop = (int)yTop;

            ///changed on 2017-08-14 to avoid long strings
            /// 
            
            DrawEmptyRow(dRightHeight + cnst_marginForCell*2, this.xLeft, ref yiTop, true, backGroud, this.columnsWidth, ref lstTargetCellPos);
            yTop = yiTop;
            yTop += (dRightHeight + cnst_marginForCell * 2);
            if(adjustEventHandler!=null)
            {
                adjustEventHandler(yTop, ref this._currentPageNumber, ref yTop,out this._currentPage, out this._currentGrph);
            }

            /// 3, Write string to target cells
            /// 
            List<string> lstData = new List<string>();
            foreach(var objItm in dicRowData)
            {
                if (objItm.Equals(default(KeyValuePair<string, string>))) continue;
                string strItm = objItm.Value;
                lstData.Add(strItm);
            }
            bool isRight = WriteCells4Row(lstTargetCellPos, lstData, ref strError,false);
            if (!isRight)
            {
                Logger.Error("DrawDataRow",string.Format("Error when call WriteCells4Row :[{0}]", strError));
                return false;
            }
            return true;
        }

        private double GetDataRowHeight(List<KeyValuePair<string, string>> dicRowData)
        {
            Logger.Info("GetDataRowHeight","to caculate height of data row");
            XTextFormatter tf = new XTextFormatter(this.currentGrph);
            double dRslt = -int.MaxValue, dCurColumnHeight=0.0;
            int iW = 0;
            foreach (var objItm in dicRowData)
            {
                if (objItm.Equals(default(List<KeyValuePair<string, string>>))) continue;
                if (!checkWidthByColumnName(objItm.Key, this.columnDictionaryInfo, ref iW))
                {
                    continue;
                }
                tf.PrepareDrawStringByTiger(objItm.Value, this.DataFont, XBrushes.LightGray, new XRect(10, 10, iW-cnst_marginCell4Hor*4, 10), XStringFormats.TopLeft, out dCurColumnHeight);
                Logger.Info("GetDataRowHeight",string.Format("Data [{0}] get Height:[{1}]", objItm.Value, dCurColumnHeight));
                //tf.PrepareDrawString(objItm.Value, this.DataFont, XBrushes.LightGray, new XRect(10, 10, iW, 10), XStringFormats.TopLeft, out dCurColumnHeight);
                dRslt = Math.Max(dRslt, dCurColumnHeight);
            }
            return dRslt;
        }
        private bool checkWidthByColumnName(string strColumnCaption, List<KeyValuePair<string, int>> objTargetList, ref int iW)
        {
            string strCmpCap = strColumnCaption ?? "";
            foreach (var objItm in objTargetList)
            {
                if (objItm.Equals(default(KeyValuePair<string, int>))) continue;
                if (string.Compare(strCmpCap, objItm.Key,true)==0)
                {
                    iW = objItm.Value- (int)(cnst_marginForCell*2);
                    return true;
                }
            }
            return false; 
        }

        public double ydCurrentPos;
        private bool DrawHeader(ref string strError)
        {
            ydCurrentPos = yTop;

            double xdLeft = xLeft;
            columnDictionaryInfo = GridData.GetGridColumnInfo();
            bool isRight = DrawHeaderRow(columnDictionaryInfo, ref ydCurrentPos,ref strError);
            return isRight;
        }

        private const double cnst_marginForCell = 1.0;
        private const double cnst_marginCell4Hor = 1.0;
        private bool DrawHeaderRow(List<KeyValuePair<string, int>> dataWithWidth,ref double yTopStart, ref string strError)
        {
            //List<int> lstWdth = new List<int>();
            columnsWidth.Clear();
            XTextFormatter tf = new XTextFormatter(this.currentGrph);
            /// adjust height based cell value
            /// 
            double dCellMaxHeight = -int.MaxValue,dTmpHeight=0.0;
            foreach (var objInfo in dataWithWidth)
            {
                if (objInfo.Equals(default(KeyValuePair<string, int>))) continue;
                string strInfo = objInfo.Key;
                columnsWidth.Add(objInfo.Value);

                tf.PrepareDrawString(strInfo, this.HeaderFont, XBrushes.Black,new XRect(0,0, objInfo.Value- cnst_marginForCell*2, 10), XStringFormats.TopLeft, out dTmpHeight);
                dCellMaxHeight = Math.Max(dCellMaxHeight, dTmpHeight);
            }
            XSize objSz = this.currentGrph.MeasureString("oOg",this.HeaderFont);
            List<XRect> lstTargetCellPos = new List<XRect>();
            int yiTop = (int)yTop;
            DrawEmptyRow(dCellMaxHeight + cnst_marginForCell*2, xLeft,ref yiTop, false,XBrushes.LightGray, columnsWidth, ref lstTargetCellPos);
            yTop = yiTop;
            bool isRight = WriteCells4Row(lstTargetCellPos, (from q in dataWithWidth select q.Key).ToList(),ref strError);
            yTopStart = yTop + dCellMaxHeight + cnst_marginForCell * 2;
            return isRight;
        }

        private bool WriteCells4Row(List<XRect> lstTargetCellPos, List<string> lstCaption,ref string strError,bool isWriteHeader=true)
        {
            Logger.logBegin("WriteCells4Row");
            if (lstTargetCellPos.Count!=lstCaption.Count)
            {
                Logger.Error("WriteCells4Row", strError= string.Format("Cells count setting aren't right. Captions list is [{0}],but Header cells' count is :[{1}]", lstCaption.Count,lstTargetCellPos.Count));
                return false; 
            }

            for (int i=0;i<lstCaption.Count;i++)
            {
                if (isWriteHeader)
                {
                    MarsReportGen.DrawStringCenterOnRect(lstCaption[i], this.currentPage, lstTargetCellPos[i], this.DataFont, this.currentGrph);
                }
                else
                {
                    double dNeedHeight = 0.0;
                    XRect rectTmp = new XRect(lstTargetCellPos[i].X + cnst_marginCell4Hor*2, lstTargetCellPos[i].Y, lstTargetCellPos[i].Width-cnst_marginCell4Hor*2, lstTargetCellPos[i].Height);
                    MarsReportGen.WriteMultRowsInsideOfARectTiger(lstCaption[i], rectTmp, this.currentPage, ref this._currentGrph, XStringFormats.TopLeft, this.DataFont,out dNeedHeight);
                    //MarsReportGen.WriteMultRowsInsideOfARect(lstCaption[i], lstTargetCellPos[i], this.currentPage, ref this._currentGrph, XStringFormats.TopLeft, this.DataFont, out dNeedHeight);
                }
            }
            return true;
        }

        private void DrawEmptyRow(double dHeight, int xdLeft, ref int yTop, bool isToDrawBorder, XBrush bkBrush,List<int> cellsWidth, ref List<XRect> lstTargetCellPos)
        {
            Logger.logBegin("DrawEmptyRow");
            int iWidth = 0;
            for (int i=0;i<cellsWidth.Count;i++)
            {
                iWidth += cellsWidth[i];
            }
            XPen objP = new XPen(XColors.Black, 0.5);
            /// draw a rectangle
            /// adjust height            
            /// 
            double dCurrentYPos = yTop;
            if (this.adjustEventHandler!=null)
            {
                this.adjustEventHandler(yTop + dHeight, ref this._currentPageNumber, ref dCurrentYPos, out this._currentPage, out this._currentGrph);
                yTop = (int)dCurrentYPos;
            }
            this.currentGrph.DrawRectangle(objP, bkBrush, new XRect(xLeft, yTop, iWidth, dHeight)) ;
            /// draw split lines
            /// 
            double dX=xdLeft, dW, dY=yTop;
            objP.Color = XColors.DarkGray;
            for (int i = 1; i < cellsWidth.Count; i++)
            {
                XRect xrecCur = new XRect(dX+ cnst_marginForCell, dY + cnst_marginForCell, dW=cellsWidth[i - 1]- cnst_marginForCell, dHeight - cnst_marginForCell);
                lstTargetCellPos.Add(xrecCur);
                //if (i!= cellsWidth.Count-1)
                    this.currentGrph.DrawLine(objP, new XPoint(dX+ cellsWidth[i - 1], dY), new XPoint(dX+ cellsWidth[i - 1], dY + dHeight));
                dX += dW;
            }
            if (cellsWidth.Count>=1)
                lstTargetCellPos.Add(new XRect(dX+ cnst_marginForCell, dY + cnst_marginForCell, cellsWidth[cellsWidth.Count - 1]- cnst_marginForCell, dHeight - cnst_marginForCell));
        }

        

    }

    public class MARSRPT_Summary
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TestResult { get; set; }
        public int TestSuiteCount;
        public int TestCaseCount;
        public int TestStepCount;

    }

    internal class MarsTigerScreenShotMgr
    {
        private PdfDocument parentDocumentPdf;
        private PdfPage currentScreenPage;
        private XGraphics currentGrphicsForCurrentPage;
        private double currentYPos = 0.0;
        public PdfDocument ParentDocumentPdf
        {
            set
            {
                parentDocumentPdf = value;
                currentScreenPage = new PdfPage();
                currentGrphicsForCurrentPage = XGraphics.FromPdfPage(currentScreenPage);
                ///Write Title
                /// 
                WriteHeaderOfScreenShot();
            }
            get
            {
                return parentDocumentPdf;
            }
        }

        const string cnst_Pic_PageHeader = "•  Screenshot Details";
        private void WriteHeaderOfScreenShot()
        {
            XPoint topLeft = MarsReportGen.GetPageDefaultTopLeft();
            XSize objSize = MarsReportGen.GetPageDefaultSize();
            double dwHeight = 0.0d;
            MarsReportGen.WriteMultRowsInsideOfARectTiger(cnst_Pic_PageHeader, new XRect(topLeft.X, topLeft.Y, objSize.Width, objSize.Height), currentScreenPage,
                ref currentGrphicsForCurrentPage, XStringFormats.TopLeft, MarsReportGen.textFont18Bold, out dwHeight);
            currentYPos = topLeft.Y + dwHeight + MarsReportGen.cnst_rowDis;

        }

    }

}
