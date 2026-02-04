namespace TestFrameMonitor
{
    partial class AnimatedHint
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnimatedHint));
            this.HintLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // HintLabel
            // 
            this.HintLabel.AutoSize = true;
            this.HintLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HintLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.HintLabel.Location = new System.Drawing.Point(2, 13);
            this.HintLabel.Name = "HintLabel";
            this.HintLabel.Size = new System.Drawing.Size(43, 18);
            this.HintLabel.TabIndex = 0;
            this.HintLabel.Text = "Hint:";
            // 
            // AnimatedHint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 53);
            this.Controls.Add(this.HintLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AnimatedHint";
            this.ShowInTaskbar = false;
            this.Text = "Mars Test Framework Hint...";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AnimatedHint_FormClosing);
            this.Load += new System.EventHandler(this.AnimatedHint_Load);
            this.Shown += new System.EventHandler(this.AnimatedHint_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label HintLabel;
    }
}