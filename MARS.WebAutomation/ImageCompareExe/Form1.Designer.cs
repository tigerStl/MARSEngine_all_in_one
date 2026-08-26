namespace ImageCompareExe;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.topPanel = new System.Windows.Forms.Panel();
        this.btnMark = new System.Windows.Forms.Button();
        this.btnSwap = new System.Windows.Forms.Button();
        this.btnLoadB = new System.Windows.Forms.Button();
        this.btnLoadA = new System.Windows.Forms.Button();
        this.lblSplit = new System.Windows.Forms.Label();
        this.trackSplit = new System.Windows.Forms.TrackBar();
        this.canvasPanel = new System.Windows.Forms.Panel();
        this.topPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.trackSplit)).BeginInit();
        this.SuspendLayout();
        //
        // topPanel
        //
        this.topPanel.Controls.Add(this.btnMark);
        this.topPanel.Controls.Add(this.btnSwap);
        this.topPanel.Controls.Add(this.btnLoadB);
        this.topPanel.Controls.Add(this.btnLoadA);
        this.topPanel.Controls.Add(this.lblSplit);
        this.topPanel.Controls.Add(this.trackSplit);
        this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.topPanel.Location = new System.Drawing.Point(0, 0);
        this.topPanel.Name = "topPanel";
        this.topPanel.Size = new System.Drawing.Size(1200, 56);
        this.topPanel.TabIndex = 0;
        //
        // btnMark
        //
        this.btnMark.Location = new System.Drawing.Point(308, 15);
        this.btnMark.Name = "btnMark";
        this.btnMark.Size = new System.Drawing.Size(100, 28);
        this.btnMark.TabIndex = 5;
        this.btnMark.Text = "Mark Diff";
        this.btnMark.UseVisualStyleBackColor = true;
        this.btnMark.Click += new System.EventHandler(this.btnMark_Click);
        //
        // btnSwap
        //
        this.btnSwap.Location = new System.Drawing.Point(202, 15);
        this.btnSwap.Name = "btnSwap";
        this.btnSwap.Size = new System.Drawing.Size(100, 28);
        this.btnSwap.TabIndex = 4;
        this.btnSwap.Text = "Swap A/B";
        this.btnSwap.UseVisualStyleBackColor = true;
        this.btnSwap.Click += new System.EventHandler(this.btnSwap_Click);
        //
        // btnLoadB
        //
        this.btnLoadB.Location = new System.Drawing.Point(106, 15);
        this.btnLoadB.Name = "btnLoadB";
        this.btnLoadB.Size = new System.Drawing.Size(90, 28);
        this.btnLoadB.TabIndex = 3;
        this.btnLoadB.Text = "Load B";
        this.btnLoadB.UseVisualStyleBackColor = true;
        this.btnLoadB.Click += new System.EventHandler(this.btnLoadB_Click);
        //
        // btnLoadA
        //
        this.btnLoadA.Location = new System.Drawing.Point(10, 15);
        this.btnLoadA.Name = "btnLoadA";
        this.btnLoadA.Size = new System.Drawing.Size(90, 28);
        this.btnLoadA.TabIndex = 2;
        this.btnLoadA.Text = "Load A";
        this.btnLoadA.UseVisualStyleBackColor = true;
        this.btnLoadA.Click += new System.EventHandler(this.btnLoadA_Click);
        //
        // lblSplit
        //
        this.lblSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.lblSplit.AutoSize = true;
        this.lblSplit.Location = new System.Drawing.Point(860, 20);
        this.lblSplit.Name = "lblSplit";
        this.lblSplit.Size = new System.Drawing.Size(57, 15);
        this.lblSplit.TabIndex = 1;
        this.lblSplit.Text = "Split: 50%";
        //
        // trackSplit
        //
        this.trackSplit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.trackSplit.Location = new System.Drawing.Point(430, 12);
        this.trackSplit.Maximum = 100;
        this.trackSplit.Name = "trackSplit";
        this.trackSplit.Size = new System.Drawing.Size(420, 45);
        this.trackSplit.TabIndex = 0;
        this.trackSplit.TickFrequency = 5;
        this.trackSplit.Value = 50;
        this.trackSplit.Scroll += new System.EventHandler(this.trackSplit_Scroll);
        //
        // canvasPanel
        //
        this.canvasPanel.BackColor = System.Drawing.Color.Black;
        this.canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.canvasPanel.Location = new System.Drawing.Point(0, 56);
        this.canvasPanel.Name = "canvasPanel";
        this.canvasPanel.Size = new System.Drawing.Size(1200, 724);
        this.canvasPanel.TabIndex = 1;
        this.canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.canvasPanel_Paint);
        this.canvasPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseDown);
        this.canvasPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseMove);
        this.canvasPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseUp);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 780);
        this.Controls.Add(this.canvasPanel);
        this.Controls.Add(this.topPanel);
        this.Name = "Form1";
        this.Text = "Image Compare EXE";
        this.topPanel.ResumeLayout(false);
        this.topPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.trackSplit)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private Panel topPanel;
    private Button btnMark;
    private Button btnSwap;
    private Button btnLoadB;
    private Button btnLoadA;
    private Label lblSplit;
    private TrackBar trackSplit;
    private Panel canvasPanel;
}
