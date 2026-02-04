using System;
using System.Drawing;
using System.Windows.Forms;
using TestFrameMonitor.Form.Info;

#if _VEDIO_TIGER_
using TestFrameMonitor.source.serializeration;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using TestFrameMonitor.source.media;
#endif

namespace TestFrameMonitor
{
    public delegate void SubMonitorFormCloseEvent();

    public partial class MarsTDetailMgrForm : System.Windows.Forms.Form
    {
        //private MarsPlayer m_objMarsPlayer = null;
#if _VEDIO_TIGER_
        private static MLogger logger = MLogger.GetLogger(typeof(MarsTDetailMgrForm));
        private Timer playingTimer = new Timer();
        public TimeSpan endPlayTime { get; set; }
#endif
        public MarsTDetailMgrForm()
        {
            InitializeComponent();
        }

#if _VEDIO_TIGER_
        public SubMonitorFormCloseEvent subMonitorFormHandler = null;
#endif

        protected void UpdateStatusInfo(string strError, bool isError)
        {
            Color clrFont = isError ? Color.Red : Color.Black;
            this.toolStripStatusLabel1.ForeColor = clrFont;
            this.toolStripStatusLabel1.Text = strError;
        }

        private void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {
#if _VEDIO_TIGER_
            if (tabControl1.SelectedIndex == 0)
            {
                // Load all list to treeview
                Dictionary<string, List<MarsXmlVedioIndex>> hshVedioFiles = new Dictionary<string, List<MarsXmlVedioIndex>>();
                string strError = "";
                bool isRight = RefreshAllVedioInfo(ref hshVedioFiles, ref strError);
                if (!isRight)
                {
                    /** Write Error message to status **/
                    UpdateStatusInfo(strError, true);
                    return;
                }
                else
                {
                    UpdateStatusInfo("New Vedio files are loaded", false);
                }
                /** Refresh Tree view **/
                RefreshTreeviewForAllVedioIndex(hshVedioFiles);
            }
#endif
        }

        private void MarsTDetailMgrForm_Load(object sender, EventArgs e)
        {
            RemoveUnImplementTabPages();
            this.splitContainer1.FixedPanel = FixedPanel.Panel1;
            tabControl1_TabIndexChanged(this.tabControl1, null);
#if _VEDIO_TIGER_
            this.axWindowsMediaPlayer1.stretchToFit = true;
#endif
            this.toolStripComboBox1.SelectedIndex = 2;// default value 
        }




        private void PlayerLoadedEvent(object sender, EventArgs e)
        {

        }

