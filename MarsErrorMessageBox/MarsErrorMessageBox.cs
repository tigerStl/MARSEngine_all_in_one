using System;
using System.Drawing;
using System.Windows.Forms;

namespace MarsErrorMessageBox
{
    public partial class MarsErrorMessageBox : Form
    {
        private bool moreState = false;
        private int dialogHeightLess;
        private int dialogHeightMore = 420;// 570;
        private int dialogWidth;

        public static Form ShowMarsErrorMessageBox(string errorMessage, string pegWindowName, string objectName, string location, string advice, string exception, string stackTrace)
        {
            var box = new MarsErrorMessageBox(errorMessage, pegWindowName, objectName, location, advice, exception, stackTrace);

            box.Show();
            return box;

        }

        public static int ShowMarsError(string errorMessage, 
            string pegWindowName, 
            string objectName, 
            string location, 
            string advice, 
            string exception, 
            string stackTrace,
            bool isNotShowDialog)
        {
            if (isNotShowDialog)
            {
                Console.WriteLine($"Error happends on Object:[{pegWindowName}].[{objectName}]\r\nError:{errorMessage}\r\n\tLocation:{location}\r\n\t{advice}\r\n\t[{stackTrace}]");
                return 1;
            }

            var box = new MarsErrorMessageBox(errorMessage, pegWindowName, objectName, location, advice, exception, stackTrace);
            DialogResult rslt = box.ShowDialog();
            if (rslt == DialogResult.Ignore)
            {
                return 2; //continue to run
            }
            else if (rslt == DialogResult.OK) return 1;
            else return 0;

        }
        public MarsErrorMessageBox(string errorMessage, string pegWindowName, string objectName, string location, string advice, string exception, string stackTrace)
        {
            InitializeComponent();
            this.ErrorTextBox.Text = errorMessage;
            this.PegWindowTextBox.Text = pegWindowName;
            this.ObjectNameTextBox.Text = objectName;
            this.LocationTextBox.Text = location;
            this.AdviceTextBox.Text = advice;
            //this.ExceptionTextBox.Text = exception;
            this.StackTraceTextBox.Text = stackTrace;
        }

        public MarsErrorMessageBox()
        {
            InitializeComponent();
        }

        private void MarsErrorMessageBox_Load(object sender, EventArgs e)
        {
            dialogHeightLess = this.Size.Height;
            dialogWidth = this.Size.Width;
        }


        private void MoreButton_Click(object sender, EventArgs e)
        {
            if (moreState == true)
            {
                moreState = false;
                MoreButton.Text = "More";
                this.Size = new Size(dialogWidth, dialogHeightLess);
            }
            else
            {
                moreState = true;
                MoreButton.Text = "Less";
                this.Size = new Size(dialogWidth, dialogHeightMore);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            string errorReport = "Error:" + "\r\n" + ErrorTextBox.Text + "\r\n\r\n" +
                                 "PegWindow:" + "\r\n" + PegWindowTextBox.Text + "\r\n\r\n" +
                                 "Object Name:" + "\r\n" + ObjectNameTextBox.Text + "\r\n\r\n" +
                                 "Location:" + "\r\n" + LocationTextBox.Text + "\r\n\r\n" +
                                 "Advice:" + "\r\n" + AdviceTextBox.Text + "\r\n\r\n" +
                                 //"Exception:" + "\r\n" + ExceptionTextBox.Text + "\r\n\r\n" +
                                 "Stack Trace:" + "\r\n" + StackTraceTextBox.Text + "\r\n\r\n";

            try
            {
                Clipboard.SetData(DataFormats.Text, (Object)errorReport);
            }
            catch
            {

            }
        }

        private void ShwPicButton_Click(object sender, EventArgs e)
        {
            PictureBox pb = new PictureBox();
            pb.Show();
        }
    }
}
