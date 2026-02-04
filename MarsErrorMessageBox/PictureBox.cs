using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MarsErrorMessageBox
{
    public partial class PictureBox : Form
    {
        public PictureBox()
        {
            InitializeComponent();
            Image image = Image.FromFile(@"c:\temp\Summit.jpg");
            PicBox.Image = image;
            PicBox.Width = image.Width;
            PicBox.Height = image.Height;
        }

        private void PicBox_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }
    }
}
