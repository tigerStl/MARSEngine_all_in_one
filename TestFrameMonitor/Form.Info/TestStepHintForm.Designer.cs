using TestFrameMonitor.Form.Info;

namespace QtpStarter.Info
{
    partial class TestStepHintForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestStepHintForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.BreakPointToolButton = new System.Windows.Forms.ToolStripButton();
            this.SkipToolButton = new System.Windows.Forms.ToolStripButton();
            this.RunFromButton = new System.Windows.Forms.ToolStripButton();
            this.PlayBackButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.resumeButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStartPlatform = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.troopModeLabel = new System.Windows.Forms.ToolStripLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.StatusGrid = new System.Windows.Forms.DataGridView();
            this.B = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Current_Running = new TestFrameMonitor.Form.Info.StepDetailCellColumn();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel2 = new System.Windows.Forms.Panel();
            this.TSTCGrid = new System.Windows.Forms.DataGridView();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.abcToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepDetailCellColumn1 = new TestFrameMonitor.Form.Info.StepDetailCellColumn();
            this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
            this.toolStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StatusGrid)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TSTCGrid)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.CanOverflow = false;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BreakPointToolButton,
            this.SkipToolButton,
            this.RunFromButton,
            this.PlayBackButton,
            this.toolStripSeparator1,
            this.resumeButton,
            this.toolStripSeparator3,
            this.toolStartPlatform,
            this.toolStripSeparator2,
            this.troopModeLabel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(324, 31);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.Visible = false;
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // BreakPointToolButton
            // 
            this.BreakPointToolButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.BreakPointToolButton.CheckOnClick = true;
            this.BreakPointToolButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.BreakPointToolButton.Image = ((System.Drawing.Image)(resources.GetObject("BreakPointToolButton.Image")));
            this.BreakPointToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BreakPointToolButton.Name = "BreakPointToolButton";
            this.BreakPointToolButton.Size = new System.Drawing.Size(28, 28);
            this.BreakPointToolButton.Tag = "Breakpoints";
            this.BreakPointToolButton.Text = "Breakpoints";
            this.BreakPointToolButton.ToolTipText = "Stop at checked Row(s)";
            this.BreakPointToolButton.Click += new System.EventHandler(this.BreakPointToolButton_Click);
            // 
            // SkipToolButton
            // 
            this.SkipToolButton.CheckOnClick = true;
            this.SkipToolButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SkipToolButton.Image = ((System.Drawing.Image)(resources.GetObject("SkipToolButton.Image")));
            this.SkipToolButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SkipToolButton.Name = "SkipToolButton";
            this.SkipToolButton.Size = new System.Drawing.Size(28, 28);
            this.SkipToolButton.Tag = "skip Steps";
            this.SkipToolButton.Text = "skip Steps";
            this.SkipToolButton.ToolTipText = "Skip Checked Row(s)";
            this.SkipToolButton.Click += new System.EventHandler(this.BreakPointToolButton_Click);
            // 
            // RunFromButton
            // 
            this.RunFromButton.CheckOnClick = true;
            this.RunFromButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RunFromButton.Image = ((System.Drawing.Image)(resources.GetObject("RunFromButton.Image")));
            this.RunFromButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RunFromButton.Name = "RunFromButton";
            this.RunFromButton.Size = new System.Drawing.Size(28, 28);
            this.RunFromButton.Tag = "Run From";
            this.RunFromButton.Text = "Run From";
            this.RunFromButton.ToolTipText = "Run from The Checked Row";
            this.RunFromButton.Visible = false;
            this.RunFromButton.Click += new System.EventHandler(this.BreakPointToolButton_Click);
            // 
            // PlayBackButton
            // 
            this.PlayBackButton.CheckOnClick = true;
            this.PlayBackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.PlayBackButton.Image = ((System.Drawing.Image)(resources.GetObject("PlayBackButton.Image")));
            this.PlayBackButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PlayBackButton.Name = "PlayBackButton";
            this.PlayBackButton.Size = new System.Drawing.Size(28, 28);
            this.PlayBackButton.Tag = "Play-Back";
            this.PlayBackButton.Text = "Play-Back";
            this.PlayBackButton.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.PlayBackButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
            this.PlayBackButton.Click += new System.EventHandler(this.BreakPointToolButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // resumeButton
            // 
            this.resumeButton.CheckOnClick = true;
            this.resumeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.resumeButton.Image = ((System.Drawing.Image)(resources.GetObject("resumeButton.Image")));
            this.resumeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.resumeButton.Name = "resumeButton";
            this.resumeButton.Size = new System.Drawing.Size(28, 28);
            this.resumeButton.Tag = "Resume";
            this.resumeButton.Text = "Resume to run";
            this.resumeButton.ToolTipText = "Resume to run";
            this.resumeButton.Click += new System.EventHandler(this.BreakPointToolButton_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 31);
            // 
            // toolStartPlatform
            // 
            this.toolStartPlatform.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStartPlatform.Image = ((System.Drawing.Image)(resources.GetObject("toolStartPlatform.Image")));
            this.toolStartPlatform.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStartPlatform.Name = "toolStartPlatform";
            this.toolStartPlatform.Size = new System.Drawing.Size(28, 28);
            this.toolStartPlatform.Text = "Video Platform";
            this.toolStartPlatform.Visible = false;
            this.toolStartPlatform.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 31);
            this.toolStripSeparator2.Visible = false;
            // 
            // troopModeLabel
            // 
            this.troopModeLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.troopModeLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.troopModeLabel.Name = "troopModeLabel";
            this.troopModeLabel.Size = new System.Drawing.Size(88, 28);
            this.troopModeLabel.Text = "Current Mode:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(324, 584);
            this.panel1.TabIndex = 3;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.StatusGrid);
            this.panel3.Controls.Add(this.listView1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 104);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(324, 480);
            this.panel3.TabIndex = 5;
            // 
            // StatusGrid
            // 
            this.StatusGrid.AllowUserToAddRows = false;
            this.StatusGrid.AllowUserToDeleteRows = false;
            this.StatusGrid.AllowUserToResizeColumns = false;
            this.StatusGrid.AllowUserToResizeRows = false;
            this.StatusGrid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.StatusGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            this.StatusGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.StatusGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.B,
            this.Current_Running});
            this.StatusGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusGrid.Location = new System.Drawing.Point(0, 0);
            this.StatusGrid.Margin = new System.Windows.Forms.Padding(1);
            this.StatusGrid.MinimumSize = new System.Drawing.Size(310, 0);
            this.StatusGrid.MultiSelect = false;
            this.StatusGrid.Name = "StatusGrid";
            this.StatusGrid.RowHeadersVisible = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.StatusGrid.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.StatusGrid.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
            this.StatusGrid.RowTemplate.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.StatusGrid.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.StatusGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.StatusGrid.Size = new System.Drawing.Size(324, 377);
            this.StatusGrid.TabIndex = 6;
            this.StatusGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.StatusGrid_CellClick);
            this.StatusGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.StatusGrid_CellContentClick_1);
            this.StatusGrid.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.StatusGrid_CellContentDoubleClick);
            this.StatusGrid.MouseClick += new System.Windows.Forms.MouseEventHandler(this.StatusGrid_MouseClick);
            // 
            // B
            // 
            this.B.FalseValue = "false";
            this.B.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.B.HeaderText = "B";
            this.B.Name = "B";
            this.B.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.B.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.B.TrueValue = "true";
            this.B.Width = 32;
            // 
            // Current_Running
            // 
            this.Current_Running.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Current_Running.FillWeight = 161.2121F;
            this.Current_Running.HeaderText = "Current Running";
            this.Current_Running.Name = "Current_Running";
            // 
            // listView1
            // 
            this.listView1.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.HotTracking = true;
            this.listView1.HoverSelection = true;
            this.listView1.Location = new System.Drawing.Point(0, 377);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.ShowItemToolTips = true;
            this.listView1.Size = new System.Drawing.Size(324, 103);
            this.listView1.TabIndex = 4;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged_1);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Current Running Log...";
            this.columnHeader1.Width = 319;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.TSTCGrid);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(324, 104);
            this.panel2.TabIndex = 4;
            // 
            // TSTCGrid
            // 
            this.TSTCGrid.AllowUserToAddRows = false;
            this.TSTCGrid.AllowUserToDeleteRows = false;
            this.TSTCGrid.AllowUserToResizeColumns = false;
            this.TSTCGrid.AllowUserToResizeRows = false;
            this.TSTCGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.TSTCGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column2,
            this.Column1});
            this.TSTCGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TSTCGrid.Location = new System.Drawing.Point(0, 0);
            this.TSTCGrid.Name = "TSTCGrid";
            this.TSTCGrid.RowHeadersVisible = false;
            this.TSTCGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.TSTCGrid.Size = new System.Drawing.Size(324, 104);
            this.TSTCGrid.TabIndex = 5;
            this.TSTCGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.TSTCGrid_CellContentClick);
            // 
            // Column2
            // 
            this.Column2.HeaderText = "";
            this.Column2.Name = "Column2";
            this.Column2.Width = 48;
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column1.HeaderText = "Current TestSuite/TestCase ";
            this.Column1.Name = "Column1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.abcToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(94, 26);
            // 
            // abcToolStripMenuItem
            // 
            this.abcToolStripMenuItem.Name = "abcToolStripMenuItem";
            this.abcToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.abcToolStripMenuItem.Text = "abc";
            // 
            // stepDetailCellColumn1
            // 
            this.stepDetailCellColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.stepDetailCellColumn1.FillWeight = 161.2121F;
            this.stepDetailCellColumn1.HeaderText = "Current Running";
            this.stepDetailCellColumn1.Name = "stepDetailCellColumn1";
            // 
            // dataGridViewImageColumn1
            // 
            this.dataGridViewImageColumn1.Description = "Vedio";
            this.dataGridViewImageColumn1.FillWeight = 38.78788F;
            this.dataGridViewImageColumn1.HeaderText = "V";
            this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dataGridViewImageColumn1.Width = 32;
            // 
            // TestStepHintForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 584);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(340, 480);
            this.Name = "TestStepHintForm";
            this.Text = "Test Step Viewer";
            this.TopMost = true;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TestStepHintForm_FormClosed);
            this.Load += new System.EventHandler(this.TestStepHintForm_Load);
            this.Shown += new System.EventHandler(this.TestStepHintForm_Shown);
            this.ResizeEnd += new System.EventHandler(this.TestStepHintForm_ResizeEnd);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.StatusGrid)).EndInit();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TSTCGrid)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

#endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton BreakPointToolButton;
        private System.Windows.Forms.ToolStripButton SkipToolButton;
        private System.Windows.Forms.ToolStripButton RunFromButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView TSTCGrid;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.DataGridView StatusGrid;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton PlayBackButton;
        private System.Windows.Forms.ToolStripButton resumeButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel troopModeLabel;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem abcToolStripMenuItem;

        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStartPlatform;

        private StepDetailCellColumn stepDetailCellColumn1;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn B;
        private StepDetailCellColumn Current_Running;
    }
}