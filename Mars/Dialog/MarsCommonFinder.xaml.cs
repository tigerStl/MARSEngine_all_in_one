using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for MarsCommonFinder.xaml
    /// </summary>
    public partial class MarsCommonFinder : Window
    {
        
        public MarsCommonFinder()
        {
            InitializeComponent();
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }


        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
            "DialogResult",
            typeof(bool?),
            typeof(MarsCommonFinder),
            new PropertyMetadata(DialogResultChanged));
        private static void DialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window != null && window.IsVisible)
            {
                window.DialogResult = e.NewValue as bool?;
            }
        }

        public static object GetDialogResult(Window target)
        {
            bool? bReturn = null;

            try
            {
                if (target != null)
                {
                    bReturn = target.GetValue(DialogResultProperty) as bool?;
                }
            }
            catch
            {
                // Just eat the exception if there is one so that the application will not crash.
            }
            return (object)bReturn;
        }

        // Property setter.
        public static void SetDialogResult(Window target, bool? value)
        {
            if (target != null)
            {
                target.SetValue(DialogResultProperty, value);
            }
        }
    }
}
