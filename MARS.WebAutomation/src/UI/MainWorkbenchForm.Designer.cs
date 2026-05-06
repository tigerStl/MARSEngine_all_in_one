namespace MARS.WebAutomation.UI
{
    partial class MainWorkbenchForm
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuFileSave;
        private System.Windows.Forms.ToolStripMenuItem menuFileExport;
        private System.Windows.Forms.ToolStripMenuItem menuFileImport;
        private System.Windows.Forms.ToolStripSeparator menuFileSep;
        private System.Windows.Forms.ToolStripMenuItem menuFileExit;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLanguage;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLangEnglish;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLangChinese;
        private System.Windows.Forms.ToolStripMenuItem menuHelpAbout;
        private System.Windows.Forms.ToolStrip toolMain;
        private System.Windows.Forms.ToolStrip toolPerf;
        private System.Windows.Forms.ToolStripButton tsbTarget;
        private System.Windows.Forms.ToolStripButton tsbRecord;
        private System.Windows.Forms.ToolStripButton tsbReplay;
        private System.Windows.Forms.ToolStripSeparator tsbSep1;
        private System.Windows.Forms.ToolStripButton tsbExport;
        private System.Windows.Forms.ToolStripButton tsbImport;
        private System.Windows.Forms.ToolStripSeparator tsbSep2;
        private System.Windows.Forms.ToolStripButton tsbSave;
        private System.Windows.Forms.ToolStripSeparator tsbSepReload;
        private System.Windows.Forms.ToolStripButton tsbReloadEngine;
        private System.Windows.Forms.ToolStripSeparator tsbSepSync;
        private System.Windows.Forms.CheckBox chkSyncFocus;
        private System.Windows.Forms.ToolStripControlHost tsbSyncHost;
        private System.Windows.Forms.ToolStripSeparator tsbSepPerf;
        private System.Windows.Forms.CheckBox chkWithPerformanceTest;
        private System.Windows.Forms.ToolStripControlHost tsbPerfHost;
        private System.Windows.Forms.ToolStripButton tsbRunPerf;
        private System.Windows.Forms.ToolStripButton tsbStopPerf;
        private System.Windows.Forms.ToolStripButton tsbRunPerfSelected;
        private System.Windows.Forms.ToolStripLabel tslBrand;
        private System.Windows.Forms.ToolStripSeparator tsbSepBrand;
        private System.Windows.Forms.StatusStrip statusMain;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabTarget;
        private System.Windows.Forms.Panel panelTargetCard;
        private System.Windows.Forms.TabPage tabObjects;
        private System.Windows.Forms.TabPage tabRecord;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.SplitContainer splitRecordMainPreview;
        private System.Windows.Forms.SplitContainer splitRecordWorkPreview;
        private System.Windows.Forms.Panel panelRecordCanvasPreview;
        private System.Windows.Forms.Label lblRecordCanvasPreview;
        private System.Windows.Forms.Panel panelRecordPerfPreview;
        private System.Windows.Forms.SplitContainer splitRecordPerfPreview;
        private System.Windows.Forms.DataGridView gridPerfRuntimePreview;
        private System.Windows.Forms.Label lblPerfDesignTitle;
        private System.Windows.Forms.Label lblPerfDesignRuntime;
        private System.Windows.Forms.Panel panelObjectsToolbar;
        private System.Windows.Forms.FlowLayoutPanel flowObjectsToolbar;
        private System.Windows.Forms.TextBox txtTreeSearch;
        private System.Windows.Forms.Button btnTreeSearchGo;
        private System.Windows.Forms.Button btnTreeSearchPrev;
        private System.Windows.Forms.Button btnTreeSearchNext;
        private System.Windows.Forms.CheckBox chkTreeRegex;
        private System.Windows.Forms.SplitContainer splitObjects;
        private System.Windows.Forms.TreeView treeObjects;
        private System.Windows.Forms.DataGridView gridObjectProps;
        private System.Windows.Forms.Button btnRefreshTree;
        private System.Windows.Forms.TableLayoutPanel layoutSettings;
        private System.Windows.Forms.Label lblDataRoot;
        private System.Windows.Forms.TextBox txtDataRoot;
        private System.Windows.Forms.CheckBox chkHeadless;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.CheckBox chkPersistHeaders;
        private System.Windows.Forms.Label lblChannel;
        private System.Windows.Forms.TextBox txtBrowserChannel;
        private System.Windows.Forms.Label lblViewport;
        private System.Windows.Forms.NumericUpDown numViewportW;
        private System.Windows.Forms.NumericUpDown numViewportH;
        private System.Windows.Forms.Button btnSaveSettings;

        private void InitializeComponent()
        {
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileExport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileImport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSep = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLanguage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLangEnglish = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLangChinese = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolMain = new System.Windows.Forms.ToolStrip();
            this.toolPerf = new System.Windows.Forms.ToolStrip();
            this.tslBrand = new System.Windows.Forms.ToolStripLabel();
            this.tsbSepBrand = new System.Windows.Forms.ToolStripSeparator();
            this.tsbTarget = new System.Windows.Forms.ToolStripButton();
            this.tsbRecord = new System.Windows.Forms.ToolStripButton();
            this.tsbReplay = new System.Windows.Forms.ToolStripButton();
            this.tsbSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripButton();
            this.tsbImport = new System.Windows.Forms.ToolStripButton();
            this.tsbSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbSepReload = new System.Windows.Forms.ToolStripSeparator();
            this.tsbReloadEngine = new System.Windows.Forms.ToolStripButton();
            this.tsbSepSync = new System.Windows.Forms.ToolStripSeparator();
            this.chkSyncFocus = new System.Windows.Forms.CheckBox();
            this.tsbSyncHost = new System.Windows.Forms.ToolStripControlHost(this.chkSyncFocus);
            this.tsbSepPerf = new System.Windows.Forms.ToolStripSeparator();
            this.chkWithPerformanceTest = new System.Windows.Forms.CheckBox();
            this.tsbPerfHost = new System.Windows.Forms.ToolStripControlHost(this.chkWithPerformanceTest);
            this.tsbRunPerf = new System.Windows.Forms.ToolStripButton();
            this.tsbStopPerf = new System.Windows.Forms.ToolStripButton();
            this.tsbRunPerfSelected = new System.Windows.Forms.ToolStripButton();
            this.statusMain = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabTarget = new System.Windows.Forms.TabPage();
            this.panelTargetCard = new System.Windows.Forms.Panel();
            this.layoutTarget = new System.Windows.Forms.TableLayoutPanel();
            this.lblSectionUrl = new System.Windows.Forms.Label();
            this.btnStartBrowser = new System.Windows.Forms.Button();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.btnNavigate = new System.Windows.Forms.Button();
            this.lblScheme = new System.Windows.Forms.Label();
            this.lblHost = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblQuery = new System.Windows.Forms.Label();
            this.tabObjects = new System.Windows.Forms.TabPage();
            this.splitObjects = new System.Windows.Forms.SplitContainer();
            this.treeObjects = new System.Windows.Forms.TreeView();
            this.gridObjectProps = new System.Windows.Forms.DataGridView();
            this.panelObjectsToolbar = new System.Windows.Forms.Panel();
            this.flowObjectsToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.txtTreeSearch = new System.Windows.Forms.TextBox();
            this.btnTreeSearchGo = new System.Windows.Forms.Button();
            this.btnTreeSearchPrev = new System.Windows.Forms.Button();
            this.btnTreeSearchNext = new System.Windows.Forms.Button();
            this.chkTreeRegex = new System.Windows.Forms.CheckBox();
            this.btnRefreshTree = new System.Windows.Forms.Button();
            this.tabRecord = new System.Windows.Forms.TabPage();
            this.splitRecordMainPreview = new System.Windows.Forms.SplitContainer();
            this.splitRecordWorkPreview = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.gridSteps = new System.Windows.Forms.DataGridView();
            this.colAct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSeq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colElapsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEvt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colData = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBounds = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogical = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLoc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colXp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLocAlt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfRef = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelRecordPerfPreview = new System.Windows.Forms.Panel();
            this.splitRecordPerfPreview = new System.Windows.Forms.SplitContainer();
            this.lblPerfDesignRuntime = new System.Windows.Forms.Label();
            this.gridPerfRuntimePreview = new System.Windows.Forms.DataGridView();
            this.colRtTx = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtOk = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtTotalReq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtFinished = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtRounds = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtTps = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtErr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRtLast = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblPerfDesignTitle = new System.Windows.Forms.Label();
            this.panelRecordCanvasPreview = new System.Windows.Forms.Panel();
            this.lblRecordCanvasPreview = new System.Windows.Forms.Label();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.layoutSettings = new System.Windows.Forms.TableLayoutPanel();
            this.lblDataRoot = new System.Windows.Forms.Label();
            this.txtDataRoot = new System.Windows.Forms.TextBox();
            this.chkHeadless = new System.Windows.Forms.CheckBox();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.numTimeout = new System.Windows.Forms.NumericUpDown();
            this.chkPersistHeaders = new System.Windows.Forms.CheckBox();
            this.lblChannel = new System.Windows.Forms.Label();
            this.txtBrowserChannel = new System.Windows.Forms.TextBox();
            this.lblViewport = new System.Windows.Forms.Label();
            this.numViewportW = new System.Windows.Forms.NumericUpDown();
            this.numViewportH = new System.Windows.Forms.NumericUpDown();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.lblRecordHint = new System.Windows.Forms.Label();
            this.lblPerfDesignAnchorSummary = new System.Windows.Forms.Label();
            this.gridPerfAnchorPreview = new System.Windows.Forms.DataGridView();
            this.colPerfAction = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colPerfAnchor = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colPerfScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfCorr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfMethod = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfParam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfHeader = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfCookie = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfPayload = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfResponse = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPerfValidation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuMain.SuspendLayout();
            this.toolMain.SuspendLayout();
            this.toolPerf.SuspendLayout();
            this.statusMain.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabTarget.SuspendLayout();
            this.panelTargetCard.SuspendLayout();
            this.layoutTarget.SuspendLayout();
            this.tabObjects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitObjects)).BeginInit();
            this.splitObjects.Panel1.SuspendLayout();
            this.splitObjects.Panel2.SuspendLayout();
            this.splitObjects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridObjectProps)).BeginInit();
            this.panelObjectsToolbar.SuspendLayout();
            this.flowObjectsToolbar.SuspendLayout();
            this.tabRecord.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordMainPreview)).BeginInit();
            this.splitRecordMainPreview.Panel1.SuspendLayout();
            this.splitRecordMainPreview.Panel2.SuspendLayout();
            this.splitRecordMainPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordWorkPreview)).BeginInit();
            this.splitRecordWorkPreview.Panel1.SuspendLayout();
            this.splitRecordWorkPreview.Panel2.SuspendLayout();
            this.splitRecordWorkPreview.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSteps)).BeginInit();
            this.panelRecordPerfPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordPerfPreview)).BeginInit();
            this.splitRecordPerfPreview.Panel1.SuspendLayout();
            this.splitRecordPerfPreview.Panel2.SuspendLayout();
            this.splitRecordPerfPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfRuntimePreview)).BeginInit();
            this.panelRecordCanvasPreview.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.layoutSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfAnchorPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // menuMain
            // 
            this.menuMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuHelp});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuMain.Size = new System.Drawing.Size(636, 24);
            this.menuMain.TabIndex = 0;
            this.menuMain.Text = "menuStrip1";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileSave,
            this.menuFileExport,
            this.menuFileImport,
            this.menuFileSep,
            this.menuFileExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(37, 20);
            this.menuFile.Text = "File";
            // 
            // menuFileSave
            // 
            this.menuFileSave.Name = "menuFileSave";
            this.menuFileSave.Size = new System.Drawing.Size(119, 22);
            this.menuFileSave.Text = "Save";
            this.menuFileSave.Click += new System.EventHandler(this.menuFileSave_Click);
            // 
            // menuFileExport
            // 
            this.menuFileExport.Name = "menuFileExport";
            this.menuFileExport.Size = new System.Drawing.Size(119, 22);
            this.menuFileExport.Text = "Export…";
            this.menuFileExport.Click += new System.EventHandler(this.menuFileExport_Click);
            // 
            // menuFileImport
            // 
            this.menuFileImport.Name = "menuFileImport";
            this.menuFileImport.Size = new System.Drawing.Size(119, 22);
            this.menuFileImport.Text = "Import…";
            this.menuFileImport.Click += new System.EventHandler(this.menuFileImport_Click);
            // 
            // menuFileSep
            // 
            this.menuFileSep.Name = "menuFileSep";
            this.menuFileSep.Size = new System.Drawing.Size(116, 6);
            // 
            // menuFileExit
            // 
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.Size = new System.Drawing.Size(119, 22);
            this.menuFileExit.Text = "Exit";
            this.menuFileExit.Click += new System.EventHandler(this.menuFileExit_Click);
            // 
            // menuHelp
            // 
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHelpLanguage,
            this.menuHelpAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(44, 20);
            this.menuHelp.Text = "Help";
            // 
            // menuHelpLanguage
            // 
            this.menuHelpLanguage.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHelpLangEnglish,
            this.menuHelpLangChinese});
            this.menuHelpLanguage.Name = "menuHelpLanguage";
            this.menuHelpLanguage.Size = new System.Drawing.Size(126, 22);
            this.menuHelpLanguage.Text = "Language";
            // 
            // menuHelpLangEnglish
            // 
            this.menuHelpLangEnglish.Name = "menuHelpLangEnglish";
            this.menuHelpLangEnglish.Size = new System.Drawing.Size(134, 22);
            this.menuHelpLangEnglish.Text = "English";
            this.menuHelpLangEnglish.Click += new System.EventHandler(this.menuHelpLangEnglish_Click);
            // 
            // menuHelpLangChinese
            // 
            this.menuHelpLangChinese.Name = "menuHelpLangChinese";
            this.menuHelpLangChinese.Size = new System.Drawing.Size(134, 22);
            this.menuHelpLangChinese.Text = "中文 (简体)";
            this.menuHelpLangChinese.Click += new System.EventHandler(this.menuHelpLangChinese_Click);
            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Name = "menuHelpAbout";
            this.menuHelpAbout.Size = new System.Drawing.Size(126, 22);
            this.menuHelpAbout.Text = "About";
            this.menuHelpAbout.Click += new System.EventHandler(this.menuHelpAbout_Click);
            // 
            // toolMain
            // 
            this.toolMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslBrand,
            this.tsbSepBrand,
            this.tsbTarget,
            this.tsbRecord,
            this.tsbReplay,
            this.tsbSep1,
            this.tsbExport,
            this.tsbImport,
            this.tsbSep2,
            this.tsbSave,
            this.tsbSepReload,
            this.tsbReloadEngine,
            this.tsbSepSync,
            this.tsbSyncHost});
            this.toolMain.Location = new System.Drawing.Point(0, 24);
            this.toolMain.Name = "toolMain";
            this.toolMain.Size = new System.Drawing.Size(636, 25);
            this.toolMain.TabIndex = 1;
            // 
            // toolPerf
            // 
            this.toolPerf.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolPerf.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbSepPerf,
            this.tsbPerfHost,
            this.tsbRunPerf,
            this.tsbStopPerf,
            this.tsbRunPerfSelected});
            this.toolPerf.Location = new System.Drawing.Point(0, 49);
            this.toolPerf.Name = "toolPerf";
            this.toolPerf.Size = new System.Drawing.Size(636, 25);
            this.toolPerf.TabIndex = 4;
            // 
            // tslBrand
            // 
            this.tslBrand.Margin = new System.Windows.Forms.Padding(10, 1, 14, 2);
            this.tslBrand.Name = "tslBrand";
            this.tslBrand.Size = new System.Drawing.Size(133, 22);
            this.tslBrand.Text = "MARS Web Automation";
            // 
            // tsbSepBrand
            // 
            this.tsbSepBrand.Name = "tsbSepBrand";
            this.tsbSepBrand.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbTarget
            // 
            this.tsbTarget.CheckOnClick = true;
            this.tsbTarget.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbTarget.Name = "tsbTarget";
            this.tsbTarget.Size = new System.Drawing.Size(44, 22);
            this.tsbTarget.Text = "Target";
            this.tsbTarget.Click += new System.EventHandler(this.tsbTarget_Click);
            // 
            // tsbRecord
            // 
            this.tsbRecord.CheckOnClick = true;
            this.tsbRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRecord.Name = "tsbRecord";
            this.tsbRecord.Size = new System.Drawing.Size(48, 22);
            this.tsbRecord.Text = "Record";
            this.tsbRecord.Click += new System.EventHandler(this.tsbRecord_Click);
            // 
            // tsbReplay
            // 
            this.tsbReplay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbReplay.Name = "tsbReplay";
            this.tsbReplay.Size = new System.Drawing.Size(46, 22);
            this.tsbReplay.Text = "Replay";
            this.tsbReplay.Click += new System.EventHandler(this.tsbReplay_Click);
            // 
            // tsbSep1
            // 
            this.tsbSep1.Name = "tsbSep1";
            this.tsbSep1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbExport
            // 
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Size = new System.Drawing.Size(44, 22);
            this.tsbExport.Text = "Export";
            this.tsbExport.Click += new System.EventHandler(this.tsbExport_Click);
            // 
            // tsbImport
            // 
            this.tsbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbImport.Name = "tsbImport";
            this.tsbImport.Size = new System.Drawing.Size(47, 22);
            this.tsbImport.Text = "Import";
            this.tsbImport.Click += new System.EventHandler(this.tsbImport_Click);
            // 
            // tsbSep2
            // 
            this.tsbSep2.Name = "tsbSep2";
            this.tsbSep2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbSave
            // 
            this.tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSave.Name = "tsbSave";
            this.tsbSave.Size = new System.Drawing.Size(35, 22);
            this.tsbSave.Text = "Save";
            this.tsbSave.Click += new System.EventHandler(this.tsbSave_Click);
            // 
            // tsbSepReload
            // 
            this.tsbSepReload.Name = "tsbSepReload";
            this.tsbSepReload.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbReloadEngine
            // 
            this.tsbReloadEngine.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.tsbReloadEngine.Name = "tsbReloadEngine";
            this.tsbReloadEngine.Size = new System.Drawing.Size(93, 22);
            this.tsbReloadEngine.Text = "Reload engine";
            this.tsbReloadEngine.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.tsbReloadEngine.Click += new System.EventHandler(this.tsbReloadEngine_Click);
            // 
            // tsbSepSync
            // 
            this.tsbSepSync.Name = "tsbSepSync";
            this.tsbSepSync.Size = new System.Drawing.Size(6, 25);
            // 
            // chkSyncFocus
            // 
            this.chkSyncFocus.AutoSize = true;
            this.chkSyncFocus.Location = new System.Drawing.Point(0, 0);
            this.chkSyncFocus.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.chkSyncFocus.Name = "chkSyncFocus";
            this.chkSyncFocus.Size = new System.Drawing.Size(52, 17);
            this.chkSyncFocus.TabIndex = 0;
            this.chkSyncFocus.Text = "Sync";
            this.chkSyncFocus.UseVisualStyleBackColor = true;
            this.chkSyncFocus.CheckedChanged += new System.EventHandler(this.chkSyncFocus_CheckedChanged);
            // 
            // tsbSyncHost
            // 
            this.tsbSyncHost.AutoSize = false;
            this.tsbSyncHost.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.tsbSyncHost.Name = "tsbSyncHost";
            this.tsbSyncHost.Size = new System.Drawing.Size(120, 22);
            this.tsbSyncHost.Text = "";
            // 
            // tsbSepPerf
            // 
            this.tsbSepPerf.Name = "tsbSepPerf";
            this.tsbSepPerf.Size = new System.Drawing.Size(6, 25);
            // 
            // chkWithPerformanceTest
            // 
            this.chkWithPerformanceTest.AutoSize = true;
            this.chkWithPerformanceTest.Checked = true;
            this.chkWithPerformanceTest.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWithPerformanceTest.Location = new System.Drawing.Point(0, 0);
            this.chkWithPerformanceTest.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.chkWithPerformanceTest.Name = "chkWithPerformanceTest";
            this.chkWithPerformanceTest.Size = new System.Drawing.Size(138, 17);
            this.chkWithPerformanceTest.TabIndex = 0;
            this.chkWithPerformanceTest.Text = "With Performance Test";
            this.chkWithPerformanceTest.UseVisualStyleBackColor = true;
            this.chkWithPerformanceTest.CheckedChanged += new System.EventHandler(this.chkWithPerformanceTest_CheckedChanged);
            // 
            // tsbPerfHost
            // 
            this.tsbPerfHost.AutoSize = false;
            this.tsbPerfHost.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.tsbPerfHost.Name = "tsbPerfHost";
            this.tsbPerfHost.Size = new System.Drawing.Size(190, 22);
            this.tsbPerfHost.Text = "";
            // 
            // tsbRunPerf
            // 
            this.tsbRunPerf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.tsbRunPerf.Name = "tsbRunPerf";
            this.tsbRunPerf.Size = new System.Drawing.Size(67, 22);
            this.tsbRunPerf.Text = "Run Perf";
            this.tsbRunPerf.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.tsbRunPerf.Click += new System.EventHandler(this.tsbRunPerf_Click);
            // 
            // tsbStopPerf
            // 
            this.tsbStopPerf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.tsbStopPerf.Enabled = false;
            this.tsbStopPerf.Name = "tsbStopPerf";
            this.tsbStopPerf.Size = new System.Drawing.Size(47, 22);
            this.tsbStopPerf.Text = "Stop";
            this.tsbStopPerf.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.tsbStopPerf.Click += new System.EventHandler(this.tsbStopPerf_Click);
            // 
            // tsbRunPerfSelected
            // 
            this.tsbRunPerfSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRunPerfSelected.Name = "tsbRunPerfSelected";
            this.tsbRunPerfSelected.Size = new System.Drawing.Size(118, 22);
            this.tsbRunPerfSelected.Text = "Run Selected Anchor";
            this.tsbRunPerfSelected.Click += new System.EventHandler(this.tsbRunPerfSelected_Click);
            // 
            // statusMain
            // 
            this.statusMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusMain.Location = new System.Drawing.Point(0, 475);
            this.statusMain.Name = "statusMain";
            this.statusMain.Size = new System.Drawing.Size(636, 22);
            this.statusMain.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabTarget);
            this.tabMain.Controls.Add(this.tabObjects);
            this.tabMain.Controls.Add(this.tabRecord);
            this.tabMain.Controls.Add(this.tabSettings);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 74);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(636, 426);
            this.tabMain.TabIndex = 3;
            // 
            // tabTarget
            // 
            this.tabTarget.Controls.Add(this.panelTargetCard);
            this.tabTarget.Location = new System.Drawing.Point(4, 22);
            this.tabTarget.Name = "tabTarget";
            this.tabTarget.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.tabTarget.Size = new System.Drawing.Size(628, 400);
            this.tabTarget.TabIndex = 0;
            this.tabTarget.Text = "Target";
            this.tabTarget.UseVisualStyleBackColor = true;
            // 
            // panelTargetCard
            // 
            this.panelTargetCard.BackColor = System.Drawing.Color.White;
            this.panelTargetCard.Controls.Add(this.layoutTarget);
            this.panelTargetCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTargetCard.Location = new System.Drawing.Point(12, 10);
            this.panelTargetCard.Name = "panelTargetCard";
            this.panelTargetCard.Size = new System.Drawing.Size(604, 380);
            this.panelTargetCard.TabIndex = 0;
            // 
            // layoutTarget
            // 
            this.layoutTarget.ColumnCount = 3;
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 141F));
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 141F));
            this.layoutTarget.Controls.Add(this.lblSectionUrl, 0, 0);
            this.layoutTarget.Controls.Add(this.btnStartBrowser, 1, 1);
            this.layoutTarget.Controls.Add(this.txtUrl, 0, 1);
            this.layoutTarget.Controls.Add(this.btnNavigate, 2, 1);
            this.layoutTarget.Controls.Add(this.lblScheme, 0, 2);
            this.layoutTarget.Controls.Add(this.lblHost, 0, 3);
            this.layoutTarget.Controls.Add(this.lblPort, 0, 4);
            this.layoutTarget.Controls.Add(this.lblPath, 0, 5);
            this.layoutTarget.Controls.Add(this.lblQuery, 0, 6);
            this.layoutTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutTarget.Location = new System.Drawing.Point(0, 0);
            this.layoutTarget.Name = "layoutTarget";
            this.layoutTarget.RowCount = 8;
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTarget.Size = new System.Drawing.Size(604, 380);
            this.layoutTarget.TabIndex = 3;
            // 
            // lblSectionUrl
            // 
            this.lblSectionUrl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.layoutTarget.SetColumnSpan(this.lblSectionUrl, 3);
            this.lblSectionUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblSectionUrl.Location = new System.Drawing.Point(81, 0);
            this.lblSectionUrl.Name = "lblSectionUrl";
            this.lblSectionUrl.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblSectionUrl.Size = new System.Drawing.Size(442, 18);
            this.lblSectionUrl.TabIndex = 9;
            this.lblSectionUrl.Text = "Page URL";
            this.lblSectionUrl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnStartBrowser
            // 
            this.btnStartBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartBrowser.Location = new System.Drawing.Point(324, 20);
            this.btnStartBrowser.Margin = new System.Windows.Forms.Padding(2);
            this.btnStartBrowser.Name = "btnStartBrowser";
            this.btnStartBrowser.Size = new System.Drawing.Size(137, 22);
            this.btnStartBrowser.TabIndex = 8;
            this.btnStartBrowser.Text = "Start browser";
            this.btnStartBrowser.UseVisualStyleBackColor = true;
            // 
            // txtUrl
            // 
            this.txtUrl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUrl.Location = new System.Drawing.Point(6, 22);
            this.txtUrl.Margin = new System.Windows.Forms.Padding(6, 4, 4, 4);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(312, 20);
            this.txtUrl.TabIndex = 0;
            // 
            // btnNavigate
            // 
            this.btnNavigate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNavigate.Location = new System.Drawing.Point(465, 20);
            this.btnNavigate.Margin = new System.Windows.Forms.Padding(2);
            this.btnNavigate.Name = "btnNavigate";
            this.btnNavigate.Size = new System.Drawing.Size(137, 22);
            this.btnNavigate.TabIndex = 2;
            this.btnNavigate.Text = "Navigate";
            this.btnNavigate.UseVisualStyleBackColor = true;
            // 
            // lblScheme
            // 
            this.lblScheme.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblScheme, 3);
            this.lblScheme.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblScheme.Location = new System.Drawing.Point(2, 46);
            this.lblScheme.Margin = new System.Windows.Forms.Padding(2);
            this.lblScheme.Name = "lblScheme";
            this.lblScheme.Size = new System.Drawing.Size(600, 19);
            this.lblScheme.TabIndex = 3;
            this.lblScheme.Text = "Scheme:";
            this.lblScheme.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblHost, 3);
            this.lblHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHost.Location = new System.Drawing.Point(2, 69);
            this.lblHost.Margin = new System.Windows.Forms.Padding(2);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(600, 19);
            this.lblHost.TabIndex = 4;
            this.lblHost.Text = "Host:";
            this.lblHost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblPort, 3);
            this.lblPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPort.Location = new System.Drawing.Point(2, 92);
            this.lblPort.Margin = new System.Windows.Forms.Padding(2);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(600, 19);
            this.lblPort.TabIndex = 5;
            this.lblPort.Text = "Port:";
            this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblPath, 3);
            this.lblPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPath.Location = new System.Drawing.Point(2, 115);
            this.lblPath.Margin = new System.Windows.Forms.Padding(2);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(600, 19);
            this.lblPath.TabIndex = 6;
            this.lblPath.Text = "Path:";
            this.lblPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuery
            // 
            this.lblQuery.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblQuery, 3);
            this.lblQuery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuery.Location = new System.Drawing.Point(2, 138);
            this.lblQuery.Margin = new System.Windows.Forms.Padding(2);
            this.lblQuery.Name = "lblQuery";
            this.lblQuery.Size = new System.Drawing.Size(600, 19);
            this.lblQuery.TabIndex = 7;
            this.lblQuery.Text = "Query:";
            this.lblQuery.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabObjects
            // 
            this.tabObjects.Controls.Add(this.splitObjects);
            this.tabObjects.Controls.Add(this.panelObjectsToolbar);
            this.tabObjects.Location = new System.Drawing.Point(4, 22);
            this.tabObjects.Name = "tabObjects";
            this.tabObjects.Padding = new System.Windows.Forms.Padding(4);
            this.tabObjects.Size = new System.Drawing.Size(628, 400);
            this.tabObjects.TabIndex = 1;
            this.tabObjects.Text = "Objects";
            this.tabObjects.UseVisualStyleBackColor = true;
            // 
            // splitObjects
            // 
            this.splitObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitObjects.Location = new System.Drawing.Point(4, 36);
            this.splitObjects.Name = "splitObjects";
            // 
            // splitObjects.Panel1
            // 
            this.splitObjects.Panel1.Controls.Add(this.treeObjects);
            // 
            // splitObjects.Panel2
            // 
            this.splitObjects.Panel2.Controls.Add(this.gridObjectProps);
            this.splitObjects.Size = new System.Drawing.Size(620, 360);
            this.splitObjects.SplitterDistance = 259;
            this.splitObjects.TabIndex = 1;
            // 
            // treeObjects
            // 
            this.treeObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeObjects.Location = new System.Drawing.Point(0, 0);
            this.treeObjects.Name = "treeObjects";
            this.treeObjects.Size = new System.Drawing.Size(259, 360);
            this.treeObjects.TabIndex = 0;
            this.treeObjects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeObjects_AfterSelect);
            // 
            // gridObjectProps
            // 
            this.gridObjectProps.AllowUserToAddRows = false;
            this.gridObjectProps.AllowUserToDeleteRows = false;
            this.gridObjectProps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridObjectProps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridObjectProps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridObjectProps.Location = new System.Drawing.Point(0, 0);
            this.gridObjectProps.Name = "gridObjectProps";
            this.gridObjectProps.ReadOnly = true;
            this.gridObjectProps.RowHeadersVisible = false;
            this.gridObjectProps.RowHeadersWidth = 51;
            this.gridObjectProps.Size = new System.Drawing.Size(357, 360);
            this.gridObjectProps.TabIndex = 0;
            // 
            // panelObjectsToolbar
            // 
            this.panelObjectsToolbar.Controls.Add(this.flowObjectsToolbar);
            this.panelObjectsToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelObjectsToolbar.Location = new System.Drawing.Point(4, 4);
            this.panelObjectsToolbar.Name = "panelObjectsToolbar";
            this.panelObjectsToolbar.Size = new System.Drawing.Size(620, 32);
            this.panelObjectsToolbar.TabIndex = 2;
            // 
            // flowObjectsToolbar
            // 
            this.flowObjectsToolbar.AutoSize = true;
            this.flowObjectsToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowObjectsToolbar.Controls.Add(this.txtTreeSearch);
            this.flowObjectsToolbar.Controls.Add(this.btnTreeSearchGo);
            this.flowObjectsToolbar.Controls.Add(this.btnTreeSearchPrev);
            this.flowObjectsToolbar.Controls.Add(this.btnTreeSearchNext);
            this.flowObjectsToolbar.Controls.Add(this.chkTreeRegex);
            this.flowObjectsToolbar.Controls.Add(this.btnRefreshTree);
            this.flowObjectsToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowObjectsToolbar.Location = new System.Drawing.Point(0, 0);
            this.flowObjectsToolbar.Name = "flowObjectsToolbar";
            this.flowObjectsToolbar.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.flowObjectsToolbar.Size = new System.Drawing.Size(620, 32);
            this.flowObjectsToolbar.TabIndex = 0;
            this.flowObjectsToolbar.WrapContents = false;
            // 
            // txtTreeSearch
            // 
            this.txtTreeSearch.Location = new System.Drawing.Point(3, 6);
            this.txtTreeSearch.Margin = new System.Windows.Forms.Padding(3, 3, 4, 3);
            this.txtTreeSearch.Name = "txtTreeSearch";
            this.txtTreeSearch.Size = new System.Drawing.Size(166, 20);
            this.txtTreeSearch.TabIndex = 0;
            // 
            // btnTreeSearchGo
            // 
            this.btnTreeSearchGo.Location = new System.Drawing.Point(176, 5);
            this.btnTreeSearchGo.Margin = new System.Windows.Forms.Padding(3, 2, 2, 2);
            this.btnTreeSearchGo.Name = "btnTreeSearchGo";
            this.btnTreeSearchGo.Size = new System.Drawing.Size(24, 21);
            this.btnTreeSearchGo.TabIndex = 1;
            this.btnTreeSearchGo.Text = "▶";
            this.btnTreeSearchGo.UseVisualStyleBackColor = true;
            this.btnTreeSearchGo.Click += new System.EventHandler(this.btnTreeSearchGo_Click);
            // 
            // btnTreeSearchPrev
            // 
            this.btnTreeSearchPrev.Location = new System.Drawing.Point(204, 5);
            this.btnTreeSearchPrev.Margin = new System.Windows.Forms.Padding(2);
            this.btnTreeSearchPrev.Name = "btnTreeSearchPrev";
            this.btnTreeSearchPrev.Size = new System.Drawing.Size(24, 21);
            this.btnTreeSearchPrev.TabIndex = 2;
            this.btnTreeSearchPrev.Text = "＜";
            this.btnTreeSearchPrev.UseVisualStyleBackColor = true;
            this.btnTreeSearchPrev.Click += new System.EventHandler(this.btnTreeSearchPrev_Click);
            // 
            // btnTreeSearchNext
            // 
            this.btnTreeSearchNext.Location = new System.Drawing.Point(232, 5);
            this.btnTreeSearchNext.Margin = new System.Windows.Forms.Padding(2);
            this.btnTreeSearchNext.Name = "btnTreeSearchNext";
            this.btnTreeSearchNext.Size = new System.Drawing.Size(24, 21);
            this.btnTreeSearchNext.TabIndex = 3;
            this.btnTreeSearchNext.Text = "＞";
            this.btnTreeSearchNext.UseVisualStyleBackColor = true;
            this.btnTreeSearchNext.Click += new System.EventHandler(this.btnTreeSearchNext_Click);
            // 
            // chkTreeRegex
            // 
            this.chkTreeRegex.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkTreeRegex.Location = new System.Drawing.Point(261, 5);
            this.chkTreeRegex.Margin = new System.Windows.Forms.Padding(3, 2, 4, 2);
            this.chkTreeRegex.Name = "chkTreeRegex";
            this.chkTreeRegex.Size = new System.Drawing.Size(27, 21);
            this.chkTreeRegex.TabIndex = 4;
            this.chkTreeRegex.Text = "*";
            this.chkTreeRegex.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkTreeRegex.UseVisualStyleBackColor = true;
            // 
            // btnRefreshTree
            // 
            this.btnRefreshTree.Location = new System.Drawing.Point(295, 5);
            this.btnRefreshTree.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRefreshTree.Name = "btnRefreshTree";
            this.btnRefreshTree.Size = new System.Drawing.Size(75, 21);
            this.btnRefreshTree.TabIndex = 5;
            this.btnRefreshTree.Text = "Refresh tree";
            this.btnRefreshTree.UseVisualStyleBackColor = true;
            this.btnRefreshTree.Click += new System.EventHandler(this.btnRefreshTree_Click);
            // 
            // tabRecord
            // 
            this.tabRecord.Controls.Add(this.splitRecordMainPreview);
            this.tabRecord.Controls.Add(this.lblRecordHint);
            this.tabRecord.Location = new System.Drawing.Point(4, 22);
            this.tabRecord.Name = "tabRecord";
            this.tabRecord.Padding = new System.Windows.Forms.Padding(4);
            this.tabRecord.Size = new System.Drawing.Size(628, 400);
            this.tabRecord.TabIndex = 2;
            this.tabRecord.Text = "Record / Replay";
            this.tabRecord.UseVisualStyleBackColor = true;
            // 
            // splitRecordMainPreview
            // 
            this.splitRecordMainPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRecordMainPreview.Location = new System.Drawing.Point(4, 37);
            this.splitRecordMainPreview.Name = "splitRecordMainPreview";
            // 
            // splitRecordMainPreview.Panel1
            // 
            this.splitRecordMainPreview.Panel1.Controls.Add(this.splitRecordWorkPreview);
            // 
            // splitRecordMainPreview.Panel2
            // 
            this.splitRecordMainPreview.Panel2.Controls.Add(this.panelRecordCanvasPreview);
            this.splitRecordMainPreview.Size = new System.Drawing.Size(620, 359);
            this.splitRecordMainPreview.SplitterDistance = 390;
            this.splitRecordMainPreview.TabIndex = 5;
            // 
            // splitRecordWorkPreview
            // 
            this.splitRecordWorkPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRecordWorkPreview.Location = new System.Drawing.Point(0, 0);
            this.splitRecordWorkPreview.Name = "splitRecordWorkPreview";
            this.splitRecordWorkPreview.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRecordWorkPreview.Panel1
            // 
            this.splitRecordWorkPreview.Panel1.Controls.Add(this.panel1);
            // 
            // splitRecordWorkPreview.Panel2
            // 
            this.splitRecordWorkPreview.Panel2.Controls.Add(this.panelRecordPerfPreview);
            this.splitRecordWorkPreview.Size = new System.Drawing.Size(390, 359);
            this.splitRecordWorkPreview.SplitterDistance = 182;
            this.splitRecordWorkPreview.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gridSteps);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(390, 182);
            this.panel1.TabIndex = 3;
            // 
            // gridSteps
            // 
            this.gridSteps.AllowUserToAddRows = false;
            this.gridSteps.AllowUserToDeleteRows = false;
            this.gridSteps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSteps.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAct,
            this.colSeq,
            this.colElapsed,
            this.colKw,
            this.colEvt,
            this.colData,
            this.colBounds,
            this.colLogical,
            this.colLoc,
            this.colXp,
            this.colLocAlt,
            this.colParam,
            this.colPerfRef});
            this.gridSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSteps.Location = new System.Drawing.Point(0, 0);
            this.gridSteps.Name = "gridSteps";
            this.gridSteps.ReadOnly = true;
            this.gridSteps.RowHeadersVisible = false;
            this.gridSteps.RowHeadersWidth = 51;
            this.gridSteps.Size = new System.Drawing.Size(390, 182);
            this.gridSteps.TabIndex = 1;
            // 
            // colAct
            // 
            this.colAct.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colAct.HeaderText = "Action";
            this.colAct.MinimumWidth = 6;
            this.colAct.Name = "colAct";
            this.colAct.ReadOnly = true;
            this.colAct.Width = 66;
            // 
            // colSeq
            // 
            this.colSeq.DataPropertyName = "RunOrder";
            this.colSeq.HeaderText = "#";
            this.colSeq.MinimumWidth = 6;
            this.colSeq.Name = "colSeq";
            this.colSeq.ReadOnly = true;
            this.colSeq.Width = 36;
            // 
            // colElapsed
            // 
            this.colElapsed.DataPropertyName = "ElapsedMsSincePrev";
            this.colElapsed.HeaderText = "Elapsed(ms)";
            this.colElapsed.MinimumWidth = 6;
            this.colElapsed.Name = "colElapsed";
            this.colElapsed.ReadOnly = true;
            // 
            // colKw
            // 
            this.colKw.DataPropertyName = "Keyword";
            this.colKw.HeaderText = "Keyword";
            this.colKw.MinimumWidth = 6;
            this.colKw.Name = "colKw";
            this.colKw.ReadOnly = true;
            this.colKw.Width = 120;
            // 
            // colEvt
            // 
            this.colEvt.DataPropertyName = "SourceEvent";
            this.colEvt.HeaderText = "Event";
            this.colEvt.MinimumWidth = 6;
            this.colEvt.Name = "colEvt";
            this.colEvt.ReadOnly = true;
            this.colEvt.Width = 96;
            // 
            // colData
            // 
            this.colData.DataPropertyName = "Data";
            this.colData.HeaderText = "Data";
            this.colData.MinimumWidth = 6;
            this.colData.Name = "colData";
            this.colData.ReadOnly = true;
            this.colData.Width = 180;
            // 
            // colBounds
            // 
            this.colBounds.DataPropertyName = "BoundsDisplay";
            this.colBounds.HeaderText = "Bounds";
            this.colBounds.MinimumWidth = 6;
            this.colBounds.Name = "colBounds";
            this.colBounds.ReadOnly = true;
            this.colBounds.Width = 150;
            // 
            // colLogical
            // 
            this.colLogical.DataPropertyName = "LogicalKind";
            this.colLogical.HeaderText = "Logical";
            this.colLogical.MinimumWidth = 6;
            this.colLogical.Name = "colLogical";
            this.colLogical.ReadOnly = true;
            this.colLogical.Width = 120;
            // 
            // colLoc
            // 
            this.colLoc.DataPropertyName = "Locator";
            this.colLoc.HeaderText = "Locator";
            this.colLoc.MinimumWidth = 6;
            this.colLoc.Name = "colLoc";
            this.colLoc.ReadOnly = true;
            this.colLoc.Width = 220;
            // 
            // colXp
            // 
            this.colXp.DataPropertyName = "ElementXpath";
            this.colXp.HeaderText = "Xpath";
            this.colXp.MinimumWidth = 6;
            this.colXp.Name = "colXp";
            this.colXp.ReadOnly = true;
            this.colXp.Width = 180;
            // 
            // colLocAlt
            // 
            this.colLocAlt.DataPropertyName = "LocatorAlternates";
            this.colLocAlt.HeaderText = "LocatorAlt";
            this.colLocAlt.MinimumWidth = 6;
            this.colLocAlt.Name = "colLocAlt";
            this.colLocAlt.ReadOnly = true;
            this.colLocAlt.Width = 220;
            // 
            // colParam
            // 
            this.colParam.DataPropertyName = "Parameter";
            this.colParam.HeaderText = "Parameter";
            this.colParam.MinimumWidth = 6;
            this.colParam.Name = "colParam";
            this.colParam.ReadOnly = true;
            this.colParam.Width = 220;
            // 
            // colPerfRef
            // 
            this.colPerfRef.HeaderText = "Perf#";
            this.colPerfRef.MinimumWidth = 6;
            this.colPerfRef.Name = "colPerfRef";
            this.colPerfRef.ReadOnly = true;
            this.colPerfRef.Width = 72;
            // 
            // panelRecordPerfPreview
            // 
            this.panelRecordPerfPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelRecordPerfPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelRecordPerfPreview.Controls.Add(this.splitRecordPerfPreview);
            this.panelRecordPerfPreview.Controls.Add(this.lblPerfDesignTitle);
            this.panelRecordPerfPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRecordPerfPreview.Location = new System.Drawing.Point(0, 0);
            this.panelRecordPerfPreview.Name = "panelRecordPerfPreview";
            this.panelRecordPerfPreview.Size = new System.Drawing.Size(390, 173);
            this.panelRecordPerfPreview.TabIndex = 4;
            // 
            // splitRecordPerfPreview
            // 
            this.splitRecordPerfPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRecordPerfPreview.Location = new System.Drawing.Point(0, 28);
            this.splitRecordPerfPreview.Name = "splitRecordPerfPreview";
            this.splitRecordPerfPreview.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRecordPerfPreview.Panel1
            // 
            this.splitRecordPerfPreview.Panel1.Controls.Add(this.gridPerfAnchorPreview);
            this.splitRecordPerfPreview.Panel1.Controls.Add(this.lblPerfDesignAnchorSummary);
            // 
            // splitRecordPerfPreview.Panel2
            // 
            this.splitRecordPerfPreview.Panel2.Controls.Add(this.lblPerfDesignRuntime);
            this.splitRecordPerfPreview.Panel2.Controls.Add(this.gridPerfRuntimePreview);
            this.splitRecordPerfPreview.Size = new System.Drawing.Size(388, 143);
            this.splitRecordPerfPreview.SplitterDistance = 88;
            this.splitRecordPerfPreview.SplitterWidth = 5;
            this.splitRecordPerfPreview.TabIndex = 2;
            // 
            // lblPerfDesignRuntime
            // 
            this.lblPerfDesignRuntime.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignRuntime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblPerfDesignRuntime.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignRuntime.Name = "lblPerfDesignRuntime";
            this.lblPerfDesignRuntime.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPerfDesignRuntime.Size = new System.Drawing.Size(388, 20);
            this.lblPerfDesignRuntime.TabIndex = 1;
            this.lblPerfDesignRuntime.Text = "Runtime progress (throughput/error rate)";
            this.lblPerfDesignRuntime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridPerfRuntimePreview
            // 
            this.gridPerfRuntimePreview.AllowUserToAddRows = false;
            this.gridPerfRuntimePreview.AllowUserToDeleteRows = false;
            this.gridPerfRuntimePreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridPerfRuntimePreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPerfRuntimePreview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRtTx,
            this.colRtOk,
            this.colRtFail,
            this.colRtTotalReq,
            this.colRtFinished,
            this.colRtRounds,
            this.colRtTps,
            this.colRtErr,
            this.colRtLast});
            this.gridPerfRuntimePreview.Location = new System.Drawing.Point(0, 20);
            this.gridPerfRuntimePreview.Name = "gridPerfRuntimePreview";
            this.gridPerfRuntimePreview.ReadOnly = true;
            this.gridPerfRuntimePreview.RowHeadersVisible = false;
            this.gridPerfRuntimePreview.RowHeadersWidth = 51;
            this.gridPerfRuntimePreview.Size = new System.Drawing.Size(388, 21);
            this.gridPerfRuntimePreview.TabIndex = 1;
            // 
            // colRtTx
            // 
            this.colRtTx.DataPropertyName = "Transaction";
            this.colRtTx.FillWeight = 180F;
            this.colRtTx.HeaderText = "Transaction";
            this.colRtTx.MinimumWidth = 64;
            this.colRtTx.Name = "colRtTx";
            this.colRtTx.ReadOnly = true;
            // 
            // colRtOk
            // 
            this.colRtOk.DataPropertyName = "Ok";
            this.colRtOk.FillWeight = 70F;
            this.colRtOk.HeaderText = "OK";
            this.colRtOk.MinimumWidth = 36;
            this.colRtOk.Name = "colRtOk";
            this.colRtOk.ReadOnly = true;
            // 
            // colRtFail
            // 
            this.colRtFail.DataPropertyName = "Fail";
            this.colRtFail.FillWeight = 70F;
            this.colRtFail.HeaderText = "Fail";
            this.colRtFail.MinimumWidth = 36;
            this.colRtFail.Name = "colRtFail";
            this.colRtFail.ReadOnly = true;
            // 
            // colRtTotalReq
            // 
            this.colRtTotalReq.DataPropertyName = "TotalRequest";
            this.colRtTotalReq.FillWeight = 95F;
            this.colRtTotalReq.HeaderText = "TotalRequest";
            this.colRtTotalReq.MinimumWidth = 48;
            this.colRtTotalReq.Name = "colRtTotalReq";
            this.colRtTotalReq.ReadOnly = true;
            // 
            // colRtFinished
            // 
            this.colRtFinished.DataPropertyName = "FinishedRequest";
            this.colRtFinished.FillWeight = 80F;
            this.colRtFinished.HeaderText = "Finished";
            this.colRtFinished.MinimumWidth = 40;
            this.colRtFinished.Name = "colRtFinished";
            this.colRtFinished.ReadOnly = true;
            // 
            // colRtRounds
            // 
            this.colRtRounds.DataPropertyName = "RoundProgress";
            this.colRtRounds.FillWeight = 90F;
            this.colRtRounds.HeaderText = "Rounds";
            this.colRtRounds.MinimumWidth = 40;
            this.colRtRounds.Name = "colRtRounds";
            this.colRtRounds.ReadOnly = true;
            // 
            // colRtTps
            // 
            this.colRtTps.DataPropertyName = "ThroughputPerSecond";
            this.colRtTps.FillWeight = 95F;
            this.colRtTps.HeaderText = "Throughput/s";
            this.colRtTps.MinimumWidth = 48;
            this.colRtTps.Name = "colRtTps";
            this.colRtTps.ReadOnly = true;
            // 
            // colRtErr
            // 
            this.colRtErr.DataPropertyName = "ErrorRate";
            this.colRtErr.FillWeight = 90F;
            this.colRtErr.HeaderText = "Error Rate";
            this.colRtErr.MinimumWidth = 40;
            this.colRtErr.Name = "colRtErr";
            this.colRtErr.ReadOnly = true;
            // 
            // colRtLast
            // 
            this.colRtLast.DataPropertyName = "LastDetail";
            this.colRtLast.FillWeight = 360F;
            this.colRtLast.HeaderText = "Last Detail";
            this.colRtLast.MinimumWidth = 80;
            this.colRtLast.Name = "colRtLast";
            this.colRtLast.ReadOnly = true;
            // 
            // lblPerfDesignTitle
            // 
            this.lblPerfDesignTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.lblPerfDesignTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPerfDesignTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblPerfDesignTitle.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignTitle.Name = "lblPerfDesignTitle";
            this.lblPerfDesignTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPerfDesignTitle.Size = new System.Drawing.Size(388, 28);
            this.lblPerfDesignTitle.TabIndex = 0;
            this.lblPerfDesignTitle.Text = "Perform Test anchors / Runtime progress";
            this.lblPerfDesignTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelRecordCanvasPreview
            // 
            this.panelRecordCanvasPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelRecordCanvasPreview.Controls.Add(this.lblRecordCanvasPreview);
            this.panelRecordCanvasPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRecordCanvasPreview.Location = new System.Drawing.Point(0, 0);
            this.panelRecordCanvasPreview.Name = "panelRecordCanvasPreview";
            this.panelRecordCanvasPreview.Size = new System.Drawing.Size(226, 359);
            this.panelRecordCanvasPreview.TabIndex = 0;
            // 
            // lblRecordCanvasPreview
            // 
            this.lblRecordCanvasPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecordCanvasPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.lblRecordCanvasPreview.Location = new System.Drawing.Point(0, 0);
            this.lblRecordCanvasPreview.Name = "lblRecordCanvasPreview";
            this.lblRecordCanvasPreview.Padding = new System.Windows.Forms.Padding(12);
            this.lblRecordCanvasPreview.Size = new System.Drawing.Size(224, 357);
            this.lblRecordCanvasPreview.TabIndex = 0;
            this.lblRecordCanvasPreview.Text = "Canvas/WebView preview area (design only)";
            this.lblRecordCanvasPreview.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabSettings
            // 
            this.tabSettings.AutoScroll = true;
            this.tabSettings.Controls.Add(this.layoutSettings);
            this.tabSettings.Location = new System.Drawing.Point(4, 22);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(8);
            this.tabSettings.Size = new System.Drawing.Size(628, 400);
            this.tabSettings.TabIndex = 3;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // layoutSettings
            // 
            this.layoutSettings.AutoSize = true;
            this.layoutSettings.ColumnCount = 2;
            this.layoutSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.layoutSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutSettings.Controls.Add(this.lblDataRoot, 0, 0);
            this.layoutSettings.Controls.Add(this.txtDataRoot, 1, 0);
            this.layoutSettings.Controls.Add(this.chkHeadless, 1, 1);
            this.layoutSettings.Controls.Add(this.lblTimeout, 0, 2);
            this.layoutSettings.Controls.Add(this.numTimeout, 1, 2);
            this.layoutSettings.Controls.Add(this.chkPersistHeaders, 1, 3);
            this.layoutSettings.Controls.Add(this.lblChannel, 0, 4);
            this.layoutSettings.Controls.Add(this.txtBrowserChannel, 1, 4);
            this.layoutSettings.Controls.Add(this.lblViewport, 0, 5);
            this.layoutSettings.Controls.Add(this.numViewportW, 1, 5);
            this.layoutSettings.Controls.Add(this.numViewportH, 1, 6);
            this.layoutSettings.Controls.Add(this.btnSaveSettings, 1, 7);
            this.layoutSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.layoutSettings.Location = new System.Drawing.Point(8, 8);
            this.layoutSettings.Name = "layoutSettings";
            this.layoutSettings.RowCount = 8;
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.layoutSettings.Size = new System.Drawing.Size(612, 264);
            this.layoutSettings.TabIndex = 0;
            // 
            // lblDataRoot
            // 
            this.lblDataRoot.AutoSize = true;
            this.lblDataRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDataRoot.Location = new System.Drawing.Point(3, 0);
            this.lblDataRoot.Name = "lblDataRoot";
            this.lblDataRoot.Size = new System.Drawing.Size(154, 32);
            this.lblDataRoot.TabIndex = 0;
            this.lblDataRoot.Text = "Data root folder";
            this.lblDataRoot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDataRoot
            // 
            this.txtDataRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDataRoot.Location = new System.Drawing.Point(163, 3);
            this.txtDataRoot.Name = "txtDataRoot";
            this.txtDataRoot.Size = new System.Drawing.Size(446, 20);
            this.txtDataRoot.TabIndex = 1;
            // 
            // chkHeadless
            // 
            this.chkHeadless.AutoSize = true;
            this.chkHeadless.Location = new System.Drawing.Point(163, 35);
            this.chkHeadless.Name = "chkHeadless";
            this.chkHeadless.Size = new System.Drawing.Size(99, 17);
            this.chkHeadless.TabIndex = 2;
            this.chkHeadless.Text = "Headless mode";
            this.chkHeadless.UseVisualStyleBackColor = true;
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeout.Location = new System.Drawing.Point(3, 64);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(154, 32);
            this.lblTimeout.TabIndex = 3;
            this.lblTimeout.Text = "Default timeout (ms)";
            this.lblTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numTimeout
            // 
            this.numTimeout.Dock = System.Windows.Forms.DockStyle.Left;
            this.numTimeout.Location = new System.Drawing.Point(163, 67);
            this.numTimeout.Maximum = new decimal(new int[] {
            600000,
            0,
            0,
            0});
            this.numTimeout.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numTimeout.Name = "numTimeout";
            this.numTimeout.Size = new System.Drawing.Size(120, 20);
            this.numTimeout.TabIndex = 4;
            this.numTimeout.Value = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            // 
            // chkPersistHeaders
            // 
            this.chkPersistHeaders.AutoSize = true;
            this.chkPersistHeaders.Location = new System.Drawing.Point(163, 99);
            this.chkPersistHeaders.Name = "chkPersistHeaders";
            this.chkPersistHeaders.Size = new System.Drawing.Size(253, 17);
            this.chkPersistHeaders.TabIndex = 5;
            this.chkPersistHeaders.Text = "Persist sensitive headers/cookies in network log";
            this.chkPersistHeaders.UseVisualStyleBackColor = true;
            // 
            // lblChannel
            // 
            this.lblChannel.AutoSize = true;
            this.lblChannel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChannel.Location = new System.Drawing.Point(3, 128);
            this.lblChannel.Name = "lblChannel";
            this.lblChannel.Size = new System.Drawing.Size(154, 32);
            this.lblChannel.TabIndex = 6;
            this.lblChannel.Text = "Browser channel (opt.)";
            this.lblChannel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBrowserChannel
            // 
            this.txtBrowserChannel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBrowserChannel.Location = new System.Drawing.Point(163, 131);
            this.txtBrowserChannel.Name = "txtBrowserChannel";
            this.txtBrowserChannel.Size = new System.Drawing.Size(446, 20);
            this.txtBrowserChannel.TabIndex = 7;
            // 
            // lblViewport
            // 
            this.lblViewport.AutoSize = true;
            this.lblViewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblViewport.Location = new System.Drawing.Point(3, 160);
            this.lblViewport.Name = "lblViewport";
            this.lblViewport.Size = new System.Drawing.Size(154, 32);
            this.lblViewport.TabIndex = 8;
            this.lblViewport.Text = "Viewport W / H";
            this.lblViewport.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numViewportW
            // 
            this.numViewportW.Dock = System.Windows.Forms.DockStyle.Left;
            this.numViewportW.Location = new System.Drawing.Point(163, 163);
            this.numViewportW.Maximum = new decimal(new int[] {
            4000,
            0,
            0,
            0});
            this.numViewportW.Minimum = new decimal(new int[] {
            400,
            0,
            0,
            0});
            this.numViewportW.Name = "numViewportW";
            this.numViewportW.Size = new System.Drawing.Size(80, 20);
            this.numViewportW.TabIndex = 9;
            this.numViewportW.Value = new decimal(new int[] {
            1280,
            0,
            0,
            0});
            // 
            // numViewportH
            // 
            this.numViewportH.Dock = System.Windows.Forms.DockStyle.Left;
            this.numViewportH.Location = new System.Drawing.Point(163, 195);
            this.numViewportH.Maximum = new decimal(new int[] {
            4000,
            0,
            0,
            0});
            this.numViewportH.Minimum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numViewportH.Name = "numViewportH";
            this.numViewportH.Size = new System.Drawing.Size(80, 20);
            this.numViewportH.TabIndex = 10;
            this.numViewportH.Value = new decimal(new int[] {
            720,
            0,
            0,
            0});
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(163, 227);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(120, 28);
            this.btnSaveSettings.TabIndex = 11;
            this.btnSaveSettings.Text = "Save settings";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // lblRecordHint
            // 
            this.lblRecordHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecordHint.Location = new System.Drawing.Point(4, 4);
            this.lblRecordHint.Name = "lblRecordHint";
            this.lblRecordHint.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.lblRecordHint.Size = new System.Drawing.Size(620, 33);
            this.lblRecordHint.TabIndex = 2;
            this.lblRecordHint.Text = "Toolbar: Record toggles capture; Replay runs the grid below. Steps use semantic k" +
    "eywords.";
            this.lblRecordHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPerfDesignAnchorSummary
            // 
            this.lblPerfDesignAnchorSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignAnchorSummary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.lblPerfDesignAnchorSummary.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignAnchorSummary.Name = "lblPerfDesignAnchorSummary";
            this.lblPerfDesignAnchorSummary.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPerfDesignAnchorSummary.Size = new System.Drawing.Size(388, 23);
            this.lblPerfDesignAnchorSummary.TabIndex = 9;
            this.lblPerfDesignAnchorSummary.Text = "Anchor groups: (design preview)";
            this.lblPerfDesignAnchorSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridPerfAnchorPreview
            // 
            this.gridPerfAnchorPreview.AllowUserToAddRows = false;
            this.gridPerfAnchorPreview.AllowUserToDeleteRows = false;
            this.gridPerfAnchorPreview.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.gridPerfAnchorPreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPerfAnchorPreview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPerfAction,
            this.colPerfAnchor,
            this.colPerfScore,
            this.colPerfGroup,
            this.colPerfCorr,
            this.colPerfUrl,
            this.colPerfType,
            this.colPerfMethod,
            this.colPerfStatus,
            this.colPerfParam,
            this.colPerfHeader,
            this.colPerfCookie,
            this.colPerfPayload,
            this.colPerfResponse,
            this.colPerfPolicy,
            this.colPerfValidation});
            this.gridPerfAnchorPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPerfAnchorPreview.Location = new System.Drawing.Point(0, 23);
            this.gridPerfAnchorPreview.Name = "gridPerfAnchorPreview";
            this.gridPerfAnchorPreview.ReadOnly = true;
            this.gridPerfAnchorPreview.RowHeadersVisible = false;
            this.gridPerfAnchorPreview.RowHeadersWidth = 51;
            this.gridPerfAnchorPreview.Size = new System.Drawing.Size(388, 65);
            this.gridPerfAnchorPreview.TabIndex = 10;
            // 
            // colPerfAction
            // 
            this.colPerfAction.DataPropertyName = "Action";
            this.colPerfAction.FillWeight = 90F;
            this.colPerfAction.HeaderText = "Action";
            this.colPerfAction.MinimumWidth = 48;
            this.colPerfAction.Name = "colPerfAction";
            this.colPerfAction.ReadOnly = true;
            this.colPerfAction.TrackVisitedState = false;
            // 
            // colPerfAnchor
            // 
            this.colPerfAnchor.DataPropertyName = "IsAnchorSelected";
            this.colPerfAnchor.FillWeight = 55F;
            this.colPerfAnchor.HeaderText = "Anchor";
            this.colPerfAnchor.MinimumWidth = 40;
            this.colPerfAnchor.Name = "colPerfAnchor";
            this.colPerfAnchor.ReadOnly = true;
            // 
            // colPerfScore
            // 
            this.colPerfScore.DataPropertyName = "AnchorScore";
            this.colPerfScore.FillWeight = 56F;
            this.colPerfScore.HeaderText = "Score";
            this.colPerfScore.MinimumWidth = 40;
            this.colPerfScore.Name = "colPerfScore";
            this.colPerfScore.ReadOnly = true;
            // 
            // colPerfGroup
            // 
            this.colPerfGroup.DataPropertyName = "AnchorGroup";
            this.colPerfGroup.FillWeight = 120F;
            this.colPerfGroup.HeaderText = "Group";
            this.colPerfGroup.MinimumWidth = 48;
            this.colPerfGroup.Name = "colPerfGroup";
            this.colPerfGroup.ReadOnly = true;
            // 
            // colPerfCorr
            // 
            this.colPerfCorr.DataPropertyName = "CorrelationHint";
            this.colPerfCorr.FillWeight = 150F;
            this.colPerfCorr.HeaderText = "Correlation";
            this.colPerfCorr.MinimumWidth = 48;
            this.colPerfCorr.Name = "colPerfCorr";
            this.colPerfCorr.ReadOnly = true;
            // 
            // colPerfUrl
            // 
            this.colPerfUrl.DataPropertyName = "Url";
            this.colPerfUrl.FillWeight = 260F;
            this.colPerfUrl.HeaderText = "URL";
            this.colPerfUrl.MinimumWidth = 80;
            this.colPerfUrl.Name = "colPerfUrl";
            this.colPerfUrl.ReadOnly = true;
            // 
            // colPerfType
            // 
            this.colPerfType.DataPropertyName = "ResourceType";
            this.colPerfType.FillWeight = 80F;
            this.colPerfType.HeaderText = "Type";
            this.colPerfType.MinimumWidth = 40;
            this.colPerfType.Name = "colPerfType";
            this.colPerfType.ReadOnly = true;
            // 
            // colPerfMethod
            // 
            this.colPerfMethod.DataPropertyName = "Method";
            this.colPerfMethod.FillWeight = 80F;
            this.colPerfMethod.HeaderText = "Method";
            this.colPerfMethod.MinimumWidth = 40;
            this.colPerfMethod.Name = "colPerfMethod";
            this.colPerfMethod.ReadOnly = true;
            // 
            // colPerfStatus
            // 
            this.colPerfStatus.DataPropertyName = "Status";
            this.colPerfStatus.FillWeight = 70F;
            this.colPerfStatus.HeaderText = "Status";
            this.colPerfStatus.MinimumWidth = 36;
            this.colPerfStatus.Name = "colPerfStatus";
            this.colPerfStatus.ReadOnly = true;
            // 
            // colPerfParam
            // 
            this.colPerfParam.DataPropertyName = "Parameter";
            this.colPerfParam.FillWeight = 180F;
            this.colPerfParam.HeaderText = "Parameter";
            this.colPerfParam.MinimumWidth = 48;
            this.colPerfParam.Name = "colPerfParam";
            this.colPerfParam.ReadOnly = true;
            // 
            // colPerfHeader
            // 
            this.colPerfHeader.DataPropertyName = "Headers";
            this.colPerfHeader.FillWeight = 220F;
            this.colPerfHeader.HeaderText = "Header";
            this.colPerfHeader.MinimumWidth = 48;
            this.colPerfHeader.Name = "colPerfHeader";
            this.colPerfHeader.ReadOnly = true;
            // 
            // colPerfCookie
            // 
            this.colPerfCookie.DataPropertyName = "Cookies";
            this.colPerfCookie.FillWeight = 160F;
            this.colPerfCookie.HeaderText = "Cookie";
            this.colPerfCookie.MinimumWidth = 48;
            this.colPerfCookie.Name = "colPerfCookie";
            this.colPerfCookie.ReadOnly = true;
            // 
            // colPerfPayload
            // 
            this.colPerfPayload.DataPropertyName = "Payload";
            this.colPerfPayload.FillWeight = 220F;
            this.colPerfPayload.HeaderText = "Payload";
            this.colPerfPayload.MinimumWidth = 48;
            this.colPerfPayload.Name = "colPerfPayload";
            this.colPerfPayload.ReadOnly = true;
            // 
            // colPerfResponse
            // 
            this.colPerfResponse.DataPropertyName = "Response";
            this.colPerfResponse.FillWeight = 220F;
            this.colPerfResponse.HeaderText = "Response";
            this.colPerfResponse.MinimumWidth = 48;
            this.colPerfResponse.Name = "colPerfResponse";
            this.colPerfResponse.ReadOnly = true;
            // 
            // colPerfPolicy
            // 
            this.colPerfPolicy.DataPropertyName = "ReplayPolicy";
            this.colPerfPolicy.FillWeight = 130F;
            this.colPerfPolicy.HeaderText = "ReplayPolicy";
            this.colPerfPolicy.MinimumWidth = 48;
            this.colPerfPolicy.Name = "colPerfPolicy";
            this.colPerfPolicy.ReadOnly = true;
            // 
            // colPerfValidation
            // 
            this.colPerfValidation.DataPropertyName = "ValidationHint";
            this.colPerfValidation.FillWeight = 180F;
            this.colPerfValidation.HeaderText = "ValidationHint";
            this.colPerfValidation.MinimumWidth = 48;
            this.colPerfValidation.Name = "colPerfValidation";
            this.colPerfValidation.ReadOnly = true;
            // 
            // MainWorkbenchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(636, 497);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.statusMain);
            this.Controls.Add(this.toolPerf);
            this.Controls.Add(this.toolMain);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.MinimumSize = new System.Drawing.Size(484, 397);
            this.Name = "MainWorkbenchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MARS Web Automation";
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.toolMain.ResumeLayout(false);
            this.toolMain.PerformLayout();
            this.toolPerf.ResumeLayout(false);
            this.toolPerf.PerformLayout();
            this.statusMain.ResumeLayout(false);
            this.statusMain.PerformLayout();
            this.tabMain.ResumeLayout(false);
            this.tabTarget.ResumeLayout(false);
            this.panelTargetCard.ResumeLayout(false);
            this.layoutTarget.ResumeLayout(false);
            this.layoutTarget.PerformLayout();
            this.tabObjects.ResumeLayout(false);
            this.splitObjects.Panel1.ResumeLayout(false);
            this.splitObjects.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitObjects)).EndInit();
            this.splitObjects.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridObjectProps)).EndInit();
            this.panelObjectsToolbar.ResumeLayout(false);
            this.panelObjectsToolbar.PerformLayout();
            this.flowObjectsToolbar.ResumeLayout(false);
            this.flowObjectsToolbar.PerformLayout();
            this.tabRecord.ResumeLayout(false);
            this.splitRecordMainPreview.Panel1.ResumeLayout(false);
            this.splitRecordMainPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordMainPreview)).EndInit();
            this.splitRecordMainPreview.ResumeLayout(false);
            this.splitRecordWorkPreview.Panel1.ResumeLayout(false);
            this.splitRecordWorkPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordWorkPreview)).EndInit();
            this.splitRecordWorkPreview.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSteps)).EndInit();
            this.panelRecordPerfPreview.ResumeLayout(false);
            this.splitRecordPerfPreview.Panel1.ResumeLayout(false);
            this.splitRecordPerfPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordPerfPreview)).EndInit();
            this.splitRecordPerfPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfRuntimePreview)).EndInit();
            this.panelRecordCanvasPreview.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.layoutSettings.ResumeLayout(false);
            this.layoutSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfAnchorPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private System.Windows.Forms.TableLayoutPanel layoutTarget;
        private System.Windows.Forms.Button btnStartBrowser;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Button btnNavigate;
        private System.Windows.Forms.Label lblScheme;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Label lblQuery;
        private System.Windows.Forms.Label lblSectionUrl;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView gridSteps;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colElapsed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKw;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEvt;
        private System.Windows.Forms.DataGridViewTextBoxColumn colData;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBounds;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogical;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoc;
        private System.Windows.Forms.DataGridViewTextBoxColumn colXp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLocAlt;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfRef;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtTx;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtOk;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtTotalReq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtFinished;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtRounds;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtTps;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtErr;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRtLast;
        private System.Windows.Forms.DataGridView gridPerfAnchorPreview;
        private System.Windows.Forms.DataGridViewLinkColumn colPerfAction;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPerfAnchor;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfCorr;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfUrl;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfMethod;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfParam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfHeader;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfCookie;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfPayload;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfResponse;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfPolicy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfValidation;
        private System.Windows.Forms.Label lblPerfDesignAnchorSummary;
        private System.Windows.Forms.Label lblRecordHint;
    }
}
