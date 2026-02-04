namespace MarsErrorMessageBox
{
    partial class MarsErrorMessageBox
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
            this.MoreButton = new System.Windows.Forms.Button();
            this.CopyButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.ErrorTextBox = new System.Windows.Forms.TextBox();
            this.AdviceTextBox = new System.Windows.Forms.TextBox();
            this.ObjectNameTextBox = new System.Windows.Forms.TextBox();
            this.PegWindowTextBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.StackTraceTextBox = new System.Windows.Forms.TextBox();
            this.LocationLabel = new System.Windows.Forms.Label();
            this.LocationTextBox = new System.Windows.Forms.TextBox();
            this.CloseButton = new System.Windows.Forms.Button();
            this.ShwPicButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // MoreButton
            // 
            this.MoreButton.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.MoreButton.Location = new System.Drawing.Point(669, 56);
            this.MoreButton.Name = "MoreButton";
            this.MoreButton.Size = new System.Drawing.Size(75, 23);
            this.MoreButton.TabIndex = 0;
            this.MoreButton.Text = "More";
            this.MoreButton.UseVisualStyleBackColor = false;
            this.MoreButton.Click += new System.EventHandler(this.MoreButton_Click);
            // 
            // CopyButton
            // 
            this.CopyButton.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.CopyButton.Location = new System.Drawing.Point(669, 85);
            this.CopyButton.Name = "CopyButton";
            this.CopyButton.Size = new System.Drawing.Size(75, 23);
            this.CopyButton.TabIndex = 1;
            this.CopyButton.Text = "Copy";
            this.CopyButton.UseVisualStyleBackColor = false;
            this.CopyButton.Click += new System.EventHandler(this.CopyButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Error";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 97);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Object Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 157);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Advice";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Peg Window";
            // 
            // ErrorTextBox
            // 
            this.ErrorTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.ErrorTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ErrorTextBox.Location = new System.Drawing.Point(130, 27);
            this.ErrorTextBox.Multiline = true;
            this.ErrorTextBox.Name = "ErrorTextBox";
            this.ErrorTextBox.ReadOnly = true;
            this.ErrorTextBox.Size = new System.Drawing.Size(499, 37);
            this.ErrorTextBox.TabIndex = 6;
            // 
            // AdviceTextBox
            // 
            this.AdviceTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.AdviceTextBox.Location = new System.Drawing.Point(130, 157);
            this.AdviceTextBox.Multiline = true;
            this.AdviceTextBox.Name = "AdviceTextBox";
            this.AdviceTextBox.ReadOnly = true;
            this.AdviceTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.AdviceTextBox.Size = new System.Drawing.Size(499, 75);
            this.AdviceTextBox.TabIndex = 7;
            // 
            // ObjectNameTextBox
            // 
            this.ObjectNameTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.ObjectNameTextBox.Location = new System.Drawing.Point(130, 96);
            this.ObjectNameTextBox.Name = "ObjectNameTextBox";
            this.ObjectNameTextBox.ReadOnly = true;
            this.ObjectNameTextBox.Size = new System.Drawing.Size(499, 20);
            this.ObjectNameTextBox.TabIndex = 8;
            // 
            // PegWindowTextBox
            // 
            this.PegWindowTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.PegWindowTextBox.Location = new System.Drawing.Point(130, 70);
            this.PegWindowTextBox.Name = "PegWindowTextBox";
            this.PegWindowTextBox.ReadOnly = true;
            this.PegWindowTextBox.Size = new System.Drawing.Size(499, 20);
            this.PegWindowTextBox.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(30, 253);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Stack Trace";
            // 
            // StackTraceTextBox
            // 
            this.StackTraceTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.StackTraceTextBox.Location = new System.Drawing.Point(130, 253);
            this.StackTraceTextBox.Multiline = true;
            this.StackTraceTextBox.Name = "StackTraceTextBox";
            this.StackTraceTextBox.ReadOnly = true;
            this.StackTraceTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.StackTraceTextBox.Size = new System.Drawing.Size(499, 118);
            this.StackTraceTextBox.TabIndex = 13;
            // 
            // LocationLabel
            // 
            this.LocationLabel.AutoSize = true;
            this.LocationLabel.Location = new System.Drawing.Point(30, 121);
            this.LocationLabel.Name = "LocationLabel";
            this.LocationLabel.Size = new System.Drawing.Size(48, 13);
            this.LocationLabel.TabIndex = 14;
            this.LocationLabel.Text = "Location";
            // 
            // LocationTextBox
            // 
            this.LocationTextBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.LocationTextBox.Location = new System.Drawing.Point(130, 122);
            this.LocationTextBox.Name = "LocationTextBox";
            this.LocationTextBox.ReadOnly = true;
            this.LocationTextBox.Size = new System.Drawing.Size(499, 20);
            this.LocationTextBox.TabIndex = 15;
            // 
            // CloseButton
            // 
            this.CloseButton.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.CloseButton.Location = new System.Drawing.Point(669, 27);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 16;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = false;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // ShwPicButton
            // 
            this.ShwPicButton.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.ShwPicButton.Location = new System.Drawing.Point(669, 116);
            this.ShwPicButton.Name = "ShwPicButton";
            this.ShwPicButton.Size = new System.Drawing.Size(75, 23);
            this.ShwPicButton.TabIndex = 17;
            this.ShwPicButton.Text = "Show Pic";
            this.ShwPicButton.UseVisualStyleBackColor = false;
            this.ShwPicButton.Click += new System.EventHandler(this.ShwPicButton_Click);
            // 
            // MarsErrorMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.ClientSize = new System.Drawing.Size(770, 242);
            this.Controls.Add(this.ShwPicButton);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.LocationTextBox);
            this.Controls.Add(this.LocationLabel);
            this.Controls.Add(this.StackTraceTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.PegWindowTextBox);
            this.Controls.Add(this.ObjectNameTextBox);
            this.Controls.Add(this.AdviceTextBox);
            this.Controls.Add(this.ErrorTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CopyButton);
            this.Controls.Add(this.MoreButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MarsErrorMessageBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarsErrorMessageBox";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.MarsErrorMessageBox_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button MoreButton;
        private System.Windows.Forms.Button CopyButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox ErrorTextBox;
        private System.Windows.Forms.TextBox AdviceTextBox;
        private System.Windows.Forms.TextBox ObjectNameTextBox;
        private System.Windows.Forms.TextBox PegWindowTextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox StackTraceTextBox;
        private System.Windows.Forms.Label LocationLabel;
        private System.Windows.Forms.TextBox LocationTextBox;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button ShwPicButton;
    }
}