using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TestFrameMonitor
{

    public partial class AnimatedHint : System.Windows.Forms.Form
    {

        public const int AW_HOR_POSITIVE = 0X1;
        /// <span class="code-SummaryComment"><summary></span>
        /// Animates the window from right to left. 
        /// This flag can be used with roll or slide animation.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_HOR_NEGATIVE = 0X2;
        /// <span class="code-SummaryComment"><summary></span>
        /// Animates the window from top to bottom. 
        /// This flag can be used with roll or slide animation.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_VER_POSITIVE = 0X4;
        /// <span class="code-SummaryComment"><summary></span>
        /// Animates the window from bottom to top. 
        /// This flag can be used with roll or slide animation.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_VER_NEGATIVE = 0X8;
        /// <span class="code-SummaryComment"><summary></span>
        /// Makes the window appear to collapse inward 
        /// if AW_HIDE is used or expand outward if the AW_HIDE is not used.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_CENTER = 0X10;
        /// <span class="code-SummaryComment"><summary></span>
        /// Hides the window. By default, the window is shown.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_HIDE = 0X10000;
        /// <span class="code-SummaryComment"><summary></span>
        /// Activates the window.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_ACTIVATE = 0X20000;
        /// <span class="code-SummaryComment"><summary></span>
        /// Uses slide animation. By default, roll animation is used.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_SLIDE = 0X40000;
        /// <span class="code-SummaryComment"><summary></span>
        /// Uses a fade effect. 
        /// This flag can be used only if hwnd is a top-level window.
        /// <span class="code-SummaryComment"></summary></span>
        public const int AW_BLEND = 0X80000;
        /// <span class="code-SummaryComment"><summary></span>
        /// Animates a window.
        /// <span class="code-SummaryComment"></summary></span>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int AnimateWindow(IntPtr hwand, int dwTime, int dwFlags);

        private static AnimatedHint objResult = null;

        public AnimatedHint()
        {
            InitializeComponent();
        }

        private void AnimatedHint_Load(object sender, EventArgs e)
        {


        }

        public void SetHintInfo(string strHint)
        {
            this.HintLabel.Text = string.Format("Hint:[{0}]\r\nThe Hint window will close in 5 seconds.", strHint);
        }

        private void AnimatedHint_FormClosing(object sender, FormClosingEventArgs e)
        {
            AnimateWindow(this.Handle, 1000, AW_VER_NEGATIVE | AW_HIDE);
        }

        private void SetWindowPosition(int x, int y)
        {
            this.SetWindowPosition(x, y);
            this.Visible = true;
        }

        private void AnimatedHint_Shown(object sender, EventArgs e)
        {
            AnimateWindow(this.Handle, 1000, AW_VER_NEGATIVE);
        }

        public static AnimatedHint CreateHintForm(int x, int y)
        {
            AnimatedHint objResult = new AnimatedHint();
            objResult.Visible = false;
            objResult.SetWindowPosition(x, y);
            objResult.Visible = true;
            Thread thrdWait = new Thread(new ThreadStart(delegate ()
            {

                Thread.Sleep(5000);
                if (objResult != null)
                    objResult.Close();
                objResult = null;

            }));
            thrdWait.Start();
            return objResult;
        }
    }
}
