using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.ExecuteStoryboard
{
    public partial class MarsMFACodeInputForm : Form
    {
        public int secondsLeft { get; set; } = 0;
        public MarsMFACodeInputForm()
        {
            InitializeComponent();
        }

        public void setTimer(int scnd)
        {
            this.secondsLeft = scnd;
            this.timer1.Stop();
            this.timer1.Interval = 1000;
            this.timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.lblLeftSeconds.Text = $"{this.secondsLeft--}";
            if (this.secondsLeft <= 0)
            {
                this.DialogResult = DialogResult.Cancel;
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        public string currrentMFACode
        {
            get => this.mfaCodeEdit.Text;
            //set => this.mfaCodeEdit.Text = value;
        }
    }
}
