using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mars.Utility
{
    public class CustomRoutedEventArgs : RoutedEventArgs
    {
        private readonly string text;

        public string Text
        {
            get { return text; }
        }

        public CustomRoutedEventArgs(RoutedEvent routedEvent, string text)
            : base(routedEvent)
        {
            this.text = text;
        }


        public CustomRoutedEventArgs(RoutedEvent routedEvent, long id)
            : base(routedEvent)
        {
            this.Id = id;
        }

        private long _id;

        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

    }
}
