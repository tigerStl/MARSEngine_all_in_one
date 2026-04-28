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
        private System.Windows.Forms.ToolStripButton tsbTarget;
        private System.Windows.Forms.ToolStripButton tsbRecord;
        private System.Windows.Forms.ToolStripButton tsbReplay;
        private System.Windows.Forms.ToolStripSeparator tsbSep1;
        private System.Windows.Forms.ToolStripButton tsbExport;
        private System.Windows.Forms.ToolStripButton tsbImport;
        private System.Windows.Forms.ToolStripSeparator tsbSep2;
        private System.Windows.Forms.ToolStripButton tsbSave;
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
            this.lblRecordHint = new System.Windows.Forms.Label();
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.gridSteps = new System.Windows.Forms.DataGridView();
            this.menuMain.SuspendLayout();
            this.toolMain.SuspendLayout();
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
            this.tabSettings.SuspendLayout();
            this.layoutSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSteps)).BeginInit();
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
            this.menuMain.Size = new System.Drawing.Size(640, 24);
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
            this.tsbSave});
            this.toolMain.Location = new System.Drawing.Point(0, 24);
            this.toolMain.Name = "toolMain";
            this.toolMain.Size = new System.Drawing.Size(640, 25);
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
            // statusMain
            // 
            this.statusMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusMain.Location = new System.Drawing.Point(0, 458);
            this.statusMain.Name = "statusMain";
            this.statusMain.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusMain.Size = new System.Drawing.Size(640, 22);
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
            this.tabMain.Location = new System.Drawing.Point(0, 49);
            this.tabMain.Margin = new System.Windows.Forms.Padding(4);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(640, 409);
            this.tabMain.TabIndex = 3;
            // 
            // tabTarget
            // 
            this.tabTarget.Controls.Add(this.panelTargetCard);
            this.tabTarget.Location = new System.Drawing.Point(4, 25);
            this.tabTarget.Margin = new System.Windows.Forms.Padding(4);
            this.tabTarget.Name = "tabTarget";
            this.tabTarget.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.tabTarget.Size = new System.Drawing.Size(632, 380);
            this.tabTarget.TabIndex = 0;
            this.tabTarget.Text = "Target";
            this.tabTarget.UseVisualStyleBackColor = true;
            // 
            // panelTargetCard
            // 
            this.panelTargetCard.BackColor = System.Drawing.Color.White;
            this.panelTargetCard.Controls.Add(this.layoutTarget);
            this.panelTargetCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTargetCard.Location = new System.Drawing.Point(16, 12);
            this.panelTargetCard.Margin = new System.Windows.Forms.Padding(4);
            this.panelTargetCard.Name = "panelTargetCard";
            this.panelTargetCard.Size = new System.Drawing.Size(600, 356);
            this.panelTargetCard.TabIndex = 0;
            // 
            // layoutTarget
            // 
            this.layoutTarget.ColumnCount = 3;
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 188F));
            this.layoutTarget.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 188F));
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
            this.layoutTarget.Margin = new System.Windows.Forms.Padding(4);
            this.layoutTarget.Name = "layoutTarget";
            this.layoutTarget.RowCount = 8;
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutTarget.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTarget.Size = new System.Drawing.Size(600, 356);
            this.layoutTarget.TabIndex = 3;
            // 
            // lblSectionUrl
            // 
            this.lblSectionUrl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.layoutTarget.SetColumnSpan(this.lblSectionUrl, 3);
            this.lblSectionUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblSectionUrl.Location = new System.Drawing.Point(4, 0);
            this.lblSectionUrl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSectionUrl.Name = "lblSectionUrl";
            this.lblSectionUrl.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.lblSectionUrl.Size = new System.Drawing.Size(592, 22);
            this.lblSectionUrl.TabIndex = 9;
            this.lblSectionUrl.Text = "Page URL";
            this.lblSectionUrl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnStartBrowser
            // 
            this.btnStartBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartBrowser.Location = new System.Drawing.Point(226, 24);
            this.btnStartBrowser.Margin = new System.Windows.Forms.Padding(2);
            this.btnStartBrowser.Name = "btnStartBrowser";
            this.btnStartBrowser.Size = new System.Drawing.Size(184, 28);
            this.btnStartBrowser.TabIndex = 8;
            this.btnStartBrowser.Text = "Start browser";
            this.btnStartBrowser.UseVisualStyleBackColor = true;
            // 
            // txtUrl
            // 
            this.txtUrl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUrl.Location = new System.Drawing.Point(8, 27);
            this.txtUrl.Margin = new System.Windows.Forms.Padding(8, 5, 5, 5);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(211, 22);
            this.txtUrl.TabIndex = 0;
            // 
            // btnNavigate
            // 
            this.btnNavigate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNavigate.Location = new System.Drawing.Point(414, 24);
            this.btnNavigate.Margin = new System.Windows.Forms.Padding(2);
            this.btnNavigate.Name = "btnNavigate";
            this.btnNavigate.Size = new System.Drawing.Size(184, 28);
            this.btnNavigate.TabIndex = 2;
            this.btnNavigate.Text = "Navigate";
            this.btnNavigate.UseVisualStyleBackColor = true;
            // 
            // lblScheme
            // 
            this.lblScheme.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblScheme, 3);
            this.lblScheme.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblScheme.Location = new System.Drawing.Point(2, 56);
            this.lblScheme.Margin = new System.Windows.Forms.Padding(2);
            this.lblScheme.Name = "lblScheme";
            this.lblScheme.Size = new System.Drawing.Size(596, 24);
            this.lblScheme.TabIndex = 3;
            this.lblScheme.Text = "Scheme:";
            this.lblScheme.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblHost, 3);
            this.lblHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHost.Location = new System.Drawing.Point(2, 84);
            this.lblHost.Margin = new System.Windows.Forms.Padding(2);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(596, 24);
            this.lblHost.TabIndex = 4;
            this.lblHost.Text = "Host:";
            this.lblHost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblPort, 3);
            this.lblPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPort.Location = new System.Drawing.Point(2, 112);
            this.lblPort.Margin = new System.Windows.Forms.Padding(2);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(596, 24);
            this.lblPort.TabIndex = 5;
            this.lblPort.Text = "Port:";
            this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblPath, 3);
            this.lblPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPath.Location = new System.Drawing.Point(2, 140);
            this.lblPath.Margin = new System.Windows.Forms.Padding(2);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(596, 24);
            this.lblPath.TabIndex = 6;
            this.lblPath.Text = "Path:";
            this.lblPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuery
            // 
            this.lblQuery.AutoSize = true;
            this.layoutTarget.SetColumnSpan(this.lblQuery, 3);
            this.lblQuery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuery.Location = new System.Drawing.Point(2, 168);
            this.lblQuery.Margin = new System.Windows.Forms.Padding(2);
            this.lblQuery.Name = "lblQuery";
            this.lblQuery.Size = new System.Drawing.Size(596, 24);
            this.lblQuery.TabIndex = 7;
            this.lblQuery.Text = "Query:";
            this.lblQuery.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabObjects
            // 
            this.tabObjects.Controls.Add(this.splitObjects);
            this.tabObjects.Controls.Add(this.panelObjectsToolbar);
            this.tabObjects.Location = new System.Drawing.Point(4, 25);
            this.tabObjects.Margin = new System.Windows.Forms.Padding(4);
            this.tabObjects.Name = "tabObjects";
            this.tabObjects.Padding = new System.Windows.Forms.Padding(5);
            this.tabObjects.Size = new System.Drawing.Size(632, 380);
            this.tabObjects.TabIndex = 1;
            this.tabObjects.Text = "Objects";
            this.tabObjects.UseVisualStyleBackColor = true;
            // 
            // splitObjects
            // 
            this.splitObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitObjects.Location = new System.Drawing.Point(5, 45);
            this.splitObjects.Margin = new System.Windows.Forms.Padding(4);
            this.splitObjects.Name = "splitObjects";
            // 
            // splitObjects.Panel1
            // 
            this.splitObjects.Panel1.Controls.Add(this.treeObjects);
            // 
            // splitObjects.Panel2
            // 
            this.splitObjects.Panel2.Controls.Add(this.gridObjectProps);
            this.splitObjects.Size = new System.Drawing.Size(622, 330);
            this.splitObjects.SplitterDistance = 260;
            this.splitObjects.SplitterWidth = 5;
            this.splitObjects.TabIndex = 1;
            // 
            // treeObjects
            // 
            this.treeObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeObjects.Location = new System.Drawing.Point(0, 0);
            this.treeObjects.Margin = new System.Windows.Forms.Padding(4);
            this.treeObjects.Name = "treeObjects";
            this.treeObjects.Size = new System.Drawing.Size(260, 330);
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
            this.gridObjectProps.Margin = new System.Windows.Forms.Padding(4);
            this.gridObjectProps.Name = "gridObjectProps";
            this.gridObjectProps.ReadOnly = true;
            this.gridObjectProps.RowHeadersVisible = false;
            this.gridObjectProps.RowHeadersWidth = 51;
            this.gridObjectProps.Size = new System.Drawing.Size(357, 330);
            this.gridObjectProps.TabIndex = 0;
            // 
            // panelObjectsToolbar
            // 
            this.panelObjectsToolbar.Controls.Add(this.flowObjectsToolbar);
            this.panelObjectsToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelObjectsToolbar.Location = new System.Drawing.Point(5, 5);
            this.panelObjectsToolbar.Margin = new System.Windows.Forms.Padding(4);
            this.panelObjectsToolbar.Name = "panelObjectsToolbar";
            this.panelObjectsToolbar.Size = new System.Drawing.Size(622, 40);
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
            this.flowObjectsToolbar.Margin = new System.Windows.Forms.Padding(4);
            this.flowObjectsToolbar.Name = "flowObjectsToolbar";
            this.flowObjectsToolbar.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.flowObjectsToolbar.Size = new System.Drawing.Size(622, 40);
            this.flowObjectsToolbar.TabIndex = 0;
            this.flowObjectsToolbar.WrapContents = false;
            // 
            // txtTreeSearch
            // 
            this.txtTreeSearch.Location = new System.Drawing.Point(4, 8);
            this.txtTreeSearch.Margin = new System.Windows.Forms.Padding(4, 4, 6, 4);
            this.txtTreeSearch.Name = "txtTreeSearch";
            this.txtTreeSearch.Size = new System.Drawing.Size(220, 22);
            this.txtTreeSearch.TabIndex = 0;
            // 
            // btnTreeSearchGo
            // 
            this.btnTreeSearchGo.Location = new System.Drawing.Point(234, 6);
            this.btnTreeSearchGo.Margin = new System.Windows.Forms.Padding(4, 2, 2, 2);
            this.btnTreeSearchGo.Name = "btnTreeSearchGo";
            this.btnTreeSearchGo.Size = new System.Drawing.Size(32, 26);
            this.btnTreeSearchGo.TabIndex = 1;
            this.btnTreeSearchGo.Text = "▶";
            this.btnTreeSearchGo.UseVisualStyleBackColor = true;
            this.btnTreeSearchGo.Click += new System.EventHandler(this.btnTreeSearchGo_Click);
            // 
            // btnTreeSearchPrev
            // 
            this.btnTreeSearchPrev.Location = new System.Drawing.Point(270, 6);
            this.btnTreeSearchPrev.Margin = new System.Windows.Forms.Padding(2);
            this.btnTreeSearchPrev.Name = "btnTreeSearchPrev";
            this.btnTreeSearchPrev.Size = new System.Drawing.Size(32, 26);
            this.btnTreeSearchPrev.TabIndex = 2;
            this.btnTreeSearchPrev.Text = "＜";
            this.btnTreeSearchPrev.UseVisualStyleBackColor = true;
            this.btnTreeSearchPrev.Click += new System.EventHandler(this.btnTreeSearchPrev_Click);
            // 
            // btnTreeSearchNext
            // 
            this.btnTreeSearchNext.Location = new System.Drawing.Point(306, 6);
            this.btnTreeSearchNext.Margin = new System.Windows.Forms.Padding(2);
            this.btnTreeSearchNext.Name = "btnTreeSearchNext";
            this.btnTreeSearchNext.Size = new System.Drawing.Size(32, 26);
            this.btnTreeSearchNext.TabIndex = 3;
            this.btnTreeSearchNext.Text = "＞";
            this.btnTreeSearchNext.UseVisualStyleBackColor = true;
            this.btnTreeSearchNext.Click += new System.EventHandler(this.btnTreeSearchNext_Click);
            // 
            // chkTreeRegex
            // 
            this.chkTreeRegex.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkTreeRegex.Location = new System.Drawing.Point(344, 6);
            this.chkTreeRegex.Margin = new System.Windows.Forms.Padding(4, 2, 6, 2);
            this.chkTreeRegex.Name = "chkTreeRegex";
            this.chkTreeRegex.Size = new System.Drawing.Size(36, 26);
            this.chkTreeRegex.TabIndex = 4;
            this.chkTreeRegex.Text = "*";
            this.chkTreeRegex.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkTreeRegex.UseVisualStyleBackColor = true;
            // 
            // btnRefreshTree
            // 
            this.btnRefreshTree.Location = new System.Drawing.Point(390, 6);
            this.btnRefreshTree.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.btnRefreshTree.Name = "btnRefreshTree";
            this.btnRefreshTree.Size = new System.Drawing.Size(100, 26);
            this.btnRefreshTree.TabIndex = 5;
            this.btnRefreshTree.Text = "Refresh tree";
            this.btnRefreshTree.UseVisualStyleBackColor = true;
            this.btnRefreshTree.Click += new System.EventHandler(this.btnRefreshTree_Click);
            // 
            // tabRecord
            // 
            this.tabRecord.Controls.Add(this.panel1);
            this.tabRecord.Controls.Add(this.lblRecordHint);
            this.tabRecord.Location = new System.Drawing.Point(4, 25);
            this.tabRecord.Margin = new System.Windows.Forms.Padding(4);
            this.tabRecord.Name = "tabRecord";
            this.tabRecord.Padding = new System.Windows.Forms.Padding(5);
            this.tabRecord.Size = new System.Drawing.Size(632, 380);
            this.tabRecord.TabIndex = 2;
            this.tabRecord.Text = "Record / Replay";
            this.tabRecord.UseVisualStyleBackColor = true;
            // 
            // lblRecordHint
            // 
            this.lblRecordHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecordHint.Location = new System.Drawing.Point(5, 5);
            this.lblRecordHint.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRecordHint.Name = "lblRecordHint";
            this.lblRecordHint.Padding = new System.Windows.Forms.Padding(13, 10, 13, 10);
            this.lblRecordHint.Size = new System.Drawing.Size(622, 41);
            this.lblRecordHint.TabIndex = 2;
            this.lblRecordHint.Text = "Toolbar: Record toggles capture; Replay runs the grid below. Steps use semantic k" +
    "eywords.";
            this.lblRecordHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.layoutSettings);
            this.tabSettings.Location = new System.Drawing.Point(4, 25);
            this.tabSettings.Margin = new System.Windows.Forms.Padding(4);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.tabSettings.Size = new System.Drawing.Size(632, 380);
            this.tabSettings.TabIndex = 3;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // layoutSettings
            // 
            this.layoutSettings.ColumnCount = 2;
            this.layoutSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 213F));
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
            this.layoutSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutSettings.Location = new System.Drawing.Point(11, 10);
            this.layoutSettings.Margin = new System.Windows.Forms.Padding(4);
            this.layoutSettings.Name = "layoutSettings";
            this.layoutSettings.RowCount = 8;
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.layoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.layoutSettings.Size = new System.Drawing.Size(610, 360);
            this.layoutSettings.TabIndex = 0;
            // 
            // lblDataRoot
            // 
            this.lblDataRoot.AutoSize = true;
            this.lblDataRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDataRoot.Location = new System.Drawing.Point(4, 0);
            this.lblDataRoot.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDataRoot.Name = "lblDataRoot";
            this.lblDataRoot.Size = new System.Drawing.Size(205, 39);
            this.lblDataRoot.TabIndex = 0;
            this.lblDataRoot.Text = "Data root folder";
            this.lblDataRoot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDataRoot
            // 
            this.txtDataRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDataRoot.Location = new System.Drawing.Point(217, 4);
            this.txtDataRoot.Margin = new System.Windows.Forms.Padding(4);
            this.txtDataRoot.Name = "txtDataRoot";
            this.txtDataRoot.Size = new System.Drawing.Size(389, 22);
            this.txtDataRoot.TabIndex = 1;
            // 
            // chkHeadless
            // 
            this.chkHeadless.AutoSize = true;
            this.chkHeadless.Location = new System.Drawing.Point(217, 43);
            this.chkHeadless.Margin = new System.Windows.Forms.Padding(4);
            this.chkHeadless.Name = "chkHeadless";
            this.chkHeadless.Size = new System.Drawing.Size(123, 20);
            this.chkHeadless.TabIndex = 2;
            this.chkHeadless.Text = "Headless mode";
            this.chkHeadless.UseVisualStyleBackColor = true;
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeout.Location = new System.Drawing.Point(4, 78);
            this.lblTimeout.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(205, 39);
            this.lblTimeout.TabIndex = 3;
            this.lblTimeout.Text = "Default timeout (ms)";
            this.lblTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numTimeout
            // 
            this.numTimeout.Dock = System.Windows.Forms.DockStyle.Left;
            this.numTimeout.Location = new System.Drawing.Point(217, 82);
            this.numTimeout.Margin = new System.Windows.Forms.Padding(4);
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
            this.numTimeout.Size = new System.Drawing.Size(160, 22);
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
            this.chkPersistHeaders.Location = new System.Drawing.Point(217, 121);
            this.chkPersistHeaders.Margin = new System.Windows.Forms.Padding(4);
            this.chkPersistHeaders.Name = "chkPersistHeaders";
            this.chkPersistHeaders.Size = new System.Drawing.Size(312, 20);
            this.chkPersistHeaders.TabIndex = 5;
            this.chkPersistHeaders.Text = "Persist sensitive headers/cookies in network log";
            this.chkPersistHeaders.UseVisualStyleBackColor = true;
            // 
            // lblChannel
            // 
            this.lblChannel.AutoSize = true;
            this.lblChannel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChannel.Location = new System.Drawing.Point(4, 156);
            this.lblChannel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblChannel.Name = "lblChannel";
            this.lblChannel.Size = new System.Drawing.Size(205, 39);
            this.lblChannel.TabIndex = 6;
            this.lblChannel.Text = "Browser channel (opt.)";
            this.lblChannel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBrowserChannel
            // 
            this.txtBrowserChannel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBrowserChannel.Location = new System.Drawing.Point(217, 160);
            this.txtBrowserChannel.Margin = new System.Windows.Forms.Padding(4);
            this.txtBrowserChannel.Name = "txtBrowserChannel";
            this.txtBrowserChannel.Size = new System.Drawing.Size(389, 22);
            this.txtBrowserChannel.TabIndex = 7;
            // 
            // lblViewport
            // 
            this.lblViewport.AutoSize = true;
            this.lblViewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblViewport.Location = new System.Drawing.Point(4, 195);
            this.lblViewport.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblViewport.Name = "lblViewport";
            this.lblViewport.Size = new System.Drawing.Size(205, 39);
            this.lblViewport.TabIndex = 8;
            this.lblViewport.Text = "Viewport W / H";
            this.lblViewport.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numViewportW
            // 
            this.numViewportW.Dock = System.Windows.Forms.DockStyle.Left;
            this.numViewportW.Location = new System.Drawing.Point(217, 199);
            this.numViewportW.Margin = new System.Windows.Forms.Padding(4);
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
            this.numViewportW.Size = new System.Drawing.Size(107, 22);
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
            this.numViewportH.Location = new System.Drawing.Point(217, 238);
            this.numViewportH.Margin = new System.Windows.Forms.Padding(4);
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
            this.numViewportH.Size = new System.Drawing.Size(107, 22);
            this.numViewportH.TabIndex = 10;
            this.numViewportH.Value = new decimal(new int[] {
            720,
            0,
            0,
            0});
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(217, 277);
            this.btnSaveSettings.Margin = new System.Windows.Forms.Padding(4);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(160, 34);
            this.btnSaveSettings.TabIndex = 11;
            this.btnSaveSettings.Text = "Save settings";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gridSteps);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(5, 46);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(622, 329);
            this.panel1.TabIndex = 3;
            // 
            // gridSteps
            // 
            this.gridSteps.AllowUserToAddRows = false;
            this.gridSteps.AllowUserToDeleteRows = false;
            this.gridSteps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridSteps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSteps.Location = new System.Drawing.Point(0, 0);
            this.gridSteps.Margin = new System.Windows.Forms.Padding(4);
            this.gridSteps.Name = "gridSteps";
            this.gridSteps.ReadOnly = true;
            this.gridSteps.RowHeadersVisible = false;
            this.gridSteps.RowHeadersWidth = 51;
            this.gridSteps.Size = new System.Drawing.Size(622, 329);
            this.gridSteps.TabIndex = 1;
            // 
            // MainWorkbenchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 480);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.statusMain);
            this.Controls.Add(this.toolMain);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(640, 480);
            this.Name = "MainWorkbenchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MARS Web Automation";
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.toolMain.ResumeLayout(false);
            this.toolMain.PerformLayout();
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
            this.tabSettings.ResumeLayout(false);
            this.layoutSettings.ResumeLayout(false);
            this.layoutSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numViewportH)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSteps)).EndInit();
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
        private System.Windows.Forms.Label lblRecordHint;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView gridSteps;
    }
}
