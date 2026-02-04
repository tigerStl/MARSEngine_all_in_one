
namespace Mars.AutoTestingDriver.ExecuteStoryboard
{
    partial class TestStepsNavigator
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.restartButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.testStepGrid = new System.Windows.Forms.DataGridView();
            this.runOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.keywordColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.happyNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.parameterColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsSkipColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.objectDetailColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.testResultColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errorLbl = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.testStepGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblHint);
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Controls.Add(this.restartButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 412);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 38);
            this.panel1.TabIndex = 4;
            // 
            // lblHint
            // 
            this.lblHint.AutoSize = true;
            this.lblHint.Location = new System.Drawing.Point(13, 13);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(16, 13);
            this.lblHint.TabIndex = 7;
            this.lblHint.Text = "   ";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(700, 7);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(85, 24);
            this.closeButton.TabIndex = 6;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // restartButton
            // 
            this.restartButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.restartButton.Location = new System.Drawing.Point(527, 7);
            this.restartButton.Name = "restartButton";
            this.restartButton.Size = new System.Drawing.Size(161, 24);
            this.restartButton.TabIndex = 4;
            this.restartButton.Text = "Restart From Selected Row";
            this.restartButton.UseVisualStyleBackColor = true;
            this.restartButton.Click += new System.EventHandler(this.restartButton_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.testStepGrid);
            this.panel2.Controls.Add(this.errorLbl);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(800, 412);
            this.panel2.TabIndex = 5;
            // 
            // testStepGrid
            // 
            this.testStepGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.testStepGrid.BackgroundColor = System.Drawing.SystemColors.Control;
            this.testStepGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.testStepGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.runOrder,
            this.keywordColumn,
            this.happyNameColumn,
            this.parameterColumn,
            this.IsSkipColumn,
            this.objectDetailColumn,
            this.dataColumn,
            this.statusColumn,
            this.testResultColumn});
            this.testStepGrid.Location = new System.Drawing.Point(13, 24);
            this.testStepGrid.Name = "testStepGrid";
            this.testStepGrid.Size = new System.Drawing.Size(775, 382);
            this.testStepGrid.TabIndex = 6;
            this.testStepGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.testStepGrid_CellDoubleClick);
            this.testStepGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.testStepGrid_CellEndEdit);
            // 
            // runOrder
            // 
            this.runOrder.HeaderText = "Run Ord.";
            this.runOrder.Name = "runOrder";
            this.runOrder.Width = 80;
            // 
            // keywordColumn
            // 
            this.keywordColumn.HeaderText = "Keyword";
            this.keywordColumn.Name = "keywordColumn";
            this.keywordColumn.Width = 120;
            // 
            // happyNameColumn
            // 
            this.happyNameColumn.HeaderText = "Object Name";
            this.happyNameColumn.Name = "happyNameColumn";
            this.happyNameColumn.Width = 180;
            // 
            // parameterColumn
            // 
            this.parameterColumn.HeaderText = "Parameter";
            this.parameterColumn.Name = "parameterColumn";
            this.parameterColumn.Width = 150;
            // 
            // IsSkipColumn
            // 
            this.IsSkipColumn.HeaderText = "Is Skip";
            this.IsSkipColumn.Name = "IsSkipColumn";
            this.IsSkipColumn.Width = 60;
            // 
            // objectDetailColumn
            // 
            this.objectDetailColumn.HeaderText = "Object Details";
            this.objectDetailColumn.Name = "objectDetailColumn";
            this.objectDetailColumn.Width = 200;
            // 
            // dataColumn
            // 
            this.dataColumn.HeaderText = "Data";
            this.dataColumn.Name = "dataColumn";
            this.dataColumn.Width = 120;
            // 
            // statusColumn
            // 
            this.statusColumn.HeaderText = "Status";
            this.statusColumn.Name = "statusColumn";
            // 
            // testResultColumn
            // 
            this.testResultColumn.HeaderText = "Test Result";
            this.testResultColumn.Name = "testResultColumn";
            // 
            // errorLbl
            // 
            this.errorLbl.AutoEllipsis = true;
            this.errorLbl.AutoSize = true;
            this.errorLbl.ForeColor = System.Drawing.SystemColors.Highlight;
            this.errorLbl.Location = new System.Drawing.Point(150, 7);
            this.errorLbl.Name = "errorLbl";
            this.errorLbl.Size = new System.Drawing.Size(78, 13);
            this.errorLbl.TabIndex = 5;
            this.errorLbl.Text = "Error Message:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "Test steps status:";
            // 
            // TestStepsNavigator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TestStepsNavigator";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MARS Running Test Steps Management";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.TestStepsNavigator_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.testStepGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button restartButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label errorLbl;
        private System.Windows.Forms.DataGridView testStepGrid;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.DataGridViewTextBoxColumn runOrder;
        private System.Windows.Forms.DataGridViewTextBoxColumn keywordColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn happyNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn parameterColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsSkipColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn objectDetailColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn statusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn testResultColumn;
    }
}