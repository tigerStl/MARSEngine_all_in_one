namespace MarsSpyTool.subToolWindows.replayTestStep
{
    partial class ReplyTestStepInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReplyTestStepInfoForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.UpdateAndRetryButton = new System.Windows.Forms.Button();
            this.StopReplayButtonClick = new System.Windows.Forms.Button();
            this.IgnoreButtonClick = new System.Windows.Forms.Button();
            this.RetryButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.testStepCatalog = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.teststepCatalogValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IdentifierSet = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.statusLabel);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.UpdateAndRetryButton);
            this.panel1.Controls.Add(this.StopReplayButtonClick);
            this.panel1.Controls.Add(this.IgnoreButtonClick);
            this.panel1.Controls.Add(this.RetryButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(263, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(101, 368);
            this.panel1.TabIndex = 1;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabel.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.statusLabel.Location = new System.Drawing.Point(10, 141);
            this.statusLabel.MaximumSize = new System.Drawing.Size(83, 290);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 15);
            this.statusLabel.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Message:";
            // 
            // UpdateAndRetryButton
            // 
            this.UpdateAndRetryButton.Location = new System.Drawing.Point(6, 5);
            this.UpdateAndRetryButton.Name = "UpdateAndRetryButton";
            this.UpdateAndRetryButton.Size = new System.Drawing.Size(87, 23);
            this.UpdateAndRetryButton.TabIndex = 3;
            this.UpdateAndRetryButton.Text = "Update&&Retry";
            this.UpdateAndRetryButton.UseVisualStyleBackColor = true;
            this.UpdateAndRetryButton.Click += new System.EventHandler(this.UpdateAndRetryButton_Click);
            // 
            // StopReplayButtonClick
            // 
            this.StopReplayButtonClick.Location = new System.Drawing.Point(6, 94);
            this.StopReplayButtonClick.Name = "StopReplayButtonClick";
            this.StopReplayButtonClick.Size = new System.Drawing.Size(87, 23);
            this.StopReplayButtonClick.TabIndex = 2;
            this.StopReplayButtonClick.Text = "Stop Replay";
            this.StopReplayButtonClick.UseVisualStyleBackColor = true;
            this.StopReplayButtonClick.Click += new System.EventHandler(this.StopReplayButtonClick_Click);
            // 
            // IgnoreButtonClick
            // 
            this.IgnoreButtonClick.Location = new System.Drawing.Point(6, 64);
            this.IgnoreButtonClick.Name = "IgnoreButtonClick";
            this.IgnoreButtonClick.Size = new System.Drawing.Size(87, 23);
            this.IgnoreButtonClick.TabIndex = 1;
            this.IgnoreButtonClick.Text = "Ignore";
            this.IgnoreButtonClick.UseVisualStyleBackColor = true;
            this.IgnoreButtonClick.Click += new System.EventHandler(this.IgnoreButtonClick_Click);
            // 
            // RetryButton
            // 
            this.RetryButton.Location = new System.Drawing.Point(6, 34);
            this.RetryButton.Name = "RetryButton";
            this.RetryButton.Size = new System.Drawing.Size(87, 23);
            this.RetryButton.TabIndex = 0;
            this.RetryButton.Text = "Retry";
            this.RetryButton.UseVisualStyleBackColor = true;
            this.RetryButton.Click += new System.EventHandler(this.RetryButton_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dataGridView1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(263, 368);
            this.panel2.TabIndex = 2;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.testStepCatalog,
            this.teststepCatalogValue,
            this.IdentifierSet});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 20;
            this.dataGridView1.Size = new System.Drawing.Size(263, 368);
            this.dataGridView1.TabIndex = 1;
            // 
            // testStepCatalog
            // 
            this.testStepCatalog.Frozen = true;
            this.testStepCatalog.HeaderText = "Type";
            this.testStepCatalog.Name = "testStepCatalog";
            this.testStepCatalog.Width = 80;
            // 
            // teststepCatalogValue
            // 
            this.teststepCatalogValue.HeaderText = "Value";
            this.teststepCatalogValue.Name = "teststepCatalogValue";
            this.teststepCatalogValue.Width = 180;
            // 
            // IdentifierSet
            // 
            this.IdentifierSet.HeaderText = "Identifier Set";
            this.IdentifierSet.Name = "IdentifierSet";
            this.IdentifierSet.Width = 40;
            // 
            // ReplyTestStepInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 368);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReplyTestStepInfoForm";
            this.ShowInTaskbar = false;
            this.Text = "Test Step Info Form";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ReplyTestStepInfoForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button StopReplayButtonClick;
        private System.Windows.Forms.Button IgnoreButtonClick;
        private System.Windows.Forms.Button RetryButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button UpdateAndRetryButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn testStepCatalog;
        private System.Windows.Forms.DataGridViewTextBoxColumn teststepCatalogValue;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IdentifierSet;
    }
}