using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mars.ViewModel
{
    public interface ITestCaseViewModel
    {
        bool SaveTestCase();
        ICommand SaveCommand { get; set; }
        ICommand ClearCommand { get; set; }
    }
}
