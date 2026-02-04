using Mars.AutoTestingDriver.WebHelpers;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Mars.AutoTestingDriver.webSupport.TestDialog
{

    public partial class MARSXpathDialog: Form
    {

        private static MARSXpathDialog instance = null;
        private bool isForceToClose = false;
        private MARSXpathDialog()
        {
            InitializeComponent();
            TopMost = true;
            Hide();
        }

        public static MARSXpathDialog GetInstance()
        {
            if (instance == null)
            {
                instance = new MARSXpathDialog();
            }
            return instance;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var marsWebDriv = MARSWebDriver.GetInstance();
            if (marsWebDriv == null) {
                return;
            }
            if (xpathTxt.Text.Trim().Length <= 0) return;

            var lst = marsWebDriv.GetObjectsByXpath(xpathTxt.Text);
            listBox1.Items.Clear();

            foreach (var obj in lst) {
                MarsWebObjListBoxItem itm = new MarsWebObjListBoxItem();
                itm.Text = $"{obj.TagName}-{obj.Text}";
                itm.Tag = obj;
                listBox1.Items.Add(itm);
            }
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = listBox1.SelectedItem as MarsWebObjListBoxItem;
            if (selected != null)
            {
                textBox1.Clear();
                var z = selected.Tag as IWebElement;
                if (z != null) {
                    textBox1.Text = z.GetAttribute("innerHTML");
                }

            }
        }

        private void MARSXpathDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (isForceToClose) { 
                
            //}
            //else
            //    e.Cancel = true;
        }

        private void MARSXpathDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            instance = null;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            isForceToClose = true;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var marsWebDriv = MARSWebDriver.GetInstance();
            var slcted = listBox1.SelectedItem as MarsWebObjListBoxItem;
            MarsWEBHighlighter.HighlightElement3Times(marsWebDriv.GetChromiumDriver(), slcted.Tag as IWebElement);
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(webAddressTextBox.Text)){
                MessageBox.Show("Please set URL first", "INFO",MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MARSKeywordWebHelpers.currentWebStepMode = MARSStep_WebConnectionMode._bySelenium;
            var webDriv = MARSWebDriver.GetInstance();
            string strError = "";

            bool isOK = MARSWebDriver.ConnectToWebByURL(webAddressTextBox.Text, ref strError);
            if (!isOK)
            {
                MessageBox.Show($"Can't connect to Web with Error\r\n{strError}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var listOfWindows = MARSWebDriver.GetWindowsInfo();
            if (listOfWindows.Count <= 0) return;

            webWidnowList.Items.Clear();
            webWidnowList.Items.AddRange(listOfWindows.ToArray());
            //(MARSKeywordWebHelpers.currentWebStepMode

        }

        private void webWidnowList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var z = webWidnowList.SelectedItem as MarsSeleniumWindowsInfo;
            if (z == null) return;
            webWindowURL.Text = z.url;

            currentWebHTML.Text = MARSWebDriver.getWindowHTML(z.windowsHandle, z.url)??"NO PAGE SOURCE IS GET";
            
        }
    }
}
