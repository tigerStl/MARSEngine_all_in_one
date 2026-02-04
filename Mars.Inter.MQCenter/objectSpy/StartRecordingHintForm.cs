using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Mars.Inter.MQCenter.objectSpy
{
    public partial class StartRecordingHintForm : Form
    {
        private int countDown = 5;
        public StartRecordingHintForm()
        {
            InitializeComponent();            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            if ((countDown--) <= 0) {
                this.DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                this.label1.Text = countDown.ToString();
                Invalidate();
            }
        }

        public void setLabelHint(string hint="Beginning to Recording")
        {
            this.label2.Text = hint;
        }

        private void StartRecordingHintForm_Load(object sender, EventArgs e)
        {

        }

        private void StartRecordingHintForm_Shown(object sender, EventArgs e)
        {
            timer1.Start();
        }
        /// <summary>
        /// a way to close the window if not work 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        public static void BeginToRecord(bool isStart = true)
        {
            StartRecordingHintForm tmpForm = new StartRecordingHintForm();
            tmpForm.setLabelHint(isStart? "Beginning to Recording" : "Beginning to Stop");
            tmpForm.ShowDialog();
            tmpForm = null;
        }
    }
}
