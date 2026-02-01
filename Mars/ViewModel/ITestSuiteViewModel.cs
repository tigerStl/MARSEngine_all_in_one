using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using System.Windows.Input;
namespace Mars.ViewModel
{
    public interface ITestSuiteViewModel
    {
        bool SaveTestSuite();
        ICommand SaveCommand { get; set; }
        ICommand ClearCommand { get; set; }
    }
}
