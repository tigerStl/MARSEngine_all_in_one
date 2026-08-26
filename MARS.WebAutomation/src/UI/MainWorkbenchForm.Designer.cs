namespace MARS.WebAutomation.UI
{
    partial class MainWorkbenchForm
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuFileSave;
        private System.Windows.Forms.ToolStripMenuItem menuFileLoad;
        private System.Windows.Forms.ToolStripMenuItem menuFileClear;
        private System.Windows.Forms.ToolStripMenuItem menuFileExport;
        private System.Windows.Forms.ToolStripMenuItem menuFileImport;
        private System.Windows.Forms.ToolStripSeparator menuFileSep;
        private System.Windows.Forms.ToolStripMenuItem menuFileExit;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLanguage;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLangEnglish;
        private System.Windows.Forms.ToolStripMenuItem menuHelpLangChinese;
        private System.Windows.Forms.ToolStripMenuItem menuHelpAbout;
        private System.Windows.Forms.ToolStripMenuItem menuRecord;
        private System.Windows.Forms.ToolStripMenuItem menuRecordCaptureSemantic;
        private System.Windows.Forms.ToolStripMenuItem menuRecordCapturePlain;
        private System.Windows.Forms.ToolStripSeparator menuRecordSepExport;
        private System.Windows.Forms.ToolStripMenuItem menuRecordExportAllPlaywrightTs;
        private System.Windows.Forms.ToolStripMenuItem menuRecordExportAllSeleniumTs;
        private System.Windows.Forms.ToolStrip toolMain;
        private System.Windows.Forms.ToolStrip toolPerf;
        private System.Windows.Forms.ToolStripButton tsbTarget;
        private System.Windows.Forms.ToolStripButton tsbRecord;
        private System.Windows.Forms.ToolStripDropDownButton tsddbRecordCapture;
        private System.Windows.Forms.ToolStripMenuItem tsmiRecordCaptureSemantic;
        private System.Windows.Forms.ToolStripMenuItem tsmiRecordCapturePlain;
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
        private System.Windows.Forms.ToolStripSeparator tsbSepPerf;
        private System.Windows.Forms.CheckBox chkWithPerformanceTest;
        private System.Windows.Forms.ToolStripButton tsbRunPerf;
        private System.Windows.Forms.ToolStripButton tsbStopPerf;
        private System.Windows.Forms.ToolStripButton tsbRunPerfSelected;
        private System.Windows.Forms.ToolStripSeparator tsbSepPerfScripts;
        private System.Windows.Forms.ToolStripButton tsbPerfExportPlaywrightTs;
        private System.Windows.Forms.ToolStripButton tsbPerfExportSeleniumTs;
        private System.Windows.Forms.ToolStripLabel tslBrand;
        private System.Windows.Forms.ToolStripSeparator tsbSepBrand;
        private System.Windows.Forms.StatusStrip statusMain;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabTarget;
        private System.Windows.Forms.Panel panelTargetCard;
        private System.Windows.Forms.TabPage tabObjects;
        private System.Windows.Forms.TabPage tabRecord;
        private System.Windows.Forms.TabPage tabApiPerformance;
        private System.Windows.Forms.TableLayoutPanel layoutApiPerf;
        private System.Windows.Forms.FlowLayoutPanel flowApiPerfActions;
        private System.Windows.Forms.Label _lblApiPerfHint;
        private System.Windows.Forms.SplitContainer splitApiPerf;
        private System.Windows.Forms.DataGridView _gridApiDefinitions;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colApiEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiMethod;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiUrl;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiSecurity;
        private System.Windows.Forms.TableLayoutPanel layoutApiEditor;
        private System.Windows.Forms.Label lblApiName;
        private System.Windows.Forms.TextBox _txtApiName;
        private System.Windows.Forms.Label lblApiMethod;
        private System.Windows.Forms.ComboBox _cmbApiMethod;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.TextBox _txtApiUrl;
        private System.Windows.Forms.Label lblApiHeaders;
        private System.Windows.Forms.DataGridView _gridApiHeaders;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiHeaderKey;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiHeaderValue;
        private System.Windows.Forms.Label lblApiSecurity;
        private System.Windows.Forms.ComboBox _cmbApiSecurity;
        private System.Windows.Forms.Label lblApiSecurityValue;
        private System.Windows.Forms.TextBox _txtApiSecurityValue;
        private System.Windows.Forms.Label lblApiPayload;
        private System.Windows.Forms.TextBox _txtApiPayload;
        private System.Windows.Forms.Label lblApiExpectedStatus;
        private System.Windows.Forms.NumericUpDown _numApiExpectedStatus;
        private System.Windows.Forms.Label lblApiGroup;
        private System.Windows.Forms.TextBox _txtApiGroup;
        private System.Windows.Forms.FlowLayoutPanel flowApiDefButtons;
        private System.Windows.Forms.Button _btnApiDefNew;
        private System.Windows.Forms.Button _btnApiDefSave;
        private System.Windows.Forms.Button _btnApiDefDelete;
        private System.Windows.Forms.Button _btnApiPerfRun;
        private System.Windows.Forms.Button _btnApiPerfRunSelected;
        private System.Windows.Forms.Button _btnApiPerfRunAll;
        private System.Windows.Forms.Button _btnApiPerfConfig;
        private System.Windows.Forms.Button _btnApiPerfExport;
        private System.Windows.Forms.Button _btnApiPerfImport;
        private System.Windows.Forms.Button _btnApiPerfGoRecord;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.SplitContainer splitRecordMainPreview;
        private System.Windows.Forms.SplitContainer splitRecordWorkPreview;
        private System.Windows.Forms.SplitContainer splitRecordCanvasProps;
        private MARS.WebAutomation.UI.StepObjectPropertyPanel stepObjectPropertyPanel;
        private System.Windows.Forms.Panel panelRecordCanvasPreview;
        private Microsoft.Web.WebView2.WinForms.WebView2 recordWebView;
        private System.Windows.Forms.ToolStrip toolStripRecordCanvas;
        private System.Windows.Forms.ToolStripButton tsbRecordCanvasZoomOut;
        private System.Windows.Forms.ToolStripButton tsbRecordCanvasZoomIn;
        private System.Windows.Forms.ToolStripButton tsbRecordCanvasCenter;
        private System.Windows.Forms.ToolStripSeparator tsbRecordCanvasSep1;
        private System.Windows.Forms.ToolStripSeparator tsbRecordCanvasSep2;
        private System.Windows.Forms.ToolStripLabel tslRecordCanvasZoom;
        private System.Windows.Forms.ToolStrip toolStripStepCommands;
        private System.Windows.Forms.ToolStripButton tsbStepInsert;
        private System.Windows.Forms.ToolStripButton tsbStepDelete;
        private System.Windows.Forms.ToolStripButton tsbStepReplay;
        private System.Windows.Forms.ToolStripSeparator tsbStepSep2;
        private System.Windows.Forms.ToolStripButton tsbStepRunAll;
        private System.Windows.Forms.ToolStripButton tsbStepClearAll;
        private System.Windows.Forms.ToolStripSeparator tsbStepSep3;
        private System.Windows.Forms.ToolStripButton tsbStepExportAllPlaywrightTs;
        private System.Windows.Forms.ToolStripButton tsbStepExportAllSeleniumTs;
        private System.Windows.Forms.ToolStripSeparator tsbStepSep1;
        private System.Windows.Forms.Label lblStepVisualization;
        private System.Windows.Forms.ContextMenuStrip cmsPerfGrid;
        private System.Windows.Forms.ToolStripMenuItem tsmPerfCtxIgnore;
        private System.Windows.Forms.ToolStripSeparator tsmPerfCtxSep1;
        private System.Windows.Forms.ToolStripMenuItem tsmPerfCtxExport;
        private System.Windows.Forms.ToolStripMenuItem tsmPerfCtxImport;
        private System.Windows.Forms.ContextMenuStrip cmsStepsGrid;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsRun;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsDelete;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsHighlight;
        private System.Windows.Forms.ToolStripSeparator tsmStepsSep1;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsExport;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsInsert;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsPrettyPaint;
        private System.Windows.Forms.ToolStripSeparator tsmStepsSep2;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsExportPlaywrightTs;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsExportSeleniumTs;
        private System.Windows.Forms.ToolStripSeparator tsmStepsSep3;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsExportAllPlaywrightTs;
        private System.Windows.Forms.ToolStripMenuItem tsmStepsExportAllSeleniumTs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjPropName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjPropValue;
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
        private System.Windows.Forms.ContextMenuStrip cmsObjectTree;
        private System.Windows.Forms.ToolStripMenuItem tsmObjectTreeAddStep;
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
            this.components = new System.ComponentModel.Container();
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileLoad = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileClear = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileExport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileImport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSep = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecord = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecordCaptureSemantic = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecordCapturePlain = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecordSepExport = new System.Windows.Forms.ToolStripSeparator();
            this.menuRecordExportAllPlaywrightTs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecordExportAllSeleniumTs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLanguage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLangEnglish = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpLangChinese = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolMain = new System.Windows.Forms.ToolStrip();
            this.tslBrand = new System.Windows.Forms.ToolStripLabel();
            this.tsbSepBrand = new System.Windows.Forms.ToolStripSeparator();
            this.tsbTarget = new System.Windows.Forms.ToolStripButton();
            this.tsbRecord = new System.Windows.Forms.ToolStripButton();
            this.tsddbRecordCapture = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmiRecordCaptureSemantic = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRecordCapturePlain = new System.Windows.Forms.ToolStripMenuItem();
            this.tsbReplay = new System.Windows.Forms.ToolStripButton();
            this.tsbSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripButton();
            this.tsbImport = new System.Windows.Forms.ToolStripButton();
            this.tsbSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbSepReload = new System.Windows.Forms.ToolStripSeparator();
            this.tsbReloadEngine = new System.Windows.Forms.ToolStripButton();
            this.tsbSepSync = new System.Windows.Forms.ToolStripSeparator();
            this.tshSyncFocusHost = new System.Windows.Forms.CheckBox();
            this.toolPerf = new System.Windows.Forms.ToolStrip();
            this.tsbSepPerf = new System.Windows.Forms.ToolStripSeparator();
            this.tshWithPerfTestHost = new System.Windows.Forms.CheckBox();
            this.tsbRunPerf = new System.Windows.Forms.ToolStripButton();
            this.tsbStopPerf = new System.Windows.Forms.ToolStripButton();
            this.tsbRunPerfSelected = new System.Windows.Forms.ToolStripButton();
            this.tsbSepPerfScripts = new System.Windows.Forms.ToolStripSeparator();
            this.tsbPerfExportPlaywrightTs = new System.Windows.Forms.ToolStripButton();
            this.tsbPerfExportSeleniumTs = new System.Windows.Forms.ToolStripButton();
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
            this.cmsObjectTree = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmObjectTreeAddStep = new System.Windows.Forms.ToolStripMenuItem();
            this.gridObjectProps = new System.Windows.Forms.DataGridView();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.value = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.cmsStepsGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmStepsRun = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsHighlight = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmStepsExport = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsInsert = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsPrettyPaint = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmStepsExportPlaywrightTs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsExportSeleniumTs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmStepsExportAllPlaywrightTs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmStepsExportAllSeleniumTs = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripStepCommands = new System.Windows.Forms.ToolStrip();
            this.tsbStepInsert = new System.Windows.Forms.ToolStripButton();
            this.tsbStepDelete = new System.Windows.Forms.ToolStripButton();
            this.tsbStepSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbStepReplay = new System.Windows.Forms.ToolStripButton();
            this.tsbStepSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbStepRunAll = new System.Windows.Forms.ToolStripButton();
            this.tsbStepClearAll = new System.Windows.Forms.ToolStripButton();
            this.tsbStepSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbStepExportAllPlaywrightTs = new System.Windows.Forms.ToolStripButton();
            this.tsbStepExportAllSeleniumTs = new System.Windows.Forms.ToolStripButton();
            this.lblStepVisualization = new System.Windows.Forms.Label();
            this.panelRecordPerfPreview = new System.Windows.Forms.Panel();
            this.splitRecordPerfPreview = new System.Windows.Forms.SplitContainer();
            this.gridPerfAnchorPreview = new System.Windows.Forms.DataGridView();
            this.cmsPerfGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmPerfCtxIgnore = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmPerfCtxSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmPerfCtxExport = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmPerfCtxImport = new System.Windows.Forms.ToolStripMenuItem();
            this.lblPerfDesignAnchorSummary = new System.Windows.Forms.Label();
            this.gridPerfRuntimePreview = new System.Windows.Forms.DataGridView();
            this.lblPerfDesignRuntime = new System.Windows.Forms.Label();
            this.lblPerfDesignTitle = new System.Windows.Forms.Label();
            this.splitRecordCanvasProps = new System.Windows.Forms.SplitContainer();
            this.panelRecordCanvasPreview = new System.Windows.Forms.Panel();
            this.recordWebView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.toolStripRecordCanvas = new System.Windows.Forms.ToolStrip();
            this.tsbRecordCanvasZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsbRecordCanvasZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsbRecordCanvasSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbRecordCanvasCenter = new System.Windows.Forms.ToolStripButton();
            this.tsbRecordCanvasSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tslRecordCanvasZoom = new System.Windows.Forms.ToolStripLabel();
            this.stepObjectPropertyPanel = new MARS.WebAutomation.UI.StepObjectPropertyPanel();
            this.lblRecordHint = new System.Windows.Forms.Label();
            this.tabApiPerformance = new System.Windows.Forms.TabPage();
            this.layoutApiPerf = new System.Windows.Forms.TableLayoutPanel();
            this._lblApiPerfHint = new System.Windows.Forms.Label();
            this.splitApiPerf = new System.Windows.Forms.SplitContainer();
            this._gridApiDefinitions = new System.Windows.Forms.DataGridView();
            this.layoutApiEditor = new System.Windows.Forms.TableLayoutPanel();
            this.lblApiName = new System.Windows.Forms.Label();
            this._txtApiName = new System.Windows.Forms.TextBox();
            this.lblApiMethod = new System.Windows.Forms.Label();
            this._cmbApiMethod = new System.Windows.Forms.ComboBox();
            this.lblApiUrl = new System.Windows.Forms.Label();
            this._txtApiUrl = new System.Windows.Forms.TextBox();
            this.lblApiHeaders = new System.Windows.Forms.Label();
            this._gridApiHeaders = new System.Windows.Forms.DataGridView();
            this.colApiHeaderKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colApiHeaderValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblApiSecurity = new System.Windows.Forms.Label();
            this._cmbApiSecurity = new System.Windows.Forms.ComboBox();
            this.lblApiSecurityValue = new System.Windows.Forms.Label();
            this._txtApiSecurityValue = new System.Windows.Forms.TextBox();
            this.lblApiPayload = new System.Windows.Forms.Label();
            this._txtApiPayload = new System.Windows.Forms.TextBox();
            this.lblApiExpectedStatus = new System.Windows.Forms.Label();
            this._numApiExpectedStatus = new System.Windows.Forms.NumericUpDown();
            this.lblApiGroup = new System.Windows.Forms.Label();
            this._txtApiGroup = new System.Windows.Forms.TextBox();
            this.flowApiDefButtons = new System.Windows.Forms.FlowLayoutPanel();
            this._btnApiDefNew = new System.Windows.Forms.Button();
            this._btnApiDefSave = new System.Windows.Forms.Button();
            this._btnApiDefDelete = new System.Windows.Forms.Button();
            this.flowApiPerfActions = new System.Windows.Forms.FlowLayoutPanel();
            this._btnApiPerfRun = new System.Windows.Forms.Button();
            this._btnApiPerfRunSelected = new System.Windows.Forms.Button();
            this._btnApiPerfRunAll = new System.Windows.Forms.Button();
            this._btnApiPerfConfig = new System.Windows.Forms.Button();
            this._btnApiPerfExport = new System.Windows.Forms.Button();
            this._btnApiPerfImport = new System.Windows.Forms.Button();
            this._btnApiPerfGoRecord = new System.Windows.Forms.Button();
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
            this.cmsObjectTree.SuspendLayout();
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
            this.cmsStepsGrid.SuspendLayout();
            this.toolStripStepCommands.SuspendLayout();
            this.panelRecordPerfPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordPerfPreview)).BeginInit();
            this.splitRecordPerfPreview.Panel1.SuspendLayout();
            this.splitRecordPerfPreview.Panel2.SuspendLayout();
            this.splitRecordPerfPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfAnchorPreview)).BeginInit();
            this.cmsPerfGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfRuntimePreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordCanvasProps)).BeginInit();
            this.splitRecordCanvasProps.Panel1.SuspendLayout();
            this.splitRecordCanvasProps.Panel2.SuspendLayout();
            this.splitRecordCanvasProps.SuspendLayout();
            this.panelRecordCanvasPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.recordWebView)).BeginInit();
            this.toolStripRecordCanvas.SuspendLayout();
            this.tabApiPerformance.SuspendLayout();
            this.layoutApiPerf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitApiPerf)).BeginInit();
            this.splitApiPerf.Panel1.SuspendLayout();
            this.splitApiPerf.Panel2.SuspendLayout();
            this.splitApiPerf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridApiDefinitions)).BeginInit();
            this.layoutApiEditor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridApiHeaders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numApiExpectedStatus)).BeginInit();
            this.flowApiDefButtons.SuspendLayout();
            this.flowApiPerfActions.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.layoutSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).BeginInit();
            this.SuspendLayout();
            // 
            // menuMain
            // 
            this.menuMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuRecord,
            this.menuHelp});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuMain.Size = new System.Drawing.Size(898, 24);
            this.menuMain.TabIndex = 0;
            this.menuMain.Text = "menuStrip1";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileSave,
            this.menuFileLoad,
            this.menuFileClear,
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
            this.menuFileSave.Size = new System.Drawing.Size(157, 22);
            this.menuFileSave.Text = "Save";
            this.menuFileSave.Click += new System.EventHandler(this.menuFileSave_Click);
            // 
            // menuFileLoad
            // 
            this.menuFileLoad.Name = "menuFileLoad";
            this.menuFileLoad.Size = new System.Drawing.Size(157, 22);
            this.menuFileLoad.Text = "Load…";
            this.menuFileLoad.Click += new System.EventHandler(this.menuFileLoad_Click);
            // 
            // menuFileClear
            // 
            this.menuFileClear.Name = "menuFileClear";
            this.menuFileClear.Size = new System.Drawing.Size(157, 22);
            this.menuFileClear.Text = "Clear";
            this.menuFileClear.Click += new System.EventHandler(this.menuFileClear_Click);
            // 
            // menuFileExport
            // 
            this.menuFileExport.Name = "menuFileExport";
            this.menuFileExport.Size = new System.Drawing.Size(157, 22);
            this.menuFileExport.Text = "Export Object…";
            this.menuFileExport.Click += new System.EventHandler(this.menuFileExport_Click);
            // 
            // menuFileImport
            // 
            this.menuFileImport.Name = "menuFileImport";
            this.menuFileImport.Size = new System.Drawing.Size(157, 22);
            this.menuFileImport.Text = "Import Object…";
            this.menuFileImport.Click += new System.EventHandler(this.menuFileImport_Click);
            // 
            // menuFileSep
            // 
            this.menuFileSep.Name = "menuFileSep";
            this.menuFileSep.Size = new System.Drawing.Size(154, 6);
            // 
            // menuFileExit
            // 
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.Size = new System.Drawing.Size(157, 22);
            this.menuFileExit.Text = "Exit";
            this.menuFileExit.Click += new System.EventHandler(this.menuFileExit_Click);
            // 
            // menuRecord
            // 
            this.menuRecord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRecordCaptureSemantic,
            this.menuRecordCapturePlain,
            this.menuRecordSepExport,
            this.menuRecordExportAllPlaywrightTs,
            this.menuRecordExportAllSeleniumTs});
            this.menuRecord.Name = "menuRecord";
            this.menuRecord.Size = new System.Drawing.Size(56, 20);
            this.menuRecord.Text = "Record";
            // 
            // menuRecordCaptureSemantic
            // 
            this.menuRecordCaptureSemantic.Name = "menuRecordCaptureSemantic";
            this.menuRecordCaptureSemantic.Size = new System.Drawing.Size(250, 22);
            this.menuRecordCaptureSemantic.Text = "Capture: semantic";
            this.menuRecordCaptureSemantic.Click += new System.EventHandler(this.menuRecordCaptureSemantic_Click);
            // 
            // menuRecordCapturePlain
            // 
            this.menuRecordCapturePlain.Name = "menuRecordCapturePlain";
            this.menuRecordCapturePlain.Size = new System.Drawing.Size(250, 22);
            this.menuRecordCapturePlain.Text = "Capture: plain (event target)";
            this.menuRecordCapturePlain.Click += new System.EventHandler(this.menuRecordCapturePlain_Click);
            // 
            // menuRecordSepExport
            // 
            this.menuRecordSepExport.Name = "menuRecordSepExport";
            this.menuRecordSepExport.Size = new System.Drawing.Size(247, 6);
            // 
            // menuRecordExportAllPlaywrightTs
            // 
            this.menuRecordExportAllPlaywrightTs.Name = "menuRecordExportAllPlaywrightTs";
            this.menuRecordExportAllPlaywrightTs.Size = new System.Drawing.Size(250, 22);
            this.menuRecordExportAllPlaywrightTs.Text = "Export all steps — Playwright (TS)";
            this.menuRecordExportAllPlaywrightTs.Click += new System.EventHandler(this.menuRecordExportAllPlaywrightTs_Click);
            // 
            // menuRecordExportAllSeleniumTs
            // 
            this.menuRecordExportAllSeleniumTs.Name = "menuRecordExportAllSeleniumTs";
            this.menuRecordExportAllSeleniumTs.Size = new System.Drawing.Size(250, 22);
            this.menuRecordExportAllSeleniumTs.Text = "Export all steps — Selenium (TS)";
            this.menuRecordExportAllSeleniumTs.Click += new System.EventHandler(this.menuRecordExportAllSeleniumTs_Click);
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
            this.tsddbRecordCapture,
            this.tsbReplay,
            this.tsbSep1,
            this.tsbExport,
            this.tsbImport,
            this.tsbSep2,
            this.tsbSave,
            this.tsbSepReload,
            this.tsbReloadEngine,
            this.tsbSepSync,
            this.tshSyncFocusHost});
            this.toolMain.Location = new System.Drawing.Point(0, 24);
            this.toolMain.Name = "toolMain";
            this.toolMain.Size = new System.Drawing.Size(898, 25);
            this.toolMain.TabIndex = 1;
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
            // tsddbRecordCapture
            // 
            this.tsddbRecordCapture.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsddbRecordCapture.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiRecordCaptureSemantic,
            this.tsmiRecordCapturePlain});
            this.tsddbRecordCapture.Name = "tsddbRecordCapture";
            this.tsddbRecordCapture.Size = new System.Drawing.Size(116, 22);
            this.tsddbRecordCapture.Text = "Capture: semantic";
            // 
            // tsmiRecordCaptureSemantic
            // 
            this.tsmiRecordCaptureSemantic.Name = "tsmiRecordCaptureSemantic";
            this.tsmiRecordCaptureSemantic.Size = new System.Drawing.Size(217, 22);
            this.tsmiRecordCaptureSemantic.Text = "Semantic (tab/menu/rules)";
            this.tsmiRecordCaptureSemantic.Click += new System.EventHandler(this.tsmiRecordCaptureSemantic_Click);
            // 
            // tsmiRecordCapturePlain
            // 
            this.tsmiRecordCapturePlain.Name = "tsmiRecordCapturePlain";
            this.tsmiRecordCapturePlain.Size = new System.Drawing.Size(217, 22);
            this.tsmiRecordCapturePlain.Text = "Plain (event target + PW)";
            this.tsmiRecordCapturePlain.Click += new System.EventHandler(this.tsmiRecordCapturePlain_Click);
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
            this.tsbExport.Size = new System.Drawing.Size(82, 22);
            this.tsbExport.Text = "Export Object";
            this.tsbExport.Click += new System.EventHandler(this.tsbExport_Click);
            // 
            // tsbImport
            // 
            this.tsbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbImport.Name = "tsbImport";
            this.tsbImport.Size = new System.Drawing.Size(85, 22);
            this.tsbImport.Text = "Import Object";
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
            this.tsbReloadEngine.Name = "tsbReloadEngine";
            this.tsbReloadEngine.Size = new System.Drawing.Size(86, 22);
            this.tsbReloadEngine.Text = "Reload engine";
            this.tsbReloadEngine.Click += new System.EventHandler(this.tsbReloadEngine_Click);
            // 
            // tsbSepSync
            // 
            this.tsbSepSync.Name = "tsbSepSync";
            this.tsbSepSync.Size = new System.Drawing.Size(6, 25);
            // 
            // tshSyncFocusHost
            // 
            this.tshSyncFocusHost.AccessibleName = "tshSyncFocusHost";
            this.tshSyncFocusHost.AutoSize = true;
            this.tshSyncFocusHost.Location = new System.Drawing.Point(740, 0);
            this.tshSyncFocusHost.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.tshSyncFocusHost.Name = "tshSyncFocusHost";
            this.tshSyncFocusHost.Size = new System.Drawing.Size(130, 24);
            this.tshSyncFocusHost.TabIndex = 0;
            this.tshSyncFocusHost.UseVisualStyleBackColor = true;
            // 
            // tshSyncFocusHost
            // 
            this.tshSyncFocusHost.AutoSize = false;
            this.tshSyncFocusHost.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.tshSyncFocusHost.Name = "tshSyncFocusHost";
            this.tshSyncFocusHost.Size = new System.Drawing.Size(130, 24);
            // 
            // toolPerf
            // 
            this.toolPerf.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolPerf.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbSepPerf,
            this.tshWithPerfTestHost,
            this.tsbRunPerf,
            this.tsbStopPerf,
            this.tsbRunPerfSelected,
            this.tsbSepPerfScripts,
            this.tsbPerfExportPlaywrightTs,
            this.tsbPerfExportSeleniumTs});
            this.toolPerf.Location = new System.Drawing.Point(0, 49);
            this.toolPerf.Name = "toolPerf";
            this.toolPerf.Size = new System.Drawing.Size(898, 25);
            this.toolPerf.TabIndex = 4;
            // 
            // tsbSepPerf
            // 
            this.tsbSepPerf.Name = "tsbSepPerf";
            this.tsbSepPerf.Size = new System.Drawing.Size(6, 25);
            // 
            // tshWithPerfTestHost
            // 
            this.tshWithPerfTestHost.AccessibleName = "tshWithPerfTestHost";
            this.tshWithPerfTestHost.AutoSize = true;
            this.tshWithPerfTestHost.Checked = true;
            this.tshWithPerfTestHost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tshWithPerfTestHost.Location = new System.Drawing.Point(17, 1);
            this.tshWithPerfTestHost.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.tshWithPerfTestHost.Name = "tshWithPerfTestHost";
            this.tshWithPerfTestHost.Size = new System.Drawing.Size(200, 22);
            this.tshWithPerfTestHost.TabIndex = 0;
            this.tshWithPerfTestHost.UseVisualStyleBackColor = true;
            // 
            // tshWithPerfTestHost
            // 
            this.tshWithPerfTestHost.AutoSize = false;
            this.tshWithPerfTestHost.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.tshWithPerfTestHost.Name = "tshWithPerfTestHost";
            this.tshWithPerfTestHost.Size = new System.Drawing.Size(200, 22);
            // 
            // tsbRunPerf
            // 
            this.tsbRunPerf.Name = "tsbRunPerf";
            this.tsbRunPerf.Size = new System.Drawing.Size(56, 22);
            this.tsbRunPerf.Text = "Run Perf";
            this.tsbRunPerf.Click += new System.EventHandler(this.tsbRunPerf_Click);
            // 
            // tsbStopPerf
            // 
            this.tsbStopPerf.Enabled = false;
            this.tsbStopPerf.Name = "tsbStopPerf";
            this.tsbStopPerf.Size = new System.Drawing.Size(35, 22);
            this.tsbStopPerf.Text = "Stop";
            this.tsbStopPerf.Click += new System.EventHandler(this.tsbStopPerf_Click);
            // 
            // tsbRunPerfSelected
            // 
            this.tsbRunPerfSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRunPerfSelected.Name = "tsbRunPerfSelected";
            this.tsbRunPerfSelected.Size = new System.Drawing.Size(121, 22);
            this.tsbRunPerfSelected.Text = "Run Selected Anchor";
            this.tsbRunPerfSelected.Click += new System.EventHandler(this.tsbRunPerfSelected_Click);
            // 
            // tsbSepPerfScripts
            // 
            this.tsbSepPerfScripts.Name = "tsbSepPerfScripts";
            this.tsbSepPerfScripts.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbPerfExportPlaywrightTs
            // 
            this.tsbPerfExportPlaywrightTs.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsbPerfExportPlaywrightTs.Name = "tsbPerfExportPlaywrightTs";
            this.tsbPerfExportPlaywrightTs.Size = new System.Drawing.Size(45, 22);
            this.tsbPerfExportPlaywrightTs.Text = "PW TS";
            this.tsbPerfExportPlaywrightTs.Click += new System.EventHandler(this.tsbPerfExportPlaywrightTs_Click);
            // 
            // tsbPerfExportSeleniumTs
            // 
            this.tsbPerfExportSeleniumTs.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsbPerfExportSeleniumTs.Name = "tsbPerfExportSeleniumTs";
            this.tsbPerfExportSeleniumTs.Size = new System.Drawing.Size(39, 22);
            this.tsbPerfExportSeleniumTs.Text = "Se TS";
            this.tsbPerfExportSeleniumTs.Click += new System.EventHandler(this.tsbPerfExportSeleniumTs_Click);
            // 
            // statusMain
            // 
            this.statusMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusMain.Location = new System.Drawing.Point(0, 554);
            this.statusMain.Name = "statusMain";
            this.statusMain.Size = new System.Drawing.Size(898, 22);
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
            this.tabMain.Controls.Add(this.tabApiPerformance);
            this.tabMain.Controls.Add(this.tabSettings);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 74);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(898, 480);
            this.tabMain.TabIndex = 3;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // tabTarget
            // 
            this.tabTarget.Controls.Add(this.panelTargetCard);
            this.tabTarget.Location = new System.Drawing.Point(4, 22);
            this.tabTarget.Name = "tabTarget";
            this.tabTarget.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.tabTarget.Size = new System.Drawing.Size(890, 454);
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
            this.panelTargetCard.Size = new System.Drawing.Size(866, 434);
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
            this.layoutTarget.Size = new System.Drawing.Size(866, 434);
            this.layoutTarget.TabIndex = 3;
            // 
            // lblSectionUrl
            // 
            this.lblSectionUrl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.layoutTarget.SetColumnSpan(this.lblSectionUrl, 3);
            this.lblSectionUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblSectionUrl.Location = new System.Drawing.Point(212, 0);
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
            this.btnStartBrowser.Location = new System.Drawing.Point(586, 20);
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
            this.txtUrl.Size = new System.Drawing.Size(574, 20);
            this.txtUrl.TabIndex = 0;
            // 
            // btnNavigate
            // 
            this.btnNavigate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNavigate.Location = new System.Drawing.Point(727, 20);
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
            this.lblScheme.Size = new System.Drawing.Size(862, 19);
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
            this.lblHost.Size = new System.Drawing.Size(862, 19);
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
            this.lblPort.Size = new System.Drawing.Size(862, 19);
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
            this.lblPath.Size = new System.Drawing.Size(862, 19);
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
            this.lblQuery.Size = new System.Drawing.Size(862, 19);
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
            this.tabObjects.Size = new System.Drawing.Size(890, 454);
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
            this.splitObjects.Size = new System.Drawing.Size(882, 414);
            this.splitObjects.SplitterDistance = 368;
            this.splitObjects.TabIndex = 1;
            // 
            // treeObjects
            // 
            this.treeObjects.ContextMenuStrip = this.cmsObjectTree;
            this.treeObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeObjects.Location = new System.Drawing.Point(0, 0);
            this.treeObjects.Name = "treeObjects";
            this.treeObjects.Size = new System.Drawing.Size(368, 414);
            this.treeObjects.TabIndex = 0;
            this.treeObjects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeObjects_AfterSelect);
            this.treeObjects.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeObjects_NodeMouseClick);
            this.treeObjects.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeObjects_NodeMouseDoubleClick);
            // 
            // cmsObjectTree
            // 
            this.cmsObjectTree.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmObjectTreeAddStep});
            this.cmsObjectTree.Name = "cmsObjectTree";
            this.cmsObjectTree.Size = new System.Drawing.Size(167, 26);
            // 
            // tsmObjectTreeAddStep
            // 
            this.tsmObjectTreeAddStep.Name = "tsmObjectTreeAddStep";
            this.tsmObjectTreeAddStep.Size = new System.Drawing.Size(166, 22);
            this.tsmObjectTreeAddStep.Text = "Add as test step…";
            this.tsmObjectTreeAddStep.Click += new System.EventHandler(this.tsmObjectTreeAddStep_Click);
            // 
            // gridObjectProps
            // 
            this.gridObjectProps.AllowUserToAddRows = false;
            this.gridObjectProps.AllowUserToDeleteRows = false;
            this.gridObjectProps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridObjectProps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridObjectProps.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.name,
            this.value});
            this.gridObjectProps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridObjectProps.Location = new System.Drawing.Point(0, 0);
            this.gridObjectProps.Name = "gridObjectProps";
            this.gridObjectProps.ReadOnly = true;
            this.gridObjectProps.RowHeadersVisible = false;
            this.gridObjectProps.RowHeadersWidth = 51;
            this.gridObjectProps.Size = new System.Drawing.Size(510, 414);
            this.gridObjectProps.TabIndex = 0;
            // 
            // name
            // 
            this.name.HeaderText = "Property";
            this.name.MinimumWidth = 6;
            this.name.Name = "name";
            this.name.ReadOnly = true;
            // 
            // value
            // 
            this.value.HeaderText = "Value";
            this.value.MinimumWidth = 6;
            this.value.Name = "value";
            this.value.ReadOnly = true;
            // 
            // panelObjectsToolbar
            // 
            this.panelObjectsToolbar.Controls.Add(this.flowObjectsToolbar);
            this.panelObjectsToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelObjectsToolbar.Location = new System.Drawing.Point(4, 4);
            this.panelObjectsToolbar.Name = "panelObjectsToolbar";
            this.panelObjectsToolbar.Size = new System.Drawing.Size(882, 32);
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
            this.flowObjectsToolbar.Size = new System.Drawing.Size(882, 32);
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
            this.tabRecord.Size = new System.Drawing.Size(890, 454);
            this.tabRecord.TabIndex = 2;
            this.tabRecord.Text = "Record / Replay";
            this.tabRecord.UseVisualStyleBackColor = true;
            this.tabRecord.SizeChanged += new System.EventHandler(this.TabRecord_SizeChanged);
            // 
            // splitRecordMainPreview
            // 
            this.splitRecordMainPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitRecordMainPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRecordMainPreview.Location = new System.Drawing.Point(4, 37);
            this.splitRecordMainPreview.Name = "splitRecordMainPreview";
            // 
            // splitRecordMainPreview.Panel1
            // 
            this.splitRecordMainPreview.Panel1.Controls.Add(this.splitRecordWorkPreview);
            this.splitRecordMainPreview.Panel1MinSize = 260;
            // 
            // splitRecordMainPreview.Panel2
            // 
            this.splitRecordMainPreview.Panel2.Controls.Add(this.splitRecordCanvasProps);
            this.splitRecordMainPreview.Panel2MinSize = 160;
            this.splitRecordMainPreview.Size = new System.Drawing.Size(882, 413);
            this.splitRecordMainPreview.SplitterDistance = 554;
            this.splitRecordMainPreview.SplitterWidth = 6;
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
            this.splitRecordWorkPreview.Panel1MinSize = 120;
            // 
            // splitRecordWorkPreview.Panel2
            // 
            this.splitRecordWorkPreview.Panel2.Controls.Add(this.panelRecordPerfPreview);
            this.splitRecordWorkPreview.Panel2MinSize = 90;
            this.splitRecordWorkPreview.Size = new System.Drawing.Size(552, 411);
            this.splitRecordWorkPreview.SplitterDistance = 207;
            this.splitRecordWorkPreview.SplitterWidth = 6;
            this.splitRecordWorkPreview.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gridSteps);
            this.panel1.Controls.Add(this.toolStripStepCommands);
            this.panel1.Controls.Add(this.lblStepVisualization);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(552, 207);
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
            this.gridSteps.ContextMenuStrip = this.cmsStepsGrid;
            this.gridSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSteps.Location = new System.Drawing.Point(0, 47);
            this.gridSteps.Name = "gridSteps";
            this.gridSteps.ReadOnly = true;
            this.gridSteps.RowHeadersVisible = false;
            this.gridSteps.RowHeadersWidth = 51;
            this.gridSteps.Size = new System.Drawing.Size(552, 160);
            this.gridSteps.TabIndex = 1;
            this.gridSteps.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSteps_CellContentClick);
            this.gridSteps.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSteps_CellDoubleClick);
            this.gridSteps.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSteps_CellEndEdit);
            this.gridSteps.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.gridSteps_CellFormatting);
            this.gridSteps.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridSteps_CellMouseClick);
            this.gridSteps.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.gridSteps_CellPainting);
            this.gridSteps.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.Grid_DataError);
            this.gridSteps.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.gridSteps_RowPrePaint);
            this.gridSteps.SelectionChanged += new System.EventHandler(this.gridSteps_SelectionChanged);
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
            // cmsStepsGrid
            // 
            this.cmsStepsGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmStepsRun,
            this.tsmStepsDelete,
            this.tsmStepsHighlight,
            this.tsmStepsSep1,
            this.tsmStepsExport,
            this.tsmStepsInsert,
            this.tsmStepsPrettyPaint,
            this.tsmStepsSep2,
            this.tsmStepsExportPlaywrightTs,
            this.tsmStepsExportSeleniumTs,
            this.tsmStepsSep3,
            this.tsmStepsExportAllPlaywrightTs,
            this.tsmStepsExportAllSeleniumTs});
            this.cmsStepsGrid.Name = "cmsStepsGrid";
            this.cmsStepsGrid.Size = new System.Drawing.Size(221, 242);
            // 
            // tsmStepsRun
            // 
            this.tsmStepsRun.Name = "tsmStepsRun";
            this.tsmStepsRun.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsRun.Text = "Run";
            this.tsmStepsRun.Click += new System.EventHandler(this.tsmStepsRun_Click);
            // 
            // tsmStepsDelete
            // 
            this.tsmStepsDelete.Name = "tsmStepsDelete";
            this.tsmStepsDelete.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsDelete.Text = "Delete";
            this.tsmStepsDelete.Click += new System.EventHandler(this.tsmStepsDelete_Click);
            // 
            // tsmStepsHighlight
            // 
            this.tsmStepsHighlight.Name = "tsmStepsHighlight";
            this.tsmStepsHighlight.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsHighlight.Text = "Highlight";
            this.tsmStepsHighlight.Click += new System.EventHandler(this.tsmStepsHighlight_Click);
            // 
            // tsmStepsSep1
            // 
            this.tsmStepsSep1.Name = "tsmStepsSep1";
            this.tsmStepsSep1.Size = new System.Drawing.Size(217, 6);
            // 
            // tsmStepsExport
            // 
            this.tsmStepsExport.Name = "tsmStepsExport";
            this.tsmStepsExport.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsExport.Text = "Export";
            this.tsmStepsExport.Click += new System.EventHandler(this.tsmStepsExport_Click);
            // 
            // tsmStepsInsert
            // 
            this.tsmStepsInsert.Name = "tsmStepsInsert";
            this.tsmStepsInsert.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsInsert.Text = "Insert row";
            this.tsmStepsInsert.Click += new System.EventHandler(this.tsmStepsInsert_Click);
            // 
            // tsmStepsPrettyPaint
            // 
            this.tsmStepsPrettyPaint.Name = "tsmStepsPrettyPaint";
            this.tsmStepsPrettyPaint.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsPrettyPaint.Text = "Pretty Paint";
            this.tsmStepsPrettyPaint.Click += new System.EventHandler(this.tsmStepsPrettyPaint_Click);
            // 
            // tsmStepsSep2
            // 
            this.tsmStepsSep2.Name = "tsmStepsSep2";
            this.tsmStepsSep2.Size = new System.Drawing.Size(217, 6);
            // 
            // tsmStepsExportPlaywrightTs
            // 
            this.tsmStepsExportPlaywrightTs.Name = "tsmStepsExportPlaywrightTs";
            this.tsmStepsExportPlaywrightTs.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsExportPlaywrightTs.Text = "Export Playwright (TS)";
            this.tsmStepsExportPlaywrightTs.Click += new System.EventHandler(this.tsmStepsExportPlaywrightTs_Click);
            // 
            // tsmStepsExportSeleniumTs
            // 
            this.tsmStepsExportSeleniumTs.Name = "tsmStepsExportSeleniumTs";
            this.tsmStepsExportSeleniumTs.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsExportSeleniumTs.Text = "Export Selenium (TS)";
            this.tsmStepsExportSeleniumTs.Click += new System.EventHandler(this.tsmStepsExportSeleniumTs_Click);
            // 
            // tsmStepsSep3
            // 
            this.tsmStepsSep3.Name = "tsmStepsSep3";
            this.tsmStepsSep3.Size = new System.Drawing.Size(217, 6);
            // 
            // tsmStepsExportAllPlaywrightTs
            // 
            this.tsmStepsExportAllPlaywrightTs.Name = "tsmStepsExportAllPlaywrightTs";
            this.tsmStepsExportAllPlaywrightTs.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsExportAllPlaywrightTs.Text = "Export all — Playwright (TS)";
            this.tsmStepsExportAllPlaywrightTs.Click += new System.EventHandler(this.tsmStepsExportAllPlaywrightTs_Click);
            // 
            // tsmStepsExportAllSeleniumTs
            // 
            this.tsmStepsExportAllSeleniumTs.Name = "tsmStepsExportAllSeleniumTs";
            this.tsmStepsExportAllSeleniumTs.Size = new System.Drawing.Size(220, 22);
            this.tsmStepsExportAllSeleniumTs.Text = "Export all — Selenium (TS)";
            this.tsmStepsExportAllSeleniumTs.Click += new System.EventHandler(this.tsmStepsExportAllSeleniumTs_Click);
            // 
            // toolStripStepCommands
            // 
            this.toolStripStepCommands.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripStepCommands.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbStepInsert,
            this.tsbStepDelete,
            this.tsbStepSep1,
            this.tsbStepReplay,
            this.tsbStepSep2,
            this.tsbStepRunAll,
            this.tsbStepClearAll,
            this.tsbStepSep3,
            this.tsbStepExportAllPlaywrightTs,
            this.tsbStepExportAllSeleniumTs});
            this.toolStripStepCommands.Location = new System.Drawing.Point(0, 22);
            this.toolStripStepCommands.Name = "toolStripStepCommands";
            this.toolStripStepCommands.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStripStepCommands.Size = new System.Drawing.Size(552, 25);
            this.toolStripStepCommands.TabIndex = 2;
            this.toolStripStepCommands.Text = "toolStripStepCommands";
            // 
            // tsbStepInsert
            // 
            this.tsbStepInsert.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepInsert.Name = "tsbStepInsert";
            this.tsbStepInsert.Size = new System.Drawing.Size(23, 22);
            this.tsbStepInsert.Text = "+";
            this.tsbStepInsert.Click += new System.EventHandler(this.tsbStepInsert_Click);
            // 
            // tsbStepDelete
            // 
            this.tsbStepDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepDelete.Name = "tsbStepDelete";
            this.tsbStepDelete.Size = new System.Drawing.Size(23, 22);
            this.tsbStepDelete.Text = "−";
            this.tsbStepDelete.Click += new System.EventHandler(this.tsbStepDelete_Click);
            // 
            // tsbStepSep1
            // 
            this.tsbStepSep1.Name = "tsbStepSep1";
            this.tsbStepSep1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbStepReplay
            // 
            this.tsbStepReplay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepReplay.Name = "tsbStepReplay";
            this.tsbStepReplay.Size = new System.Drawing.Size(23, 22);
            this.tsbStepReplay.Text = "▶";
            this.tsbStepReplay.Click += new System.EventHandler(this.tsbStepReplay_Click);
            // 
            // tsbStepSep2
            // 
            this.tsbStepSep2.Name = "tsbStepSep2";
            this.tsbStepSep2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbStepRunAll
            // 
            this.tsbStepRunAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepRunAll.Name = "tsbStepRunAll";
            this.tsbStepRunAll.Size = new System.Drawing.Size(31, 22);
            this.tsbStepRunAll.Text = "▶▶";
            this.tsbStepRunAll.Click += new System.EventHandler(this.tsbStepRunAll_Click);
            // 
            // tsbStepClearAll
            // 
            this.tsbStepClearAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbStepClearAll.Name = "tsbStepClearAll";
            this.tsbStepClearAll.Size = new System.Drawing.Size(23, 22);
            this.tsbStepClearAll.Text = "Clear";
            this.tsbStepClearAll.ToolTipText = "Clear all steps";
            this.tsbStepClearAll.Click += new System.EventHandler(this.tsbStepClearAll_Click);
            // 
            // tsbStepSep3
            // 
            this.tsbStepSep3.Name = "tsbStepSep3";
            this.tsbStepSep3.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbStepExportAllPlaywrightTs
            // 
            this.tsbStepExportAllPlaywrightTs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepExportAllPlaywrightTs.Name = "tsbStepExportAllPlaywrightTs";
            this.tsbStepExportAllPlaywrightTs.Size = new System.Drawing.Size(53, 22);
            this.tsbStepExportAllPlaywrightTs.Text = "All→PW";
            this.tsbStepExportAllPlaywrightTs.Click += new System.EventHandler(this.tsbStepExportAllPlaywrightTs_Click);
            // 
            // tsbStepExportAllSeleniumTs
            // 
            this.tsbStepExportAllSeleniumTs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStepExportAllSeleniumTs.Name = "tsbStepExportAllSeleniumTs";
            this.tsbStepExportAllSeleniumTs.Size = new System.Drawing.Size(47, 22);
            this.tsbStepExportAllSeleniumTs.Text = "All→Se";
            this.tsbStepExportAllSeleniumTs.Click += new System.EventHandler(this.tsbStepExportAllSeleniumTs_Click);
            // 
            // lblStepVisualization
            // 
            this.lblStepVisualization.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.lblStepVisualization.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStepVisualization.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.lblStepVisualization.Location = new System.Drawing.Point(0, 0);
            this.lblStepVisualization.Name = "lblStepVisualization";
            this.lblStepVisualization.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStepVisualization.Size = new System.Drawing.Size(552, 22);
            this.lblStepVisualization.TabIndex = 3;
            this.lblStepVisualization.Text = "Visualization";
            this.lblStepVisualization.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.panelRecordPerfPreview.Size = new System.Drawing.Size(552, 198);
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
            this.splitRecordPerfPreview.Panel1MinSize = 100;
            // 
            // splitRecordPerfPreview.Panel2
            // 
            this.splitRecordPerfPreview.Panel2.Controls.Add(this.gridPerfRuntimePreview);
            this.splitRecordPerfPreview.Panel2.Controls.Add(this.lblPerfDesignRuntime);
            this.splitRecordPerfPreview.Panel2MinSize = 60;
            this.splitRecordPerfPreview.Size = new System.Drawing.Size(550, 168);
            this.splitRecordPerfPreview.SplitterDistance = 102;
            this.splitRecordPerfPreview.SplitterWidth = 6;
            this.splitRecordPerfPreview.TabIndex = 2;
            // 
            // gridPerfAnchorPreview
            // 
            this.gridPerfAnchorPreview.AllowUserToAddRows = false;
            this.gridPerfAnchorPreview.AllowUserToDeleteRows = false;
            this.gridPerfAnchorPreview.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.gridPerfAnchorPreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPerfAnchorPreview.ContextMenuStrip = this.cmsPerfGrid;
            this.gridPerfAnchorPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPerfAnchorPreview.Location = new System.Drawing.Point(0, 22);
            this.gridPerfAnchorPreview.Name = "gridPerfAnchorPreview";
            this.gridPerfAnchorPreview.RowHeadersVisible = false;
            this.gridPerfAnchorPreview.RowHeadersWidth = 51;
            this.gridPerfAnchorPreview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPerfAnchorPreview.Size = new System.Drawing.Size(550, 80);
            this.gridPerfAnchorPreview.TabIndex = 10;
            this.gridPerfAnchorPreview.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridPerformance_CellContentClick);
            this.gridPerfAnchorPreview.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridPerformance_CellDoubleClick);
            this.gridPerfAnchorPreview.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.Grid_DataError);
            this.gridPerfAnchorPreview.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.gridPerformance_RowPrePaint);
            this.gridPerfAnchorPreview.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gridPerformance_MouseDown);
            // 
            // cmsPerfGrid
            // 
            this.cmsPerfGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmPerfCtxIgnore,
            this.tsmPerfCtxSep1,
            this.tsmPerfCtxExport,
            this.tsmPerfCtxImport});
            this.cmsPerfGrid.Name = "cmsPerfGrid";
            this.cmsPerfGrid.Size = new System.Drawing.Size(219, 76);
            // 
            // tsmPerfCtxIgnore
            // 
            this.tsmPerfCtxIgnore.Name = "tsmPerfCtxIgnore";
            this.tsmPerfCtxIgnore.Size = new System.Drawing.Size(218, 22);
            this.tsmPerfCtxIgnore.Text = "Ignore…";
            this.tsmPerfCtxIgnore.Click += new System.EventHandler(this.tsmPerfCtxIgnore_Click);
            // 
            // tsmPerfCtxSep1
            // 
            this.tsmPerfCtxSep1.Name = "tsmPerfCtxSep1";
            this.tsmPerfCtxSep1.Size = new System.Drawing.Size(215, 6);
            // 
            // tsmPerfCtxExport
            // 
            this.tsmPerfCtxExport.Name = "tsmPerfCtxExport";
            this.tsmPerfCtxExport.Size = new System.Drawing.Size(218, 22);
            this.tsmPerfCtxExport.Text = "Export Performance Pack…";
            this.tsmPerfCtxExport.Click += new System.EventHandler(this.tsmPerfCtxExport_Click);
            // 
            // tsmPerfCtxImport
            // 
            this.tsmPerfCtxImport.Name = "tsmPerfCtxImport";
            this.tsmPerfCtxImport.Size = new System.Drawing.Size(218, 22);
            this.tsmPerfCtxImport.Text = "Import Performance Pack…";
            this.tsmPerfCtxImport.Click += new System.EventHandler(this.tsmPerfCtxImport_Click);
            // 
            // lblPerfDesignAnchorSummary
            // 
            this.lblPerfDesignAnchorSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.lblPerfDesignAnchorSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignAnchorSummary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.lblPerfDesignAnchorSummary.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignAnchorSummary.Name = "lblPerfDesignAnchorSummary";
            this.lblPerfDesignAnchorSummary.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPerfDesignAnchorSummary.Size = new System.Drawing.Size(550, 22);
            this.lblPerfDesignAnchorSummary.TabIndex = 9;
            this.lblPerfDesignAnchorSummary.Text = "Anchor groups: (design preview)";
            this.lblPerfDesignAnchorSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridPerfRuntimePreview
            // 
            this.gridPerfRuntimePreview.AllowUserToAddRows = false;
            this.gridPerfRuntimePreview.AllowUserToDeleteRows = false;
            this.gridPerfRuntimePreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPerfRuntimePreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPerfRuntimePreview.Location = new System.Drawing.Point(0, 20);
            this.gridPerfRuntimePreview.Name = "gridPerfRuntimePreview";
            this.gridPerfRuntimePreview.ReadOnly = true;
            this.gridPerfRuntimePreview.RowHeadersVisible = false;
            this.gridPerfRuntimePreview.RowHeadersWidth = 51;
            this.gridPerfRuntimePreview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPerfRuntimePreview.Size = new System.Drawing.Size(550, 40);
            this.gridPerfRuntimePreview.TabIndex = 1;
            this.gridPerfRuntimePreview.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridPerfRuntime_CellDoubleClick);
            this.gridPerfRuntimePreview.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.Grid_DataError);
            // 
            // lblPerfDesignRuntime
            // 
            this.lblPerfDesignRuntime.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignRuntime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblPerfDesignRuntime.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignRuntime.Name = "lblPerfDesignRuntime";
            this.lblPerfDesignRuntime.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPerfDesignRuntime.Size = new System.Drawing.Size(550, 20);
            this.lblPerfDesignRuntime.TabIndex = 1;
            this.lblPerfDesignRuntime.Text = "Runtime progress (throughput/error rate)";
            this.lblPerfDesignRuntime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPerfDesignTitle
            // 
            this.lblPerfDesignTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.lblPerfDesignTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPerfDesignTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPerfDesignTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblPerfDesignTitle.Location = new System.Drawing.Point(0, 0);
            this.lblPerfDesignTitle.Name = "lblPerfDesignTitle";
            this.lblPerfDesignTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPerfDesignTitle.Size = new System.Drawing.Size(550, 28);
            this.lblPerfDesignTitle.TabIndex = 0;
            this.lblPerfDesignTitle.Text = "Perform Test anchors / Runtime progress";
            this.lblPerfDesignTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitRecordCanvasProps
            // 
            this.splitRecordCanvasProps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRecordCanvasProps.Location = new System.Drawing.Point(0, 0);
            this.splitRecordCanvasProps.Name = "splitRecordCanvasProps";
            // 
            // splitRecordCanvasProps.Panel1
            // 
            this.splitRecordCanvasProps.Panel1.Controls.Add(this.panelRecordCanvasPreview);
            this.splitRecordCanvasProps.Panel1MinSize = 200;
            // 
            // splitRecordCanvasProps.Panel2
            // 
            this.splitRecordCanvasProps.Panel2.Controls.Add(this.stepObjectPropertyPanel);
            this.splitRecordCanvasProps.Panel2MinSize = 28;
            this.splitRecordCanvasProps.Size = new System.Drawing.Size(320, 411);
            this.splitRecordCanvasProps.SplitterDistance = 220;
            this.splitRecordCanvasProps.SplitterWidth = 6;
            this.splitRecordCanvasProps.TabIndex = 0;
            // 
            // panelRecordCanvasPreview
            // 
            this.panelRecordCanvasPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelRecordCanvasPreview.Controls.Add(this.recordWebView);
            this.panelRecordCanvasPreview.Controls.Add(this.toolStripRecordCanvas);
            this.panelRecordCanvasPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRecordCanvasPreview.Location = new System.Drawing.Point(0, 0);
            this.panelRecordCanvasPreview.Name = "panelRecordCanvasPreview";
            this.panelRecordCanvasPreview.Size = new System.Drawing.Size(220, 411);
            this.panelRecordCanvasPreview.TabIndex = 0;
            // 
            // recordWebView
            // 
            this.recordWebView.AllowExternalDrop = true;
            this.recordWebView.CreationProperties = null;
            this.recordWebView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.recordWebView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recordWebView.Location = new System.Drawing.Point(0, 0);
            this.recordWebView.Name = "recordWebView";
            this.recordWebView.Size = new System.Drawing.Size(218, 409);
            this.recordWebView.TabIndex = 0;
            this.recordWebView.ZoomFactor = 1D;
            this.recordWebView.CoreWebView2InitializationCompleted += new System.EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs>(this.RecordWebView_CoreWebView2InitializationCompleted);
            // 
            // toolStripRecordCanvas
            // 
            this.toolStripRecordCanvas.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripRecordCanvas.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbRecordCanvasZoomOut,
            this.tsbRecordCanvasZoomIn,
            this.tsbRecordCanvasSep1,
            this.tsbRecordCanvasCenter,
            this.tsbRecordCanvasSep2,
            this.tslRecordCanvasZoom});
            this.toolStripRecordCanvas.Location = new System.Drawing.Point(0, 0);
            this.toolStripRecordCanvas.Name = "toolStripRecordCanvas";
            this.toolStripRecordCanvas.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStripRecordCanvas.Size = new System.Drawing.Size(322, 25);
            this.toolStripRecordCanvas.TabIndex = 1;
            this.toolStripRecordCanvas.Text = "toolStripRecordCanvas";
            this.toolStripRecordCanvas.Visible = false;
            // 
            // tsbRecordCanvasZoomOut
            // 
            this.tsbRecordCanvasZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRecordCanvasZoomOut.Name = "tsbRecordCanvasZoomOut";
            this.tsbRecordCanvasZoomOut.Size = new System.Drawing.Size(23, 22);
            this.tsbRecordCanvasZoomOut.Text = "−";
            this.tsbRecordCanvasZoomOut.Click += new System.EventHandler(this.tsbRecordCanvasZoomOut_Click);
            // 
            // tsbRecordCanvasZoomIn
            // 
            this.tsbRecordCanvasZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRecordCanvasZoomIn.Name = "tsbRecordCanvasZoomIn";
            this.tsbRecordCanvasZoomIn.Size = new System.Drawing.Size(23, 22);
            this.tsbRecordCanvasZoomIn.Text = "+";
            this.tsbRecordCanvasZoomIn.Click += new System.EventHandler(this.tsbRecordCanvasZoomIn_Click);
            // 
            // tsbRecordCanvasSep1
            // 
            this.tsbRecordCanvasSep1.Name = "tsbRecordCanvasSep1";
            this.tsbRecordCanvasSep1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbRecordCanvasCenter
            // 
            this.tsbRecordCanvasCenter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRecordCanvasCenter.Name = "tsbRecordCanvasCenter";
            this.tsbRecordCanvasCenter.Size = new System.Drawing.Size(46, 22);
            this.tsbRecordCanvasCenter.Text = "Center";
            this.tsbRecordCanvasCenter.Click += new System.EventHandler(this.tsbRecordCanvasCenter_Click);
            // 
            // tsbRecordCanvasSep2
            // 
            this.tsbRecordCanvasSep2.Name = "tsbRecordCanvasSep2";
            this.tsbRecordCanvasSep2.Size = new System.Drawing.Size(6, 25);
            // 
            // tslRecordCanvasZoom
            // 
            this.tslRecordCanvasZoom.Name = "tslRecordCanvasZoom";
            this.tslRecordCanvasZoom.Size = new System.Drawing.Size(35, 22);
            this.tslRecordCanvasZoom.Text = "100%";
            // 
            // stepObjectPropertyPanel
            // 
            this.stepObjectPropertyPanel.BackColor = System.Drawing.Color.White;
            this.stepObjectPropertyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stepObjectPropertyPanel.ExpandedWidth = 240;
            this.stepObjectPropertyPanel.Location = new System.Drawing.Point(0, 0);
            this.stepObjectPropertyPanel.MinimumSize = new System.Drawing.Size(28, 80);
            this.stepObjectPropertyPanel.Name = "stepObjectPropertyPanel";
            this.stepObjectPropertyPanel.Size = new System.Drawing.Size(94, 411);
            this.stepObjectPropertyPanel.TabIndex = 0;
            // 
            // lblRecordHint
            // 
            this.lblRecordHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecordHint.Location = new System.Drawing.Point(4, 4);
            this.lblRecordHint.Name = "lblRecordHint";
            this.lblRecordHint.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.lblRecordHint.Size = new System.Drawing.Size(882, 33);
            this.lblRecordHint.TabIndex = 2;
            this.lblRecordHint.Text = "Toolbar: Record toggles capture; Replay runs the grid below. Steps use semantic k" +
    "eywords.";
            this.lblRecordHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabApiPerformance
            // 
            this.tabApiPerformance.Controls.Add(this.layoutApiPerf);
            this.tabApiPerformance.Location = new System.Drawing.Point(4, 22);
            this.tabApiPerformance.Name = "tabApiPerformance";
            this.tabApiPerformance.Padding = new System.Windows.Forms.Padding(10);
            this.tabApiPerformance.Size = new System.Drawing.Size(890, 454);
            this.tabApiPerformance.TabIndex = 3;
            this.tabApiPerformance.Text = "API Performance Testing";
            this.tabApiPerformance.UseVisualStyleBackColor = true;
            // 
            // layoutApiPerf
            // 
            this.layoutApiPerf.ColumnCount = 1;
            this.layoutApiPerf.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutApiPerf.Controls.Add(this._lblApiPerfHint, 0, 0);
            this.layoutApiPerf.Controls.Add(this.splitApiPerf, 0, 1);
            this.layoutApiPerf.Controls.Add(this.flowApiPerfActions, 0, 2);
            this.layoutApiPerf.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutApiPerf.Location = new System.Drawing.Point(10, 10);
            this.layoutApiPerf.Name = "layoutApiPerf";
            this.layoutApiPerf.RowCount = 3;
            this.layoutApiPerf.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutApiPerf.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutApiPerf.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.layoutApiPerf.Size = new System.Drawing.Size(870, 434);
            this.layoutApiPerf.TabIndex = 0;
            // 
            // _lblApiPerfHint
            // 
            this._lblApiPerfHint.AutoSize = true;
            this._lblApiPerfHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblApiPerfHint.Location = new System.Drawing.Point(3, 0);
            this._lblApiPerfHint.Name = "_lblApiPerfHint";
            this._lblApiPerfHint.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._lblApiPerfHint.Size = new System.Drawing.Size(864, 21);
            this._lblApiPerfHint.TabIndex = 0;
            this._lblApiPerfHint.Text = "Use this module to execute NBomber API performance testing and manage performance" +
    " packs.";
            // 
            // splitApiPerf
            // 
            this.splitApiPerf.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitApiPerf.Location = new System.Drawing.Point(3, 24);
            this.splitApiPerf.Name = "splitApiPerf";
            // 
            // splitApiPerf.Panel1
            // 
            this.splitApiPerf.Panel1.Controls.Add(this._gridApiDefinitions);
            // 
            // splitApiPerf.Panel2
            // 
            this.splitApiPerf.Panel2.Controls.Add(this.layoutApiEditor);
            this.splitApiPerf.Size = new System.Drawing.Size(864, 327);
            this.splitApiPerf.SplitterDistance = 360;
            this.splitApiPerf.TabIndex = 1;
            // 
            // _gridApiDefinitions
            // 
            this._gridApiDefinitions.AllowUserToAddRows = false;
            this._gridApiDefinitions.AllowUserToDeleteRows = false;
            this._gridApiDefinitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._gridApiDefinitions.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridApiDefinitions.Location = new System.Drawing.Point(0, 0);
            this._gridApiDefinitions.Name = "_gridApiDefinitions";
            this._gridApiDefinitions.RowHeadersVisible = false;
            this._gridApiDefinitions.Size = new System.Drawing.Size(360, 327);
            this._gridApiDefinitions.TabIndex = 0;
            this._gridApiDefinitions.SelectionChanged += new System.EventHandler(this._gridApiDefinitions_SelectionChanged);
            // 
            // layoutApiEditor
            // 
            this.layoutApiEditor.ColumnCount = 2;
            this.layoutApiEditor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.layoutApiEditor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutApiEditor.Controls.Add(this.lblApiName, 0, 0);
            this.layoutApiEditor.Controls.Add(this._txtApiName, 1, 0);
            this.layoutApiEditor.Controls.Add(this.lblApiMethod, 0, 1);
            this.layoutApiEditor.Controls.Add(this._cmbApiMethod, 1, 1);
            this.layoutApiEditor.Controls.Add(this.lblApiUrl, 0, 2);
            this.layoutApiEditor.Controls.Add(this._txtApiUrl, 1, 2);
            this.layoutApiEditor.Controls.Add(this.lblApiHeaders, 0, 3);
            this.layoutApiEditor.Controls.Add(this._gridApiHeaders, 1, 3);
            this.layoutApiEditor.Controls.Add(this.lblApiSecurity, 0, 4);
            this.layoutApiEditor.Controls.Add(this._cmbApiSecurity, 1, 4);
            this.layoutApiEditor.Controls.Add(this.lblApiSecurityValue, 0, 5);
            this.layoutApiEditor.Controls.Add(this._txtApiSecurityValue, 1, 5);
            this.layoutApiEditor.Controls.Add(this.lblApiPayload, 0, 6);
            this.layoutApiEditor.Controls.Add(this._txtApiPayload, 1, 6);
            this.layoutApiEditor.Controls.Add(this.lblApiExpectedStatus, 0, 7);
            this.layoutApiEditor.Controls.Add(this._numApiExpectedStatus, 1, 7);
            this.layoutApiEditor.Controls.Add(this.lblApiGroup, 0, 8);
            this.layoutApiEditor.Controls.Add(this._txtApiGroup, 1, 8);
            this.layoutApiEditor.Controls.Add(this.flowApiDefButtons, 1, 9);
            this.layoutApiEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutApiEditor.Location = new System.Drawing.Point(0, 0);
            this.layoutApiEditor.Name = "layoutApiEditor";
            this.layoutApiEditor.RowCount = 10;
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layoutApiEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutApiEditor.Size = new System.Drawing.Size(500, 327);
            this.layoutApiEditor.TabIndex = 0;
            // 
            // lblApiName
            // 
            this.lblApiName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiName.Location = new System.Drawing.Point(3, 0);
            this.lblApiName.Name = "lblApiName";
            this.lblApiName.Size = new System.Drawing.Size(102, 26);
            this.lblApiName.TabIndex = 0;
            this.lblApiName.Text = "API Name";
            this.lblApiName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtApiName
            // 
            this._txtApiName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtApiName.Location = new System.Drawing.Point(111, 3);
            this._txtApiName.Name = "_txtApiName";
            this._txtApiName.Size = new System.Drawing.Size(386, 20);
            this._txtApiName.TabIndex = 1;
            // 
            // lblApiMethod
            // 
            this.lblApiMethod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiMethod.Location = new System.Drawing.Point(3, 26);
            this.lblApiMethod.Name = "lblApiMethod";
            this.lblApiMethod.Size = new System.Drawing.Size(102, 26);
            this.lblApiMethod.TabIndex = 2;
            this.lblApiMethod.Text = "Method";
            this.lblApiMethod.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cmbApiMethod
            // 
            this._cmbApiMethod.Dock = System.Windows.Forms.DockStyle.Left;
            this._cmbApiMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbApiMethod.FormattingEnabled = true;
            this._cmbApiMethod.Items.AddRange(new object[] {
            "GET",
            "POST",
            "PUT",
            "PATCH",
            "DELETE"});
            this._cmbApiMethod.Location = new System.Drawing.Point(111, 29);
            this._cmbApiMethod.Name = "_cmbApiMethod";
            this._cmbApiMethod.Size = new System.Drawing.Size(140, 21);
            this._cmbApiMethod.TabIndex = 3;
            // 
            // lblApiUrl
            // 
            this.lblApiUrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiUrl.Location = new System.Drawing.Point(3, 52);
            this.lblApiUrl.Name = "lblApiUrl";
            this.lblApiUrl.Size = new System.Drawing.Size(102, 26);
            this.lblApiUrl.TabIndex = 4;
            this.lblApiUrl.Text = "URL";
            this.lblApiUrl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtApiUrl
            // 
            this._txtApiUrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtApiUrl.Location = new System.Drawing.Point(111, 55);
            this._txtApiUrl.Name = "_txtApiUrl";
            this._txtApiUrl.Size = new System.Drawing.Size(386, 20);
            this._txtApiUrl.TabIndex = 5;
            // 
            // lblApiHeaders
            // 
            this.lblApiHeaders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiHeaders.Location = new System.Drawing.Point(3, 78);
            this.lblApiHeaders.Name = "lblApiHeaders";
            this.lblApiHeaders.Size = new System.Drawing.Size(102, 96);
            this.lblApiHeaders.TabIndex = 6;
            this.lblApiHeaders.Text = "HTTP Headers";
            this.lblApiHeaders.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _gridApiHeaders
            // 
            this._gridApiHeaders.AllowUserToResizeColumns = false;
            this._gridApiHeaders.AllowUserToResizeRows = false;
            this._gridApiHeaders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._gridApiHeaders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colApiHeaderKey,
            this.colApiHeaderValue});
            this._gridApiHeaders.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridApiHeaders.Location = new System.Drawing.Point(111, 81);
            this._gridApiHeaders.Name = "_gridApiHeaders";
            this._gridApiHeaders.RowHeadersVisible = false;
            this._gridApiHeaders.Size = new System.Drawing.Size(386, 90);
            this._gridApiHeaders.TabIndex = 7;
            // 
            // colApiHeaderKey
            // 
            this.colApiHeaderKey.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colApiHeaderKey.FillWeight = 40F;
            this.colApiHeaderKey.HeaderText = "Key";
            this.colApiHeaderKey.Name = "colApiHeaderKey";
            // 
            // colApiHeaderValue
            // 
            this.colApiHeaderValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colApiHeaderValue.FillWeight = 60F;
            this.colApiHeaderValue.HeaderText = "Value";
            this.colApiHeaderValue.Name = "colApiHeaderValue";
            // 
            // lblApiSecurity
            // 
            this.lblApiSecurity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiSecurity.Location = new System.Drawing.Point(3, 174);
            this.lblApiSecurity.Name = "lblApiSecurity";
            this.lblApiSecurity.Size = new System.Drawing.Size(102, 26);
            this.lblApiSecurity.TabIndex = 8;
            this.lblApiSecurity.Text = "Security";
            this.lblApiSecurity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cmbApiSecurity
            // 
            this._cmbApiSecurity.Dock = System.Windows.Forms.DockStyle.Left;
            this._cmbApiSecurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbApiSecurity.FormattingEnabled = true;
            this._cmbApiSecurity.Items.AddRange(new object[] {
            "None",
            "Bearer",
            "Basic",
            "ApiKeyHeader"});
            this._cmbApiSecurity.Location = new System.Drawing.Point(111, 177);
            this._cmbApiSecurity.Name = "_cmbApiSecurity";
            this._cmbApiSecurity.Size = new System.Drawing.Size(140, 21);
            this._cmbApiSecurity.TabIndex = 9;
            // 
            // lblApiSecurityValue
            // 
            this.lblApiSecurityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiSecurityValue.Location = new System.Drawing.Point(3, 200);
            this.lblApiSecurityValue.Name = "lblApiSecurityValue";
            this.lblApiSecurityValue.Size = new System.Drawing.Size(102, 26);
            this.lblApiSecurityValue.TabIndex = 10;
            this.lblApiSecurityValue.Text = "Security Value";
            this.lblApiSecurityValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtApiSecurityValue
            // 
            this._txtApiSecurityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtApiSecurityValue.Location = new System.Drawing.Point(111, 203);
            this._txtApiSecurityValue.Name = "_txtApiSecurityValue";
            this._txtApiSecurityValue.Size = new System.Drawing.Size(386, 20);
            this._txtApiSecurityValue.TabIndex = 11;
            // 
            // lblApiPayload
            // 
            this.lblApiPayload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiPayload.Location = new System.Drawing.Point(3, 226);
            this.lblApiPayload.Name = "lblApiPayload";
            this.lblApiPayload.Size = new System.Drawing.Size(102, 40);
            this.lblApiPayload.TabIndex = 12;
            this.lblApiPayload.Text = "Payload";
            this.lblApiPayload.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtApiPayload
            // 
            this._txtApiPayload.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtApiPayload.Location = new System.Drawing.Point(111, 229);
            this._txtApiPayload.Multiline = true;
            this._txtApiPayload.Name = "_txtApiPayload";
            this._txtApiPayload.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtApiPayload.Size = new System.Drawing.Size(386, 34);
            this._txtApiPayload.TabIndex = 13;
            // 
            // lblApiExpectedStatus
            // 
            this.lblApiExpectedStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiExpectedStatus.Location = new System.Drawing.Point(3, 266);
            this.lblApiExpectedStatus.Name = "lblApiExpectedStatus";
            this.lblApiExpectedStatus.Size = new System.Drawing.Size(102, 26);
            this.lblApiExpectedStatus.TabIndex = 14;
            this.lblApiExpectedStatus.Text = "Expected Status";
            this.lblApiExpectedStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _numApiExpectedStatus
            // 
            this._numApiExpectedStatus.Location = new System.Drawing.Point(111, 269);
            this._numApiExpectedStatus.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this._numApiExpectedStatus.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._numApiExpectedStatus.Name = "_numApiExpectedStatus";
            this._numApiExpectedStatus.Size = new System.Drawing.Size(78, 20);
            this._numApiExpectedStatus.TabIndex = 15;
            this._numApiExpectedStatus.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // lblApiGroup
            // 
            this.lblApiGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApiGroup.Location = new System.Drawing.Point(3, 292);
            this.lblApiGroup.Name = "lblApiGroup";
            this.lblApiGroup.Size = new System.Drawing.Size(102, 26);
            this.lblApiGroup.TabIndex = 16;
            this.lblApiGroup.Text = "Group";
            this.lblApiGroup.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtApiGroup
            // 
            this._txtApiGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtApiGroup.Location = new System.Drawing.Point(111, 295);
            this._txtApiGroup.Name = "_txtApiGroup";
            this._txtApiGroup.Size = new System.Drawing.Size(386, 20);
            this._txtApiGroup.TabIndex = 17;
            this._txtApiGroup.Text = "General";
            // 
            // flowApiDefButtons
            // 
            this.flowApiDefButtons.AutoSize = true;
            this.flowApiDefButtons.Controls.Add(this._btnApiDefNew);
            this.flowApiDefButtons.Controls.Add(this._btnApiDefSave);
            this.flowApiDefButtons.Controls.Add(this._btnApiDefDelete);
            this.flowApiDefButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowApiDefButtons.Location = new System.Drawing.Point(111, 321);
            this.flowApiDefButtons.Name = "flowApiDefButtons";
            this.flowApiDefButtons.Size = new System.Drawing.Size(386, 3);
            this.flowApiDefButtons.TabIndex = 18;
            // 
            // _btnApiDefNew
            // 
            this._btnApiDefNew.Location = new System.Drawing.Point(3, 3);
            this._btnApiDefNew.Name = "_btnApiDefNew";
            this._btnApiDefNew.Size = new System.Drawing.Size(90, 28);
            this._btnApiDefNew.TabIndex = 0;
            this._btnApiDefNew.Text = "New";
            this._btnApiDefNew.UseVisualStyleBackColor = true;
            this._btnApiDefNew.Click += new System.EventHandler(this._btnApiDefNew_Click);
            // 
            // _btnApiDefSave
            // 
            this._btnApiDefSave.Location = new System.Drawing.Point(99, 3);
            this._btnApiDefSave.Name = "_btnApiDefSave";
            this._btnApiDefSave.Size = new System.Drawing.Size(110, 28);
            this._btnApiDefSave.TabIndex = 1;
            this._btnApiDefSave.Text = "Add / Update";
            this._btnApiDefSave.UseVisualStyleBackColor = true;
            this._btnApiDefSave.Click += new System.EventHandler(this._btnApiDefSave_Click);
            // 
            // _btnApiDefDelete
            // 
            this._btnApiDefDelete.Location = new System.Drawing.Point(215, 3);
            this._btnApiDefDelete.Name = "_btnApiDefDelete";
            this._btnApiDefDelete.Size = new System.Drawing.Size(90, 28);
            this._btnApiDefDelete.TabIndex = 2;
            this._btnApiDefDelete.Text = "Delete";
            this._btnApiDefDelete.UseVisualStyleBackColor = true;
            this._btnApiDefDelete.Click += new System.EventHandler(this._btnApiDefDelete_Click);
            // 
            // flowApiPerfActions
            // 
            this.flowApiPerfActions.AutoSize = true;
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfRun);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfRunSelected);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfRunAll);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfConfig);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfExport);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfImport);
            this.flowApiPerfActions.Controls.Add(this._btnApiPerfGoRecord);
            this.flowApiPerfActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowApiPerfActions.Location = new System.Drawing.Point(3, 357);
            this.flowApiPerfActions.Name = "flowApiPerfActions";
            this.flowApiPerfActions.Size = new System.Drawing.Size(864, 74);
            this.flowApiPerfActions.TabIndex = 2;
            // 
            // _btnApiPerfRun
            // 
            this._btnApiPerfRun.Location = new System.Drawing.Point(3, 3);
            this._btnApiPerfRun.Name = "_btnApiPerfRun";
            this._btnApiPerfRun.Size = new System.Drawing.Size(160, 32);
            this._btnApiPerfRun.TabIndex = 0;
            this._btnApiPerfRun.Text = "Run NBomber";
            this._btnApiPerfRun.UseVisualStyleBackColor = true;
            this._btnApiPerfRun.Click += new System.EventHandler(this._btnApiPerfRun_Click);
            // 
            // _btnApiPerfRunSelected
            // 
            this._btnApiPerfRunSelected.Location = new System.Drawing.Point(169, 3);
            this._btnApiPerfRunSelected.Name = "_btnApiPerfRunSelected";
            this._btnApiPerfRunSelected.Size = new System.Drawing.Size(130, 32);
            this._btnApiPerfRunSelected.TabIndex = 1;
            this._btnApiPerfRunSelected.Text = "Run Selected API";
            this._btnApiPerfRunSelected.UseVisualStyleBackColor = true;
            this._btnApiPerfRunSelected.Click += new System.EventHandler(this._btnApiPerfRunSelected_Click);
            // 
            // _btnApiPerfRunAll
            // 
            this._btnApiPerfRunAll.Location = new System.Drawing.Point(305, 3);
            this._btnApiPerfRunAll.Name = "_btnApiPerfRunAll";
            this._btnApiPerfRunAll.Size = new System.Drawing.Size(110, 32);
            this._btnApiPerfRunAll.TabIndex = 2;
            this._btnApiPerfRunAll.Text = "Run All APIs";
            this._btnApiPerfRunAll.UseVisualStyleBackColor = true;
            this._btnApiPerfRunAll.Click += new System.EventHandler(this._btnApiPerfRunAll_Click);
            // 
            // _btnApiPerfConfig
            // 
            this._btnApiPerfConfig.Location = new System.Drawing.Point(421, 3);
            this._btnApiPerfConfig.Name = "_btnApiPerfConfig";
            this._btnApiPerfConfig.Size = new System.Drawing.Size(180, 32);
            this._btnApiPerfConfig.TabIndex = 3;
            this._btnApiPerfConfig.Text = "Configure Transactions";
            this._btnApiPerfConfig.UseVisualStyleBackColor = true;
            this._btnApiPerfConfig.Click += new System.EventHandler(this._btnApiPerfConfig_Click);
            // 
            // _btnApiPerfExport
            // 
            this._btnApiPerfExport.Location = new System.Drawing.Point(607, 3);
            this._btnApiPerfExport.Name = "_btnApiPerfExport";
            this._btnApiPerfExport.Size = new System.Drawing.Size(170, 32);
            this._btnApiPerfExport.TabIndex = 4;
            this._btnApiPerfExport.Text = "Export Performance Pack";
            this._btnApiPerfExport.UseVisualStyleBackColor = true;
            this._btnApiPerfExport.Click += new System.EventHandler(this._btnApiPerfExport_Click);
            // 
            // _btnApiPerfImport
            // 
            this._btnApiPerfImport.Location = new System.Drawing.Point(3, 41);
            this._btnApiPerfImport.Name = "_btnApiPerfImport";
            this._btnApiPerfImport.Size = new System.Drawing.Size(170, 32);
            this._btnApiPerfImport.TabIndex = 5;
            this._btnApiPerfImport.Text = "Import Performance Pack";
            this._btnApiPerfImport.UseVisualStyleBackColor = true;
            this._btnApiPerfImport.Click += new System.EventHandler(this._btnApiPerfImport_Click);
            // 
            // _btnApiPerfGoRecord
            // 
            this._btnApiPerfGoRecord.Location = new System.Drawing.Point(179, 41);
            this._btnApiPerfGoRecord.Name = "_btnApiPerfGoRecord";
            this._btnApiPerfGoRecord.Size = new System.Drawing.Size(220, 32);
            this._btnApiPerfGoRecord.TabIndex = 6;
            this._btnApiPerfGoRecord.Text = "Open Record/Replay Performance Grid";
            this._btnApiPerfGoRecord.UseVisualStyleBackColor = true;
            this._btnApiPerfGoRecord.Click += new System.EventHandler(this._btnApiPerfGoRecord_Click);
            // 
            // tabSettings
            // 
            this.tabSettings.AutoScroll = true;
            this.tabSettings.Controls.Add(this.layoutSettings);
            this.tabSettings.Location = new System.Drawing.Point(4, 22);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(8);
            this.tabSettings.Size = new System.Drawing.Size(890, 454);
            this.tabSettings.TabIndex = 4;
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
            this.layoutSettings.Size = new System.Drawing.Size(874, 264);
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
            this.txtDataRoot.Size = new System.Drawing.Size(708, 20);
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
            this.txtBrowserChannel.Size = new System.Drawing.Size(708, 20);
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
            // MainWorkbenchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(898, 576);
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
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWorkbenchForm_FormClosed);
            this.Load += new System.EventHandler(this.MainWorkbenchForm_Load);
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
            this.cmsObjectTree.ResumeLayout(false);
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
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSteps)).EndInit();
            this.cmsStepsGrid.ResumeLayout(false);
            this.toolStripStepCommands.ResumeLayout(false);
            this.toolStripStepCommands.PerformLayout();
            this.panelRecordPerfPreview.ResumeLayout(false);
            this.splitRecordPerfPreview.Panel1.ResumeLayout(false);
            this.splitRecordPerfPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordPerfPreview)).EndInit();
            this.splitRecordPerfPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfAnchorPreview)).EndInit();
            this.cmsPerfGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPerfRuntimePreview)).EndInit();
            this.splitRecordCanvasProps.Panel1.ResumeLayout(false);
            this.splitRecordCanvasProps.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecordCanvasProps)).EndInit();
            this.splitRecordCanvasProps.ResumeLayout(false);
            this.panelRecordCanvasPreview.ResumeLayout(false);
            this.panelRecordCanvasPreview.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.recordWebView)).EndInit();
            this.toolStripRecordCanvas.ResumeLayout(false);
            this.toolStripRecordCanvas.PerformLayout();
            this.tabApiPerformance.ResumeLayout(false);
            this.layoutApiPerf.ResumeLayout(false);
            this.layoutApiPerf.PerformLayout();
            this.splitApiPerf.Panel1.ResumeLayout(false);
            this.splitApiPerf.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitApiPerf)).EndInit();
            this.splitApiPerf.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridApiDefinitions)).EndInit();
            this.layoutApiEditor.ResumeLayout(false);
            this.layoutApiEditor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridApiHeaders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numApiExpectedStatus)).EndInit();
            this.flowApiDefButtons.ResumeLayout(false);
            this.flowApiPerfActions.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.layoutSettings.ResumeLayout(false);
            this.layoutSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).EndInit();
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
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPerfCorr;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfRecordId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPerfRecordTime;
        private System.Windows.Forms.Label lblPerfDesignAnchorSummary;
        private System.Windows.Forms.Label lblRecordHint;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn value;
        private System.Windows.Forms.CheckBox tshSyncFocusHost;
        private System.Windows.Forms.CheckBox tshWithPerfTestHost;
    }
}
