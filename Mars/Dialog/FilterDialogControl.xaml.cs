using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for FilterDialogControl.xaml
    /// </summary>
    public partial class FilterDialogControl : Window
    {

        private ObservableCollection<Person> Persons = new ObservableCollection<Person>();
        public FilterDialogControl()
        {
            InitializeComponent();

            Persons.Add(new Person()
            {
                Name = "Orange",
                Age = 2,
            });

            Persons.Add(new Person()
            {
                Name = "Tomato",
                Age = 1,
            });

            Persons.Add(new Person()
            {
                Name = "Coconut",
                Age = 15,
            });

            Persons.Add(new Person()
            {
                Name = "Banana",
                Age = 6,
            });

            listBox.ItemsSource = Persons;
        }
    }


    public class Person
    {
        public string Name
        {
            get;
            set;
        }

        public int Age
        {
            get;
            set;
        }
    }
}