        private void RemoveUnImplementTabPages()
        {
            this.tabControl1.TabPages.Remove(this.tabRpt);
            this.tabControl1.TabPages.Remove(this.tabUsersInfo);
            this.tabControl1.TabPages.Remove(this.tabLocalTestCasesManage);
        }

#if _VEDIO_TIGER_
        /// <summary>
        /// Get all latested vedios index files from \vedio\index
        ///
        /// </summary>
        private bool RefreshAllVedioInfo(ref Dictionary<string, List<MarsXmlVedioIndex>> hshVedioFiles, ref string strError)
        {
            /***
             * Steps:
             * 1, get path of index from current assembly location
             * 2, navigate all files
             * 3, get files to dictionary orgnized by date
             **/
            /**1, get path of index from current assembly location**/
            string strIndexPath = typeof(MarsTDetailMgrForm).Assembly.Location;
            strIndexPath = TigerMarsUtil.GetPathWithoutFileName(strIndexPath);
            strIndexPath = string.Format("{0}\\..\\results\\Index", strIndexPath);
            try
            {
                /**2, navigate all files**/
                string[] arrFiles = Directory.GetFiles(strIndexPath, "*.mti", SearchOption.TopDirectoryOnly);
                foreach (string strOneFile in arrFiles)
                {
                    DateTime dtFileDate = File.GetLastWriteTime(strOneFile);
                    List<MarsXmlVedioIndex> lstIndexXmlFileName = null;
                    /** 3, get files to dictionary orgnized by date **/
                    string strDt = dtFileDate.ToString("yyyy-MM-dd");
                    if (hshVedioFiles.ContainsKey(strDt))
                    {
                        lstIndexXmlFileName = hshVedioFiles[strDt];
                    }
                    else
                    {
                        lstIndexXmlFileName = new List<MarsXmlVedioIndex>();
                        hshVedioFiles.Add(strDt, lstIndexXmlFileName);
                    }
                    MarsXmlVedioIndex objIndx = MarsXmlVedioIndex.Import(strOneFile);
                    if (objIndx != null)
                    {
                        lstIndexXmlFileName.Add(objIndx);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                logger.Error("RefreshAllVedioInfo", strError = string.Format("Exceptions:[{0}] when try to get all index files from [{1}]", e.Message, strIndexPath),e);
                return false;
            }
            
        }

        private void RefreshTreeviewForAllVedioIndex(Dictionary<string, List<MarsXmlVedioIndex>> hshVedioFiles)
        {
            this.trvwStpAndTCVedios.BeginUpdate();
            try
            {
                this.trvwStpAndTCVedios.Nodes.Clear();
                List<string> lstDt = hshVedioFiles.Keys.ToList<string>();
                lstDt.Sort();
                foreach(string strKey in lstDt)
                {
                    TreeNode objCurrentRoot = this.trvwStpAndTCVedios.Nodes.Add(strKey);
                    objCurrentRoot.ImageIndex = 0;
                    if (objCurrentRoot == null) continue;
                    List<MarsXmlVedioIndex> lstXml = hshVedioFiles[strKey];
                    foreach (MarsXmlVedioIndex objVedio in lstXml)
                    {
                        if (objVedio == null) continue;
                        /**insert node for all steps**/
                        TreeNode objTC = new TreeNode();
                        objTC.Text = string.Format("[{0}]-{1}",objVedio.TestSuiteName,objVedio.TestStartTime==null?"[N/A]":string.Format("[{0}]",objVedio.TestStartTime.ToString("hh:MM:ss")));
                        objTC.Tag = objVedio;
                        objTC.ToolTipText = objVedio.GetTCSummary();
                        objCurrentRoot.Nodes.Add(objTC);
                        objTC.ImageIndex = 1;
                        objTC.Tag = objVedio;
                        /** Insert all test steps to the nodes **/
                        foreach (MarsTigerXmlReportItem objXmlItm in objVedio.TestSteps)
                        {
                            TreeNode objDetail = new TreeNode();
                            objDetail.Text = objXmlItm.GetSummary();
                            objTC.Nodes.Add(objDetail);
                            objDetail.ImageIndex = 2;
                            objDetail.Tag = new object[] { objVedio, objXmlItm };
                        }
                    }
                }
            }
            catch(Exception e)
            {
                logger.Error("RefreshTreeviewForAllVedioIndex", string.Format("Exceptions:[{0}], when refresh treeview of Vedio",e.Message),e);
            }
            finally
            {
                this.trvwStpAndTCVedios.EndUpdate();
            }
        }

        private void PlayAFile(string strFileName, MarsXmlVedioIndex objXmlIndex, TimeSpan tsStartTime = default(TimeSpan), TimeSpan tsEndTime= default(TimeSpan))
        {
            try
            {
                /** Initialization part **/
                playingTimer.Enabled = false;
                playingTimer.Tag = null;
                playingTimer.Tick -= renderSubtitles;
                axWindowsMediaPlayer1.PlayStateChange -= AxWindowsMediaPlayer1_PlayStateChange;
                endPlayTime = default(TimeSpan);
                //this.axWindowsMediaPlayer1.PositionChange -= AxWindowsMediaPlayer1_PositionChange;
                if (!File.Exists(strFileName)) return;
                
                this.axWindowsMediaPlayer1.URL = strFileName;
                
                //axWindowsMediaPlayer1.closedCaption.                
                if ((tsStartTime == default(TimeSpan)) && (tsEndTime == default(TimeSpan)))
                {
                    
                    playingTimer.Tick += renderSubtitles;
                    playingTimer.Interval = 100;
                    playingTimer.Tag = objXmlIndex;
                    axWindowsMediaPlayer1.PlayStateChange += AxWindowsMediaPlayer1_PlayStateChange;
                    //this.axWindowsMediaPlayer1.PositionChange
                    this.axWindowsMediaPlayer1.Ctlcontrols.play();
                    playingTimer.Enabled = true;
                    return;
                }
                if (tsEndTime==default(TimeSpan))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition = tsStartTime.TotalSeconds;
                    playingTimer.Interval = 50;
                    playingTimer.Tag = objXmlIndex;
                    playingTimer.Tick += renderSubtitles;
                    axWindowsMediaPlayer1.PlayStateChange += AxWindowsMediaPlayer1_PlayStateChange;
                    
                    this.axWindowsMediaPlayer1.Ctlcontrols.play();
                    playingTimer.Enabled = true;
                    return;
                }

                if (tsEndTime!=default(TimeSpan))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition = tsStartTime.TotalSeconds;
                    playingTimer.Interval = 50;
                    playingTimer.Tag = objXmlIndex;
                    playingTimer.Tick += renderSubtitles;
                    axWindowsMediaPlayer1.PlayStateChange += AxWindowsMediaPlayer1_PlayStateChange;
                    endPlayTime = tsEndTime;

                    this.axWindowsMediaPlayer1.Ctlcontrols.play();
                    playingTimer.Enabled = true;
                }
                
            }
            catch (Exception e)
            {
                logger.Error("PlayAFile", string.Format("Exceptions:[{0}], When play a file:[{1}]", e.Message, strFileName));                
            }
        }
#if _VEDIO_TIGER_
        private void AxWindowsMediaPlayer1_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {

            if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                if (this.endPlayTime==default(TimeSpan))
                    playingTimer.Enabled = true;
                else
                {
                    if (this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition > this.endPlayTime.TotalSeconds)
                        playingTimer.Enabled = false;
                }
                
            }
            else
            {
                playingTimer.Enabled = false;
            }

        }
#endif

