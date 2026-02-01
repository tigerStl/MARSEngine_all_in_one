using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Mars.Utility.visualObjects
{
    internal class MarsObjectsAndChildrenHelper
    {
        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static List<T> GetChildList<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;
            List<T> result = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
               
                var itm = (child as T) ?? GetChildOfType<T>(child);
                if (itm != null) result.Add(itm as T); ;
            }
            return result;
        }
    }
}
