using System;
#if _VEDIO_TIGER_
using WMPLib;
#endif


namespace TestFrameMonitor
{
    public partial class DockableFormMARS : System.Windows.Forms.Form
    {
#if _VEDIO_TIGER_
        static WindowsMediaPlayer gObjPlayer=null;
#endif
        public DockableFormMARS()
        {
            InitializeComponent();
        }

        private void InitPlayer()
        {
#if _VEDIO_TIGER_
            if (gObjPlayer == null)
                gObjPlayer = new WindowsMediaPlayer();
#endif
        }

        private void panel1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //
        }
    }
}