        private void renderSubtitles(object sender, EventArgs e)
        {
            try
            {
                double dCurrnt = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
                if (this.playingTimer.Tag == null) return;
                MarsXmlVedioIndex objIdx = (MarsXmlVedioIndex)this.playingTimer.Tag;
                string strCaption = objIdx.GetCaptionByRelativePosition(dCurrnt);
                this.lblCaption.Text = strCaption;
            }
            catch (Exception)
            {

                
            }
            
        }

        private void AxWindowsMediaPlayer1_PositionChange(object sender, AxWMPLib._WMPOCXEvents_PositionChangeEvent e)
        {
            
        }

        private void trvwStpAndTCVedios_DoubleClick(object sender, EventArgs e)
        {
            /***  ***/
            if (this.trvwStpAndTCVedios.SelectedNode == null) return;
            TreeNode objSelected = this.trvwStpAndTCVedios.SelectedNode;
            
            if (objSelected == null) return;


            if (objSelected.Tag == null) return;
            if (objSelected.Tag is MarsXmlVedioIndex)
            {
                /**play all the test vedio**/
                MarsXmlVedioIndex objTC = (MarsXmlVedioIndex)objSelected.Tag;
                string strFile = objTC.GetSaveFileWithPath();
                string strVedioFile = objTC.GetAssociatedVedioFile();
                
                PlayAFile(strVedioFile, objTC);
                ChangePlaySpeed(null, null);
                return;
            }

            if (objSelected.Tag is object[])
            {
                MarsXmlVedioIndex dtTestC = (MarsXmlVedioIndex)((object[])objSelected.Tag)[0];
                MarsTigerXmlReportItem objItm = (MarsTigerXmlReportItem)((object[])objSelected.Tag)[1];
                ReplayItem(dtTestC,objItm);
                ChangePlaySpeed(null, null);
                return;
            }
        }

        public void ReplayItem(MarsXmlVedioIndex dtTestC, MarsTigerXmlReportItem objItm )
        {
            /** Play special Item **/
            string strFile = dtTestC.GetSaveFileWithPath();
            string strVedioFile = dtTestC.GetAssociatedVedioFile();

            PlayAFile(strVedioFile, dtTestC, objItm.StartTime - dtTestC.TestStartTime, objItm.EndTime - dtTestC.TestStartTime);
        }

        private void MarsTDetailMgrForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.playingTimer.Enabled = false;
            if (this.subMonitorFormHandler == null) return;
            this.subMonitorFormHandler();
        }

        

        private void tlprcSpeedBar_Click(object sender, EventArgs e)
        {
            
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void ChangePlaySpeed(object sender, EventArgs e)
        {
            double[] arrRate = {0.25, 0.5, 1, 2, 4 };
            if (this.axWindowsMediaPlayer1.mediaCollection.getAll().count == 0) return;
            if (this.axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                this.axWindowsMediaPlayer1.settings.rate = arrRate[toolStripComboBox1.SelectedIndex];
            }
        }

        

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode objNodeSelect = null;
            if ((objNodeSelect=this.trvwStpAndTCVedios.SelectedNode) == null)
            {
                MessageBox.Show("Please select a Node first. ", "MARS Hint...");
                return;
            }
            if (MessageBox.Show("Are you sure to delete all assigned Files?", "MARS Hint...", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            /// Delete selected vedio information
            /// Steps:
            /// 1, delete index files
            /// 2, delete vedio files

            /** 1, Delete index files **/
            while (objNodeSelect.Level > 1)
            {
                objNodeSelect = objNodeSelect.Parent;
            }
            if (!(objNodeSelect.Tag is MarsXmlVedioIndex)) return;
            MarsXmlVedioIndex objSlctIdx = (MarsXmlVedioIndex)objNodeSelect.Tag;
            string strVedioFile = objSlctIdx.GetAssociatedVedioFile();
            string strIdxFile = objSlctIdx.GetSaveFileWithPath();
            string strOriginalVFile = objSlctIdx.GetVedioOrignalFileName();

            /** delete the node **/
            this.trvwStpAndTCVedios.Nodes.Remove(objNodeSelect);
            DeleteVedioFile(strIdxFile);
            DeleteVedioFile(strVedioFile);
            DeleteVedioFile(strVedioFile);
            
        }

        private bool DeleteVedioFile(string strFileName)
        {
            if (File.Exists(strFileName))
            {
                try
                {
                    File.Delete(strFileName);
                    return true;
                }
                catch (Exception e)
                {
                    logger.Error("deleteToolStripMenuItem_Click", string.Format("Can't delete file:[{0}], \r\nException:[{1}]", strFileName, e.Message), e);
                    return false;
                }
            }
            return true;
        }

        

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Pause_ButtonClick",string.Format("Exeptions:[{0}]", ex.Message),ex);
            }
            
        }
#else
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void ChangePlaySpeed(object sender, EventArgs e)
        {

        }
        private void MarsTDetailMgrForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
        }
        private void trvwStpAndTCVedios_DoubleClick(object sender, EventArgs e)
        {
        }
#endif
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FrmAbout objFrm = new FrmAbout())
            {
                objFrm.ShowDialog();
            }
        }
    }
}
