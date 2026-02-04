using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomComboBox
{
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        private void UserControl1_SizeChanged(object sender, EventArgs e)
        {
            // Calculate size of checked list box.
            checkedListBox1.Size = new Size(DisplayRectangle.Width - checkedListBox1.Left - 5,
                                            DisplayRectangle.Height - checkedListBox1.Top - button1.Height - 10);
        }
    }
}
