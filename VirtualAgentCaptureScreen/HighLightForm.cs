using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualAgentCaptureScreen
{
    public partial class HighLightForm : Form
    {
        public string targetFileName;
        private bool isFirst = false;
        
        
        private void highlightAndClose()
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    this.Show();
                    this.Update();
                    System.Threading.Thread.Sleep(500);
                    this.Hide();
                    System.Threading.Thread.Sleep(200);
                }

                if ("-NoFile".Equals(targetFileName, StringComparison.OrdinalIgnoreCase)) return;
                
                //Bitmap bmp = new Bitmap(Screen.FromPoint(new Point(ix, iy)).Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                Bitmap bmp = new Bitmap(this.Width,this.Height);
                Graphics graphics = Graphics.FromImage(bmp as Image);
                graphics.CopyFromScreen(this.Left, this.Top, 0, 0, new Size(this.Width , this.Height));

                bmp.Save(targetFileName, ImageFormat.Bmp);

                Console.WriteLine($"saved to {targetFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                this.Close();
            }
        }

        public HighLightForm()
        {
            InitializeComponent();

            timer1.Enabled  = true;
            timer1.Interval = 500;
            
        }

        private void HighLightForm_Shown(object sender, EventArgs e)
        {
            if (!isFirst) {
                this.BackColor = Color.White;
                this.TransparencyKey = Color.White;
                isFirst = true;
                
            }
        }

        private void HighLightForm_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                this.ClientRectangle,
                Color.Red, 1, ButtonBorderStyle.Solid,
                Color.Red, 1, ButtonBorderStyle.Solid,
                Color.Red, 1, ButtonBorderStyle.Solid,
                Color.Red, 1, ButtonBorderStyle.Solid
                );
        }

        private void HighLightForm_Load(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            highlightAndClose();
            //autoCloseThread = new System.Threading.Thread(new System.Threading.ThreadStart(highlightAndClose));
            //autoCloseThread.Start();
        }
    }
}
