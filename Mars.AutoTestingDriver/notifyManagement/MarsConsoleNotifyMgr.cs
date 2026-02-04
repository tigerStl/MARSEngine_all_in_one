using Mars.AutoTestingDriver.webSocketService;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.notifyManagement
{
    class HiddenForm : Form
    {
        private NotifyIcon notifyIcon;
        private static MenuItem startSvcClickMenu = null;

        public HiddenForm()
        {
            // Initialize the form
            InitializeComponent();

            // Create the NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "MARS svc engine";
            notifyIcon.Icon = new System.Drawing.Icon(Properties.Resources.notifySvcIco, 40, 40);

            // Display the NotifyIcon in the system tray
            notifyIcon.Visible = true;

            //ContextMenu contextMenu = new ContextMenu();
            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("About...", mnuAboutClick);
            trayMenu.MenuItems.Add("-");
            startSvcClickMenu = trayMenu.MenuItems.Add("Start Web Socket svc", mnuStartWebSocketSvcClick);
            trayMenu.MenuItems.Add("Start MARS Spy++...", mnuStartSpyClick);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", mnuExitClick);

            //startSvcClickMenu.Checked = true;

            notifyIcon.ContextMenu = trayMenu;
        }
    

        private static void mnuAboutClick(object sender, EventArgs e)
        {
            MessageBox.Show("not implemented");
        }
        private static void mnuExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        private async static void mnuStartWebSocketSvcClick(object sender, EventArgs e)
        {
            //MessageBox.Show("not implemented");
            if (startSvcClickMenu.Checked) return;
            startSvcClickMenu.Checked = true;
            await MarsWebSocketServer.startDefaultSvc();
        }

        private static void mnuStartSpyClick(object sender, EventArgs e)
        {
            MessageBox.Show("not implemented");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up any resources being used
                notifyIcon.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Initialize form components
            // Set properties, such as FormBorderStyle, WindowState, etc.
            // You can leave them with default values if you want a completely hidden form
        }
    }


    internal class MarsConsoleNotifyMgr
    {
        private static ContextMenu trayMenu = null;
        private static HiddenForm trayIconFrm = null;
        public static void CreateNotifyAndListen()
        {
            //trayIconFrm = new HiddenForm();
            //if (trayMenu == null) InitTrayMenu();            
        }

        

    }
}
