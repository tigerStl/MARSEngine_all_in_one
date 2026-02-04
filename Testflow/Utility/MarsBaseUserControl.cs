using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Mars.Utility
{
    public class MarsBaseUserControl: UserControl
    {
        public static readonly RoutedEvent MarsConsumePropertyChangeEvent = EventManager.RegisterRoutedEvent("ConsumeProperty", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MarsBaseUserControl));
        public event RoutedEventHandler MarsPropertyChangeEvent
        {
            add { AddHandler(MarsConsumePropertyChangeEvent, value); }
            remove { RemoveHandler(MarsConsumePropertyChangeEvent,value); }
        }
        public void RaiseConsumePropertyChangeEvent(string strProperty)
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(MarsBaseUserControl.MarsConsumePropertyChangeEvent);
            RaiseEvent(newEventArgs);
        }
    }

    public class MarsSystemUtilty
    {
        public static void ShowSpecialMessage(DispatcherObject objHost, string strMessage, string strCaption = "Hint")
        {
            if (objHost == null) return;
            objHost.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                   (new Action(delegate () {
                       MessageBox.Show(strMessage, strCaption);
                   }))
                   );
        }
    }
}
